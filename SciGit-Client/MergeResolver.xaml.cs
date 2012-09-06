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
using System.Windows.Shapes;
using SciGit_Filter;
using System.IO;
using System.Text.RegularExpressions;

namespace SciGit_Client
{
  public class FileData
  {
    public string filename;
    public string original;
    public string myVersion;
    public string newVersion;
  }

  /// <summary>
  /// Interaction logic for MergeResolver.xaml
  /// </summary>
  public partial class MergeResolver : Window
  {
    Project project;
    List<FileData> unmergedFiles;
    List<DiffViewer> diffViewers;
    int active;
    public bool Saved { get; private set; }

    public MergeResolver(Project p) {
      InitializeComponent();

      project = p;
      unmergedFiles = GetUnmergedFiles();

      /*
      FileData test = new FileData();
      test.filename = "test1";
      test.original = "Sentence one.\nSentence two. Sentence three. Sentence four.\nSome crap after\na change\netc\nanother conflict\n";
      test.myVersion = "Sentence one.\nSentence two. Sentence threea.\nPlus a newline. Sentence four.\nSome crap after\na change\netc\nanother disagreement\n";
      test.newVersion = "Sentence one.\nSentence two. Sentence threeb. Sentence four.\nSome crap after\na small change\netc\nanother difference\n";
      unmergedFiles.Add(test);
       */

      if (unmergedFiles.Count == 0) {
        // TODO: show an error or something
        Close();
      }

      diffViewers = new List<DiffViewer>();
      for (int i = 0; i < unmergedFiles.Count; i++) {
        diffViewers.Add(CreateDiffViewer(unmergedFiles[i]));
      }

      active = 0;
      diffViewers[active].Visibility = Visibility.Visible;
      fileDropdown.SelectedIndex = 0;
    }

    void SetActiveDiffViewer(int dv) {
      if (active != dv) {
        diffViewers[active].Visibility = Visibility.Hidden;
        diffViewers[dv].Visibility = Visibility.Visible;
        active = dv;
      }
    }

    void SelectNextFile() {
      SetActiveDiffViewer((active + 1) % diffViewers.Count);
      fileDropdown.SelectedIndex = active;
    }

    void Finish() {
      List<string> unmerged = new List<string>();
      for (int i = 0; i < unmergedFiles.Count; i++) {
        if (!diffViewers[i].Finished()) {
          unmerged.Add(unmergedFiles[i].filename);
        }
      }

      if (unmerged.Count > 0) {
        string msg = String.Join("\n", unmerged.Select(str => "- " + str));
        MessageBox.Show("The following files are still unmerged:\n" + msg, "Unmerged Changes");
      } else {
        DiffPreview preview = new DiffPreview(unmergedFiles, diffViewers.Select(dv => dv.GetMergeResult()).ToList());
        preview.ShowDialog();
        if (preview.Saved) {
          Saved = true;
          List<string> mergeResults = preview.GetFinalText();
          string dir = SGProjectManager.GetProjectDirectory(project);
          for (int i = 0; i < unmergedFiles.Count; i++) {
            File.WriteAllBytes(dir + "/" + unmergedFiles[i].filename,
              Encoding.UTF8.GetBytes(mergeResults[i]));
          }
          Close();
        }
      }
    }

    List<FileData> GetUnmergedFiles() {
      string dir = SGProjectManager.GetProjectDirectory(project);
      GitReturn ret = GitWrapper.ListUnmergedFiles(dir);

      Dictionary<String, FileData> files = new Dictionary<string, FileData>();
      string[] lines = SentenceFilter.SplitLines(ret.Output);
      foreach (string line in lines) {
        var match = Regex.Match(line, "^[0-9]+ ([a-z0-9]+) ([0-9]+)\t(.*)$");
        if (match.Success) {
          string hash = match.Groups[1].Value;
          int stage = int.Parse(match.Groups[2].Value);
          // TODO: take care of weird edge cases with quoted filename
          string file = match.Groups[3].Value;
          if (!files.ContainsKey(file)) {
            files[file] = new FileData { filename = file };
          }

          GitReturn r = GitWrapper.ShowObject(dir, hash);
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
      DiffViewer dv = new DiffViewer(f.original, f.myVersion, f.newVersion);
      dv.NextFileCallback = SelectNextFile;
      dv.FinishCallback = Finish;
      Grid.SetRow(dv, 1);
      dv.Visibility = Visibility.Hidden;
      grid.Children.Add(dv);

      var cbItem = new ComboBoxItem();
      cbItem.Content = f.filename;
      int cur = fileDropdown.Items.Count;
      cbItem.Selected += (e, o) => SetActiveDiffViewer(cur);
      fileDropdown.Items.Add(cbItem);

      return dv;
    }

    private void Window_Closed(object sender, EventArgs e) {
      // TODO: display confirmation message
    }
  }
}
