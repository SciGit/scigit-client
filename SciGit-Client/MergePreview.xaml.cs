using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using SciGit_Filter;

namespace SciGit_Client
{
  /// <summary>
  /// Interaction logic for DiffPreview.xaml
  /// </summary>
  public partial class MergePreview : Window
  {
    int activeTextBlock;
    List<TextBox> textBoxes;
    private List<bool> special;
    private List<string> originalText;

    public MergePreview(List<FileData> files, List<string> fileContents) {
      InitializeComponent();

      textBoxes = new List<TextBox>();
      special = new List<bool>();
      originalText = new List<string>();
      for (int i = 0; i < files.Count; i++) {
        FileData f = files[i];
        string text = fileContents[i];

        var textBox = new TextBox();
        originalText.Add(text);
        if (SentenceFilter.IsBinary(text)) {
          textBox.Text = "This is a binary file.";
          textBox.IsEnabled = false;
          special.Add(true);
        } else if (text == null) {
          textBox.Text = "This file will be deleted.";
          textBox.IsEnabled = false;
          special.Add(true);
        } else {
          textBox.Text = text; 
          special.Add(false);
        }
        if (i > 0) {
          textBox.Visibility = Visibility.Hidden;
        }
        Grid.SetRow(textBox, 1);
        grid.Children.Add(textBox);
        textBoxes.Add(textBox);

        var cbItem = new ComboBoxItem {Content = f.filename};
        int cur = fileDropdown.Items.Count;
        cbItem.Selected += (e, o) => SetActiveTextBlock(cur);
        fileDropdown.Items.Add(cbItem);
      }

      activeTextBlock = 0;
      fileDropdown.SelectedIndex = 0;
      SetActiveTextBlock(0);
    }

    public bool Saved { get; private set; }

    public List<string> GetFinalText() {
      var result = new List<string>();
      for (int i = 0; i < textBoxes.Count; i++) {
        var textBox = textBoxes[i];
        result.Add(special[i] ? originalText[i] : textBox.Text);
      }
      return result;
    }

    private void SetActiveTextBlock(int index) {
      if (index != activeTextBlock) {
        textBoxes[activeTextBlock].Visibility = Visibility.Hidden;
        textBoxes[index].Visibility = Visibility.Visible;
        activeTextBlock = index;
      }
    }

    private void ClickFinish(object sender, RoutedEventArgs e) {
      Saved = true;
      Close();
    }

    private void ClickCancel(object sender, RoutedEventArgs e) {
      Close();
    }
  }
}
