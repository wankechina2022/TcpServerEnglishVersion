using System;

namespace TcpServer.Model
{
    /// <summary>
    /// 端口运行时状态实体 —— 供主界面表格绑定的只读视图对象
    /// </summary>
    public class PortRuntimeInfo
    {
        /// <summary>
        /// 监听端口号
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 是否启用（未启用的端口不参与"启动全部"）
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// 备注名称
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// 当前运行状态
        /// </summary>
        public PortState State { get; set; }

        /// <summary>
        /// 当前在线客户端数量
        /// </summary>
        public int ClientCount { get; set; }

        /// <summary>
        /// 累计接收字节数
        /// </summary>
        public long BytesReceived { get; set; }

        /// <summary>
        /// 累计发送字节数
        /// </summary>
        public long BytesSent { get; set; }

        /// <summary>
        /// 最后一次活动时间
        /// </summary>
        public DateTime LastActiveTime { get; set; }

        /// <summary>
        /// 状态补充说明（如"端口被占用""已停止"），可为空
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 状态的界面显示文本
        /// </summary>
        public string StateText
        {
            get
            {
                switch (State)
                {
                    case PortState.Listening:
                        return "Listening";
                    case PortState.Starting:
                        return "Starting";
                    case PortState.Stopping:
                        return "Stopping";
                    case PortState.Faulted:
                        return "Start Failed";
                    default:
                        return "Stopped";
                }
            }
        }

        /// <summary>
        /// 构造函数 —— 所有字段均提供默认值
        /// </summary>
        public PortRuntimeInfo()
        {
            Port = 0;
            Enabled = true;
            Remark = string.Empty;
            State = PortState.Stopped;
            ClientCount = 0;
            BytesReceived = 0L;
            BytesSent = 0L;
            LastActiveTime = DateTime.MinValue;
            Message = string.Empty;
        }
    }
}
