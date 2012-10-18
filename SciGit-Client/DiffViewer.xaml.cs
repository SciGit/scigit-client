using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using DiffPlex;
using DiffPlex.Model;
using SciGit_Filter;
using System.Text;

namespace SciGit_Client
{
  /// <summary>
  /// Interaction logic for DiffViewer.xaml
  /// </summary>
  public partial class DiffViewer : UserControl
  {
    private List<int> conflictBlocks;
    private int[] conflictChoice, conflictHover;
    private List<LineBlock> conflictOrigBlocks;
    private List<LineBlock>[] content;
    private int curConflict;
    private int[] lineCount;
    private List<TextBlock>[] lineNums;
    private List<Border>[] lineNumBackgrounds, lineTextBackgrounds;
    private List<RichTextBox>[] lineTexts;
    private LineBlock[][] origBlocks;
    private string gitFilename, fullpath, newFullpath;
    private Project project;
    private bool binary = false;
    private int binarySide = -1;
    private int deletedSide = -1;
    
    public DiffViewer(Project p, string filename, string original, string myVersion, string newVersion) {
      InitializeComponent();

      gitFilename = filename;
      project = p;

      if (original == null) {
        original = ""; // We don't really have to display anything differently.
        // Also, add-add conflicts don't need any special treatment.
      }

      if (myVersion == null) {
        titleMe.Text = "Your Version (file was deleted)";
        deletedSide = 0;
      } else if (newVersion == null) {
        titleNew.Text = "Updated Version (file was deleted)";
        deletedSide = 1;
      }

      if (SentenceFilter.IsBinary(myVersion) || SentenceFilter.IsBinary(newVersion)) {
        binary = true;
        InitializeBinaryEditor(myVersion, newVersion);
        return;
      }

      ProcessDiff(original, myVersion, newVersion);
      InitializeEditor();

      // Select the first conflict, update the conflict indicators.
      conflictHover = new int[conflictBlocks.Count];
      conflictChoice = new int[conflictBlocks.Count];
      for (int i = 0; i < conflictChoice.Length; i++) {
        conflictChoice[i] = -1;
      }
      SelectConflict(0);
      if (conflictBlocks.Count > 1) {
        nextConflict.IsEnabled = prevConflict.IsEnabled = true;
      }
    }

    public void Cleanup() {
      if (binary) {
        if (File.Exists(newFullpath)) {
          File.Delete(newFullpath);
        }
      }
    }

    public Action NextFileCallback { get; set; }
    public Action FinishCallback { get; set; }

    public bool Finished() {
      if (binary) return binarySide != -1;
      return !conflictChoice.Contains(-1);
    }

    public string GetMergeResult() {
      if (!Finished()) return null;

      if (binary) {
        if (binarySide == deletedSide) return null;
        return File.ReadAllText(binarySide == 0 ? fullpath : newFullpath, Encoding.Default);
      }

      int totalLines = 0;
      string result = "";
      for (int i = 0; i < content[0].Count; i++) {
        int side = 0;
        int conflictIndex = conflictBlocks.BinarySearch(i);
        if (conflictIndex >= 0) {
          side = conflictChoice[conflictIndex];
        } else if (content[1][i].type == BlockType.ChangeAdd) {
          side = 1;
        }

        result += content[side][i].ToString();
        totalLines += content[side][i].lines.Count;
        if (content[side][i].lines.Count > 0 && i != content[0].Count - 1) {
          result += "\n";
        }
      }
      return totalLines > 0 ? result : null;
    }

    private Style GetStyle(string name) {
      return Application.Current.Resources[name] as Style;
    }

