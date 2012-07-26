using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SciGit_Client
{
  class SGProjectManager
  {
    public List<Project> Projects { get; private set; }

    public SGProjectManager(List<Project> projects) {
      if (!Directory.Exists(GetProjectDirectory())) {
        CreateProjectDirectory();
      }

      /* TODO:
       * - check Git/ssh installations
       * - add SSH public key to SciGit
       * - make sure the scigit server is a known_host / override that
       */

      Projects = projects;
      foreach (var project in Projects) {
        InitializeProject(project);
      }
    }

    private void InitializeProject(Project p) {
      Directory.SetCurrentDirectory(GetProjectDirectory());
      if (!Directory.Exists(p.Name)) {
        GitWrapper.Clone(p);
      }
    }

    private static void CreateProjectDirectory() {
      Directory.CreateDirectory(GetProjectDirectory());
      // TODO: add special shell properties
    }

    public static string GetProjectDirectory() {
      string homepath = (Environment.OSVersion.Platform == PlatformID.Unix ||
              Environment.OSVersion.Platform == PlatformID.MacOSX)
          ? Environment.GetEnvironmentVariable("HOME")
          : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
      return homepath + Path.DirectorySeparatorChar + "SciGit";
    }
  }
}
