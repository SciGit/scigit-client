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
  public class Block
  {
    public Block(string v) {
      value = new string[]{v};
    }
    public Block(string v, string v2) {
      value = new string[]{v, v2};
    }

    public string[] value;
  }

  /// <summary>
  /// Interaction logic for MergeResolver.xaml
  /// </summary>
  public partial class MergeResolver : Window
  {
    public MergeResolver() {
      InitializeComponent();

      ComboBoxItem item = new ComboBoxItem();
      item.Content = "test.tex";
      fileDropdown.Items.Add(item);
      fileDropdown.SelectedIndex = 0;
    }

    private void Window_Closed(object sender, EventArgs e) {
      Environment.Exit(0);
    }
  }
}
