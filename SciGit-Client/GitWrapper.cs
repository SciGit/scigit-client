using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using SciGit_Client.Properties;

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

  class AsyncStreamReader
  {
    private string data = "";
    private byte[] buffer;
    private const int bufferSize = 1024;
    private Stream stream;
    private Thread t;

    public AsyncStreamReader(Stream stream) {
      buffer = new byte[bufferSize];
      this.stream = stream;
      t = new Thread(new ThreadStart(this.BeginRead));
      t.Start();
    }

    public string GetData() {
      t.Join();
      return data;
    }

    private void BeginRead() {
      while (true) {
        IAsyncResult ar = this.stream.BeginRead(buffer, 0, bufferSize, null, null);
        int bytes = stream.EndRead(ar);
        if (bytes == 0) {
          return;
        }
        data += Encoding.Default.GetString(buffer, 0, bytes);
      }
    }
  }

  class GitWrapper
  {
    public static readonly string ServerHost = App.Hostname;
    public const int ProcessTimeout = 30000;

    public static string GetAppDataPath() {
      return Util.PathCombine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SciGit");
    }

    private static ProcessReturn ExecuteCommand(string args, string dir = "", string exe = "git.exe") {
      string appPath = Path.GetDirectoryName(Application.ExecutablePath);
      exe = Util.PathCombine(appPath, "Libraries", "git", "bin", exe);

      var startInfo = new ProcessStartInfo {
        FileName = exe,
        Arguments = args,
        CreateNoWindow = true,
        RedirectStandardError = true,
        RedirectStandardOutput = true,
        UseShellExecute = false,
        WorkingDirectory = dir
      };
      startInfo.EnvironmentVariables["HOME"] = Util.PathCombine(GetAppDataPath(), RestClient.Username);
      var process = new Process {StartInfo = startInfo};
      process.Start();
      AsyncStreamReader stdout = new AsyncStreamReader(process.StandardOutput.BaseStream), 
                        stderr = new AsyncStreamReader(process.StandardError.BaseStream);
      if (!process.WaitForExit(ProcessTimeout)) {
        return new ProcessReturn(-1, "", "Process timed out.");
      }
      Thread.Sleep(50);
      return new ProcessReturn(process.ExitCode, stdout.GetData(), stderr.GetData());
    }

    private static string EscapeShellArg(string s) {
      // We assume s does not contain any shell metacharacters (()%!^"<>&|;,)
      return '"' + s + '"';
    }

    public static ProcessReturn Clone(string dir, Project p) {
      return ExecuteCommand(String.Format("clone git@{0}:r{1} {2}", ServerHost, p.id, EscapeShellArg(p.name)), dir);
    }

    public static ProcessReturn AddAll(string dir) {
      return ExecuteCommand("add -A", dir);
    }

    public static ProcessReturn Commit(string dir, string msg, string options = "") {
      return ExecuteCommand(String.Format("commit -a -m {0} {1}", EscapeShellArg(msg), options), dir);
    }

    public static ProcessReturn Fetch(string dir, string options = "") {
      return ExecuteCommand("fetch " + options, dir);
    }

    public static ProcessReturn Log(string dir, string options = "") {
      return ExecuteCommand("log --pretty=\"%H %ae %at %s\" " + options, dir);
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
      return Directory.Exists(Util.PathCombine(dir, ".git", "rebase-merge")) ||
             Directory.Exists(Util.PathCombine(dir, ".git", "rebase-apply"));
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
