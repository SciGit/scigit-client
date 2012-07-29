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
      InitializeContextMenu();
      projectManager = new SGProjectManager();
      projectManager.projectUpdatedCallbacks.Add(ProjectUpdated);
      projectManager.StartMonitoring();
      UpdateContextMenu();
    }

    private void OpenDirectory(object sender, EventArgs e) {
      Process.Start(SGProjectManager.GetProjectDirectory());
    }

    private EventHandler UpdateProject(Project p) {
      return (s, e) => {
        ProgressForm form = new ProgressForm();
        form.Show();
        BackgroundWorker bg = new BackgroundWorker();
        bg.WorkerReportsProgress = true;
        bg.DoWork += (bw, _) => projectManager.UpdateProject(p, (BackgroundWorker)bw);
        bg.ProgressChanged += form.UpdateProgress;
        bg.RunWorkerCompleted += form.Completed;
        bg.RunWorkerAsync();
      };
    }

    private EventHandler UploadProject(Project p) {
      return (s, e) => {
        ProgressForm form = new ProgressForm();
        form.Show();
        BackgroundWorker bg = new BackgroundWorker();
        bg.WorkerReportsProgress = true;
        bg.DoWork += (bw, _) => projectManager.UploadProject(p, (BackgroundWorker)bw);
        bg.ProgressChanged += form.UpdateProgress;
        bg.RunWorkerCompleted += form.Completed;
        bg.RunWorkerAsync();
      };
    }

    private void UpdateAll(object sender, EventArgs e) {
      ProgressForm form = new ProgressForm();
      form.Show();
      BackgroundWorker bg = new BackgroundWorker();
      bg.WorkerReportsProgress = true;
      bg.DoWork += (bw, _) => projectManager.UpdateAllProjects((BackgroundWorker)bw);
      bg.ProgressChanged += form.UpdateProgress;
      bg.RunWorkerCompleted += form.Completed;
      bg.RunWorkerAsync();
    }

    private void UploadAll(object sender, EventArgs e) {
      ProgressForm form = new ProgressForm();
      form.Show();
      BackgroundWorker bg = new BackgroundWorker();
      bg.WorkerReportsProgress = true;
      bg.DoWork += (bw, _) => projectManager.UploadAllProjects((BackgroundWorker)bw);
      bg.ProgressChanged += form.UpdateProgress;
      bg.RunWorkerCompleted += form.Completed;
      bg.RunWorkerAsync();
    }

    private void ExitClick(object sender, EventArgs e) {
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

        foreach (MenuItem item in update.MenuItems) {
          if (!updated.Contains(item.Text)) {
            update.MenuItems.Remove(item);
          }
        }

        foreach (MenuItem item in upload.MenuItems) {
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
  }
}
