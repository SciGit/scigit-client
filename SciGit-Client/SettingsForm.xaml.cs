﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

      int notifyMask = Settings.Default.NotifyMask;
      projectFolder = Settings.Default.ProjectFolder;
      notifyUpdate.IsChecked = (notifyMask & (int)NotifyFlags.NotifyUpdate) != 0;
      notifyAddDelete.IsChecked = (notifyMask & (int)NotifyFlags.NotifyAddDelete) != 0;
      notifyUpload.IsChecked = (notifyMask & (int)NotifyFlags.NotifyUpload) != 0;
      if (String.IsNullOrEmpty(projectFolder)) {
        folder.Text = projectFolder = ProjectMonitor.GetProjectDirectory();
      } else {
        folder.Text = projectFolder;
      }
    }

    private void ClickChooseFolder(object sender, EventArgs e) {
      var dialog = new FolderBrowserDialog();
      dialog.Description = "Select a folder for your SciGit projects. Projects will appear as subdirectories.";
      dialog.ShowNewFolderButton = true;
      DialogResult result = dialog.ShowDialog();
      if (result == System.Windows.Forms.DialogResult.OK) {
        folder.Text = dialog.SelectedPath;
      }
    }

    private void ClickManageProjects(object sender, EventArgs e) {
      Process.Start("http://" + RestClient.ServerHost + "/projects");
    }

    private void ClickManageAccount(object sender, EventArgs e) {
      Process.Start("http://" + RestClient.ServerHost + "/users/profile");
    }

    private void ClickOK(object sender, EventArgs e) {
      int newNotifyMask = 0;
      if (notifyUpdate.IsChecked ?? false) newNotifyMask |= (int)NotifyFlags.NotifyUpdate;
      if (notifyAddDelete.IsChecked ?? false) newNotifyMask |= (int)NotifyFlags.NotifyAddDelete;
      if (notifyUpload.IsChecked ?? false) newNotifyMask |= (int)NotifyFlags.NotifyUpload;
      if (folder.Text != projectFolder) {
        projectFolder = folder.Text;
        try {
          if (!Directory.Exists(projectFolder)) {
            var directoryInfo = Directory.CreateDirectory(projectFolder);
          }
        } catch (Exception) {
          MessageBox.Show(this, "Invalid project directory.", "Error");
          return;
        }
        Settings.Default.ProjectFolder = projectFolder;
      }
      Settings.Default.NotifyMask = newNotifyMask;
      Settings.Default.Save();

      Close();
    }

    private void ClickCancel(object sender, EventArgs e) {
      Close();
    }
  }
}
