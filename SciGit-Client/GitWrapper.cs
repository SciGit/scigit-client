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
    public const string ServerHost = "scigit.sherk.me";

    private static GitReturn ExecuteGitCommand(string args, string dir = "") {
      ProcessStartInfo startInfo = new ProcessStartInfo();
      startInfo.FileName = "git.exe";
      startInfo.Arguments = args;
      startInfo.CreateNoWindow = true;
      startInfo.RedirectStandardError = true;
      startInfo.RedirectStandardOutput = true;
      startInfo.UseShellExecute = false;
      startInfo.WorkingDirectory = dir;
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

    public static GitReturn Clone(string dir, Project p) {
      return ExecuteGitCommand(String.Format("clone git@{0}:r{1} {2}", ServerHost, p.Id, EscapeShellArg(p.Name)), dir);
    }

    public static GitReturn Add(string dir, string args) {
      return ExecuteGitCommand("add " + args, dir);
    }

    public static GitReturn Commit(string dir, string msg) {
      return ExecuteGitCommand(String.Format("commit -a -m {0}", EscapeShellArg(msg)), dir);
    }

    public static GitReturn Diff(string dir, string options = "") {
      return ExecuteGitCommand("diff " + options, dir);
    }

    public static GitReturn Fetch(string dir, string options = "") {
      return ExecuteGitCommand("fetch " + options, dir);
    }

    public static GitReturn Log(string dir, string options = "") {
      return ExecuteGitCommand("log " + options, dir);
    }

    public static GitReturn Push(string dir, string options = "") {
      return ExecuteGitCommand("push origin master " + options, dir);
    }

    public static GitReturn Rebase(string dir, string options) {
      return ExecuteGitCommand("rebase " + options, dir);
    }

    public static GitReturn Reset(string dir, string options) {
      return ExecuteGitCommand("reset " + options, dir);
    }

    public static GitReturn ListUnmergedFiles(string dir) {
      return ExecuteGitCommand("ls-files -u", dir);
    }

    public static GitReturn ShowObject(string dir, string hash) {
      return ExecuteGitCommand("show " + hash, dir);
    }
  }
}
