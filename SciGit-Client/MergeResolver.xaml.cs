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
    private int active;
    private Project project;
    private List<MergeViewer> diffViewers;
    private List<FileData> unmergedFiles;
    private Dictionary<string, MergeViewer> diffViewerMap;
    private List<string> updated, created, deleted, combined;

    public MergeResolver(Project p) {
      InitializeComponent();

      Title += " for " + p.name;
      project = p;

      if (p.id == 0) {
        string orig_big = "", my_big = "", new_big = "";
        for (int i = 0; i < 25; i++) {
          orig_big += "a\na\n";
          my_big += "b\na\n";
          new_big += "c\na\n";
        }

        unmergedFiles = new List<FileData> {
          new FileData {
            filename = "file.txt",
            original = "Sentence one.\nSentence two. Sentence three. Sentence four.\nSome crap after\na change\netc\nanother conflict\n",
            myVersion = "Sentence one.\nSentence two. Sentence threea.\nPlus a newline. Sentence four.\nSome crap after\na change\netc\nanother disagreement\n",
            newVersion = "Sentence one.\nSentence two. Sentence threeb. Sentence four.\nSome crap after\na small change\netc\nanother difference\n"
          },
          new FileData {
            filename = "bin_file.txt",
            original = "\007Sentence one.\nSentence two. Sentence three. Sentence four.\nSome crap after\na change\netc\nanother conflict\n",
            myVersion = null,
            newVersion = "\007Sentence one.\nSentence two. Sentence threeb. Sentence four.\nSome crap after\na small change\netc\nanother difference\n"
          },
          new FileData {
            filename = "file.docx",
            original = "\007Sentence one.\nSentence two. Sentence three. Sentence four.\nSome crap after\na change\netc\nanother conflict\n",
            myVersion = File.ReadAllText("C:\\temp\\a.docx", Encoding.Default),
            newVersion = File.ReadAllText("C:\\temp\\b.docx", Encoding.Default)
          },
          new FileData {
            filename = "big_file.txt",
            original = orig_big,
            myVersion = my_big,
            newVersion = new_big
          },
          new FileData {
            filename = "added_file.txt",
            original = null,
            myVersion = "Sentence one.\nSentence two. Sentence threea.\nPlus a newline. Sentence four.\nSome crap after\na change\netc\nanother disagreement\n",
            newVersion = "Sentence one.\nSentence two. Sentence threeb. Sentence four.\nSome crap after\na small change\netc\nanother difference\n"
          },
          new FileData {
            filename = "my_deletion.txt",
            original = "Sentence one.\nSentence two. Sentence three. Sentence four.\nSome crap after\na change\netc\nanother conflict\n",
            myVersion = null,
            newVersion = "Sentence one.\nSentence two. Sentence threeb. Sentence four.\nSome crap after\na small change\netc\nanother difference\n"
          },
          new FileData {
            filename = "their_deletion.txt",
            original = "Sentence one.\nSentence two. Sentence three. Sentence four.\nSome crap after\na change\netc\nanother conflict\n",
            myVersion = "Sentence one.\nSentence two. Sentence threea.\nPlus a newline. Sentence four.\nSome crap after\na change\netc\nanother disagreement\n",
            newVersion = null
          }
        };
      } else {
        unmergedFiles = GetUnmergedFiles();
      }

      diffViewerMap = new Dictionary<string, MergeViewer>();
      updated = new List<string>();
      created = new List<string>();
      deleted = new List<string>();

      for (int i = 0; i < unmergedFiles.Count; i++) {
        CreateDiffViewer(unmergedFiles[i]);
      }

      updated.Sort();
      created.Sort();
      deleted.Sort();
      combined = new List<string>(updated);
      combined.AddRange(created);
      combined.AddRange(deleted);
      diffViewers = new List<MergeViewer>(combined.Select(name => diffViewerMap[name]));

      var unmergedMap = unmergedFiles.ToDictionary(x => x.filename);
      unmergedFiles = new List<FileData>(combined.Select(name => unmergedMap[name]));

      foreach (var name in updated) {
        fileListing.AddFile(0, name);
      }
      foreach (var name in created) {
        fileListing.AddFile(1, name);
      }
      foreach (var name in deleted) {
        fileListing.AddFile(2, name);
      }
      fileListing.SelectionHandlers.Add(SelectFilename);

      active = 0;
      diffViewers[active].Visibility = Visibility.Visible;
      SetActiveFile(active);
      if (unmergedFiles.Count <= 1) {
        nextFile.IsEnabled = false;
      }
    }

    public bool Saved { get; private set; }

    void SelectFilename(int index) {
      SetActiveDiffViewer(index);
    }
    
    void SetActiveDiffViewer(int dv) {
      if (active != dv) {
        diffViewers[active].Visibility = Visibility.Hidden;
        diffViewers[dv].Visibility = Visibility.Visible;
        active = dv;
      }
    }

    void SetActiveFile(int index) {
      SetActiveDiffViewer(index);
      fileListing.Select(index);
    }

    void ClickNextFile(object sender, RoutedEventArgs e) {
      SetActiveFile((active + 1) % diffViewers.Count);
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
        var preview = new MergePreview(unmergedFiles, diffViewers.Select(dv => dv.GetMergeResult()).ToList());
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

    private MergeViewer CreateDiffViewer(FileData f) {
      MergeViewer dv;
      if (SentenceFilter.IsBinary(f.myVersion) || SentenceFilter.IsBinary(f.newVersion)) {
        dv = new BinaryMergeViewer(project, f.filename, f.original, f.myVersion, f.newVersion);
      } else {
        dv = new TextMergeViewer(project, f.filename, f.original, f.myVersion, f.newVersion);
      }
      dv.Visibility = Visibility.Hidden;
      Grid.SetRow(dv, 0);
      Grid.SetRowSpan(dv, 2);
      Grid.SetColumn(dv, 1);
      grid.Children.Add(dv);
      diffViewerMap[f.filename] = dv;

      if (f.original == null && f.newVersion != null) {
        created.Add(f.filename);
      } else if (f.newVersion != null) {
        updated.Add(f.filename);
      } else {
        deleted.Add(f.filename);
      }

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
