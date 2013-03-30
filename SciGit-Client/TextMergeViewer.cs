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
  public class TextMergeViewer : MergeViewer
  {
    private string filename, myVersion, newVersion;

    private List<int> conflictBlocks;
    private int[] conflictChoice, conflictHover;
    private List<LineBlock> conflictOrigBlocks;
    private List<LineBlock>[] content;
    private int curConflict;
    private int[] lineCount;
    private List<TextBlock>[] lineNums;
    private List<Border>[] lineNumBackgrounds, lineTextBackgrounds;
    private List<Border> scrollNavBorders;
    private List<RichTextBox>[] lineTexts;
    private LineBlock[][] origBlocks;

    public TextMergeViewer(Project p, string filename, string original, string myVersion, string newVersion)
        : base(p, filename, original, myVersion, newVersion)
    {
      this.filename = filename;
      this.myVersion = myVersion;
      this.newVersion = newVersion;

      if (original == null) {
        original = ""; // We don't really have to display anything differently.
        // Also, add-add conflicts don't need any special treatment.
      }

      ProcessDiff(original, myVersion, newVersion);
      InitializeEditor();

      // Select the first conflict, update the conflict indicators.
      conflictHover = new int[conflictBlocks.Count];
      conflictChoice = new int[conflictBlocks.Count];
      for (int i = 0; i < conflictChoice.Length; i++) {
        conflictHover[i] = conflictChoice[i] = -1;
      }
      SelectConflict(0);
      if (conflictBlocks.Count > 1) {
        nextConflict.IsEnabled = prevConflict.IsEnabled = true;
      }

      UpdateStatus();
    }

    public override bool Finished() {
      if (manual) {
        return selectedSide != -1;
      }
      return !conflictChoice.Contains(-1);
    }

    public override string GetMergeResult() {
      if (!Finished()) return null;

      if (manual) {
        if (selectedSide == deletedSide) return null;
        return Util.ReadFile(selectedSide == 0 ? fullpath : newFullpath);
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
          textBackground.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 0xdd, 0));
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

      if (index == curConflict) {
        scrollNavBorders[index].Style = GetStyle("scrollBorderActive");
      } else if (conflictChoice[index] != -1) {
        scrollNavBorders[index].Style = GetStyle("scrollBorderDone");
      } else {
        scrollNavBorders[index].Style = GetStyle("scrollBorder");
      }
    }

    private void UpdateStatus() {
      int cnt = manual ? (selectedSide == -1 ? 1 : 0) : conflictChoice.Count(x => x == -1);
      status.Text = cnt + " unresolved conflicts remaining.";
    }

    private void SelectConflict(int index, bool scroll = true) {
      int newBlock = conflictBlocks[index];
      int line = lineCount.Take(newBlock).Sum();
      if (scroll) {
        Point p = lineNums[0][line].TransformToAncestor(grid).Transform(new Point(0, 0));
        scrollViewer.ScrollToVerticalOffset(p.Y - scrollViewer.ViewportHeight/2 + lineTextBackgrounds[0][newBlock].ActualHeight/2);
      }

      Grid.SetRow(actionsMe, 2*(line + lineCount[newBlock]) - 1);
      Grid.SetRow(actionsThem, 2*(line + lineCount[newBlock]) - 1);

      conflictNumber.Text = "Conflict " + (index + 1) + "/" + conflictBlocks.Count;
      int lastConflict = curConflict;
      curConflict = index;
      UpdateConflictBlock(lastConflict);
      UpdateConflictBlock(curConflict);

      acceptMe.IsChecked = conflictChoice[index] == 0;
      acceptThem.IsChecked = conflictChoice[index] == 1;
      revertMe.IsEnabled = content[0][newBlock].type == BlockType.Edited;
      revertThem.IsEnabled = content[1][newBlock].type == BlockType.Edited;
    }

    private void HoverConflict(int index, int side, bool on) {
      conflictHover[index] = on ? side : -1;
      UpdateConflictBlock(index);
    }

    private void ChooseConflict(int index, int side) {
      conflictChoice[index] = side;
      acceptMe.IsChecked = side == 0;
      acceptThem.IsChecked = side == 1;
      UpdateStatus();
      UpdateConflictBlock(index);
    }

    private void EditConflict(int index, int side) {
      int block = conflictBlocks[index];
      LineBlock chosenBlock = content[side][block];
      var de = new MergeEditor(origBlocks[0][block], origBlocks[1][block], conflictOrigBlocks[index], chosenBlock);
      de.ShowDialog();
      if (de.newBlock != null) {
        ProcessBlockDiff(conflictOrigBlocks[index], de.newBlock, false);
        content[side][block] = de.newBlock;
        if (side == 0) {
          revertMe.IsEnabled = true;
        } else {
          revertThem.IsEnabled = true;
        }
        ReloadEditor();
      }
    }

    private void ClickAccept(int index, int side) {
      if (manual) {
        selectedSide = side;
        acceptMe.IsChecked = side == 0;
        acceptThem.IsChecked = side == 1;
        UpdateStatus();
        return;
      }

      if (conflictChoice[index] == side) {
        ChooseConflict(index, -1);
      } else {
        ChooseConflict(index, side);
      }
    }

    protected override void Accept(int side) {
      ClickAccept(curConflict, side);
    }

    protected override void SelectPreviousConflict(object sender, RoutedEventArgs e) {
      int numConflicts = conflictBlocks.Count;
      SelectConflict((curConflict + numConflicts - 1) % numConflicts);
    }

    protected override void SelectNextConflict(object sender, RoutedEventArgs e) {
      int numConflicts = conflictBlocks.Count;
      SelectConflict((curConflict + 1) % numConflicts);
    }

    protected override void ClickEditMe(object sender, RoutedEventArgs e) {
      EditConflict(curConflict, 0);
    }

    protected override void ClickEditThem(object sender, RoutedEventArgs e) {
      EditConflict(curConflict, 1);
    }

    private void ClickRevert(int side) {
      int block = conflictBlocks[curConflict];
      content[side][block] = origBlocks[side][block];
      if (side == 0) {
        revertMe.IsEnabled = false;
      } else {
        revertThem.IsEnabled = false;
      }
      ReloadEditor();
    }

    protected override void ClickRevertMe(object sender, RoutedEventArgs e) {
      ClickRevert(0);
    }

    protected override void ClickRevertThem(object sender, RoutedEventArgs e) {
      ClickRevert(1);
    }

    protected override void ClickManualMerge(object sender, RoutedEventArgs e) {
      if (manual == false) {
        manual = true;
        selectedSide = -1;
        ClearEditor();
        CreateFiles(filename, myVersion, newVersion);
        acceptMe.IsChecked = acceptThem.IsChecked = false;
        editMe.Visibility = editThem.Visibility = Visibility.Collapsed;
        revertMe.Visibility = revertThem.Visibility = Visibility.Collapsed;
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Grid.SetRow(actionsMe, 1);
        Grid.SetRow(actionsThem, 1);
      } else {
        manual = false;
        Cleanup();
        ReloadEditor();
        messageMe.Visibility = messageNew.Visibility = Visibility.Collapsed;
        acceptMe.Content = "Accept _my version";
        acceptThem.Content = "Accept _updated version";
        editMe.Visibility = editThem.Visibility = Visibility.Visible;
        revertMe.Visibility = revertThem.Visibility = Visibility.Visible;
      }

      UpdateStatus();
    }

    private void ClearEditor() {
      grid.RowDefinitions.Clear();
      var rd = new RowDefinition { Height = new GridLength(1, GridUnitType.Star) };
      grid.RowDefinitions.Add(rd);
      conflictNav.RowDefinitions.Clear();

      // Remove the line number blocks and such. actionsThem is the last item in the XAML
      int x = grid.Children.IndexOf(actionsThem);
      grid.Children.RemoveRange(x + 1, grid.Children.Count - x);

      conflictNav.Visibility = Visibility.Collapsed;
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
        grid.RowDefinitions.Insert(0, new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Insert(0, new RowDefinition { Height = GridLength.Auto });
        conflictNav.RowDefinitions.Insert(0, new RowDefinition());
      }

      // Only show the scroll helper if it's a long file.
      if (lines > 25) {
        conflictNav.Visibility = Visibility.Visible;
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
          var lineNum = new TextBlock { Style = GetStyle("lineNumber") };
          Panel.SetZIndex(lineNum, 5);
          Grid.SetRow(lineNum, 2 * j);
          Grid.SetColumn(lineNum, 2 * i);
          grid.Children.Add(lineNum);
          lineNums[i].Add(lineNum);

          var text = new RichTextBox {
            Style = GetStyle("lineText"),
            HorizontalAlignment = HorizontalAlignment.Stretch
          };
          Panel.SetZIndex(text, 5);
          Grid.SetRow(text, 2 * j);
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
      scrollNavBorders = new List<Border>();
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
          Grid.SetRow(numBorder, 2 * prevLine);
          Grid.SetColumn(numBorder, side * 2);
          Grid.SetRowSpan(numBorder, 2 * lineCount[g]);
          grid.Children.Add(numBorder);
          lineNumBackgrounds[side].Add(numBorder);

          var textBorder = new Border();
          Panel.SetZIndex(textBorder, 2);
          Grid.SetRow(textBorder, 2 * prevLine);
          Grid.SetColumn(textBorder, side * 2 + 1);
          Grid.SetRowSpan(textBorder, 2 * lineCount[g]);
          grid.Children.Add(textBorder);
          lineTextBackgrounds[side].Add(textBorder);

          if (lblock.type != BlockType.Normal) {
            numBorder.Style = GetStyle("numBackground" + lblock.type);
            textBorder.Style = GetStyle("textBackground" + lblock.type);
            int cIndex = conflictBlocks.BinarySearch(g);
            for (int line = 0; line < lineCount[g]; line++) {
              TextBlock num = lineNums[side][prevLine + line];
              RichTextBox text = lineTexts[side][prevLine + line];
              num.Style = GetStyle("lineNum" + lblock.type);
              text.Style = GetStyle("lineText" + lblock.type);
              if (cIndex >= 0) {
                int mySide = side; // for lambda scoping
                text.PreviewMouseLeftButtonUp += (o, e) => { ChooseConflict(cIndex, mySide); SelectConflict(cIndex, false); };
                text.PreviewMouseDoubleClick += (o, e) => EditConflict(cIndex, mySide);
                // text.ContextMenu = new ContextMenu();
                text.MouseEnter += (o, e) => HoverConflict(cIndex, mySide, true);
                text.MouseLeave += (o, e) => HoverConflict(cIndex, mySide, false);
              }
            }

            if (side == 0) {
              var scrollBorder = new Border();
              Grid.SetRow(scrollBorder, prevLine);
              Grid.SetRowSpan(scrollBorder, lineCount[g]);
              conflictNav.Children.Add(scrollBorder);
              scrollBorder.Style = GetStyle("scrollBorder" + (scrollNavBorders.Count == 0 ? "Active" : ""));
              scrollBorder.MouseUp += (o, e) => SelectConflict(cIndex);
              scrollBorder.ToolTip = String.Format("Conflict {0}/{1}", cIndex+1, conflictBlocks.Count);
              scrollNavBorders.Add(scrollBorder);
            }
          }

          prevLine += lineCount[g];
        }
      }
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
      if (myVersion == "") newPieces[0] = new string[] { "" };
      if (newVersion == "") newPieces[1] = new string[] { "" };

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
            lineBlocks.Add(new LineBlock(Util.ArraySlice(origLines, nextOriginalLine, block.DeleteStartA - nextOriginalLine)));
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
          var oldBlock = new LineBlock(Util.ArraySlice(diffs[owner].PiecesOld, block.DeleteStartA, block.DeleteCountA), BlockType.ChangeDelete);
          var newBlock = new LineBlock(Util.ArraySlice(newPieces[owner], block.InsertStartB, block.InsertCountB), BlockType.ChangeAdd);
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
                conflictBlock.AddLines(Util.ArraySlice(origLines, curOriginalLine, subBlock.DeleteStartA - curOriginalLine));
              }
              curOriginalLine = subBlock.DeleteStartA + subBlock.DeleteCountA;
              var oldBlock = new LineBlock(Util.ArraySlice(diffs[side].PiecesOld, subBlock.DeleteStartA, subBlock.DeleteCountA), BlockType.ChangeDelete);
              var newBlock = new LineBlock(Util.ArraySlice(newPieces[side], subBlock.InsertStartB, subBlock.InsertCountB), BlockType.ChangeAdd);
              ProcessBlockDiff(oldBlock, newBlock, false);
              origBlock.Append(oldBlock);
              conflictBlock.Append(newBlock);
            }
            if (rangeEnd > curOriginalLine) {
              conflictBlock.AddLines(Util.ArraySlice(origLines, curOriginalLine, rangeEnd - curOriginalLine));
            }
            if (conflictBlock.lines.Count == 0) {
              conflictBlock.type = BlockType.ChangeDelete;
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
          lineBlocks.Add(new LineBlock(Util.ArraySlice(origLines, nextOriginalLine, origLines.Length - nextOriginalLine)));
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
