﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using SciGit_Client.Properties;
using MessageBox = System.Windows.Forms.MessageBox;
using MessageBoxOptions = System.Windows.Forms.MessageBoxOptions;

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
    private const int balloonTipTimeout = 3000;
    private bool loaded = false;
    private Queue<BalloonTip> balloonTips;
    private Icon notifyIconBase, notifyIconLoading, notifyIconUpdate;
    private ShellCommandHandler shellCmdHandler;
    private ProjectMonitor projectMonitor;
    private UpdateChecker updateChecker;
    private Login loginWindow;

    public Main(Login loginWindow) {
      this.loginWindow = loginWindow;

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
      projectMonitor.projectEditedCallbacks.Add(ProjectEdited);
      projectMonitor.loadedCallbacks.Add(OnProjectMonitorLoaded);
      projectMonitor.failureCallbacks.Add(OnProjectMonitorFailure);
      projectMonitor.disconnectCallbacks.Add(OnProjectMonitorDisconnect);
      projectMonitor.StartMonitoring();

      updateChecker = new UpdateChecker();
      updateChecker.Start();

      // Show the intro if it's the first load.
      if (!Settings.Default.Loaded) {
        GettingStarted gs = new GettingStarted();
        ShowTop(gs);
        Settings.Default.Loaded = true;
        Settings.Default.Save();
      }
    }

    // Simple hack to bring a window to the front.
    private void ShowTop(Window w) {
      w.Show();
      w.Topmost = true;
      w.Topmost = false;
    }

    private void HandleWebCommand(string url) {
      var uri = new Uri(url);
      try {
        if (uri.Host == "view_change") {
          NameValueCollection query = HttpUtility.ParseQueryString(uri.Query);
          string project_id = query["project_id"];
          Project p = projectMonitor.GetProjectById(int.Parse(project_id));
          if (p.id == 0) {
            Util.ShowMessageBox("You don't seem to have access to that project.", "Invalid SciGit project");
            return;
          }
          string hash = query["commit_hash"];
          string filename = query["filename"];
          if (filename != null) {
            this.Invoke(new Action(() => OpenFileHistory(p, filename, hash)));
          } else {
            this.Invoke(new Action(() => OpenProjectHistory(p, hash)));
          }
        } else {
          Util.ShowMessageBox("Invalid link provided.", "Invalid SciGit command");
        }
      } catch (Exception ex) {
        Logger.LogException(ex);
        Util.ShowMessageBox("Invalid link provided.", "Invalid SciGit command");
      }
    }

    private void HandleCommand(string verb, string filename) {
      if (verb == "--url") {
        HandleWebCommand(filename);
        return;
      }

      Project p = projectMonitor.GetProjectFromFilename(ref filename);
      if (p.id == 0) {
        Util.ShowMessageBox("This file does not belong to a valid SciGit project.", "Invalid SciGit file");
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

      shellCmdHandler = new ShellCommandHandler(HandleCommand);
      shellCmdHandler.Start();
    }

    private void OnProjectMonitorFailure() {
      this.Invoke(new Action(() => {
        Util.ShowMessageBox("Could not authenticate with the SciGit server. Please log in again.", "Error");
        Logout();
      }));
    }

    private void OnProjectMonitorDisconnect() {
      this.Invoke(new Action(() => {
        notifyIcon.Text = "SciGit\nWaiting for connection...";
        notifyIcon.Icon = notifyIconLoading;
      }));
    }

    private void OpenFileHistory(Project p, string filename, string hash = null) {
      if (!projectMonitor.CheckProject(p)) return;

      string dir = ProjectMonitor.GetProjectDirectory(p);
      if (!File.Exists(Util.PathCombine(dir, filename))) {
        Util.ShowMessageBox("File " + filename + " does not exist. You may have to update the project.", "SciGit error");
      } else {
        FileHistory fh = null;
        try {
          fh = new FileHistory(p, filename, hash);
          ShowTop(fh);
        } catch (InvalidRepositoryException) {
          if (fh != null) fh.Hide();
          Util.ShowMessageBox("This file does not belong to a valid SciGit project.", "SciGit error");
        } catch (Exception e) {
          if (fh != null) fh.Hide();
          ErrorForm.Show(e);
        }
      }
    }

    private void OpenProjectHistory(Project p, string hash = null) {
      if (!projectMonitor.CheckProject(p)) return;

      string dir = ProjectMonitor.GetProjectDirectory(p);
      if (!Directory.Exists(dir)) {
        Util.ShowMessageBox("Project does not exist.", "Error");
      } else {
        ProjectHistory ph = null;
        try {
          ph = new ProjectHistory(p, hash);
          ShowTop(ph);
        } catch (InvalidRepositoryException) {
          if (ph != null) ph.Hide();
          Util.ShowMessageBox("This file does not belong to a valid SciGit project.", "Error");
        } catch (Exception e) {
          if (ph != null) ph.Hide();
          ErrorForm.Show(e);
        }
      }
    }

    private void OpenDirectoryHandler(object sender, EventArgs e) {
      string dir = ProjectMonitor.GetProjectDirectory();
      if (dir != null) {
        Process.Start(dir);
      }
    }

    private void ManageProjectsHandler(object sender, EventArgs e) {
      Process.Start("http://" + RestClient.ServerHost + "/projects");
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
        var progressForm = new ProgressForm("Updating " + p.name, (form, bw) => projectMonitor.UpdateProject(p, form, bw));
        ShowTop(progressForm);
      };
    }

    private EventHandler CreateUploadProjectHandler(Project p) {
      return (s, e) => {
        var progressForm = new ProgressForm("Uploading " + p.name, (form, bw) => projectMonitor.UploadProject(p, form, bw));
        ShowTop(progressForm);
      };
    }

    private EventHandler CreateViewProjectHistoryHandler(Project p) {
      return (s, e) => {
        if (!projectMonitor.CheckProject(p)) return;
        var ph = new ProjectHistory(p);
        ShowTop(ph);
      };
    }

    private void CreateUpdateAllHandler(object sender, EventArgs e) {
      var progressForm = new ProgressForm("Updating Projects", projectMonitor.UpdateAllProjects);
      ShowTop(progressForm);
    }

    private void CreateUploadAllHandler(object sender, EventArgs e) {
      var progressForm = new ProgressForm("Uploading Projects", projectMonitor.UploadAllProjects);
      ShowTop(progressForm);
    }

    private void Logout() {
      loginWindow.Reset();
      loginWindow.Show();
      loginWindow.Topmost = true;
      loginWindow.Topmost = false;
      Close();
    }

    private void SettingsClick(object sender, EventArgs e) {
      var sf = new SettingsForm();
      sf.ShowDialog();

      if (Settings.Default.ProjectDirectory != ProjectMonitor.GetProjectDirectory()) {
        Util.ShowMessageBox("To complete the change, SciGit needs to be restarted. " +
          "Please finish any remaining operations.", "Restart Required");
        Logout();
      }
    }

    private void LogoutClick(object sender, EventArgs e) {
      Logout();
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
      if ((Settings.Default.NotifyMask & (int)NotifyFlags.NotifyAddDelete) != 0) {
        QueueBalloonTip("Project Added",
                        "Project " + p.name + (p.can_write ? "" : " (read-only)") +
                          " has been added. Click to open the project folder...", CreateOpenDirectoryHandler(p));
      }
    }

    private void ProjectRemoved(Project p) {
      if ((Settings.Default.NotifyMask & (int)NotifyFlags.NotifyAddDelete) != 0) {
        QueueBalloonTip("Project Removed",
                        "You are no longer receiving updates for project " + p.name +
                          ". Click to open the project folder...", CreateOpenDirectoryHandler(p));
      }
    }

    private void ProjectUpdated(Project p) {
      if ((Settings.Default.NotifyMask & (int)NotifyFlags.NotifyUpdate) != 0) {
        QueueBalloonTip("Project Updated",
                        "Project " + p.name + " has been updated. Click to update the local version...",
                        CreateUpdateProjectHandler(p));
      }
    }

    private void ProjectEdited(Project p) {
      if ((Settings.Default.NotifyMask & (int)NotifyFlags.NotifyUpload) != 0) {
        QueueBalloonTip("Project Edited",
                        "You made changes to project " + p.name + ". Click to upload your changes...",
                        CreateUploadProjectHandler(p));
      }
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
      notifyIcon.ContextMenu.MenuItems.Add("View Project History").Enabled = false;
      notifyIcon.ContextMenu.MenuItems.Add("-");
      notifyIcon.ContextMenu.MenuItems.Add("Update All", CreateUpdateAllHandler).Enabled = false;
      notifyIcon.ContextMenu.MenuItems.Add("Upload All", CreateUploadAllHandler).Enabled = false;
      notifyIcon.ContextMenu.MenuItems.Add("Loading...").Enabled = false;
      notifyIcon.ContextMenu.MenuItems.Add("-");
      notifyIcon.ContextMenu.MenuItems.Add("Settings...", SettingsClick);
      notifyIcon.ContextMenu.MenuItems.Add("Logout...", LogoutClick);
      notifyIcon.ContextMenu.MenuItems.Add("Exit", ExitClick);

      // Just show the loading indicator for now
      notifyIcon.Icon = notifyIconLoading;
      notifyIcon.Text = "SciGit\nLoading...";
      for (int i = 3; i <= 8; i++) {
        notifyIcon.ContextMenu.MenuItems[i].Visible = false;
      }
    }

    private void UpdateContextMenu() {
      var projects = projectMonitor.GetProjects();
      var updatedProjects = projectMonitor.GetUpdatedProjects();
      var editedProjects = projectMonitor.GetEditedProjects();
      var update = notifyIcon.ContextMenu.MenuItems[3];
      var upload = notifyIcon.ContextMenu.MenuItems[4];
      var view = notifyIcon.ContextMenu.MenuItems[5];      
      var curNames = new HashSet<string>(from item in update.MenuItems.Cast<MenuItem>() select item.Text);
      var newNames = new HashSet<string>(from p in projects select p.name);
      var updNames = new HashSet<string>(from p in updatedProjects select p.name);
      var editNames = new HashSet<string>(from p in editedProjects select p.name);
      var writeNames = new HashSet<string>(from p in projects where p.can_write select p.name);

      for (int i = update.MenuItems.Count - 1; i >= 0; i--) {
        MenuItem item = update.MenuItems[i];
        if (!newNames.Contains(item.Text)) {
          update.MenuItems.Remove(item);
        } else {
          item.Checked = updNames.Contains(item.Text);
        }
      }

      for (int i = view.MenuItems.Count - 1; i >= 0; i--) {
        MenuItem item = view.MenuItems[i];
        if (!newNames.Contains(item.Text)) {
          view.MenuItems.Remove(item);
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
          item.Checked = canWrite && editNames.Contains(projName);
          item.Text = projName + (canWrite ? "" : readOnlySuffix);
        }
      }

      foreach (var project in projects) {
        if (!curNames.Contains(project.name)) {
          var curProject = project; // closure issues
          var item = new MenuItem(project.name, CreateUpdateProjectHandler(curProject)) {
            Checked = updNames.Contains(project.name),
            RadioCheck = true
          };
          update.MenuItems.Add(item);
          item = new MenuItem(project.name + (project.can_write ? "" : readOnlySuffix),
                              CreateUploadProjectHandler(curProject)) {
            Enabled = project.can_write,
            Checked = project.can_write && editNames.Contains(project.name),
            RadioCheck = true
          };
          upload.MenuItems.Add(item);
          item = new MenuItem(project.name, CreateViewProjectHistoryHandler(curProject));
          view.MenuItems.Add(item);
        }
      }

      var updateAll = notifyIcon.ContextMenu.MenuItems[7];
      updateAll.Text = "Update All" + (updNames.Count > 0 ? String.Format(" ({0})", updNames.Count) : "");

      var uploadAll = notifyIcon.ContextMenu.MenuItems[8];
      int uploadable = writeNames.Intersect(editNames).Count();
      uploadAll.Text = "Upload All" + (uploadable > 0 ? String.Format(" ({0})", uploadable) : "");
      uploadAll.Enabled = writeNames.Count > 0;

      update.Enabled = upload.Enabled = updateAll.Enabled = view.Enabled = projects.Count > 0;

      // Hide the loading indicator and show others
      notifyIcon.Icon = updNames.Count > 0 || uploadable > 0 ? notifyIconUpdate : notifyIconBase;
      notifyIcon.Text = "SciGit\n";
      if (updNames.Count > 0) {
        notifyIcon.Text += "Project updates available.";
      } else if (uploadable > 0) {
        notifyIcon.Text += "Local changes awaiting upload.";
      } else {
        notifyIcon.Text += "All projects up to date.";
      }

      for (int i = 3; i <= 8; i++) {
        notifyIcon.ContextMenu.MenuItems[i].Visible = true;
      }
      notifyIcon.ContextMenu.MenuItems[9].Visible = false;
    }

    private void InitializeSSH() {
      // TODO: check Git/SSH installations

      string appPath = Util.PathCombine(GitWrapper.GetAppDataPath(), RestClient.Username);
      Directory.CreateDirectory(appPath);

      string sshDir = Util.PathCombine(appPath, ".ssh");
      Directory.CreateDirectory(sshDir);

      string keyFile = Util.PathCombine(sshDir, "id_rsa");
      if (!File.Exists(keyFile + ".pub")) {
        GitWrapper.GenerateSSHKey(keyFile);
      }

      GitWrapper.GlobalConfig("user.name", RestClient.Username);
      GitWrapper.GlobalConfig("user.email", RestClient.Username);

      string key = File.ReadAllText(keyFile + ".pub").Trim();
      bool? uploadResult = RestClient.UploadPublicKey(key);
      if (uploadResult != true) {
        Util.ShowMessageBox(uploadResult == false ?
          "It appears that your public key is invalid. Please remove or regenerate it." :
          "Could not connect to the SciGit server. Please try again later.", "Error");
        // TODO: add ability to regenerate
        Environment.Exit(1);
      }

      // Add scigit server to known_hosts
      string knownHostsFile = Util.PathCombine(sshDir, "known_hosts");
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

    private void OnClosed(object sender, FormClosedEventArgs e) {
      projectMonitor.StopMonitoring();
      updateChecker.Stop();
      if (shellCmdHandler != null) shellCmdHandler.Stop();
    }
  }
}
