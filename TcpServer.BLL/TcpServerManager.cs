using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using TcpServer.Common;
using TcpServer.Common.Helpers;
using TcpServer.Model;

namespace TcpServer.BLL
{
    /// <summary>
    /// Multi-port listening manager - schedules a group of port listening services and drives auto-reply.
    /// </summary>
    public class TcpServerManager : IDisposable
    {
        #region Fields

        private readonly object _lockObj = new object();
        private readonly Dictionary<int, PortListener> _listeners = new Dictionary<int, PortListener>();
        private readonly AutoReplyEngine _replyEngine;
        private List<PortConfig> _portConfigs;
        private string _listenIp;
        private bool _disposed;

        #endregion

        #region Properties

        /// <summary>Number of ports currently listening.</summary>
        public int ListeningCount
        {
            get
            {
                int count = 0;
                List<PortListener> snapshot = GetListenerSnapshot();

                foreach (PortListener listener in snapshot)
                {
                    if (listener != null && listener.IsListening) { count++; }
                }

                return count;
            }
        }

        /// <summary>Copy of the currently configured port list.</summary>
        public List<PortConfig> PortConfigs
        {
            get
            {
                lock (_lockObj)
                {
                    return new List<PortConfig>(_portConfigs);
                }
            }
        }

        /// <summary>Current listening address.</summary>
        public string ListenIp
        {
            get { return _listenIp; }
        }

        /// <summary>Auto-reply engine instance.</summary>
        public AutoReplyEngine ReplyEngine
        {
            get { return _replyEngine; }
        }

        #endregion

        #region Events

        /// <summary>Port state change event.</summary>
        public event EventHandler<PortStateChangedEventArgs> PortStateChanged;

        /// <summary>Client online / offline event.</summary>
        public event EventHandler<ClientChangedEventArgs> ClientChanged;

        /// <summary>Data received event.</summary>
        public event EventHandler<PortDataEventArgs> DataReceived;

