namespace TcpServer.Common
{
    /// <summary>
    /// Global constant definitions - all tunable parameters and "magic numbers" are gathered here
    /// for centralized maintenance.
    /// </summary>
    public static class AppConstants
    {
        /// <summary>Application version (convention: the main window must display the version number)</summary>
        public const string APP_VERSION = "1.0.0";

        /// <summary>Default listening address</summary>
        public const string DEFAULT_LISTEN_IP = "127.0.0.1";

        /// <summary>Address for listening on all network adapters</summary>
        public const string LISTEN_ALL_IP = "0.0.0.0";

        /// <summary>Default starting port</summary>
        public const int DEFAULT_START_PORT = 60000;

        /// <summary>Minimum port number</summary>
        public const int MIN_PORT = 1;

        /// <summary>Maximum port number</summary>
        public const int MAX_PORT = 65535;

        /// <summary>Minimum port count</summary>
        public const int MIN_PORT_COUNT = 1;

        /// <summary>Maximum port count</summary>
        public const int MAX_PORT_COUNT = 200;

        /// <summary>Log retention in months (expired files are deleted automatically)</summary>
        public const int LOG_KEEP_MONTHS = 6;

        /// <summary>Log directory name</summary>
        public const string LOG_FOLDER_NAME = "Logs";

        /// <summary>Configuration directory name</summary>
        public const string CONFIG_FOLDER_NAME = "Config";

        /// <summary>Business configuration file name</summary>
        public const string CONFIG_FILE_NAME = "portconfig.json";

        /// <summary>Minimum network read / write interval (milliseconds; 20~50ms is required to prevent packet sticking)</summary>
        public const int IO_MIN_INTERVAL_MS = 20;

        /// <summary>Receive buffer size (bytes)</summary>
        public const int RECEIVE_BUFFER_SIZE = 8192;

        /// <summary>
        /// Whether to enable TCP keep-alive probing (added 2026-09-14).
        /// Purpose: clean up zombie sessions left by clients that lost power or were unplugged
        /// (no FIN / RST notification is ever sent).
        /// </summary>
        public const bool KEEPALIVE_ENABLED = true;

        /// <summary>
        /// TCP keep-alive probing: how many seconds of idle time before probe packets start
        /// (added 2026-09-14, default 15 seconds).
        /// Note: this is not the "disconnect time" - it only means how long the connection may be
        /// idle before probing begins.
        /// </summary>
        public const int KEEPALIVE_IDLE_SECONDS = 15;

        /// <summary>
        /// TCP keep-alive probing: interval in seconds between two probes
        /// (added 2026-09-14, default 3 seconds).
        /// </summary>
        public const int KEEPALIVE_INTERVAL_SECONDS = 3;

        /// <summary>
        /// Back-off in milliseconds after an accept-connection exception (added 2026-09-14).
        /// Purpose: prevents a tight busy loop from driving CPU usage up when Accept keeps throwing.
        /// </summary>
        public const int ACCEPT_ERROR_BACKOFF_MS = 10;

        /// <summary>
        /// Send timeout in milliseconds (added 2026-09-14, default 10000).
        /// Purpose: when a client connects but never reads, Send blocks until the send buffer fills up.
        ///          Manual sending runs on the UI thread, which would freeze the whole interface;
        ///          with a timeout, an exception is thrown at that point and the call exits on its own.
        /// </summary>
        public const int SEND_TIMEOUT_MS = 10000;

        /// <summary>
        /// Maximum size of a single log file in bytes (added 2026-09-14, default 20MB).
        /// Purpose: during long, high-frequency traffic a single day's log file would grow without
        ///          limit; once the cap is exceeded the file rolls over to _1 / _2 volumes.
        /// </summary>
        public const long LOG_MAX_FILE_SIZE = 20L * 1024 * 1024;

        /// <summary>
        /// Maximum length of the asynchronous log write queue (added 2026-09-14, default 20000 entries).
        /// Purpose: protects memory when disk writes cannot keep up with production;
        ///          the oldest entries are dropped once the limit is exceeded.
        /// </summary>
        public const int LOG_QUEUE_MAX_LENGTH = 20000;

        /// <summary>
        /// Number of configuration backup files to keep (added 2026-09-14, default 20).
        /// Purpose: every configuration save creates a .bak file; without cleanup they accumulate forever.
        /// </summary>
        public const int CONFIG_BACKUP_KEEP_COUNT = 20;

        /// <summary>
        /// Watchdog inspection interval for the listener thread in milliseconds (added 2026-09-14, default 5000).
        /// Purpose: rebuild the listener automatically if the Accept thread dies unexpectedly, so the UI
        ///          never shows "Listening" while connections can never succeed.
        /// </summary>
        public const int WATCHDOG_INTERVAL_MS = 5000;

        /// <summary>
        /// Inspection interval in hours for periodic log cleanup (added 2026-09-14, default 24 hours).
        /// Purpose: cleanup used to run only once at startup, so a program that stayed up for a long time
        ///          never cleaned anything. It now runs periodically on a dedicated cleanup thread at this
        ///          interval, so expired logs are still purged during long uptimes.
        ///          Set to 0 to disable periodic cleanup (leaving only the once-at-startup cleanup).
        /// </summary>
        public const int LOG_CLEANUP_INTERVAL_HOURS = 24;

        /// <summary>
        /// Maximum milliseconds to wait for the periodic log cleanup thread to wake on stop (added 2026-09-14).
        /// Purpose: wake the cleanup thread immediately on stop, so shutdown does not sit idle for a whole
        ///          inspection period (following the same ManualResetEventSlim wake-up approach as the watchdog).
        /// </summary>
        public const int LOG_CLEANUP_STOP_TIMEOUT_MS = 3000;

        /// <summary>Single-instance mutex name</summary>
        public const string MUTEX_NAME = "TcpServer_MultiPort_Listener_SingleInstance_Mutex";

        /// <summary>Business configuration schema version (written to JSON to support future upgrade compatibility)</summary>
        public const string CONFIG_VERSION = "1.0";
    }
}
