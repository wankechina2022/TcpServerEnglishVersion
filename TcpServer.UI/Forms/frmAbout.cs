using System;
using System.Windows.Forms;
using TcpServer.Common;
using TcpServer.Common.Helpers;

namespace TcpServer.UI.Forms
{
    /// <summary>
    /// About form - shows the version number and key paths
    /// (convention: the main window must let the user confirm the version).
    /// </summary>
    public partial class frmAbout : Form
    {
        // ============================================================
        // 1. Private fields
        // ============================================================

        /// <summary>Logging utility.</summary>
        private readonly LogHelper _logger = LogHelper.Instance;

        // ============================================================
        // 2. Constructors
        // ============================================================

        /// <summary>
        /// Constructor.
        /// </summary>
        public frmAbout()
        {
            InitializeComponent();
            InitializeCustomSettings();
        }

        // ============================================================
        // 3. Custom initialization
        // ============================================================

        /// <summary>
        /// Custom initialization - fills in the version and path information.
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
                    ", default listen address " + AppConstants.DEFAULT_LISTEN_IP + ".";

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
        // 4. Event handlers
        // ============================================================

        /// <summary>
        /// Closes the form.
        /// </summary>
        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
