using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.ComponentModel;
using System.Windows.Forms;
using System.Windows.Threading;

namespace SciGit_Client
{
  public struct Project
  {
    public int Id { get; set; }
    public string Name { get; set; }
    public int OwnerId { get; set; }
    public int CreatedTime { get; set; }
  }

  class ProjectMonitor
  {
    public List<Project> projects { get; private set; }
    public delegate void ProgressCallback(int percent, string operation, string extra);
    public List<Action> projectUpdatedCallbacks;
    private Thread monitorThread;
    private const int monitorDelay = 10 * 1000;

    public ProjectMonitor(List<Project> projects = null) {
      if (!Directory.Exists(GetProjectDirectory())) {
        CreateProjectDirectory();
      }

      if (projects != null) {
        this.projects = projects;
        foreach (var project in projects) {
          InitializeProject(project);
        }
      } else {
        this.projects = new List<Project>();
      }

      monitorThread = new Thread(new ThreadStart(MonitorProjects));
      projectUpdatedCallbacks = new List<Action>();
    }

    public void StartMonitoring() {
      monitorThread.Start();
    }

    public void MonitorProjects() {
      while (true) {
        List<Project> newProjects = RestClient.GetProjects();
        if (newProjects != null && !newProjects.SequenceEqual(projects)) {
          lock (projects) {
            projects = newProjects;
          }
          foreach (var project in projects) {
            InitializeProject(project);
          }
          // TODO: delete removed projects?
          foreach (var cb in projectUpdatedCallbacks) {
            cb();
          }
        }

        Thread.Sleep(monitorDelay);
      }
    }

    public void InitializeProject(Project p) {
      string dir = GetProjectDirectory();
      if (!Directory.Exists(p.Name)) {
        GitWrapper.Clone(dir, p);
      }
    }

    private void ShowError(Form form, Dispatcher disp, string err) {
      disp.Invoke(new Action(() => MessageBox.Show(form, err, "Error")));
    }

    public bool UpdateProject(Project p, Form form, Dispatcher disp, BackgroundWorker worker = null) {
      string dir = ProjectMonitor.GetProjectDirectory(p);

      GitReturn ret;
      // TODO: check if a previous merge is still in progress.

      if (worker != null) worker.ReportProgress(20, Tuple.Create("Fetching updates...", ""));
      ret = GitWrapper.Fetch(dir);
      if (ret.ReturnValue != 0) {
        ShowError(form, disp, "Error trying to retrieve updates: " + ret.Output);
        if (worker != null) worker.ReportProgress(100, Tuple.Create("Error.", ret.Output));
        return false;
      }

      ret = GitWrapper.Log(dir, "FETCH_HEAD -n 1");
      if (ret.ReturnValue != 0) {
        // Empty respository.
        if (worker != null) worker.ReportProgress(100, Tuple.Create("No changes.", ret.Output));
        return true;
      }

      // Make a temporary commit to facilitate merging.
      ret = GitWrapper.Add(dir, ".");
      ret = GitWrapper.Commit(dir, "tempCommit " + DateTime.Now);
      bool tempCommit = ret.ReturnValue == 0;

      if (worker != null) worker.ReportProgress(50, Tuple.Create("Merging...", ""));
      ret = GitWrapper.Rebase(dir, "FETCH_HEAD");
      string message = "Finished.";
      bool success = true;
      if (ret.ReturnValue != 0) {
        // TODO: any other error conditions? currently assuming it's a merge conflict.
        string dialogMsg = "Merge conflict(s) were detected. Would you like to resolve them now using the SciGit editor?\r\n" +
          "You can also resolve them manually using your text editor.";
        DialogResult resp = DialogResult.Abort;
        disp.Invoke(new Action(() => resp = MessageBox.Show(form, dialogMsg, "Merge Conflict", MessageBoxButtons.YesNoCancel)));
        MergeResolver mr = null;
        if (resp == DialogResult.Yes) {
          disp.Invoke(new Action(() => {
            mr = new MergeResolver(p);
            mr.ShowDialog();
          }));
        }
        if (resp != DialogResult.No && (mr == null || !mr.Saved)) {
          // Cancel the process here.
          GitWrapper.Rebase(dir, "--abort");
          if (tempCommit) {
            GitWrapper.Reset(dir, "HEAD^");
          }
          message = "Canceled.";
          success = false;
        } else {
          GitWrapper.Add(dir, ".");
          GitWrapper.Rebase(dir, "--continue");
          // TODO: any possible errors?
        }
      } else if (ret.Output.Contains("up to date")) {
        message = "No changes.";
      }

      if (worker != null) worker.ReportProgress(100, Tuple.Create(message, ret.Output));
      if (tempCommit) {
        GitWrapper.Reset(dir, "HEAD^");
      }
      return success;
    }

