using System;
using TcpServer.Model;

namespace TcpServer.BLL
{
    /// <summary>
    /// Client connection change event arguments (online / offline).
    /// </summary>
    public class ClientChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Port number this client belongs to.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Unique session identifier.
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// Client remote endpoint (IP:Port).
        /// </summary>
        public string RemoteEndPoint { get; set; }

        /// <summary>
        /// true means online, false means offline.
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// Number of clients currently online on this port.
        /// </summary>
        public int ClientCount { get; set; }

        /// <summary>
        /// Time the event occurred.
        /// </summary>
        public DateTime EventTime { get; set; }

        /// <summary>
        /// Constructor - every field is initialized to a default value.
        /// </summary>
        public ClientChangedEventArgs()
        {
            Port = 0;
            SessionId = string.Empty;
            RemoteEndPoint = string.Empty;
            IsConnected = false;
            ClientCount = 0;
            EventTime = DateTime.Now;
        }

        /// <summary>
        /// Builds event arguments from client information.
        /// </summary>
        /// <param name="port">Port number.</param>
        /// <param name="client">Client information; may be null.</param>
        /// <param name="isConnected">Whether the client came online.</param>
        /// <param name="clientCount">Current online count.</param>
        /// <returns>Event arguments object, never null.</returns>
        public static ClientChangedEventArgs Create(int port, ClientInfo client, bool isConnected, int clientCount)
        {
            ClientChangedEventArgs args = new ClientChangedEventArgs();
            args.Port = port;
            args.IsConnected = isConnected;
            args.ClientCount = clientCount;
            args.EventTime = DateTime.Now;

            if (client != null)
            {
                args.SessionId = client.SessionId ?? string.Empty;
                args.RemoteEndPoint = client.RemoteEndPoint ?? string.Empty;
            }

            return args;
        }
    }
}
