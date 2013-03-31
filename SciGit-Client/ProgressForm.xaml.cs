using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
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

    public ProgressForm(string title, BackgroundAction action) {
      InitializeComponent();
      Style = (Style)FindResource(typeof(Window));

      Title = title;
      detailTextBox.Visibility = Visibility.Collapsed;
      
      bg = new BackgroundWorker();
      bg.WorkerReportsProgress = true;
      bg.WorkerSupportsCancellation = true;
      bg.DoWork += (bw, dwe) => {
        bool owned;
        var mutex = new Mutex(true, "SciGitOperationMutex", out owned);
        if (!owned) {
          status.Dispatcher.Invoke(new Action(() => status.Text = "Waiting for other operations to finish..."));
          this.Dispatcher.Invoke(new Action(() => close.IsEnabled = true));
          mutex.WaitOne();
          this.Dispatcher.Invoke(new Action(() => close.IsEnabled = false));
        }
        this.Dispatcher.Invoke(new Action(() => cancel.IsEnabled = true));

        try {
          if (!action(this, (BackgroundWorker)bw)) {
            dwe.Cancel = true;
          }
        } catch (Exception e) {
          this.Dispatcher.Invoke(new Action(() => {
            ErrorForm.ShowDialog(this, e);
            status.Text = "Error.";
          }));
        }
        mutex.ReleaseMutex();
      };
      bg.ProgressChanged += UpdateProgress;
      bg.RunWorkerCompleted += Completed;
      bg.RunWorkerAsync();
    }

    private void SetProgressValue(int percentage) {
      var duration = new Duration(TimeSpan.FromSeconds(0.5));
      var anim = new DoubleAnimation(percentage, duration);
      progressBar.BeginAnimation(RangeBase.ValueProperty, anim);
    }

    public void UpdateProgress(object sender, ProgressChangedEventArgs e) {
      var data = (string)e.UserState;
      if (e.ProgressPercentage >= 0) {
        SetProgressValue(e.ProgressPercentage);
        status.Text = data;
        detailTextBox.Text += data + "\r\n";
      } else if (data.Length > 0) {
        string str = data.Replace("\n", "\r\n");
        if (!str.EndsWith("\r\n")) str += "\r\n";
        detailTextBox.Text += str;
      }
    }

    public void Completed(object sender, RunWorkerCompletedEventArgs e) {
      SetProgressValue(100);
      if (e.Cancelled) {
        status.Text = "Cancelled.";
        cancel.Content = "Cancelled.";
      }
      cancel.IsEnabled = false;
      close.IsEnabled = true;

      // Stop progress animation *hack*
      var glow = progressBar.Template.FindName("Animation", progressBar) as FrameworkElement;
      if (glow != null) glow.Visibility = Visibility.Hidden;
    }

    private void details_Click(object sender, EventArgs e) {
      if (detailTextBox.Visibility == Visibility.Collapsed) {
        detailTextBox.Visibility = Visibility.Visible;
        details.IsChecked = true;
      } else {
        detailTextBox.Visibility = Visibility.Collapsed;
        details.IsChecked = false;
      }
    }

    private void cancel_Click(object sender, EventArgs e) {
      bg.CancelAsync();
      cancel.Content = "Cancelling...";
      cancel.IsEnabled = false;
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
