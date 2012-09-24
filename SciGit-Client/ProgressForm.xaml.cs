using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using System.Threading;

namespace SciGit_Client
{
  /// <summary>
  /// Interaction logic for ProgressForm2.xaml
  /// </summary>
  public partial class ProgressForm : Window
  {
    public delegate void BackgroundAction(Window wind, Dispatcher disp, BackgroundWorker bw);

    public ProgressForm(Dispatcher disp, BackgroundAction action) {
      InitializeComponent();

      textBox.Visibility = System.Windows.Visibility.Collapsed;
      TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
      
      BackgroundWorker bg = new BackgroundWorker();
      bg.WorkerReportsProgress = true;
      bg.DoWork += (bw, _) => {
        Mutex mutex = new Mutex(false, "SciGitOperationMutex");
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
      Duration duration = new Duration(TimeSpan.FromSeconds(0.5));
      DoubleAnimation anim = new DoubleAnimation(e.ProgressPercentage, duration);
      progressBar.BeginAnimation(ProgressBar.ValueProperty, anim);
      progressBar.Value = e.ProgressPercentage;
      TaskbarItemInfo.ProgressValue = (double)(e.ProgressPercentage) / 100;
      Tuple<String, String> data = (Tuple<String, String>)e.UserState;
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
      textBox.Visibility = (textBox.Visibility == System.Windows.Visibility.Collapsed) ?
        System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
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