        /// <summary>Data sent event.</summary>
        public event EventHandler<PortDataEventArgs> DataSent;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor - uses the default listening address and the default encoding.
        /// </summary>
        public TcpServerManager()
            : this(ConfigHelper.DefaultListenIp, null)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="listenIp">Listening address; uses the default address when empty.</param>
        /// <param name="encoding">Text encoding; uses GBK when null.</param>
        public TcpServerManager(string listenIp, Encoding encoding)
        {
            _listenIp = string.IsNullOrWhiteSpace(listenIp) ? AppConstants.DEFAULT_LISTEN_IP : listenIp.Trim();
            _portConfigs = new List<PortConfig>();
            _replyEngine = new AutoReplyEngine(encoding);
            _disposed = false;
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Sets the port list - rebuilds listener instances (does not affect listeners already started
        /// until Start is called again).
        /// </summary>
        /// <param name="configs">Port configuration list; may be null.</param>
        public void SetPortConfigs(List<PortConfig> configs)
        {
            lock (_lockObj)
            {
                _portConfigs = configs ?? new List<PortConfig>();

                // Remove listeners that are no longer in the list.
                List<int> configPorts = new List<int>();
                foreach (PortConfig cfg in _portConfigs)
                {
                    if (cfg != null) { configPorts.Add(cfg.Port); }
                }

                List<int> removeKeys = new List<int>();
                foreach (KeyValuePair<int, PortListener> pair in _listeners)
                {
                    if (!configPorts.Contains(pair.Key))
                    {
                        removeKeys.Add(pair.Key);
                    }
                }

                foreach (int port in removeKeys)
                {
                    PortListener listener = _listeners[port];
                    _listeners.Remove(port);

                    if (listener != null)
                    {
                        try { listener.Dispose(); }
                        catch (Exception) { }
                    }
                }
            }
        }

        /// <summary>
        /// Updates the auto-reply rules.
        /// </summary>
        /// <param name="rules">Rule list; may be null.</param>
        public void SetRules(List<AutoReplyRule> rules)
        {
            _replyEngine.UpdateRules(rules);
        }

        /// <summary>
        /// Sets the listening address - only effective while no port is listening.
        /// </summary>
        /// <param name="listenIp">Listening address.</param>
        /// <returns>true when the setting succeeded.</returns>
        public bool SetListenIp(string listenIp)
        {
            if (!ValidationHelper.IsValidIpAddress(listenIp))
            {
                return false;
            }

            lock (_lockObj)
            {
                foreach (KeyValuePair<int, PortListener> pair in _listeners)
                {
                    if (pair.Value != null && pair.Value.IsListening)
                    {
                        // A port is already listening, so the address cannot be changed.
                        return false;
                    }
                }

                _listenIp = listenIp.Trim();

                foreach (KeyValuePair<int, PortListener> pair in _listeners)
                {
                    if (pair.Value != null)
                    {
                        pair.Value.ChangeListenIp(_listenIp);
                    }
                }
            }

            return true;
        }

        #endregion

        #region Start / Stop Control

        /// <summary>
        /// Starts every enabled port.
        /// </summary>
        /// <param name="message">Result summary description.</param>
        /// <returns>true when at least one port started successfully.</returns>
        public bool StartAll(out string message)
        {
            message = string.Empty;

            if (_disposed)
            {
                message = "Object disposed";
                return false;
            }

            List<PortConfig> configs;
            lock (_lockObj)
            {
                configs = new List<PortConfig>(_portConfigs);
            }

            if (configs.Count == 0)
            {
                message = "The port list is empty. Set the start port and count first";
                return false;
            }

            int successCount = 0;
            int skipCount = 0;
            int failCount = 0;
            List<string> failDetails = new List<string>();

            foreach (PortConfig cfg in configs)
            {
                if (cfg == null || !cfg.Enabled)
                {
                    continue;
                }

                PortListener listener = GetOrCreateListener(cfg.Port, cfg);


                if (listener.IsListening)
                {
                    skipCount++;
                    continue;
                }

                string childMessage;
                if (listener.Start(out childMessage))
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                    failDetails.Add(string.Format("{0}({1})", cfg.Port, childMessage));
                }
            }

            message = string.Format("Start finished: {0} succeeded, {1} failed, {2} skipped (already listening).",
                successCount, failCount, skipCount);

            if (failDetails.Count > 0)
            {
                message += " Failed ports: " + string.Join(", ", failDetails.ToArray());
            }

            LogHelper.Instance.Info(message);
            return successCount > 0;
        }

        /// <summary>
        /// Stops every port.
        /// </summary>
        public void StopAll()
        {
            List<PortListener> snapshot = GetListenerSnapshot();

            foreach (PortListener listener in snapshot)
            {
                if (listener == null) { continue; }

                try
                {
                    listener.Stop();
                }
                catch (Exception ex)
                {
                    LogHelper.Instance.Error(string.Format("Exception while stopping port {0}: {1}", listener.Port, ex.Message), ex);
                }
            }
        }

        /// <summary>
        /// Starts a single port.
        /// </summary>
        /// <param name="port">Port number.</param>
        /// <param name="message">Result description.</param>
        /// <returns>true when the start succeeded.</returns>
        public bool StartPort(int port, out string message)
        {
            message = string.Empty;

            PortConfig cfg = FindConfig(port);
            if (cfg == null)
            {
                cfg = new PortConfig(port);
            }

            PortListener listener = GetOrCreateListener(port, cfg);
            return listener.Start(out message);
        }

        /// <summary>
        /// Stops a single port.
        /// </summary>
        /// <param name="port">Port number.</param>
        public void StopPort(int port)
        {
            PortListener listener = FindListener(port);
            if (listener == null) { return; }

            try
            {
                listener.Stop();
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error(string.Format("Exception while stopping port {0}: {1}", port, ex.Message), ex);
            }
        }

        #endregion

        #region Data Transfer

        /// <summary>
        /// Sends data to a specific client on the specified port.
        /// </summary>
        /// <param name="port">Port number.</param>
        /// <param name="sessionId">Session identifier.</param>
        /// <param name="data">Data to send.</param>
        /// <param name="error">Failure reason.</param>
        /// <returns>true when the send succeeded.</returns>
        public bool SendToClient(int port, string sessionId, byte[] data, out string error)
        {
            error = string.Empty;

            PortListener listener = FindListener(port);
            if (listener == null || !listener.IsListening)
            {
                error = "This port is not listening";
                return false;
            }

            return listener.SendToClient(sessionId, data, out error);
        }

        /// <summary>
        /// Broadcasts data to every client online on the specified port.
        /// </summary>
        /// <param name="port">Port number.</param>
        /// <param name="data">Data to send.</param>
        /// <param name="error">Failure reason.</param>
        /// <returns>Number of clients the data was sent to successfully.</returns>
        public int SendToAllClients(int port, byte[] data, out string error)
        {
            error = string.Empty;

            PortListener listener = FindListener(port);
            if (listener == null || !listener.IsListening)
            {
                error = "This port is not listening";
                return 0;
            }

            return listener.SendToAllClients(data, out error);
        }

        #endregion

        #region State Query

        /// <summary>
        /// Gets the runtime state of every port (in configuration order).
        /// </summary>
        /// <returns>State collection, never null.</returns>
        public List<PortRuntimeInfo> GetRuntimeInfos()
        {
            List<PortRuntimeInfo> result = new List<PortRuntimeInfo>();

            List<PortConfig> configs;
            lock (_lockObj)
            {
                configs = new List<PortConfig>(_portConfigs);
            }

            foreach (PortConfig cfg in configs)
            {
                if (cfg == null) { continue; }
                result.Add(GetRuntimeInfo(cfg.Port));
            }

            return result;
        }

        /// <summary>
        /// Gets the runtime state of the specified port.
        /// </summary>
        /// <param name="port">Port number.</param>
        /// <returns>State object, never null.</returns>
        public PortRuntimeInfo GetRuntimeInfo(int port)
        {
            PortConfig cfg = FindConfig(port);
            bool enabled = cfg == null || cfg.Enabled;
            string remark = cfg == null ? string.Empty : cfg.Remark;

            PortListener listener = FindListener(port);
            if (listener == null)
            {
                PortRuntimeInfo empty = new PortRuntimeInfo();
                empty.Port = port;
                empty.Enabled = enabled;
                empty.Remark = remark;
                empty.State = PortState.Stopped;
                empty.Message = "Stopped";
                return empty;
            }

            return listener.GetRuntimeInfo(enabled, remark);
        }

        /// <summary>
        /// Gets the list of clients online on the specified port.
        /// </summary>
        /// <param name="port">Port number.</param>
        /// <returns>Client collection, never null.</returns>
        public List<ClientInfo> GetClientList(int port)
        {
            PortListener listener = FindListener(port);
            if (listener == null)
            {
                return new List<ClientInfo>();
            }

            return listener.GetClientList();
        }

        #endregion

        #region Dispose

        /// <summary>
        /// Releases resources - stops all listeners first
        /// (convention: before exiting, check whether devices are still connected and call close methods).
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                StopAll();
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Warn("Exception while stopping all ports on dispose:" + ex.Message);
            }

