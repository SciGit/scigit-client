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
using System.Diagnostics;

namespace SciGit_Client
{
    /// <summary>
    /// Interaction logic for test.xaml
    /// </summary>
    public partial class SGLogin : UserControl
    {
        public SGLogin()
        {
            InitializeComponent();
            if (Properties.Settings.Default.RememberUser)
            {
                rememberMe.IsChecked = true;
                emailValue.Text = Properties.Settings.Default.SavedUsername;
                passwordValue.Password = Properties.Settings.Default.SavedPassword;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.AbsoluteUri);
            e.Handled = true;
        }
    }
}
