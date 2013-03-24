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
    public BinaryDiffViewer(Project p, string filename, string original, string myVersion, string newVersion)
        : base(p, filename, original, myVersion, newVersion) {
      editMe.Visibility = editThem.Visibility = Visibility.Collapsed;
      revertMe.Visibility = revertThem.Visibility = Visibility.Collapsed;
      manualMerge.Visibility = Visibility.Collapsed;
      grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      Grid.SetRow(actionsMe, 1);
      Grid.SetRow(actionsThem, 1);

      CreateFiles(filename, myVersion, newVersion);
      status.Text = "1 unresolved conflicts remaining.";
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

    protected override void MergeInWord(object sender, RequestNavigateEventArgs e) {
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
