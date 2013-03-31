﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using SciGit_Client.Properties;

namespace SciGit_Client
{
  public struct Project
  {
    public int id { get; set; }
    public string name { get; set; }
    public int created_ts { get; set; }
    public string last_commit_hash { get; set; }
    public bool can_write { get; set; }
  }

  class ProjectMonitor
  {
    #region Delegates

    public delegate void ProgressCallback(int percent, string operation, string extra);

    public delegate void ProjectCallback(Project p);

    #endregion

    public List<Action> loadedCallbacks, updateCallbacks, failureCallbacks, disconnectCallbacks;

    public List<ProjectCallback> projectAddedCallbacks, projectRemovedCallbacks;
    public List<ProjectCallback> projectUpdatedCallbacks, projectEditedCallbacks;

    private Thread monitorThread;
    private const int monitorDelay = 5 * 1000;
    private bool connected = true;

    private List<Project> projects, updatedProjects, editedProjects;
    private static string projectDirectory;
    private FileSystemWatcher fileWatcher;
    private int activeProjectId;

    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    public ProjectMonitor(List<Project> projects = null) {
      projectDirectory = Settings.Default.ProjectDirectory;
      if (String.IsNullOrEmpty(projectDirectory)) {
        projectDirectory = DefaultProjectDirectory();
        Settings.Default.ProjectDirectory = projectDirectory;
        Settings.Default.Save();
      }
      if (!Directory.Exists(projectDirectory)) {
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
      editedProjects = new List<Project>();

      monitorThread = new Thread(MonitorProjects);
      updateCallbacks = new List<Action>();
      loadedCallbacks = new List<Action>();
      disconnectCallbacks = new List<Action>();
      projectUpdatedCallbacks = new List<ProjectCallback>();
      projectEditedCallbacks = new List<ProjectCallback>();
      projectAddedCallbacks = new List<ProjectCallback>();
      projectRemovedCallbacks = new List<ProjectCallback>();
      failureCallbacks = new List<Action>();

      fileWatcher = new FileSystemWatcher(GetProjectDirectory());
      fileWatcher.IncludeSubdirectories = true;
      fileWatcher.NotifyFilter = NotifyFilters.LastWrite;
      fileWatcher.Changed += FileChanged;
      fileWatcher.Created += FileChanged;
      fileWatcher.Deleted += FileChanged;
      fileWatcher.EnableRaisingEvents = true;
    }

    public void StartMonitoring() {
      monitorThread.Start();
    }

    public void StopMonitoring() {
      monitorThread.Abort();
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

    public List<Project> GetEditedProjects() {
      lock (editedProjects) {
        return new List<Project>(editedProjects);
      }
    }

    public Project GetProjectById(int id) {
      lock (projects) {
        return projects.Find(x => x.id == id);
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
          return projects.Find(p => String.Compare(p.name, projectName, true) == 0);
        }
      }

      return new Project();
    }

    public bool CheckProject(Project p) {
      string dir = GetProjectDirectory(p);
      if (!Directory.Exists(dir) || !Directory.Exists(Path.Combine(dir, ".git"))) {
        var result = Util.ShowMessageBox("Project " + p.name + " seems to be corrupted. Do you want SciGit to repair it?\r\n" +
              "You may want to back up your files first.", "Project corrupted", MessageBoxButton.YesNo);
        if (result == MessageBoxResult.Yes) {
          return InitializeProject(p);
        }
        return false;
      }
      return true;
    }

    private void DispatchCallbacks(List<ProjectCallback> callbacks, Project p) {
      foreach (var callback in callbacks) {
        callback(p);
      }
    }

    private void MonitorProjects() {
      bool loaded = false;
      while (true) {
        try {
          var response = RestClient.GetProjects();
          List<Project> newProjects = response.Data;
          if (response.Error == RestClient.ErrorType.Forbidden) {
            // Try to log in again.
            Logger.LogMessage("Authentication token expired, trying to log in again...");
            var loginResponse = RestClient.Relogin();
            if (loginResponse.Error != RestClient.ErrorType.NoError) {
              failureCallbacks.ForEach(c => c.Invoke());
            }
            continue;
          } else if (response.Error != RestClient.ErrorType.NoError) {
            Logger.LogMessage(response.Error.ToString() + " getting projects");
            disconnectCallbacks.ForEach(c => c.Invoke());
            connected = false;
          }

          if (newProjects != null) {
            if (!loaded || !newProjects.SequenceEqual(projects)) {
              Dictionary<int, Project> oldProjectDict, newProjectDict, updatedProjectDict, editedProjectDict;
              oldProjectDict = GetProjects().ToDictionary(p => p.id);
              newProjectDict = newProjects.ToDictionary(p => p.id);
              updatedProjectDict = GetUpdatedProjects().ToDictionary(p => p.id);
              editedProjectDict = GetEditedProjects().ToDictionary(p => p.id);

              var newUpdatedProjects = new List<Project>();
              foreach (var project in newProjects) {
                if (InitializeProject(project) || loaded && !oldProjectDict.ContainsKey(project.id)) {
                  DispatchCallbacks(projectAddedCallbacks, project);
                }
                // This only needs to be done at load. Otherwise, the file watcher will catch it.
                if (!loaded && HasUpload(project)) {
                  lock (editedProjects) {
                    editedProjects.Add(project);
                  }
                  if (project.can_write && !editedProjectDict.ContainsKey(project.id)) {
                    DispatchCallbacks(projectEditedCallbacks, project);
                  }
                }
                if (HasUpdate(project)) {
                  newUpdatedProjects.Add(project);
                  if (!updatedProjectDict.ContainsKey(project.id) ||
                    updatedProjectDict[project.id].last_commit_hash != project.last_commit_hash) {
                    DispatchCallbacks(projectUpdatedCallbacks, project);
                  }
                }
              }

              lock (updatedProjects) {
                updatedProjects = newUpdatedProjects;
              }

              foreach (var project in oldProjectDict) {
                if (!newProjectDict.ContainsKey(project.Key)) {
                  DispatchCallbacks(projectRemovedCallbacks, project.Value);
                }
              }

              lock (projects) {
                projects = newProjects;
              }
              updateCallbacks.ForEach(c => c.Invoke());
            } else if (!connected) {
              // Need to send an update even if nothing changed (to update the tray icon)
              connected = true;
              updateCallbacks.ForEach(c => c.Invoke());
            }

            if (!loaded) {
              loaded = true;
              loadedCallbacks.ForEach(c => c.Invoke());
            }
          }
        } catch (Exception e) {
          Logger.LogException(e);
        }

        Thread.Sleep(monitorDelay);
      }
    }

    private bool InitializeProject(Project p) {
      string dir = GetProjectDirectory();
      string pdir = Util.PathCombine(dir, p.name);
      ProcessReturn ret;

      int oldActive = activeProjectId;
      activeProjectId = p.id;

      try {
        if (Directory.Exists(pdir)) {
          if (!Directory.Exists(Util.PathCombine(pdir, ".git"))) {
            // Re-generate the .git directory only. Make a clone in a temp directory:
            string tempPath = Path.Combine(Util.GetTempPath(), p.name);
            if (Directory.Exists(tempPath)) {
              Directory.Delete(tempPath, true);
            }
            ret = GitWrapper.Clone(Util.GetTempPath(), p, "-n");
            if (ret.ReturnValue != 0) return false;
            Directory.Move(Path.Combine(tempPath, ".git"), Path.Combine(pdir, ".git"));
            return true;
          } else {
            return false;
          }
        }

        ret = GitWrapper.Clone(dir, p);
        if (ret.ReturnValue != 0) return false;
        dir = GetProjectDirectory(p);
        File.WriteAllText(Util.PathCombine(dir, ".git", "info", "attributes"), "* -merge -diff");
        // Ignore some common temporary files.
        string[] exclude = new string[] {"*~", "~*", "*.tmp", "*.scigitUpdated*", "*.swp"};
        File.WriteAllText(Util.PathCombine(dir, ".git", "info", "exclude"), String.Join("\n", exclude));
        return true;
      } catch (Exception ex) {
        Logger.LogException(ex);
        return false;
      } finally {
        activeProjectId = oldActive;
      }
    }

    private ProcessReturn CheckReturn(string command, ProcessReturn ret, BackgroundWorker worker) {
      worker.ReportProgress(-1, command + ": " + ret.Output);
      if (ret.ReturnValue != 0) {
        throw new Exception(command + ": " + ret.Output);
      }
      return ret;
    }

    private void ShowError(Window window, string message) {
      window.Dispatcher.Invoke(new Action(() => 
        MessageBox.Show(window, message, "Error")
      ));
    }

    public bool UpdateProject(Project p, Window window, BackgroundWorker worker, bool progress = true) {
      activeProjectId = p.id;

      string dir = GetProjectDirectory(p);
      if (!Directory.Exists(dir)) {
        worker.ReportProgress(progress ? 25 : -1, "Repairing project...");
        MessageBoxResult result = Util.ShowMessageBox("Project " + p.name + " does not exist. Would you like to re-create it?",
                                                      "Project does not exist", MessageBoxButton.YesNo);
        if (result == MessageBoxResult.Yes) {
          if (!InitializeProject(p)) {
            ShowError(window, "Could not obtain project from the SciGit servers. Please try again later.");
          } else {
            worker.ReportProgress(progress ? 100 : -1, "Repair successful.");
            return true;
          }
        }
        return false;
      }

      bool possibleCommit = false, rebaseStarted = false, success = false;
      ProcessReturn ret;

      try {
        if (GitWrapper.RebaseInProgress(dir)) {
          GitWrapper.Rebase(dir, "--abort");
        }
        if (worker.CancellationPending) return false;

        worker.ReportProgress(progress ? 25 : -1, "Checking for updates...");
        ret = GitWrapper.Fetch(dir);
        worker.ReportProgress(-1, ret.Output);
        if (ret.ReturnValue != 0) {
          if (ret.Output.Contains("Not a git")) {
            MessageBoxResult res = Util.ShowMessageBox("Project " + p.name + " seems to be corrupted. Do you want SciGit to repair it?\r\n" +
              "You may want to back up your files first.", "Project corrupted", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.Yes) {
              string gitDir = Path.Combine(dir, ".git");
              if (Directory.Exists(gitDir)) {
                Directory.Delete(gitDir, true);
              }
              worker.ReportProgress(progress ? 30 : -1, "Repairing project...");
              if (!InitializeProject(p)) {
                ShowError(window, "Could not obtain project from the SciGit servers. Please try again later.");
                return false;
              }
              worker.ReportProgress(progress ? 35 : -1, "Checking for updates...");
              ret = GitWrapper.Fetch(dir);
              worker.ReportProgress(-1, ret.Output);
            } else {
              return false;
            }
          }
          if (ret.ReturnValue != 0) {
            ShowError(window, "Could not connect to the SciGit servers. Please try again later.");
            return false;
          }
        }

        if (worker.CancellationPending) return false;

        // Reset commits until we get to something in common with FETCH_HEAD.
        ret = CheckReturn("merge-base", GitWrapper.MergeBase(dir, "HEAD", "FETCH_HEAD"), worker);
        string baseCommit = ret.Stdout.Trim();
        GitWrapper.Reset(dir, baseCommit);
        if (worker.CancellationPending) return false;

        // Make a temporary commit to facilitate merging.
        GitWrapper.AddAll(dir);
        worker.ReportProgress(-1, "Creating temporary commit...");
        ret = GitWrapper.Commit(dir, "tempCommit " + DateTime.Now);
        possibleCommit = true;
        worker.ReportProgress(-1, ret.Output);
        if (worker.CancellationPending) return false;

        worker.ReportProgress(progress ? 50 : -1, "Merging...");
        ret = GitWrapper.Rebase(dir, "FETCH_HEAD");
        worker.ReportProgress(-1, ret.Output);

        if (ret.Output.Contains("Permission denied")) {
          // One of the files is open.
          Util.ShowMessageBox("One of the project files is currently open and cannot be edited. "
            + "Please save and close your changes before continuing.", "File Locked");
          // If the return value isn't 0, there was a merge conflict on top of this (so abort the rebase)
          if (ret.ReturnValue == 0) {
            GitWrapper.Reset(dir, baseCommit);
          } else {
            rebaseStarted = true;
          }
        } else if (ret.ReturnValue != 0) {
          rebaseStarted = true;
          if (worker.CancellationPending) return false;
          if (ret.Output.Contains("CONFLICT")) {
            const string dialogMsg =
              "Merge conflict(s) were detected. Would you like to resolve them now using the SciGit editor?\r\n" +
              "Please save any changes to open files before continuing.";
            MessageBoxResult resp = MessageBoxResult.Cancel;
            window.Dispatcher.Invoke(
              new Action(() => resp = MessageBox.Show(window, dialogMsg, "Merge Conflict", MessageBoxButton.OKCancel)));
            MergeResolver mr = null;
            Exception exception = null;
            if (resp == MessageBoxResult.OK) {
              window.Dispatcher.Invoke(new Action(() => {
                try {
                  mr = new MergeResolver(p);
                  mr.ShowDialog();
                } catch (Exception e) {
                  if (mr != null) mr.Hide();
                  exception = e;
                }
              }));
            }
            if (exception != null) throw new Exception("", exception);

            if (resp != MessageBoxResult.No && (mr == null || !mr.Saved)) {
              // Cancel the process here.
              return false;
            } else {
              GitWrapper.AddAll(dir);
              worker.ReportProgress(progress ? 75 : -1, "Continuing merge...");
              ret = GitWrapper.Rebase(dir, "--continue");
              worker.ReportProgress(-1, ret.Output);
              if (ret.ReturnValue != 0) {
                if (ret.Output.Contains("No changes")) {
                  // The temp commit was effectively ignored. Just skip it.
                  CheckReturn("rebase", GitWrapper.Rebase(dir, "--skip"), worker);
                } else {
                  throw new Exception("rebase: " + ret.Output);
                }
              } 
              worker.ReportProgress(progress ? 100 : -1, "Merge successful.");
              success = true;
              rebaseStarted = false;
            }
          } else {
            throw new Exception("rebase: " + ret.Output);
          }
        } else {
          worker.ReportProgress(progress ? 100 : -1, ret.Output.Contains("up to date") ? "No changes." : "Changes merged without conflict.");
          success = true;
          if (progress && !ret.Output.Contains("up to date")) {
            var result = Util.ShowMessageBox("Project " + p.name + " was successfully updated. Would you like to view the changes?",
                "Project updated", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes) {
              window.Dispatcher.Invoke(new Action(() => {
                var ph = new ProjectHistory(p, "HEAD");
                ph.Show();
              }));
            }
          }
        }
      } catch (Exception e) {
        throw new Exception("", e);
      } finally {
        if (rebaseStarted) GitWrapper.Rebase(dir, "--abort");
        if (possibleCommit) {
          // Reset commits until we get to something in common with FETCH_HEAD.
          ret = CheckReturn("merge-base", GitWrapper.MergeBase(dir, "HEAD", "FETCH_HEAD"), worker);
          CheckReturn("reset", GitWrapper.Reset(dir, ret.Stdout.Trim()), worker);
        }
        if (success) {
          lock (updatedProjects) {
            updatedProjects.RemoveAll(pr => pr.id == p.id);
          }
          updateCallbacks.ForEach(c => c.Invoke());
        }
        activeProjectId = 0;
      }

      return success;
    }

    public bool UploadProject(Project p, Window window, BackgroundWorker worker, bool progress = true) {
      activeProjectId = p.id;

      Project updatedProject;
      lock (projects) {
        updatedProject = projects.Find(pr => pr.id == p.id);
      }
      if (!updatedProject.can_write) {
        window.Dispatcher.Invoke(new Action(() =>
          MessageBox.Show(window, "You don't have write access to this project.", "Not Authorized")
        ));
        return false;
      }
      string dir = GetProjectDirectory(p);

      bool committed = false, success = false;
      string commitMsg = null;
      try {
        while (true) {
          worker.ReportProgress(progress ? 25 : -1, "First, checking for updates...");
          if (!UpdateProject(p, window, worker, false)) {
            return false;
          }
          if (worker.CancellationPending) return false;

          GitWrapper.AddAll(dir);
          worker.ReportProgress(progress ? 50 : -1, "Saving your changes...");
          ProcessReturn ret = CheckReturn("status", GitWrapper.Status(dir), worker);
          if (worker.CancellationPending) return false;
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
            CheckReturn("commit", GitWrapper.Commit(dir, commitMsg), worker);
            committed = true;
            if (worker.CancellationPending) return false;

            worker.ReportProgress(progress ? 75 : -1, "Uploading...");
            ret = GitWrapper.Push(dir);
            worker.ReportProgress(-1, ret.Output);
            if (ret.ReturnValue == 0) {
              worker.ReportProgress(progress ? 100 : -1, "Upload successful.");
              break;
            } else if (ret.Output.Contains("non-fast-forward")) {
              if (worker.CancellationPending) return false;
              committed = false;
              CheckReturn("reset", GitWrapper.Reset(dir, "HEAD^"), worker);
              MessageBoxResult resp = MessageBoxResult.Cancel;
              window.Dispatcher.Invoke(new Action(() =>
                resp = MessageBox.Show(window,
                  "Additional updates must be merged in. Continue?",
                  "Project Updated", MessageBoxButton.OKCancel)));
              if (resp == MessageBoxResult.Cancel) {
                return false;
              }
            } else if (ret.Output.Contains("not authorized")) {
              ShowError(window, "You don't have write access to this project anymore.");
              return false;
            } else {
              ShowError(window, "Could not connect to the SciGit servers. Please try again later.");
              return false;
            }
          } else {
            worker.ReportProgress(progress ? 100 : -1, "No changes to upload.");
            break;
          }
        }
        success = true;
        return true;
      } catch (Exception e) {
        throw e;
      } finally {
        if (!success && committed) {
          CheckReturn("reset", GitWrapper.Reset(dir, "HEAD^"), worker);
        }
        updateCallbacks.ForEach(c => c.Invoke());
        activeProjectId = 0;
      }
    }

