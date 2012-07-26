using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace SciGit_Client
{
  public partial class SGMain : Form
  {
    SGProjectManager projectManager;

    public SGMain() {
      InitializeComponent();
      InitializeProjects();
    }

    private void SGMain_Load(object sender, EventArgs e) {
    }

    private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e) {

    }

    private void showAllFiles_Click(object sender, EventArgs e) {
      Process.Start(SGProjectManager.GetProjectDirectory());
    }

    private void update_Click(object sender, EventArgs e) {

    }

    private void upload_Click(object sender, EventArgs e) {

    }

    private void exit_Click(object sender, EventArgs e) {
      Environment.Exit(0);
    }

    private void InitializeProjects() {
      projectManager = new SGProjectManager(SGRestClient.GetProjects());
      var update = (ToolStripMenuItem)contextMenuStrip.Items["update"];
      var upload = (ToolStripMenuItem)contextMenuStrip.Items["upload"];
      foreach (var project in projectManager.Projects) {
        update.DropDownItems.Add(new ToolStripMenuItem(project.Name, null, null, ""));
        upload.DropDownItems.Add(new ToolStripMenuItem(project.Name, null, null, ""));
      }

      if (projectManager.Projects.Count == 0) {
        update.Enabled = false;
        upload.Enabled = false;
      }
    }
  }
}
