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
    private Main main;

    public Login() {
      // new MergeResolver(new Project {name="Test Project"}).Show(); Close(); return;
      InitializeComponent();

      registerLink.NavigateUri = new Uri("https://" + Settings.Default.SciGitHostname + "/auth/register");
      forgotPassLink.NavigateUri = new Uri("https://" + Settings.Default.SciGitHostname + "/auth/forgot_password");

      if (Settings.Default.RememberUser) {
        rememberMe.IsChecked = true;
        emailValue.Text = Settings.Default.SavedUsername;
        passwordValue.Password = Settings.Default.SavedPassword;
        string[] args = Environment.GetCommandLineArgs();
        if (args.Length == 2 && args[1] == "-autologin") {
          BeginLogin();
        }
      }
    }

    public void Reset() {
      login.Content = "Login";
      login.IsEnabled = true;
    }

    private void login_Click(object sender, RoutedEventArgs e) {
      BeginLogin();
    }

    private void BeginLogin() {
      login.IsEnabled = false;
      login.Content = "Logging in...";
      var bg = new BackgroundWorker();
      string email = emailValue.Text, password = passwordValue.Password;
      bg.DoWork += (bw, _) => {
        var result = RestClient.Login(email, password);
        Dispatcher.Invoke(new Action(() => FinishLogin(result)));
      };
      bg.RunWorkerAsync();
    }

    private void FinishLogin(RestClient.Response<bool> result) {
      if (result.Data) {
        if (rememberMe.IsChecked ?? false) {
          Settings.Default.RememberUser = true;
          Settings.Default.SavedUsername = emailValue.Text;
          Settings.Default.SavedPassword = passwordValue.Password;
          Settings.Default.Save();
        } else {
          Settings.Default.RememberUser = false;
          Settings.Default.Save();
        }
        Hide();
        main = new Main(this);
        main.Show();
        main.Hide();
      } else {
        MessageBox.Show(result.Error == RestClient.ErrorType.Forbidden ?
          "Incorrect username or password." :
          "Could not connect to the SciGit servers. Please try again later.", "Login error");
        Reset();
      }
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e) {
      Process.Start(e.Uri.AbsoluteUri);
    }
  }
}