            List<PortListener> snapshot = GetListenerSnapshot();

            foreach (PortListener listener in snapshot)
            {
                if (listener == null) { continue; }

                try { listener.Dispose(); }
                catch (Exception) { }
            }

            lock (_lockObj)
            {
                _listeners.Clear();
            }

            _disposed = true;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets or creates the listener for the specified port.
        /// </summary>
        /// <param name="port">Port number.</param>
        /// <param name="cfg">Corresponding configuration (may be null).</param>
        /// <returns>Listener instance, never null.</returns>
        private PortListener GetOrCreateListener(int port, PortConfig cfg)
        {
            lock (_lockObj)
            {
                PortListener listener;
                if (_listeners.TryGetValue(port, out listener) && listener != null)
                {
                    // Synchronize the address over when it changed and the listener is not listening.
                    if (!listener.IsListening)
                    {
                        listener.ChangeListenIp(_listenIp);
                    }

                    return listener;
                }

                listener = new PortListener(port, _listenIp);
                listener.StateChanged += OnListenerStateChanged;
                listener.ClientChanged += OnListenerClientChanged;
                listener.DataReceived += OnListenerDataReceived;
                listener.DataSent += OnListenerDataSent;

                _listeners[port] = listener;
                return listener;
            }
        }

        /// <summary>
        /// Finds the listener for the specified port.
        /// </summary>
        /// <param name="port">Port number.</param>
        /// <returns>Listener; null when it does not exist.</returns>
        private PortListener FindListener(int port)
        {
            lock (_lockObj)
            {
                PortListener listener;
                return _listeners.TryGetValue(port, out listener) ? listener : null;
            }
        }

