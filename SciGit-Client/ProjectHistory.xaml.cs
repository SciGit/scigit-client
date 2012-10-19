using System;
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

    public ProjectHistory(Project p) {
      InitializeComponent();

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
      projectHistory.Items.Add(CreateListViewItem("Current Version", "", timestamp));
      commitHashes = new List<string>();
      foreach (var commit in actualCommits) {
        string[] data = commit.Split(new[] { ' ' }, 4);
        commitHashes.Add(data[0]);
        projectHistory.Items.Add(CreateListViewItem(data[3], data[1], int.Parse(data[2])));
      }

      projectHistory.SelectedIndex = 0;
    }

    private ListViewItem CreateListViewItem(string message, string author, int time) {
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
      return item;
    }

    private void ClickRevert(object sender, EventArgs e) {
      if (projectHistory.SelectedIndex != 0) {
        MessageBoxResult res = MessageBox.Show(this,
          "You will lose any un-uploaded changes to your current files, as well as any new un-uploaded files. Are you sure?",
          "Confirm", MessageBoxButton.YesNo);
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
