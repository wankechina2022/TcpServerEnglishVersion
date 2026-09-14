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
    /// 端口监听服务 —— 负责单个 TCP 端口的监听、客户端接入与收发调度
    /// 规约要求：设备连接断开不允许二次连接；启动前做端口可用性检查；循环线程必须为后台线程
    /// 2026-09-14 拆分：看门狗与线程收尾部分移至 PortListener.Watchdog.cs
    /// </summary>
    public partial class PortListener : IDisposable
    {
        #region 字段

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
        /// 看门狗唤醒信号（2026-09-14 新增）：Stop 时立即唤醒巡检中的看门狗，避免 Join 空等满一个巡检周期。
        /// 机制详见 PortListener.Watchdog.cs 中 WakeupWatchdog / WatchdogLoop 的说明。
        /// </summary>
        private readonly ManualResetEventSlim _watchdogWakeup = new ManualResetEventSlim(false);

        /// <summary>
        /// 看门狗代数（2026-09-14 新增）
        /// 用途：Stop 后立即 Start 时，旧看门狗线程可能仍在 Sleep 中，
        ///       醒来后会把 _watchdogRunning 重新当作 true 继续巡检，导致两条看门狗并存、
        ///       故障时并发重建监听。用代数号让旧线程识别出自己已过期并退出。
        /// </summary>
        private int _watchdogGeneration;

        private volatile bool _running;
        private volatile bool _watchdogRunning;
        private bool _disposed;

        // 2026-09-14 改为原子更新：多客户端并发收发时，普通 += 会丢统计
        private long _totalBytesReceived;
        private long _totalBytesSent;
        private long _lastActiveTicks;

        /// <summary>单端口允许的最大客户端连接数</summary>
        private const int MAX_CLIENTS_PER_PORT = 64;

        #endregion

        #region 属性

        /// <summary>监听端口号</summary>
        public int Port { get; private set; }

        /// <summary>当前监听地址</summary>
        public string ListenIp
        {
            get { return _listenIp; }
        }

        /// <summary>当前运行状态</summary>
        public PortState State { get; private set; }

        /// <summary>状态补充说明</summary>
        public string StateMessage { get; private set; }

        /// <summary>当前在线客户端数量</summary>
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

        /// <summary>是否正在监听</summary>
        public bool IsListening
        {
            get { return _running && State == PortState.Listening; }
        }

        /// <summary>
        /// 最近一次活动时间（2026-09-14 新增，原子读取）
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

        #region 事件

        /// <summary>状态变化事件</summary>
        public event EventHandler<PortStateChangedEventArgs> StateChanged;

        /// <summary>客户端上下线事件</summary>
        public event EventHandler<ClientChangedEventArgs> ClientChanged;

        /// <summary>收到数据事件</summary>
        public event EventHandler<PortDataEventArgs> DataReceived;

        /// <summary>发出数据事件</summary>
        public event EventHandler<PortDataEventArgs> DataSent;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="port">监听端口号</param>
        /// <param name="listenIp">监听地址，为空时使用默认地址</param>
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
            StateMessage = "已停止";
        }

        #endregion

        #region 对外方法

        /// <summary>
        /// 修改监听地址 —— 仅在未监听状态下允许修改
        /// </summary>
        /// <param name="listenIp">新的监听地址</param>
        /// <returns>修改成功返回 true</returns>
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
        /// 启动监听
        /// </summary>
        /// <param name="message">输出提示信息</param>
        /// <returns>启动成功返回 true</returns>
        public bool Start(out string message)
        {
            message = string.Empty;

            if (_disposed)
            {
                message = "对象已释放";
                return false;
            }

            // 规约要求：网络设备不允许二次连接
            if (_running)
            {
                message = "该端口已在监听中";
                return false;
            }

            if (!ValidationHelper.IsValidPort(Port))
            {
                message = "端口号非法";
                SetState(PortState.Faulted, message);
                return false;
            }

            if (!ValidationHelper.IsValidIpAddress(_listenIp))
            {
                message = "监听地址非法";
                SetState(PortState.Faulted, message);
                return false;
            }

            // 启动前先做端口占用检查（规约：写入前加网络连通性测试）
            // 2026-09-14 修改：按实际要监听的地址探测，避免其它程序只绑某块网卡时误报"已被占用"
            if (!NetHelper.IsPortAvailable(_listenIp, Port))
            {
                message = "端口已被其他程序占用";
                SetState(PortState.Faulted, message);
                LogHelper.Instance.Warn(string.Format("端口 {0} 启动失败：{1}", Port, message));
                return false;
            }

            SetState(PortState.Starting, "正在启动监听");

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

                // 2026-09-14 新增：启动监听线程看门狗
                StartWatchdog();

                SetState(PortState.Listening, "监听已启动");
                LogHelper.Instance.Info(string.Format("端口 {0} 已在 {1} 上启动监听。", Port, _listenIp));
                return true;
            }
            catch (SocketException ex)
            {
                message = "启动监听失败：" + ex.Message;
                SetState(PortState.Faulted, message);
                LogHelper.Instance.Error(string.Format("端口 {0} 启动监听异常：{1}", Port, ex.Message), ex);
                CleanupListener();
                return false;
            }
            catch (Exception ex)
            {
                message = "启动监听异常：" + ex.Message;
                SetState(PortState.Faulted, message);
                LogHelper.Instance.Error(string.Format("端口 {0} 启动监听未知异常：{1}", Port, ex.Message), ex);
                CleanupListener();
                return false;
            }
        }

        /// <summary>
        /// 停止监听 —— 会关闭该端口下所有客户端连接（幂等）
        /// </summary>
        public void Stop()
        {
            if (!_running && State == PortState.Stopped)
            {
                return;
            }

            // 2026-09-14 修改：先记录停止前的状态与说明。
            // 原实现无条件把状态置为 Stopped，会把 Faulted 的故障原因（如"端口已被占用"）冲掉，
            // 界面上就看不到真实失败原因了；此处保留故障态。
            PortState previousState = State;
            string previousMessage = StateMessage;

            SetState(PortState.Stopping, "正在停止监听");

            // 2026-09-14 新增：先停看门狗，避免停止过程中被自动重启
            // 同时递增代数号，让旧看门狗线程醒来后据此判断自己已过期并退出
            Interlocked.Increment(ref _watchdogGeneration);
            _watchdogRunning = false;

            // 2026-09-14 修改：立即唤醒巡检中的看门狗，否则下面的 JoinThread 必须空等满超时（实测每个端口 1 秒）
            WakeupWatchdog();

            _running = false;

            // 先断开全部客户端
            CloseAllClients();

            // 再关闭监听器，使阻塞中的 Accept 立即返回
            CleanupListener();

            // 等待接收线程退出，避免资源悬挂
            JoinThread(_acceptThread, 2000);
            _acceptThread = null;

            JoinThread(_watchdogThread, 1000);
            _watchdogThread = null;

            if (previousState == PortState.Faulted)
            {
                string message = string.IsNullOrWhiteSpace(previousMessage)
                    ? "已停止（停止前处于故障状态）"
                    : string.Format("{0}（已停止）", previousMessage);

                SetState(PortState.Faulted, message);
            }
            else
            {
                SetState(PortState.Stopped, "已停止");
            }

            LogHelper.Instance.Info(string.Format("端口 {0} 已停止监听。", Port));
        }

        /// <summary>
        /// 向指定客户端发送数据
        /// </summary>
        /// <param name="sessionId">会话标识</param>
        /// <param name="data">待发送数据</param>
        /// <param name="error">失败原因</param>
        /// <returns>发送成功返回 true</returns>
        public bool SendToClient(string sessionId, byte[] data, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(sessionId))
            {
                error = "未指定客户端";
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
                error = "客户端已断开";
                return false;
            }

            return session.Send(data, out error);
        }

        /// <summary>
        /// 向该端口所有在线客户端广播数据
        /// </summary>
        /// <param name="data">待发送数据</param>
        /// <param name="error">失败原因（全部失败时填充）</param>
        /// <returns>成功发送的客户端数量</returns>
        public int SendToAllClients(byte[] data, out string error)
        {
            error = string.Empty;
            int successCount = 0;

            List<ClientSession> snapshot = GetClientSnapshot();

            if (snapshot.Count == 0)
            {
                error = "当前没有客户端连接";
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
        /// 获取当前在线客户端信息列表（快照）
        /// </summary>
        /// <returns>客户端信息集合，永不为 null</returns>
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
        /// 获取运行时状态信息（供界面表格绑定）
        /// </summary>
        /// <param name="enabled">配置中的启用状态</param>
        /// <param name="remark">备注名称</param>
        /// <returns>运行时状态对象，永不为 null</returns>
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
        /// 释放资源
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
                LogHelper.Instance.Warn(string.Format("端口 {0} 释放时停止监听异常：{1}", Port, ex.Message));
            }

            _disposed = true;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 接受客户端连接循环（后台线程）
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

                    // 2026-09-14 修改：原为 break，会导致监听线程在异常后静默退出——
                    // 界面仍显示"监听中"，但之后所有新连接都接不进来，只能重启程序。
                    // 改为跳过本次异常继续 Accept，保证短连接"断开后下次还能连"。
                    LogHelper.Instance.Warn(string.Format("端口 {0} 接受连接异常：{1}", Port, ex.Message));
                    SleepAcceptBackoff();
                    continue;
                }
                catch (ObjectDisposedException)
                {
                    // 监听器已被主动关闭，属正常退出
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

                    // 2026-09-14 修改：同 SocketException，未知异常同样不应终止整个监听循环
                    LogHelper.Instance.Error(string.Format("端口 {0} 接受连接未知异常：{1}", Port, ex.Message), ex);
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
        /// Accept 异常后的短暂退避（2026-09-14 新增）
        /// 用途：异常持续抛出时（如系统瞬时句柄不足），避免循环空转占满 CPU
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
        /// 处理新接入的客户端
        /// </summary>
        /// <param name="socket">已连接的套接字</param>
        private void HandleNewClient(Socket socket)
        {
            string sessionId = string.Empty;

            try
            {
                if (ClientCount >= _maxClients)
                {
                    LogHelper.Instance.Warn(string.Format(
                        "端口 {0} 客户端数量已达上限 {1}，拒绝新连接。", Port, _maxClients));

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

                    // 2026-09-14 新增：启动接收线程失败时原先只从字典移除，
                    // 未释放套接字，会造成句柄泄漏；此处补上释放
                    try { session.Dispose(); }
                    catch (Exception) { }

                    return;
                }

                RaiseClientChanged(session.ToClientInfo(), true);
                LogHelper.Instance.Info(string.Format("端口 {0} 新客户端接入：{1}", Port, session.RemoteEndPoint));
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error(string.Format("端口 {0} 处理新客户端异常：{1}", Port, ex.Message), ex);

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
        /// 原子更新"最后活动时间"（2026-09-14 新增）
        /// 说明：DateTime 不是原子类型，直接赋值在多客户端并发时可能读到中间态，
        ///       故以 Ticks 存储并用 CAS 循环只向前推进。
        /// </summary>
        /// <param name="time">活动时间</param>
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
        /// 设置状态并触发事件
        /// </summary>
        /// <param name="state">新状态</param>
        /// <param name="message">状态说明</param>
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
                LogHelper.Instance.Error("状态变更事件处理异常：" + ex.Message, ex);
            }
        }

        /// <summary>
        /// 触发客户端上下线事件
        /// </summary>
        /// <param name="client">客户端信息</param>
        /// <param name="isConnected">是否为上线</param>
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
                LogHelper.Instance.Error("客户端变更事件处理异常：" + ex.Message, ex);
            }
        }

        #endregion
    }
}
