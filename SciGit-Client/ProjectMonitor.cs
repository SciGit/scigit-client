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
    public string LastCommitHash { get; set; }
  }

  class ProjectMonitor
  {
    List<Project> projects, updatedProjects;
    public delegate void ProjectCallback(Project p);
    public delegate void ProgressCallback(int percent, string operation, string extra);
    public List<Action> updateCallbacks, loadedCallbacks;
    public List<ProjectCallback> projectAddedCallbacks, projectRemovedCallbacks, projectUpdatedCallbacks;
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
      updatedProjects = new List<Project>();

      monitorThread = new Thread(new ThreadStart(MonitorProjects));
      updateCallbacks = new List<Action>();
      loadedCallbacks = new List<Action>();
      projectUpdatedCallbacks = new List<ProjectCallback>();
      projectAddedCallbacks = new List<ProjectCallback>();
      projectRemovedCallbacks = new List<ProjectCallback>();
    }

    public void StartMonitoring() {
      monitorThread.Start();
    }

    // Return a clone to avoid concurrency issues.
    public List<Project> GetProjects() {
      lock (projects) {
        return new List<Project>(projects);
      }
    }

    public List<Project> GetUpdatedProjects() {
      lock (updatedProjects) {
        return new List<Project>(updatedProjects);
      }
    }

    public Project GetProjectFromFilename(ref string filename) {
      string dir = GetProjectDirectory() + Path.DirectorySeparatorChar;
      if (filename.StartsWith(dir)) {
        filename = filename.Substring(dir.Length);
        int slash = filename.IndexOf(Path.DirectorySeparatorChar);
        string projectName;
        if (slash != -1) {
          projectName = filename.Substring(0, slash);
          filename = filename.Substring(slash + 1);
        } else {
          projectName = filename;
          filename = "";
        }

        lock (projects) {
          return projects.Find(p => String.Compare(p.Name, projectName, true) == 0);
        }
      }

      return new Project();
    }

    private void DispatchCallbacks(List<ProjectCallback> callbacks, Project p) {
      foreach (var callback in callbacks) {
        callback(p);
      }
    }

    private void MonitorProjects() {
      bool loaded = false;
      while (true) {
        List<Project> newProjects = RestClient.GetProjects();
        if (newProjects != null && !newProjects.SequenceEqual(projects)) {
          Dictionary<int, Project> oldProjectDict = projects.ToDictionary(p => p.Id);
          Dictionary<int, Project> newProjectDict = newProjects.ToDictionary(p => p.Id);

          List<Project> newUpdatedProjects = new List<Project>();
          foreach (var project in newProjects) {
            if (InitializeProject(project)) {
              DispatchCallbacks(projectAddedCallbacks, project);
            } else if (HasUpdate(project)) {
              newUpdatedProjects.Add(project);
              DispatchCallbacks(projectUpdatedCallbacks, project);
            }
          }

          lock (updatedProjects) {
            updatedProjects = newUpdatedProjects;
          }

          foreach (var project in oldProjectDict) {
            if (!newProjectDict.ContainsKey(project.Value.Id)) {
              DispatchCallbacks(projectRemovedCallbacks, project.Value);
              // TODO: delete removed projects?
            }
          }

          lock (projects) {
            projects = newProjects;
          }
          updateCallbacks.ForEach(c => c.Invoke());
        }

        if (newProjects != null && !loaded) {
          loaded = true;
          loadedCallbacks.ForEach(c => c.Invoke());
        }

        Thread.Sleep(monitorDelay);
      }
    }

    private bool InitializeProject(Project p) {
      string dir = GetProjectDirectory();
      if (Directory.Exists(dir + Path.DirectorySeparatorChar + p.Name)) return false;
      GitWrapper.Clone(dir, p);
      return true;      
    }

    private void ShowError(Form form, Dispatcher disp, string err) {
      disp.Invoke(new Action(() => MessageBox.Show(form, err, "Error")));
    }

    public bool UpdateProject(Project p, Form form, Dispatcher disp, BackgroundWorker worker = null) {
      string dir = ProjectMonitor.GetProjectDirectory(p);

      try {
        ProcessReturn ret;
        // TODO: check if a previous merge is still in progress.

        if (worker != null) worker.ReportProgress(20, Tuple.Create("Fetching updates...", ""));
        ret = GitWrapper.Fetch(dir);
        if (ret.ReturnValue != 0) {
          ShowError(form, disp, "Error trying to retrieve updates: " + ret.Output);
          if (worker != null) worker.ReportProgress(100, Tuple.Create("Error.", ret.Output));
          return false;
        }

        ret = GitWrapper.GetLastCommit(dir, "FETCH_HEAD");
        if (ret.ReturnValue != 0) {
          // Empty respository.
          if (worker != null) worker.ReportProgress(100, Tuple.Create("No changes.", ret.Output));
          return true;
        }

        // Make a temporary commit to facilitate merging.
        ret = GitWrapper.AddAll(dir);
        ret = GitWrapper.Commit(dir, "tempCommit " + DateTime.Now);
        bool tempCommit = ret.ReturnValue == 0;

        if (worker != null) worker.ReportProgress(50, Tuple.Create("Merging...", ""));
        ret = GitWrapper.Rebase(dir, "FETCH_HEAD");
        string message = "Finished.";
        bool success = true;
        if (ret.ReturnValue != 0) {
          // TODO: any other error conditions? currently assuming it's a merge conflict.
          string dialogMsg = "Merge conflict(s) were detected. Would you like to resolve them now using the SciGit editor?\r\n" +
            "You can also resolve them manually using your text editor.\r\n" +
            "Please save any open files before continuing.";
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
            message = "Canceled.";
            success = false;
          } else {
            GitWrapper.AddAll(dir);
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
        if (success) {
          lock (updatedProjects) {
            updatedProjects = updatedProjects.Where(up => up.Id != p.Id).ToList();
          }
        }
        return success;
      } catch (Exception e) {
        if (worker != null) worker.ReportProgress(100, Tuple.Create("Error.", e.Message));
        return false;
      }
    }

    public bool UploadProject(Project p, Form form, Dispatcher disp, BackgroundWorker worker = null) {
      string dir = ProjectMonitor.GetProjectDirectory(p);
      GitWrapper.AddAll(dir);
      ProcessReturn ret = GitWrapper.Status(dir);
      if (ret.Output.Trim() == "") {
        if (worker != null) worker.ReportProgress(20, Tuple.Create("No changes.", ""));
        return true;
      }

      try {
        string commitMsg = null;
        while (true) {
          if (worker != null) worker.ReportProgress(20, Tuple.Create("Checking for updates...", ""));
          if (!UpdateProject(p, form, disp)) {
            if (worker != null) worker.ReportProgress(100, Tuple.Create("Canceled.", ""));
            return false;
          }

          ret = GitWrapper.AddAll(dir);
          if (worker != null) worker.ReportProgress(50, Tuple.Create("Committing...", ret.Output));
          ret = GitWrapper.Status(dir);
          if (ret.Output.Trim() != "") {
            if (commitMsg == null) {
              CommitForm commitForm = null;
              disp.Invoke(new Action(() => {
                commitForm = new CommitForm(p);
                commitForm.ShowDialog();
              }));
              commitMsg = commitForm.savedMessage;
              if (commitMsg == null) {
                if (worker != null) worker.ReportProgress(100, Tuple.Create("Canceled.", ""));
                return false;
              }
            }
            ret = GitWrapper.Commit(dir, commitMsg);
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
              GitWrapper.Reset(dir, "HEAD^");
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
      } catch (Exception e) {
        if (worker != null) worker.ReportProgress(100, Tuple.Create("Error.", e.Message));
        return false;
      }
    }

    public void UpdateAllProjects(Form form, Dispatcher disp, BackgroundWorker worker = null) {
      lock (projects) {
        int done = 0;
        foreach (var project in projects) {
          if (worker != null) {
            worker.ReportProgress(100 * done++ / projects.Count,
              Tuple.Create("Updating " + project.Name + "...", ""));
          }
          if (HasUpdate(project)) {
            UpdateProject(project, form, disp);
          }
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

    public bool HasUpdate(Project p) {
      string dir = GetProjectDirectory(p);
      ProcessReturn ret = GitWrapper.GetLastCommit(dir);
      string lastHash = "";
      if (ret.ReturnValue == 0) {
        lastHash = ret.Output.Trim();
      }
      return lastHash != p.LastCommitHash;
    }

    private static void CreateProjectDirectory() {
      Directory.CreateDirectory(GetProjectDirectory());
    }

    public static string GetProjectDirectory() {
      return Path.Combine(Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%"), "SciGit");
    }

    public static string GetProjectDirectory(Project p) {
      return Path.Combine(GetProjectDirectory(), p.Name);
    }
  }
}
