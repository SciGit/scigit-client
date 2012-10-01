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

    public delegate void BackgroundAction(Window wind, Dispatcher disp, BackgroundWorker bw);

    #endregion

    public ProgressForm(Dispatcher disp, BackgroundAction action) {
      InitializeComponent();

      textBox.Visibility = Visibility.Collapsed;
      TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
      
      var bg = new BackgroundWorker();
      bg.WorkerReportsProgress = true;
      bg.DoWork += (bw, _) => {
        var mutex = new Mutex(false, "SciGitOperationMutex");
        mutex.WaitOne();
        action(this, disp, (BackgroundWorker)bw);
        mutex.ReleaseMutex();
      };
      bg.ProgressChanged += UpdateProgress;
      bg.RunWorkerCompleted += Completed;


      status.Text = "Waiting for other operations to finish...";
      bg.RunWorkerAsync();
    }

    public void UpdateProgress(object sender, ProgressChangedEventArgs e) {
      var duration = new Duration(TimeSpan.FromSeconds(0.5));
      var anim = new DoubleAnimation(e.ProgressPercentage, duration);
      progressBar.BeginAnimation(RangeBase.ValueProperty, anim);
      progressBar.Value = e.ProgressPercentage;
      TaskbarItemInfo.ProgressValue = (double)(e.ProgressPercentage) / 100;
      var data = (Tuple<String, String>)e.UserState;
      status.Text = data.Item1;
      if (data.Item2.Length > 0) {
        textBox.Text += data.Item2.Replace("\n", "\r\n") + "\r\n";
      }
    }

    public void Completed(object sender, RunWorkerCompletedEventArgs e) {      
      progressBar.Value = 100;
      close.IsEnabled = true;
    }

    private void details_Click(object sender, EventArgs e) {
      textBox.Visibility = (textBox.Visibility == Visibility.Collapsed) ?
        Visibility.Visible : Visibility.Collapsed;
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