        /// <summary>
        /// Finds the configuration for the specified port.
        /// </summary>
        /// <param name="port">Port number.</param>
        /// <returns>Configuration; null when it does not exist.</returns>
        private PortConfig FindConfig(int port)
        {
            lock (_lockObj)
            {
                foreach (PortConfig cfg in _portConfigs)
                {
                    if (cfg != null && cfg.Port == port)
                    {
                        return cfg;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets a listener snapshot.
        /// </summary>
        /// <returns>Listener collection, never null.</returns>
        private List<PortListener> GetListenerSnapshot()
        {
            List<PortListener> snapshot = new List<PortListener>();

            lock (_lockObj)
            {
                foreach (KeyValuePair<int, PortListener> pair in _listeners)
                {
                    snapshot.Add(pair.Value);
                }
            }

            return snapshot;
        }

        /// <summary>
        /// Port state changed - forwards the event.
        /// </summary>
        private void OnListenerStateChanged(object sender, PortStateChangedEventArgs e)
        {
            EventHandler<PortStateChangedEventArgs> handler = PortStateChanged;
            if (handler == null || e == null) { return; }

            try { handler(this, e); }
            catch (Exception ex) { LogHelper.Instance.Error("Exception while forwarding port state event:" + ex.Message, ex); }
        }

        /// <summary>
        /// Client online / offline - forwards the event.
        /// </summary>
        private void OnListenerClientChanged(object sender, ClientChangedEventArgs e)
        {
            EventHandler<ClientChangedEventArgs> handler = ClientChanged;
            if (handler == null || e == null) { return; }

            try { handler(this, e); }
            catch (Exception ex) { LogHelper.Instance.Error("Exception while forwarding client event:" + ex.Message, ex); }
        }

        /// <summary>
        /// Data received - forwards the event and triggers auto-reply.
        /// </summary>
        private void OnListenerDataReceived(object sender, PortDataEventArgs e)
        {
            if (e == null) { return; }

            EventHandler<PortDataEventArgs> handler = DataReceived;
            if (handler != null)
            {
                try { handler(this, e); }
                catch (Exception ex) { LogHelper.Instance.Error("Exception while forwarding data event:" + ex.Message, ex); }
            }

            TryAutoReply(e);
        }

        /// <summary>
        /// Data sent - forwards the event.
        /// </summary>
        private void OnListenerDataSent(object sender, PortDataEventArgs e)
        {
            if (e == null) { return; }

            EventHandler<PortDataEventArgs> handler = DataSent;
            if (handler != null)
            {
                try { handler(this, e); }
                catch (Exception ex) { LogHelper.Instance.Error("Exception while forwarding send event:" + ex.Message, ex); }
            }
        }

        /// <summary>
        /// Attempts an auto-reply - on a rule hit, replies asynchronously to avoid blocking the receive thread.
        /// </summary>
        /// <param name="e">Data received event arguments.</param>
        private void TryAutoReply(PortDataEventArgs e)
        {
            if (e == null || e.Direction != DataDirection.Received) { return; }

            AutoReplyRule rule;
            byte[] replyData;

            if (!_replyEngine.TryMatch(e.Port, e.Data, e.Length, out rule, out replyData))
            {
                return;
            }

            if (rule == null || replyData == null || replyData.Length == 0)
            {
                return;
            }

            int port = e.Port;
            string sessionId = e.SessionId;
            int delayMs = rule.DelayMs;
            string ruleName = rule.RuleName;

            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    if (delayMs > 0)
                    {
                        Thread.Sleep(delayMs);
                    }

                    string error;
                    if (SendToClient(port, sessionId, replyData, out error))
                    {
                        LogHelper.Instance.Info(string.Format(
                            "Port {0} auto-replied {2} byte(s) by rule [{1}].", port, ruleName, replyData.Length));
                    }
                    else
                    {
                        LogHelper.Instance.Warn(string.Format(
                            "Port {0}: auto reply failed: {1}", port, error));
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Instance.Error("Exception in auto reply thread:" + ex.Message, ex);
                }
            });
        }

        #endregion
    }
}
