using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using Microsoft.Office.Interop.Word;
using Hyperlink = System.Windows.Documents.Hyperlink;

namespace SciGit_Client
{
  public class BinaryDiffViewer : DiffViewer
  {
    protected string dir, fullpath, newFullpath;
    protected int selectedSide = -1;

    public BinaryDiffViewer(Project p, string filename, string original, string myVersion, string newVersion)
        : base(p, filename, original, myVersion, newVersion) {
      editMe.Visibility = editThem.Visibility = Visibility.Collapsed;
      revertMe.Visibility = revertThem.Visibility = Visibility.Collapsed;
      grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      Grid.SetRow(actionsMe, 1);
      Grid.SetRow(actionsThem, 1);

      messageMe.Visibility = Visibility.Visible;
      messageNew.Visibility = Visibility.Visible;
      string projectDir;
      if (project.id == 0) { // for testing purposes only
        projectDir = "C:\\temp";
      } else {
        projectDir = ProjectMonitor.GetProjectDirectory(project);
      }
      string winFilename = gitFilename.Replace('/', System.IO.Path.DirectorySeparatorChar);
      fullpath = Util.PathCombine(projectDir, winFilename);
      dir = System.IO.Path.GetDirectoryName(fullpath);
      string name = System.IO.Path.GetFileName(fullpath);
      if (myVersion != null) {
        CreateMessage(ref messageMe, name, fullpath, "your");
        File.WriteAllText(fullpath, myVersion, Encoding.Default);
      } else {
        messageMe.Text = "You deleted this file.";
      }
      // Copy updated text into a new, temporary file.
      string newFilename = System.IO.Path.GetFileNameWithoutExtension(name) + ".sciGitUpdated" +
          System.IO.Path.GetExtension(filename);
      newFullpath = Util.PathCombine(dir, newFilename);
      if (newVersion != null) {
        CreateMessage(ref messageNew, newFilename, newFullpath, "the updated");
        File.WriteAllText(newFullpath, newVersion, Encoding.Default);
      } else {
        messageMe.Text = "This file was deleted in the updated version.";
      }

      acceptMe.Content = "Accept " + filename;
      acceptThem.Content = "Accept " + newFilename;
      status.Text = "1 unresolved conflicts remaining.";
    }

    public override void Cleanup() {
      if (File.Exists(newFullpath)) {
        File.Delete(newFullpath);
      }
    }

    public override bool Finished() {
      return selectedSide != -1;
    }

    public override string GetMergeResult() {
      if (!Finished()) return null;

      if (selectedSide == deletedSide) return null;
      return File.ReadAllText(selectedSide == 0 ? fullpath : newFullpath, Encoding.Default);
    }

    protected override void Accept(int side) {
      selectedSide = side;
      acceptMe.IsChecked = side == 0;
      acceptThem.IsChecked = side == 1;
      status.Text = "0 unresolved conflicts remaining.";
    }

    private void CreateMessage(ref TextBlock text, string filename, string path, string pronoun) {
      string ext = System.IO.Path.GetExtension(filename);
      text.Inlines.Add("This is a ");
      text.Inlines.Add(ext == ".doc" || ext == ".docx" ? "Word document" : "binary file");
      text.Inlines.Add(". Please open the file ");
      var fakeUri = new Uri("http://asdf.com");
      var link = new Hyperlink(new Run(filename)) { NavigateUri = fakeUri, TargetName = path };
      link.RequestNavigate += OpenFile;
      text.Inlines.Add(link);
      text.Inlines.Add(" to edit " + pronoun + " version");
      if (ext == ".doc" || ext == ".docx") {
        text.Inlines.Add(" or ");
        link = new Hyperlink(new Run("merge")) {
          NavigateUri = fakeUri, TargetName = System.IO.Path.GetFileNameWithoutExtension(path)
        };
        link.RequestNavigate += MergeInWord;
        text.Inlines.Add(link);
        text.Inlines.Add(" the files in Word and save the result in the desired file.");
      } else {
        text.Inlines.Add(".");
      }
    }

    private void OpenFile(object sender, RequestNavigateEventArgs e) {
      Process.Start(e.Target);
    }

    private void MergeInWord(object sender, RequestNavigateEventArgs e) {
      Object missing = Type.Missing;
      var wordapp = new Microsoft.Office.Interop.Word.Application();
      try {
        var doc = wordapp.Documents.Open(fullpath, ReadOnly: true);
        doc.Compare(newFullpath);
        doc.Close(WdSaveOptions.wdDoNotSaveChanges); // Close the original document
        var dialog = wordapp.Dialogs[WdWordDialog.wdDialogFileSummaryInfo];
        // Pre-set the save destination by setting the Title in the save dialog.
        // This must be done through reflection, since "dynamic" is only supported in .NET 4
        dialog.GetType().InvokeMember("Title", BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty,
            null, dialog, new object[] {e.Target});
        dialog.Execute();
        wordapp.ChangeFileOpenDirectory(dir);
        wordapp.Visible = true;
        wordapp.Activate();
      } catch (Exception ex) {
        Logger.LogException(ex);
        MessageBox.Show("Word could not open these documents. Please edit each file manually.", "Error");
        wordapp.Quit();
      }
    }
  }
}
