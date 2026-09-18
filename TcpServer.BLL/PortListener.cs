using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using TcpServer.Common;
using TcpServer.Common.Helpers;
using TcpServer.Model;

namespace TcpServer.BLL
{
    /// <summary>
    /// Port listening service - handles listening on a single TCP port, client admission and
    /// send / receive scheduling.
    /// Convention: a device that has disconnected is not allowed to connect a second time; run a port
    ///             availability check before starting; loop threads must be background threads.
    /// 2026-09-14 split: the watchdog and thread-finalization parts moved to PortListener.Watchdog.cs.
    /// </summary>
    public partial class PortListener : IDisposable
    {
        #region Fields

        private readonly object _clientsLock = new object();
        private readonly Dictionary<string, ClientSession> _clients = new Dictionary<string, ClientSession>();
        private readonly int _bufferSize;
        private readonly int _ioIntervalMs;
        private readonly int _maxClients;

        private TcpListener _listener;
        private Thread _acceptThread;
        private Thread _watchdogThread;
        private string _listenIp;
        private int _sessionSeed;

        /// <summary>
        /// Watchdog wake-up signal (added 2026-09-14): wakes an inspecting watchdog immediately on Stop,
        /// so Join does not sit idle for a whole inspection period.
        /// See the notes on WakeupWatchdog / WatchdogLoop in PortListener.Watchdog.cs for the mechanism.
        /// </summary>
        private readonly ManualResetEventSlim _watchdogWakeup = new ManualResetEventSlim(false);

        /// <summary>
        /// Watchdog generation counter (added 2026-09-14).
        /// Purpose: when Stop is immediately followed by Start, the old watchdog thread may still be
        ///          sleeping; on waking it would treat _watchdogRunning as true again and keep inspecting,
        ///          leaving two watchdogs running and rebuilding the listener concurrently on failure.
        ///          The generation number lets an old thread recognise that it is superseded and exit.
        /// </summary>
        private int _watchdogGeneration;

        private volatile bool _running;
        private volatile bool _watchdogRunning;
        private bool _disposed;

        // 2026-09-14 switched to atomic updates: with concurrent multi-client traffic, a plain += loses counts.
        private long _totalBytesReceived;
        private long _totalBytesSent;
        private long _lastActiveTicks;

        /// <summary>Maximum number of client connections allowed on one port.</summary>
        private const int MAX_CLIENTS_PER_PORT = 64;

        #endregion

        #region Properties

        /// <summary>Listening port number.</summary>
        public int Port { get; private set; }

        /// <summary>Current listening address.</summary>
        public string ListenIp
        {
            get { return _listenIp; }
        }

        /// <summary>Current runtime state.</summary>
        public PortState State { get; private set; }

        /// <summary>Supplementary state description.</summary>
        public string StateMessage { get; private set; }

        /// <summary>Number of clients currently online.</summary>
        public int ClientCount
        {
            get
            {
                lock (_clientsLock)
                {
                    return _clients.Count;
                }
            }
        }

        /// <summary>Whether the port is currently listening.</summary>
        public bool IsListening
        {
            get { return _running && State == PortState.Listening; }
        }

        /// <summary>
        /// Most recent activity time (added 2026-09-14, read atomically).
        /// </summary>
        private DateTime LastActiveTime
        {
            get
            {
                long ticks = Interlocked.Read(ref _lastActiveTicks);
                return ticks <= 0L ? DateTime.MinValue : new DateTime(ticks);
            }
        }

        #endregion

        #region Events

        /// <summary>State change event.</summary>
        public event EventHandler<PortStateChangedEventArgs> StateChanged;

        /// <summary>Client online / offline event.</summary>
        public event EventHandler<ClientChangedEventArgs> ClientChanged;

        /// <summary>Data received event.</summary>
        public event EventHandler<PortDataEventArgs> DataReceived;

