using System;
using System.Net.Sockets;
using System.Threading;
using TcpServer.Common;
using TcpServer.Common.Helpers;
using TcpServer.Model;

namespace TcpServer.BLL
{
    /// <summary>
    /// 客户端会话 —— 封装单个 TCP 客户端的接收循环与发送逻辑
    /// 规约要求：网络读写加锁防粘连、循环内加间隔、线程设为后台线程
    /// </summary>
    public class ClientSession : IDisposable
    {
        #region 字段

        private readonly Socket _socket;
        private readonly object _sendLock = new object();
        private readonly int _bufferSize;
        private readonly int _ioIntervalMs;
        private Thread _receiveThread;
        private volatile bool _closed;
        private bool _disposed;

        #endregion

        #region 属性

        /// <summary>会话唯一标识</summary>
        public string SessionId { get; private set; }

        /// <summary>所属本地监听端口</summary>
        public int LocalPort { get; private set; }

        /// <summary>客户端远端地址（IP:Port）</summary>
        public string RemoteEndPoint { get; private set; }

        /// <summary>连接建立时间</summary>
        public DateTime ConnectedTime { get; private set; }

        /// <summary>累计接收字节数</summary>
        public long BytesReceived { get; private set; }

        /// <summary>累计发送字节数</summary>
        public long BytesSent { get; private set; }

        /// <summary>最后活动时间</summary>
        public DateTime LastActiveTime { get; private set; }

        /// <summary>会话是否仍然有效</summary>
        public bool IsConnected
        {
            get { return !_closed && !_disposed && _socket != null; }
        }

        #endregion

        #region 事件

        /// <summary>收到数据事件</summary>
        public event EventHandler<PortDataEventArgs> DataReceived;

        /// <summary>发出数据事件</summary>
        public event EventHandler<PortDataEventArgs> DataSent;

        /// <summary>会话结束事件</summary>
        public event EventHandler<ClientSession> SessionClosed;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="socket">已连接的客户端套接字，不能为 null</param>
        /// <param name="localPort">所属本地监听端口</param>
        /// <param name="sessionId">会话标识</param>
        /// <param name="bufferSize">接收缓冲区大小</param>
        /// <param name="ioIntervalMs">接收循环间隔毫秒数</param>
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
                LogHelper.Instance.Warn(string.Format("端口 {0} 获取客户端地址失败：{1}", LocalPort, ex.Message));
                RemoteEndPoint = "未知";
            }

            // 2026-09-14 新增：启用 TCP 保活探测，用于自动清理客户端断电/拔线造成的僵尸会话
            ConfigureKeepAlive();

