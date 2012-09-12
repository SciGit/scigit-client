using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Threading;
using Microsoft.Win32;

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
            // Send the command to the existing task.
          }
          MessageBox.Show("An instance of SciGit is already open.", "Existing instance");
          Environment.Exit(0);
        }

        // If there is no instance open, then process the arguments after everything has loaded.
      }
    }
}