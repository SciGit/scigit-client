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
  public partial class Main : Form
  {
    ProjectMonitor projectManager;

    public Main() {
      InitializeComponent();
      InitializeContextMenu();
      InitializeSSH();

      projectManager = new ProjectMonitor();
      projectManager.updateCallbacks.Add(UpdateContextMenu);
      projectManager.projectAddedCallbacks.Add(ProjectAdded);
      projectManager.projectUpdatedCallbacks.Add(ProjectUpdated);
      projectManager.StartMonitoring();
    }

    private void FatalError(string err) {
      MessageBox.Show(err, "Error");
      Environment.Exit(1);
    }

    private void OpenDirectory(object sender, EventArgs e) {
      Process.Start(ProjectMonitor.GetProjectDirectory());
    }

    private EventHandler UpdateProjectHandler(Project p) {
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

    private EventHandler UploadProjectHandler(Project p) {
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

    private void UpdateAllHandler(object sender, EventArgs e) {
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

    private void UploadAllHandler(object sender, EventArgs e) {
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

    private void ProjectUpdated(Project p) {
    }

    private void ProjectAdded(Project p) {
    }

    // ContextMenuStrip (from the designer) is inconsistent with Windows context menus.
    // Use our own
    private void InitializeContextMenu() {
      notifyIcon.ContextMenu = new ContextMenu();
      var menuItem = new MenuItem("Open SciGit Projects...", OpenDirectory);
      menuItem.DefaultItem = true;
      notifyIcon.ContextMenu.MenuItems.Add(menuItem);
      notifyIcon.ContextMenu.MenuItems.Add("-");
      notifyIcon.ContextMenu.MenuItems.Add("Update Project");
      notifyIcon.ContextMenu.MenuItems.Add("Upload Project");
      notifyIcon.ContextMenu.MenuItems.Add("-");
      notifyIcon.ContextMenu.MenuItems.Add("Update All", UpdateAllHandler);
      notifyIcon.ContextMenu.MenuItems.Add("Upload All", UploadAllHandler);
      notifyIcon.ContextMenu.MenuItems.Add("-");
      notifyIcon.ContextMenu.MenuItems.Add("Exit", ExitClick);
      notifyIcon.ContextMenu.MenuItems[2].Enabled =
        notifyIcon.ContextMenu.MenuItems[3].Enabled =
          notifyIcon.ContextMenu.MenuItems[5].Enabled =
            notifyIcon.ContextMenu.MenuItems[6].Enabled = false;
    }

    private void UpdateContextMenu() {
      List<Project> projects = projectManager.GetProjects();
      var update = notifyIcon.ContextMenu.MenuItems[2];
      var upload = notifyIcon.ContextMenu.MenuItems[3];
      HashSet<string> current = new HashSet<string>(from item in update.MenuItems.Cast<MenuItem>() select item.Text);
      HashSet<string> updated = new HashSet<string>(from p in projects select p.Name);

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

      foreach (var project in projects) {
        if (!current.Contains(project.Name)) {
          var curProject = project; // closure issues
          update.MenuItems.Add(new MenuItem(project.Name, UpdateProjectHandler(curProject)));
          upload.MenuItems.Add(new MenuItem(project.Name, UploadProjectHandler(curProject)));
        }
      }

      var updateAll = notifyIcon.ContextMenu.MenuItems[5];
      var uploadAll = notifyIcon.ContextMenu.MenuItems[6];
      update.Enabled = upload.Enabled = updateAll.Enabled = uploadAll.Enabled = projects.Count > 0;
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
      if (!RestClient.UploadPublicKey(key)) {
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
