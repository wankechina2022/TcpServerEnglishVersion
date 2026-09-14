namespace TcpServer.Common
{
    /// <summary>
    /// 全局常量定义 —— 所有可调参数与"魔法数字"集中于此，便于统一维护
    /// </summary>
    public static class AppConstants
    {
        /// <summary>程序版本号（规约约定：主界面必须展示版本号）</summary>
        public const string APP_VERSION = "1.0.0";

        /// <summary>默认监听地址</summary>
        public const string DEFAULT_LISTEN_IP = "127.0.0.1";

        /// <summary>监听全部网卡的地址</summary>
        public const string LISTEN_ALL_IP = "0.0.0.0";

        /// <summary>默认起始端口</summary>
        public const int DEFAULT_START_PORT = 60000;

        /// <summary>端口号最小值</summary>
        public const int MIN_PORT = 1;

        /// <summary>端口号最大值</summary>
        public const int MAX_PORT = 65535;

        /// <summary>端口数量最小值</summary>
        public const int MIN_PORT_COUNT = 1;

        /// <summary>端口数量最大值</summary>
        public const int MAX_PORT_COUNT = 200;

        /// <summary>日志保留月数（超期自动删除）</summary>
        public const int LOG_KEEP_MONTHS = 6;

        /// <summary>日志目录名</summary>
        public const string LOG_FOLDER_NAME = "Logs";

        /// <summary>配置目录名</summary>
        public const string CONFIG_FOLDER_NAME = "Config";

        /// <summary>业务配置文件文件名</summary>
        public const string CONFIG_FILE_NAME = "portconfig.json";

        /// <summary>网络读写最小间隔（毫秒，规约要求 20~50ms 防粘包）</summary>
        public const int IO_MIN_INTERVAL_MS = 20;

        /// <summary>接收缓冲区大小（字节）</summary>
        public const int RECEIVE_BUFFER_SIZE = 8192;

        /// <summary>
        /// 是否启用 TCP 保活探测（2026-09-14 新增）
        /// 用途：清理客户端断电/拔线（无 FIN/RST 通知）造成的僵尸会话
        /// </summary>
        public const bool KEEPALIVE_ENABLED = true;

        /// <summary>
        /// TCP 保活探测：连接空闲多少秒后开始发探测包（2026-09-14 新增，默认 15 秒）
        /// 注意：该值不是"断开时间"，仅表示空闲多久后开始探测
        /// </summary>
        public const int KEEPALIVE_IDLE_SECONDS = 15;

        /// <summary>
        /// TCP 保活探测：两次探测的间隔秒数（2026-09-14 新增，默认 3 秒）
        /// </summary>
        public const int KEEPALIVE_INTERVAL_SECONDS = 3;

        /// <summary>
        /// 接受连接异常后的退避毫秒数（2026-09-14 新增）
        /// 用途：避免 Accept 持续抛异常时循环空转导致 CPU 占用飙升
        /// </summary>
        public const int ACCEPT_ERROR_BACKOFF_MS = 10;

        /// <summary>
        /// 发送超时毫秒数（2026-09-14 新增，默认 10000）
        /// 用途：客户端连上后不读数据时，Send 会一直阻塞直至发送缓冲写满，
        ///       而手动发送跑在 UI 线程上，会把整个界面卡死；设置超时后到点抛异常自行退出。
        /// </summary>
        public const int SEND_TIMEOUT_MS = 10000;

        /// <summary>
        /// 单个日志文件大小上限（字节，2026-09-14 新增，默认 20MB）
        /// 用途：长时间高频收发时单日日志文件无上限增长，超限后自动滚动为 _1 / _2 分卷
        /// </summary>
        public const long LOG_MAX_FILE_SIZE = 20L * 1024 * 1024;

        /// <summary>
        /// 日志异步写入队列最大长度（2026-09-14 新增，默认 20000 条）
        /// 用途：写盘跟不上产生速度时保护内存，超过后丢弃最旧的日志
        /// </summary>
        public const int LOG_QUEUE_MAX_LENGTH = 20000;

        /// <summary>
        /// 配置备份文件保留份数（2026-09-14 新增，默认 20 份）
        /// 用途：每次保存配置都会生成一份 .bak，不清理会无限累积
        /// </summary>
        public const int CONFIG_BACKUP_KEEP_COUNT = 20;

        /// <summary>
        /// 监听线程看门狗巡检间隔毫秒数（2026-09-14 新增，默认 5000）
        /// 用途：Accept 线程意外死亡时自动重建监听，避免界面显示"监听中"却永远连不上
        /// </summary>
        public const int WATCHDOG_INTERVAL_MS = 5000;

        /// <summary>
        /// 日志定期清理的巡检间隔小时数（2026-09-14 新增，默认 24 小时）
        /// 用途：原先清理只在程序启动时执行一次，程序长期不关闭则永远不会清理。
        ///       现由专职清理线程按本间隔周期性执行，保证长时间运行也能自动清理超期日志。
        ///       设为 0 表示关闭定期清理（仅保留启动时清理一次）。
        /// </summary>
        public const int LOG_CLEANUP_INTERVAL_HOURS = 24;

        /// <summary>
        /// 日志定期清理线程停止时等待唤醒的最长毫秒数（2026-09-14 新增）
        /// 用途：停止时立即唤醒清理线程，避免退出时白等一个完整巡检周期
        ///       （沿用看门狗 ManualResetEventSlim 唤醒方案的经验）
        /// </summary>
        public const int LOG_CLEANUP_STOP_TIMEOUT_MS = 3000;

        /// <summary>单实例互斥体名称</summary>
        public const string MUTEX_NAME = "TcpServer_MultiPort_Listener_SingleInstance_Mutex";

        /// <summary>业务配置结构版本（写入 json 便于后续升级兼容）</summary>
        public const string CONFIG_VERSION = "1.0";
    }
}
