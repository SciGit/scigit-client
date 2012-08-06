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
using System.ComponentModel;

namespace SciGit_Client
{
  /// <summary>
  /// Interaction logic for SGStart.xaml
  /// </summary>
  public partial class SGStart : Window
  {
    public SGStart() {
      System.Windows.Forms.Application.EnableVisualStyles();
      InitializeComponent();
      if (Properties.Settings.Default.RememberUser) {
        rememberMe.IsChecked = true;
        emailValue.Text = Properties.Settings.Default.SavedUsername;
        passwordValue.Password = Properties.Settings.Default.SavedPassword;
      }

      MergeResolver mr = new MergeResolver();
      mr.Show();
    }

    private void login_Click(object sender, RoutedEventArgs e) {
      login.IsEnabled = false;
      login.Content = "Logging in...";
      BackgroundWorker bg = new BackgroundWorker();
      string email = emailValue.Text, password = passwordValue.Password;
      bg.DoWork += (bw, _) => SGRestClient.Login(email, password, LoginCallback, Dispatcher);
      bg.RunWorkerAsync();
    }

    private void LoginCallback(bool success) {
      if (success) {
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
      } else {
        MessageBox.Show("Invalid username or password.", "Error");
        login.IsEnabled = true;
        login.Content = "Login";
      }
    }

    private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e) {
      Process.Start(e.Uri.AbsoluteUri);
    }
  }
}
