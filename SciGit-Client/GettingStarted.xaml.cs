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
    private List<Panel> panels;
    private int curPanel;

    public GettingStarted() {
      InitializeComponent();
      Style = (Style)FindResource(typeof(Window));

      linkDirectory.Inlines.Add(new Run(ProjectMonitor.GetProjectDirectory()));

      panels = new List<Panel>();
      foreach (var elem in grid.Children) {
        if (elem is Panel) {
          var sp = elem as Panel;
          if (sp.Name.StartsWith("step")) {
            sp.Visibility = Visibility.Collapsed;
            panels.Add(sp);
          }
        }
      }

      curPanel = 0;
      SelectPanel(curPanel);
    }

    private void SelectPanel(int panel) {
      if (panel < 0 || panel >= panels.Count) return;

      panels[curPanel].Visibility = Visibility.Collapsed;
      panels[panel].Visibility = Visibility.Visible;
      curPanel = panel;

      prev.IsEnabled = (panel > 0);
      if (panel+1 == panels.Count) {
        next.Content = "Done";
      } else {
        next.Content = "Next >";
      }

      status.Text = String.Format("Step {0}/{1}", panel + 1, panels.Count);
    }

    private void ClickDirectory(object sender, EventArgs e) {
      Process.Start(ProjectMonitor.GetProjectDirectory());
    }

    private void ClickManageProjects(object sender, EventArgs e) {
      Process.Start("http://" + RestClient.ServerHost + "/projects");
    }

    private void ClickManageAccount(object sender, EventArgs e) {
      Process.Start("http://" + RestClient.ServerHost + "/users/profile");
    }

    private void ClickNext(object sender, EventArgs e) {
      if (curPanel + 1 == panels.Count) {
        Close();
      }
      SelectPanel(curPanel + 1);
    }

    private void ClickPrevious(object sender, EventArgs e) {
      SelectPanel(curPanel - 1);
    }

    private void ClickClose(object sender, EventArgs e) {
      Close();
    }

    private void HyperlinkNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e) {
      Process.Start(e.Uri.AbsoluteUri);
      e.Handled = true;
    }

    private void Button_Click(object sender, RoutedEventArgs e) {

    }
  }
}
