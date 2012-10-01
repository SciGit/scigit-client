using System;
using System.IO.Pipes;
using System.Threading;
using System.Windows;

namespace SciGit_Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
      static Mutex mutex;

      public App() {
        bool owned;
        mutex = new Mutex(true, "SciGitApplicationMutex", out owned);
        string[] args = Environment.GetCommandLineArgs();
        if (!owned) {
          if (args.Length == 3) {
            var pipeClient = new NamedPipeClientStream(".", "sciGitPipe", PipeDirection.Out);
            try {
              pipeClient.Connect(1000);
              var ss = new StreamString(pipeClient);
              ss.WriteString(args[1]);
              ss.WriteString(args[2]);
            } catch (Exception) {
              MessageBox.Show("Please wait for the SciGit client to connect.", "Error");
            }
          } else {
            MessageBox.Show("An instance of SciGit is already open.", "Existing instance");
          }
          Environment.Exit(0);
        }

        // If there is no instance open, then process the arguments after everything has loaded.
      }
    }
}