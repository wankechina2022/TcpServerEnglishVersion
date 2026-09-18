using System;
using System.Configuration;

namespace TcpServer.Common.Helpers
{
    /// <summary>
    /// Configuration reading helper - centralizes reading of read-only runtime parameters from App.config.
    /// Convention: configuration file data is read-only and never written back; writable business parameters
    ///             are persisted separately by the DAL layer.
    /// </summary>
    public static class ConfigHelper
    {
        /// <summary>
        /// Default listening address (default 127.0.0.1).
        /// </summary>
        public static string DefaultListenIp
        {
            get { return GetStringConfig("DefaultListenIp", AppConstants.DEFAULT_LISTEN_IP); }
        }

        /// <summary>
        /// Default starting port (default 60000).
        /// </summary>
        public static int DefaultStartPort
        {
            get
            {
                int value = GetIntConfig("DefaultStartPort", AppConstants.DEFAULT_START_PORT);
                if (value < AppConstants.MIN_PORT || value > AppConstants.MAX_PORT)
                {
                    return AppConstants.DEFAULT_START_PORT;
                }
                return value;
            }
        }

        /// <summary>
        /// Log retention in months (default 6 months).
        /// </summary>
        public static int LogKeepMonths
        {
            get
            {
                int value = GetIntConfig("LogKeepMonths", AppConstants.LOG_KEEP_MONTHS);
                return value < 1 ? AppConstants.LOG_KEEP_MONTHS : value;
            }
        }

        /// <summary>
        /// Receive buffer size (bytes, default 8192).
        /// </summary>
        public static int ReceiveBufferSize
        {
            get
            {
                int value = GetIntConfig("ReceiveBufferSize", AppConstants.RECEIVE_BUFFER_SIZE);
                if (value < 1024 || value > 1024 * 1024)
                {
                    return AppConstants.RECEIVE_BUFFER_SIZE;
                }
                return value;
            }
        }

        /// <summary>
        /// Minimum network read / write interval (milliseconds, default 20).
        /// </summary>
        public static int IoMinIntervalMs
        {
            get
            {
                int value = GetIntConfig("IoMinIntervalMs", AppConstants.IO_MIN_INTERVAL_MS);
                if (value < 0 || value > 5000)
                {
                    return AppConstants.IO_MIN_INTERVAL_MS;
                }
                return value;
            }
        }

        /// <summary>
        /// Whether to start listening automatically at launch (default false, to avoid occupying ports by accident).
        /// </summary>
        public static bool AutoStartOnLaunch
        {
            get { return GetBoolConfig("AutoStartOnLaunch", false); }
        }

        /// <summary>
        /// Whether to enable TCP keep-alive probing (added 2026-09-14, default true).
        /// </summary>
        public static bool KeepAliveEnabled
        {
            get { return GetBoolConfig("KeepAliveEnabled", AppConstants.KEEPALIVE_ENABLED); }
        }

        /// <summary>
        /// Idle seconds before TCP keep-alive probing starts (added 2026-09-14, default 15 seconds, valid range 1~7200).
        /// </summary>
        public static int KeepAliveIdleSeconds
        {
            get
            {
                int value = GetIntConfig("KeepAliveIdleSeconds", AppConstants.KEEPALIVE_IDLE_SECONDS);
                if (value < 1 || value > 7200)
                {
                    return AppConstants.KEEPALIVE_IDLE_SECONDS;
                }
                return value;
            }
        }

        /// <summary>
        /// Interval in seconds between TCP keep-alive probes (added 2026-09-14, default 3 seconds, valid range 1~300).
        /// </summary>
        public static int KeepAliveIntervalSeconds
        {
            get
            {
                int value = GetIntConfig("KeepAliveIntervalSeconds", AppConstants.KEEPALIVE_INTERVAL_SECONDS);
                if (value < 1 || value > 300)
                {
                    return AppConstants.KEEPALIVE_INTERVAL_SECONDS;
                }
                return value;
            }
        }

        /// <summary>
        /// Send timeout in milliseconds (added 2026-09-14, default 10000, valid range 500~600000).
        /// A value of 0 means unlimited (not recommended: the interface will freeze when the client stops reading).
        /// </summary>
        public static int SendTimeoutMs
        {
            get
            {
                int value = GetIntConfig("SendTimeoutMs", AppConstants.SEND_TIMEOUT_MS);
                if (value == 0)
                {
                    return 0;
                }
                if (value < 500 || value > 600000)
                {
                    return AppConstants.SEND_TIMEOUT_MS;
                }
                return value;
            }
        }

