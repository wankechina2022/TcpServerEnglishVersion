namespace TcpServer.UI.Forms
{
    partial class frmMain
    {
        /// <summary>
        /// 必需的设计器变量
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }

                // 释放业务对象（规约：非托管资源必须有释放代码）
                if (_serverManager != null)
                {
                    _serverManager.Dispose();
                    _serverManager = null;
                }
            }

            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改此方法的内容
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.mnuConfig = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuSaveConfig = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuReloadConfig = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuOpenConfigDir = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuOpenLogDir = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuSep1 = new System.Windows.Forms.ToolStripSeparator();
            this.mnuExit = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuRule = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuManageRule = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuHelp = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuAbout = new System.Windows.Forms.ToolStripMenuItem();
            this.pnlTop = new System.Windows.Forms.Panel();
            this.lblListenIp = new System.Windows.Forms.Label();
            this.cboListenIp = new System.Windows.Forms.ComboBox();
            this.lblStartPort = new System.Windows.Forms.Label();
            this.nudStartPort = new System.Windows.Forms.NumericUpDown();
            this.lblPortCount = new System.Windows.Forms.Label();
            this.nudPortCount = new System.Windows.Forms.NumericUpDown();
            this.chkAutoStart = new System.Windows.Forms.CheckBox();
            this.btnGenerate = new System.Windows.Forms.Button();
            this.btnStartAll = new System.Windows.Forms.Button();
            this.btnStopAll = new System.Windows.Forms.Button();
            this.btnSaveConfig = new System.Windows.Forms.Button();
            this.pnlGrid = new System.Windows.Forms.Panel();
            this.dgvPorts = new System.Windows.Forms.DataGridView();
            this.colPort = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colState = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colClients = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colReceived = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSent = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLastActive = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRemark = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.pnlData = new System.Windows.Forms.Panel();
            this.txtData = new System.Windows.Forms.RichTextBox();
            this.pnlSend = new System.Windows.Forms.Panel();
            this.lblSend = new System.Windows.Forms.Label();
            this.txtSend = new System.Windows.Forms.TextBox();
            this.chkSendHex = new System.Windows.Forms.CheckBox();
            this.chkSendCrlf = new System.Windows.Forms.CheckBox();
            this.btnSend = new System.Windows.Forms.Button();
            this.btnSendAll = new System.Windows.Forms.Button();
            this.pnlDataTool = new System.Windows.Forms.Panel();
            this.lblClient = new System.Windows.Forms.Label();
            this.cboClient = new System.Windows.Forms.ComboBox();
            this.chkHexView = new System.Windows.Forms.CheckBox();
            this.btnClearData = new System.Windows.Forms.Button();
            this.btnOpenLog = new System.Windows.Forms.Button();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.lblVersion = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblListenStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblConfigPath = new System.Windows.Forms.ToolStripStatusLabel();
            this.tmrRefresh = new System.Windows.Forms.Timer(this.components);
            this.menuStrip.SuspendLayout();
            this.pnlTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudStartPort)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPortCount)).BeginInit();
            this.pnlGrid.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPorts)).BeginInit();
            this.pnlData.SuspendLayout();
            this.pnlSend.SuspendLayout();
            this.pnlDataTool.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip
            // 
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuConfig,
            this.mnuRule,
            this.mnuHelp});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(1120, 25);
            this.menuStrip.TabIndex = 0;
            this.menuStrip.Text = "menuStrip";
            // 
            // mnuConfig
            // 
            this.mnuConfig.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuSaveConfig,
            this.mnuReloadConfig,
            this.mnuOpenConfigDir,
            this.mnuOpenLogDir,
            this.mnuSep1,
            this.mnuExit});
            this.mnuConfig.Name = "mnuConfig";
            this.mnuConfig.Size = new System.Drawing.Size(62, 21);
            this.mnuConfig.Text = "配置(&C)";
            // 
            // mnuSaveConfig
            // 
            this.mnuSaveConfig.Name = "mnuSaveConfig";
            this.mnuSaveConfig.Size = new System.Drawing.Size(180, 22);
            this.mnuSaveConfig.Text = "保存配置";
            // 
            // mnuReloadConfig
            // 
            this.mnuReloadConfig.Name = "mnuReloadConfig";
            this.mnuReloadConfig.Size = new System.Drawing.Size(180, 22);
            this.mnuReloadConfig.Text = "重新加载配置";
            // 
            // mnuOpenConfigDir
            // 
            this.mnuOpenConfigDir.Name = "mnuOpenConfigDir";
            this.mnuOpenConfigDir.Size = new System.Drawing.Size(180, 22);
            this.mnuOpenConfigDir.Text = "打开配置目录";
            // 
            // mnuOpenLogDir
            // 
            this.mnuOpenLogDir.Name = "mnuOpenLogDir";
            this.mnuOpenLogDir.Size = new System.Drawing.Size(180, 22);
            this.mnuOpenLogDir.Text = "打开日志目录";
            // 
            // mnuSep1
            // 
            this.mnuSep1.Name = "mnuSep1";
            this.mnuSep1.Size = new System.Drawing.Size(177, 6);
            // 
            // mnuExit
            // 
            this.mnuExit.Name = "mnuExit";
            this.mnuExit.Size = new System.Drawing.Size(180, 22);
            this.mnuExit.Text = "退出";
            // 
            // mnuRule
            // 
            this.mnuRule.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuManageRule});
            this.mnuRule.Name = "mnuRule";
            this.mnuRule.Size = new System.Drawing.Size(62, 21);
            this.mnuRule.Text = "规则(&R)";
            // 
            // mnuManageRule
            // 
            this.mnuManageRule.Name = "mnuManageRule";
            this.mnuManageRule.Size = new System.Drawing.Size(180, 22);
            this.mnuManageRule.Text = "应答规则管理";
            // 
            // mnuHelp
            // 
            this.mnuHelp.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuAbout});
            this.mnuHelp.Name = "mnuHelp";
            this.mnuHelp.Size = new System.Drawing.Size(62, 21);
            this.mnuHelp.Text = "帮助(&H)";
            // 
            // mnuAbout
            // 
            this.mnuAbout.Name = "mnuAbout";
            this.mnuAbout.Size = new System.Drawing.Size(180, 22);
            this.mnuAbout.Text = "关于";
            // 
            // pnlTop
            // 
            this.pnlTop.Controls.Add(this.btnSaveConfig);
            this.pnlTop.Controls.Add(this.btnStopAll);
            this.pnlTop.Controls.Add(this.btnStartAll);
            this.pnlTop.Controls.Add(this.btnGenerate);
            this.pnlTop.Controls.Add(this.chkAutoStart);
            this.pnlTop.Controls.Add(this.nudPortCount);
            this.pnlTop.Controls.Add(this.lblPortCount);
            this.pnlTop.Controls.Add(this.nudStartPort);
            this.pnlTop.Controls.Add(this.lblStartPort);
            this.pnlTop.Controls.Add(this.cboListenIp);
            this.pnlTop.Controls.Add(this.lblListenIp);
            this.pnlTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTop.Location = new System.Drawing.Point(0, 25);
            this.pnlTop.Name = "pnlTop";
            this.pnlTop.Size = new System.Drawing.Size(1120, 48);
            this.pnlTop.TabIndex = 1;
            // 
            // lblListenIp
            // 
            this.lblListenIp.Location = new System.Drawing.Point(12, 16);
            this.lblListenIp.Name = "lblListenIp";
            this.lblListenIp.Size = new System.Drawing.Size(62, 16);
            this.lblListenIp.TabIndex = 0;
            this.lblListenIp.Text = "监听地址";
            this.lblListenIp.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cboListenIp
            // 
            this.cboListenIp.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboListenIp.FormattingEnabled = true;
            this.cboListenIp.Location = new System.Drawing.Point(78, 12);
            this.cboListenIp.Name = "cboListenIp";
            this.cboListenIp.Size = new System.Drawing.Size(130, 25);
            this.cboListenIp.TabIndex = 1;
            // 
            // lblStartPort
            // 
            this.lblStartPort.Location = new System.Drawing.Point(220, 16);
            this.lblStartPort.Name = "lblStartPort";
            this.lblStartPort.Size = new System.Drawing.Size(62, 16);
            this.lblStartPort.TabIndex = 2;
            this.lblStartPort.Text = "起始端口";
            this.lblStartPort.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // nudStartPort
            // 
            this.nudStartPort.Location = new System.Drawing.Point(286, 12);
            this.nudStartPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            this.nudStartPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudStartPort.Name = "nudStartPort";
            this.nudStartPort.Size = new System.Drawing.Size(80, 25);
            this.nudStartPort.TabIndex = 3;
            this.nudStartPort.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.nudStartPort.Value = new decimal(new int[] { 60000, 0, 0, 0 });
            // 
            // lblPortCount
            // 
            this.lblPortCount.Location = new System.Drawing.Point(378, 16);
            this.lblPortCount.Name = "lblPortCount";
            this.lblPortCount.Size = new System.Drawing.Size(62, 16);
            this.lblPortCount.TabIndex = 4;
            this.lblPortCount.Text = "端口数量";
            this.lblPortCount.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // nudPortCount
            // 
            this.nudPortCount.Location = new System.Drawing.Point(444, 12);
            this.nudPortCount.Maximum = new decimal(new int[] { 200, 0, 0, 0 });
            this.nudPortCount.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudPortCount.Name = "nudPortCount";
            this.nudPortCount.Size = new System.Drawing.Size(70, 25);
            this.nudPortCount.TabIndex = 5;
            this.nudPortCount.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.nudPortCount.Value = new decimal(new int[] { 20, 0, 0, 0 });
            // 
            // chkAutoStart
            // 
            this.chkAutoStart.Location = new System.Drawing.Point(528, 15);
            this.chkAutoStart.Name = "chkAutoStart";
            this.chkAutoStart.Size = new System.Drawing.Size(130, 21);
            this.chkAutoStart.TabIndex = 6;
            this.chkAutoStart.Text = "启动即自动监听";
            this.chkAutoStart.UseVisualStyleBackColor = true;
            // 
            // btnGenerate
            // 
            this.btnGenerate.Location = new System.Drawing.Point(668, 10);
            this.btnGenerate.Name = "btnGenerate";
            this.btnGenerate.Size = new System.Drawing.Size(84, 29);
            this.btnGenerate.TabIndex = 7;
            this.btnGenerate.Text = "生成端口";
            this.btnGenerate.UseVisualStyleBackColor = true;
            // 
            // btnStartAll
            // 
            this.btnStartAll.Location = new System.Drawing.Point(760, 10);
            this.btnStartAll.Name = "btnStartAll";
            this.btnStartAll.Size = new System.Drawing.Size(84, 29);
            this.btnStartAll.TabIndex = 8;
            this.btnStartAll.Text = "启动全部";
            this.btnStartAll.UseVisualStyleBackColor = true;
            // 
            // btnStopAll
            // 
            this.btnStopAll.Location = new System.Drawing.Point(852, 10);
            this.btnStopAll.Name = "btnStopAll";
            this.btnStopAll.Size = new System.Drawing.Size(84, 29);
            this.btnStopAll.TabIndex = 9;
            this.btnStopAll.Text = "停止全部";
            this.btnStopAll.UseVisualStyleBackColor = true;
            // 
            // btnSaveConfig
            // 
            this.btnSaveConfig.Location = new System.Drawing.Point(944, 10);
            this.btnSaveConfig.Name = "btnSaveConfig";
            this.btnSaveConfig.Size = new System.Drawing.Size(84, 29);
            this.btnSaveConfig.TabIndex = 10;
            this.btnSaveConfig.Text = "保存配置";
            this.btnSaveConfig.UseVisualStyleBackColor = true;
            // 
            // pnlGrid
            // 
            this.pnlGrid.Controls.Add(this.dgvPorts);
            this.pnlGrid.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlGrid.Location = new System.Drawing.Point(0, 73);
            this.pnlGrid.Name = "pnlGrid";
            this.pnlGrid.Size = new System.Drawing.Size(1120, 250);
            this.pnlGrid.TabIndex = 2;
            // 
            // dgvPorts
            // 
            this.dgvPorts.AllowUserToAddRows = false;
            this.dgvPorts.AllowUserToDeleteRows = false;
            this.dgvPorts.AllowUserToResizeRows = false;
            this.dgvPorts.BackgroundColor = System.Drawing.Color.White;
            this.dgvPorts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgvPorts.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colPort,
            this.colState,
            this.colClients,
            this.colReceived,
            this.colSent,
            this.colLastActive,
            this.colRemark});
            this.dgvPorts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvPorts.Location = new System.Drawing.Point(0, 0);
            this.dgvPorts.MultiSelect = false;
            this.dgvPorts.Name = "dgvPorts";
            this.dgvPorts.ReadOnly = true;
            this.dgvPorts.RowHeadersVisible = false;
            this.dgvPorts.RowTemplate.Height = 25;
            this.dgvPorts.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvPorts.Size = new System.Drawing.Size(1120, 250);
            this.dgvPorts.TabIndex = 0;
            // 
            // colPort
            // 
            this.colPort.HeaderText = "端口";
            this.colPort.Name = "colPort";
            this.colPort.ReadOnly = true;
            this.colPort.Width = 80;
            // 
            // colState
            // 
            this.colState.HeaderText = "状态";
            this.colState.Name = "colState";
            this.colState.ReadOnly = true;
            this.colState.Width = 90;
            // 
            // colClients
            // 
            this.colClients.HeaderText = "客户端数";
            this.colClients.Name = "colClients";
            this.colClients.ReadOnly = true;
            this.colClients.Width = 90;
            // 
            // colReceived
            // 
            this.colReceived.HeaderText = "接收字节";
            this.colReceived.Name = "colReceived";
            this.colReceived.ReadOnly = true;
            this.colReceived.Width = 110;
            // 
            // colSent
            // 
            this.colSent.HeaderText = "发送字节";
            this.colSent.Name = "colSent";
            this.colSent.ReadOnly = true;
            this.colSent.Width = 110;
            // 
            // colLastActive
            // 
            this.colLastActive.HeaderText = "最后活动";
            this.colLastActive.Name = "colLastActive";
            this.colLastActive.ReadOnly = true;
            this.colLastActive.Width = 150;
            // 
            // colRemark
            // 
            this.colRemark.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colRemark.HeaderText = "备注";
            this.colRemark.MinimumWidth = 120;
            this.colRemark.Name = "colRemark";
            this.colRemark.ReadOnly = true;
            // 
            // pnlData
            // 
            this.pnlData.Controls.Add(this.txtData);
            this.pnlData.Controls.Add(this.pnlSend);
            this.pnlData.Controls.Add(this.pnlDataTool);
            this.pnlData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlData.Location = new System.Drawing.Point(0, 323);
            this.pnlData.Name = "pnlData";
            this.pnlData.Size = new System.Drawing.Size(1120, 375);
            this.pnlData.TabIndex = 3;
            // 
            // txtData
            // 
            this.txtData.BackColor = System.Drawing.Color.White;
            this.txtData.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtData.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtData.Location = new System.Drawing.Point(0, 34);
            this.txtData.Name = "txtData";
            this.txtData.ReadOnly = true;
            this.txtData.Size = new System.Drawing.Size(1120, 299);
            this.txtData.TabIndex = 2;
            this.txtData.Text = "";
            // 
            // pnlSend
            // 
            this.pnlSend.Controls.Add(this.btnSendAll);
            this.pnlSend.Controls.Add(this.btnSend);
            this.pnlSend.Controls.Add(this.chkSendCrlf);
            this.pnlSend.Controls.Add(this.chkSendHex);
            this.pnlSend.Controls.Add(this.txtSend);
            this.pnlSend.Controls.Add(this.lblSend);
            this.pnlSend.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlSend.Location = new System.Drawing.Point(0, 333);
            this.pnlSend.Name = "pnlSend";
            this.pnlSend.Size = new System.Drawing.Size(1120, 42);
            this.pnlSend.TabIndex = 1;
            // 
            // lblSend
            // 
            this.lblSend.Location = new System.Drawing.Point(12, 13);
            this.lblSend.Name = "lblSend";
            this.lblSend.Size = new System.Drawing.Size(40, 16);
            this.lblSend.TabIndex = 0;
            this.lblSend.Text = "发送";
            this.lblSend.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtSend
            // 
            this.txtSend.Location = new System.Drawing.Point(56, 9);
            this.txtSend.Name = "txtSend";
            this.txtSend.Size = new System.Drawing.Size(530, 25);
            this.txtSend.TabIndex = 1;
            // 
            // chkSendHex
            // 
            this.chkSendHex.Location = new System.Drawing.Point(594, 11);
            this.chkSendHex.Name = "chkSendHex";
            this.chkSendHex.Size = new System.Drawing.Size(90, 21);
            this.chkSendHex.TabIndex = 2;
            this.chkSendHex.Text = "HEX 发送";
            this.chkSendHex.UseVisualStyleBackColor = true;
            // 
            // chkSendCrlf
            // 
            this.chkSendCrlf.Location = new System.Drawing.Point(692, 11);
            this.chkSendCrlf.Name = "chkSendCrlf";
            this.chkSendCrlf.Size = new System.Drawing.Size(135, 21);
            this.chkSendCrlf.TabIndex = 3;
            this.chkSendCrlf.Text = "追加 0x0D 0x0A";
            this.chkSendCrlf.UseVisualStyleBackColor = true;
            // 
            // btnSend
            // 
            this.btnSend.Location = new System.Drawing.Point(835, 8);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(76, 28);
            this.btnSend.TabIndex = 4;
            this.btnSend.Text = "发送";
            this.btnSend.UseVisualStyleBackColor = true;
            // 
            // btnSendAll
            // 
            this.btnSendAll.Location = new System.Drawing.Point(919, 8);
            this.btnSendAll.Name = "btnSendAll";
            this.btnSendAll.Size = new System.Drawing.Size(100, 28);
            this.btnSendAll.TabIndex = 5;
            this.btnSendAll.Text = "广播全部";
            this.btnSendAll.UseVisualStyleBackColor = true;
            // 
            // pnlDataTool
            // 
            this.pnlDataTool.Controls.Add(this.btnOpenLog);
            this.pnlDataTool.Controls.Add(this.btnClearData);
            this.pnlDataTool.Controls.Add(this.chkHexView);
            this.pnlDataTool.Controls.Add(this.cboClient);
            this.pnlDataTool.Controls.Add(this.lblClient);
            this.pnlDataTool.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlDataTool.Location = new System.Drawing.Point(0, 0);
            this.pnlDataTool.Name = "pnlDataTool";
            this.pnlDataTool.Size = new System.Drawing.Size(1120, 34);
            this.pnlDataTool.TabIndex = 0;
            // 
            // lblClient
            // 
            this.lblClient.Location = new System.Drawing.Point(12, 9);
            this.lblClient.Name = "lblClient";
            this.lblClient.Size = new System.Drawing.Size(56, 16);
            this.lblClient.TabIndex = 0;
            this.lblClient.Text = "客户端";
            this.lblClient.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cboClient
            // 
            this.cboClient.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboClient.FormattingEnabled = true;
            this.cboClient.Location = new System.Drawing.Point(72, 5);
            this.cboClient.Name = "cboClient";
            this.cboClient.Size = new System.Drawing.Size(240, 25);
            this.cboClient.TabIndex = 1;
            // 
            // chkHexView
            // 
            this.chkHexView.Location = new System.Drawing.Point(326, 7);
            this.chkHexView.Name = "chkHexView";
            this.chkHexView.Size = new System.Drawing.Size(96, 21);
            this.chkHexView.TabIndex = 2;
            this.chkHexView.Text = "HEX 显示";
            this.chkHexView.UseVisualStyleBackColor = true;
            // 
            // btnClearData
            // 
            this.btnClearData.Location = new System.Drawing.Point(428, 4);
            this.btnClearData.Name = "btnClearData";
            this.btnClearData.Size = new System.Drawing.Size(70, 26);
            this.btnClearData.TabIndex = 3;
            this.btnClearData.Text = "清空";
            this.btnClearData.UseVisualStyleBackColor = true;
            // 
            // btnOpenLog
            // 
            this.btnOpenLog.Location = new System.Drawing.Point(506, 4);
            this.btnOpenLog.Name = "btnOpenLog";
            this.btnOpenLog.Size = new System.Drawing.Size(110, 26);
            this.btnOpenLog.TabIndex = 4;
            this.btnOpenLog.Text = "打开日志目录";
            this.btnOpenLog.UseVisualStyleBackColor = true;
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblVersion,
            this.lblListenStatus,
            this.lblConfigPath});
            this.statusStrip.Location = new System.Drawing.Point(0, 698);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(1120, 22);
            this.statusStrip.TabIndex = 4;
            // 
            // lblVersion
            // 
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(60, 17);
            this.lblVersion.Text = "版本";
            // 
            // lblListenStatus
            // 
            this.lblListenStatus.Name = "lblListenStatus";
            this.lblListenStatus.Size = new System.Drawing.Size(120, 17);
            this.lblListenStatus.Text = "监听端口：0 / 0";
            // 
            // lblConfigPath
            // 
            this.lblConfigPath.Name = "lblConfigPath";
            this.lblConfigPath.Size = new System.Drawing.Size(400, 17);
            this.lblConfigPath.Spring = true;
            this.lblConfigPath.Text = "配置文件";
            this.lblConfigPath.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tmrRefresh
            // 
            this.tmrRefresh.Interval = 1000;
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1120, 720);
            this.Controls.Add(this.pnlData);
            this.Controls.Add(this.pnlGrid);
            this.Controls.Add(this.pnlTop);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.menuStrip);
            this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.MainMenuStrip = this.menuStrip;
            this.MinimumSize = new System.Drawing.Size(1000, 600);
            this.Name = "frmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "多端口 TCP 监听调试工具";
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.pnlTop.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.nudStartPort)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPortCount)).EndInit();
            this.pnlGrid.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvPorts)).EndInit();
            this.pnlData.ResumeLayout(false);
            this.pnlSend.ResumeLayout(false);
            this.pnlSend.PerformLayout();
            this.pnlDataTool.ResumeLayout(false);
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem mnuConfig;
        private System.Windows.Forms.ToolStripMenuItem mnuSaveConfig;
        private System.Windows.Forms.ToolStripMenuItem mnuReloadConfig;
        private System.Windows.Forms.ToolStripMenuItem mnuOpenConfigDir;
        private System.Windows.Forms.ToolStripMenuItem mnuOpenLogDir;
        private System.Windows.Forms.ToolStripSeparator mnuSep1;
        private System.Windows.Forms.ToolStripMenuItem mnuExit;
        private System.Windows.Forms.ToolStripMenuItem mnuRule;
        private System.Windows.Forms.ToolStripMenuItem mnuManageRule;
        private System.Windows.Forms.ToolStripMenuItem mnuHelp;
        private System.Windows.Forms.ToolStripMenuItem mnuAbout;
        private System.Windows.Forms.Panel pnlTop;
        private System.Windows.Forms.Label lblListenIp;
        private System.Windows.Forms.ComboBox cboListenIp;
        private System.Windows.Forms.Label lblStartPort;
        private System.Windows.Forms.NumericUpDown nudStartPort;
        private System.Windows.Forms.Label lblPortCount;
        private System.Windows.Forms.NumericUpDown nudPortCount;
        private System.Windows.Forms.CheckBox chkAutoStart;
        private System.Windows.Forms.Button btnGenerate;
        private System.Windows.Forms.Button btnStartAll;
        private System.Windows.Forms.Button btnStopAll;
        private System.Windows.Forms.Button btnSaveConfig;
        private System.Windows.Forms.Panel pnlGrid;
        private System.Windows.Forms.DataGridView dgvPorts;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPort;
        private System.Windows.Forms.DataGridViewTextBoxColumn colState;
        private System.Windows.Forms.DataGridViewTextBoxColumn colClients;
        private System.Windows.Forms.DataGridViewTextBoxColumn colReceived;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSent;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLastActive;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRemark;
        private System.Windows.Forms.Panel pnlData;
        private System.Windows.Forms.RichTextBox txtData;
        private System.Windows.Forms.Panel pnlSend;
        private System.Windows.Forms.Label lblSend;
        private System.Windows.Forms.TextBox txtSend;
        private System.Windows.Forms.CheckBox chkSendHex;
        private System.Windows.Forms.CheckBox chkSendCrlf;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.Button btnSendAll;
        private System.Windows.Forms.Panel pnlDataTool;
        private System.Windows.Forms.Label lblClient;
        private System.Windows.Forms.ComboBox cboClient;
        private System.Windows.Forms.CheckBox chkHexView;
        private System.Windows.Forms.Button btnClearData;
        private System.Windows.Forms.Button btnOpenLog;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel lblVersion;
        private System.Windows.Forms.ToolStripStatusLabel lblListenStatus;
        private System.Windows.Forms.ToolStripStatusLabel lblConfigPath;
        private System.Windows.Forms.Timer tmrRefresh;
    }
}
