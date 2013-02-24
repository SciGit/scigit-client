﻿using System.Windows;
using System.Windows.Controls;

namespace SciGit_Client
{
  /// <summary>
  /// Interaction logic for DiffViewer.xaml
  /// </summary>
  public partial class DiffViewer : UserControl
  {
    protected Project project;
    protected string gitFilename;
    protected int deletedSide = -1;
    
    public DiffViewer(Project p, string filename, string original, string myVersion, string newVersion) {
      InitializeComponent();

      gitFilename = filename;
      project = p;

      if (myVersion == null) {
        titleMe.Text += " (file was deleted)";
        deletedSide = 0;
      } else if (original == null) {
        titleMe.Text += " (file was added)";
      } else {
        titleMe.Text += " (file was modified)";
      }
      
      if (newVersion == null) {
        titleNew.Text += " (file was deleted)";
        deletedSide = 1;
        // Only one side can be deleted in a conflict, so deletedSide is either 0 or 1
      } else if (original == null) {
        titleNew.Text += " (file was added)";
      } else {
        titleNew.Text += " (file was modified)";
      }
    }

    public virtual void Cleanup() {
    }

    public virtual bool Finished() {
      return false;
    }

    public virtual string GetMergeResult() {
      return null;
    }

    protected virtual void Accept(int side) {
    }

    protected virtual void SelectPreviousConflict(object sender, RoutedEventArgs e) {
    }

    protected virtual void SelectNextConflict(object sender, RoutedEventArgs e) {
    }

    protected virtual void ClickAcceptMe(object sender, RoutedEventArgs e) {
      Accept(0);
    }

    protected virtual void ClickAcceptThem(object sender, RoutedEventArgs e) {
      Accept(1);
    }

    protected virtual void ClickEdit(object sender, RoutedEventArgs e) {
    }

    protected virtual void ClickRevert(object sender, RoutedEventArgs e) {
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e) {

    }
  }
}
