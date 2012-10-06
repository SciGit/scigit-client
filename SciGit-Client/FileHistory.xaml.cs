using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using SciGit_Filter;
using System.Collections.Generic;
using System.Text;

namespace SciGit_Client
{
  /// <summary>
  /// Interaction logic for FileHistory.xaml
  /// </summary>
  public partial class FileHistory : Window
  {
    DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
    private Project project;
    private string fullpath, gitFilename, filename;
    private Dictionary<string, string> fileData;
    private List<string> hashes;

    public FileHistory(Project p, string filename) {
      InitializeComponent();

      project = p;
      this.filename = filename;
      filenameText.Text = "File: " + Path.Combine(p.Name, filename);
      gitFilename = filename.Replace(Path.DirectorySeparatorChar, '/');

      string dir = ProjectMonitor.GetProjectDirectory(p);
      ProcessReturn ret = GitWrapper.Log(dir, String.Format("-- \"{0}\"", filename));
      string[] commits = SentenceFilter.SplitLines(ret.Output.Trim());
      if (ret.ReturnValue != 0 || commits.Length == 0) {
        throw new InvalidRepositoryException(p);
      }

      fileData = new Dictionary<string, string>();
      hashes = new List<string>();
      fullpath = Path.Combine(dir, filename);
      fileData[""] = File.ReadAllText(fullpath, Encoding.Default);
      hashes.Add("");
      var timestamp = (int)(File.GetLastWriteTimeUtc(fullpath) - epoch).TotalSeconds;
      fileHistory.Items.Add(CreateListViewItem("", "Current Version", "", timestamp));
      foreach (var commit in commits) {
        string[] data = commit.Split(new[] { ' ' }, 4);
        if (data.Length == 4) {
          hashes.Add(data[0]);
          fileHistory.Items.Add(CreateListViewItem(data[0], data[3], data[1], int.Parse(data[2])));
        }
      }

      fileHistory.SelectedIndex = 0;
    }

    private void DisplayText(string text) {
      revert.IsEnabled = save.IsEnabled = true;
      if (text == null) {
        fileContents.IsEnabled = false;
        revert.IsEnabled = save.IsEnabled = false;
        fileContents.Text = "The file was deleted in this version.";
      } else if (SentenceFilter.IsBinary(text)) {
        fileContents.IsEnabled = false;
        fileContents.Text = "The contents of the file are binary. You can save and view it in its appropriate program.";
      } else {
        fileContents.IsEnabled = true;
        fileContents.Text = text;
      }
    }

    private string LoadFile(string hash) {
      if (fileData.ContainsKey(hash)) {
        return fileData[hash];
      } else {
        string dir = ProjectMonitor.GetProjectDirectory(project);
        ProcessReturn ret = GitWrapper.ShowObject(dir, String.Format("{0}:\"{1}\"", hash, gitFilename));
        if (ret.ReturnValue == 0) {
          return fileData[hash] = ret.Stdout;
        }
        return fileData[hash] = null;
      }
    }

    private ListViewItem CreateListViewItem(string hash, string message, string author, int time) {
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
      item.Selected += (s, e) => DisplayText(LoadFile(hash));
      return item;
    }

    private void ClickRevert(object sender, EventArgs e) {
      string hash = hashes[fileHistory.SelectedIndex];
      if (fileHistory.SelectedIndex != 0) {
        MessageBoxResult res = MessageBox.Show(this, "This will permanently overwrite your current version. Are you sure?", "Confirm", MessageBoxButton.YesNo);
        if (res == MessageBoxResult.Yes) {
          File.WriteAllText(fullpath, LoadFile(hash), Encoding.Default);
        } else {
          return;
        }
      }
      Close();
    }

    private void ClickSave(object sender, EventArgs e) {
      string hash = hashes[fileHistory.SelectedIndex];
      string text = LoadFile(hash);
      var dialog = new System.Windows.Forms.SaveFileDialog();
      dialog.FileName = this.filename;
      dialog.Filter = "All files (*.*)|*.*";
      dialog.FilterIndex = 0;
      dialog.InitialDirectory = ProjectMonitor.GetProjectDirectory(project);
      if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
        var file = dialog.OpenFile();
        byte[] bytes = Encoding.Default.GetBytes(text);
        file.Write(bytes, 0, bytes.Length);
        file.Close();
      }
    }

    private void ClickClose(object sender, EventArgs e) {
      Close();
    }
  }
}
