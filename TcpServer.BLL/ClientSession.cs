using System;
using System.Net.Sockets;
using System.Threading;
using TcpServer.Common;
using TcpServer.Common.Helpers;
using TcpServer.Model;

namespace TcpServer.BLL
{
    /// <summary>
    /// Client session - encapsulates the receive loop and send logic of a single TCP client.
    /// Convention: network reads / writes are locked against interleaving; a delay is added inside the loop;
    ///             threads are set as background threads.
    /// </summary>
    public class ClientSession : IDisposable
    {
        #region Fields

        private readonly Socket _socket;
        private readonly object _sendLock = new object();
        private readonly int _bufferSize;
        private readonly int _ioIntervalMs;
        private Thread _receiveThread;
        private volatile bool _closed;
        private bool _disposed;

        #endregion

        #region Properties

        /// <summary>Unique session identifier.</summary>
        public string SessionId { get; private set; }

        /// <summary>Local listening port this session belongs to.</summary>
        public int LocalPort { get; private set; }

        /// <summary>Client remote endpoint (IP:Port).</summary>
        public string RemoteEndPoint { get; private set; }

        /// <summary>Time the connection was established.</summary>
        public DateTime ConnectedTime { get; private set; }

        /// <summary>Cumulative number of bytes received.</summary>
        public long BytesReceived { get; private set; }

        /// <summary>Cumulative number of bytes sent.</summary>
        public long BytesSent { get; private set; }

        /// <summary>Last activity time.</summary>
        public DateTime LastActiveTime { get; private set; }

        /// <summary>Whether the session is still valid.</summary>
        public bool IsConnected
        {
            get { return !_closed && !_disposed && _socket != null; }
        }

        #endregion

        #region Events

        /// <summary>Data received event.</summary>
        public event EventHandler<PortDataEventArgs> DataReceived;

        /// <summary>Data sent event.</summary>
        public event EventHandler<PortDataEventArgs> DataSent;

