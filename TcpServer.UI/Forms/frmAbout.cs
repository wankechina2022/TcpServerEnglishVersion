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
                lblVersion.Text = string.Format("版本：v{0}", AppConstants.APP_VERSION);

                lblDescription.Text =
                    "用途：一次性监听多个 TCP 端口，模拟服务端接收设备数据。" + Environment.NewLine +
                    "特性：端口清单与应答规则自动保存，下次启动无需重新输入。" + Environment.NewLine +
                    "默认起始端口 " + AppConstants.DEFAULT_START_PORT +
                    "，默认监听地址 " + AppConstants.DEFAULT_LISTEN_IP + "。";

                lblPaths.Text = string.Format(
                    "程序目录：{0}{1}配置文件：{2}{1}日志目录：{3}",
                    AppDomain.CurrentDomain.BaseDirectory,
                    Environment.NewLine,
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        AppConstants.CONFIG_FOLDER_NAME, AppConstants.CONFIG_FILE_NAME),
                    _logger.LogDirectory);

                btnClose.Click += btnClose_Click;
            }
            catch (Exception ex)
            {
                _logger.Error("关于窗体初始化失败：" + ex.Message, ex);
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
