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
  /// Interaction logic for CommitForm.xaml
  /// </summary>
  public partial class CommitForm : Window
  {
    public string savedMessage = null;

    public CommitForm(Project p) {
      InitializeComponent();
    }

    private void ClickUpload(object sender, RoutedEventArgs e) {
      savedMessage = message.Text;
      Close();
    }

    private void ClickCancel(object sender, RoutedEventArgs e) {
      Close();
    }

    private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e) {
      if (savedMessage == null) {
        var res = MessageBox.Show(this, "This will cancel the upload process. Are you sure?", "Confirm cancel", MessageBoxButton.YesNo);
        if (res == MessageBoxResult.No) {
          e.Cancel = true;
        }
      }
    }
  }
}