    private void UpdateConflictBlock(int index) {
      int block = conflictBlocks[index];
      Style active = GetStyle("textBackgroundConflictActive");
      Style hover = GetStyle("textBackgroundConflictHover");
      Style refused = GetStyle("textBackgroundConflictRefused");
      for (int i = 0; i < 2; i++) {
        Style normal = GetStyle("textBackground" + content[i][block].type);
        Border textBackground = lineTextBackgrounds[i][block];
        double opacity = 1;
        bool border = false;

        // background color
        if (conflictHover[index] == i && conflictChoice[index] != i) {
          textBackground.Style = hover;
        } else {
          if (conflictChoice[index] == i) {
            border = true;
          } else if (conflictChoice[index] != -1) {
            opacity = 0.3;
          }
          textBackground.Style = normal;
        }

        textBackground.ClearValue(Border.BorderThicknessProperty);
        textBackground.ClearValue(Border.BorderBrushProperty);
        if (border) {
          textBackground.BorderThickness = new Thickness(3);
          textBackground.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 255, 0));
        }

        int startLine = lineCount.Take(block).Sum();
        for (int line = 0; line < lineCount[block]; line++) {
          lineTexts[i][line + startLine].Opacity = opacity;
        }

        // drop shadow
        if (index == curConflict) {
          textBackground.Effect = (active.Setters[0] as Setter).Value as Effect;
        } else {
          textBackground.Effect = null;
        }
      }
    }

    private void SelectConflict(int index) {
      int newBlock = conflictBlocks[index];
      int line = lineCount.Take(newBlock).Sum();
      Point p = lineNums[0][line].TransformToAncestor(grid).Transform(new Point(0, 0));
      scrollViewer.ScrollToVerticalOffset(p.Y);

      conflictNumber.Text = "Conflict " + (index + 1) + "/" + conflictBlocks.Count;
      int lastConflict = curConflict;
      curConflict = index;
      UpdateConflictBlock(lastConflict);
      UpdateConflictBlock(curConflict);

      acceptMe.IsChecked = conflictChoice[index] == 0;
      acceptThem.IsChecked = conflictChoice[index] == 1;
      revert.IsEnabled = conflictChoice[index] == -1 ? false :
        content[conflictChoice[index]][newBlock].type == BlockType.Edited;
    }

    private void SelectPreviousConflict(object sender, RoutedEventArgs e) {
      int numConflicts = conflictBlocks.Count;
      SelectConflict((curConflict + numConflicts - 1) % numConflicts);
    }

    private void SelectNextConflict(object sender, RoutedEventArgs e) {
      int numConflicts = conflictBlocks.Count;
      SelectConflict((curConflict + 1) % numConflicts);
    }

    private void HoverConflict(int index, int side, bool on) {
      conflictHover[index] = on ? side : -1;
      UpdateConflictBlock(index);
    }

    private void ChooseConflict(int index, int side) {
      conflictChoice[index] = side;
      acceptMe.IsChecked = side == 0;
      acceptThem.IsChecked = side == 1;
      UpdateConflictBlock(index);
    }

    private void EditConflict(int index) {
      int block = conflictBlocks[index];
      LineBlock chosenBlock = conflictChoice[index] == -1 ? null : content[conflictChoice[index]][block];
      var de = new DiffEditor(origBlocks[0][block], origBlocks[1][block], conflictOrigBlocks[index], chosenBlock);
      de.ShowDialog();
      if (de.newBlock != null) {
        ProcessBlockDiff(conflictOrigBlocks[index], de.newBlock, false);
        if (conflictChoice[index] == -1) conflictChoice[index] = 0;
        content[conflictChoice[index]][block] = de.newBlock;
        revert.IsEnabled = true;
        ReloadEditor();
      }
    }

    private void ClickAccept(int index, int side) {
      if (conflictChoice[index] == side) {
        ChooseConflict(index, -1);
      } else {
        ChooseConflict(index, side);
        if (index + 1 != conflictBlocks.Count) {
          SelectConflict(index + 1);
        }
      }
    }

    private void Accept(int side) {
      if (binary) {
        binarySide = side;
        acceptMe.IsChecked = side == 0;
        acceptThem.IsChecked = side == 1;
      } else {
        ClickAccept(curConflict, side);
      }
    }

    private void ClickAcceptMe(object sender, RoutedEventArgs e) {
      Accept(0);
    }

    private void ClickAcceptThem(object sender, RoutedEventArgs e) {
      Accept(1);
    }

    private void ClickEdit(object sender, RoutedEventArgs e) {
      EditConflict(curConflict);
    }

    private void ClickRevert(object sender, RoutedEventArgs e) {
      int block = conflictBlocks[curConflict];
      int side = conflictChoice[curConflict];
      content[side][block] = origBlocks[side][block];
      revert.IsEnabled = false;
      ReloadEditor();
    }

    private void ClickNextFile(object sender, RoutedEventArgs e) {
      if (NextFileCallback != null) {
        NextFileCallback();
      }
    }

    private void ClickFinish(object sender, RoutedEventArgs e) {
      if (FinishCallback != null) {
        FinishCallback();
      }
    }

    private void ClearEditor() {
      grid.RowDefinitions.Clear();
      var rd = new RowDefinition {Height = new GridLength(1, GridUnitType.Star)};
      grid.RowDefinitions.Add(rd);

      var rects = new List<UIElement>();
      foreach (UIElement child in grid.Children) {
        if (child.GetType() == typeof(Rectangle)) {
          rects.Add(child);
        }
      }
      grid.Children.Clear();
      foreach (var elem in rects) {
        grid.Children.Add(elem);
      }
    }

    private void ReloadEditor() {
      ClearEditor();
      InitializeEditor();
      for (int i = 0; i < conflictBlocks.Count; i++) {
        UpdateConflictBlock(i);
      }
      SelectConflict(curConflict);
    }

    private void InitializeEditor() {
      // Obtain line counts.
      conflictBlocks = new List<int>();
      lineCount = new int[content[0].Count];
      for (int i = 0; i < content[0].Count; i++) {
        lineCount[i] = Math.Max(content[0][i].lines.Count, content[1][i].lines.Count);
        if (content[0][i].type != BlockType.Normal) {
          conflictBlocks.Add(i);
        }
      }

      int lines = lineCount.Sum();
      for (int i = 0; i < lines; i++) {
        var rd = new RowDefinition {Height = GridLength.Auto};
        grid.RowDefinitions.Insert(0, rd);
      }

      // Create grid cells for text blocks.
      lineTexts = new List<RichTextBox>[2];
      lineNums = new List<TextBlock>[2];
      lineTextBackgrounds = new List<Border>[2];
      lineNumBackgrounds = new List<Border>[2];
      for (int i = 0; i < 2; i++) {
        lineTexts[i] = new List<RichTextBox>();
        lineNums[i] = new List<TextBlock>();
        for (int j = 0; j < lines; j++) {
          var lineNum = new TextBlock {Style = GetStyle("lineNumber")};
          Panel.SetZIndex(lineNum, 5);
          Grid.SetRow(lineNum, j);
          Grid.SetColumn(lineNum, 2 * i);
          grid.Children.Add(lineNum);
          lineNums[i].Add(lineNum);

          var text = new RichTextBox {
            Style = GetStyle("lineText"),
            HorizontalAlignment = HorizontalAlignment.Stretch
          };
          Panel.SetZIndex(text, 5);
          Grid.SetRow(text, j);
          Grid.SetColumn(text, 1 + 2 * i);
          var doc = new FlowDocument();
          var p = new Paragraph();
          doc.Blocks.Add(p);
          text.Document = doc;
          grid.Children.Add(text);
          lineTexts[i].Add(text);
        }
      }

      // Draw the actual text blocks.
      for (int side = 0; side < 2; side++) {
        int prevLine = 0;
        int lineNum = 1;
        lineNumBackgrounds[side] = new List<Border>();
        lineTextBackgrounds[side] = new List<Border>();
        for (int g = 0; g < content[side].Count; g++) {
          LineBlock lblock = content[side][g];
          for (int i = 0; i < lblock.lines.Count; i++) {
            Line line = lblock.lines[i];
            var p = (Paragraph)lineTexts[side][prevLine + i].Document.Blocks.First();
            foreach (Block b in line.blocks) {
              string text = b.text;
              if (text.EndsWith(SentenceFilter.SentenceDelim)) {
                text = text.Substring(0, text.Length - SentenceFilter.SentenceDelim.Length);
              }
              var run = new Run(text);
              if (b.type != BlockType.Normal) {
                if (lblock.type == BlockType.Conflict || lblock.type == BlockType.Edited) {
                  b.type = lblock.type;
                }
                run.Style = GetStyle("text" + b.type);
              }
              p.Inlines.Add(run);
            }
            lineNums[side][prevLine + i].Text = lineNum++.ToString();
          }

          var numBorder = new Border();
          Panel.SetZIndex(numBorder, 3);
          Grid.SetRow(numBorder, prevLine);
          Grid.SetColumn(numBorder, side * 2);
          Grid.SetRowSpan(numBorder, lineCount[g]);
          grid.Children.Add(numBorder);
          lineNumBackgrounds[side].Add(numBorder);

          var textBorder = new Border();
          Panel.SetZIndex(textBorder, 2);
          Grid.SetRow(textBorder, prevLine);
          Grid.SetColumn(textBorder, side * 2 + 1);
          Grid.SetRowSpan(textBorder, lineCount[g]);
          grid.Children.Add(textBorder);
          lineTextBackgrounds[side].Add(textBorder);

          if (lblock.type != BlockType.Normal) {
            numBorder.Style = GetStyle("numBackground" + lblock.type);
            textBorder.Style = GetStyle("textBackground" + lblock.type);
            for (int line = 0; line < lineCount[g]; line++) {
              TextBlock num = lineNums[side][prevLine + line];
              RichTextBox text = lineTexts[side][prevLine + line];
              num.Style = GetStyle("lineNum" + lblock.type);
              text.Style = GetStyle("lineText" + lblock.type);
              int cIndex = conflictBlocks.BinarySearch(g);
              if (cIndex >= 0) {
                int mySide = side; // for lambda scoping
                text.PreviewMouseLeftButtonUp += (o, e) => { ChooseConflict(cIndex, mySide); SelectConflict(cIndex); };
                text.PreviewMouseDoubleClick += (o, e) => EditConflict(cIndex);
                // text.ContextMenu = new ContextMenu();
                text.MouseEnter += (o, e) => HoverConflict(cIndex, mySide, true);
                text.MouseLeave += (o, e) => HoverConflict(cIndex, mySide, false);
              }
            }
          }

          prevLine += lineCount[g];
        }
      }
    }

    private void InitializeBinaryEditor(string myVersion, string newVersion) {
      edit.IsEnabled = false;
      messageMe.Visibility = Visibility.Visible;
      messageNew.Visibility = Visibility.Visible;
      string projectDir = ProjectMonitor.GetProjectDirectory(project);
      string winFilename = gitFilename.Replace('/', System.IO.Path.DirectorySeparatorChar);
      fullpath = Util.PathCombine(projectDir, winFilename);
      string dir = System.IO.Path.GetDirectoryName(fullpath);
      string name = System.IO.Path.GetFileName(fullpath);
      if (myVersion != null) {
        messageMe.Text = "This is a binary file. Please edit the file " + name + " if you wish to edit your version.";
        File.WriteAllText(fullpath, myVersion, Encoding.Default);
      } else {
        messageMe.Text = "You deleted this file.";
      }
      // Copy updated text into a new, temporary file.
      string newFilename = System.IO.Path.GetFileNameWithoutExtension(name) + ".sciGitUpdated" +
        System.IO.Path.GetExtension(name);
      newFullpath = Util.PathCombine(dir, newFilename);
      if (newVersion != null) {
        messageNew.Text = "This is a binary file. Please edit the file " + newFilename +
          " if you wish to edit the updated version.";
        File.WriteAllText(newFullpath, newVersion, Encoding.Default);
      } else {
        messageMe.Text = "This file was deleted in the updated version.";
      }
    }

    private string[] ArraySlice(string[] a, int start, int length) {
      length = Math.Min(length, a.Length - start);
      var ret = new string[length];
      for (int i = 0; i < length; i++) {
        ret[i] = a[start + i];
      }
      return ret;
    }

    private void ProcessBlockDiff(LineBlock oldLineBlock, LineBlock newLineBlock, bool modifyOld = true) {
      // Do block-by-block diffs inside LineBlocks.
      var oldBlocks = new List<Block>();
      var newBlocks = new List<Block>();

      foreach (var line in oldLineBlock.lines) {
        oldBlocks.AddRange(line.blocks);
      }

      foreach (var line in newLineBlock.lines) {
        newBlocks.AddRange(line.blocks);
      }

      var d = new Differ();
      DiffResult diff = d.CreateLineDiffs(String.Join("\n", oldBlocks.Select(x => x.ToString()).ToArray()),
        String.Join("\n", newBlocks.Select(x => x.ToString()).ToArray()), false);

      foreach (DiffBlock dblock in diff.DiffBlocks) {
        if (modifyOld) {
          for (int i = 0; i < dblock.DeleteCountA && dblock.DeleteStartA + i < oldBlocks.Count; i++) {
            oldBlocks[i + dblock.DeleteStartA].type = BlockType.ChangeDelete;
          }
        }
        for (int i = 0; i < dblock.InsertCountB && dblock.InsertStartB + i < newBlocks.Count; i++) {
          newBlocks[i + dblock.InsertStartB].type = BlockType.ChangeAdd;
        }
      }
    }

    private void ProcessDiff(string original, string myVersion, string newVersion) {
      // Do a line-by-line diff, marking changed line groups.
      // Also, find intersecting diffs and mark them as conflicted changes for resolution.
      var d = new Differ();
      var diffs = new DiffResult[2] {
        d.CreateLineDiffs(original, myVersion ?? "", false),
        d.CreateLineDiffs(original, newVersion ?? "", false)
      };

      string[][] newPieces = new string[2][];
      newPieces[0] = diffs[0].PiecesNew;
      newPieces[1] = diffs[1].PiecesNew;
      if (myVersion == "") newPieces[0] = new string[] {""};
      if (newVersion == "") newPieces[1] = new string[] {""};

      var dblocks = new List<Tuple<DiffBlock, int>>();
      for (int side = 0; side < 2; side++) {
        foreach (var block in diffs[side].DiffBlocks) {
          DiffBlock actualBlock = block;
          if (diffs[side].PiecesNew.Length == 0 && block.InsertCountB == 0 && block.InsertStartB == 0) {
            actualBlock = new DiffBlock(block.DeleteStartA, block.DeleteCountA, 0, 1);
          }
          dblocks.Add(new Tuple<DiffBlock, int>(actualBlock, side));
        }
      }
      dblocks.Sort((a, b) => a.Item1.DeleteStartA.CompareTo(b.Item1.DeleteStartA));

      content = new List<LineBlock>[2];
      for (int i = 0; i < 2; i++) {
        content[i] = new List<LineBlock>();
      }
      conflictOrigBlocks = new List<LineBlock>();

      string[] origLines = diffs[0].PiecesOld;
      int nextOriginalLine = 0;
      for (int i = 0; i < dblocks.Count; ) {
        DiffBlock block = dblocks[i].Item1;
        int owner = dblocks[i].Item2;
        // Add unchanged (original) lines.
        if (block.DeleteStartA > nextOriginalLine) {
          foreach (var lineBlocks in content) {
            lineBlocks.Add(new LineBlock(ArraySlice(origLines, nextOriginalLine, block.DeleteStartA - nextOriginalLine)));
          }
          nextOriginalLine = block.DeleteStartA;
        }

        int rangeStart = block.DeleteStartA;
        int rangeEnd = rangeStart + block.DeleteCountA;
        int j = i;
        // If this change intersects any other changes, then merge them together to form a block.
        while (j < dblocks.Count && dblocks[j].Item1.DeleteStartA <= rangeEnd) {
          rangeEnd = Math.Max(rangeEnd, dblocks[j].Item1.DeleteStartA + dblocks[j].Item1.DeleteCountA);
          j++;
        }

        if (j == i + 1) {
          // A regular change.
          var oldBlock = new LineBlock(ArraySlice(diffs[owner].PiecesOld, block.DeleteStartA, block.DeleteCountA), BlockType.ChangeDelete);
          var newBlock = new LineBlock(ArraySlice(newPieces[owner], block.InsertStartB, block.InsertCountB), BlockType.ChangeAdd);
          if (block.DeleteCountA != 0 && block.InsertCountB != 0) {
            oldBlock.type = BlockType.Conflict;
            newBlock.type = BlockType.Conflict;
          } else if (block.DeleteCountA == 0) {
            oldBlock.type = BlockType.Blank;
          } else if (block.InsertCountB == 0) {
            newBlock.type = BlockType.Blank;
          } // can't both be empty!
          ProcessBlockDiff(oldBlock, newBlock);
          content[owner].Add(newBlock);
          content[1 - owner].Add(oldBlock);
          conflictOrigBlocks.Add(owner == 0 ? newBlock : oldBlock);
        } else {
          // Create a change block.
          for (int side = 0; side < 2; side++) {
            int curOriginalLine = rangeStart;
            var conflictBlock = new LineBlock();
            var origBlock = new LineBlock();
            conflictBlock.type = BlockType.Conflict;
            for (int k = i; k < j; k++) {
              DiffBlock subBlock = dblocks[k].Item1;
              if (dblocks[k].Item2 != side) continue;
              if (subBlock.DeleteStartA > curOriginalLine) {
                conflictBlock.AddLines(ArraySlice(origLines, curOriginalLine, subBlock.DeleteStartA - curOriginalLine));
              }
              curOriginalLine = subBlock.DeleteStartA + subBlock.DeleteCountA;
              var oldBlock = new LineBlock(ArraySlice(diffs[side].PiecesOld, subBlock.DeleteStartA, subBlock.DeleteCountA), BlockType.ChangeDelete);
              var newBlock = new LineBlock(ArraySlice(newPieces[side], subBlock.InsertStartB, subBlock.InsertCountB), BlockType.ChangeAdd);
              ProcessBlockDiff(oldBlock, newBlock, false);
              origBlock.Append(oldBlock);
              conflictBlock.Append(newBlock);
            }
            if (rangeEnd > curOriginalLine) {
              conflictBlock.AddLines(ArraySlice(origLines, curOriginalLine, rangeEnd - curOriginalLine));
            }
            content[side].Add(conflictBlock);
            if (side == 0) {
              conflictOrigBlocks.Add(origBlock);
            }
          }

          int last = content[0].Count - 1;
          if (content[0][last].ToString() == content[1][last].ToString()) {
            // Not actually a conflict if they're both the same change.
            if (content[0][last].lines.Count == 0) {
              // If both are deleted, just show nothing.
              content[0].RemoveAt(last);
              content[1].RemoveAt(last);
            } else {
              // Make them normal blocks.
              content[0][last] = content[1][last] = conflictOrigBlocks.Last();
            }
            conflictOrigBlocks.RemoveAt(conflictOrigBlocks.Count - 1);
          }
        }

        nextOriginalLine = rangeEnd;
        i = j;
      }

      // Add any remaining unchanged lines.
      if (origLines.Length > nextOriginalLine) {
        foreach (var lineBlocks in content) {
          lineBlocks.Add(new LineBlock(ArraySlice(origLines, nextOriginalLine, origLines.Length - nextOriginalLine)));
        }
      }

      origBlocks = new LineBlock[2][];
      origBlocks[0] = new LineBlock[content[0].Count];
      origBlocks[1] = new LineBlock[content[1].Count];
      content[0].CopyTo(origBlocks[0]);
      content[1].CopyTo(origBlocks[1]);
    }
  }
}
