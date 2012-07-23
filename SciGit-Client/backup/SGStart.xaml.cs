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
        public SGStart()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SGWelcome sgWelcome = new SGWelcome();
            Canvas.SetLeft(sgWelcome, 0);
            Canvas.SetTop(sgWelcome, 0);
        }

        private void SGStart_continue_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}
