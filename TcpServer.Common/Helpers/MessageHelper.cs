using System.Windows.Forms;

namespace TcpServer.Common.Helpers
{
    /// <summary>
    /// Message prompt helper - unifies the MessageBox style
    /// (convention: prompt title and icon must be consistent).
    /// </summary>
    public static class MessageHelper
    {
        /// <summary>
        /// Shows an information prompt.
        /// </summary>
        /// <param name="message">Prompt content.</param>
        public static void ShowInfo(string message)
        {
            MessageBox.Show(message ?? string.Empty, "Notice",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Shows a warning prompt.
        /// </summary>
        /// <param name="message">Warning content.</param>
        public static void ShowWarning(string message)
        {
            MessageBox.Show(message ?? string.Empty, "Warning",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// <summary>
        /// Shows an error prompt.
        /// </summary>
        /// <param name="message">Error content.</param>
        public static void ShowError(string message)
        {
            MessageBox.Show(message ?? string.Empty, "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// Shows a confirmation dialog.
        /// </summary>
        /// <param name="message">Confirmation content.</param>
        /// <returns>true if the user chooses "Yes", otherwise false.</returns>
        public static bool ShowConfirm(string message)
        {
            return MessageBox.Show(message ?? string.Empty, "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        /// <summary>
        /// Shows an operation-succeeded prompt.
        /// </summary>
        /// <param name="operation">Operation name.</param>
        public static void ShowSuccess(string operation = "Operation")
        {
            ShowInfo(string.Format("{0} succeeded!", operation));
        }

        /// <summary>
        /// Shows an operation-failed prompt.
        /// </summary>
        /// <param name="operation">Operation name.</param>
        public static void ShowFail(string operation = "Operation")
        {
            ShowError(string.Format("{0} failed. See the log for details!", operation));
        }
    }
}