    public bool UpdateAllProjects(Window window, BackgroundWorker worker) {
      lock (projects) {
        for (int i = 0; i < projects.Count; i++) {
          var project = projects[i];
          worker.ReportProgress(100 * (i + 1) / (projects.Count + 1), "Updating " + project.name + "...");
          bool cancelled = !UpdateProject(project, window, worker, false);
          if (worker.CancellationPending && (i+1 != projects.Count || cancelled)) {
            return false;
          }
        }
      }
      worker.ReportProgress(100, "Finished.");
      return true;
    }

    public bool UploadAllProjects(Window window, BackgroundWorker worker) {
      lock (projects) {
        for (int i = 0; i < projects.Count; i++) {
          var project = projects[i];
          if (!project.can_write) continue;
          worker.ReportProgress(100 * (i + 1) / (projects.Count + 1), "Uploading " + project.name + "...");
          bool cancelled = !UploadProject(project, window, worker, false);
          if (worker.CancellationPending && (i+1 != projects.Count || cancelled)) {
            return false;
          }
        }
      }
      worker.ReportProgress(100, "Finished.");
      return true;
    }

    public bool HasUpdate(Project p) {
      string dir = GetProjectDirectory(p);
      if (!Directory.Exists(dir)) return false;

      ProcessReturn ret = GitWrapper.GetLastCommit(dir);
      string lastHash = "";
      if (ret.ReturnValue == 0) {
        lastHash = ret.Stdout.Trim();
      }
      return lastHash != p.last_commit_hash;
    }