        /// <summary>Data sent event.</summary>
        public event EventHandler<PortDataEventArgs> DataSent;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="port">Listening port number.</param>
        /// <param name="listenIp">Listening address; uses the default address when empty.</param>
        public PortListener(int port, string listenIp)
        {
            Port = port;
            _listenIp = string.IsNullOrWhiteSpace(listenIp) ? AppConstants.DEFAULT_LISTEN_IP : listenIp.Trim();
            _bufferSize = ConfigHelper.ReceiveBufferSize;
            _ioIntervalMs = ConfigHelper.IoMinIntervalMs;
            _maxClients = MAX_CLIENTS_PER_PORT;
            _sessionSeed = 0;
            _running = false;
            _watchdogRunning = false;
            _disposed = false;
            _totalBytesReceived = 0L;
            _totalBytesSent = 0L;
            _lastActiveTicks = 0L;
            State = PortState.Stopped;
            StateMessage = "Stopped";
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Changes the listening address - only allowed while not listening.
        /// </summary>
        /// <param name="listenIp">New listening address.</param>
        /// <returns>true when the change succeeded.</returns>
        public bool ChangeListenIp(string listenIp)
        {
            if (_running)
            {
                return false;
            }

            if (!ValidationHelper.IsValidIpAddress(listenIp))
            {
                return false;
            }

            _listenIp = listenIp.Trim();
            return true;
        }

        /// <summary>
        /// Starts listening.
        /// </summary>
        /// <param name="message">Output message.</param>
        /// <returns>true when the start succeeded.</returns>
        public bool Start(out string message)
        {
            message = string.Empty;

            if (_disposed)
            {
                message = "Object disposed";
                return false;
            }

            // Convention: a network device is not allowed to connect a second time.
            if (_running)
            {
                message = "This port is already listening";
                return false;
            }

            if (!ValidationHelper.IsValidPort(Port))
            {
                message = "Invalid port number";
                SetState(PortState.Faulted, message);
                return false;
            }

            if (!ValidationHelper.IsValidIpAddress(_listenIp))
            {
                message = "Invalid listen address";
                SetState(PortState.Faulted, message);
                return false;
            }

            // Check port occupancy before starting (convention: run a network connectivity test before binding).
            // 2026-09-14 change: probe against the address actually being listened on, so that another program
            // having bound only one NIC does not cause a false "already in use".
            if (!NetHelper.IsPortAvailable(_listenIp, Port))
            {
                message = "Port already in use by another program";
                SetState(PortState.Faulted, message);
                LogHelper.Instance.Warn(string.Format("Port {0} failed to start: {1}", Port, message));
                return false;
            }

            SetState(PortState.Starting, "Starting listener");

            try
            {
                IPAddress address = IPAddress.Parse(_listenIp);
                _listener = new TcpListener(address, Port);
                _listener.Start();

                _running = true;

                _acceptThread = new Thread(AcceptLoop);
                _acceptThread.IsBackground = true;
                _acceptThread.Name = string.Format("TcpAccept_{0}", Port);
                _acceptThread.Start();

                // Added 2026-09-14: start the listener-thread watchdog.
                StartWatchdog();

                SetState(PortState.Listening, "Listening");
                LogHelper.Instance.Info(string.Format("Port {0} is now listening on {1}.", Port, _listenIp));
                return true;
            }
            catch (SocketException ex)
            {
                message = "Failed to start listening:" + ex.Message;
                SetState(PortState.Faulted, message);
                LogHelper.Instance.Error(string.Format("Port {0}: exception while starting listener: {1}", Port, ex.Message), ex);
                CleanupListener();
                return false;
            }
            catch (Exception ex)
            {
                message = "Exception while starting listener:" + ex.Message;
                SetState(PortState.Faulted, message);
                LogHelper.Instance.Error(string.Format("Port {0}: unknown exception while starting listener: {1}", Port, ex.Message), ex);
                CleanupListener();
                return false;
            }
        }

        /// <summary>
        /// Stops listening - closes every client connection under this port (idempotent).
        /// </summary>
        public void Stop()
        {
            if (!_running && State == PortState.Stopped)
            {
                return;
            }

            // 2026-09-14 change: capture the state and message from before the stop.
            // The original implementation unconditionally set the state to Stopped, which wiped the reason
            // for a Faulted condition (such as "port already in use"), hiding the real failure cause in the UI;
            // the fault state is preserved here.
            PortState previousState = State;
            string previousMessage = StateMessage;

            SetState(PortState.Stopping, "Stopping listener");

            // Added 2026-09-14: stop the watchdog first so it cannot auto-restart during the stop.
            // The generation counter is also incremented so an old watchdog thread sees on waking that it
            // is superseded and exits.
            Interlocked.Increment(ref _watchdogGeneration);
            _watchdogRunning = false;

            // 2026-09-14 change: wake the inspecting watchdog immediately; otherwise the JoinThread call
            // below has to wait out the full timeout (measured as 1 second per port).
            WakeupWatchdog();

            _running = false;

            // Disconnect all clients first.
            CloseAllClients();

            // Then close the listener so a blocked Accept returns immediately.
            CleanupListener();

            // Wait for the accept thread to exit, avoiding lingering resources.
            JoinThread(_acceptThread, 2000);
            _acceptThread = null;

            JoinThread(_watchdogThread, 1000);
            _watchdogThread = null;

            if (previousState == PortState.Faulted)
            {
                string message = string.IsNullOrWhiteSpace(previousMessage)
                    ? "Stopped (was in fault state before stopping)"
                    : string.Format("{0} (stopped)", previousMessage);

                SetState(PortState.Faulted, message);
            }
            else
            {
                SetState(PortState.Stopped, "Stopped");
            }

            LogHelper.Instance.Info(string.Format("Port {0} stopped listening.", Port));
        }

        /// <summary>
        /// Sends data to the specified client.
        /// </summary>
        /// <param name="sessionId">Session identifier.</param>
        /// <param name="data">Data to send.</param>
        /// <param name="error">Failure reason.</param>
        /// <returns>true when the send succeeded.</returns>
        public bool SendToClient(string sessionId, byte[] data, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(sessionId))
            {
                error = "No client specified";
                return false;
            }

            ClientSession session;
            lock (_clientsLock)
            {
                if (!_clients.TryGetValue(sessionId, out session))
                {
                    session = null;
                }
            }

            if (session == null)
            {
                error = "Client disconnected";
                return false;
            }

            return session.Send(data, out error);
        }

