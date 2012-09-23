using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;

namespace SciGit_Client
{
  class ProcessReturn
  {
    public ProcessReturn(int ret, string stdout, string stderr) {
      ReturnValue = ret;
      Stdout = stdout;
      Stderr = stderr;
    }
    public int ReturnValue { get; set; }
    public string Stdout { get; set; }
    public string Stderr { get; set; }
    public string Output { get { return Stdout + Stderr; } }
  }

  class GitWrapper
  {
    public const string ServerHost = "stage.scigit.sherk.me";

    public static string GetAppDataPath() {
      return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SciGit");
    }

    private static ProcessReturn ExecuteCommand(string args, string dir = "", string exe = "git.exe") {
      string appPath = Path.GetDirectoryName(Application.ExecutablePath);
      exe = Path.Combine(appPath, "Libraries", "git", "bin", exe);

      ProcessStartInfo startInfo = new ProcessStartInfo();
      startInfo.FileName = exe;
      startInfo.Arguments = args;
      startInfo.CreateNoWindow = true;
      startInfo.RedirectStandardError = true;
      startInfo.RedirectStandardOutput = true;
      startInfo.UseShellExecute = false;
      startInfo.EnvironmentVariables["HOME"] = Path.Combine(GitWrapper.GetAppDataPath(), RestClient.username);
      startInfo.WorkingDirectory = dir;
      Process process = new Process();
      process.StartInfo = startInfo;
      process.Start();
      process.WaitForExit();
      return new ProcessReturn(process.ExitCode, process.StandardOutput.ReadToEnd(), process.StandardError.ReadToEnd());
    }

    private static string EscapeShellArg(string s) {
      // We assume s does not contain any shell metacharacters (()%!^"<>&|;,)
      return '"' + s + '"';
    }

    public static ProcessReturn Clone(string dir, Project p) {
      return ExecuteCommand(String.Format("clone git@{0}:r{1} {2}", ServerHost, p.Id, EscapeShellArg(p.Name)), dir);
    }

    public static ProcessReturn AddAll(string dir) {
      return ExecuteCommand("add -A", dir);
    }

    public static ProcessReturn Commit(string dir, string msg, string options = "") {
      return ExecuteCommand(String.Format("commit -a -m {0} {1}", EscapeShellArg(msg), options), dir);
    }

    public static ProcessReturn Diff(string dir, string options = "") {
      return ExecuteCommand("diff " + options, dir);
    }

    public static ProcessReturn Fetch(string dir, string options = "") {
      return ExecuteCommand("fetch " + options, dir);
    }

    public static ProcessReturn Log(string dir, string options = "") {
      return ExecuteCommand("log " + options, dir);
    }

    public static ProcessReturn GetLastCommit(string dir, string obj = "") {
      return ExecuteCommand("log --pretty=%H -n 1 " + obj, dir);
    }

    public static ProcessReturn Push(string dir, string options = "") {
      return ExecuteCommand("push origin master " + options, dir);
    }

    public static ProcessReturn Merge(string dir, string options = "") {
      return ExecuteCommand("merge " + options, dir);
    }

    public static ProcessReturn MergeBase(string dir, string obj1, string obj2, string options = "") {
      return ExecuteCommand("merge-base " + obj1 + " " + obj2 + " " + options, dir);
    }

    public static ProcessReturn Rebase(string dir, string options = "") {
      return ExecuteCommand("rebase " + options, dir);
    }

    public static bool RebaseInProgress(string dir) {
      return Directory.Exists(Path.Combine(dir, ".git", "rebase-merge")) ||
             Directory.Exists(Path.Combine(dir, ".git", "rebase-apply"));
    }

    public static ProcessReturn Reset(string dir, string options = "") {
      return ExecuteCommand("reset " + options, dir);
    }

    public static ProcessReturn Status(string dir, string options = "") {
      return ExecuteCommand("status --porcelain -z " + options, dir);
    }

    public static ProcessReturn ListUnmergedFiles(string dir) {
      return ExecuteCommand("ls-files -u -z", dir);
    }

    public static ProcessReturn ShowObject(string dir, string hash) {
      return ExecuteCommand("show " + hash, dir);
    }

    public static ProcessReturn GlobalConfig(string key, string value) {
      return ExecuteCommand(String.Format("config --global {0} {1}", key, EscapeShellArg(value)));
    }

    public static ProcessReturn GenerateSSHKey(string keyFile) {
      return ExecuteCommand(String.Format("-t rsa -f '{0}' -P ''", keyFile), "", "ssh-keygen.exe");
    }

    public static ProcessReturn RemoveHostSSHKey(string hostName) {
      return ExecuteCommand("-R " + hostName, "", "ssh-keygen.exe");
    }

    public static ProcessReturn GetHostSSHKey(string hostName) {
      return ExecuteCommand("-t rsa " + hostName, "", "ssh-keyscan.exe");
    }
  }
}