    public bool HasUpload(Project p) {
      string dir = GetProjectDirectory(p);
      if (!Directory.Exists(dir)) return false;

      ProcessReturn ret = GitWrapper.Status(dir);
      return ret.Stdout.Trim() != "";
    }

    private void FileChanged(object sender, FileSystemEventArgs e) {
      string filename = e.FullPath;
      Project p = GetProjectFromFilename(ref filename);
      if (p.id != 0) {
        lock (editedProjects) {
          bool contains = editedProjects.Find(pr => pr.id == p.id).id != 0;
          if (HasUpload(p)) {
            if (!contains) {
              editedProjects.Add(p);
              if (connected && p.can_write && activeProjectId != p.id) {
                DispatchCallbacks(projectEditedCallbacks, p);
                updateCallbacks.ForEach(c => c.Invoke());
              }
            }
          } else if (contains) {
            editedProjects.RemoveAll(pr => pr.id == p.id);
            if (connected && activeProjectId != p.id) {
              updateCallbacks.ForEach(c => c.Invoke());
            }
          }
        }
      }
    }

    private static string DefaultProjectDirectory() {
      return Util.PathCombine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SciGit");
    }

    private static void CreateProjectDirectory() {
      try {
        Directory.CreateDirectory(GetProjectDirectory());
      } catch (Exception) {
        Settings.Default.ProjectDirectory = projectDirectory = DefaultProjectDirectory();
        Util.ShowMessageBox("Invalid project directory. Reverting to default (" + 
          projectDirectory + ")", "Error");
        Settings.Default.Save();
        if (!Directory.Exists(projectDirectory)) {
          Directory.CreateDirectory(projectDirectory);
        }
      }
    }

    public static string GetProjectDirectory() {
      if (projectDirectory == null) return null;
      lock (projectDirectory) {
        return projectDirectory;
      }
    }

    public static string GetProjectDirectory(Project p) {
      if (p.id == 0) {
        return Path.GetTempPath(); // for testing
      }
      return Util.PathCombine(GetProjectDirectory(), p.name);
    }
  }
}
