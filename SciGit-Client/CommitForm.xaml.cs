using System;
using System.ComponentModel;
using System.Windows;

namespace SciGit_Client
{
  /// <summary>
  /// Interaction logic for CommitForm.xaml
  /// </summary>
  public partial class CommitForm : Window
  {
    Project project;
    public string savedMessage;

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
      foreach (var line in status.Split(new[] {'\0'}, StringSplitOptions.RemoveEmptyEntries)) {
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
      if (message.Text.Trim() == "") {
        MessageBox.Show("You must provide a message.", "Error");
        return;
      }
      savedMessage = message.Text;
      Close();
    }

    private void ClickCancel(object sender, RoutedEventArgs e) {
      Close();
    }

    private void WindowClosing(object sender, CancelEventArgs e) {
      if (savedMessage == null) {
        var res = MessageBox.Show(this, "This will cancel the upload process. Are you sure?", "Confirm cancel", MessageBoxButton.YesNo);
        if (res == MessageBoxResult.No) {
          e.Cancel = true;
        }
      }
    }
  }
}
