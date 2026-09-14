namespace TcpServer.Model
{
    /// <summary>
    /// 数据方向枚举 —— 用于区分收发流水中的记录类型
    /// </summary>
    public enum DataDirection
    {
        /// <summary>接收（来自客户端）</summary>
        Received = 0,

        /// <summary>发送（发往客户端）</summary>
        Sent = 1,

        /// <summary>系统提示（连接建立、断开、错误等）</summary>
        System = 2
    }
}
