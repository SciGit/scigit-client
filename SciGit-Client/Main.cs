﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Threading;

namespace SciGit_Client
{
  struct BalloonTip
  {
    public string message;
    public EventHandler onClick;
    public string title;
  }

  public partial class Main : Form
  {
    const int balloonTipTimeout = 3000;
    bool loaded = false;
    Queue<BalloonTip> balloonTips;
    Icon notifyIconBase, notifyIconLoading, notifyIconUpdate;
    NamedPipeServerStream pipeServer;
    ProjectMonitor projectMonitor;

    public Main() {
      InitializeComponent();
      var resources = new ComponentResourceManager(typeof(Main));
      notifyIconBase = notifyIcon.Icon;
      notifyIconLoading = (Icon)resources.GetObject("notifyIconLoading.Icon");
      notifyIconUpdate = (Icon)resources.GetObject("notifyIconUpdate.Icon");

      InitializeContextMenu();
      InitializeSSH();
      
      balloonTips = new Queue<BalloonTip>();
      notifyIcon.BalloonTipClosed += BalloonTipClosed;
      notifyIcon.BalloonTipClicked += BalloonTipClicked;

      projectMonitor = new ProjectMonitor();
      projectMonitor.updateCallbacks.Add(UpdateContextMenu);
      projectMonitor.projectAddedCallbacks.Add(ProjectAdded);
      projectMonitor.projectRemovedCallbacks.Add(ProjectRemoved);
      projectMonitor.projectUpdatedCallbacks.Add(ProjectUpdated);
      projectMonitor.loadedCallbacks.Add(OnProjectMonitorLoaded);
      projectMonitor.StartMonitoring();
    }

    private void HandleCommand(string verb, string filename) {
      Project p = projectMonitor.GetProjectFromFilename(ref filename);
      if (p.Id == 0) {
        MessageBox.Show("This file does not belong to a valid SciGit project.", "Invalid SciGit file");
      } else if (verb == "--versions") {
        this.Invoke(new Action(() => OpenFileHistory(p, filename)));
      } else if (verb == "--update") {
        this.Invoke(CreateUpdateProjectHandler(p), new object[] { null, null });
      } else if (verb == "--upload") {
        this.Invoke(CreateUploadProjectHandler(p), new object[] { null, null });
      } else if (verb == "--pversions") {
        this.Invoke(new Action(() => OpenProjectHistory(p)));
      }
    }

    private void OnProjectMonitorLoaded() {
      loaded = true;
      lock (balloonTips) {
        if (balloonTips.Count > 0) {
          ShowBalloonTip(null, null);
        }
      }

      string[] args = Environment.GetCommandLineArgs();
      if (args.Length == 3) {
        HandleCommand(args[1], args[2]);
      }

      pipeServer = new NamedPipeServerStream("sciGitPipe", PipeDirection.In, 2);
      var t = new Thread(() => {
        while (true) {
          pipeServer.WaitForConnection();
          try {
            var ss = new StreamString(pipeServer);
            string verb = ss.ReadString();
            string filename = ss.ReadString();
            HandleCommand(verb, filename);
          } catch (Exception e) {
            Logger.LogException(e);
          }
          pipeServer.Disconnect();
        }
      });
      t.Start();
    }

    private void OpenFileHistory(Project p, string filename) {
      string dir = ProjectMonitor.GetProjectDirectory(p);
      if (!File.Exists(Path.Combine(dir, filename))) {
        MessageBox.Show("File does not exist.", "Error");
      } else {
        FileHistory fh = null;
        try {
          fh = new FileHistory(p, filename);
          fh.Show();
        } catch (InvalidRepositoryException) {
          if (fh != null) fh.Hide();
          MessageBox.Show("This file does not belong to a valid SciGit project.", "Error");
        } catch (Exception e) {
          if (fh != null) fh.Hide();
          ErrorForm.Show(e);
        }
      }
    }

    private void OpenProjectHistory(Project p) {
      string dir = ProjectMonitor.GetProjectDirectory(p);
      if (!Directory.Exists(Path.Combine(dir))) {
        MessageBox.Show("Project does not exist.", "Error");
      } else {
        ProjectHistory ph = null;
        try {
          ph = new ProjectHistory(p);
          ph.Show();
        } catch (InvalidRepositoryException) {
          if (ph != null) ph.Hide();
          MessageBox.Show("This file does not belong to a valid SciGit project.", "Error");
        } catch (Exception e) {
          if (ph != null) ph.Hide();
          ErrorForm.Show(e);
        }
      }
    }

