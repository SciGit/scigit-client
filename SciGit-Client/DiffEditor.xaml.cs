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

namespace SciGit_Client
{
  /// <summary>
  /// Interaction logic for DiffEditor.xaml
  /// </summary>
  public partial class DiffEditor : Window
  {
    public LineBlock newBlock;
    string myStr, updatedStr, originalStr;

    public DiffEditor(LineBlock yourBlock, LineBlock updatedBlock, LineBlock originalBlock, LineBlock editBlock = null) {
      InitializeComponent();

      RenderLineBlock(yourBlock, yourText);
      myStr = yourBlock.ToString();
      RenderLineBlock(updatedBlock, updatedText);
      updatedStr = updatedBlock.ToString();
      originalStr = originalBlock.ToString();
      if (editBlock != null) {
        mergedText.Text = editBlock.ToString();
      }
    }

    private Style GetStyle(string name) {
      return Application.Current.Resources[name] as Style;
    }

    private void RenderLineBlock(LineBlock lineBlock, RichTextBox r) {
      r.Document.Blocks.Clear();
      foreach (Line line in lineBlock.lines) {
        Paragraph p = new Paragraph();
        foreach (Block block in line.blocks) {
          string text = block.text;
          if (text.EndsWith(SentenceFilter.SentenceDelim)) {
            text = text.Substring(0, text.Length - SentenceFilter.SentenceDelim.Length);
          }
          Run run = new Run(text);
          run.Style = GetStyle("text" + block.type.ToString());
          p.Inlines.Add(run);
        }
        r.Document.Blocks.Add(p);
      }
    }

    private void ClickMine(object sender, RoutedEventArgs e) {
      mergedText.Text = myStr;
    }

    private void ClickUpdated(object sender, RoutedEventArgs e) {
      mergedText.Text = updatedStr;
    }

    private void ClickOriginal(object sender, RoutedEventArgs e) {
      mergedText.Text = originalStr;
    }

    private void ClickSave(object sender, RoutedEventArgs e) {
      string text = mergedText.Text;
      newBlock = new LineBlock(SentenceFilter.SplitLines(text), BlockType.Edited);
      Close();
    }

    private void ClickCancel(object sender, RoutedEventArgs e) {
      Close();
    }
  }
}
