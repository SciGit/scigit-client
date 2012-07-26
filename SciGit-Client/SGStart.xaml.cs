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
    /// Interaction logic for SGStart.xaml
    /// </summary>
    public partial class SGStart : Window
    {
        enum Phase
        {
            Start_Welcome = 0,
            Start_Login,
            Start_Length
        }

        Phase mPhase = Phase.Start_Login;

        public SGStart()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void continue_Click(object sender, RoutedEventArgs e)
        {
            switch (mPhase)
            {
                case Phase.Start_Welcome:
                    if (!(welcome.dontHaveAccount.IsChecked ?? false) && !(welcome.haveAccount.IsChecked ?? false))
                    {
                        // Don't continue since they need to choose an option.
                        return;
                    }

                    if (welcome.haveAccount.IsChecked ?? false)
                    {
                        welcome.Visibility = Visibility.Collapsed;
                        login.Visibility = Visibility.Visible;
                        back.Visibility = Visibility.Visible;
                        mPhase++;
                    }
                    else
                    {
                        System.Diagnostics.Process.Start("https://scigit.sherk.me/auth/login");
                    }

                    break;
                case Phase.Start_Login:
                    if (SGRestClient.Login(login.emailValue.Text, login.passwordValue.Password))
                    {
                        SGMain sgMain = new SGMain();
                        sgMain.Show();
                        sgMain.Hide();
                        Hide();
                    }
                    break;
            }
        }

        private void back_Click(object sender, RoutedEventArgs e)
        {
            switch (mPhase)
            {
                case Phase.Start_Welcome:
                    // what???
                    break;
                case Phase.Start_Login:
                    welcome.Visibility = Visibility.Visible;
                    login.Visibility = Visibility.Collapsed;
                    back.Visibility = Visibility.Collapsed;
                    mPhase--;
                    break;
            }
        }
    }
}
