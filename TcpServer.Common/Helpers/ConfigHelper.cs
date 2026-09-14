using System;
using System.Configuration;

namespace TcpServer.Common.Helpers
{
    /// <summary>
    /// 配置读取帮助类 —— 统一读取 App.config 中的只读运行参数
    /// 规约约定：配置文件数据只读取不反写；可反写的业务参数由 DAL 层单独持久化
    /// </summary>
    public static class ConfigHelper
    {
        /// <summary>
        /// 默认监听地址（默认 127.0.0.1）
        /// </summary>
        public static string DefaultListenIp
        {
            get { return GetStringConfig("DefaultListenIp", AppConstants.DEFAULT_LISTEN_IP); }
        }

        /// <summary>
        /// 默认起始端口（默认 60000）
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
        /// 日志保留月数（默认 6 个月）
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
        /// 接收缓冲区大小（字节，默认 8192）
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
        /// 网络读写最小间隔（毫秒，默认 20）
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
        /// 是否在启动时自动开始监听（默认 false，避免误占端口）
        /// </summary>
        public static bool AutoStartOnLaunch
        {
            get { return GetBoolConfig("AutoStartOnLaunch", false); }
        }

        /// <summary>
        /// 是否启用 TCP 保活探测（2026-09-14 新增，默认 true）
        /// </summary>
        public static bool KeepAliveEnabled
        {
            get { return GetBoolConfig("KeepAliveEnabled", AppConstants.KEEPALIVE_ENABLED); }
        }

        /// <summary>
        /// TCP 保活探测启动前的空闲秒数（2026-09-14 新增，默认 15 秒，有效范围 1~7200）
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
        /// TCP 保活探测的间隔秒数（2026-09-14 新增，默认 3 秒，有效范围 1~300）
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
        /// 发送超时毫秒数（2026-09-14 新增，默认 10000，有效范围 500~600000）
        /// 设为 0 表示不限制（不推荐：客户端不读数据时会让界面卡死）
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
        /// 单个日志文件大小上限（字节，2026-09-14 新增，默认 20MB，有效范围 1MB~2GB）
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
        /// 日志异步写入队列最大长度（2026-09-14 新增，默认 20000，有效范围 1000~1000000）
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
        /// 配置备份文件保留份数（2026-09-14 新增，默认 20，有效范围 1~1000）
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
        /// 监听线程看门狗巡检间隔毫秒数（2026-09-14 新增，默认 5000，有效范围 1000~60000）
        /// 设为 0 表示关闭看门狗
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
        /// 日志定期清理的巡检间隔小时数（2026-09-14 新增，默认 24，有效范围 1~8760）
        /// 设为 0 表示关闭定期清理（仅保留启动时清理一次）
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
        /// 获取字符串型配置项
        /// </summary>
        /// <param name="key">配置键名</param>
        /// <param name="defaultValue">缺省值</param>
        /// <returns>配置值；未配置或为空时返回缺省值</returns>
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
                LogHelper.Instance.Warn(string.Format("读取配置项 [{0}] 失败，已使用默认值。原因：{1}", key, ex.Message));
                return defaultValue ?? string.Empty;
            }
        }

        /// <summary>
        /// 获取整型配置项
        /// </summary>
        /// <param name="key">配置键名</param>
        /// <param name="defaultValue">缺省值</param>
        /// <returns>配置值；解析失败时返回缺省值</returns>
        public static int GetIntConfig(string key, int defaultValue = 0)
        {
            string value = GetStringConfig(key, string.Empty);
            int result;
            return int.TryParse(value, out result) ? result : defaultValue;
        }

        /// <summary>
        /// 获取布尔型配置项
        /// </summary>
        /// <param name="key">配置键名</param>
        /// <param name="defaultValue">缺省值</param>
        /// <returns>配置值；解析失败时返回缺省值</returns>
        public static bool GetBoolConfig(string key, bool defaultValue = false)
        {
            string value = GetStringConfig(key, string.Empty);
            bool result;
            return bool.TryParse(value, out result) ? result : defaultValue;
        }

        /// <summary>
        /// 获取长整型配置项（2026-09-14 新增，用于文件大小等大数值配置）
        /// </summary>
        /// <param name="key">配置键名</param>
        /// <param name="defaultValue">缺省值</param>
        /// <returns>配置值；解析失败时返回缺省值</returns>
        public static long GetLongConfig(string key, long defaultValue = 0L)
        {
            string value = GetStringConfig(key, string.Empty);
            long result;
            return long.TryParse(value, out result) ? result : defaultValue;
        }
    }
}
