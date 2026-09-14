using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace TcpServer.UI.Helpers
{
    /// <summary>
    /// 界面通用帮助类 —— 收敛窗体中重复的界面操作代码
    /// </summary>
    public static class UiHelper
    {
        /// <summary>
        /// 将数值限制在 NumericUpDown 的合法区间内
        /// </summary>
        /// <param name="nud">数值控件，为 null 时原值返回</param>
        /// <param name="value">目标值</param>
        /// <returns>合法数值</returns>
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
        /// 打开指定目录（不存在时自动创建）
        /// </summary>
        /// <param name="path">目录绝对路径</param>
        /// <param name="error">失败原因</param>
        /// <returns>打开成功返回 true</returns>
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
        /// 向多行文本框追加一行文本，并控制最大保留行数
        /// </summary>
        /// <param name="box">目标文本框，为 null 时直接返回</param>
        /// <param name="line">文本行</param>
        /// <param name="maxLines">最大保留行数，小于 1 时不限制</param>
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
                // 界面文本追加失败不影响业务，静默忽略
            }
        }
    }
}
