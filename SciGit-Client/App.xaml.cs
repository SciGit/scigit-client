using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Threading;

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
        if (!owned) {
          MessageBox.Show("An instance of SciGit is already open.", "Existing instance");
          Environment.Exit(0);
        }
      }
    }
}