using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using TcpServer.Common.Helpers;
using TcpServer.Model;

namespace TcpServer.BLL
{
    /// <summary>
    /// Port listening service (client session callbacks and resource cleanup part).
    /// 2026-09-14 split: after the watchdog wake-up mechanism was added to the main PortListener body it
    /// again approached the convention red line of "a single class must not exceed 800 lines", so it was
    /// split into this partial file following the project's existing practice (see frmMain.cs /
    /// frmMain.Console.cs / PortListener.Watchdog.cs).
    /// Responsibilities of this file: session event callbacks (receive / send / close), client collection
    /// add / remove and snapshots, listener handle cleanup.
    /// Port start / stop lives in PortListener.cs; the watchdog and thread finalization live in
    /// PortListener.Watchdog.cs.
    /// </summary>
    public partial class PortListener
    {
        #region Private Methods - Session Callbacks and Cleanup

        /// <summary>
        /// Session received data.
        /// </summary>
        private void OnSessionDataReceived(object sender, PortDataEventArgs e)
        {
            if (e == null) { return; }

            // 2026-09-14 change: switched to atomic accumulation and atomic time update, so statistics
            // are not lost under concurrent multi-client traffic.
            Interlocked.Add(ref _totalBytesReceived, e.Length);
            UpdateLastActiveTime(e.EventTime);

            LogHelper.Instance.WritePortLog(Port,
                string.Format("RX [{0}] {1}", e.RemoteEndPoint, HexHelper.ToLogText(e.Data, e.Length)));

            EventHandler<PortDataEventArgs> handler = DataReceived;
            if (handler != null)
            {
                try { handler(this, e); }
                catch (Exception ex) { LogHelper.Instance.Error("Exception in data received event handler:" + ex.Message, ex); }
            }
        }

        /// <summary>
        /// Session sent data.
        /// </summary>
        private void OnSessionDataSent(object sender, PortDataEventArgs e)
        {
            if (e == null) { return; }

            // 2026-09-14 change: same as the receive side, switched to atomic accumulation and atomic time update.
            Interlocked.Add(ref _totalBytesSent, e.Length);
            UpdateLastActiveTime(e.EventTime);

            LogHelper.Instance.WritePortLog(Port,
                string.Format("TX [{0}] {1}", e.RemoteEndPoint, HexHelper.ToLogText(e.Data, e.Length)));

            EventHandler<PortDataEventArgs> handler = DataSent;
            if (handler != null)
            {
                try { handler(this, e); }
                catch (Exception ex) { LogHelper.Instance.Error("Exception in data sent event handler:" + ex.Message, ex); }
            }
        }

        /// <summary>
        /// Session closed.
        /// </summary>
        private void OnSessionClosed(object sender, ClientSession session)
        {
            if (session == null) { return; }

            ClientInfo info = session.ToClientInfo();
            RemoveClient(session.SessionId);

            // Release the session resources.
            try { session.Dispose(); }
            catch (Exception) { }

            RaiseClientChanged(info, false);
        }

        /// <summary>
        /// Removes a session from the dictionary.
        /// </summary>
        /// <param name="sessionId">Session identifier.</param>
        private void RemoveClient(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) { return; }

            lock (_clientsLock)
            {
                if (_clients.ContainsKey(sessionId))
                {
                    _clients.Remove(sessionId);
                }
            }
        }

        /// <summary>
        /// Gets a snapshot of the current clients (avoids calling external methods while holding the lock).
        /// </summary>
        /// <returns>Session collection, never null.</returns>
        private List<ClientSession> GetClientSnapshot()
        {
            List<ClientSession> snapshot = new List<ClientSession>();

            lock (_clientsLock)
            {
                foreach (KeyValuePair<string, ClientSession> pair in _clients)
                {
                    snapshot.Add(pair.Value);
                }
            }

            return snapshot;
        }

        /// <summary>
        /// Closes all client connections.
        /// </summary>
        private void CloseAllClients()
        {
            List<ClientSession> snapshot = GetClientSnapshot();

            foreach (ClientSession session in snapshot)
            {
                if (session == null) { continue; }
                try { session.Close(); }
                catch (Exception) { }
            }

            lock (_clientsLock)
            {
                _clients.Clear();
            }
        }

        /// <summary>
        /// Cleans up the listener resources.
        /// </summary>
        private void CleanupListener()
        {
            if (_listener == null) { return; }

            try { _listener.Stop(); }
            catch (Exception) { }
            finally { _listener = null; }
        }

        #endregion
    }
}
