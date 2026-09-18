using System;

namespace TcpServer.Model
{
    /// <summary>
    /// Client connection information entity - describes a connected TCP client session.
    /// </summary>
    public class ClientInfo
    {
        /// <summary>
        /// Unique session identifier (taken from an auto-increment sequence;
        /// both the UI and the logs use it to distinguish sessions).
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// The local listening port this session belongs to.
        /// </summary>
        public int LocalPort { get; set; }

        /// <summary>
        /// Client remote endpoint (IP:Port).
        /// </summary>
        public string RemoteEndPoint { get; set; }

        /// <summary>
        /// Time the connection was established.
        /// </summary>
        public DateTime ConnectedTime { get; set; }

        /// <summary>
        /// Cumulative number of bytes received.
        /// </summary>
        public long BytesReceived { get; set; }

        /// <summary>
        /// Cumulative number of bytes sent.
        /// </summary>
        public long BytesSent { get; set; }

        /// <summary>
        /// Last activity time (refreshed on both send and receive).
        /// </summary>
        public DateTime LastActiveTime { get; set; }

        /// <summary>
        /// Constructor - every field is initialized to a default value.
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
        /// Returns a text representation suitable for log output.
        /// </summary>
        /// <returns>Client information description text.</returns>
        public override string ToString()
        {
            return string.Format("[{0}] {1}", SessionId ?? string.Empty, RemoteEndPoint ?? string.Empty);
        }
    }
}