        /// <summary>
        /// Maximum size of a single log file (bytes, added 2026-09-14, default 20MB, valid range 1MB~2GB).
        /// </summary>
        public static long LogMaxFileSize
        {
            get
            {
                long value = GetLongConfig("LogMaxFileSizeBytes", AppConstants.LOG_MAX_FILE_SIZE);
                if (value < 1024 * 1024 || value > 2048L * 1024 * 1024)
                {
                    return AppConstants.LOG_MAX_FILE_SIZE;
                }
                return value;
            }
        }

        /// <summary>
        /// Maximum length of the asynchronous log write queue (added 2026-09-14, default 20000, valid range 1000~1000000).
        /// </summary>
        public static int LogQueueMaxLength
        {
            get
            {
                int value = GetIntConfig("LogQueueMaxLength", AppConstants.LOG_QUEUE_MAX_LENGTH);
                if (value < 1000 || value > 1000000)
                {
                    return AppConstants.LOG_QUEUE_MAX_LENGTH;
                }
                return value;
            }
        }

        /// <summary>
        /// Number of configuration backup files to keep (added 2026-09-14, default 20, valid range 1~1000).
        /// </summary>
        public static int ConfigBackupKeepCount
        {
            get
            {
                int value = GetIntConfig("ConfigBackupKeepCount", AppConstants.CONFIG_BACKUP_KEEP_COUNT);
                if (value < 1 || value > 1000)
                {
                    return AppConstants.CONFIG_BACKUP_KEEP_COUNT;
                }
                return value;
            }
        }

        /// <summary>
        /// Watchdog inspection interval for the listener thread in milliseconds
        /// (added 2026-09-14, default 5000, valid range 1000~60000).
        /// A value of 0 disables the watchdog.
        /// </summary>
        public static int WatchdogIntervalMs
        {
            get
            {
                int value = GetIntConfig("WatchdogIntervalMs", AppConstants.WATCHDOG_INTERVAL_MS);
                if (value == 0)
                {
                    return 0;
                }
                if (value < 1000 || value > 60000)
                {
                    return AppConstants.WATCHDOG_INTERVAL_MS;
                }
                return value;
            }
        }

        /// <summary>
        /// Inspection interval in hours for periodic log cleanup
        /// (added 2026-09-14, default 24, valid range 1~8760).
        /// A value of 0 disables periodic cleanup (leaving only the once-at-startup cleanup).
        /// </summary>
        public static int LogCleanupIntervalHours
        {
            get
            {
                int value = GetIntConfig("LogCleanupIntervalHours", AppConstants.LOG_CLEANUP_INTERVAL_HOURS);
                if (value == 0)
                {
                    return 0;
                }
                if (value < 1 || value > 8760)
                {
                    return AppConstants.LOG_CLEANUP_INTERVAL_HOURS;
                }
                return value;
            }
        }

        /// <summary>
        /// Reads a string configuration item.
        /// </summary>
        /// <param name="key">Configuration key.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>Configuration value; returns the default when unset or empty.</returns>
        public static string GetStringConfig(string key, string defaultValue = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    return defaultValue ?? string.Empty;
                }

                string value = ConfigurationManager.AppSettings[key];
                return string.IsNullOrWhiteSpace(value) ? (defaultValue ?? string.Empty) : value.Trim();
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Warn(string.Format("Failed to read config item [{0}], using the default value. Reason: {1}", key, ex.Message));
                return defaultValue ?? string.Empty;
            }
        }

        /// <summary>
        /// Reads an integer configuration item.
        /// </summary>
        /// <param name="key">Configuration key.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>Configuration value; returns the default when parsing fails.</returns>
        public static int GetIntConfig(string key, int defaultValue = 0)
        {
            string value = GetStringConfig(key, string.Empty);
            int result;
            return int.TryParse(value, out result) ? result : defaultValue;
        }

        /// <summary>
        /// Reads a boolean configuration item.
        /// </summary>
        /// <param name="key">Configuration key.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>Configuration value; returns the default when parsing fails.</returns>
        public static bool GetBoolConfig(string key, bool defaultValue = false)
        {
            string value = GetStringConfig(key, string.Empty);
            bool result;
            return bool.TryParse(value, out result) ? result : defaultValue;
        }

        /// <summary>
        /// Reads a long integer configuration item
        /// (added 2026-09-14, for large numeric values such as file sizes).
        /// </summary>
        /// <param name="key">Configuration key.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>Configuration value; returns the default when parsing fails.</returns>
        public static long GetLongConfig(string key, long defaultValue = 0L)
        {
            string value = GetStringConfig(key, string.Empty);
            long result;
            return long.TryParse(value, out result) ? result : defaultValue;
        }
    }
}
