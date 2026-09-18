using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace TcpServer.UI.Helpers
{
    /// <summary>
    /// General UI helper - consolidates repeated UI operation code from the forms.
    /// </summary>
    public static class UiHelper
    {
        /// <summary>
        /// Clamps a value into the valid range of a NumericUpDown.
        /// </summary>
        /// <param name="nud">Numeric control; returns the original value when null.</param>
        /// <param name="value">Target value.</param>
        /// <returns>A valid numeric value.</returns>
        public static decimal ClampValue(NumericUpDown nud, int value)
        {
            if (nud == null)
            {
                return value;
            }

            if (value < nud.Minimum) { return nud.Minimum; }
            if (value > nud.Maximum) { return nud.Maximum; }
            return value;
        }

        /// <summary>
        /// Opens the specified directory (creating it automatically when missing).
        /// </summary>
        /// <param name="path">Absolute directory path.</param>
        /// <param name="error">Failure reason.</param>
        /// <returns>true when it opened successfully.</returns>
        public static bool OpenDirectory(string path, out string error)
        {
            error = string.Empty;

            try
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    error = "Folder path is empty";
                    return false;
                }

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                Process.Start("explorer.exe", path);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Appends one line of text to a multi-line text box, capping the maximum number of retained lines.
        /// </summary>
        /// <param name="box">Target text box; returns immediately when null.</param>
        /// <param name="line">Text line.</param>
        /// <param name="maxLines">Maximum number of retained lines; values below 1 mean unlimited.</param>
        public static void AppendLine(RichTextBox box, string line, int maxLines)
        {
            if (box == null || string.IsNullOrEmpty(line))
            {
                return;
            }

            try
            {
                box.AppendText(line + Environment.NewLine);

                if (maxLines > 0 && box.Lines.Length > maxLines)
                {
                    int removeLines = box.Lines.Length - maxLines;
                    int charIndex = box.GetFirstCharIndexFromLine(removeLines);

                    if (charIndex > 0)
                    {
                        box.Select(0, charIndex);
                        box.SelectedText = string.Empty;
                    }
                }

                box.SelectionStart = box.TextLength;
                box.ScrollToCaret();
            }
            catch (Exception)
            {
                // A failed UI text append does not affect the business flow; ignore it silently.
            }
        }
    }
}
