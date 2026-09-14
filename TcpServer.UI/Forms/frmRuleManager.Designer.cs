namespace TcpServer.UI.Forms
{
    partial class frmRuleManager
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
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改此方法的内容
        /// </summary>
        private void InitializeComponent()
        {
            this.dgvRules = new System.Windows.Forms.DataGridView();
            this.colRuleName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colEnabled = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colMatchText = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMatchAsHex = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colMatchExactly = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colReplyText = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colReplyAsHex = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colDelayMs = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colOnlyForPort = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.pnlTip = new System.Windows.Forms.Panel();
            this.lblTip = new System.Windows.Forms.Label();
            this.pnlButtons = new System.Windows.Forms.Panel();
            this.btnAdd = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnMoveUp = new System.Windows.Forms.Button();
            this.btnMoveDown = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgvRules)).BeginInit();
            this.pnlTip.SuspendLayout();
            this.pnlButtons.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgvRules
            // 
            this.dgvRules.AllowUserToAddRows = false;
            this.dgvRules.AllowUserToDeleteRows = false;
            this.dgvRules.AllowUserToResizeRows = false;
            this.dgvRules.BackgroundColor = System.Drawing.Color.White;
            this.dgvRules.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgvRules.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colRuleName,
            this.colEnabled,
            this.colMatchText,
            this.colMatchAsHex,
            this.colMatchExactly,
            this.colReplyText,
            this.colReplyAsHex,
            this.colDelayMs,
            this.colOnlyForPort});
            this.dgvRules.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvRules.Location = new System.Drawing.Point(0, 34);
            this.dgvRules.MultiSelect = false;
            this.dgvRules.Name = "dgvRules";
            this.dgvRules.RowHeadersVisible = false;
            this.dgvRules.RowTemplate.Height = 25;
            this.dgvRules.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvRules.Size = new System.Drawing.Size(1000, 420);
            this.dgvRules.TabIndex = 1;
            // 
            // colRuleName
            // 
            this.colRuleName.DataPropertyName = "RuleName";
            this.colRuleName.HeaderText = "Rule Name";
            this.colRuleName.Name = "colRuleName";
            this.colRuleName.Width = 130;
            // 
            // colEnabled
            // 
            this.colEnabled.DataPropertyName = "Enabled";
            this.colEnabled.HeaderText = "Enabled";
            this.colEnabled.Name = "colEnabled";
            this.colEnabled.Width = 70;
            // 
            // colMatchText
            // 
            this.colMatchText.DataPropertyName = "MatchText";
            this.colMatchText.HeaderText = "Match";
            this.colMatchText.Name = "colMatchText";
            this.colMatchText.Width = 160;
            // 
            // colMatchAsHex
            // 
            this.colMatchAsHex.DataPropertyName = "MatchAsHex";
            this.colMatchAsHex.HeaderText = "Match HEX";
            this.colMatchAsHex.Name = "colMatchAsHex";
            this.colMatchAsHex.Width = 80;
            // 
            // colMatchExactly
            // 
            this.colMatchExactly.DataPropertyName = "MatchExactly";
            this.colMatchExactly.HeaderText = "Exact Match";
            this.colMatchExactly.Name = "colMatchExactly";
            this.colMatchExactly.Width = 100;
            // 
            // colReplyText
            // 
            this.colReplyText.DataPropertyName = "ReplyText";
            this.colReplyText.HeaderText = "Reply";
            this.colReplyText.Name = "colReplyText";
            this.colReplyText.Width = 160;
            // 
            // colReplyAsHex
            // 
            this.colReplyAsHex.DataPropertyName = "ReplyAsHex";
            this.colReplyAsHex.HeaderText = "Reply HEX";
            this.colReplyAsHex.Name = "colReplyAsHex";
            this.colReplyAsHex.Width = 80;
            // 
            // colDelayMs
            // 
            this.colDelayMs.DataPropertyName = "DelayMs";
            this.colDelayMs.HeaderText = "Delay (ms)";
            this.colDelayMs.Name = "colDelayMs";
            this.colDelayMs.Width = 95;
            // 
            // colOnlyForPort
            // 
            this.colOnlyForPort.DataPropertyName = "OnlyForPort";
            this.colOnlyForPort.HeaderText = "Port Only";
            this.colOnlyForPort.Name = "colOnlyForPort";
            this.colOnlyForPort.Width = 85;
            // 
            // pnlTip
            // 
            this.pnlTip.Controls.Add(this.lblTip);
            this.pnlTip.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTip.Location = new System.Drawing.Point(0, 0);
            this.pnlTip.Name = "pnlTip";
            this.pnlTip.Size = new System.Drawing.Size(1000, 34);
            this.pnlTip.TabIndex = 0;
            // 
            // lblTip
            // 
            this.lblTip.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTip.Location = new System.Drawing.Point(0, 0);
            this.lblTip.Name = "lblTip";
            this.lblTip.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblTip.Size = new System.Drawing.Size(1000, 34);
            this.lblTip.TabIndex = 0;
            this.lblTip.Text = "Tip: edit directly in the grid. Matching is \"contains\" by default (check \"Exact Match\" to require a full match). Set Port Only to 0 to apply to all ports.";
            this.lblTip.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pnlButtons
            // 
            this.pnlButtons.Controls.Add(this.btnCancel);
            this.pnlButtons.Controls.Add(this.btnOk);
            this.pnlButtons.Controls.Add(this.btnMoveDown);
            this.pnlButtons.Controls.Add(this.btnMoveUp);
            this.pnlButtons.Controls.Add(this.btnDelete);
            this.pnlButtons.Controls.Add(this.btnAdd);
            this.pnlButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlButtons.Location = new System.Drawing.Point(0, 454);
            this.pnlButtons.Name = "pnlButtons";
            this.pnlButtons.Size = new System.Drawing.Size(1000, 50);
            this.pnlButtons.TabIndex = 2;
            // 
            // btnAdd
            // 
            this.btnAdd.Location = new System.Drawing.Point(12, 10);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(80, 30);
            this.btnAdd.TabIndex = 0;
            this.btnAdd.Text = "Add Rule";
            this.btnAdd.UseVisualStyleBackColor = true;
            // 
            // btnDelete
            // 
            this.btnDelete.Location = new System.Drawing.Point(100, 10);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(90, 30);
            this.btnDelete.TabIndex = 1;
            this.btnDelete.Text = "Delete Rule";
            this.btnDelete.UseVisualStyleBackColor = true;
            // 
            // btnMoveUp
            // 
            this.btnMoveUp.Location = new System.Drawing.Point(198, 10);
            this.btnMoveUp.Name = "btnMoveUp";
            this.btnMoveUp.Size = new System.Drawing.Size(70, 30);
            this.btnMoveUp.TabIndex = 2;
            this.btnMoveUp.Text = "Move Up";
            this.btnMoveUp.UseVisualStyleBackColor = true;
            // 
            // btnMoveDown
            // 
            this.btnMoveDown.Location = new System.Drawing.Point(276, 10);
            this.btnMoveDown.Name = "btnMoveDown";
            this.btnMoveDown.Size = new System.Drawing.Size(80, 30);
            this.btnMoveDown.TabIndex = 3;
            this.btnMoveDown.Text = "Move Down";
            this.btnMoveDown.UseVisualStyleBackColor = true;
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(810, 10);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(84, 30);
            this.btnOk.TabIndex = 4;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(904, 10);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(84, 30);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // frmRuleManager
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 504);
            this.Controls.Add(this.dgvRules);
            this.Controls.Add(this.pnlButtons);
            this.Controls.Add(this.pnlTip);
            this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(900, 500);
            this.Name = "frmRuleManager";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Auto Reply Rules";
            ((System.ComponentModel.ISupportInitialize)(this.dgvRules)).EndInit();
            this.pnlTip.ResumeLayout(false);
            this.pnlButtons.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.DataGridView dgvRules;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRuleName;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colEnabled;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMatchText;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colMatchAsHex;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colMatchExactly;
        private System.Windows.Forms.DataGridViewTextBoxColumn colReplyText;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colReplyAsHex;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDelayMs;
        private System.Windows.Forms.DataGridViewTextBoxColumn colOnlyForPort;
        private System.Windows.Forms.Panel pnlTip;
        private System.Windows.Forms.Label lblTip;
        private System.Windows.Forms.Panel pnlButtons;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnMoveUp;
        private System.Windows.Forms.Button btnMoveDown;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
    }
}
