using System.Windows.Forms;

namespace TcpServer.Common.Helpers
{
    /// <summary>
    /// 消息提示帮助类 —— 统一 MessageBox 风格（规约要求：提示标题与图标一致）
    /// </summary>
    public static class MessageHelper
    {
        /// <summary>
        /// 显示信息提示
        /// </summary>
        /// <param name="message">提示内容</param>
        public static void ShowInfo(string message)
        {
            MessageBox.Show(message ?? string.Empty, "Notice",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// 显示警告提示
        /// </summary>
        /// <param name="message">警告内容</param>
        public static void ShowWarning(string message)
        {
            MessageBox.Show(message ?? string.Empty, "Warning",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// <summary>
        /// 显示错误提示
        /// </summary>
        /// <param name="message">错误内容</param>
        public static void ShowError(string message)
        {
            MessageBox.Show(message ?? string.Empty, "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        /// <param name="message">确认内容</param>
        /// <returns>用户选择"是"返回 true，否则返回 false</returns>
        public static bool ShowConfirm(string message)
        {
            return MessageBox.Show(message ?? string.Empty, "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        /// <summary>
        /// 显示操作成功提示
        /// </summary>
        /// <param name="operation">操作名称</param>
        public static void ShowSuccess(string operation = "Operation")
        {
            ShowInfo(string.Format("{0} succeeded!", operation));
        }

        /// <summary>
        /// 显示操作失败提示
        /// </summary>
        /// <param name="operation">操作名称</param>
        public static void ShowFail(string operation = "Operation")
        {
            ShowError(string.Format("{0} failed. See the log for details!", operation));
        }
    }
}
