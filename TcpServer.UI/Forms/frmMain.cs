using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using TcpServer.BLL;
using TcpServer.Common;
using TcpServer.Common.Helpers;
using TcpServer.Model;
using TcpServer.UI.Helpers;

namespace TcpServer.UI.Forms
{
    /// <summary>
    /// Main window - the console UI for multi-port TCP listening (port management, start / stop
    /// control, configuration persistence).
    /// Members related to the data console live in frmMain.Console.cs.
    /// </summary>
    public partial class frmMain : Form
    {
        // ============================================================
        // 1. Private fields (grouped by function)
        // ============================================================

        /// <summary>Logging utility.</summary>
        private readonly LogHelper _logger = LogHelper.Instance;

        /// <summary>Configuration business object.</summary>
        private readonly ConfigBLL _configBll = new ConfigBLL();

        /// <summary>Multi-port listener manager.</summary>
        private TcpServerManager _serverManager;

        /// <summary>Current configuration.</summary>
        private AppConfigModel _config;

        /// <summary>Operation-in-progress flag - prevents buttons from being clicked repeatedly.</summary>
        private bool _isBusy;

        // ============================================================
        // 2. Constructor
        // ============================================================

        /// <summary>
        /// Constructor.
        /// </summary>
        public frmMain()
        {
            InitializeComponent();
            InitializeCustomSettings();
        }

        // ============================================================
        // 3. Custom initialization methods
        // ============================================================

        /// <summary>
        /// Custom initialization - binds events, loads the configuration and prepares the listener manager.
        /// </summary>
        private void InitializeCustomSettings()
        {
            try
            {
                _isBusy = false;
                ResetConsoleSelection();

                Text = string.Format("Multi-Port TCP Listener Debugger  v{0}", AppConstants.APP_VERSION);
                lblVersion.Text = string.Format("Version v{0}", AppConstants.APP_VERSION);

                BindEvents();
                InitializeListenIpComboBox();

                _serverManager = new TcpServerManager(ConfigHelper.DefaultListenIp, null);
                _serverManager.PortStateChanged += OnPortStateChanged;
                _serverManager.ClientChanged += OnClientChanged;
                _serverManager.DataReceived += OnDataReceived;
                _serverManager.DataSent += OnDataSent;

                LoadConfigToUi();

                lblConfigPath.Text = "Config file: " + _configBll.ConfigFilePath;

                tmrRefresh.Start();

                _logger.Info("Main UI initialized.");

                if (chkAutoStart.Checked)
                {
                    StartAllPorts(true);
                }
            }
            catch (Exception ex)
            {
                _logger.Fatal("Main UI initialization failed:" + ex.Message, ex);
                MessageHelper.ShowError("UI initialization failed. See the log for details!\n" + ex.Message);
            }
        }

        /// <summary>
        /// Binds the control events.
        /// </summary>
        private void BindEvents()
        {
            btnGenerate.Click += btnGenerate_Click;
            btnStartAll.Click += btnStartAll_Click;
            btnStopAll.Click += btnStopAll_Click;
            btnSaveConfig.Click += btnSaveConfig_Click;
            btnOpenLog.Click += btnOpenLog_Click;

            btnSend.Click += btnSend_Click;
            btnSendAll.Click += btnSendAll_Click;
            btnClearData.Click += btnClearData_Click;

            dgvPorts.SelectionChanged += dgvPorts_SelectionChanged;
            dgvPorts.CellDoubleClick += dgvPorts_CellDoubleClick;

            cboClient.SelectedIndexChanged += cboClient_SelectedIndexChanged;
            cboListenIp.SelectedIndexChanged += cboListenIp_SelectedIndexChanged;

            tmrRefresh.Tick += tmrRefresh_Tick;

            mnuSaveConfig.Click += btnSaveConfig_Click;
            mnuReloadConfig.Click += mnuReloadConfig_Click;
            mnuOpenConfigDir.Click += mnuOpenConfigDir_Click;
            mnuOpenLogDir.Click += btnOpenLog_Click;
            mnuExit.Click += mnuExit_Click;

            mnuManageRule.Click += mnuManageRule_Click;
            mnuAbout.Click += mnuAbout_Click;

            FormClosing += frmMain_FormClosing;
        }

