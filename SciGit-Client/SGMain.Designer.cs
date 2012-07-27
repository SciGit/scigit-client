namespace SciGit_Client
{
    partial class SGMain
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
      this.components = new System.ComponentModel.Container();
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SGMain));
      this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
      this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
      this.openSciGitProjects = new System.Windows.Forms.ToolStripMenuItem();
      this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
      this.update = new System.Windows.Forms.ToolStripMenuItem();
      this.upload = new System.Windows.Forms.ToolStripMenuItem();
      this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
      this.exit = new System.Windows.Forms.ToolStripMenuItem();
      this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
      this.updateAll = new System.Windows.Forms.ToolStripMenuItem();
      this.uploadAll = new System.Windows.Forms.ToolStripMenuItem();
      this.contextMenuStrip.SuspendLayout();
      this.SuspendLayout();
      // 
      // notifyIcon
      // 
      this.notifyIcon.ContextMenuStrip = this.contextMenuStrip;
      this.notifyIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon.Icon")));
      this.notifyIcon.Text = "SciGit";
      this.notifyIcon.Visible = true;
      this.notifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon_MouseDoubleClick);
      // 
      // contextMenuStrip
      // 
      this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openSciGitProjects,
            this.toolStripSeparator1,
            this.update,
            this.upload,
            this.toolStripSeparator2,
            this.updateAll,
            this.uploadAll,
            this.toolStripSeparator3,
            this.exit});
      this.contextMenuStrip.Name = "contextMenuStrip";
      this.contextMenuStrip.Size = new System.Drawing.Size(191, 176);
      // 
      // openSciGitProjects
      // 
      this.openSciGitProjects.Name = "openSciGitProjects";
      this.openSciGitProjects.Size = new System.Drawing.Size(190, 22);
      this.openSciGitProjects.Text = "Open SciGit Projects...";
      this.openSciGitProjects.Click += new System.EventHandler(this.showAllFiles_Click);
      // 
      // toolStripSeparator1
      // 
      this.toolStripSeparator1.Name = "toolStripSeparator1";
      this.toolStripSeparator1.Size = new System.Drawing.Size(187, 6);
      // 
      // update
      // 
      this.update.Name = "update";
      this.update.Size = new System.Drawing.Size(190, 22);
      this.update.Text = "Update Project";
      this.update.Click += new System.EventHandler(this.update_Click);
      // 
      // upload
      // 
      this.upload.Name = "upload";
      this.upload.Size = new System.Drawing.Size(190, 22);
      this.upload.Text = "Upload Project";
      this.upload.Click += new System.EventHandler(this.upload_Click);
      // 
      // toolStripSeparator2
      // 
      this.toolStripSeparator2.Name = "toolStripSeparator2";
      this.toolStripSeparator2.Size = new System.Drawing.Size(187, 6);
      // 
      // exit
      // 
      this.exit.Name = "exit";
      this.exit.Size = new System.Drawing.Size(190, 22);
      this.exit.Text = "Exit";
      this.exit.Click += new System.EventHandler(this.exit_Click);
      // 
      // toolStripSeparator3
      // 
      this.toolStripSeparator3.Name = "toolStripSeparator3";
      this.toolStripSeparator3.Size = new System.Drawing.Size(187, 6);
      // 
      // updateAll
      // 
      this.updateAll.Name = "updateAll";
      this.updateAll.Size = new System.Drawing.Size(190, 22);
      this.updateAll.Text = "Update All";
      this.updateAll.Click += new System.EventHandler(this.updateAll_Click);
      // 
      // uploadAll
      // 
      this.uploadAll.Name = "uploadAll";
      this.uploadAll.Size = new System.Drawing.Size(190, 22);
      this.uploadAll.Text = "Upload All";
      this.uploadAll.Click += new System.EventHandler(this.uploadAll_Click);
      // 
      // SGMain
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(284, 262);
      this.Name = "SGMain";
      this.Opacity = 0D;
      this.ShowIcon = false;
      this.ShowInTaskbar = false;
      this.Text = "SGMain";
      this.Load += new System.EventHandler(this.SGMain_Load);
      this.contextMenuStrip.ResumeLayout(false);
      this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem openSciGitProjects;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem update;
        private System.Windows.Forms.ToolStripMenuItem upload;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem exit;
        private System.Windows.Forms.ToolStripMenuItem updateAll;
        private System.Windows.Forms.ToolStripMenuItem uploadAll;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
    }
}