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
  /// Interaction logic for SettingsForm.xaml
  /// </summary>
  public partial class SettingsForm : Window
  {
    public SettingsForm() {
      InitializeComponent();
    }

    private void ClickChooseFolder(object sender, EventArgs e) {
    }

    private void ClickManageProjects(object sender, EventArgs e) {
      Process.Start("http://" + RestClient.ServerHost + "/projects");
    }

    private void ClickManageAccount(object sender, EventArgs e) {
      Process.Start("http://" + RestClient.ServerHost + "/users/profile");
    }

    private void ClickOK(object sender, EventArgs e) {

    }

    private void ClickCancel(object sender, EventArgs e) {
      Close();
    }
  }
}
