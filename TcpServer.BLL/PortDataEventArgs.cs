using System;
using TcpServer.Model;

namespace TcpServer.BLL
{
    /// <summary>
    /// 端口数据事件参数（收到数据 / 发出数据）
    /// </summary>
    public class PortDataEventArgs : EventArgs
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
        /// 数据方向
        /// </summary>
        public DataDirection Direction { get; set; }

        /// <summary>
        /// 原始字节数据
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// 本次有效字节长度
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// 事件发生时间
        /// </summary>
        public DateTime EventTime { get; set; }

        /// <summary>
        /// 十六进制显示文本
        /// </summary>
        public string HexText
        {
            get
            {
                if (Data == null || Length < 1) { return string.Empty; }
                return Common.Helpers.HexHelper.BytesToHex(Data, Length);
            }
        }

        /// <summary>
        /// 构造函数 —— 所有字段均提供默认值
        /// </summary>
        public PortDataEventArgs()
        {
            Port = 0;
            SessionId = string.Empty;
            RemoteEndPoint = string.Empty;
            Direction = DataDirection.System;
            Data = new byte[0];
            Length = 0;
            EventTime = DateTime.Now;
        }

        /// <summary>
        /// 由字节数据构造事件参数
        /// </summary>
        /// <param name="port">端口号</param>
        /// <param name="sessionId">会话标识</param>
        /// <param name="remoteEndPoint">远端地址</param>
        /// <param name="direction">数据方向</param>
        /// <param name="data">原始数据</param>
        /// <param name="length">有效长度</param>
        /// <returns>事件参数对象，永不为 null</returns>
        public static PortDataEventArgs Create(int port, string sessionId, string remoteEndPoint,
            DataDirection direction, byte[] data, int length)
        {
            PortDataEventArgs args = new PortDataEventArgs();
            args.Port = port;
            args.SessionId = sessionId ?? string.Empty;
            args.RemoteEndPoint = remoteEndPoint ?? string.Empty;
            args.Direction = direction;
            args.Data = data ?? new byte[0];
            args.Length = length < 0 ? 0 : length;
            args.EventTime = DateTime.Now;
            return args;
        }
    }
}
