using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using SciGit_Client.Properties;
using MessageBox = System.Windows.MessageBox;

namespace SciGit_Client
{
  enum NotifyFlags
  {
    NotifyUpdate = 1,
    NotifyAddDelete = 2,
    NotifyUpload = 4
  }

  /// <summary>
  /// Interaction logic for SettingsForm.xaml
  /// </summary>
  public partial class SettingsForm : Window
  {
    private string projectFolder;

    public SettingsForm() {
      InitializeComponent();
      Style = (Style)FindResource(typeof(Window));

      Assembly assembly = Assembly.GetExecutingAssembly();
      version.Text = "SciGit v" + FileVersionInfo.GetVersionInfo(assembly.Location).ProductVersion;

      autoSave.IsChecked = Settings.Default.AutoSave;
      autoUpdate.IsChecked = Settings.Default.AutoUpdate;

      int notifyMask = Settings.Default.NotifyMask;
      projectFolder = Settings.Default.ProjectDirectory;
      notifyUpdate.IsChecked = (notifyMask & (int)NotifyFlags.NotifyUpdate) != 0;
      notifyAddDelete.IsChecked = (notifyMask & (int)NotifyFlags.NotifyAddDelete) != 0;
      notifyUpload.IsChecked = (notifyMask & (int)NotifyFlags.NotifyUpload) != 0;
      if (String.IsNullOrEmpty(projectFolder)) {
        folder.Text = projectFolder = ProjectMonitor.GetProjectDirectory();
      } else {
        folder.Text = projectFolder;
      }

      RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
      startup.IsChecked = rk.GetValue("SciGit") != null;
    }

    private void ClickChooseFolder(object sender, EventArgs e) {
      var dialog = new FolderBrowserDialog();
      dialog.Description = "Select a folder for your projects. Projects will be placed under a SciGit folder at the destination.";
      dialog.ShowNewFolderButton = true;
      DialogResult result = dialog.ShowDialog();
      if (result == System.Windows.Forms.DialogResult.OK) {
        folder.Text = Util.PathCombine(dialog.SelectedPath, "SciGit");
      }
    }

    private void ClickManageProjects(object sender, EventArgs e) {
      Process.Start("http://" + App.Hostname + "/projects");
    }

    private void ClickManageAccount(object sender, EventArgs e) {
      Process.Start("http://" + App.Hostname + "/users/profile");
    }

    private void ClickOK(object sender, EventArgs e) {
      int newNotifyMask = 0;

      Settings.Default.AutoSave = autoSave.IsChecked == true;
      Settings.Default.AutoUpdate = autoUpdate.IsChecked == true;

      if (notifyUpdate.IsChecked ?? false) newNotifyMask |= (int)NotifyFlags.NotifyUpdate;
      if (notifyAddDelete.IsChecked ?? false) newNotifyMask |= (int)NotifyFlags.NotifyAddDelete;
      if (notifyUpload.IsChecked ?? false) newNotifyMask |= (int)NotifyFlags.NotifyUpload;
      if (folder.Text != projectFolder) {
        var result = MessageBox.Show(this, "Would you like to move your existing projects over?", "Move Projects", MessageBoxButton.YesNoCancel);
        try {
          if (!Directory.Exists(folder.Text)) {
            Directory.CreateDirectory(folder.Text);
          }
          if (result == MessageBoxResult.Yes) {
            FileSystem.MoveDirectory(projectFolder, folder.Text, UIOption.AllDialogs);
          } else if (result == MessageBoxResult.Cancel) {
            return;
          }
        } catch (Exception ex) {
          Logger.LogException(ex);
          MessageBox.Show("Error creating project directory.");
          return;
        }
        Settings.Default.ProjectDirectory = projectFolder = folder.Text;
      }
      Settings.Default.NotifyMask = newNotifyMask;
      Settings.Default.Save();

      RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
      if (startup.IsChecked == true) {
        rk.SetValue("SciGit", '"' + System.Windows.Forms.Application.ExecutablePath + "\" -autologin");
      } else {
        rk.DeleteValue("SciGit", false);
      }

      Close();
    }

    private void ClickCancel(object sender, EventArgs e) {
      Close();
    }

    private void AutoUpdateChecked(object sender, RoutedEventArgs e) {
      var res = MessageBox.Show("Warning: in certain text editors, this may cause your files to be updated while you have unsaved changes pending, "
        + "potentially causing loss of data. Are you sure you want to enable this feature?\n\n" +
          "This is not an issue if you are working with Word documents.", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
      if (res == MessageBoxResult.No) {
        autoUpdate.IsChecked = false;
      }
    }
  }
}