    private void OpenDirectoryHandler(object sender, EventArgs e) {
      Process.Start(ProjectMonitor.GetProjectDirectory());
    }

    private void ManageProjectsHandler(object sender, EventArgs e) {
      Process.Start("http://" + RestClient.serverHost + "/projects");
    }

    private void NotifyClick(object sender, EventArgs e) {
      var me = (MouseEventArgs)e;
      if (me.Button == MouseButtons.Left) {
        OpenDirectoryHandler(sender, e);
      }
    }

    private EventHandler CreateOpenDirectoryHandler(Project p) {
      return (s, e) => Process.Start(ProjectMonitor.GetProjectDirectory(p));
    }

    private EventHandler CreateUpdateProjectHandler(Project p) {
      return (s, e) => {
        var progressForm = new ProgressForm((form, bw) => projectMonitor.UpdateProject(p, form, bw));
        progressForm.Show();
      };
    }

    private EventHandler CreateUploadProjectHandler(Project p) {
      return (s, e) => {
        var progressForm = new ProgressForm((form, bw) => projectMonitor.UploadProject(p, form, bw));
        progressForm.Show();
      };
    }

    private void CreateUpdateAllHandler(object sender, EventArgs e) {
      var progressForm = new ProgressForm(projectMonitor.UpdateAllProjects);
      progressForm.Show();
    }

    private void CreateUploadAllHandler(object sender, EventArgs e) {
      var progressForm = new ProgressForm(projectMonitor.UploadAllProjects);
      progressForm.Show();
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
        balloonTips.Enqueue(new BalloonTip { title = title, message = message, onClick = onClick });
        if (balloonTips.Count == 1 && loaded) {
          ShowBalloonTip(null, null);
        }
      }
    }

    private void ProjectAdded(Project p) {
      QueueBalloonTip("Project Added",
        "Project " + p.Name + (p.CanWrite ? "" : " (read-only)") +
        " has been added. Click to open the project folder...", CreateOpenDirectoryHandler(p));
    }

    private void ProjectRemoved(Project p) {
      QueueBalloonTip("Project Removed",
        "You are no longer receiving updates for project " + p.Name + ". Click to open the project folder...", CreateOpenDirectoryHandler(p));
    }

    private void ProjectUpdated(Project p) {
      QueueBalloonTip("Project Updated",
        "Project " + p.Name + " has been updated. Click to update the local version...", CreateUpdateProjectHandler(p));
    }

    // ContextMenuStrip (from the designer) is inconsistent with Windows context menus.
    // Use our own
    private void InitializeContextMenu() {
      notifyIcon.ContextMenu = new ContextMenu();
      var menuItem = new MenuItem("Open SciGit projects...", OpenDirectoryHandler);
      menuItem.DefaultItem = true;
      notifyIcon.ContextMenu.MenuItems.Add(menuItem);
      notifyIcon.ContextMenu.MenuItems.Add("Manage SciGit projects...", ManageProjectsHandler);
      notifyIcon.ContextMenu.MenuItems.Add("-");
      notifyIcon.ContextMenu.MenuItems.Add("Update Project").Enabled = false;
      notifyIcon.ContextMenu.MenuItems.Add("Upload Project").Enabled = false;
      notifyIcon.ContextMenu.MenuItems.Add("-");
      notifyIcon.ContextMenu.MenuItems.Add("Update All", CreateUpdateAllHandler).Enabled = false;
      notifyIcon.ContextMenu.MenuItems.Add("Upload All", CreateUploadAllHandler).Enabled = false;
      notifyIcon.ContextMenu.MenuItems.Add("Loading...").Enabled = false;
      notifyIcon.ContextMenu.MenuItems.Add("-");
      notifyIcon.ContextMenu.MenuItems.Add("Exit", ExitClick);

      // Just show the loading indicator for now
      notifyIcon.Icon = notifyIconLoading;
      for (int i = 3; i <= 7; i++) {
        notifyIcon.ContextMenu.MenuItems[i].Visible = false;
      }
    }

