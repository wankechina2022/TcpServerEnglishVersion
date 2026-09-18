using System;
using System.Windows.Forms;
using TcpServer.Common;
using TcpServer.Common.Helpers;
using TcpServer.UI.Forms;

namespace TcpServer.UI
{
    /// <summary>
    /// Application entry point - handles global exception fallback, single-instance control and
    /// main window startup.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Application main entry point.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            // Convention: a windowed application may only be started once; a second launch is not allowed.
            using (SingleInstanceHelper instance = new SingleInstanceHelper())
            {
                if (!instance.IsFirstInstance)
                {
                    MessageBox.Show("The application is already running. Do not start it again!", "Notice",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Register the global exception fallback (convention: cover both UI threads and non-UI threads).
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += Application_ThreadException;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Clean up expired logs (convention: 6 months retained by default, expired files deleted automatically).
                // 2026-09-14 change: this only performs the "clean once at startup"; while the program stays up,
                // LogHelper's dedicated cleanup thread keeps cleaning according to App.config's
                // LogCleanupIntervalHours (24 hours by default).
                try
                {
                    int cleaned = LogHelper.Instance.CleanExpiredLogs(ConfigHelper.LogKeepMonths);

                    LogHelper.Instance.Info(string.Format(
                        "Application started, version v{0}. Startup cleanup removed {1} expired log file(s); periodic cleanup {2}.",
                        AppConstants.APP_VERSION,
                        cleaned,
                        LogHelper.Instance.CleanupRunning ? "enabled" : "disabled"));
                }
                catch (Exception ex)
                {
                    LogHelper.Instance.Warn("Failed to clean up logs at startup:" + ex.Message);
                }

                try
                {
                    Application.Run(new frmMain());
                }
                catch (Exception ex)
                {
                    LogHelper.Instance.Fatal("Exception in main form:" + ex.Message, ex);
                    MessageBox.Show("A fatal error occurred. The application will now exit.\nError:" + ex.Message,
                        "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    LogHelper.Instance.Info("Application exited.");
                }
            }
        }

        /// <summary>
        /// UI thread exception fallback.
        /// </summary>
        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            string message = e == null || e.Exception == null ? "Unknown exception" : e.Exception.Message;
            LogHelper.Instance.Error("UI thread exception:" + message, e == null ? null : e.Exception);

            MessageBox.Show("An error occurred. Please check the log and try again.\nError:" + message,
                "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// Non-UI thread exception fallback.
        /// </summary>
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e == null ? null : e.ExceptionObject as Exception;
            string message = ex == null ? "Unknown exception" : ex.Message;

            LogHelper.Instance.Fatal("Non-UI thread exception:" + message, ex);

            MessageBox.Show("A fatal system error occurred. The application will now exit.\nError:" + message,
                "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
