using System;
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
    private List<string> changedFiles;
    private string curAuthor;
    private Dictionary<string, Tuple<string, string>> fileData;
    private Dictionary<string, string> fullpath;

    public ProjectHistory(Project p) {
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
      foreach (var commit in actualCommits) {
        string[] data = commit.Split(new[] { ' ' }, 4);
        commitHashes.Add(data[0]);
        projectHistory.Items.Add(CreateListViewItem(data[0], data[3], data[1], int.Parse(data[2])));
      }

      projectHistory.SelectedIndex = 0;
      fileListing.SelectionHandlers.Add(SelectFile);
    }

    private ListViewItem CreateListViewItem(string hash, string message, string author, int time) {
      var item = new ListViewItem();
      var sp = new StackPanel { Orientation = Orientation.Vertical };
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
      item.Selected += (s, e) => ShowChanges(hash, author);
      return item;
    }

    private void SelectFile(int index) {
      DisplayDiff(changedFiles[index]);
    }

    private void DisplayDiff(string name) {
      diffViewer.DisplayDiff(name, fullpath[name], curAuthor, fileData[name].Item1, fileData[name].Item2);
    }

    private void ShowChanges(string hash, string author) {
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
      } else {
        updated = new List<string>();
        created = new List<string>();
        deleted = new List<string>();
        files.Sort();

        fileListing.Clear();
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
            fileListing.AddFile(1, file);
          } else if (data2 == null) {
            deleted.Add(file);
            fileListing.AddFile(2, file);
          } else {
            updated.Add(file);
            fileListing.AddFile(0, file);
          }
        }

        changedFiles = new List<string>();
        changedFiles.AddRange(updated);
        changedFiles.AddRange(created);
        changedFiles.AddRange(deleted);
        fileListing.Select(0);
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

    private void ClickClose(object sender, EventArgs e) {
      Close();
    }
  }
}
