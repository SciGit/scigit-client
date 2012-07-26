using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace SciGit_Client
{
  class GitWrapper
  {
    public const string ServerHost = "git@scigit.sherk.me";

    private static int ExecuteGitCommand(string args) {
      ProcessStartInfo startInfo = new ProcessStartInfo();
      startInfo.FileName = "git.exe";
      startInfo.Arguments = args;
      startInfo.WindowStyle = ProcessWindowStyle.Hidden;
      Process process = new Process();
      process.StartInfo = startInfo;
      process.Start();
      process.WaitForExit();
      return process.ExitCode;
    }

    public static bool Clone(Project p) {
      return ExecuteGitCommand(String.Format("clone {0}:r{1} \"{2}\"", ServerHost, p.Id, p.Name)) == 0;
    }
  }
}