        /// <summary>
        /// Initializes the listen-address drop-down.
        /// </summary>
        private void InitializeListenIpComboBox()
        {
            cboListenIp.Items.Clear();

            List<string> addresses = NetHelper.GetListenAddressList();
            foreach (string ip in addresses)
            {
                cboListenIp.Items.Add(ip);
            }

            if (cboListenIp.Items.Count > 0)
            {
                cboListenIp.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Loads the configuration into the UI.
        /// </summary>
        private void LoadConfigToUi()
        {
            _config = _configBll.LoadConfig();

            if (_config == null)
            {
                _config = new AppConfigModel();
            }

            // Listen address: if the configured value is not in the drop-down yet, append it.
            if (!ValidationHelper.IsNullOrWhiteSpace(_config.ListenIp))
            {
                if (!cboListenIp.Items.Contains(_config.ListenIp))
                {
                    cboListenIp.Items.Add(_config.ListenIp);
                }

                cboListenIp.SelectedItem = _config.ListenIp;
            }

            nudStartPort.Value = UiHelper.ClampValue(nudStartPort, _config.StartPort);
            nudPortCount.Value = UiHelper.ClampValue(nudPortCount, _config.PortCount);
            chkAutoStart.Checked = _config.AutoStartOnLaunch;
            chkHexView.Checked = _config.DisplayAsHex;

            _serverManager.SetListenIp(_config.ListenIp);
            _serverManager.SetPortConfigs(_config.Ports);
            _serverManager.SetRules(_config.Rules);

            RebuildPortGrid(_config.Ports);
        }

        // ============================================================
        // 4. Event handlers
        // ============================================================

        /// <summary>
        /// Generates the port list.
        /// </summary>
        private void btnGenerate_Click(object sender, EventArgs e)
        {
            GeneratePortList();
        }

        /// <summary>
        /// Starts all ports.
        /// </summary>
        private void btnStartAll_Click(object sender, EventArgs e)
        {
            StartAllPorts(false);
        }

        /// <summary>
        /// Stops all ports.
        /// </summary>
        private void btnStopAll_Click(object sender, EventArgs e)
        {
            StopAllPorts();
        }

        /// <summary>
        /// Saves the configuration.
        /// </summary>
        private void btnSaveConfig_Click(object sender, EventArgs e)
        {
            SaveConfigToFile(true);
        }

        /// <summary>
        /// Opens the log folder.
        /// </summary>
        private void btnOpenLog_Click(object sender, EventArgs e)
        {
            OpenDirectory(_logger.LogDirectory, "Log Folder");
        }

        /// <summary>
        /// Selected row in the port grid changed.
        /// </summary>
        private void dgvPorts_SelectionChanged(object sender, EventArgs e)
        {
            UpdateCurrentPortFromGrid();
        }

        /// <summary>
        /// Double-click on the port grid - toggles start / stop for that port.
        /// </summary>
        private void dgvPorts_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e == null || e.RowIndex < 0) { return; }

            TogglePortByRow(e.RowIndex);
        }

        /// <summary>
        /// Listen address changed - switching is only allowed while nothing is listening.
        /// </summary>
        private void cboListenIp_SelectedIndexChanged(object sender, EventArgs e)
        {
            string ip = cboListenIp.SelectedItem == null ? string.Empty : cboListenIp.SelectedItem.ToString();
            if (ValidationHelper.IsNullOrWhiteSpace(ip)) { return; }
            if (_serverManager == null) { return; }

            if (_serverManager.ListeningCount > 0)
            {
                MessageHelper.ShowWarning("Some ports are still listening. Stop all ports before changing the listen address!");
                return;
            }

            _serverManager.SetListenIp(ip);
        }

