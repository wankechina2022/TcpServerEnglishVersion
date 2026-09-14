using System;
using TcpServer.Model;

namespace TcpServer.BLL
{
    /// <summary>
    /// 端口状态变化事件参数
    /// </summary>
    public class PortStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 端口号
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 变化后的状态
        /// </summary>
        public PortState State { get; set; }

        /// <summary>
        /// 状态说明（启动失败原因等），可为空
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 事件发生时间
        /// </summary>
        public DateTime EventTime { get; set; }

        /// <summary>
        /// 构造函数 —— 所有字段均提供默认值
        /// </summary>
        public PortStateChangedEventArgs()
        {
            Port = 0;
            State = PortState.Stopped;
            Message = string.Empty;
            EventTime = DateTime.Now;
        }
    }
}