            // 2026-09-14 新增：发送超时 + 禁用 Nagle，详见方法注释
            ConfigureSocketOptions();
        }

        #endregion

        #region 对外方法

        /// <summary>
        /// 启动接收循环（后台线程）
        /// </summary>
        /// <returns>启动成功返回 true</returns>
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
                LogHelper.Instance.Error(string.Format("端口 {0} 启动接收线程失败：{1}", LocalPort, ex.Message), ex);
                return false;
            }
        }

        /// <summary>
        /// 向该客户端发送数据（加锁防并发粘连）
        /// </summary>
        /// <param name="data">待发送字节数组</param>
        /// <param name="error">失败原因</param>
        /// <returns>发送成功返回 true</returns>
        public bool Send(byte[] data, out string error)
        {
            error = string.Empty;

            if (data == null || data.Length == 0)
            {
                error = "发送内容为空";
                return false;
            }

            if (!IsConnected)
            {
                error = "连接已断开";
                return false;
            }

            // PV 操作：同一会话的发送必须串行，防止数据包粘连
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
                            error = "发送被中断";
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
                    // 2026-09-14 新增：区分"发送超时"与普通发送失败，便于现场快速判断原因
                    if (ex.SocketErrorCode == SocketError.TimedOut)
                    {
                        error = string.Format("发送超时（{0} 毫秒内未发完，客户端可能连上后未读取数据）",
                            _socket.SendTimeout);
                        LogHelper.Instance.Warn(string.Format("端口 {0} 向 {1} 发送超时，已断开该会话：{2}",
                            LocalPort, RemoteEndPoint, ex.SocketErrorCode));
                        Close();
                        return false;
                    }

                    error = "发送失败：" + ex.Message;
                    LogHelper.Instance.Warn(string.Format("端口 {0} 向 {1} 发送失败：{2}",
                        LocalPort, RemoteEndPoint, ex.Message));
                    Close();
                    return false;
                }
                catch (Exception ex)
                {
                    error = "发送异常：" + ex.Message;
                    LogHelper.Instance.Error(string.Format("端口 {0} 发送数据异常：{1}", LocalPort, ex.Message), ex);
                    Close();
                    return false;
                }
            }
        }

        /// <summary>
        /// 关闭会话（幂等，可重复调用）
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
                LogHelper.Instance.Warn(string.Format("端口 {0} 关闭客户端连接异常：{1}", LocalPort, ex.Message));
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
        /// 转换为客户端信息实体（供界面绑定）
        /// </summary>
        /// <returns>客户端信息对象，永不为 null</returns>
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
        /// 释放资源
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

        #region 私有方法

        /// <summary>
        /// 配置 TCP 保活探测（2026-09-14 新增）
        /// 用途：客户端异常断电/拔网线时不会发送 FIN 或 RST，服务端 Receive 会永久阻塞，
        ///       会话成为"僵尸"并占用连接数上限。开启保活后，探测包无响应即判定对端失联并清理会话。
        /// 与"空闲超时断开"的本质区别：保活探测包由对端操作系统内核直接应答，
        ///       对端应用层无需做任何事；只要对端机器在线，即使长时间不发一个字节，连接也不会被断开，
        ///       因此长连接完全不受影响。
        /// 注意：Windows 默认 KeepAliveTime 为 2 小时，必须显式设置探测时间，只写 KeepAlive=true 等同未启用。
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

                // SIO_KEEPALIVE_VALS：12 字节 = [是否启用(4B)] [空闲时间ms(4B)] [探测间隔ms(4B)]
                // .NET Framework 未提供 TcpKeepAliveTime 系列枚举，只能用 IOControl 精细化设置
                byte[] optionValues = new byte[12];
                BitConverter.GetBytes((uint)1).CopyTo(optionValues, 0);
                BitConverter.GetBytes((uint)idleMs).CopyTo(optionValues, 4);
                BitConverter.GetBytes((uint)intervalMs).CopyTo(optionValues, 8);

                _socket.IOControl(IOControlCode.KeepAliveValues, optionValues, null);
            }
            catch (Exception ex)
            {
                // 保活设置失败不影响正常收发，仅记录告警，避免因个别环境不支持而中断连接建立
                LogHelper.Instance.Warn(string.Format(
                    "端口 {0} 客户端 {1} 设置 TCP 保活失败，连接仍可正常使用：{2}",
                    LocalPort, RemoteEndPoint, ex.Message));
            }
        }

        /// <summary>
        /// 配置套接字发送行为（2026-09-14 新增）
        ///
        /// 1) SendTimeout —— 不设置时，若客户端连上后不读数据，服务端 Send 会一直阻塞到
        ///    发送缓冲区被写满为止；而"手动发送"跑在 UI 线程上，会把整个界面卡死。
        ///    设置超时后到点即抛 SocketException(TimedOut) 自行退出，界面不会被拖住。
        ///
        /// 2) NoDelay —— 关闭 Nagle 算法。Nagle 会把连续的小包合并后发送，
        ///    调试时看到的接收时序与真实发送时序不一致；调试工具需要真实时序，故关闭。
        ///
        /// 注意：这里刻意不设置 ReceiveTimeout —— 长连接可能长时间没有数据往来，
        ///       一旦设置空闲超时会把"在线但空闲"的长连接误杀，这正是要避免的。
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
                // 个别平台/协议栈不支持 NoDelay，不影响收发
                LogHelper.Instance.Warn(string.Format(
                    "端口 {0} 客户端 {1} 设置 NoDelay 失败，连接仍可正常使用：{2}",
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
                // 发送超时设置失败不影响正常收发，仅告警
                LogHelper.Instance.Warn(string.Format(
                    "端口 {0} 客户端 {1} 设置发送超时失败，连接仍可正常使用：{2}",
                    LocalPort, RemoteEndPoint, ex.Message));
            }
        }

        /// <summary>
        /// 接收循环 —— 阻塞接收，收到数据后触发事件
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
                        LogHelper.Instance.Info(string.Format("端口 {0} 客户端 {1} 连接中断：{2}",
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
                        LogHelper.Instance.Warn(string.Format("端口 {0} 接收异常：{1}", LocalPort, ex.Message));
                    }
                    break;
                }

                // 返回 0 表示对端已正常关闭
                if (received <= 0)
                {
                    break;
                }

                BytesReceived += received;
                LastActiveTime = DateTime.Now;

                // 复制数据后再抛出事件，避免缓冲区被下一轮接收覆盖
                byte[] copy = new byte[received];
                Buffer.BlockCopy(buffer, 0, copy, 0, received);
                RaiseDataReceived(copy, received);

                // 规约要求：循环内加时间间隔，降低数据粘连概率
                if (_ioIntervalMs > 0)
                {
                    try { Thread.Sleep(_ioIntervalMs); }
                    catch (Exception) { }
                }
            }

            // 循环退出即视为连接结束
            if (!_closed)
            {
                LogHelper.Instance.Info(string.Format("端口 {0} 客户端 {1} 已断开连接。", LocalPort, RemoteEndPoint));
                Close();
            }
        }

        /// <summary>
        /// 触发收到数据事件（异常不外泄）
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="length">有效长度</param>
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
                LogHelper.Instance.Error("接收事件处理异常：" + ex.Message, ex);
            }
        }

        /// <summary>
        /// 触发发出数据事件（异常不外泄）
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="length">有效长度</param>
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
                LogHelper.Instance.Error("发送事件处理异常：" + ex.Message, ex);
            }
        }

        #endregion
    }
}
