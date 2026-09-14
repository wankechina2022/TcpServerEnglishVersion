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
                    MessageBox.Show("程序已在运行中，请勿重复启动！", "提示",
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
                        "程序启动，版本 v{0}。启动清理删除 {1} 个超期日志文件；定期清理{2}。",
                        AppConstants.APP_VERSION,
                        cleaned,
                        LogHelper.Instance.CleanupRunning ? "已启用" : "未启用"));
                }
                catch (Exception ex)
                {
                    LogHelper.Instance.Warn("启动清理日志失败：" + ex.Message);
                }

                try
                {
                    Application.Run(new frmMain());
                }
                catch (Exception ex)
                {
                    LogHelper.Instance.Fatal("主窗体运行异常：" + ex.Message, ex);
                    MessageBox.Show("程序发生严重异常，即将退出。\n错误信息：" + ex.Message,
                        "严重错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    LogHelper.Instance.Info("程序已退出。");
                }
            }
        }

        /// <summary>
        /// UI 线程异常兜底
        /// </summary>
        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            string message = e == null || e.Exception == null ? "未知异常" : e.Exception.Message;
            LogHelper.Instance.Error("UI 线程异常：" + message, e == null ? null : e.Exception);

            MessageBox.Show("系统发生异常，请查看日志后重试。\n错误信息：" + message,
                "系统错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// 非 UI 线程异常兜底
        /// </summary>
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e == null ? null : e.ExceptionObject as Exception;
            string message = ex == null ? "未知异常" : ex.Message;

            LogHelper.Instance.Fatal("非 UI 线程异常：" + message, ex);

            MessageBox.Show("系统发生严重异常，即将退出。\n错误信息：" + message,
                "严重错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
