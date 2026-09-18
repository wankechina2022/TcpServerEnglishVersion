namespace TcpServer.Model
{
    /// <summary>
    /// Data direction enumeration - distinguishes record types in the send / receive traffic log.
    /// </summary>
    public enum DataDirection
    {
        /// <summary>Received (from the client)</summary>
        Received = 0,

        /// <summary>Sent (to the client)</summary>
        Sent = 1,

        /// <summary>System notice (connection established, closed, error, etc.)</summary>
        System = 2
    }
}
