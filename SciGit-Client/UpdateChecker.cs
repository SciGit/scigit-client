﻿using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace SciGit_Client
{
  class UpdateChecker
  {
    private Thread thread;
    private const int retryIntervalMs = 5 * 1000;
    private const int updateIntervalMs = 3600*1000; // once per hour
    private Form parent;

    public UpdateChecker(Form parent) {
      this.parent = parent;
      thread = new Thread(new ThreadStart(CheckForUpdates));
    }

    public void Start() {
      thread.Start();
    }

    public void Stop() {
      thread.Abort();
    }

    private void CheckForUpdates() {
      try {
        while (true) {
          var resp = RestClient.GetLatestClientVersion();
          string newVersion = resp.Data;
          if (newVersion != null) {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            Version x = new Version(fvi.ProductVersion), y = new Version(newVersion);
            if (x < y) {
              var result = MessageBox.Show(
                String.Format("A new version ({0}) of the SciGit client is available. Would you like to update now?", newVersion),
                "Update Available", MessageBoxButton.YesNo);
              if (result == MessageBoxResult.Yes) {
                Process.Start("http://" + App.Hostname + "/download");
              }
              // Don't pester the user any longer.
              break;
            }
            Thread.Sleep(updateIntervalMs);
          } else {
            Thread.Sleep(retryIntervalMs);
          }
        }
      } catch (ThreadAbortException) {
        // Normal, just ignore
      } catch (Exception ex) {
        Thread.Sleep(2000);
        parent.Invoke(new Action(() => Util.HandleException(ex)));
      }
    }
  }
}
