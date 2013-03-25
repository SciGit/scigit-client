using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using DiffPlex;
using SciGit_Filter;
using System.Collections.Generic;

namespace SciGit_Client
{
  /// <summary>
  /// Interaction logic for DiffViewer.xaml
  /// </summary>
  public partial class DiffViewer : UserControl
  {
    public DiffViewer() {
      InitializeComponent();
    }

    public void DisplayEmpty() {
      grid.RowDefinitions.Clear();
      var rd = new RowDefinition { Height = new GridLength(1, GridUnitType.Star) };
      grid.RowDefinitions.Add(rd);
      
      // Remove the line number blocks and such. actionsThem is the last item in the XAML
      int x = grid.Children.IndexOf(message);
      grid.Children.RemoveRange(x + 1, grid.Children.Count - x);

      message.Visibility = Visibility.Visible;
      message.Text = "No files were changed.";
    }

    public void DisplayDiff(string filename, string fullpath, string author, string old, string updated) {
      grid.RowDefinitions.Clear();
      var rd = new RowDefinition { Height = new GridLength(1, GridUnitType.Star) };
      grid.RowDefinitions.Add(rd);

      // Remove the line number blocks and such. actionsThem is the last item in the XAML
      int x = grid.Children.IndexOf(message);
      grid.Children.RemoveRange(x + 1, grid.Children.Count - x);

      message.Visibility = Visibility.Collapsed;
      if (filename.EndsWith(".docx") || filename.EndsWith(".doc")) {
        // Word document. So let the user open the doc in Word
        message.Visibility = Visibility.Visible;
        message.Text = "This is a Word document. Please save it to view its contents.";
        message.Inlines.Add(" You can also ");
        var fakeUri = new Uri("http://asdf.com");
        var link = new Hyperlink(new Run("view the changes")) { NavigateUri = fakeUri };
        link.RequestNavigate += (s, e) => CompareInWord(old, updated, filename, Path.GetDirectoryName(fullpath), author);
        message.Inlines.Add(link);
        message.Inlines.Add(" in Word.");
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
  }
}
