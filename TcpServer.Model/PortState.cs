namespace TcpServer.Model
{
    /// <summary>
    /// 端口运行状态枚举
    /// </summary>
    public enum PortState
    {
        /// <summary>已停止（未监听）</summary>
        Stopped = 0,

        /// <summary>正在启动</summary>
        Starting = 1,

        /// <summary>监听中</summary>
        Listening = 2,

        /// <summary>正在停止</summary>
        Stopping = 3,

        /// <summary>启动失败（端口被占用等）</summary>
        Faulted = 4
    }
}
