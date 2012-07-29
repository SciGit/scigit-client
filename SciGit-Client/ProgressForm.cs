using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SciGit_Client
{
  public partial class ProgressForm : Form
  {
    public ProgressForm() {
      InitializeComponent();
      this.textBox1.Visible = false;
    }

    public void UpdateProgress(object sender, ProgressChangedEventArgs e) {
      this.progressBar1.Value = e.ProgressPercentage;
      Tuple<String, String> status = (Tuple<String, String>)e.UserState;
      this.label1.Text = status.Item1;
      if (status.Item2.Length > 0) {
        this.textBox1.Text += status.Item2 + "\r\n";
      }
    }

    public void Completed(object sender, RunWorkerCompletedEventArgs e) {
      this.progressBar1.Value = 100;
      this.label1.Text = "Finished.";
      //Close();
    }

    private void details_Click(object sender, EventArgs e) {
      this.textBox1.Visible ^= true;
    }
  }
}
