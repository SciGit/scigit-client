using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace SciGit_Client
{
  class GitReturn
  {
    public GitReturn(int ret, string str) {
      ReturnValue = ret;
      Output = str;
    }
    public int ReturnValue { get; set; }
    public string Output { get; set; }
  }

  class GitWrapper
  {
    public const string ServerHost = "git@scigit.sherk.me";

    private static GitReturn ExecuteGitCommand(string args) {
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
      string shellOutput = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
      process.WaitForExit();
      return new GitReturn(process.ExitCode, shellOutput);
    }

    private static string EscapeShellArg(string s) {
      // We assume s does not contain any shell metacharacters (()%!^"<>&|;,)
      return '"' + s + '"';
    }

    public static GitReturn Clone(Project p) {
      return ExecuteGitCommand(String.Format("clone {0}:r{1} {2}", ServerHost, p.Id, EscapeShellArg(p.Name)));
    }

    public static GitReturn Add(string args) {
      return ExecuteGitCommand("add " + args);
    }

    public static GitReturn Commit(string msg) {
      return ExecuteGitCommand(String.Format("commit -a -m {0}", EscapeShellArg(msg)));
    }

    public static GitReturn Pull() {
      return ExecuteGitCommand("pull");
    }

    public static GitReturn Push() {
      return ExecuteGitCommand("push origin master");
    }
  }
}
