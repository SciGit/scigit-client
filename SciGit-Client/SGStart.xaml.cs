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
using System.Diagnostics;

namespace SciGit_Client
{
  /// <summary>
  /// Interaction logic for SGStart.xaml
  /// </summary>
  public partial class SGStart : Window
  {
    public SGStart() {
      InitializeComponent();
      if (Properties.Settings.Default.RememberUser) {
        rememberMe.IsChecked = true;
        emailValue.Text = Properties.Settings.Default.SavedUsername;
        passwordValue.Password = Properties.Settings.Default.SavedPassword;
      }
    }

    private void login_Click(object sender, RoutedEventArgs e) {
      if (SGRestClient.Login(emailValue.Text, passwordValue.Password)) {
        if (rememberMe.IsChecked ?? false) {
          Properties.Settings.Default.RememberUser = true;
          Properties.Settings.Default.SavedUsername = emailValue.Text;
          Properties.Settings.Default.SavedPassword = passwordValue.Password;
          Properties.Settings.Default.Save();
        }
        SGMain sgMain = new SGMain();
        sgMain.Show();
        sgMain.Hide();
        Hide();
      }
    }

    private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e) {
      Process.Start(e.Uri.AbsoluteUri);
    }
  }
}
