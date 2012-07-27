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
      startInfo.CreateNoWindow = true;
      startInfo.RedirectStandardError = true;
      startInfo.RedirectStandardOutput = true;
      startInfo.UseShellExecute = false;
      Process process = new Process();
      process.StartInfo = startInfo;
      process.Start();
      Debug.Write(process.StandardError.ReadToEnd());
      Debug.Write(process.StandardOutput.ReadToEnd());
      process.WaitForExit();
      return process.ExitCode;
    }

    private static string EscapeShellArg(string s) {
      // We assume s does not contain any shell metacharacters (()%!^"<>&|;,)
      return '"' + s + '"';
    }

    public static bool Clone(Project p) {
      return ExecuteGitCommand(String.Format("clone {0}:r{1} {2}", ServerHost, p.Id, EscapeShellArg(p.Name))) == 0;
    }

    public static bool Add(string args) {
      return ExecuteGitCommand("add " + args) == 0;
    }

    public static bool Commit(string msg) {
      return ExecuteGitCommand(String.Format("commit -a -m {0}", EscapeShellArg(msg))) == 0;
    }

    public static bool Pull() {
      return ExecuteGitCommand("pull") == 0;
    }

    public static bool Push() {
      return ExecuteGitCommand("push origin master") == 0;
    }
  }
}
