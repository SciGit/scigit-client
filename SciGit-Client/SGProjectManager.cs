using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.ComponentModel;

namespace SciGit_Client
{
  class SGProjectManager
  {
    public List<Project> projects { get; private set; }
    public delegate void ProjectUpdatedCallback();
    public delegate void ProgressCallback(int percent, string operation, string extra);
    public List<ProjectUpdatedCallback> projectUpdatedCallbacks;
    private Thread monitorThread;
    private const int monitorDelay = 10 * 1000;

    public SGProjectManager(List<Project> projects = null) {
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
      projectUpdatedCallbacks = new List<ProjectUpdatedCallback>();
    }

    public void StartMonitoring() {
      monitorThread.Start();
    }

    public void MonitorProjects() {
      while (true) {
        List<Project> newProjects = SGRestClient.GetProjects();
        if (!newProjects.Equals(projects)) {
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
      Directory.SetCurrentDirectory(GetProjectDirectory());
      if (!Directory.Exists(p.Name)) {
        GitWrapper.Clone(p);
      }
    }

    public void UpdateProject(Project p, BackgroundWorker worker = null) {
      Directory.SetCurrentDirectory(SGProjectManager.GetProjectDirectory(p));

      GitReturn ret;
      if (worker != null) worker.ReportProgress(50, Tuple.Create("Pulling...", ""));
      ret = GitWrapper.Pull();
      if (worker != null) worker.ReportProgress(100, Tuple.Create("Finished.", ret.Output));
    }

    public void UploadProject(Project p, BackgroundWorker worker = null) {
      Directory.SetCurrentDirectory(SGProjectManager.GetProjectDirectory(p));

      GitReturn ret;
      if (worker != null) worker.ReportProgress(20, Tuple.Create("Processing changes...", ""));
      ret = GitWrapper.Add(".");
      if (worker != null) worker.ReportProgress(30, Tuple.Create("Committing...", ret.Output));
      ret = GitWrapper.Commit(DateTime.Now + " commit");
      if (ret.ReturnValue == 0) {
        if (worker != null) worker.ReportProgress(50, Tuple.Create("Merging...", ret.Output));
        ret = GitWrapper.Pull();
        if (worker != null) worker.ReportProgress(70, Tuple.Create("Pushing...", ret.Output));
        ret = GitWrapper.Push();
        if (worker != null) worker.ReportProgress(100, Tuple.Create("Finished.", ret.Output));
      }
    }

    public void UpdateAllProjects(BackgroundWorker worker = null) {
      lock (projects) {
        int done = 0;
        foreach (var project in projects) {
          UpdateProject(project);
          if (worker != null) {
            worker.ReportProgress(100 * ++done / projects.Count, Tuple.Create("Updating...", project.Name));
          }
        }
      }
    }

    public void UploadAllProjects(BackgroundWorker worker = null) {
      lock (projects) {
        int done = 0;
        foreach (var project in projects) {
          UploadProject(project);
          if (worker != null) {
            worker.ReportProgress(100 * ++done / projects.Count, Tuple.Create("Uploading...", project.Name));
          }
        }
      }
    }

    private static void CreateProjectDirectory() {
      Directory.CreateDirectory(GetProjectDirectory());
      // TODO: add special shell properties
    }

    public static string GetProjectDirectory(Project p = null) {
      string path = Environment.GetEnvironmentVariable("HOME") + Path.DirectorySeparatorChar + "SciGit";
      if (p != null) {
        path += Path.DirectorySeparatorChar + p.Name;
      }
      return path;
    }
  }
}
