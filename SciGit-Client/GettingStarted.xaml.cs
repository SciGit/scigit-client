using System;
using System.Collections.Generic;
using System.Diagnostics;
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
  /// Interaction logic for GettingStarted.xaml
  /// </summary>
  public partial class GettingStarted : Window
  {
    public GettingStarted() {
      InitializeComponent();
    }

    private void ClickDone(object sender, EventArgs e) {
      Close();
    }

    private void IconClick(object sender, MouseButtonEventArgs e) {
      Process.Start(ProjectMonitor.GetProjectDirectory());
    }

    private void HyperlinkNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e) {
      Process.Start(e.Uri.AbsoluteUri);
      e.Handled = true;
    }
  }
}
