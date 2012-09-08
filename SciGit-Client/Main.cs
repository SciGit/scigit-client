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
using System.Reflection;

namespace SciGit_Client
{
  struct BalloonTip
  {
    public string title;
    public string message;
    public EventHandler onClick;
  }

  public partial class Main : Form
  {
    ProjectMonitor projectManager;
    Queue<BalloonTip> balloonTips;
    const int balloonTipTimeout = 3000;

    public Main() {
      InitializeComponent();
      InitializeContextMenu();
      InitializeSSH();

      balloonTips = new Queue<BalloonTip>();
      notifyIcon.BalloonTipClosed += BalloonTipClosed;
      notifyIcon.BalloonTipClicked += BalloonTipClicked;

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

    private void OpenDirectoryHandler(object sender, EventArgs e) {
      Process.Start(ProjectMonitor.GetProjectDirectory());
    }

    private EventHandler CreateOpenDirectoryHandler(Project p) {
      return (s, e) => Process.Start(ProjectMonitor.GetProjectDirectory(p));
    }

    private EventHandler CreateUpdateProjectHandler(Project p) {
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

    private EventHandler CreateUploadProjectHandler(Project p) {
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

    private void CreateUpdateAllHandler(object sender, EventArgs e) {
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

    private void CreateUploadAllHandler(object sender, EventArgs e) {
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

    private void ShowBalloonTip(object sender, EventArgs e) {
      lock (balloonTips) {
        BalloonTip bt = balloonTips.Peek();
        notifyIcon.ShowBalloonTip(balloonTipTimeout, bt.title, bt.message, ToolTipIcon.Info);
      }
    }

    private void BalloonTipClosed(object sender, EventArgs e) {
      lock (balloonTips) {
        balloonTips.Dequeue();
        if (balloonTips.Count > 0) {
          ShowBalloonTip(null, null);
        }
      }
    }

    private void BalloonTipClicked(object sender, EventArgs e) {
      lock (balloonTips) {
        BalloonTip bt = balloonTips.Peek();
        bt.onClick(sender, e);
        BalloonTipClosed(sender, e);
      }
    }

    private void QueueBalloonTip(string title, string message, EventHandler onClick) {
      lock (balloonTips) {
        balloonTips.Enqueue(new BalloonTip() { title = title, message = message, onClick = onClick });
        if (balloonTips.Count == 1) {
          ShowBalloonTip(null, null);
        }
      }
    }

    private void ProjectUpdated(Project p) {
      QueueBalloonTip("Project Updated",
        "Project " + p.Name + " has been updated. Click to update the local version...", CreateUpdateProjectHandler(p));
    }

    private void ProjectAdded(Project p) {
      QueueBalloonTip("Project Added",
        "Project " + p.Name + " has been added. Click to open the project folder...", CreateOpenDirectoryHandler(p));
    }

    // ContextMenuStrip (from the designer) is inconsistent with Windows context menus.
    // Use our own
    private void InitializeContextMenu() {
      notifyIcon.ContextMenu = new ContextMenu();
      var menuItem = new MenuItem("Open SciGit Projects...", OpenDirectoryHandler);
      menuItem.DefaultItem = true;
      notifyIcon.ContextMenu.MenuItems.Add(menuItem);
      notifyIcon.ContextMenu.MenuItems.Add("-");
      notifyIcon.ContextMenu.MenuItems.Add("Update Project");
      notifyIcon.ContextMenu.MenuItems.Add("Upload Project");
      notifyIcon.ContextMenu.MenuItems.Add("-");
      notifyIcon.ContextMenu.MenuItems.Add("Update All", CreateUpdateAllHandler);
      notifyIcon.ContextMenu.MenuItems.Add("Upload All", CreateUploadAllHandler);
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
          update.MenuItems.Add(new MenuItem(project.Name, CreateUpdateProjectHandler(curProject)));
          upload.MenuItems.Add(new MenuItem(project.Name, CreateUploadProjectHandler(curProject)));
        }
      }

      var updateAll = notifyIcon.ContextMenu.MenuItems[5];
      var uploadAll = notifyIcon.ContextMenu.MenuItems[6];
      update.Enabled = upload.Enabled = updateAll.Enabled = uploadAll.Enabled = projects.Count > 0;
    }

    private void OpenContextMenu(object sender, EventArgs e) {
      // Hack from StackOverflow: use reflection to get the internal context menu function
      MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
      mi.Invoke(notifyIcon, null);
    }

    private void InitializeSSH() {
      /* TODO:
       * - check Git/ssh installations
       */

      string homeDir = Environment.GetEnvironmentVariable("HOME");
      string keyFile = homeDir + @"\.ssh\id_rsa.pub";
      if (!File.Exists(keyFile)) {
        GitWrapper.GenerateSSHKey(keyFile);
      }

      string key = File.ReadAllText(keyFile).Trim();
      if (!RestClient.UploadPublicKey(key)) {
        FatalError("It appears that your public key is invalid. Please remove or regenerate it.");
      }

      // Add scigit server to known_hosts
      string knownHostsFile = homeDir + @"\.ssh\known_hosts";
      GitWrapper.RemoveHostSSHKey(GitWrapper.ServerHost);
      string hostKey = GitWrapper.GetHostSSHKey(GitWrapper.ServerHost).Output;
      var knownHostsHandle = File.Open(knownHostsFile, FileMode.Append);
      knownHostsHandle.Write(Encoding.ASCII.GetBytes(hostKey), 0, hostKey.Length);
      knownHostsHandle.Close();
    }
  }
}
