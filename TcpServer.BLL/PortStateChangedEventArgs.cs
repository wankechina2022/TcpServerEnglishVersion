using System;
using TcpServer.Model;

namespace TcpServer.BLL
{
    /// <summary>
    /// Port state change event arguments.
    /// </summary>
    public class PortStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Port number.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// The state after the change.
        /// </summary>
        public PortState State { get; set; }

        /// <summary>
        /// State description (start failure reason, etc.); may be empty.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Time the event occurred.
        /// </summary>
        public DateTime EventTime { get; set; }

        /// <summary>
        /// Constructor - every field is initialized to a default value.
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
