using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using DiffPlex;
using DiffPlex.Model;
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
      filenameText.Text = "File: " + Util.PathCombine(p.name, filename);
      gitFilename = filename.Replace(Path.DirectorySeparatorChar, '/');

      string dir = ProjectMonitor.GetProjectDirectory(p);
      ProcessReturn ret = GitWrapper.Log(dir, String.Format("-- \"{0}\"", filename));
      string[] commits = SentenceFilter.SplitLines(ret.Output.Trim());
      if (ret.ReturnValue != 0 || commits.Length == 0) {
        throw new InvalidRepositoryException(p);
      }

      fileData = new Dictionary<string, string>();
      hashes = new List<string>();
      fullpath = Util.PathCombine(dir, filename);
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

    private void DisplayDiff(string author, string old, string updated) {
      revert.IsEnabled = save.IsEnabled = true;
      grid.RowDefinitions.Clear();
      var rd = new RowDefinition {Height = new GridLength(1, GridUnitType.Star)};
      grid.RowDefinitions.Add(rd);

      // Remove the line number blocks and such. actionsThem is the last item in the XAML
      int x = grid.Children.IndexOf(message);
      grid.Children.RemoveRange(x + 1, grid.Children.Count - x);

      message.Visibility = Visibility.Collapsed;
      if (filename.EndsWith(".docx") || filename.EndsWith(".doc")) {
        // Word document. So let the user open the doc in Word
        message.Visibility = Visibility.Visible;
        message.Text = "This is a Word document. Please save it to view its contents.";
        if (old != null) {
          message.Inlines.Add(" You can also ");
          var fakeUri = new Uri("http://asdf.com");
          var link = new Hyperlink(new Run("view the changes")) {NavigateUri = fakeUri};
          link.RequestNavigate += (s, e) => CompareInWord(old, updated, filename, Path.GetDirectoryName(fullpath), author);
          message.Inlines.Add(link);
          message.Inlines.Add(" in Word.");
        }
      } else if (SentenceFilter.IsBinary(old) || SentenceFilter.IsBinary(updated)) {
        message.Visibility = Visibility.Visible;
        message.Text = "This file is in binary. Please save it to view its contents.";
      } else {
        // Compute the diff, break it into blocks
        if (old == null) old = "";
        if (updated == null) updated = "";
        var d = new Differ();
        var dr = d.CreateLineDiffs(old, updated, false);
        int curBlock = 0, numBlocks = dr.DiffBlocks.Count;

        List<LineBlock> lineBlocks = new List<LineBlock>();
        for (int i = 0; i <= dr.PiecesOld.Length; i++) {
          while (curBlock < numBlocks && dr.DiffBlocks[curBlock].DeleteStartA < i) {
            curBlock++;
          }

          if (curBlock < numBlocks && dr.DiffBlocks[curBlock].DeleteStartA == i) {
            var db = dr.DiffBlocks[curBlock];
            if (db.DeleteCountA > 0) {
              lineBlocks.Add(new LineBlock(Util.ArraySlice(dr.PiecesOld, db.DeleteStartA, db.DeleteCountA), BlockType.ChangeDelete));
            }
            if (db.InsertCountB > 0) {
              lineBlocks.Add(new LineBlock(Util.ArraySlice(dr.PiecesNew, db.InsertStartB, db.InsertCountB), BlockType.ChangeAdd));
            }
            i += db.DeleteCountA;
            curBlock++;
          }

          if (i < dr.PiecesOld.Length) {
            lineBlocks.Add(new LineBlock(Util.ArraySlice(dr.PiecesOld, i, 1)));
          }
        }

        // Draw the actual blocks.
        DrawLineBlocks(lineBlocks);
      }
    }

    private void CompareInWord(string old, string updated, string name, string fullpath, string author) {
      string guid = Guid.NewGuid().ToString();
      string temp1 = Path.GetTempPath() + "scigit_compare1" + guid + ".docx";
      string temp2 = Path.GetTempPath() + "scigit_compare2" + guid + ".docx";
      File.WriteAllText(temp1, old, Encoding.Default);
      File.WriteAllText(temp2, updated, Encoding.Default);
      Util.CompareInWord(temp1, temp2, name, fullpath, author);
    }

    private Style GetStyle(string name) {
      return Application.Current.Resources[name] as Style;
    }

    private void DrawLineBlocks(List<LineBlock> lineBlocks) {
      int lines = 0;
      for (int i = 0; i < lineBlocks.Count; i++) {
        lines += lineBlocks[i].lines.Count;
      }
      for (int i = 0; i < lines; i++) {
        grid.RowDefinitions.Insert(0, new RowDefinition { Height = GridLength.Auto });
      }

      // Create grid cells for text blocks.
      var lineTexts = new List<RichTextBox>();
      var lineNums = new List<TextBlock>();
      var lineTextBackgrounds = new List<Border>();
      var lineNumBackgrounds = new List<Border>();
      for (int i = 0; i < lines; i++) {
        var lineNum = new TextBlock { Style = GetStyle("lineNumber") };
        Panel.SetZIndex(lineNum, 5);
        Grid.SetRow(lineNum, i);
        Grid.SetColumn(lineNum, 0);
        grid.Children.Add(lineNum);
        lineNums.Add(lineNum);

        var text = new RichTextBox {
          Style = GetStyle("lineText"),
          HorizontalAlignment = HorizontalAlignment.Stretch
        };
        Panel.SetZIndex(text, 5);
        Grid.SetRow(text, i);
        Grid.SetColumn(text, 1);
        var doc = new FlowDocument();
        var p = new Paragraph();
        doc.Blocks.Add(p);
        text.Document = doc;
        grid.Children.Add(text);
        lineTexts.Add(text);
      }

      // Draw the actual text blocks.
      int prevLine = 0;
      int lineCounter = 1;
      for (int g = 0; g < lineBlocks.Count; g++) {
        LineBlock lblock = lineBlocks[g];
        for (int i = 0; i < lblock.lines.Count; i++) {
          Line line = lblock.lines[i];
          var p = (Paragraph)lineTexts[prevLine + i].Document.Blocks.First();
          foreach (Block b in line.blocks) {
            // More processing can be added for per-sentence / per-word diffs.
            p.Inlines.Add(new Run(b.text));
          }
          if (lblock.type != BlockType.ChangeDelete) {
            lineNums[prevLine + i].Text = lineCounter++.ToString();
          }
        }

        if (lblock.type != BlockType.Normal) {
          var numBorder = new Border();
          numBorder.Style = GetStyle("numBackground" + lblock.type);
          Panel.SetZIndex(numBorder, 3);
          Grid.SetRow(numBorder, prevLine);
          Grid.SetColumn(numBorder, 0);
          Grid.SetRowSpan(numBorder, lblock.lines.Count);
          grid.Children.Add(numBorder);

          var textBorder = new Border();
          textBorder.Style = GetStyle("textBackground" + lblock.type);
          Panel.SetZIndex(textBorder, 2);
          Grid.SetRow(textBorder, prevLine);
          Grid.SetColumn(textBorder, 1);
          Grid.SetRowSpan(textBorder, lblock.lines.Count);
          grid.Children.Add(textBorder);

          for (int line = 0; line < lblock.lines.Count; line++) {
            TextBlock num = lineNums[prevLine + line];
            RichTextBox text = lineTexts[prevLine + line];
            num.Style = GetStyle("lineNum" + lblock.type);
            text.Style = GetStyle("lineText" + lblock.type);
          }
        }

        prevLine += lblock.lines.Count;
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
      string previous = hash == "" ? "HEAD" : hash + "^";
      item.Selected += (s, e) => DisplayDiff(author, LoadFile(hash + "^"), LoadFile(hash));
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
