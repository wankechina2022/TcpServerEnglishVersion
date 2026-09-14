using System;
using System.Windows.Forms;
using TcpServer.Common;
using TcpServer.Common.Helpers;
using TcpServer.UI.Forms;

namespace TcpServer.UI
{
    /// <summary>
    /// 程序入口 —— 负责全局异常兜底、单实例控制与主窗体启动
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// 应用程序主入口点
        /// </summary>
        [STAThread]
        private static void Main()
        {
            // 规约约定：窗体程序只能启动一次，不能二次启动
            using (SingleInstanceHelper instance = new SingleInstanceHelper())
            {
                if (!instance.IsFirstInstance)
                {
                    MessageBox.Show("The application is already running. Do not start it again!", "Notice",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 注册全局异常兜底（规约：UI 线程与非 UI 线程都要兜底）
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += Application_ThreadException;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // 清理超期日志（规约：默认保留 6 个月，超期自动删除）
                // 2026-09-14 修改：这里只是"启动时清一次"；程序长期不关闭时由 LogHelper 的
                // 专职清理线程按 App.config 的 LogCleanupIntervalHours（默认 24 小时）继续清理。
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
        /// UI 线程异常兜底
        /// </summary>
        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            string message = e == null || e.Exception == null ? "Unknown exception" : e.Exception.Message;
            LogHelper.Instance.Error("UI thread exception:" + message, e == null ? null : e.Exception);

            MessageBox.Show("An error occurred. Please check the log and try again.\nError:" + message,
                "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// 非 UI 线程异常兜底
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