        /// <summary>
        /// Periodically refreshes the UI state.
        /// </summary>
        private void tmrRefresh_Tick(object sender, EventArgs e)
        {
            RefreshPortGrid();
        }

        /// <summary>
        /// Reloads the configuration.
        /// </summary>
        private void mnuReloadConfig_Click(object sender, EventArgs e)
        {
            if (_serverManager != null && _serverManager.ListeningCount > 0)
            {
                MessageHelper.ShowWarning("Some ports are still listening. Stop all ports before reloading the config!");
                return;
            }

            if (!MessageHelper.ShowConfirm("Reloading will discard all unsaved changes. Continue?"))
            {
                return;
            }

            LoadConfigToUi();
            MessageHelper.ShowSuccess("Reload Config");
        }

        /// <summary>
        /// Opens the config folder.
        /// </summary>
        private void mnuOpenConfigDir_Click(object sender, EventArgs e)
        {
            OpenDirectory(Path.GetDirectoryName(_configBll.ConfigFilePath), "Config Folder");
        }

        /// <summary>
        /// Exits the application.
        /// </summary>
        private void mnuExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Reply rule management.
        /// </summary>
        private void mnuManageRule_Click(object sender, EventArgs e)
        {
            using (frmRuleManager dialog = new frmRuleManager(_config.Rules))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                _config.Rules = dialog.Rules;
                _serverManager.SetRules(_config.Rules);
                SaveConfigToFile(false);
                MessageHelper.ShowSuccess("Save Reply Rules");
            }
        }

        /// <summary>
        /// About.
        /// </summary>
        private void mnuAbout_Click(object sender, EventArgs e)
        {
            using (frmAbout dialog = new frmAbout())
            {
                dialog.ShowDialog(this);
            }
        }

        /// <summary>
        /// Form closing - saves the configuration and stops all listeners.
        /// </summary>
        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            int listening = _serverManager == null ? 0 : _serverManager.ListeningCount;

            string prompt = listening > 0
                ? string.Format("{0} port(s) are listening. Exiting will disconnect all of them. Exit anyway?", listening)
                : "Are you sure you want to exit?";

            if (!MessageHelper.ShowConfirm(prompt))
            {
                e.Cancel = true;
                return;
            }

