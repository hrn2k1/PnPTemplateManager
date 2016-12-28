namespace ShareProv
{
    partial class frmMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.lblUserName = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblMsg = new System.Windows.Forms.ToolStripStatusLabel();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mitmConnect = new System.Windows.Forms.ToolStripMenuItem();
            this.btnExtract = new System.Windows.Forms.Button();
            this.btnApply = new System.Windows.Forms.Button();
            this.pgTemplate = new System.Windows.Forms.PropertyGrid();
            this.btnUploadTemplate = new System.Windows.Forms.Button();
            this.btnSaveTemplate = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.webBrowser1 = new System.Windows.Forms.WebBrowser();
            this.statusStrip1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(18, 18);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblUserName,
            this.lblMsg});
            this.statusStrip1.Location = new System.Drawing.Point(0, 563);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(886, 22);
            this.statusStrip1.TabIndex = 0;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // lblUserName
            // 
            this.lblUserName.Name = "lblUserName";
            this.lblUserName.Size = new System.Drawing.Size(59, 17);
            this.lblUserName.Text = "User Name";
            // 
            // lblMsg
            // 
            this.lblMsg.Name = "lblMsg";
            this.lblMsg.Size = new System.Drawing.Size(0, 17);
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(18, 18);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(886, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mitmConnect});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(35, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // mitmConnect
            // 
            this.mitmConnect.Name = "mitmConnect";
            this.mitmConnect.Size = new System.Drawing.Size(118, 24);
            this.mitmConnect.Text = "Connect";
            this.mitmConnect.Click += new System.EventHandler(this.mitmConnect_Click);
            // 
            // btnExtract
            // 
            this.btnExtract.Location = new System.Drawing.Point(23, 38);
            this.btnExtract.Name = "btnExtract";
            this.btnExtract.Size = new System.Drawing.Size(324, 34);
            this.btnExtract.TabIndex = 0;
            this.btnExtract.Text = "&Extract Template";
            this.btnExtract.UseVisualStyleBackColor = true;
            this.btnExtract.Click += new System.EventHandler(this.btnExtract_Click);
            // 
            // btnApply
            // 
            this.btnApply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnApply.Location = new System.Drawing.Point(23, 513);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(324, 34);
            this.btnApply.TabIndex = 0;
            this.btnApply.Text = "&Apply Template";
            this.btnApply.UseVisualStyleBackColor = true;
            // 
            // pgTemplate
            // 
            this.pgTemplate.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pgTemplate.CommandsVisibleIfAvailable = false;
            this.pgTemplate.HelpVisible = false;
            this.pgTemplate.Location = new System.Drawing.Point(23, 78);
            this.pgTemplate.Name = "pgTemplate";
            this.pgTemplate.PropertySort = System.Windows.Forms.PropertySort.NoSort;
            this.pgTemplate.Size = new System.Drawing.Size(837, 303);
            this.pgTemplate.TabIndex = 2;
            this.pgTemplate.ToolbarVisible = false;
            // 
            // btnUploadTemplate
            // 
            this.btnUploadTemplate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnUploadTemplate.Location = new System.Drawing.Point(536, 38);
            this.btnUploadTemplate.Name = "btnUploadTemplate";
            this.btnUploadTemplate.Size = new System.Drawing.Size(324, 34);
            this.btnUploadTemplate.TabIndex = 0;
            this.btnUploadTemplate.Text = "&Upload Template";
            this.btnUploadTemplate.UseVisualStyleBackColor = true;
            this.btnUploadTemplate.Click += new System.EventHandler(this.btnUploadTemplate_Click);
            // 
            // btnSaveTemplate
            // 
            this.btnSaveTemplate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSaveTemplate.Location = new System.Drawing.Point(536, 513);
            this.btnSaveTemplate.Name = "btnSaveTemplate";
            this.btnSaveTemplate.Size = new System.Drawing.Size(324, 34);
            this.btnSaveTemplate.TabIndex = 0;
            this.btnSaveTemplate.Text = "&Save Template";
            this.btnSaveTemplate.UseVisualStyleBackColor = true;
            this.btnSaveTemplate.Click += new System.EventHandler(this.btnSaveTemplate_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.DefaultExt = "pnp";
            this.openFileDialog1.Filter = "PnP files | *.pnp";
            this.openFileDialog1.RestoreDirectory = true;
            this.openFileDialog1.Title = "Select PnP file";
            // 
            // saveFileDialog1
            // 
            this.saveFileDialog1.DefaultExt = "pnp";
            this.saveFileDialog1.Filter = "pnp files| *.pnp";
            this.saveFileDialog1.RestoreDirectory = true;
            this.saveFileDialog1.Title = "Select .PnP file location";
            // 
            // webBrowser1
            // 
            this.webBrowser1.Location = new System.Drawing.Point(23, 387);
            this.webBrowser1.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowser1.Name = "webBrowser1";
            this.webBrowser1.Size = new System.Drawing.Size(837, 120);
            this.webBrowser1.TabIndex = 3;
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(886, 585);
            this.Controls.Add(this.webBrowser1);
            this.Controls.Add(this.pgTemplate);
            this.Controls.Add(this.btnSaveTemplate);
            this.Controls.Add(this.btnApply);
            this.Controls.Add(this.btnUploadTemplate);
            this.Controls.Add(this.btnExtract);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.Name = "frmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "SharePoint Provisioning";
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel lblUserName;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mitmConnect;
        private System.Windows.Forms.Button btnExtract;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.PropertyGrid pgTemplate;
        private System.Windows.Forms.Button btnUploadTemplate;
        private System.Windows.Forms.Button btnSaveTemplate;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.ToolStripStatusLabel lblMsg;
        private System.Windows.Forms.WebBrowser webBrowser1;
    }
}

