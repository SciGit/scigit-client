using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using SciGit_Client.Properties;
using System;

namespace SciGit_Client
{
  /// <summary>
  /// Interaction logic for Login.xaml
  /// </summary>
  public partial class Login : Window
  {
    public Login() {
      InitializeComponent();
      if (Settings.Default.RememberUser) {
        rememberMe.IsChecked = true;
        emailValue.Text = Settings.Default.SavedUsername;
        passwordValue.Password = Settings.Default.SavedPassword;
      }

      // MergeResolver mr = new MergeResolver(new Project { Id = 1, Name = "project1" });
      // mr.Show();
    }

    private void login_Click(object sender, RoutedEventArgs e) {
      login.IsEnabled = false;
      login.Content = "Logging in...";
      var bg = new BackgroundWorker();
      string email = emailValue.Text, password = passwordValue.Password;
      bg.DoWork += (bw, _) => RestClient.Login(email, password, LoginCallback);
      bg.RunWorkerAsync();
    }

    private void LoginCallback(bool success, string error = "") {
      Dispatcher.Invoke(new Action(() => {
        if (success) {
          if (rememberMe.IsChecked ?? false) {
            Settings.Default.RememberUser = true;
            Settings.Default.SavedUsername = emailValue.Text;
            Settings.Default.SavedPassword = passwordValue.Password;
            Settings.Default.Save();
          }
          Hide();
          var sgMain = new Main();
          sgMain.Show();
          sgMain.Hide();
        } else {
          MessageBox.Show(error, "Error");
          login.IsEnabled = true;
          login.Content = "Login";
        }
      }));
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e) {
      Process.Start(e.Uri.AbsoluteUri);
    }
  }
}
