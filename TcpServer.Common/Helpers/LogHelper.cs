using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace TcpServer.Common.Helpers
{
    /// <summary>
    /// Logging utility - supports file logging and console output.
    /// Convention: one log file per day; files are kept for 6 months by default and expired ones are deleted
    ///             automatically; every exception must be logged.
    ///
    /// 2026-09-14 rework notes (three points):
    /// 1) Previously every log line meant "open file -> write -> close file", and system logs shared a single
    ///    lock with port traffic logs; with 20 ports sending and receiving at high frequency, all receive
    ///    threads blocked serially on file IO. It now works as "business threads only enqueue; a dedicated
    ///    writer thread persists to disk", the file handle is held open during writes instead of being
    ///    repeatedly opened and closed, and receive threads no longer wait on the disk.
    /// 2) A single log file previously had no size cap and grew without limit under long, high-frequency
    ///    traffic; once the cap is exceeded it now rolls over automatically into xxx_1.log / xxx_2.log
    ///    volumes, while cleanup still follows the retention-month rule uniformly.
    /// 3) Cleanup previously ran only once at startup, so a program that stayed up for a long time (for
    ///    example a permanent on-site deployment) never cleaned anything; a dedicated cleanup thread now
    ///    runs periodically at LogCleanupIntervalHours (default 24 hours), and on stop a ManualResetEventSlim
    ///    wakes it immediately instead of sitting idle for a whole period.
    /// </summary>
    public sealed class LogHelper
    {
        #region Singleton and Fields

        private static readonly LogHelper _instance = new LogHelper();

        private readonly object _queueLock = new object();
        private readonly object _writerLock = new object();
        private readonly Queue<LogEntry> _queue = new Queue<LogEntry>();
        private readonly Dictionary<string, StreamWriter> _writers = new Dictionary<string, StreamWriter>();
        private readonly Thread _writerThread;

        /// <summary>Dedicated log cleanup thread (added 2026-09-14).</summary>
        private readonly Thread _cleanupThread;

        /// <summary>
        /// Cleanup thread wake-up signal (added 2026-09-14).
        /// Purpose: wake a sleeping cleanup thread immediately on stop, instead of waiting out a whole inspection period.
        /// </summary>
        private readonly ManualResetEventSlim _cleanupWakeup = new ManualResetEventSlim(false);

        private readonly string _logDirectory;

        private volatile bool _shutdownRequested;

        /// <summary>Running flag of the cleanup thread (added 2026-09-14).</summary>
        private volatile bool _cleanupRunning;

        /// <summary>Currently effective log retention in months (added 2026-09-14, reused by the cleanup thread).</summary>
        private int _cleanupKeepMonths;
        private bool _consoleOutputEnabled;

        private long _maxFileSize;
        private int _maxQueueLength;
        private long _droppedCount;

        /// <summary>Log level.</summary>
        public enum LogLevel
        {
            DEBUG = 0,
            INFO = 1,
            WARN = 2,
            ERROR = 3,
            FATAL = 4
        }

        /// <summary>
        /// A pending log entry (added 2026-09-14, internal use only).
        /// </summary>
        private sealed class LogEntry
        {
            /// <summary>Absolute path of the target file.</summary>
            public string FilePath;

            /// <summary>Log body.</summary>
            public string Content;
        }

        #endregion

        #region Constructor and Singleton

        /// <summary>
        /// Private constructor - creates the log directory and starts the dedicated writer thread.
        /// Note: App.config must not be read inside the constructor (a configuration read exception could
        ///       call back into this class while the singleton is not yet ready).
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
                // A failure to create the log directory must not affect the main flow;
                // silently degrade to console-only output.
                _consoleOutputEnabled = true;
            }

            _writerThread = new Thread(WriterLoop);
            _writerThread.IsBackground = true;
            _writerThread.Name = "LogWriter";
            _writerThread.Start();

            // Periodic cleanup thread (added 2026-09-14): exits after finishing, not permanent.
            // It only closes the gap of "clean once at startup, then never again"; an interval of 0 ends
            // this thread immediately.
            _cleanupThread = new Thread(CleanupLoop);
            _cleanupThread.IsBackground = true;
            _cleanupThread.Name = "LogCleaner";
            _cleanupThread.Start();

            // Flush the remaining queued logs on process exit so the last few entries are not lost
            // (added 2026-09-14).
            try
            {
                AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
                AppDomain.CurrentDomain.DomainUnload += OnProcessExit;
            }
            catch (Exception)
            {
                // A failure to register the event does not affect logging.
            }
        }

        /// <summary>
        /// Gets the logging utility singleton.
        /// </summary>
        public static LogHelper Instance
        {
            get { return _instance; }
        }

        /// <summary>
        /// Absolute path of the log directory.
        /// </summary>
        public string LogDirectory
        {
            get { return _logDirectory; }
        }

        /// <summary>
        /// Number of logs currently waiting to be persisted (added 2026-09-14, for diagnostics).
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
        /// Whether the periodic cleanup thread is running (added 2026-09-14, for diagnostics).
        /// Note: when the interval is configured as 0 this thread ends immediately, and this returns false.
        /// </summary>
        public bool CleanupRunning
        {
            get { return _cleanupRunning; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Writes a log entry.
        /// </summary>
        /// <param name="level">Log level.</param>
        /// <param name="message">Log content; null is allowed.</param>
        /// <param name="ex">Exception object; may be null.</param>
        public void Log(LogLevel level, string message, Exception ex = null)
        {
            string logContent = string.Format("[{0:yyyy-MM-dd HH:mm:ss.fff}] [{1}] {2}",
                DateTime.Now, level, message ?? string.Empty);

            if (ex != null)
            {
                logContent += string.Format("\r\n  Exception: {0}\r\n  Stack trace: {1}",
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
                    // A console output failure does not affect file logging.
                }
            }

            string fileName = string.Format("{0}_{1:yyyyMMdd}.log",
                level.ToString().ToLowerInvariant(), DateTime.Now);

            Enqueue(Path.Combine(_logDirectory, fileName), logContent + "\r\n\r\n");
        }

        /// <summary>Writes a DEBUG-level log entry.</summary>
        /// <param name="message">Log content.</param>
        public void Debug(string message) { Log(LogLevel.DEBUG, message); }

        /// <summary>Writes an INFO-level log entry.</summary>
        /// <param name="message">Log content.</param>
        public void Info(string message) { Log(LogLevel.INFO, message); }

        /// <summary>Writes a WARN-level log entry.</summary>
        /// <param name="message">Log content.</param>
        public void Warn(string message) { Log(LogLevel.WARN, message); }

        /// <summary>Writes an ERROR-level log entry.</summary>
        /// <param name="message">Log content.</param>
        /// <param name="ex">Exception object; may be null.</param>
        public void Error(string message, Exception ex = null) { Log(LogLevel.ERROR, message, ex); }

        /// <summary>Writes a FATAL-level log entry.</summary>
        /// <param name="message">Log content.</param>
        /// <param name="ex">Exception object; may be null.</param>
        public void Fatal(string message, Exception ex = null) { Log(LogLevel.FATAL, message, ex); }

        /// <summary>
        /// Writes a traffic log record for the specified port - each port gets its own file for easier
        /// troubleshooting.
        /// 2026-09-14 rework: switched to asynchronous enqueueing; the calling thread no longer waits on the disk.
        /// </summary>
        /// <param name="port">Port number.</param>
        /// <param name="content">Traffic content.</param>
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
                // A failure to write the traffic log does not affect the main send / receive flow.
            }
        }

        /// <summary>
        /// Gets today's traffic log file path for the specified port.
        /// </summary>
        /// <param name="port">Port number.</param>
        /// <returns>Absolute log file path.</returns>
        public string GetPortLogFilePath(int port)
        {
            string fileName = string.Format("Port_{0}_{1:yyyyMMdd}.log", port, DateTime.Now);
            return Path.Combine(_logDirectory, fileName);
        }

        /// <summary>
        /// Waits until all queued logs have been persisted (added 2026-09-14, intended for use before exit).
        /// </summary>
        /// <param name="timeoutMs">Maximum wait in milliseconds.</param>
        /// <returns>true when the queue has been drained.</returns>
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
        /// Stops the writer thread and persists the remaining logs
        /// (added 2026-09-14, called automatically on process exit).
        /// 2026-09-14 change: also wakes and terminates the periodic cleanup thread,
        /// so no background thread is left behind on exit.
        /// </summary>
        /// <param name="timeoutMs">Milliseconds to wait for the writer thread to end.</param>
        public void Shutdown(int timeoutMs = 3000)
        {
            try
            {
                lock (_queueLock)
                {
                    _shutdownRequested = true;
                    Monitor.PulseAll(_queueLock);
                }

                // Wake the cleanup thread first (otherwise it may still be waiting out a whole period).
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
                        // A failure to wait for the cleanup thread does not affect log persistence.
                    }
                }

                if (_writerThread != null && _writerThread.IsAlive)
                {
                    _writerThread.Join(timeoutMs < 0 ? 0 : timeoutMs);
                }
            }
            catch (Exception)
            {
                // No exceptions are thrown during the shutdown phase.
            }
        }

        /// <summary>
        /// Wakes the periodic cleanup thread immediately (added 2026-09-14).
        /// Note: all exceptions are swallowed so the stop flow always completes.
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
        /// Body of the periodic cleanup thread (added 2026-09-14).
        /// Note: this thread does not read configuration inside the constructor (reading App.config during
        ///       construction could call back into this class while the singleton is not yet ready); the
        ///       configuration is read on the first loop iteration, and an interval of 0 or a failed read
        ///       falls back to the default. The startup cleanup is invoked explicitly by Program.cs, so this
        ///       thread waits out a full period first and does not clean twice.
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
                    // Use the default when the configuration read fails; the loop continues regardless.
                }

                // 0 means the user disabled periodic cleanup; end this thread immediately.
                if (intervalHours <= 0) { return; }

                _cleanupKeepMonths = ConfigHelper.LogKeepMonths;

                int intervalMs = intervalHours * 60 * 60 * 1000;
                _cleanupRunning = true;

                while (!_shutdownRequested)
                {
                    // Use an interruptible wait instead of Sleep: Set() on stop returns immediately,
                    // rather than waiting out a whole period.
                    if (_cleanupWakeup.Wait(intervalMs)) { break; }

                    if (_shutdownRequested) { break; }

                    try
                    {
                        CleanExpiredLogs(_cleanupKeepMonths);
                    }
                    catch (Exception ex)
                    {
                        // A single failed cleanup round does not affect the next one.
                        Log(LogLevel.WARN, "Exception in periodic log cleanup:" + ex.Message);
                    }
                }
            }
            catch (Exception)
            {
                // No exception in the cleanup thread may affect the main flow.
            }
            finally
            {
                _cleanupRunning = false;
            }
        }

        /// <summary>
        /// Cleans up historical logs older than the retention limit (6 months by default).
        /// </summary>
        /// <param name="keepMonths">Retention in months; values below 1 use the default.</param>
        /// <returns>Number of files actually deleted.</returns>
        public int CleanExpiredLogs(int keepMonths = AppConstants.LOG_KEEP_MONTHS)
        {
            int deletedCount = 0;

            try
            {
                if (!Directory.Exists(_logDirectory)) { return 0; }

                // Cleanup may delete a volume whose file handle is still in use; flush and release all
                // write handles first to avoid a failed delete.
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
                        // A single failed file deletion does not abort the overall cleanup.
                        Log(LogLevel.WARN, string.Format("Failed to clean up historical log: {0}, reason: {1}", file, ex.Message));
                    }
                }

                if (deletedCount > 0)
                {
                    Log(LogLevel.INFO, string.Format("Cleaned up {0} historical log file(s) older than {1} month(s).", deletedCount, months));
                }
            }
            catch (Exception ex)
            {
                Log(LogLevel.WARN, "Exception during historical log cleanup:" + ex.Message, ex);
            }

            return deletedCount;
        }

        #endregion

        #region Private Methods - Enqueue

        /// <summary>
        /// Enqueues a log entry (added 2026-09-14) - a single locked enqueue only; the calling thread never
        /// touches the disk.
        /// </summary>
        /// <param name="filePath">Absolute path of the target file.</param>
        /// <param name="content">Log body.</param>
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
                    // Set the fallback value before reading configuration: even if reading the configuration
                    // throws internally and calls back into this class, the second entry finds this value > 0
                    // and no infinite recursion can occur.
                    _maxQueueLength = AppConstants.LOG_QUEUE_MAX_LENGTH;

                    int configured = ConfigHelper.LogQueueMaxLength;
                    if (configured > 0) { _maxQueueLength = configured; }
                }

                LogEntry entry = new LogEntry();
                entry.FilePath = filePath;
                entry.Content = content;

                lock (_queueLock)
                {
                    // When the queue is full, drop the oldest logs to keep memory from being blown up by logging.
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
                // A failed enqueue must not affect the main business flow.
            }
        }

        #endregion

        #region Private Methods - Writer Thread

        /// <summary>
        /// Dedicated log writer thread - loops over the queue and persists entries (added 2026-09-14).
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

            // Write out all remaining logs before closing.
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
        /// Persists a single log entry.
        /// </summary>
        /// <param name="entry">Log entry.</param>
        private void WriteEntry(LogEntry entry)
        {
            if (entry == null) { return; }

            try
            {
                // Write handles may be touched by other threads such as Flush / CleanExpiredLogs, so lock uniformly.
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
                // A failed file log write must never be thrown, to avoid affecting the business flow.
            }
        }

        /// <summary>
        /// Gets (or creates) the write handle for the specified log file - handles are reused, so files are
        /// no longer opened and closed per line.
        /// When the size cap is exceeded, the current file is rolled into a volume first and a new handle is created.
        /// </summary>
        /// <param name="filePath">Absolute log file path.</param>
        /// <returns>Write handle; null when it cannot be created.</returns>
        private StreamWriter GetWriter(string filePath)
        {
            if (_maxFileSize <= 0)
            {
                // As above: set the fallback first so a configuration read exception that calls back into this
                // class cannot cause recursion.
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

                    // Over the cap: roll into a new volume and rebuild the handle.
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
        /// Rolls a log file into a volume - abc.log -> abc_1.log / abc_2.log (added 2026-09-14).
        /// </summary>
        /// <param name="filePath">Absolute path of the current log file.</param>
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
                // When rolling fails, keep using the original file; log writing must not be affected.
            }
        }

        /// <summary>
        /// Flushes the buffers of all write handles to disk (handles are kept, not closed).
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
        /// Closes and clears all write handles.
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
        /// Process exit callback - persists the remaining logs (added 2026-09-14).
        /// </summary>
        private void OnProcessExit(object sender, EventArgs e)
        {
            Shutdown(2000);
        }

        #endregion
    }
}
