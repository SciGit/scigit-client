using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using SciGit_Filter;

namespace SciGit_Client
{
  /// <summary>
  /// Interaction logic for FileHistory.xaml
  /// </summary>
  public partial class FileHistory : Window
  {
    DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
    string fullpath;

    public FileHistory(Project p, string filename) {
      InitializeComponent();

      this.filename.Text = "File: " + Path.Combine(p.Name, filename);
      string gitFilename = filename.Replace(Path.DirectorySeparatorChar, '/');

      string dir = ProjectMonitor.GetProjectDirectory(p);
      ProcessReturn ret = GitWrapper.Log(dir, String.Format("--pretty=\"%H %ae %at %s\" -- \"{0}\"", filename));
      string[] commits = SentenceFilter.SplitLines(ret.Output.Trim());

      fullpath = Path.Combine(dir, filename);
      string curText = File.ReadAllText(fullpath);
      var timestamp = (int)(File.GetLastWriteTimeUtc(fullpath) - epoch).TotalSeconds;
      fileHistory.Items.Add(CreateListViewItem("Current Version", "", timestamp, curText));
      foreach (var commit in commits) {
        string[] data = commit.Split(new[] { ' ' }, 4);
        ret = GitWrapper.ShowObject(dir, String.Format("{0}:\"{1}\"", data[0], gitFilename));
        if (ret.ReturnValue == 0) {
          fileHistory.Items.Add(CreateListViewItem(data[3], data[1], int.Parse(data[2]), ret.Output));
        }
      }

      fileHistory.SelectedIndex = 0;
    }

    private ListViewItem CreateListViewItem(string message, string author, int time, string text) {
      var item = new ListViewItem();
      var sp = new StackPanel {Orientation = Orientation.Vertical};
      var tb = new TextBlock {Text = message, FontSize = 12, FontWeight = FontWeights.Bold};
      sp.Children.Add(tb);
      DateTime date = epoch.AddSeconds(time);
      tb = new TextBlock {
        Text = (author == "" ? "" : "by " + author + " ") + "on " +
          date.ToLocalTime().ToString("MMM d, yyyy h:mmtt"),
        FontSize = 10
      };
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
