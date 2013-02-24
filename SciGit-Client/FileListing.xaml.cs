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

namespace SciGit_Client
{
  /// <summary>
  /// Interaction logic for FileListing.xaml
  /// </summary>
  public partial class FileListing : UserControl
  {
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
      "Title",
      typeof(string),
      typeof(FileListing),
      new PropertyMetadata("", Update)
    );

    public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
      "Color",
      typeof (Color),
      typeof (FileListing),
      new PropertyMetadata(Colors.DeepSkyBlue, Update)
    );

    public string Title {
      get { return (string)GetValue(TitleProperty); }
      set { SetValue(TitleProperty, value); }
    }

    public Color Color {
      get { return (Color)GetValue(ColorProperty); }
      set { SetValue(ColorProperty, value); }
    }

    public List<Action<string>> SelectionHandlers = new List<Action<string>>();
    private bool cleared = false;

    public FileListing() {
      InitializeComponent();
      listBox.SelectionChanged += SelectionChanged;
    }

    public void AddFile(string filename) {
      if (!listBox.IsEnabled) {
        listBox.IsEnabled = true;
        listBox.Items.Clear();
      }

      var item = new ListBoxItem();
      item.Content = filename;
      listBox.Items.Add(item);
    }

    public void Select(int index) {
      listBox.SelectedItem = listBox.Items[index];
    }

    public void ClearSelection() {
      cleared = true;
      listBox.UnselectAll();
      cleared = false;
    }

    private void SelectionChanged(object sender, SelectionChangedEventArgs e) {
      // When an item becomes selected, notify the callbacks
      if (e.AddedItems.Count > 0) {
        foreach (var handler in SelectionHandlers) {
          handler((e.AddedItems[0] as ListBoxItem).Content as string);
        } 
      }

      // Don't allow deselection. When it happens, reselect the previously selected item.
      if (!cleared && listBox.SelectedItem == null) {
        listBox.SelectedItem = e.RemovedItems[0];
      }
    }

    private static void Update(DependencyObject o, DependencyPropertyChangedEventArgs e) {
      var obj = o as FileListing;
      obj.title.Text = obj.Title;
      var brush = obj.Resources["ColorBrush"] as SolidColorBrush;
      brush.Color = obj.Color;      
    }
  }
}
