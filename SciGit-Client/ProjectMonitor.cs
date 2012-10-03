using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
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
    #region Delegates

    public delegate void ProgressCallback(int percent, string operation, string extra);

    public delegate void ProjectCallback(Project p);

    #endregion

    private const int monitorDelay = 10 * 1000;

    public List<Action> loadedCallbacks;
    private Thread monitorThread;
    public List<ProjectCallback> projectAddedCallbacks, projectRemovedCallbacks, projectUpdatedCallbacks;
    List<Project> projects;
    public List<Action> updateCallbacks;
    List<Project> updatedProjects;

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

      monitorThread = new Thread(MonitorProjects);
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

          var newUpdatedProjects = new List<Project>();
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
      if (Directory.Exists(Path.Combine(dir, p.Name))) return false;
      GitWrapper.Clone(dir, p);
      return true;      
    }

    private void ShowError(Window window, string err) {
      window.Dispatcher.Invoke(new Action(() => MessageBox.Show(window, err, "Error",
        MessageBoxButton.OK, MessageBoxImage.Error)));
    }

    public bool UpdateProject(Project p, Window window, BackgroundWorker worker, bool progress = true) {
      string dir = GetProjectDirectory(p);

      if (GitWrapper.RebaseInProgress(dir)) {
        GitWrapper.Rebase(dir, "--abort");
      }

      worker.ReportProgress(progress ? 25 : -1, "Fetching updates...");
      ProcessReturn ret = GitWrapper.Fetch(dir);
      worker.ReportProgress(-1, ret.Output);
      if (ret.ReturnValue != 0) {
        throw new Exception("fetch: " + ret.Output);
      }

      // Reset commits until we get to something in common with FETCH_HEAD.
      ret = GitWrapper.MergeBase(dir, "HEAD", "FETCH_HEAD");
      if (ret.ReturnValue != 0) {
        throw new Exception("merge-base: "  + ret.Output);
      }
      GitWrapper.Reset(dir, ret.Stdout.Trim());

      // Make a temporary commit to facilitate merging.
      GitWrapper.AddAll(dir);
      ret = GitWrapper.Commit(dir, "tempCommit " + DateTime.Now);
      worker.ReportProgress(-1, "Creating temporary commit...");
      worker.ReportProgress(-1, ret.Output);
      bool tempCommit = ret.ReturnValue == 0;

      worker.ReportProgress(progress ? 50 : -1, "Merging...");
      ret = GitWrapper.Rebase(dir, "FETCH_HEAD");
      worker.ReportProgress(-1, ret.Output);

      string message = "New changes successfully merged.";
      bool success = true;
      if (ret.ReturnValue != 0) {
        if (ret.Output.Contains("CONFLICT")) {
          const string dialogMsg =
            "Merge conflict(s) were detected. Would you like to resolve them now using the SciGit editor?\r\n" +
            "You can also resolve them manually using your text editor.\r\n" +
            "Please save any open files before continuing.";
          MessageBoxResult resp = MessageBoxResult.Cancel;
          window.Dispatcher.Invoke(new Action(() => resp = MessageBox.Show(window, dialogMsg, "Merge Conflict", MessageBoxButton.YesNoCancel)));
          MergeResolver mr = null;
          if (resp == MessageBoxResult.Yes) {
            window.Dispatcher.Invoke(new Action(() => {
              mr = new MergeResolver(p);
              mr.ShowDialog();
            }));
          }
          if (resp != MessageBoxResult.No && (mr == null || !mr.Saved)) {
            // Cancel the process here.
            GitWrapper.Rebase(dir, "--abort");
            message = "Cancelled.";
            success = false;
          } else {
            GitWrapper.AddAll(dir);
            ret = GitWrapper.Rebase(dir, "--continue");
            worker.ReportProgress(progress ? 75 : -1, "Continuing merge...");
            worker.ReportProgress(-1, ret.Output);
            if (ret.ReturnValue != 0) {
              throw new Exception("rebase: " + ret.Output);
            }
          }
        } else {
          throw new Exception("rebase: " + ret.Output);
        }
      } else if (ret.Output.Contains("up to date")) {
        message = "No changes.";
      }

      worker.ReportProgress(progress ? 100 : -1, message);
      if (tempCommit) GitWrapper.Reset(dir, "HEAD^");
      if (success) {
        lock (updatedProjects) {
          updatedProjects = updatedProjects.Where(up => up.Id != p.Id).ToList();
        }
        updateCallbacks.ForEach(c => c.Invoke());
      }
      return success;
    }

    public bool UploadProject(Project p, Window window, BackgroundWorker worker, bool progress = true) {
      string dir = GetProjectDirectory(p);

      string commitMsg = null;
      while (true) {
        worker.ReportProgress(progress ? 25 : -1, "Checking for updates...");
        if (!UpdateProject(p, window, worker, false)) {
          return false;
        }

        GitWrapper.AddAll(dir);
        worker.ReportProgress(progress ? 50 : -1, "Committing...");
        ProcessReturn ret = GitWrapper.Status(dir);
        if (ret.Stdout.Trim() != "") {
          if (commitMsg == null) {
            CommitForm commitForm = null;
            window.Dispatcher.Invoke(new Action(() => {
              commitForm = new CommitForm(p);
              commitForm.ShowDialog();
            }));
            commitMsg = commitForm.savedMessage;
            if (commitMsg == null) {
              return false;
            }
          }
          ret = GitWrapper.Commit(dir, commitMsg);
          worker.ReportProgress(-1, ret.Output);

          worker.ReportProgress(progress ? 75 : -1, "Pushing...");
          ret = GitWrapper.Push(dir);
          worker.ReportProgress(-1, ret.Output);
          if (ret.ReturnValue == 0) {
            break;
          } else if (ret.Output.Contains("non-fast-forward")) {
            GitWrapper.Reset(dir, "HEAD^");
            MessageBoxResult resp = MessageBoxResult.Cancel;
            window.Dispatcher.Invoke(new Action(() =>
              resp = MessageBox.Show(window, "Additional updates must be merged in. Continue?",
                  "Additional updates", MessageBoxButton.OKCancel)));
            if (resp == MessageBoxResult.Cancel) {
              return false;
            }
          } else {
            GitWrapper.Reset(dir, "HEAD^");
            throw new Exception("push: " + ret.Output);
          }
        } else {
          worker.ReportProgress(progress ? 100 : -1, "No changes to upload.");
          return true;
        }
      }

      worker.ReportProgress(progress ? 100 : -1, "Upload successful.");
      return true;
    }

    public bool UpdateAllProjects(Window window, BackgroundWorker worker) {
      lock (projects) {
        int done = 0;
        foreach (var project in projects) {
          worker.ReportProgress(100 * done++/projects.Count, "Updating " + project.Name + "...");
          if (HasUpdate(project)) {
            UpdateProject(project, window, worker, false);
          }
          if (worker.CancellationPending) {
            return false;
          }
        }
      }
      worker.ReportProgress(100, "Finished.");
      return true;
    }

    public bool UploadAllProjects(Window window, BackgroundWorker worker) {
      lock (projects) {
        int done = 0;
        foreach (var project in projects) {
          worker.ReportProgress(100 * done++ / projects.Count, "Uploading " + project.Name + "...");
          UploadProject(project, window, worker, false);
          if (worker.CancellationPending) {
            return false;
          }
        }
      }
      worker.ReportProgress(100, "Finished.");
      return true;
    }

    public bool HasUpdate(Project p) {
      string dir = GetProjectDirectory(p);
      ProcessReturn ret = GitWrapper.GetLastCommit(dir);
      string lastHash = "";
      if (ret.ReturnValue == 0) {
        lastHash = ret.Stdout.Trim();
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