            try
            {
                tmrRefresh.Stop();
                SaveConfigToFile(false);
                _logger.Info("UI closing, stopping all listening ports.");
            }
            catch (Exception ex)
            {
                _logger.Warn("Exception while saving the config on form close:" + ex.Message);
            }
            finally
            {
                if (_serverManager != null)
                {
                    _serverManager.StopAll();
                }
            }
        }

        // ============================================================
        // 5. Private business methods
        // ============================================================

        /// <summary>
        /// Builds the port list from the start port and count.
        /// </summary>
        private void GeneratePortList()
        {
            if (_isBusy) { return; }

            int startPort = (int)nudStartPort.Value;
            int count = (int)nudPortCount.Value;

            string errorMessage;
            List<PortConfig> newPorts = _configBll.BuildPortList(
                startPort, count, _config.Ports, out errorMessage);

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                MessageHelper.ShowWarning(errorMessage);
                return;
            }

            // Ports that are already listening must be stopped before the list can be rebuilt.
            if (_serverManager.ListeningCount > 0)
            {
                if (!MessageHelper.ShowConfirm("Generating a new port list requires stopping all listening ports. Continue?"))
                {
                    return;
                }

                _serverManager.StopAll();
            }

            _config.StartPort = startPort;
            _config.PortCount = count;
            _config.Ports = newPorts;

            _serverManager.SetPortConfigs(_config.Ports);
            RebuildPortGrid(_config.Ports);

            SaveConfigToFile(false);

            MessageHelper.ShowInfo(string.Format("Generated {0} port(s): {1} ~ {2}",
                newPorts.Count, startPort, startPort + newPorts.Count - 1));
        }

        /// <summary>
        /// Starts all ports.
        /// </summary>
        /// <param name="silent">Silent mode (auto-listen on launch, no confirmation dialog).</param>
        private void StartAllPorts(bool silent)
        {
            if (_isBusy) { return; }

            if (_config.Ports == null || _config.Ports.Count == 0)
            {
                if (!silent)
                {
                    MessageHelper.ShowWarning("The port list is empty. Set the start port and count, then click \"Generate Ports\"!");
                }
                return;
            }

            if (!silent && !MessageHelper.ShowConfirm("Start all enabled ports?"))
            {
                return;
            }

            _isBusy = true;
            btnStartAll.Enabled = false;
            btnStopAll.Enabled = false;

            try
            {
                string message;
                bool anySuccess = _serverManager.StartAll(out message);

                RefreshPortGrid();

                if (!silent)
                {
                    if (anySuccess)
                    {
                        MessageHelper.ShowInfo(message);
                    }
                    else
                    {
                        MessageHelper.ShowWarning(message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception while starting all ports:" + ex.Message, ex);
                MessageHelper.ShowError("Failed to start all ports. See the log for details!");
            }
            finally
            {
                _isBusy = false;
                btnStartAll.Enabled = true;
                btnStopAll.Enabled = true;
            }
        }

        /// <summary>
        /// Stops all ports.
        /// </summary>
        private void StopAllPorts()
        {
            if (_isBusy) { return; }

            int listening = _serverManager.ListeningCount;
            if (listening == 0)
            {
                MessageHelper.ShowInfo("No port is currently listening.");
                return;
            }

            if (!MessageHelper.ShowConfirm(string.Format(
                "Stop all {0} listening port(s)? This will disconnect all connected clients.", listening)))
            {
                return;
            }

            _isBusy = true;

            try
            {
                _serverManager.StopAll();
                RefreshPortGrid();
                MessageHelper.ShowSuccess("Stop All Ports");
            }
            catch (Exception ex)
            {
                _logger.Error("Exception while stopping all ports:" + ex.Message, ex);
                MessageHelper.ShowError("Failed to stop all ports. See the log for details!");
            }
            finally
            {
                _isBusy = false;
            }
        }

        /// <summary>
        /// Toggles start / stop for one port by grid row.
        /// </summary>
        /// <param name="rowIndex">Row index.</param>
        private void TogglePortByRow(int rowIndex)
        {
            int port = PortGridHelper.GetRowPort(dgvPorts, rowIndex);
            if (!ValidationHelper.IsValidPort(port)) { return; }

            PortRuntimeInfo info = _serverManager.GetRuntimeInfo(port);

            if (info != null && info.State == PortState.Listening)
            {
                if (!MessageHelper.ShowConfirm(string.Format("Stop listening on port {0}?", port)))
                {
                    return;
                }

                _serverManager.StopPort(port);
                RefreshPortGrid();
                MessageHelper.ShowInfo(string.Format("Port {0} stopped.", port));
                return;
            }

            string message;
            bool result = _serverManager.StartPort(port, out message);

            RefreshPortGrid();

            if (result)
            {
                MessageHelper.ShowInfo(string.Format("Port {0} started listening.", port));
            }
            else
            {
                MessageHelper.ShowWarning(string.Format("Port {0} failed to start: {1}", port, message));
            }
        }

        /// <summary>
        /// Saves the configuration to file.
        /// </summary>
        /// <param name="showTip">Whether to show a success prompt.</param>
        /// <returns>True if the save succeeded.</returns>
        private bool SaveConfigToFile(bool showTip)
        {
            try
            {
                SyncConfigFromUi();

                bool success = _configBll.SaveConfig(_config);

                if (showTip)
                {
                    if (success)
                    {
                        MessageHelper.ShowSuccess("Save Config");
                    }
                    else
                    {
                        MessageHelper.ShowFail("Save Config");
                    }
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.Error("Exception while saving the config:" + ex.Message, ex);

                if (showTip)
                {
                    MessageHelper.ShowError("Failed to save the config. See the log for details!");
                }

                return false;
            }
        }

        /// <summary>
        /// Synchronizes the UI parameters back into the configuration object.
        /// </summary>
        private void SyncConfigFromUi()
        {
            if (_config == null)
            {
                _config = new AppConfigModel();
            }

            _config.ListenIp = cboListenIp.SelectedItem == null
                ? ConfigHelper.DefaultListenIp
                : cboListenIp.SelectedItem.ToString();

            _config.StartPort = (int)nudStartPort.Value;
            _config.PortCount = (int)nudPortCount.Value;
            _config.AutoStartOnLaunch = chkAutoStart.Checked;
            _config.DisplayAsHex = chkHexView.Checked;
        }

        /// <summary>
        /// Rebuilds the port grid.
        /// </summary>
        /// <param name="ports">Port list; may be null.</param>
        private void RebuildPortGrid(List<PortConfig> ports)
        {
            PortGridHelper.Rebuild(dgvPorts, ports);

            ResetConsoleSelection();
            RefreshListenStatus();
        }

        /// <summary>
        /// Refreshes the runtime data shown in the port grid.
        /// </summary>
        private void RefreshPortGrid()
        {
            try
            {
                if (_serverManager == null) { return; }

                PortGridHelper.Refresh(dgvPorts, _serverManager.GetRuntimeInfos());
                RefreshListenStatus();
            }
            catch (Exception ex)
            {
                _logger.Warn("Exception while refreshing the port grid:" + ex.Message);
            }
        }

        /// <summary>
        /// Refreshes the listening statistics in the status bar.
        /// </summary>
        private void RefreshListenStatus()
        {
            try
            {
                int total = 0;
                int listening = 0;

                if (_config != null && _config.Ports != null)
                {
                    foreach (PortConfig cfg in _config.Ports)
                    {
                        if (cfg != null && cfg.Enabled) { total++; }
                    }
                }

                if (_serverManager != null)
                {
                    listening = _serverManager.ListeningCount;
                }

                lblListenStatus.Text = string.Format("Listening ports: {0} / {1}", listening, total);
            }
            catch (Exception ex)
            {
                _logger.Warn("Exception while refreshing the status bar:" + ex.Message);
            }
        }

        /// <summary>
        /// Port state change callback.
        /// </summary>
        private void OnPortStateChanged(object sender, PortStateChangedEventArgs e)
        {
            if (e == null) { return; }

            InvokeSafely(delegate { RefreshPortGrid(); });
        }

        // ============================================================
        // 6. Helper methods
        // ============================================================

        /// <summary>
        /// Executes an action safely on the UI thread.
        /// </summary>
        /// <param name="action">Delegate to execute.</param>
        private void InvokeSafely(MethodInvoker action)
        {
            if (action == null) { return; }

            try
            {
                if (IsDisposed || !IsHandleCreated) { return; }

                if (InvokeRequired)
                {
                    BeginInvoke(action);
                }
                else
                {
                    action();
                }
            }
            catch (Exception ex)
            {
                _logger.Debug("UI refresh delegate failed:" + ex.Message);
            }
        }

        /// <summary>
        /// Opens the specified folder (reports the failure reason when it does not exist).
        /// </summary>
        /// <param name="path">Folder path.</param>
        /// <param name="name">Folder name (used in the message).</param>
        private void OpenDirectory(string path, string name)
        {
            string error;
            if (UiHelper.OpenDirectory(path, out error))
            {
                return;
            }

            _logger.Error(string.Format("Failed to open {0}: {1}", name, error));
            MessageHelper.ShowError(string.Format("Failed to open {0}: {1}", name, error));
        }
    }
}
