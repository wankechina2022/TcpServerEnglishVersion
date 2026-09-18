namespace TcpServer.UI.Forms
{
    partial class frmAbout
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Cleans up all resources in use.
        /// </summary>
        /// <param name="disposing">True to release managed resources; otherwise false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        // ----------------------------------------------------------------------------
        // 2026-09-18 English edition - layout adjustments in this file:
        //   lblDescription : height 90 -> 210  (the longer English description needs ~11 lines)
        //   lblPaths       : Y 184 -> 300      (cleared by the taller lblDescription)
        //   btnClose       : Y 266 -> 412      (cleared by the repositioned lblPaths)
        //   frmAbout       : ClientSize height 312 -> 462 (to fit the grown labels above)
        // No control text, name, z-order or tab order was changed.
        // ----------------------------------------------------------------------------

        /// <summary>
        /// Designer support method - do not modify the body of this method.
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
            this.lblTitle.Text = "Multi-Port TCP Listener Debugger";
            // 
            // lblVersion
            // 
            this.lblVersion.Location = new System.Drawing.Point(22, 52);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(420, 20);
            this.lblVersion.TabIndex = 1;
            this.lblVersion.Text = "Version";
            // 
            // lblDescription
            // 
            this.lblDescription.Location = new System.Drawing.Point(22, 84);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(420, 210);
            this.lblDescription.TabIndex = 2;
            this.lblDescription.Text = "Description";
            // 
            // lblPaths
            // 
            this.lblPaths.Location = new System.Drawing.Point(22, 300);
            this.lblPaths.Name = "lblPaths";
            this.lblPaths.Size = new System.Drawing.Size(420, 90);
            this.lblPaths.TabIndex = 3;
            this.lblPaths.Text = "Paths";
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(350, 412);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(90, 30);
            this.btnClose.TabIndex = 4;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            // 
            // frmAbout
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(464, 462);
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
            this.Text = "About";
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