        /// <summary>Session closed event.</summary>
        public event EventHandler<ClientSession> SessionClosed;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="socket">Connected client socket; must not be null.</param>
        /// <param name="localPort">Local listening port it belongs to.</param>
        /// <param name="sessionId">Session identifier.</param>
        /// <param name="bufferSize">Receive buffer size.</param>
        /// <param name="ioIntervalMs">Receive loop interval in milliseconds.</param>
        public ClientSession(Socket socket, int localPort, string sessionId, int bufferSize, int ioIntervalMs)
        {
            SessionId = string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId;
            LocalPort = localPort;
            _socket = socket;
            _bufferSize = bufferSize < 1024 ? AppConstants.RECEIVE_BUFFER_SIZE : bufferSize;
            _ioIntervalMs = ioIntervalMs < 0 ? AppConstants.IO_MIN_INTERVAL_MS : ioIntervalMs;
            _closed = false;
            _disposed = false;

            ConnectedTime = DateTime.Now;
            LastActiveTime = DateTime.Now;
            BytesReceived = 0L;
            BytesSent = 0L;
            RemoteEndPoint = string.Empty;

            try
            {
                if (_socket != null && _socket.RemoteEndPoint != null)
                {
                    RemoteEndPoint = _socket.RemoteEndPoint.ToString();
                }
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Warn(string.Format("Port {0}: failed to get client address: {1}", LocalPort, ex.Message));
                RemoteEndPoint = "Unknown";
            }

            // Added 2026-09-14: enable TCP keep-alive probing, to clean up zombie sessions caused by a client
            // losing power or being unplugged.
            ConfigureKeepAlive();

            // Added 2026-09-14: send timeout + Nagle disabled; see the method comments for details.
            ConfigureSocketOptions();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the receive loop (background thread).
        /// </summary>
        /// <returns>true when the start succeeded.</returns>
        public bool StartReceive()
        {
            if (_socket == null || _closed)
            {
                return false;
            }

            try
            {
                _receiveThread = new Thread(ReceiveLoop);
                _receiveThread.IsBackground = true;
                _receiveThread.Name = string.Format("TcpRecv_{0}_{1}", LocalPort, SessionId);
                _receiveThread.Start();
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error(string.Format("Port {0}: failed to start the receive thread: {1}", LocalPort, ex.Message), ex);
                return false;
            }
        }

        /// <summary>
        /// Sends data to this client (locked to prevent concurrent interleaving).
        /// </summary>
        /// <param name="data">Byte array to send.</param>
        /// <param name="error">Failure reason.</param>
        /// <returns>true when the send succeeded.</returns>
        public bool Send(byte[] data, out string error)
        {
            error = string.Empty;

            if (data == null || data.Length == 0)
            {
                error = "Nothing to send";
                return false;
            }

            if (!IsConnected)
            {
                error = "Connection closed";
                return false;
            }

            // P / V operation: sends on the same session must be serialized to prevent packets from merging.
            lock (_sendLock)
            {
                try
                {
                    int totalSent = 0;
                    while (totalSent < data.Length)
                    {
                        int sent = _socket.Send(data, totalSent, data.Length - totalSent, SocketFlags.None);
                        if (sent <= 0)
                        {
                            error = "Send interrupted";
                            return false;
                        }

                        totalSent += sent;
                    }

                    BytesSent += totalSent;
                    LastActiveTime = DateTime.Now;

                    RaiseDataSent(data, data.Length);
                    return true;
                }
                catch (SocketException ex)
                {
                    // Added 2026-09-14: distinguish "send timeout" from an ordinary send failure, so the cause
                    // can be identified quickly on site.
                    if (ex.SocketErrorCode == SocketError.TimedOut)
                    {
                        error = string.Format("Send timed out (not finished within {0} ms; the client may not be reading data)",
                            _socket.SendTimeout);
                        LogHelper.Instance.Warn(string.Format("Port {0} -> {1}: send timed out, session closed: {2}",
                            LocalPort, RemoteEndPoint, ex.SocketErrorCode));
                        Close();
                        return false;
                    }

                    error = "Send failed:" + ex.Message;
                    LogHelper.Instance.Warn(string.Format("Port {0} -> {1}: send failed: {2}",
                        LocalPort, RemoteEndPoint, ex.Message));
                    Close();
                    return false;
                }
                catch (Exception ex)
                {
                    error = "Exception while sending:" + ex.Message;
                    LogHelper.Instance.Error(string.Format("Port {0}: exception while sending data: {1}", LocalPort, ex.Message), ex);
                    Close();
                    return false;
                }
            }
        }

        /// <summary>
        /// Closes the session (idempotent, may be called repeatedly).
        /// </summary>
        public void Close()
        {
            if (_closed)
            {
                return;
            }

            _closed = true;

            try
            {
                if (_socket != null)
                {
                    try { _socket.Shutdown(SocketShutdown.Both); }
                    catch (Exception) { }

                    _socket.Close();
                }
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Warn(string.Format("Port {0}: exception while closing client connection: {1}", LocalPort, ex.Message));
            }
            finally
            {
                EventHandler<ClientSession> handler = SessionClosed;
                if (handler != null)
                {
                    try { handler(this, this); }
                    catch (Exception) { }
                }
            }
        }

        /// <summary>
        /// Converts to a client information entity (for UI binding).
        /// </summary>
        /// <returns>Client information object, never null.</returns>
        public ClientInfo ToClientInfo()
        {
            ClientInfo info = new ClientInfo();
            info.SessionId = SessionId;
            info.LocalPort = LocalPort;
            info.RemoteEndPoint = RemoteEndPoint;
            info.ConnectedTime = ConnectedTime;
            info.BytesReceived = BytesReceived;
            info.BytesSent = BytesSent;
            info.LastActiveTime = LastActiveTime;
            return info;
        }

        /// <summary>
        /// Releases resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Close();
            _disposed = true;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Configures TCP keep-alive probing (added 2026-09-14).
        /// Purpose: when a client loses power or its network cable is pulled, no FIN or RST is sent, so the
        ///          server-side Receive blocks forever and the session becomes a "zombie" occupying a slot in
        ///          the connection limit. With keep-alive enabled, an unanswered probe marks the peer as lost
        ///          and the session is cleaned up.
        /// Essential difference from "disconnect on idle timeout": keep-alive probes are answered directly by
        ///          the peer operating system kernel - the peer application layer need do nothing. As long as
        ///          the peer machine is online, the connection is not dropped even if not a single byte is sent
        ///          for a long time, so long-lived connections are completely unaffected.
        /// Note: Windows defaults KeepAliveTime to 2 hours, so the probe timings must be set explicitly;
        ///       writing only KeepAlive=true is equivalent to not enabling it.
        /// </summary>
        private void ConfigureKeepAlive()
        {
            if (_socket == null)
            {
                return;
            }

            if (!ConfigHelper.KeepAliveEnabled)
            {
                return;
            }

            int idleMs = ConfigHelper.KeepAliveIdleSeconds * 1000;
            int intervalMs = ConfigHelper.KeepAliveIntervalSeconds * 1000;

            try
            {
                _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                // SIO_KEEPALIVE_VALS: 12 bytes = [enabled(4B)] [idle time ms(4B)] [probe interval ms(4B)]
                // .NET Framework offers no TcpKeepAliveTime enumeration, so IOControl is the only way to set
                // these precisely.
                byte[] optionValues = new byte[12];
                BitConverter.GetBytes((uint)1).CopyTo(optionValues, 0);
                BitConverter.GetBytes((uint)idleMs).CopyTo(optionValues, 4);
                BitConverter.GetBytes((uint)intervalMs).CopyTo(optionValues, 8);

                _socket.IOControl(IOControlCode.KeepAliveValues, optionValues, null);
            }
            catch (Exception ex)
            {
                // A failed keep-alive setting does not affect normal traffic; only a warning is logged, so an
                // unsupported environment does not prevent connections from being established.
                LogHelper.Instance.Warn(string.Format(
                    "Port {0} client {1}: failed to set TCP keep-alive, connection still usable: {2}",
                    LocalPort, RemoteEndPoint, ex.Message));
            }
        }

        /// <summary>
        /// Configures socket send behaviour (added 2026-09-14).
        ///
        /// 1) SendTimeout - when unset, if a client connects but never reads, the server-side Send blocks
        ///    until the send buffer is full; and "manual send" runs on the UI thread, which would freeze the
        ///    whole interface. With a timeout, a SocketException(TimedOut) is thrown at that point and the
        ///    call exits on its own, so the interface is not dragged down.
        ///
        /// 2) NoDelay - disables the Nagle algorithm. Nagle merges consecutive small packets before sending,
        ///    so the receive timing observed while debugging differs from the real send timing; a debugging
        ///    tool needs the real timing, hence this is disabled.
        ///
        /// Note: ReceiveTimeout is deliberately not set here - a long-lived connection may go a long time
        ///       without any traffic, and setting an idle timeout would wrongly kill an "online but idle"
        ///       long-lived connection, which is exactly what must be avoided.
        /// </summary>
        private void ConfigureSocketOptions()
        {
            if (_socket == null)
            {
                return;
            }

            try
            {
                _socket.NoDelay = true;
            }
            catch (Exception ex)
            {
                // Some platforms / protocol stacks do not support NoDelay; traffic is unaffected.
                LogHelper.Instance.Warn(string.Format(
                    "Port {0} client {1}: failed to set NoDelay, connection still usable: {2}",
                    LocalPort, RemoteEndPoint, ex.Message));
            }

            try
            {
                int timeoutMs = ConfigHelper.SendTimeoutMs;
                if (timeoutMs > 0)
                {
                    _socket.SendTimeout = timeoutMs;
                }
            }
            catch (Exception ex)
            {
                // A failed send timeout setting does not affect normal traffic; only a warning is logged.
                LogHelper.Instance.Warn(string.Format(
                    "Port {0} client {1}: failed to set send timeout, connection still usable: {2}",
                    LocalPort, RemoteEndPoint, ex.Message));
            }
        }

        /// <summary>
        /// Receive loop - blocks on receive and raises an event once data arrives.
        /// </summary>
        private void ReceiveLoop()
        {
            byte[] buffer = new byte[_bufferSize];

            while (!_closed)
            {
                int received;

                try
                {
                    received = _socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                }
                catch (SocketException ex)
                {
                    if (!_closed)
                    {
                        LogHelper.Instance.Info(string.Format("Port {0} client {1}: connection interrupted: {2}",
                            LocalPort, RemoteEndPoint, ex.SocketErrorCode));
                    }
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (!_closed)
                    {
                        LogHelper.Instance.Warn(string.Format("Port {0}: exception while receiving: {1}", LocalPort, ex.Message));
                    }
                    break;
                }

                // A return value of 0 means the peer closed the connection normally.
                if (received <= 0)
                {
                    break;
                }

                BytesReceived += received;
                LastActiveTime = DateTime.Now;

                // Copy the data before raising the event, so the buffer is not overwritten by the next receive.
                byte[] copy = new byte[received];
                Buffer.BlockCopy(buffer, 0, copy, 0, received);
                RaiseDataReceived(copy, received);

                // Convention: add a time interval inside the loop to reduce the probability of data merging.
                if (_ioIntervalMs > 0)
                {
                    try { Thread.Sleep(_ioIntervalMs); }
                    catch (Exception) { }
                }
            }

            // Exiting the loop counts as the connection ending.
            if (!_closed)
            {
                LogHelper.Instance.Info(string.Format("Port {0}: client {1} disconnected.", LocalPort, RemoteEndPoint));
                Close();
            }
        }

        /// <summary>
        /// Raises the data received event (exceptions do not escape).
        /// </summary>
        /// <param name="data">Data.</param>
        /// <param name="length">Effective length.</param>
        private void RaiseDataReceived(byte[] data, int length)
        {
            EventHandler<PortDataEventArgs> handler = DataReceived;
            if (handler == null) { return; }

            try
            {
                handler(this, PortDataEventArgs.Create(LocalPort, SessionId, RemoteEndPoint,
                    DataDirection.Received, data, length));
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error("Exception in receive event handler:" + ex.Message, ex);
            }
        }

        /// <summary>
        /// Raises the data sent event (exceptions do not escape).
        /// </summary>
        /// <param name="data">Data.</param>
        /// <param name="length">Effective length.</param>
        private void RaiseDataSent(byte[] data, int length)
        {
            EventHandler<PortDataEventArgs> handler = DataSent;
            if (handler == null) { return; }

            try
            {
                handler(this, PortDataEventArgs.Create(LocalPort, SessionId, RemoteEndPoint,
                    DataDirection.Sent, data, length));
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error("Exception in send event handler:" + ex.Message, ex);
            }
        }

        #endregion
    }
}
