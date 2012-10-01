using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SciGit_Client
{
  /// <summary>
  /// Interaction logic for DiffPreview.xaml
  /// </summary>
  public partial class DiffPreview : Window
  {
    int activeTextBlock;
    List<TextBox> textBoxes;

    public DiffPreview(List<FileData> files, List<string> fileContents) {
      InitializeComponent();

      textBoxes = new List<TextBox>();
      for (int i = 0; i < files.Count; i++) {
        FileData f = files[i];
        string text = fileContents[i];

        var textBox = new TextBox();
        textBox.Text = text;
        if (i > 0) {
          textBox.Visibility = Visibility.Hidden;
        }
        Grid.SetRow(textBox, 2);
        grid.Children.Add(textBox);
        textBoxes.Add(textBox);

        var cbItem = new ComboBoxItem();
        cbItem.Content = f.filename;
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
      foreach (var textBox in textBoxes) {
        result.Add(textBox.Text);
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
