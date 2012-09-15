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
using System.Threading.Tasks;
using System.Threading;
using System.IO.Pipes;

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
    ProjectMonitor projectMonitor;
    Queue<BalloonTip> balloonTips;
    const int balloonTipTimeout = 3000;
    Dispatcher dispatcher;
    NamedPipeServerStream pipeServer;

    public Main() {
      InitializeComponent();
      InitializeContextMenu();
      InitializeSSH();
      
      balloonTips = new Queue<BalloonTip>();
      notifyIcon.BalloonTipClosed += BalloonTipClosed;
      notifyIcon.BalloonTipClicked += BalloonTipClicked;

      dispatcher = Dispatcher.CurrentDispatcher;
      projectMonitor = new ProjectMonitor();
      projectMonitor.updateCallbacks.Add(UpdateContextMenu);
      projectMonitor.projectAddedCallbacks.Add(ProjectAdded);
      projectMonitor.projectUpdatedCallbacks.Add(ProjectUpdated);
      projectMonitor.loadedCallbacks.Add(OnProjectMonitorLoaded);
      projectMonitor.StartMonitoring();
    }

    private void FatalError(string err) {
      MessageBox.Show(err, "Error");
      Environment.Exit(1);
    }

    private void HandleCommand(string verb, string filename) {
      Project p = projectMonitor.GetProjectFromFilename(ref filename);
      if (p.Id == 0) {
        MessageBox.Show("This file does not belong to a valid SciGit project.", "Invalid SciGit file");
      } else if (verb == "--versions") {
        dispatcher.Invoke(new Action(() => OpenFileHistory(p, filename)));
      } else if (verb == "--update") {
        dispatcher.Invoke(CreateUpdateProjectHandler(p), new object[] { null, null });
      } else if (verb == "--upload") {
        dispatcher.Invoke(CreateUploadProjectHandler(p), new object[] { null, null });
      }
    }

    private void OnProjectMonitorLoaded() {
      string[] args = Environment.GetCommandLineArgs();
      if (args.Length == 3) {
        HandleCommand(args[1], args[2]);
      }

      pipeServer = new NamedPipeServerStream("sciGitPipe", PipeDirection.In, 2);
      Thread t = new Thread(new ThreadStart(() => {
        while (true) {
          pipeServer.WaitForConnection();
          try {
            StreamString ss = new StreamString(pipeServer);
            string verb = ss.ReadString();
            string filename = ss.ReadString();
            HandleCommand(verb, filename);
          } catch (Exception) {
            // TODO: log errors somewhere
          }
          pipeServer.Disconnect();
        }
      }));
      t.Start();
    }

    private void OpenFileHistory(Project p, string filename) {
      string dir = ProjectMonitor.GetProjectDirectory(p);
      if (!File.Exists(dir + Path.DirectorySeparatorChar + filename)) {
        MessageBox.Show("File does not exist.", "Error");
      } else {
        FileHistory fh = new FileHistory(p, filename);
        fh.Show();
      }
    }

    private void OpenDirectoryHandler(object sender, EventArgs e) {
      Process.Start(ProjectMonitor.GetProjectDirectory());
    }

    private void ManageProjectsHandler(object sender, EventArgs e) {
      Process.Start("http://" + RestClient.serverHost + "/projects");
    }

    private void NotifyClick(object sender, EventArgs e) {
      MouseEventArgs me = (MouseEventArgs)e;
      if (me.Button == System.Windows.Forms.MouseButtons.Left) {
        OpenDirectoryHandler(sender, e);
      }
    }

    private EventHandler CreateOpenDirectoryHandler(Project p) {
      return (s, e) => Process.Start(ProjectMonitor.GetProjectDirectory(p));
    }

    private EventHandler CreateUpdateProjectHandler(Project p) {
      return (s, e) => {
        ProgressForm progressForm = new ProgressForm(dispatcher,
          (form, disp, bw) => {
            projectMonitor.UpdateProject(p, form, disp, bw);
            UpdateContextMenu();
          }
         );
        progressForm.Show();
      };
    }

    private EventHandler CreateUploadProjectHandler(Project p) {
      return (s, e) => {
        ProgressForm progressForm = new ProgressForm(dispatcher,
          (form, disp, bw) => {
            projectMonitor.UploadProject(p, form, disp, bw);
            UpdateContextMenu();
          }
         );
        progressForm.Show();
      };
    }

    private void CreateUpdateAllHandler(object sender, EventArgs e) {
      ProgressForm progressForm = new ProgressForm(dispatcher,
        (form, disp, bw) => {
          projectMonitor.UpdateAllProjects(form, disp, bw);
          UpdateContextMenu();
        }
       );
      progressForm.Show();
    }

    private void CreateUploadAllHandler(object sender, EventArgs e) {
      ProgressForm progressForm = new ProgressForm(dispatcher,
        (form, disp, bw) => {
          projectMonitor.UploadAllProjects(form, disp, bw);
          UpdateContextMenu();
        }
       );
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
      notifyIcon.ContextMenu.MenuItems.Add("-");
      notifyIcon.ContextMenu.MenuItems.Add("Exit", ExitClick);
      // TODO: show a loading indicator
    }

    private void UpdateContextMenu() {
      List<Project> projects = projectMonitor.GetProjects();
      List<Project> updatedProjects = projectMonitor.GetUpdatedProjects();
      var update = notifyIcon.ContextMenu.MenuItems[3];
      var upload = notifyIcon.ContextMenu.MenuItems[4];      
      HashSet<string> curNames = new HashSet<string>(from item in update.MenuItems.Cast<MenuItem>() select item.Text);
      HashSet<string> newNames = new HashSet<string>(from p in projects select p.Name);
      HashSet<string> updNames = new HashSet<string>(from p in updatedProjects select p.Name);

      for (int i = update.MenuItems.Count - 1; i >= 0; i--) {
        MenuItem item = update.MenuItems[i];
        item.Checked = updNames.Contains(item.Text);
        if (!newNames.Contains(item.Text)) {
          update.MenuItems.Remove(item);
        }
      }

      for (int i = upload.MenuItems.Count - 1; i >= 0; i--) {
        MenuItem item = upload.MenuItems[i];
        if (!newNames.Contains(item.Text)) {
          upload.MenuItems.Remove(item);
        }
      }

      foreach (var project in projects) {
        if (!curNames.Contains(project.Name)) {
          var curProject = project; // closure issues
          MenuItem item = new MenuItem(project.Name, CreateUpdateProjectHandler(curProject));
          item.Checked = updNames.Contains(project.Name);
          item.RadioCheck = true;
          update.MenuItems.Add(item);
          item = new MenuItem(project.Name, CreateUploadProjectHandler(curProject));
          upload.MenuItems.Add(item);
        }
      }

      var updateAll = notifyIcon.ContextMenu.MenuItems[6];
      updateAll.Text = "Update All" + (updNames.Count > 0 ? String.Format(" ({0})", updNames.Count) : "");

      var uploadAll = notifyIcon.ContextMenu.MenuItems[7];
      update.Enabled = upload.Enabled = updateAll.Enabled = uploadAll.Enabled = projects.Count > 0;
    }

    private void InitializeSSH() {
      // TODO: check Git/SSH installations

      string homeDir = Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
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