    private void UpdateContextMenu() {
      List<Project> projects = projectMonitor.GetProjects();
      List<Project> updatedProjects = projectMonitor.GetUpdatedProjects();
      var update = notifyIcon.ContextMenu.MenuItems[3];
      var upload = notifyIcon.ContextMenu.MenuItems[4];      
      var curNames = new HashSet<string>(from item in update.MenuItems.Cast<MenuItem>() select item.Text);
      var newNames = new HashSet<string>(from p in projects select p.Name);
      var updNames = new HashSet<string>(from p in updatedProjects select p.Name);
      var writeNames = new HashSet<string>(from p in projects where p.CanWrite select p.Name);

      for (int i = update.MenuItems.Count - 1; i >= 0; i--) {
        MenuItem item = update.MenuItems[i];
        item.Checked = updNames.Contains(item.Text);
        if (!newNames.Contains(item.Text)) {
          update.MenuItems.Remove(item);
        }
      }

      const string readOnlySuffix = " (read-only)";
      for (int i = upload.MenuItems.Count - 1; i >= 0; i--) {
        MenuItem item = upload.MenuItems[i];
        string projName = item.Text;
        if (projName.EndsWith(readOnlySuffix)) {
          projName = projName.Substring(0, projName.Length - readOnlySuffix.Length);
        }
        if (!newNames.Contains(projName)) {
          upload.MenuItems.Remove(item);
        } else {
          bool canWrite = writeNames.Contains(projName); 
          item.Enabled = canWrite;
          item.Text = projName + (canWrite ? "" : readOnlySuffix);
        }
      }

      foreach (var project in projects) {
        if (!curNames.Contains(project.Name)) {
          var curProject = project; // closure issues
          var item = new MenuItem(project.Name, CreateUpdateProjectHandler(curProject)) {
            Checked = updNames.Contains(project.Name),
            RadioCheck = true
          };
          update.MenuItems.Add(item);
          item = new MenuItem(project.Name + (project.CanWrite ? "" : readOnlySuffix),
                              CreateUploadProjectHandler(curProject)) {
            Enabled = project.CanWrite
          };
          upload.MenuItems.Add(item);
        }
      }

      var updateAll = notifyIcon.ContextMenu.MenuItems[6];
      updateAll.Text = "Update All" + (updNames.Count > 0 ? String.Format(" ({0})", updNames.Count) : "");

      var uploadAll = notifyIcon.ContextMenu.MenuItems[7];
      update.Enabled = upload.Enabled = updateAll.Enabled = projects.Count > 0;
      uploadAll.Enabled = writeNames.Count > 0;

      // Hide the loading indicator and show others
      notifyIcon.Icon = updNames.Count > 0 ? notifyIconUpdate : notifyIconBase;
      for (int i = 3; i <= 7; i++) {
        notifyIcon.ContextMenu.MenuItems[i].Visible = true;
      }
      notifyIcon.ContextMenu.MenuItems[8].Visible = false;
    }

    private void InitializeSSH() {
      // TODO: check Git/SSH installations

      string appPath = Path.Combine(GitWrapper.GetAppDataPath(), RestClient.Username);
      Directory.CreateDirectory(appPath);

      string sshDir = Path.Combine(appPath, ".ssh");
      Directory.CreateDirectory(sshDir);

      string keyFile = Path.Combine(sshDir, "id_rsa");
      if (!File.Exists(keyFile + ".pub")) {
        GitWrapper.GenerateSSHKey(keyFile);
      }

      GitWrapper.GlobalConfig("user.name", RestClient.Username);
      GitWrapper.GlobalConfig("user.email", RestClient.Username);

      string key = File.ReadAllText(keyFile + ".pub").Trim();
      bool? uploadResult = RestClient.UploadPublicKey(key);
      if (uploadResult != true) {
        MessageBox.Show(uploadResult == false ?
          "It appears that your public key is invalid. Please remove or regenerate it." :
          "Could not connect to the SciGit server. Please try again later.", "Error");
        // TODO: add ability to regenerate
        Environment.Exit(1);
      }

      // Add scigit server to known_hosts
      string knownHostsFile = Path.Combine(sshDir, "known_hosts");
      GitWrapper.RemoveHostSSHKey(GitWrapper.ServerHost);
      string hostKey = GitWrapper.GetHostSSHKey(GitWrapper.ServerHost).Output;
      if (hostKey.Contains('\n')) {
        hostKey = hostKey.Substring(0, hostKey.IndexOf('\n'));
      }
      hostKey += "\n";
      var knownHostsHandle = File.Open(knownHostsFile, FileMode.Append);
      knownHostsHandle.Write(Encoding.ASCII.GetBytes(hostKey), 0, hostKey.Length);
      knownHostsHandle.Close();
    }
  }
}
