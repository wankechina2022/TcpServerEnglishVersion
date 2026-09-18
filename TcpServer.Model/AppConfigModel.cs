using System;
using System.Collections.Generic;

namespace TcpServer.Model
{
    /// <summary>
    /// Application configuration root object - maps to the top-level structure of Config\portconfig.json.
    /// </summary>
    public class AppConfigModel
    {
        /// <summary>
        /// Configuration schema version - used to stay compatible with older files during future upgrades.
        /// </summary>
        public string ConfigVersion { get; set; }

        /// <summary>
        /// Listening address (e.g. 127.0.0.1, a local NIC address, or 0.0.0.0).
        /// </summary>
        public string ListenIp { get; set; }

        /// <summary>
        /// Starting port number (default 60000).
        /// </summary>
        public int StartPort { get; set; }

        /// <summary>
        /// Number of ports - how many consecutive ports to open from the starting port.
        /// </summary>
        public int PortCount { get; set; }

        /// <summary>
        /// Whether to start listening automatically after launch (default false, to avoid occupying ports by accident).
        /// </summary>
        public bool AutoStartOnLaunch { get; set; }

        /// <summary>
        /// Whether the data area is displayed as hexadecimal (false means ASCII).
        /// </summary>
        public bool DisplayAsHex { get; set; }

        /// <summary>
        /// Port list - the main body of the business configuration.
        /// </summary>
        public List<PortConfig> Ports { get; set; }

        /// <summary>
        /// Auto-reply rule list.
        /// </summary>
        public List<AutoReplyRule> Rules { get; set; }

        /// <summary>
        /// Time the configuration was last saved.
        /// </summary>
        public DateTime LastSavedTime { get; set; }

        /// <summary>Reserved field 1 (convention: keep 5 extension fields so future upgrades do not change the schema)</summary>
        public string Exp1 { get; set; }

        /// <summary>Reserved field 2</summary>
        public string Exp2 { get; set; }

        /// <summary>Reserved field 3</summary>
        public string Exp3 { get; set; }

        /// <summary>Reserved field 4</summary>
        public string Exp4 { get; set; }

        /// <summary>Reserved field 5</summary>
        public string Exp5 { get; set; }

        /// <summary>
        /// Constructor - every field is initialized to a default value, and non-null collections are created.
        /// </summary>
        public AppConfigModel()
        {
            ConfigVersion = "1.0";
            ListenIp = "127.0.0.1";
            StartPort = 60000;
            PortCount = 0;
            AutoStartOnLaunch = false;
            DisplayAsHex = false;
            Ports = new List<PortConfig>();
            Rules = new List<AutoReplyRule>();
            LastSavedTime = DateTime.MinValue;
            Exp1 = string.Empty;
            Exp2 = string.Empty;
            Exp3 = string.Empty;
            Exp4 = string.Empty;
            Exp5 = string.Empty;
        }
    }
}
