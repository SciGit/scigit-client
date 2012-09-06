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
using System.Windows.Threading;

namespace SciGit_Client
{
  public partial class SGMain : Form
  {
    SGProjectManager projectManager;

    public SGMain() {
      InitializeComponent();
      InitializeContextMenu();

      InitializeSSH();

      projectManager = new SGProjectManager();
      projectManager.projectUpdatedCallbacks.Add(ProjectUpdated);
      projectManager.StartMonitoring();
      UpdateContextMenu();
    }

    private void FatalError(string err) {
      MessageBox.Show(err, "Error");
      Environment.Exit(1);
    }

    private void OpenDirectory(object sender, EventArgs e) {
      Process.Start(SGProjectManager.GetProjectDirectory());
    }

    private EventHandler UpdateProject(Project p) {
      return (s, e) => {
        var disp = Dispatcher.CurrentDispatcher;
        ProgressForm form = new ProgressForm();
        form.Show();
        BackgroundWorker bg = new BackgroundWorker();
        bg.WorkerReportsProgress = true;
        bg.DoWork += (bw, _) => projectManager.UpdateProject(p, form, disp, (BackgroundWorker)bw);
        bg.ProgressChanged += form.UpdateProgress;
        bg.RunWorkerCompleted += form.Completed;
        bg.RunWorkerAsync();
      };
    }

    private EventHandler UploadProject(Project p) {
      return (s, e) => {
        var disp = Dispatcher.CurrentDispatcher;
        ProgressForm form = new ProgressForm();
        form.Show();
        BackgroundWorker bg = new BackgroundWorker();
        bg.WorkerReportsProgress = true;
        bg.DoWork += (bw, _) => projectManager.UploadProject(p, form, disp, (BackgroundWorker)bw);
        bg.ProgressChanged += form.UpdateProgress;
        bg.RunWorkerCompleted += form.Completed;
        bg.RunWorkerAsync();
      };
    }

    private void UpdateAll(object sender, EventArgs e) {
      var disp = Dispatcher.CurrentDispatcher;
      ProgressForm form = new ProgressForm();
      form.Show();
      BackgroundWorker bg = new BackgroundWorker();
      bg.WorkerReportsProgress = true;
      bg.DoWork += (bw, _) => projectManager.UpdateAllProjects(form, disp, (BackgroundWorker)bw);
      bg.ProgressChanged += form.UpdateProgress;
      bg.RunWorkerCompleted += form.Completed;
      bg.RunWorkerAsync();
    }

    private void UploadAll(object sender, EventArgs e) {
      var disp = Dispatcher.CurrentDispatcher;
      ProgressForm form = new ProgressForm();
      form.Show();
      BackgroundWorker bg = new BackgroundWorker();
      bg.WorkerReportsProgress = true;
      bg.DoWork += (bw, _) => projectManager.UploadAllProjects(form, disp, (BackgroundWorker)bw);
      bg.ProgressChanged += form.UpdateProgress;
      bg.RunWorkerCompleted += form.Completed;
      bg.RunWorkerAsync();
    }

    private void ExitClick(object sender, EventArgs e) {
      notifyIcon.Visible = false;
      Environment.Exit(0);
    }

    private void ProjectUpdated() {
      UpdateContextMenu();
    }

    // ContextMenuStrip (from the designer) is inconsistent with Windows context menus.
    // Use our own
    private void InitializeContextMenu() {
      this.notifyIcon.ContextMenu = new ContextMenu();
      var menuItem = new MenuItem("Open SciGit Projects...", OpenDirectory);
      menuItem.DefaultItem = true;
      this.notifyIcon.ContextMenu.MenuItems.Add(menuItem);
      this.notifyIcon.ContextMenu.MenuItems.Add("-");
      this.notifyIcon.ContextMenu.MenuItems.Add("Update Project");
      this.notifyIcon.ContextMenu.MenuItems.Add("Upload Project");
      this.notifyIcon.ContextMenu.MenuItems.Add("-");
      this.notifyIcon.ContextMenu.MenuItems.Add("Update All", UpdateAll);
      this.notifyIcon.ContextMenu.MenuItems.Add("Upload All", UploadAll);
      this.notifyIcon.ContextMenu.MenuItems.Add("-");
      this.notifyIcon.ContextMenu.MenuItems.Add("Exit", ExitClick);
    }

    private void UpdateContextMenu() {
      lock (projectManager.projects) {
        var update = this.notifyIcon.ContextMenu.MenuItems[2];
        var upload = this.notifyIcon.ContextMenu.MenuItems[3];
        HashSet<string> current = new HashSet<string>(from item in update.MenuItems.Cast<MenuItem>() select item.Text);
        HashSet<string> updated = new HashSet<string>(from p in projectManager.projects select p.Name);

        for (int i = update.MenuItems.Count - 1; i >= 0; i--) {
          MenuItem item = update.MenuItems[i];
          if (!updated.Contains(item.Text)) {
            update.MenuItems.Remove(item);
          }
        }

        for (int i = upload.MenuItems.Count - 1; i >= 0; i--) {
          MenuItem item = upload.MenuItems[i];
          if (!updated.Contains(item.Text)) {
            upload.MenuItems.Remove(item);
          }
        }

        foreach (var project in projectManager.projects) {
          if (!current.Contains(project.Name)) {
            var curProject = project; // closure issues
            update.MenuItems.Add(new MenuItem(project.Name, UpdateProject(curProject)));
            upload.MenuItems.Add(new MenuItem(project.Name, UploadProject(curProject)));
          }
        }

        if (projectManager.projects.Count == 0) {
          update.Enabled = false;
          upload.Enabled = false;
        } else {
          update.Enabled = true;
          upload.Enabled = true;
        }
      }
    }

    private string RunProcess(string filename, string args) {
      ProcessStartInfo startInfo = new ProcessStartInfo();
      startInfo.FileName = filename;
      startInfo.Arguments = args;
      startInfo.CreateNoWindow = true;
      startInfo.RedirectStandardOutput = true;
      startInfo.UseShellExecute = false;
      Process process = new Process();
      process.StartInfo = startInfo;
      process.Start();
      string output = process.StandardOutput.ReadToEnd();
      process.WaitForExit();
      return output;
    }

    private void InitializeSSH() {
      /* TODO:
       * - check Git/ssh installations
       */

      string homeDir = Environment.GetEnvironmentVariable("HOME");
      string keyFile = homeDir + @"\.ssh\id_rsa.pub";
      if (!File.Exists(keyFile)) {
        RunProcess("ssh-keygen.exe", String.Format("-t rsa -f '{0}' -P ''", keyFile));
      }

      string key = File.ReadAllText(keyFile).Trim();
      if (!SGRestClient.UploadPublicKey(key)) {
        FatalError("It appears that your public key is invalid. Please remove or regenerate it.");
      }

      // Add scigit server to known_hosts
      string knownHostsFile = homeDir + @"\.ssh\known_hosts";
      RunProcess("ssh-keygen.exe", "-R " + GitWrapper.ServerHost);
      string hostKey = RunProcess("ssh-keyscan.exe", "-t rsa " + GitWrapper.ServerHost);
      var knownHostsHandle = File.Open(knownHostsFile, FileMode.Append);
      knownHostsHandle.Write(Encoding.ASCII.GetBytes(hostKey), 0, hostKey.Length);
      knownHostsHandle.Close();
    }
  }
}
