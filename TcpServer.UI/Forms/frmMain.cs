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
    /// 主窗体 —— 多端口 TCP 监听的控制台界面（端口管理、启停控制、配置持久化）
    /// 数据控制台相关成员见 frmMain.Console.cs
    /// </summary>
    public partial class frmMain : Form
    {
        // ============================================================
        // 1. 私有字段（按功能分组）
        // ============================================================

        /// <summary>日志工具</summary>
        private readonly LogHelper _logger = LogHelper.Instance;

        /// <summary>配置业务对象</summary>
        private readonly ConfigBLL _configBll = new ConfigBLL();

        /// <summary>多端口监听管理器</summary>
        private TcpServerManager _serverManager;

        /// <summary>当前配置</summary>
        private AppConfigModel _config;

        /// <summary>操作进行中标志 —— 防止按钮被重复点击</summary>
        private bool _isBusy;

        // ============================================================
        // 2. 构造函数
        // ============================================================

        /// <summary>
        /// 构造函数
        /// </summary>
        public frmMain()
        {
            InitializeComponent();
            InitializeCustomSettings();
        }

        // ============================================================
        // 3. 自定义初始化方法
        // ============================================================

        /// <summary>
        /// 自定义初始化 —— 绑定事件、加载配置、准备监听管理器
        /// </summary>
        private void InitializeCustomSettings()
        {
            try
            {
                _isBusy = false;
                ResetConsoleSelection();

                Text = string.Format("多端口 TCP 监听调试工具  v{0}", AppConstants.APP_VERSION);
                lblVersion.Text = string.Format("版本 v{0}", AppConstants.APP_VERSION);

                BindEvents();
                InitializeListenIpComboBox();

                _serverManager = new TcpServerManager(ConfigHelper.DefaultListenIp, null);
                _serverManager.PortStateChanged += OnPortStateChanged;
                _serverManager.ClientChanged += OnClientChanged;
                _serverManager.DataReceived += OnDataReceived;
                _serverManager.DataSent += OnDataSent;

                LoadConfigToUi();

                lblConfigPath.Text = "配置文件：" + _configBll.ConfigFilePath;

                tmrRefresh.Start();

                _logger.Info("主界面初始化完成。");

                if (chkAutoStart.Checked)
                {
                    StartAllPorts(true);
                }
            }
            catch (Exception ex)
            {
                _logger.Fatal("主界面初始化失败：" + ex.Message, ex);
                MessageHelper.ShowError("界面初始化失败，详情请查看日志！\n" + ex.Message);
            }
        }

        /// <summary>
        /// 绑定控件事件
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
        /// 初始化监听地址下拉框
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
        /// 将配置加载到界面
        /// </summary>
        private void LoadConfigToUi()
        {
            _config = _configBll.LoadConfig();

            if (_config == null)
            {
                _config = new AppConfigModel();
            }

            // 监听地址：若已有配置项不在下拉列表中则补进去
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
        // 4. 事件处理方法
        // ============================================================

        /// <summary>
        /// 生成端口清单
        /// </summary>
        private void btnGenerate_Click(object sender, EventArgs e)
        {
            GeneratePortList();
        }

        /// <summary>
        /// 启动全部端口
        /// </summary>
        private void btnStartAll_Click(object sender, EventArgs e)
        {
            StartAllPorts(false);
        }

        /// <summary>
        /// 停止全部端口
        /// </summary>
        private void btnStopAll_Click(object sender, EventArgs e)
        {
            StopAllPorts();
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        private void btnSaveConfig_Click(object sender, EventArgs e)
        {
            SaveConfigToFile(true);
        }

        /// <summary>
        /// 打开日志目录
        /// </summary>
        private void btnOpenLog_Click(object sender, EventArgs e)
        {
            OpenDirectory(_logger.LogDirectory, "日志目录");
        }

        /// <summary>
        /// 端口列表选中行变化
        /// </summary>
        private void dgvPorts_SelectionChanged(object sender, EventArgs e)
        {
            UpdateCurrentPortFromGrid();
        }

        /// <summary>
        /// 端口列表双击 —— 切换该端口的启动/停止
        /// </summary>
        private void dgvPorts_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e == null || e.RowIndex < 0) { return; }

            TogglePortByRow(e.RowIndex);
        }

        /// <summary>
        /// 监听地址变化 —— 仅在未监听时允许切换
        /// </summary>
        private void cboListenIp_SelectedIndexChanged(object sender, EventArgs e)
        {
            string ip = cboListenIp.SelectedItem == null ? string.Empty : cboListenIp.SelectedItem.ToString();
            if (ValidationHelper.IsNullOrWhiteSpace(ip)) { return; }
            if (_serverManager == null) { return; }

            if (_serverManager.ListeningCount > 0)
            {
                MessageHelper.ShowWarning("已有端口处于监听状态，请先停止全部端口再切换监听地址！");
                return;
            }

            _serverManager.SetListenIp(ip);
        }

        /// <summary>
        /// 定时刷新界面状态
        /// </summary>
        private void tmrRefresh_Tick(object sender, EventArgs e)
        {
            RefreshPortGrid();
        }

        /// <summary>
        /// 重新加载配置
        /// </summary>
        private void mnuReloadConfig_Click(object sender, EventArgs e)
        {
            if (_serverManager != null && _serverManager.ListeningCount > 0)
            {
                MessageHelper.ShowWarning("已有端口处于监听状态，请先停止全部端口再重新加载配置！");
                return;
            }

            if (!MessageHelper.ShowConfirm("重新加载将放弃当前未保存的改动，确定继续吗？"))
            {
                return;
            }

            LoadConfigToUi();
            MessageHelper.ShowSuccess("重新加载配置");
        }

        /// <summary>
        /// 打开配置目录
        /// </summary>
        private void mnuOpenConfigDir_Click(object sender, EventArgs e)
        {
            OpenDirectory(Path.GetDirectoryName(_configBll.ConfigFilePath), "配置目录");
        }

        /// <summary>
        /// 退出程序
        /// </summary>
        private void mnuExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        /// 应答规则管理
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
                MessageHelper.ShowSuccess("保存应答规则");
            }
        }

        /// <summary>
        /// 关于
        /// </summary>
        private void mnuAbout_Click(object sender, EventArgs e)
        {
            using (frmAbout dialog = new frmAbout())
            {
                dialog.ShowDialog(this);
            }
        }

        /// <summary>
        /// 窗体关闭 —— 保存配置并关闭全部监听
        /// </summary>
        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            int listening = _serverManager == null ? 0 : _serverManager.ListeningCount;

            string prompt = listening > 0
                ? string.Format("当前有 {0} 个端口正在监听，退出将全部断开。确定退出吗？", listening)
                : "确定要退出程序吗？";

            if (!MessageHelper.ShowConfirm(prompt))
            {
                e.Cancel = true;
                return;
            }

            try
            {
                tmrRefresh.Stop();
                SaveConfigToFile(false);
                _logger.Info("界面关闭，正在停止全部监听端口。");
            }
            catch (Exception ex)
            {
                _logger.Warn("窗体关闭时保存配置异常：" + ex.Message);
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
        // 5. 私有业务方法
        // ============================================================

        /// <summary>
        /// 按起始端口与数量生成端口清单
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

            // 已监听的端口需要先停止才能重建清单
            if (_serverManager.ListeningCount > 0)
            {
                if (!MessageHelper.ShowConfirm("生成新清单需要先停止全部监听端口，确定继续吗？"))
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

            MessageHelper.ShowInfo(string.Format("已生成 {0} 个端口：{1} ~ {2}",
                newPorts.Count, startPort, startPort + newPorts.Count - 1));
        }

        /// <summary>
        /// 启动全部端口
        /// </summary>
        /// <param name="silent">静默模式（启动时自动监听，不弹确认）</param>
        private void StartAllPorts(bool silent)
        {
            if (_isBusy) { return; }

            if (_config.Ports == null || _config.Ports.Count == 0)
            {
                if (!silent)
                {
                    MessageHelper.ShowWarning("端口清单为空，请先设置起始端口与数量后点击\"生成端口\"！");
                }
                return;
            }

            if (!silent && !MessageHelper.ShowConfirm("确定要启动全部启用的端口吗？"))
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
                _logger.Error("启动全部端口异常：" + ex.Message, ex);
                MessageHelper.ShowError("启动全部端口失败，详情请查看日志！");
            }
            finally
            {
                _isBusy = false;
                btnStartAll.Enabled = true;
                btnStopAll.Enabled = true;
            }
        }

        /// <summary>
        /// 停止全部端口
        /// </summary>
        private void StopAllPorts()
        {
            if (_isBusy) { return; }

            int listening = _serverManager.ListeningCount;
            if (listening == 0)
            {
                MessageHelper.ShowInfo("当前没有端口处于监听状态。");
                return;
            }

            if (!MessageHelper.ShowConfirm(string.Format(
                "确定要停止全部 {0} 个监听端口吗？该操作会断开所有已连接的客户端。", listening)))
            {
                return;
            }

            _isBusy = true;

            try
            {
                _serverManager.StopAll();
                RefreshPortGrid();
                MessageHelper.ShowSuccess("停止全部端口");
            }
            catch (Exception ex)
            {
                _logger.Error("停止全部端口异常：" + ex.Message, ex);
                MessageHelper.ShowError("停止全部端口失败，详情请查看日志！");
            }
            finally
            {
                _isBusy = false;
            }
        }

        /// <summary>
        /// 按行切换端口启停
        /// </summary>
        /// <param name="rowIndex">行索引</param>
        private void TogglePortByRow(int rowIndex)
        {
            int port = PortGridHelper.GetRowPort(dgvPorts, rowIndex);
            if (!ValidationHelper.IsValidPort(port)) { return; }

            PortRuntimeInfo info = _serverManager.GetRuntimeInfo(port);

            if (info != null && info.State == PortState.Listening)
            {
                if (!MessageHelper.ShowConfirm(string.Format("确定要停止端口 {0} 的监听吗？", port)))
                {
                    return;
                }

                _serverManager.StopPort(port);
                RefreshPortGrid();
                MessageHelper.ShowInfo(string.Format("端口 {0} 已停止。", port));
                return;
            }

            string message;
            bool result = _serverManager.StartPort(port, out message);

            RefreshPortGrid();

            if (result)
            {
                MessageHelper.ShowInfo(string.Format("端口 {0} 已启动监听。", port));
            }
            else
            {
                MessageHelper.ShowWarning(string.Format("端口 {0} 启动失败：{1}", port, message));
            }
        }

        /// <summary>
        /// 保存配置到文件
        /// </summary>
        /// <param name="showTip">是否弹出成功提示</param>
        /// <returns>保存成功返回 true</returns>
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
                        MessageHelper.ShowSuccess("保存配置");
                    }
                    else
                    {
                        MessageHelper.ShowFail("保存配置");
                    }
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.Error("保存配置异常：" + ex.Message, ex);

                if (showTip)
                {
                    MessageHelper.ShowError("保存配置失败，详情请查看日志！");
                }

                return false;
            }
        }

        /// <summary>
        /// 将界面上的参数同步回配置对象
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
        /// 重建端口表格
        /// </summary>
        /// <param name="ports">端口清单，可为 null</param>
        private void RebuildPortGrid(List<PortConfig> ports)
        {
            PortGridHelper.Rebuild(dgvPorts, ports);

            ResetConsoleSelection();
            RefreshListenStatus();
        }

        /// <summary>
        /// 刷新端口表格中的运行时数据
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
                _logger.Warn("刷新端口表格异常：" + ex.Message);
            }
        }

        /// <summary>
        /// 刷新状态栏的监听统计
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

                lblListenStatus.Text = string.Format("监听端口：{0} / {1}", listening, total);
            }
            catch (Exception ex)
            {
                _logger.Warn("刷新状态栏异常：" + ex.Message);
            }
        }

        /// <summary>
        /// 端口状态变化回调
        /// </summary>
        private void OnPortStateChanged(object sender, PortStateChangedEventArgs e)
        {
            if (e == null) { return; }

            InvokeSafely(delegate { RefreshPortGrid(); });
        }

        // ============================================================
        // 6. 辅助方法
        // ============================================================

        /// <summary>
        /// 在 UI 线程上安全执行操作
        /// </summary>
        /// <param name="action">待执行委托</param>
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
                _logger.Debug("界面刷新委托执行失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 打开指定目录（不存在时提示失败原因）
        /// </summary>
        /// <param name="path">目录路径</param>
        /// <param name="name">目录名称（用于提示）</param>
        private void OpenDirectory(string path, string name)
        {
            string error;
            if (UiHelper.OpenDirectory(path, out error))
            {
                return;
            }

            _logger.Error(string.Format("打开{0}失败：{1}", name, error));
            MessageHelper.ShowError(string.Format("打开{0}失败：{1}", name, error));
        }
    }
}
