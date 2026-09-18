using System;

namespace TcpServer.Model
{
    /// <summary>
    /// Port listening configuration entity - describes a TCP port listened on by this tool.
    /// </summary>
    public class PortConfig
    {
        /// <summary>
        /// Listening port number (valid range 1 ~ 65535).
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Whether this port is enabled - when false, "Start All" skips this port.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Remark name - helps identify the device this port emulates; may be empty.
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// Constructor - every field is initialized to a default value
        /// (convention: all declared variables must have a default value).
        /// </summary>
        public PortConfig()
        {
            Port = 0;
            Enabled = true;
            Remark = string.Empty;
        }

        /// <summary>
        /// Constructor - creates an instance from a port number.
        /// </summary>
        /// <param name="port">Port number.</param>
        public PortConfig(int port)
        {
            Port = port;
            Enabled = true;
            Remark = string.Empty;
        }

        /// <summary>
        /// Returns a text representation suitable for log output.
        /// </summary>
        /// <returns>Port configuration description text.</returns>
        public override string ToString()
        {
            return string.Format("Port={0}, Enabled={1}, Remark={2}",
                Port, Enabled, Remark ?? string.Empty);
        }
    }
}
