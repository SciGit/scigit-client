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
using System.Windows.Navigation;
using System.Windows.Shapes;
using SciGit_Filter;

namespace SciGit_Client
{
  /// <summary>
  /// Interaction logic for DiffViewer.xaml
  /// </summary>
  public partial class DiffViewer : UserControl
  {
    public DiffViewer() {
      InitializeComponent();

      string diff = @"This is sentence one.
this is sentence two.
this is where %%%%%%%CS%%%%%%%things start%%%%%%%CD%%%%%%%stuff starts%%%%%%%CE%%%%%%% to differ. long ass sentence here blahblahbalh";

      List<Block> blocks = SplitBlocks(diff);

      int lines = diff.Count(ch => ch == '\n') + 1;
      for (int i = 0; i < lines; i++) {
        RowDefinition rd = new RowDefinition();
        rd.Height = GridLength.Auto;
        grid.RowDefinitions.Insert(0, rd);
      }

      List<RichTextBox>[] lineTexts = new List<RichTextBox>[2];
      List<TextBlock>[] lineNums = new List<TextBlock>[2];
      for (int i = 0; i < 2; i++) {
        lineTexts[i] = new List<RichTextBox>();
        lineNums[i] = new List<TextBlock>();
        for (int j = 0; j < lines; j++) {
          TextBlock lineNum = new TextBlock();
          lineNum.Text = (j + 1).ToString();
          lineNum.Style = (Style)Resources["lineNumber"];
          Grid.SetRow(lineNum, j);
          Grid.SetColumn(lineNum, 2*i);
          grid.Children.Add(lineNum);          
          lineNums[i].Add(lineNum);

          RichTextBox text = new RichTextBox();
          text.Style = (Style)Resources["lineText"];
          Grid.SetRow(text, j);
          Grid.SetColumn(text, 1 + 2*i);
          FlowDocument doc = new FlowDocument();
          doc.Blocks.Add(new Paragraph());
          text.Document = doc;
          grid.Children.Add(text);
          lineTexts[i].Add(text);
        }
      }

      for (int i = 0; i < 2; i++) {
        int curBlock = 0;
        foreach (var block in blocks) {
          string str = block.value.Length == 1 ? block.value[0] : block.value[i];
          string[] blockLines = str.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
          bool conflict = false;
          for (int j = 0; j < blockLines.Length; j++) {
            Run run = new Run(blockLines[j]);
            if (block.value.Length > 1) {
              run.Style = (Style)Resources["textConflict"];
              conflict = true;
            }
            Paragraph p = (Paragraph)lineTexts[i][curBlock].Document.Blocks.First();
            p.Inlines.Add(run);
            if (j != blockLines.Length - 1) curBlock++;
          }
          if (conflict) {
            lineTexts[i][curBlock].Style = (Style)Resources["lineTextConflict"];
            lineNums[i][curBlock].Style = (Style)Resources["lineNumConflict"];
          }
        }
      }
    }

    public List<Block> SplitBlocks(string str) {
      List<Block> blocks = new List<Block>();
      int pos = str.IndexOf(SentenceFilter.ConflictStart);
      while (pos != -1) {
        if (pos != 0) {
          blocks.Add(new Block(str.Substring(0, pos)));
        }
        int pos2 = str.IndexOf(SentenceFilter.ConflictDelim);
        int pos3 = str.IndexOf(SentenceFilter.ConflictEnd);
        int len = SentenceFilter.ConflictStart.Length;
        blocks.Add(new Block(str.Substring(pos + len, pos2 - pos - len),
                             str.Substring(pos2 + len, pos3 - pos2 - len)));
        str = str.Substring(pos3 + SentenceFilter.ConflictEnd.Length);
        pos = str.IndexOf(SentenceFilter.ConflictStart);
      }
      if (str.Length > 0) {
        blocks.Add(new Block(str));
      }
      return blocks;
    }
  }
}
