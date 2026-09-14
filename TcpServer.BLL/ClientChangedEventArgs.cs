using System;
using TcpServer.Model;

namespace TcpServer.BLL
{
    /// <summary>
    /// 客户端连接变化事件参数（上线 / 下线）
    /// </summary>
    public class ClientChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 所属端口号
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 会话唯一标识
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// 客户端远端地址（IP:Port）
        /// </summary>
        public string RemoteEndPoint { get; set; }

        /// <summary>
        /// true 表示上线，false 表示下线
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// 当前该端口的在线客户端数量
        /// </summary>
        public int ClientCount { get; set; }

        /// <summary>
        /// 事件发生时间
        /// </summary>
        public DateTime EventTime { get; set; }

        /// <summary>
        /// 构造函数 —— 所有字段均提供默认值
        /// </summary>
        public ClientChangedEventArgs()
        {
            Port = 0;
            SessionId = string.Empty;
            RemoteEndPoint = string.Empty;
            IsConnected = false;
            ClientCount = 0;
            EventTime = DateTime.Now;
        }

        /// <summary>
        /// 由客户端信息构造事件参数
        /// </summary>
        /// <param name="port">端口号</param>
        /// <param name="client">客户端信息，可为 null</param>
        /// <param name="isConnected">是否为上线</param>
        /// <param name="clientCount">当前在线数量</param>
        /// <returns>事件参数对象，永不为 null</returns>
        public static ClientChangedEventArgs Create(int port, ClientInfo client, bool isConnected, int clientCount)
        {
            ClientChangedEventArgs args = new ClientChangedEventArgs();
            args.Port = port;
            args.IsConnected = isConnected;
            args.ClientCount = clientCount;
            args.EventTime = DateTime.Now;

            if (client != null)
            {
                args.SessionId = client.SessionId ?? string.Empty;
                args.RemoteEndPoint = client.RemoteEndPoint ?? string.Empty;
            }

            return args;
        }
    }
}
