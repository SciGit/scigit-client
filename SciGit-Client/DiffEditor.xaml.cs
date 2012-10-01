using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using SciGit_Filter;

namespace SciGit_Client
{
  /// <summary>
  /// Interaction logic for DiffEditor.xaml
  /// </summary>
  public partial class DiffEditor : Window
  {
    string myStr;
    public LineBlock newBlock;
    string originalStr;
    string updatedStr;

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
        var p = new Paragraph();
        foreach (Block block in line.blocks) {
          string text = block.text;
          if (text.EndsWith(SentenceFilter.SentenceDelim)) {
            text = text.Substring(0, text.Length - SentenceFilter.SentenceDelim.Length);
          }
          var run = new Run(text);
          run.Style = GetStyle("text" + block.type);
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
