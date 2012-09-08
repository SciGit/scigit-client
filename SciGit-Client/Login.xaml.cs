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
  /// Interaction logic for Login.xaml
  /// </summary>
  public partial class Login : Window
  {
    public Login() {
      System.Windows.Forms.Application.EnableVisualStyles();
      InitializeComponent();
      if (Properties.Settings.Default.RememberUser) {
        rememberMe.IsChecked = true;
        emailValue.Text = Properties.Settings.Default.SavedUsername;
        passwordValue.Password = Properties.Settings.Default.SavedPassword;
      }

      // MergeResolver mr = new MergeResolver(new Project { Id = 1, Name = "project1" });
      // mr.Show();
    }

    private void login_Click(object sender, RoutedEventArgs e) {
      login.IsEnabled = false;
      login.Content = "Logging in...";
      BackgroundWorker bg = new BackgroundWorker();
      string email = emailValue.Text, password = passwordValue.Password;
      bg.DoWork += (bw, _) => RestClient.Login(email, password, LoginCallback, Dispatcher);
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
        Hide();
        Main sgMain = new Main();
        sgMain.Show();
        sgMain.Hide();
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
