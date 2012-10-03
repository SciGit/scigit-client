using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using System.Windows.Shell;
using System.Windows.Threading;

namespace SciGit_Client
{
  /// <summary>
  /// Interaction logic for ProgressForm2.xaml
  /// </summary>
  public partial class ProgressForm : Window
  {
    #region Delegates

    public delegate bool BackgroundAction(Window wind, BackgroundWorker bw);

    #endregion

    BackgroundWorker bg;

    public ProgressForm(BackgroundAction action) {
      InitializeComponent();

      textBox.Visibility = Visibility.Collapsed;
      TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
      
      bg = new BackgroundWorker();
      bg.WorkerReportsProgress = true;
      bg.WorkerSupportsCancellation = true;
      bg.DoWork += (bw, dwe) => {
        bool owned;
        var mutex = new Mutex(true, "SciGitOperationMutex", out owned);
        if (!owned) {
          status.Dispatcher.Invoke(new Action(() => status.Text = "Waiting for other operations to finish..."));
          mutex.WaitOne();
        }

        try {
          if (!action(this, (BackgroundWorker)bw)) {
            dwe.Cancel = true;
          }
        } catch (Exception e) {
          this.Dispatcher.Invoke(new Action(() => {
            MessageBox.Show(this, e.Message, "Error");
            status.Text = "Error.";
          }));
        }
        mutex.ReleaseMutex();
      };
      bg.ProgressChanged += UpdateProgress;
      bg.RunWorkerCompleted += Completed;

      bg.RunWorkerAsync();
    }

    public void UpdateProgress(object sender, ProgressChangedEventArgs e) {
      var data = (string)e.UserState;
      if (e.ProgressPercentage >= 0) {
        var duration = new Duration(TimeSpan.FromSeconds(0.5));
        var anim = new DoubleAnimation(e.ProgressPercentage, duration);
        progressBar.BeginAnimation(RangeBase.ValueProperty, anim);
        progressBar.Value = e.ProgressPercentage;
        TaskbarItemInfo.ProgressValue = (double) (e.ProgressPercentage)/100;
        status.Text = data;
      } else if (data.Length > 0) {
        string str = data.Replace("\n", "\r\n");
        if (!str.EndsWith("\r\n")) str += "\r\n";
        textBox.Text += str;
      }
    }

    public void Completed(object sender, RunWorkerCompletedEventArgs e) {
      TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
      progressBar.Value = 100;
      if (e.Cancelled) {
        status.Text = "Cancelled.";
      }
      close.IsEnabled = true;
    }

    private void details_Click(object sender, EventArgs e) {
      textBox.Visibility = (textBox.Visibility == Visibility.Collapsed) ?
        Visibility.Visible : Visibility.Collapsed;
    }

    private void cancel_Click(object sender, EventArgs e) {
      bg.CancelAsync();
    }

    private void close_Click(object sender, EventArgs e) {
      Close();
    }

    protected override void OnClosing(CancelEventArgs e) {
      base.OnClosing(e);
      if (!close.IsEnabled) {
        e.Cancel = true;
      }
    }
  }
}
