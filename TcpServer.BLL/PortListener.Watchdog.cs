using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using TcpServer.Common;
using TcpServer.Common.Helpers;
using TcpServer.Model;

namespace TcpServer.BLL
{
    /// <summary>
    /// Port listening service (watchdog and thread finalization part).
    /// 2026-09-14 split: with the watchdog added, the main PortListener body exceeded the convention red
    /// line of "a single class must not exceed 800 lines", so it was split into this partial file following
    /// the project's existing practice (see frmMain.cs / frmMain.Console.cs).
    /// Port start / stop, client admission and send / receive members live in PortListener.cs.
    /// </summary>
    public partial class PortListener
    {
        #region Private Methods - Thread Finalization

        /// <summary>
        /// Waits for a thread to exit (added 2026-09-14, unified exception and timeout handling).
        /// </summary>
        /// <param name="thread">Target thread; may be null.</param>
        /// <param name="timeoutMs">Maximum wait in milliseconds.</param>
        private void JoinThread(Thread thread, int timeoutMs)
        {
            if (thread == null) { return; }

            try
            {
                if (thread.IsAlive)
                {
                    thread.Join(timeoutMs);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Warn(string.Format("Port {0}: exception while waiting for thread {1} to exit: {2}",
                    Port, thread.Name, ex.Message));
            }
        }

        /// <summary>
        /// Wakes the watchdog thread (added 2026-09-14).
        /// Purpose: on Stop there is no need to wait out a full inspection interval (5 seconds by default);
        ///          the watchdog exits immediately, reducing "waste 1 second per port stopped" to milliseconds.
        /// Note: all exceptions are swallowed - even if the signal object has been disposed, the stop flow
        ///       must still run to completion, otherwise the port cannot be stopped cleanly.
        /// </summary>
        private void WakeupWatchdog()
        {
            try
            {
                _watchdogWakeup.Set();
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Warn(string.Format("Port {0}: exception while waking up the watchdog: {1}", Port, ex.Message));
            }
        }

        #endregion

        #region Private Methods - Listener Watchdog

        /// <summary>
        /// Starts the listener-thread watchdog (added 2026-09-14).
        /// Purpose: although the Accept loop was changed to continue after an exception, if the thread exits
        ///          for another unexpected reason (for example being terminated forcibly), the UI would still
        ///          show "Listening" while no connection could ever get in again, leaving a program restart as
        ///          the only fix. The watchdog inspects periodically and rebuilds the listener automatically
        ///          once the thread is found dead, turning "must restart the program" into self-healing.
        /// Note: every start increments the generation counter and passes it into the inspection loop, so an
        ///       old thread sees on waking that it is superseded and exits, avoiding two watchdogs coexisting
        ///       when a stop is immediately followed by a start.
        /// </summary>
        private void StartWatchdog()
        {
            int intervalMs = ConfigHelper.WatchdogIntervalMs;

            // A configured value of 0 disables the watchdog.
            if (intervalMs <= 0)
            {
                return;
            }

            try
            {
                int generation = Interlocked.Increment(ref _watchdogGeneration);

                // Added 2026-09-14: clear the wake-up signal left by the previous stop first, otherwise the
                // new watchdog thread would be "woken" the moment it starts and exit immediately.
                _watchdogWakeup.Reset();

                _watchdogRunning = true;

                _watchdogThread = new Thread(delegate() { WatchdogLoop(generation); });
                _watchdogThread.IsBackground = true;
                _watchdogThread.Name = string.Format("TcpWatchdog_{0}", Port);
                _watchdogThread.Start();
            }
            catch (Exception ex)
            {
                _watchdogRunning = false;
                LogHelper.Instance.Warn(string.Format("Port {0}: failed to start watchdog, listening not affected: {1}",
                    Port, ex.Message));
            }
        }

        /// <summary>
        /// Watchdog inspection loop (added 2026-09-14).
        /// </summary>
        /// <param name="generation">This thread's generation number; a mismatch with the current generation means
        ///                         it has been superseded and must exit.</param>
        private void WatchdogLoop(int generation)
        {
            int intervalMs = ConfigHelper.WatchdogIntervalMs;
            if (intervalMs <= 0) { intervalMs = AppConstants.WATCHDOG_INTERVAL_MS; }

            while (_watchdogRunning && generation == Volatile.Read(ref _watchdogGeneration))
            {
                bool wokenUp = false;

                // 2026-09-14 change: this was Thread.Sleep(intervalMs), so on Stop the Join could only
                // return after the full timeout (measured as roughly 1 wasted second per port).
                // It is now an interruptible wait: Set inside Stop makes it return true immediately.
                try
                {
                    wokenUp = _watchdogWakeup.Wait(intervalMs);
                }
                catch (Exception)
                {
                }

                // A stop wake-up was received, or the generation changed (stopped / restarted) - this thread
                // is void and exits at once.
                if (wokenUp || generation != Volatile.Read(ref _watchdogGeneration))
                {
                    break;
                }

                if (!_watchdogRunning || !_running || _disposed)
                {
                    continue;
                }

                // Listening but the Accept thread is gone - listening has failed and needs rebuilding.
                Thread acceptThread = _acceptThread;
                if (acceptThread != null && acceptThread.IsAlive)
                {
                    continue;
                }

                LogHelper.Instance.Warn(string.Format(
                    "Port {0}: watchdog detected that the listener thread exited, restoring listening now.", Port));

                RestartListener(generation);

                // After the rebuild this thread simply keeps inspecting; if it was stopped / restarted in the
                // meantime, the next generation check makes it exit.
            }
        }

        /// <summary>
        /// Rebuilds the listener (called by the watchdog, added 2026-09-14).
        /// </summary>
        /// <param name="generation">Caller generation number; the rebuild is abandoned on a mismatch with the
        ///                         current generation.</param>
        private void RestartListener(int generation)
        {
            try
            {
                // Release the old listener first and then rebind, avoiding a false fault of "occupied by itself".
                CleanupListener();

                // Stop / restart actions change the generation number; verify once more here to avoid creating
                // a duplicate listener when already stopped or already taken over by a new watchdog.
                if (generation != Volatile.Read(ref _watchdogGeneration)
                    || !_watchdogRunning || !_running || _disposed)
                {
                    return;
                }

                IPAddress address = IPAddress.Parse(_listenIp);
                _listener = new TcpListener(address, Port);
                _listener.Start();

                // Verify once more after a successful bind: if it was stopped during that window, reclaim the
                // handle that was just created.
                if (generation != Volatile.Read(ref _watchdogGeneration)
                    || !_watchdogRunning || !_running || _disposed)
                {
                    CleanupListener();
                    return;
                }

                _acceptThread = new Thread(AcceptLoop);
                _acceptThread.IsBackground = true;
                _acceptThread.Name = string.Format("TcpAccept_{0}", Port);
                _acceptThread.Start();

                SetState(PortState.Listening, "Listening restored automatically");
                LogHelper.Instance.Info(string.Format("Port {0}: listening restored by the watchdog.", Port));
            }
            catch (Exception ex)
            {
                SetState(PortState.Faulted, "Listening error and auto-restore failed. Please restart this port manually");
                LogHelper.Instance.Error(string.Format("Port {0}: failed to restore listening: {1}", Port, ex.Message), ex);
            }
        }

        #endregion
    }
}
