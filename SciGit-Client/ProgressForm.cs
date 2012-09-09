using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Threading;

namespace SciGit_Client
{
  public partial class ProgressForm : Form
  {
    public delegate void BackgroundAction(Form form, Dispatcher disp, BackgroundWorker bw);

    public ProgressForm(Dispatcher disp, BackgroundAction action) {
      InitializeComponent();

      BackgroundWorker bg = new BackgroundWorker();
      bg.WorkerReportsProgress = true;
      bg.DoWork += (bw, _) => action(this, disp, (BackgroundWorker)bw);
      bg.ProgressChanged += UpdateProgress;
      bg.RunWorkerCompleted += Completed;
      bg.RunWorkerAsync();
    }

    public void UpdateProgress(object sender, ProgressChangedEventArgs e) {
      this.progressBar1.Value = e.ProgressPercentage;
      Tuple<String, String> status = (Tuple<String, String>)e.UserState;
      this.label1.Text = status.Item1;
      if (status.Item2.Length > 0) {
        this.textBox1.Text += status.Item2.Replace("\n", "\r\n") + "\r\n";
      }
    }

    public void Completed(object sender, RunWorkerCompletedEventArgs e) {
      this.progressBar1.Value = 100;
      this.close.Enabled = true;
    }

    private void details_Click(object sender, EventArgs e) {
      this.textBox1.Visible ^= true;
    }

    private void close_Click(object sender, EventArgs e) {
      Close();
    }
  }
}
