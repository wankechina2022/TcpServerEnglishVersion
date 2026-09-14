using System;

namespace TcpServer.Model
{
    /// <summary>
    /// 客户端连接信息实体 —— 描述一个已接入的 TCP 客户端会话
    /// </summary>
    public class ClientInfo
    {
        /// <summary>
        /// 会话唯一标识（取自自增序号，界面与日志均以此区分）
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// 所属的本地监听端口号
        /// </summary>
        public int LocalPort { get; set; }

        /// <summary>
        /// 客户端远端地址（IP:Port）
        /// </summary>
        public string RemoteEndPoint { get; set; }

        /// <summary>
        /// 建立连接的时间
        /// </summary>
        public DateTime ConnectedTime { get; set; }

        /// <summary>
        /// 累计接收字节数
        /// </summary>
        public long BytesReceived { get; set; }

        /// <summary>
        /// 累计发送字节数
        /// </summary>
        public long BytesSent { get; set; }

        /// <summary>
        /// 最后一次活动时间（收发均会刷新）
        /// </summary>
        public DateTime LastActiveTime { get; set; }

        /// <summary>
        /// 构造函数 —— 所有字段均提供默认值
        /// </summary>
        public ClientInfo()
        {
            SessionId = string.Empty;
            LocalPort = 0;
            RemoteEndPoint = string.Empty;
            ConnectedTime = DateTime.MinValue;
            BytesReceived = 0L;
            BytesSent = 0L;
            LastActiveTime = DateTime.MinValue;
        }

        /// <summary>
        /// 返回便于日志输出的文本
        /// </summary>
        /// <returns>客户端信息描述文本</returns>
        public override string ToString()
        {
            return string.Format("[{0}] {1}", SessionId ?? string.Empty, RemoteEndPoint ?? string.Empty);
        }
    }
}
