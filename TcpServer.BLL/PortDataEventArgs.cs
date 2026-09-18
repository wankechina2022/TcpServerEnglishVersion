using System;
using TcpServer.Model;

namespace TcpServer.BLL
{
    /// <summary>
    /// Port data event arguments (data received / data sent).
    /// </summary>
    public class PortDataEventArgs : EventArgs
    {
        /// <summary>
        /// Port number this data belongs to.
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
        /// Data direction.
        /// </summary>
        public DataDirection Direction { get; set; }

        /// <summary>
        /// Raw byte data.
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// Effective byte length for this event.
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// Time the event occurred.
        /// </summary>
        public DateTime EventTime { get; set; }

        /// <summary>
        /// Hexadecimal display text.
        /// </summary>
        public string HexText
        {
            get
            {
                if (Data == null || Length < 1) { return string.Empty; }
                return Common.Helpers.HexHelper.BytesToHex(Data, Length);
            }
        }

        /// <summary>
        /// Constructor - every field is initialized to a default value.
        /// </summary>
        public PortDataEventArgs()
        {
            Port = 0;
            SessionId = string.Empty;
            RemoteEndPoint = string.Empty;
            Direction = DataDirection.System;
            Data = new byte[0];
            Length = 0;
            EventTime = DateTime.Now;
        }

        /// <summary>
        /// Builds event arguments from byte data.
        /// </summary>
        /// <param name="port">Port number.</param>
        /// <param name="sessionId">Session identifier.</param>
        /// <param name="remoteEndPoint">Remote endpoint.</param>
        /// <param name="direction">Data direction.</param>
        /// <param name="data">Raw data.</param>
        /// <param name="length">Effective length.</param>
        /// <returns>Event arguments object, never null.</returns>
        public static PortDataEventArgs Create(int port, string sessionId, string remoteEndPoint,
            DataDirection direction, byte[] data, int length)
        {
            PortDataEventArgs args = new PortDataEventArgs();
            args.Port = port;
            args.SessionId = sessionId ?? string.Empty;
            args.RemoteEndPoint = remoteEndPoint ?? string.Empty;
            args.Direction = direction;
            args.Data = data ?? new byte[0];
            args.Length = length < 0 ? 0 : length;
            args.EventTime = DateTime.Now;
            return args;
        }
    }
}
