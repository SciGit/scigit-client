using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using SciGit_Filter;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

namespace SciGit_Client
{
  /// <summary>
  /// Interaction logic for ProjectHistory.xaml
  /// </summary>
  public partial class ProjectHistory : Window
  {
    DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
    private Project project;
    private List<string> commitHashes;
    private List<string> updated, created, deleted;
    private string curFile, curAuthor;
    private Dictionary<string, Tuple<string, string>> fileData;
    private Dictionary<string, string> fullpath;

    public ProjectHistory(Project p, string hash = null) {
      InitializeComponent();

      Title += " for " + p.name;
      project = p;
      projectName.Text = "Project: " + project.name;

      string dir = ProjectMonitor.GetProjectDirectory(project);
      ProcessReturn ret = GitWrapper.Log(dir);
      string[] commits = SentenceFilter.SplitLines(ret.Output.Trim());
      if (ret.ReturnValue != 0 || commits.Length == 0) {
        throw new InvalidRepositoryException(project);
      }
      var actualCommits = new string[commits.Length - 1];
      Array.Copy(commits, actualCommits, commits.Length - 1);

      var timestamp = (int)(Directory.GetLastWriteTimeUtc(dir) - epoch).TotalSeconds;
      projectHistory.Items.Add(CreateListViewItem("", "Current Version", "", timestamp));
      commitHashes = new List<string>();

      int cIndex = 1;
      int? hashIndex = null;
      if (actualCommits.Count() > 0 && hash == "HEAD") {
        hashIndex = 1;
      }

      foreach (var commit in actualCommits) {
        string[] data = commit.Split(new[] { ' ' }, 4);
        commitHashes.Add(data[0]);
        if (data[0] == hash) {
          hashIndex = cIndex;
        }
        cIndex++;
        projectHistory.Items.Add(CreateListViewItem(data[0], data[3], data[1], int.Parse(data[2])));
      }

      if (hash != null && hashIndex == null) {
        MessageBox.Show("Could not find that revision. You may have to update the project.", "Version not found");
      }
      projectHistory.SelectedIndex = hashIndex ?? 0;
      projectHistory.Focus();
    }

    private ListBoxItem CreateListViewItem(string hash, string message, string author, int time) {
      var item = new ListBoxItem { HorizontalAlignment = HorizontalAlignment.Stretch };
      var sp = new StackPanel { Orientation = Orientation.Vertical, HorizontalAlignment = HorizontalAlignment.Stretch };
      var tb = new TextBlock { Text = message, FontSize = 12, FontWeight = FontWeights.Bold };
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
      item.Unselected += DeselectItem;
      item.Selected += (s, e) => ShowChanges(item, hash, author);
      return item;
    }

    private void DeselectItem(object sender, RoutedEventArgs e) {
      var item = sender as ListBoxItem;
      var sp = item.Content as StackPanel;
      // Remove everything but the first 2 items (i.e. remove the file listing)
      sp.Children.RemoveRange(2, sp.Children.Count - 2);
    }

    private void DisplayDiff(string name) {
      changesHeader.Text = "Changes to " + name;
      diffViewer.DisplayDiff(name, fullpath[name], curAuthor, fileData[name].Item1, fileData[name].Item2);
      curFile = name;
    }

    private void ShowChanges(ListBoxItem lvItem, string hash, string author) {
      var sp = lvItem.Content as StackPanel;

      var dir = ProjectMonitor.GetProjectDirectory(project);
      ProcessReturn ret;
      List<string> files;
      curAuthor = author;
      if (hash == "") {
        ret = GitWrapper.Status(dir, "-uall");
        if (ret.ReturnValue != 0) throw new Exception(ret.Output);
        files = new List<string>(ret.Stdout.Split(new[] {'\0'}, StringSplitOptions.RemoveEmptyEntries));
        for (int i = 0; i < files.Count; i++) {
          files[i] = files[i].Substring(3);
        }
      } else {
        ret = GitWrapper.ListChangedFiles(dir, hash);
        if (ret.ReturnValue != 0) throw new Exception(ret.Output);
        files = new List<string>(ret.Stdout.Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries));
      }
      
