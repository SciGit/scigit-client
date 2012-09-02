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

namespace SciGit_Client
{
  /// <summary>
  /// Interaction logic for DiffPreview.xaml
  /// </summary>
  public partial class DiffPreview : Window
  {
    List<TextBox> textBoxes;
    int activeTextBlock;
    public bool Saved { get; private set; }

    public DiffPreview(List<FileData> files, List<string> fileContents) {
      InitializeComponent();

      textBoxes = new List<TextBox>();
      for (int i = 0; i < files.Count; i++) {
        FileData f = files[i];
        string text = fileContents[i];

        var textBox = new TextBox();
        textBox.Text = text;
        if (i > 0) {
          textBox.Visibility = System.Windows.Visibility.Hidden;
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

    private void SetActiveTextBlock(int index) {
      if (index != activeTextBlock) {
        textBoxes[activeTextBlock].Visibility = System.Windows.Visibility.Hidden;
        textBoxes[index].Visibility = System.Windows.Visibility.Visible;
        activeTextBlock = index;
      }
    }

    private void ClickFinish(object sender, RoutedEventArgs e) {
      Saved = true;
      // TODO: write the files
      Close();
    }

    private void ClickCancel(object sender, RoutedEventArgs e) {
      Close();
    }
  }
}