        /// <summary>
        /// Broadcasts data to every client online on this port.
        /// </summary>
        /// <param name="data">Data to send.</param>
        /// <param name="error">Failure reason (filled when every send failed).</param>
        /// <returns>Number of clients the data was sent to successfully.</returns>
        public int SendToAllClients(byte[] data, out string error)
        {
            error = string.Empty;
            int successCount = 0;

            List<ClientSession> snapshot = GetClientSnapshot();

            if (snapshot.Count == 0)
            {
                error = "No client connected";
                return 0;
            }

            foreach (ClientSession session in snapshot)
            {
                if (session == null) { continue; }

                string childError;
                if (session.Send(data, out childError))
                {
                    successCount++;
                }
                else
                {
                    error = childError;
                }
            }

            return successCount;
        }

        /// <summary>
        /// Gets a snapshot of the currently online client information.
        /// </summary>
        /// <returns>Client information collection, never null.</returns>
        public List<ClientInfo> GetClientList()
        {
            List<ClientInfo> list = new List<ClientInfo>();
            List<ClientSession> snapshot = GetClientSnapshot();

            foreach (ClientSession session in snapshot)
            {
                if (session == null) { continue; }
                list.Add(session.ToClientInfo());
            }

            return list;
        }

        /// <summary>
        /// Gets runtime state information (bound to the main grid).
        /// </summary>
        /// <param name="enabled">Enabled state from the configuration.</param>
        /// <param name="remark">Remark name.</param>
        /// <returns>Runtime state object, never null.</returns>
        public PortRuntimeInfo GetRuntimeInfo(bool enabled, string remark)
        {
            PortRuntimeInfo info = new PortRuntimeInfo();
            info.Port = Port;
            info.Enabled = enabled;
            info.Remark = remark ?? string.Empty;
            info.State = State;
            info.Message = StateMessage ?? string.Empty;
            info.ClientCount = ClientCount;
            info.BytesReceived = Interlocked.Read(ref _totalBytesReceived);
            info.BytesSent = Interlocked.Read(ref _totalBytesSent);
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

            try
            {
                Stop();
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Warn(string.Format("Port {0}: exception while stopping listener on dispose: {1}", Port, ex.Message));
            }

            _disposed = true;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Accept-client loop (background thread).
        /// </summary>
        private void AcceptLoop()
        {
            while (_running)
            {
                Socket socket = null;

                try
                {
                    if (_listener == null)
                    {
                        break;
                    }

                    socket = _listener.AcceptSocket();
                }
                catch (SocketException ex)
                {
                    if (!_running)
                    {
                        break;
                    }

                    // 2026-09-14 change: this used to `break`, which made the listener thread exit silently
                    // after an exception - the UI still showed "Listening" but no new connection could ever
                    // get in again, leaving a program restart as the only fix.
                    // It now skips the failed iteration and continues accepting, so a short-lived connection
                    // can still connect again after disconnecting.
                    LogHelper.Instance.Warn(string.Format("Port {0}: exception while accepting connection: {1}", Port, ex.Message));
                    SleepAcceptBackoff();
                    continue;
                }
                catch (ObjectDisposedException)
                {
                    // The listener was closed deliberately; this is a normal exit.
                    break;
                }
                catch (InvalidOperationException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (!_running)
                    {
                        break;
                    }

                    // 2026-09-14 change: as with SocketException, an unknown exception must not terminate
                    // the whole accept loop either.
                    LogHelper.Instance.Error(string.Format("Port {0}: unknown exception while accepting connection: {1}", Port, ex.Message), ex);
                    SleepAcceptBackoff();
                    continue;
                }

                if (socket == null)
                {
                    continue;
                }

                HandleNewClient(socket);
            }
        }

        /// <summary>
        /// Brief back-off after an Accept exception (added 2026-09-14).
        /// Purpose: when exceptions keep being thrown (for example a momentary system-wide handle shortage),
        ///          prevent a tight busy loop from saturating the CPU.
        /// </summary>
        private void SleepAcceptBackoff()
        {
            try
            {
                Thread.Sleep(AppConstants.ACCEPT_ERROR_BACKOFF_MS);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Handles a newly accepted client.
        /// </summary>
        /// <param name="socket">The connected socket.</param>
        private void HandleNewClient(Socket socket)
        {
            string sessionId = string.Empty;

            try
            {
                if (ClientCount >= _maxClients)
                {
                    LogHelper.Instance.Warn(string.Format(
                        "Port {0}: client limit {1} reached, new connection rejected.", Port, _maxClients));

                    try { socket.Close(); }
                    catch (Exception) { }
                    return;
                }

                int seed = Interlocked.Increment(ref _sessionSeed);
                sessionId = string.Format("{0}-{1}", Port, seed);

                ClientSession session = new ClientSession(socket, Port, sessionId, _bufferSize, _ioIntervalMs);
                session.DataReceived += OnSessionDataReceived;
                session.DataSent += OnSessionDataSent;
                session.SessionClosed += OnSessionClosed;

                lock (_clientsLock)
                {
                    _clients[sessionId] = session;
                }

                if (!session.StartReceive())
                {
                    RemoveClient(sessionId);

                    // Added 2026-09-14: when starting the receive thread failed, the original code only
                    // removed the session from the dictionary without releasing the socket, leaking handles;
                    // the release is added here.
                    try { session.Dispose(); }
                    catch (Exception) { }

                    return;
                }

                RaiseClientChanged(session.ToClientInfo(), true);
                LogHelper.Instance.Info(string.Format("Port {0}: new client connected: {1}", Port, session.RemoteEndPoint));
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error(string.Format("Port {0}: exception while handling new client: {1}", Port, ex.Message), ex);

                try
                {
                    if (socket != null) { socket.Close(); }
                }
                catch (Exception) { }

                if (!string.IsNullOrWhiteSpace(sessionId))
                {
                    RemoveClient(sessionId);
                }
            }
        }

        /// <summary>
        /// Atomically updates the "last active time" (added 2026-09-14).
        /// Note: DateTime is not atomic, so a direct assignment could be read mid-write under concurrent
        ///       multi-client traffic; it is therefore stored as Ticks and advanced forward only, using a
        ///       CAS loop.
        /// </summary>
        /// <param name="time">Activity time.</param>
        private void UpdateLastActiveTime(DateTime time)
        {
            long ticks = time.Ticks;

            long current = Interlocked.Read(ref _lastActiveTicks);
            while (ticks > current)
            {
                long prior = Interlocked.CompareExchange(ref _lastActiveTicks, ticks, current);
                if (prior == current)
                {
                    break;
                }

                current = prior;
            }
        }

        /// <summary>
        /// Sets the state and raises the event.
        /// </summary>
        /// <param name="state">New state.</param>
        /// <param name="message">State description.</param>
        private void SetState(PortState state, string message)
        {
            State = state;
            StateMessage = string.IsNullOrWhiteSpace(message) ? string.Empty : message;

            EventHandler<PortStateChangedEventArgs> handler = StateChanged;
            if (handler == null) { return; }

            try
            {
                PortStateChangedEventArgs args = new PortStateChangedEventArgs();
                args.Port = Port;
                args.State = state;
                args.Message = StateMessage;
                args.EventTime = DateTime.Now;
                handler(this, args);
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error("Exception in state changed event handler:" + ex.Message, ex);
            }
        }

        /// <summary>
        /// Raises the client online / offline event.
        /// </summary>
        /// <param name="client">Client information.</param>
        /// <param name="isConnected">Whether the client came online.</param>
        private void RaiseClientChanged(ClientInfo client, bool isConnected)
        {
            EventHandler<ClientChangedEventArgs> handler = ClientChanged;
            if (handler == null) { return; }

            try
            {
                handler(this, ClientChangedEventArgs.Create(Port, client, isConnected, ClientCount));
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error("Exception in client changed event handler:" + ex.Message, ex);
            }
        }

        #endregion
    }
}
