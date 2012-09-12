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
using System.IO;
using SciGit_Filter;

namespace SciGit_Client
{
  /// <summary>
  /// Interaction logic for FileHistory.xaml
  /// </summary>
  public partial class FileHistory : Window
  {
    DateTime epoch = new System.DateTime(1970, 1, 1, 0, 0, 0, 0);
    Project project;
    string fullpath;

    public FileHistory(Project p, string filename) {
      InitializeComponent();

      project = p;
      this.filename.Text = "File: " + p.Name + Path.DirectorySeparatorChar + filename;
      string gitFilename = filename.Replace(Path.DirectorySeparatorChar, '/');

      string dir = ProjectMonitor.GetProjectDirectory(p);
      ProcessReturn ret = GitWrapper.Log(dir, String.Format("--pretty=\"%H %ae %at %s\" -- \"{0}\"", filename));
      string[] commits = SentenceFilter.SplitLines(ret.Output.Trim());

      fullpath = dir + Path.DirectorySeparatorChar + filename;
      string curText = File.ReadAllText(fullpath);
      int timestamp = (int)(File.GetLastWriteTimeUtc(fullpath) - epoch).TotalSeconds;
      fileHistory.Items.Add(CreateListViewItem("Current Version", "", timestamp, curText));
      foreach (var commit in commits) {
        string[] data = commit.Split(new char[] { ' ' }, 4);
        ret = GitWrapper.ShowObject(dir, String.Format("{0}:\"{1}\"", data[0], gitFilename));
        if (ret.ReturnValue == 0) {
          fileHistory.Items.Add(CreateListViewItem(data[3], data[1], int.Parse(data[2]), ret.Output));
        }
      }

      fileHistory.SelectedIndex = 0;
    }

    private ListViewItem CreateListViewItem(string message, string author, int time, string text) {
      var item = new ListViewItem();
      StackPanel sp = new StackPanel();
      sp.Orientation = Orientation.Vertical;
      var tb = new TextBlock();
      tb.Text = message;
      tb.FontSize = 12;
      tb.FontWeight = FontWeights.Bold;
      sp.Children.Add(tb);
      DateTime date = epoch.AddSeconds(time);
      tb = new TextBlock();
      tb.Text = (author == "" ? "" : "by " + author + " ") + "on " + date.ToLocalTime().ToString("MMM d, yyyy h:mmtt");
      tb.FontSize = 10;
      sp.Children.Add(tb);
      sp.Margin = new Thickness(2, 5, 5, 5);
      item.Content = sp;
      item.Selected += (s, e) => fileContents.Text = text;
      return item;
    }

    private void ClickRevert(object sender, EventArgs e) {
      if (fileHistory.SelectedIndex != 0) {
        MessageBoxResult res = MessageBox.Show(this, "This will permanently overwrite your current version. Are you sure?", "Confirm", MessageBoxButton.YesNo);
        if (res == MessageBoxResult.Yes) {
          File.WriteAllText(fullpath, fileContents.Text);
        } else {
          return;
        }
      }
      Close();
    }

    private void ClickClose(object sender, EventArgs e) {
      Close();
    }
  }
}
