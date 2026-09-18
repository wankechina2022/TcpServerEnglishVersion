using System;

namespace TcpServer.Model
{
    /// <summary>
    /// Port runtime state entity - a read-only view object bound to the main grid.
    /// </summary>
    public class PortRuntimeInfo
    {
        /// <summary>
        /// Listening port number.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Whether enabled (disabled ports are excluded from "Start All").
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Remark name.
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// Current runtime state.
        /// </summary>
        public PortState State { get; set; }

        /// <summary>
        /// Number of clients currently online.
        /// </summary>
        public int ClientCount { get; set; }

        /// <summary>
        /// Cumulative number of bytes received.
        /// </summary>
        public long BytesReceived { get; set; }

        /// <summary>
        /// Cumulative number of bytes sent.
        /// </summary>
        public long BytesSent { get; set; }

        /// <summary>
        /// Last activity time.
        /// </summary>
        public DateTime LastActiveTime { get; set; }

        /// <summary>
        /// Supplementary state description (e.g. "port already in use", "stopped"); may be empty.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// UI display text for the state.
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
        /// Constructor - every field is initialized to a default value.
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
