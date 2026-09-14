using System;
using System.Windows.Forms;
using TcpServer.Common;
using TcpServer.Common.Helpers;

namespace TcpServer.UI.Forms
{
    /// <summary>
    /// 关于窗体 —— 展示版本号与关键路径（规约约定：主界面必须能确认版本）
    /// </summary>
    public partial class frmAbout : Form
    {
        // ============================================================
        // 1. 私有字段
        // ============================================================

        /// <summary>日志工具</summary>
        private readonly LogHelper _logger = LogHelper.Instance;

        // ============================================================
        // 2. 构造函数
        // ============================================================

        /// <summary>
        /// 构造函数
        /// </summary>
        public frmAbout()
        {
            InitializeComponent();
            InitializeCustomSettings();
        }

        // ============================================================
        // 3. 自定义初始化
        // ============================================================

        /// <summary>
        /// 自定义初始化 —— 填充版本与路径信息
        /// </summary>
        private void InitializeCustomSettings()
        {
            try
            {
                lblVersion.Text = string.Format("Version: v{0}", AppConstants.APP_VERSION);

                lblDescription.Text =
                    "Purpose: listen on multiple TCP ports at once and simulate a server receiving device data." + Environment.NewLine +
                    "Features: the port list and reply rules are saved automatically, so you do not need to re-enter them next time." + Environment.NewLine +
                    "Default start port " + AppConstants.DEFAULT_START_PORT +
                    ", default listen address " + AppConstants.DEFAULT_LISTEN_IP + "。";

                lblPaths.Text = string.Format(
                    "App directory: {0}{1}Config file: {2}{1}Log directory: {3}",
                    AppDomain.CurrentDomain.BaseDirectory,
                    Environment.NewLine,
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        AppConstants.CONFIG_FOLDER_NAME, AppConstants.CONFIG_FILE_NAME),
                    _logger.LogDirectory);

                btnClose.Click += btnClose_Click;
            }
            catch (Exception ex)
            {
                _logger.Error("About form initialization failed:" + ex.Message, ex);
            }
        }

        // ============================================================
        // 4. 事件处理
        // ============================================================

        /// <summary>
        /// 关闭窗体
        /// </summary>
        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
