using System;
using System.IO.Pipes;
using System.Threading;
using System.Windows;
using SciGit_Client.Properties;

namespace SciGit_Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
      private static Mutex mutex;
      public static string Hostname;

      public App() {
        bool owned;
        mutex = new Mutex(true, "SciGitApplicationMutex", out owned);
        string[] args = Environment.GetCommandLineArgs();
        // Process context menu commands (of the form --{command} {file})
        // If there is no instance open, then process the arguments after everything has loaded.
        if (!owned) {
          if (args.Length == 3 && args[1] != "--hostname") {
            var pipeClient = new NamedPipeClientStream(".", "sciGitPipe", PipeDirection.Out);
            try {
              pipeClient.Connect(1000);
              var ss = new StreamString(pipeClient);
              ss.WriteString(args[1]);
              ss.WriteString(args[2]);
            } catch (Exception e) {
              Logger.LogException(e);
              MessageBox.Show("Please wait for the SciGit client to connect.", "Error");
            }
          } else {
            MessageBox.Show("An instance of SciGit is already open.", "Existing Instance");
          }
          Environment.Exit(0);
        }

        // Should the settings be upgraded?
        if (Settings.Default.NewInstall) {
          Settings.Default.Upgrade();
          Settings.Default.NewInstall = false;
          Settings.Default.Save();
        }

        // See if the user provided a custom hostname.
        for (int i = 1; i < args.Length; i++) {
          if (args[i] == "--hostname" && i+1 < args.Length) {
            Hostname = args[i + 1];
            return;
          }
        }

#if STAGE
        Hostname = Settings.Default.StageHostname;
#else
        Hostname = Settings.Default.SciGitHostname;
#endif
      }
    }
}