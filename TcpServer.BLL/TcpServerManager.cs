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
    /// 多端口监听管理器 —— 统一调度一组端口监听服务，并驱动自动应答
    /// </summary>
    public class TcpServerManager : IDisposable
    {
        #region 字段

        private readonly object _lockObj = new object();
        private readonly Dictionary<int, PortListener> _listeners = new Dictionary<int, PortListener>();
        private readonly AutoReplyEngine _replyEngine;
        private List<PortConfig> _portConfigs;
        private string _listenIp;
        private bool _disposed;

        #endregion

        #region 属性

        /// <summary>当前正在监听的端口数量</summary>
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

        /// <summary>当前配置的端口清单副本</summary>
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

        /// <summary>当前监听地址</summary>
        public string ListenIp
        {
            get { return _listenIp; }
        }

        /// <summary>自动应答引擎实例</summary>
        public AutoReplyEngine ReplyEngine
        {
            get { return _replyEngine; }
        }

        #endregion

        #region 事件

        /// <summary>端口状态变化事件</summary>
        public event EventHandler<PortStateChangedEventArgs> PortStateChanged;

        /// <summary>客户端上下线事件</summary>
        public event EventHandler<ClientChangedEventArgs> ClientChanged;

        /// <summary>收到数据事件</summary>
        public event EventHandler<PortDataEventArgs> DataReceived;

        /// <summary>发出数据事件</summary>
        public event EventHandler<PortDataEventArgs> DataSent;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数 —— 使用默认监听地址与默认编码
        /// </summary>
        public TcpServerManager()
            : this(ConfigHelper.DefaultListenIp, null)
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="listenIp">监听地址，为空时使用默认地址</param>
        /// <param name="encoding">文本编码，为 null 时使用 GBK</param>
        public TcpServerManager(string listenIp, Encoding encoding)
        {
            _listenIp = string.IsNullOrWhiteSpace(listenIp) ? AppConstants.DEFAULT_LISTEN_IP : listenIp.Trim();
            _portConfigs = new List<PortConfig>();
            _replyEngine = new AutoReplyEngine(encoding);
            _disposed = false;
        }

        #endregion

        #region 配置相关

        /// <summary>
        /// 设置端口清单 —— 会重建监听器实例（不影响已启动的监听，直到再次 Start）
        /// </summary>
        /// <param name="configs">端口配置列表，可为 null</param>
        public void SetPortConfigs(List<PortConfig> configs)
        {
            lock (_lockObj)
            {
                _portConfigs = configs ?? new List<PortConfig>();

                // 清除已不在清单中的监听器
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
        /// 更新自动应答规则
        /// </summary>
        /// <param name="rules">规则列表，可为 null</param>
        public void SetRules(List<AutoReplyRule> rules)
        {
            _replyEngine.UpdateRules(rules);
        }

        /// <summary>
        /// 设置监听地址 —— 仅在没有任何端口处于监听状态时生效
        /// </summary>
        /// <param name="listenIp">监听地址</param>
        /// <returns>设置成功返回 true</returns>
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
                        // 已有端口在监听，地址不可变更
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

        #region 启停控制

        /// <summary>
        /// 启动全部启用的端口
        /// </summary>
        /// <param name="message">结果汇总描述</param>
        /// <returns>至少成功启动一个端口返回 true</returns>
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
                message += " Failed ports: " + string.Join("、", failDetails.ToArray());
            }

            LogHelper.Instance.Info(message);
            return successCount > 0;
        }

        /// <summary>
        /// 停止全部端口
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
        /// 启动单个端口
        /// </summary>
        /// <param name="port">端口号</param>
        /// <param name="message">结果描述</param>
        /// <returns>启动成功返回 true</returns>
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
        /// 停止单个端口
        /// </summary>
        /// <param name="port">端口号</param>
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

        #region 数据收发

        /// <summary>
        /// 向指定端口的指定客户端发送数据
        /// </summary>
        /// <param name="port">端口号</param>
        /// <param name="sessionId">会话标识</param>
        /// <param name="data">待发送数据</param>
        /// <param name="error">失败原因</param>
        /// <returns>发送成功返回 true</returns>
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
        /// 向指定端口的所有在线客户端广播数据
        /// </summary>
        /// <param name="port">端口号</param>
        /// <param name="data">待发送数据</param>
        /// <param name="error">失败原因</param>
        /// <returns>成功发送的客户端数量</returns>
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

        #region 状态查询

        /// <summary>
        /// 获取全部端口的运行时状态（与配置顺序一致）
        /// </summary>
        /// <returns>状态集合，永不为 null</returns>
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
        /// 获取指定端口的运行时状态
        /// </summary>
        /// <param name="port">端口号</param>
        /// <returns>状态对象，永不为 null</returns>
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
        /// 获取指定端口的在线客户端列表
        /// </summary>
        /// <param name="port">端口号</param>
        /// <returns>客户端集合，永不为 null</returns>
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

        #region 释放

        /// <summary>
        /// 释放资源 —— 会先停止全部监听（规约：退出前检查设备是否仍在连接并调用关闭方法）
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

        #region 私有方法

        /// <summary>
        /// 获取或创建指定端口的监听器
        /// </summary>
        /// <param name="port">端口号</param>
        /// <param name="cfg">对应的配置（可为 null）</param>
        /// <returns>监听器实例，永不为 null</returns>
        private PortListener GetOrCreateListener(int port, PortConfig cfg)
        {
            lock (_lockObj)
            {
                PortListener listener;
                if (_listeners.TryGetValue(port, out listener) && listener != null)
                {
                    // 地址已变更且未在监听时同步过去
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
        /// 查找指定端口的监听器
        /// </summary>
        /// <param name="port">端口号</param>
        /// <returns>监听器；不存在时返回 null</returns>
        private PortListener FindListener(int port)
        {
            lock (_lockObj)
            {
                PortListener listener;
                return _listeners.TryGetValue(port, out listener) ? listener : null;
            }
        }

        /// <summary>
        /// 查找指定端口的配置
        /// </summary>
        /// <param name="port">端口号</param>
        /// <returns>配置；不存在时返回 null</returns>
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
        /// 获取监听器快照
        /// </summary>
        /// <returns>监听器集合，永不为 null</returns>
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
        /// 端口状态变化 —— 转发事件
        /// </summary>
        private void OnListenerStateChanged(object sender, PortStateChangedEventArgs e)
        {
            EventHandler<PortStateChangedEventArgs> handler = PortStateChanged;
            if (handler == null || e == null) { return; }

            try { handler(this, e); }
            catch (Exception ex) { LogHelper.Instance.Error("Exception while forwarding port state event:" + ex.Message, ex); }
        }

        /// <summary>
        /// 客户端上下线 —— 转发事件
        /// </summary>
        private void OnListenerClientChanged(object sender, ClientChangedEventArgs e)
        {
            EventHandler<ClientChangedEventArgs> handler = ClientChanged;
            if (handler == null || e == null) { return; }

            try { handler(this, e); }
            catch (Exception ex) { LogHelper.Instance.Error("Exception while forwarding client event:" + ex.Message, ex); }
        }

        /// <summary>
        /// 收到数据 —— 转发事件并触发自动应答
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
        /// 发出数据 —— 转发事件
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
        /// 尝试自动应答 —— 命中规则时异步回发，避免阻塞接收线程
        /// </summary>
        /// <param name="e">收到的数据事件参数</param>
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
