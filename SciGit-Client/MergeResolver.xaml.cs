using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using SciGit_Filter;

namespace SciGit_Client
{
  public class FileData
  {
    public string filename;
    public string myVersion;
    public string newVersion;
    public string original;
  }

  /// <summary>
  /// Interaction logic for MergeResolver.xaml
  /// </summary>
  public partial class MergeResolver : Window
  {
    int active;
    List<DiffViewer> diffViewers;
    Project project;
    List<FileData> unmergedFiles;

    public MergeResolver(Project p) {
      InitializeComponent();

      project = p;
      unmergedFiles = GetUnmergedFiles();

      /*FileData test = new FileData();
      test.filename = "test.docx";
      test.original = File.ReadAllText("C:\\Users\\Hanson\\paper_orig.docx", Encoding.Default);
      test.myVersion = File.ReadAllText("C:\\Users\\Hanson\\paper_orig.docx", Encoding.Default);
      test.newVersion = File.ReadAllText("C:\\Users\\Hanson\\paper.docx", Encoding.Default);
      test.original = "Sentence one.\nSentence two. Sentence three. Sentence four.\nSome crap after\na change\netc\nanother conflict\n";
      test.myVersion = "Sentence one.\nSentence two. Sentence threea.\nPlus a newline. Sentence four.\nSome crap after\na change\netc\nanother disagreement\n";
      test.newVersion = "Sentence one.\nSentence two. Sentence threeb. Sentence four.\nSome crap after\na small change\netc\nanother difference\n";
      unmergedFiles.Add(test);*/

      diffViewers = new List<DiffViewer>();
      for (int i = 0; i < unmergedFiles.Count; i++) {
        diffViewers.Add(CreateDiffViewer(unmergedFiles[i]));
      }

      active = 0;
      diffViewers[active].Visibility = Visibility.Visible;
      fileDropdown.SelectedIndex = 0;
      if (unmergedFiles.Count <= 1) {
        nextFile.IsEnabled = false;
      }
    }

    public bool Saved { get; private set; }

    void SetActiveDiffViewer(int dv) {
      if (active != dv) {
        diffViewers[active].Visibility = Visibility.Hidden;
        diffViewers[dv].Visibility = Visibility.Visible;
        active = dv;
      }
    }

    void ClickNextFile(object sender, RoutedEventArgs e) {
      SetActiveDiffViewer((active + 1) % diffViewers.Count);
      fileDropdown.SelectedIndex = active;
    }

    void ClickFinish(object sender, RoutedEventArgs e) {
      var unmerged = new List<string>();
      for (int i = 0; i < unmergedFiles.Count; i++) {
        if (!diffViewers[i].Finished()) {
          unmerged.Add(unmergedFiles[i].filename);
        }
      }

      if (unmerged.Count > 0) {
        string msg = String.Join("\n", unmerged.Select(str => "- " + str).ToArray());
        MessageBox.Show("The following files are still unmerged:\n" + msg, "Unmerged Changes");
      } else {
        var preview = new DiffPreview(unmergedFiles, diffViewers.Select(dv => dv.GetMergeResult()).ToList());
        preview.ShowDialog();
        if (preview.Saved) {
          Saved = true;
          List<string> mergeResults = preview.GetFinalText();
          string dir = ProjectMonitor.GetProjectDirectory(project);
          for (int i = 0; i < unmergedFiles.Count; i++) {
            string filename = dir + "/" + unmergedFiles[i].filename;
            if (mergeResults[i] == null) {
              // null means the file will be deleted
              if (File.Exists(filename)) {
                File.Delete(filename);
              }
            } else {
              File.WriteAllText(filename, mergeResults[i], Encoding.Default);
            }
          }
          Close();
        }
      }
    }

    List<FileData> GetUnmergedFiles() {
      string dir = ProjectMonitor.GetProjectDirectory(project);
      ProcessReturn ret = GitWrapper.ListUnmergedFiles(dir);

      var files = new Dictionary<string, FileData>();
      string[] lines = ret.Output.Split(new[] {'\0'}, StringSplitOptions.RemoveEmptyEntries);
      foreach (string line in lines) {
        var match = Regex.Match(line, "^[0-9]+ ([a-z0-9]+) ([0-9]+)\t(.*)$");
        if (match.Success) {
          string hash = match.Groups[1].Value;
          int stage = int.Parse(match.Groups[2].Value);
          string file = match.Groups[3].Value;
          if (!files.ContainsKey(file)) {
            files[file] = new FileData { filename = file };
          }

          ProcessReturn r = GitWrapper.ShowObject(dir, hash);
          string contents = r.Output;
          if (stage == 1) {
            files[file].original = contents;
          } else if (stage == 2) {
            files[file].newVersion = contents;
          } else {
            files[file].myVersion = contents;
          }
        }
      }

      return files.Values.ToList();
    }

    private DiffViewer CreateDiffViewer(FileData f) {
      DiffViewer dv;
      if (SentenceFilter.IsBinary(f.myVersion) || SentenceFilter.IsBinary(f.newVersion)) {
        dv = new BinaryDiffViewer(project, f.filename, f.original, f.myVersion, f.newVersion);
      } else {
        dv = new TextDiffViewer(project, f.filename, f.original, f.myVersion, f.newVersion);
      }
      dv.Visibility = Visibility.Hidden;
      Grid.SetRow(dv, 1);
      grid.Children.Add(dv);

      var cbItem = new ComboBoxItem {Content = f.filename};
      int cur = fileDropdown.Items.Count;
      cbItem.Selected += (e, o) => SetActiveDiffViewer(cur);
      fileDropdown.Items.Add(cbItem);

      return dv;
    }

    private void WindowClosing(object sender, CancelEventArgs e) {
      if (!Saved) {
        var res = MessageBox.Show(this, "This will cancel the merging process. Are you sure?", "Confirm Cancel", MessageBoxButton.YesNo);
        if (res == MessageBoxResult.No) {
          e.Cancel = true;
        }
      }
    }

    private void WindowClosed(object sender, EventArgs e) {
      foreach (var viewer in diffViewers) {
        viewer.Cleanup();
      }
    }
  }
}
