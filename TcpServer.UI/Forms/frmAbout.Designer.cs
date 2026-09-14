namespace TcpServer.UI.Forms
{
    partial class frmAbout
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
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblVersion = new System.Windows.Forms.Label();
            this.lblDescription = new System.Windows.Forms.Label();
            this.lblPaths = new System.Windows.Forms.Label();
            this.btnClose = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.Font = new System.Drawing.Font("Microsoft YaHei UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(20, 18);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(420, 30);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "多端口 TCP 监听调试工具";
            // 
            // lblVersion
            // 
            this.lblVersion.Location = new System.Drawing.Point(22, 52);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(420, 20);
            this.lblVersion.TabIndex = 1;
            this.lblVersion.Text = "版本";
            // 
            // lblDescription
            // 
            this.lblDescription.Location = new System.Drawing.Point(22, 84);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(420, 90);
            this.lblDescription.TabIndex = 2;
            this.lblDescription.Text = "说明";
            // 
            // lblPaths
            // 
            this.lblPaths.Location = new System.Drawing.Point(22, 184);
            this.lblPaths.Name = "lblPaths";
            this.lblPaths.Size = new System.Drawing.Size(420, 70);
            this.lblPaths.TabIndex = 3;
            this.lblPaths.Text = "路径";
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(350, 266);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(90, 30);
            this.btnClose.TabIndex = 4;
            this.btnClose.Text = "关闭";
            this.btnClose.UseVisualStyleBackColor = true;
            // 
            // frmAbout
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(464, 312);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.lblPaths);
            this.Controls.Add(this.lblDescription);
            this.Controls.Add(this.lblVersion);
            this.Controls.Add(this.lblTitle);
            this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmAbout";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "关于";
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblVersion;
        private System.Windows.Forms.Label lblDescription;
        private System.Windows.Forms.Label lblPaths;
        private System.Windows.Forms.Button btnClose;
    }
}
