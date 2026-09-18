namespace TcpServer.Model
{
    /// <summary>
    /// Port runtime state enumeration.
    /// </summary>
    public enum PortState
    {
        /// <summary>Stopped (not listening)</summary>
        Stopped = 0,

        /// <summary>Starting up</summary>
        Starting = 1,

        /// <summary>Listening</summary>
        Listening = 2,

        /// <summary>Stopping</summary>
        Stopping = 3,

        /// <summary>Start failed (port already in use, etc.)</summary>
        Faulted = 4
    }
}
