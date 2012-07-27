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

    private void UpdateProject(Project p) {
      Directory.SetCurrentDirectory(SGProjectManager.GetProjectDirectory(p));
      GitWrapper.Pull();
    }

    private void UploadProject(Project p) {
      Directory.SetCurrentDirectory(SGProjectManager.GetProjectDirectory(p));
      GitWrapper.Add(".");
      if (GitWrapper.Commit(DateTime.Now + " commit")) {
        GitWrapper.Pull();
        GitWrapper.Push();
      }
    }

    private void InitializeProjects() {
      projectManager = new SGProjectManager(SGRestClient.GetProjects());
      var update = (ToolStripMenuItem)contextMenuStrip.Items["update"];
      var upload = (ToolStripMenuItem)contextMenuStrip.Items["upload"];
      foreach (var project in projectManager.Projects) {
        var thisProject = project; // avoid closure issues
        update.DropDownItems.Add(new ToolStripMenuItem(project.Name, null, (sender, e) => UpdateProject(thisProject), ""));
        upload.DropDownItems.Add(new ToolStripMenuItem(project.Name, null, (sender, e) => UploadProject(thisProject), ""));
      }

      if (projectManager.Projects.Count == 0) {
        update.Enabled = false;
        upload.Enabled = false;
      }
    }

    private void updateAll_Click(object sender, EventArgs e) {
      foreach (var project in projectManager.Projects) {
        UpdateProject(project);
      }
    }

    private void uploadAll_Click(object sender, EventArgs e) {
      foreach (var project in projectManager.Projects) {
        UploadProject(project);
      }
    }
  }
}
