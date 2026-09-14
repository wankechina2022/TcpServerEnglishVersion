using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace TcpServer.Common.Helpers
{
    /// <summary>
    /// 日志工具类 —— 支持文件日志与控制台输出
    /// 规约要求：每天一个日志文件；默认保留 6 个月，超期自动删除；所有异常必须记录日志
    ///
    /// 2026-09-14 改造说明（三处）：
    /// 1) 原先每写一行日志都要"开文件→写→关文件"，且系统日志与端口流水共用一把锁；
    ///    20 个端口同时高频收发时，接收线程会全部串行阻塞在文件 IO 上。
    ///    现改为"业务线程只入队、专职写线程负责落盘"，落盘期间持有文件句柄不再反复开关，
    ///    接收线程不再等待磁盘。
    /// 2) 单个日志文件原先没有大小上限，长时间高频收发会无限增长；
    ///    现超过上限后自动滚动为 xxx_1.log、xxx_2.log 分卷，清理规则仍按保留月数统一执行。
    /// 3) 清理原先只在程序启动时执行一次，程序长期不关闭（如常驻现场）则永远不会清理；
    ///    现增加专职清理线程按 LogCleanupIntervalHours 周期执行（默认 24 小时），
    ///    停止时用 ManualResetEventSlim 立即唤醒，不白等一个完整周期。
    /// </summary>
    public sealed class LogHelper
    {
        #region 单例与字段

        private static readonly LogHelper _instance = new LogHelper();

        private readonly object _queueLock = new object();
        private readonly object _writerLock = new object();
        private readonly Queue<LogEntry> _queue = new Queue<LogEntry>();
        private readonly Dictionary<string, StreamWriter> _writers = new Dictionary<string, StreamWriter>();
        private readonly Thread _writerThread;

        /// <summary>专职日志清理线程（2026-09-14 新增）</summary>
        private readonly Thread _cleanupThread;

        /// <summary>
        /// 清理线程唤醒信号（2026-09-14 新增）
        /// 用途：停止时立即唤醒休眠中的清理线程，避免白等一个完整巡检周期
        /// </summary>
        private readonly ManualResetEventSlim _cleanupWakeup = new ManualResetEventSlim(false);

        private readonly string _logDirectory;

        private volatile bool _shutdownRequested;

        /// <summary>清理线程的运行标志（2026-09-14 新增）</summary>
        private volatile bool _cleanupRunning;

        /// <summary>当前生效的日志保留月数（2026-09-14 新增，供清理线程复用）</summary>
        private int _cleanupKeepMonths;
        private bool _consoleOutputEnabled;

        private long _maxFileSize;
        private int _maxQueueLength;
        private long _droppedCount;

        /// <summary>日志级别</summary>
        public enum LogLevel
        {
            DEBUG = 0,
            INFO = 1,
            WARN = 2,
            ERROR = 3,
            FATAL = 4
        }

        /// <summary>
        /// 待写日志条目（2026-09-14 新增，仅内部使用）
        /// </summary>
        private sealed class LogEntry
        {
            /// <summary>目标文件绝对路径</summary>
            public string FilePath;

            /// <summary>日志正文</summary>
            public string Content;
        }

        #endregion

        #region 构造与单例

        /// <summary>
        /// 私有构造函数 —— 创建日志目录并启动专职写线程
        /// 注意：构造函数内不得读取 App.config（避免配置读取异常时回调本类导致单例尚未就绪）
        /// </summary>
        private LogHelper()
        {
            _consoleOutputEnabled = false;
            _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppConstants.LOG_FOLDER_NAME);

            _shutdownRequested = false;
            _maxFileSize = 0L;
            _maxQueueLength = 0;
            _droppedCount = 0L;
            _cleanupRunning = false;
            _cleanupKeepMonths = AppConstants.LOG_KEEP_MONTHS;

            try
            {
                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                }
            }
            catch (Exception)
            {
                // 日志目录创建失败时不允许影响主流程，静默降级为仅控制台输出
                _consoleOutputEnabled = true;
            }

            _writerThread = new Thread(WriterLoop);
            _writerThread.IsBackground = true;
            _writerThread.Name = "LogWriter";
            _writerThread.Start();

            // 定期清理线程（2026-09-14 新增）：跑完就退出，不常驻。
            // 只解决"启动时清一次、之后永不再清"的缺口；间隔为 0 时本线程立刻结束。
            _cleanupThread = new Thread(CleanupLoop);
            _cleanupThread.IsBackground = true;
            _cleanupThread.Name = "LogCleaner";
            _cleanupThread.Start();

            // 进程退出时把队列里剩余的日志落盘，避免最后几条丢失（2026-09-14 新增）
            try
            {
                AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
                AppDomain.CurrentDomain.DomainUnload += OnProcessExit;
            }
            catch (Exception)
            {
                // 事件注册失败不影响日志功能
            }
        }

        /// <summary>
        /// 获取日志工具单例
        /// </summary>
        public static LogHelper Instance
        {
            get { return _instance; }
        }

        /// <summary>
        /// 日志目录绝对路径
        /// </summary>
        public string LogDirectory
        {
            get { return _logDirectory; }
        }

        /// <summary>
        /// 当前待落盘的日志条数（2026-09-14 新增，供诊断用）
        /// </summary>
        public int PendingCount
        {
            get
            {
                lock (_queueLock)
                {
                    return _queue.Count;
                }
            }
        }

        /// <summary>
        /// 定期清理线程是否正在运行（2026-09-14 新增，供诊断用）
        /// 说明：间隔配置为 0 时该线程会立即结束，此处返回 false
        /// </summary>
        public bool CleanupRunning
        {
            get { return _cleanupRunning; }
        }

        #endregion

        #region 对外方法

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="message">日志内容，允许为 null</param>
        /// <param name="ex">异常对象，可为 null</param>
        public void Log(LogLevel level, string message, Exception ex = null)
        {
            string logContent = string.Format("[{0:yyyy-MM-dd HH:mm:ss.fff}] [{1}] {2}",
                DateTime.Now, level, message ?? string.Empty);

            if (ex != null)
            {
                logContent += string.Format("\r\n  异常信息：{0}\r\n  堆栈跟踪：{1}",
                    ex.Message, ex.StackTrace);
            }

            if (_consoleOutputEnabled)
            {
                try
                {
                    Console.WriteLine(logContent);
                }
                catch (Exception)
                {
                    // 控制台输出失败不影响文件日志
                }
            }

            string fileName = string.Format("{0}_{1:yyyyMMdd}.log",
                level.ToString().ToLowerInvariant(), DateTime.Now);

            Enqueue(Path.Combine(_logDirectory, fileName), logContent + "\r\n\r\n");
        }

        /// <summary>记录 DEBUG 级别日志</summary>
        /// <param name="message">日志内容</param>
        public void Debug(string message) { Log(LogLevel.DEBUG, message); }

        /// <summary>记录 INFO 级别日志</summary>
        /// <param name="message">日志内容</param>
        public void Info(string message) { Log(LogLevel.INFO, message); }

        /// <summary>记录 WARN 级别日志</summary>
        /// <param name="message">日志内容</param>
        public void Warn(string message) { Log(LogLevel.WARN, message); }

        /// <summary>记录 ERROR 级别日志</summary>
        /// <param name="message">日志内容</param>
        /// <param name="ex">异常对象，可为 null</param>
        public void Error(string message, Exception ex = null) { Log(LogLevel.ERROR, message, ex); }

        /// <summary>记录 FATAL 级别日志</summary>
        /// <param name="message">日志内容</param>
        /// <param name="ex">异常对象，可为 null</param>
        public void Fatal(string message, Exception ex = null) { Log(LogLevel.FATAL, message, ex); }

        /// <summary>
        /// 按指定端口写一份收发流水日志 —— 每个端口独立文件，便于单独排查
        /// 2026-09-14 改造：改为异步入队，调用线程不再等待磁盘写入
        /// </summary>
        /// <param name="port">端口号</param>
        /// <param name="content">流水内容</param>
        public void WritePortLog(int port, string content)
        {
            if (string.IsNullOrEmpty(content)) { return; }

            try
            {
                string line = string.Format("[{0:yyyy-MM-dd HH:mm:ss.fff}] {1}\r\n", DateTime.Now, content);
                Enqueue(GetPortLogFilePath(port), line);
            }
            catch (Exception)
            {
                // 流水日志写入失败不影响收发主流程
            }
        }

        /// <summary>
        /// 获取指定端口当天的流水日志文件路径
        /// </summary>
        /// <param name="port">端口号</param>
        /// <returns>日志文件绝对路径</returns>
        public string GetPortLogFilePath(int port)
        {
            string fileName = string.Format("Port_{0}_{1:yyyyMMdd}.log", port, DateTime.Now);
            return Path.Combine(_logDirectory, fileName);
        }

        /// <summary>
        /// 等待队列中的日志全部落盘（2026-09-14 新增，供退出前调用）
        /// </summary>
        /// <param name="timeoutMs">最长等待毫秒数</param>
        /// <returns>队列已清空返回 true</returns>
        public bool Flush(int timeoutMs = 3000)
        {
            try
            {
                DateTime deadline = DateTime.Now.AddMilliseconds(timeoutMs < 0 ? 0 : timeoutMs);

                while (PendingCount > 0)
                {
                    if (DateTime.Now > deadline) { return false; }
                    Thread.Sleep(20);
                }

                FlushWriters();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 停止写线程并落盘剩余日志（2026-09-14 新增，进程退出时自动调用）
        /// 2026-09-14 修改：同时唤醒并结束定期清理线程，避免退出时残留后台线程
        /// </summary>
        /// <param name="timeoutMs">等待写线程结束的毫秒数</param>
        public void Shutdown(int timeoutMs = 3000)
        {
            try
            {
                lock (_queueLock)
                {
                    _shutdownRequested = true;
                    Monitor.PulseAll(_queueLock);
                }

                // 先唤醒清理线程（否则它可能还在 Wait 一个完整周期）
                WakeupCleanup();

                if (_cleanupThread != null && _cleanupThread.IsAlive
                    && _cleanupThread != Thread.CurrentThread)
                {
                    try
                    {
                        _cleanupThread.Join(AppConstants.LOG_CLEANUP_STOP_TIMEOUT_MS);
                    }
                    catch (Exception)
                    {
                        // 等待清理线程结束失败不影响日志落盘
                    }
                }

                if (_writerThread != null && _writerThread.IsAlive)
                {
                    _writerThread.Join(timeoutMs < 0 ? 0 : timeoutMs);
                }
            }
            catch (Exception)
            {
                // 退出阶段不再抛异常
            }
        }

        /// <summary>
        /// 立即唤醒定期清理线程（2026-09-14 新增）
        /// 说明：异常一律吞掉，保证停止流程能完整走完
        /// </summary>
        private void WakeupCleanup()
        {
            try
            {
                _cleanupWakeup.Set();
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 定期清理线程主体（2026-09-14 新增）
        /// 说明：本线程不在构造函数内读配置（构造期读 App.config 可能导致单例尚未就绪时回调本类），
        ///       配置在首次循环内读取；间隔为 0 或读配置失败时按默认值处理。
        ///       启动时的那次清理已由 Program.cs 显式调用，故本线程首轮先等满一个周期，不重复清。
        /// </summary>
        private void CleanupLoop()
        {
            try
            {
                int intervalHours = AppConstants.LOG_CLEANUP_INTERVAL_HOURS;

                try
                {
                    intervalHours = ConfigHelper.LogCleanupIntervalHours;
                }
                catch (Exception)
                {
                    // 读配置失败时用默认值，不影响后续循环
                }

                // 0 表示用户关闭了定期清理，直接结束本线程
                if (intervalHours <= 0) { return; }

                _cleanupKeepMonths = ConfigHelper.LogKeepMonths;

                int intervalMs = intervalHours * 60 * 60 * 1000;
                _cleanupRunning = true;

                while (!_shutdownRequested)
                {
                    // 用可唤醒的等待代替 Sleep：停止时 Set() 立即返回，不白等一个完整周期
                    if (_cleanupWakeup.Wait(intervalMs)) { break; }

                    if (_shutdownRequested) { break; }

                    try
                    {
                        CleanExpiredLogs(_cleanupKeepMonths);
                    }
                    catch (Exception ex)
                    {
                        // 单轮清理失败不影响下一轮
                        Log(LogLevel.WARN, "定期清理日志异常：" + ex.Message);
                    }
                }
            }
            catch (Exception)
            {
                // 清理线程任何异常都不允许影响主流程
            }
            finally
            {
                _cleanupRunning = false;
            }
        }

        /// <summary>
        /// 清理超过保留期限的历史日志（默认 6 个月）
        /// </summary>
        /// <param name="keepMonths">保留月数，小于 1 时使用默认值</param>
        /// <returns>实际删除的文件数量</returns>
        public int CleanExpiredLogs(int keepMonths = AppConstants.LOG_KEEP_MONTHS)
        {
            int deletedCount = 0;

            try
            {
                if (!Directory.Exists(_logDirectory)) { return 0; }

                // 清理会删除文件句柄仍在使用的分卷？此处先落盘并释放全部写句柄，避免删除失败
                FlushWriters();

                int months = keepMonths < 1 ? AppConstants.LOG_KEEP_MONTHS : keepMonths;
                DateTime expireTime = DateTime.Now.AddMonths(-months);

                string[] files = Directory.GetFiles(_logDirectory, "*.log");
                foreach (string file in files)
                {
                    try
                    {
                        FileInfo info = new FileInfo(file);
                        if (info.LastWriteTime < expireTime)
                        {
                            File.Delete(file);
                            deletedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        // 单个文件删除失败不中断整体清理
                        Log(LogLevel.WARN, string.Format("清理历史日志失败：{0}，原因：{1}", file, ex.Message));
                    }
                }

                if (deletedCount > 0)
                {
                    Log(LogLevel.INFO, string.Format("已清理 {0} 个超过 {1} 个月的历史日志文件。", deletedCount, months));
                }
            }
            catch (Exception ex)
            {
                Log(LogLevel.WARN, "清理历史日志过程发生异常：" + ex.Message, ex);
            }

            return deletedCount;
        }

        #endregion

        #region 私有方法 —— 入队

        /// <summary>
        /// 日志入队（2026-09-14 新增）—— 仅做一次加锁入队，调用线程不碰磁盘
        /// </summary>
        /// <param name="filePath">目标文件绝对路径</param>
        /// <param name="content">日志正文</param>
        private void Enqueue(string filePath, string content)
        {
            if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrEmpty(content))
            {
                return;
            }

            try
            {
                if (_maxQueueLength <= 0)
                {
                    // 先落兜底值再读配置：即便配置读取过程内部发生异常并回调本类，
                    // 第二次进入时该值已 > 0，不会形成无限递归
                    _maxQueueLength = AppConstants.LOG_QUEUE_MAX_LENGTH;

                    int configured = ConfigHelper.LogQueueMaxLength;
                    if (configured > 0) { _maxQueueLength = configured; }
                }

                LogEntry entry = new LogEntry();
                entry.FilePath = filePath;
                entry.Content = content;

                lock (_queueLock)
                {
                    // 队列满时丢弃最旧的日志，保护内存不被日志撑爆
                    while (_queue.Count >= _maxQueueLength)
                    {
                        _queue.Dequeue();
                        _droppedCount++;
                    }

                    _queue.Enqueue(entry);
                    Monitor.Pulse(_queueLock);
                }
            }
            catch (Exception)
            {
                // 入队失败不允许影响业务主流程
            }
        }

        #endregion

        #region 私有方法 —— 写线程

        /// <summary>
        /// 专职写日志线程 —— 循环取队列并落盘（2026-09-14 新增）
        /// </summary>
        private void WriterLoop()
        {
            while (true)
            {
                LogEntry entry = null;

                lock (_queueLock)
                {
                    if (_queue.Count > 0)
                    {
                        entry = _queue.Dequeue();
                    }
                    else if (_shutdownRequested)
                    {
                        break;
                    }
                    else
                    {
                        Monitor.Wait(_queueLock, 200);
                    }
                }

                if (entry != null)
                {
                    WriteEntry(entry);
                }
            }

            // 关闭前把剩余日志全部写完
            while (true)
            {
                LogEntry left = null;

                lock (_queueLock)
                {
                    if (_queue.Count > 0) { left = _queue.Dequeue(); }
                }

                if (left == null) { break; }
                WriteEntry(left);
            }

            CloseAllWriters();
        }

        /// <summary>
        /// 单条日志落盘
        /// </summary>
        /// <param name="entry">日志条目</param>
        private void WriteEntry(LogEntry entry)
        {
            if (entry == null) { return; }

            try
            {
                // 写句柄可能被 Flush/CleanExpiredLogs 等其它线程访问，故统一加锁
                lock (_writerLock)
                {
                    StreamWriter writer = GetWriter(entry.FilePath);
                    if (writer == null) { return; }

                    writer.WriteLine(entry.Content);
                    writer.Flush();
                }
            }
            catch (Exception)
            {
                // 文件日志写入失败时不允许抛出，避免影响业务
            }
        }

        /// <summary>
        /// 获取（或创建）指定日志文件的写句柄 —— 句柄复用，不再每行开关文件
        /// 超过大小上限时先把当前文件滚动分卷，再新建句柄
        /// </summary>
        /// <param name="filePath">日志文件绝对路径</param>
        /// <returns>写句柄；无法创建时返回 null</returns>
        private StreamWriter GetWriter(string filePath)
        {
            if (_maxFileSize <= 0)
            {
                // 同上：先落兜底值，避免配置读取异常回调本类导致递归
                _maxFileSize = AppConstants.LOG_MAX_FILE_SIZE;

                long configured = ConfigHelper.LogMaxFileSize;
                if (configured > 0) { _maxFileSize = configured; }
            }

            StreamWriter writer;

            if (_writers.TryGetValue(filePath, out writer) && writer != null)
            {
                try
                {
                    if (writer.BaseStream.Length < _maxFileSize)
                    {
                        return writer;
                    }

                    // 超限：滚动分卷后重建句柄
                    writer.Flush();
                    writer.Dispose();
                }
                catch (Exception)
                {
                    try { writer.Dispose(); }
                    catch (Exception) { }
                }

                _writers.Remove(filePath);
                RollFile(filePath);
            }

            try
            {
                string directory = Path.GetDirectoryName(filePath);
                if (!FileHelper.EnsureDirectory(directory))
                {
                    return null;
                }

                FileStream stream = new FileStream(filePath, FileMode.Append, FileAccess.Write,
                    FileShare.ReadWrite);

                writer = new StreamWriter(stream, new UTF8Encoding(false));
                writer.AutoFlush = false;

                _writers[filePath] = writer;
                return writer;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 日志文件滚动分卷 —— abc.log → abc_1.log / abc_2.log（2026-09-14 新增）
        /// </summary>
        /// <param name="filePath">当前日志文件绝对路径</param>
        private void RollFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) { return; }

                string directory = Path.GetDirectoryName(filePath) ?? string.Empty;
                string baseName = Path.GetFileNameWithoutExtension(filePath);
                string extension = Path.GetExtension(filePath);

                int index = 1;
                string target;

                do
                {
                    target = Path.Combine(directory,
                        string.Format("{0}_{1}{2}", baseName, index, extension));
                    index++;
                }
                while (File.Exists(target));

                File.Move(filePath, target);
            }
            catch (Exception)
            {
                // 分卷失败时继续使用原文件，不允许影响日志写入
            }
        }

        /// <summary>
        /// 把全部写句柄的缓冲刷到磁盘（保留句柄，不关闭）
        /// </summary>
        private void FlushWriters()
        {
            lock (_writerLock)
            {
                foreach (StreamWriter writer in _writers.Values)
                {
                    if (writer == null) { continue; }

                    try { writer.Flush(); }
                    catch (Exception) { }
                }
            }
        }

        /// <summary>
        /// 关闭并清空全部写句柄
        /// </summary>
        private void CloseAllWriters()
        {
            List<StreamWriter> snapshot;

            lock (_writerLock)
            {
                snapshot = new List<StreamWriter>(_writers.Values);
                _writers.Clear();
            }

            foreach (StreamWriter writer in snapshot)
            {
                if (writer == null) { continue; }

                try
                {
                    writer.Flush();
                    writer.Dispose();
                }
                catch (Exception) { }
            }
        }

        /// <summary>
        /// 进程退出回调 —— 落盘剩余日志（2026-09-14 新增）
        /// </summary>
        private void OnProcessExit(object sender, EventArgs e)
        {
            Shutdown(2000);
        }

        #endregion
    }
}