      // See what happened in each of these files.
      if (files.Count == 0) {
        diffViewer.DisplayEmpty();
        save.IsEnabled = false;
        changesHeader.Text = "Changes";
        sp.Children.Add(new TextBlock { Text = "No files changed.", FontSize = 10, Margin = new Thickness(5, 5, 0, 0) });
      } else {
        save.IsEnabled = true;
        sp.Children.Add(new TextBlock { Text = "Files changed:", FontSize = 10, Margin = new Thickness(5, 5, 0, 0) });
        var lb = new ListBox { HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(5, 2, 5, 0), BorderThickness = new Thickness(0) };
        sp.Children.Add(lb);

        updated = new List<string>();
        created = new List<string>();
        deleted = new List<string>();
        files.Sort();

        fileData = new Dictionary<string, Tuple<string, string>>();
        fullpath = new Dictionary<string, string>();
        foreach (var file in files) {
          string data1, data2;
          string winFile = Path.Combine(dir, file.Replace('/', Path.PathSeparator));
          fullpath[file] = winFile;
          if (hash == "") {
            ret = GitWrapper.ShowObject(dir, String.Format("{0}:\"{1}\"", hash, file));
            data1 = ret.ReturnValue == 0 ? ret.Stdout : null;
            data2 = File.Exists(winFile) ? File.ReadAllText(winFile, Encoding.Default) : null;
          } else {
            ret = GitWrapper.ShowObject(dir, String.Format("{0}:\"{1}\"", hash + "^", file));
            data1 = ret.ReturnValue == 0 ? ret.Stdout : null;
            ret = GitWrapper.ShowObject(dir, String.Format("{0}:\"{1}\"", hash, file));
            data2 = ret.ReturnValue == 0 ? ret.Stdout : null;
          }

          fileData[file] = new Tuple<string, string>(data1, data2);
          if (data1 == null) {
            created.Add(file);
          } else if (data2 == null) {
            deleted.Add(file);
          } else {
            updated.Add(file);
          }
        }

        foreach (var file in updated) {
          var item = new ListBoxItem {Content = file};
          item.Selected += (s, e) => DisplayDiff(file);
          lb.Items.Add(item);
        }

        foreach (var file in created) {
          var item = new ListBoxItem {Content = file + " (added)"};
          item.Selected += (s, e) => DisplayDiff(file);
          lb.Items.Add(item);
        }

        foreach (var file in deleted) {
          var item = new ListBoxItem {Content = file + " (deleted)"};
          item.Selected += (s, e) => DisplayDiff(file);
          lb.Items.Add(item);
        }

        lb.SelectedIndex = 0;
        lb.Focus();
      }
    }

    private void ClickRevert(object sender, EventArgs e) {
      if (projectHistory.SelectedIndex != 0) {
        MessageBoxResult res = MessageBox.Show(this,
          "You will lose any un-uploaded changes to your current files, as well as any new un-uploaded files. Are you sure?",
          "Confirm Revert", MessageBoxButton.YesNo);
        if (res == MessageBoxResult.Yes) {
          string hash = commitHashes[projectHistory.SelectedIndex - 1];
          string dir = ProjectMonitor.GetProjectDirectory(project);
          try {
            GitWrapper.AddAll(dir);
            ProcessReturn ret = GitWrapper.Reset(dir, "--hard " + hash);
            if (ret.ReturnValue != 0) throw new Exception("reset: " + ret.Output);
            ret = GitWrapper.Reset(dir, "ORIG_HEAD");
            if (ret.ReturnValue != 0) throw new Exception("reset: " + ret.Output);
          } catch (Exception ex) {
            ErrorForm.Show(ex);
          }
        } else {
          return;
        }
      }
      Close();
    }

    private void ClickSave(object sender, EventArgs e) {
      string text = fileData[curFile].Item2;

      if (text == null) {
        MessageBox.Show(this, "The file was deleted in this revision.", "Deleted");
        return;
      }

      var dialog = new System.Windows.Forms.SaveFileDialog();
      dialog.FileName = curFile;
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