    public bool UploadProject(Project p, Form form, Dispatcher disp, BackgroundWorker worker = null) {
      string dir = ProjectMonitor.GetProjectDirectory(p);

      string commitMsg = "commit " + DateTime.Now;
      GitReturn ret;
      while (true) {
        if (worker != null) worker.ReportProgress(20, Tuple.Create("Checking for updates...", ""));
        if (!UpdateProject(p, form, disp)) {
          if (worker != null) worker.ReportProgress(100, Tuple.Create("Canceled.", ""));
          return false;
        }

        ret = GitWrapper.Add(dir, ".");
        if (worker != null) worker.ReportProgress(50, Tuple.Create("Committing...", ret.Output));
        // TODO: prompt for commit message
        ret = GitWrapper.Commit(dir, commitMsg);
        if (ret.ReturnValue == 0) {
          if (worker != null) worker.ReportProgress(70, Tuple.Create("Pushing...", ret.Output));
          ret = GitWrapper.Push(dir);
          if (ret.ReturnValue == 0) {
            break;
          } else if (ret.Output.Contains("non-fast-forward")) {
            GitWrapper.Reset(dir, "HEAD^");
            DialogResult resp = DialogResult.Abort;
            disp.Invoke(new Action(() =>
              resp = MessageBox.Show(form, "Additional updates must be merged in. Continue?", 
                                     "Additional updates", MessageBoxButtons.OKCancel)));
            if (resp == DialogResult.Cancel) {
              if (worker != null) worker.ReportProgress(100, Tuple.Create("Canceled.", ""));
              return false;
            }
          } else {
            ShowError(form, disp, "Error pushing: " + ret.Output);
            if (worker != null) worker.ReportProgress(100, Tuple.Create("Error.", ""));
            return false;
          }
        } else {
          if (worker != null) worker.ReportProgress(100, Tuple.Create("No changes.", ret.Output));
          return true;
        }
      }
      
      if (worker != null) worker.ReportProgress(100, Tuple.Create("Finished.", ""));
      return true;
    }

    public void UpdateAllProjects(Form form, Dispatcher disp, BackgroundWorker worker = null) {
      lock (projects) {
        int done = 0;
        foreach (var project in projects) {
          if (worker != null) {
            worker.ReportProgress(100 * done++ / projects.Count,
              Tuple.Create("Updating " + project.Name + "...", ""));
          }
          UpdateProject(project, form, disp);
        }
        worker.ReportProgress(100, Tuple.Create("Finished.", ""));
      }
    }

    public void UploadAllProjects(Form form, Dispatcher disp, BackgroundWorker worker = null) {
      lock (projects) {
        int done = 0;
        foreach (var project in projects) {
          if (worker != null) {
            worker.ReportProgress(100 * done++ / projects.Count,
              Tuple.Create("Uploading " + project.Name + "...", ""));
          }
          UploadProject(project, form, disp);
        }
        worker.ReportProgress(100, Tuple.Create("Finished.", ""));
      }
    }

    private static void CreateProjectDirectory() {
      Directory.CreateDirectory(GetProjectDirectory());
      // TODO: add special shell properties
    }

    public static string GetProjectDirectory() {
      return Environment.GetEnvironmentVariable("HOME") + Path.DirectorySeparatorChar + "SciGit";
    }

    public static string GetProjectDirectory(Project p) {
      return GetProjectDirectory() + Path.DirectorySeparatorChar + p.Name;
    }
  }
}
