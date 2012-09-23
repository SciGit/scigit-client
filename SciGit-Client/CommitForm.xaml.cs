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
using SciGit_Filter;

namespace SciGit_Client
{
  /// <summary>
  /// Interaction logic for CommitForm.xaml
  /// </summary>
  public partial class CommitForm : Window
  {
    Project project;
    public string savedMessage = null;

    public CommitForm(Project p) {
      InitializeComponent();
      project = p;
      GetChanges();
    }

    private void GetChanges() {
      string dir = ProjectMonitor.GetProjectDirectory(project);
      string status = GitWrapper.Status(dir, "-uno").Stdout;
      string changeText = "";
      bool isFilename = false;
      foreach (var line in status.Split(new char[] {'\0'}, StringSplitOptions.RemoveEmptyEntries)) {
        if (isFilename) {
          isFilename = false;
          changeText += line + ")\r\n";
          continue;
        }

        string filename = line.Substring(3);
        string mode = line.Substring(0, 2).Trim();
        if (mode == "M") {
          mode = "modified";
        } else if (mode == "A") {
          mode = "added";
        } else if (mode == "D") {
          mode = "deleted";
        } else if (mode == "R") {
          mode = "renamed";
          isFilename = true;
        }

        changeText += mode + ": " + filename;
        if (mode == "renamed") {
          changeText += " (originally ";
        } else {
          changeText += "\r\n";
        }
      }

      changes.Text = changeText;
    }

    private void ClickUpload(object sender, RoutedEventArgs e) {
      savedMessage = message.Text;
      Close();
    }

    private void ClickCancel(object sender, RoutedEventArgs e) {
      Close();
    }

    private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e) {
      if (savedMessage == null) {
        var res = MessageBox.Show(this, "This will cancel the upload process. Are you sure?", "Confirm cancel", MessageBoxButton.YesNo);
        if (res == MessageBoxResult.No) {
          e.Cancel = true;
        }
      }
    }
  }
}
