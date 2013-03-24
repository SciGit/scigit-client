using System;
using System.IO;
using System.Reflection;
using System.Windows;
using Microsoft.Office.Interop.Word;

namespace SciGit_Client
{
  class Util
  {
    public static string PathCombine(params string[] args) {
      if (args.Length == 0) return "";
      if (args.Length == 1) return args[0];
      string s = Path.Combine(args[0], args[1]);
      for (int i = 2; i < args.Length; i++) {
        s = Path.Combine(s, args[i]);
      }
      return s;
    }

    public static string[] ArraySlice(string[] a, int start, int length) {
      length = Math.Min(length, a.Length - start);
      var ret = new string[length];
      for (int i = 0; i < length; i++) {
        ret[i] = a[start + i];
      }
      return ret;
    }

    public static void CompareInWord(string fullpath, string newFullpath, string saveName, string saveDir, string author = null) {
      Object missing = Type.Missing;
      var wordapp = new Microsoft.Office.Interop.Word.Application();
      try {
        var doc = wordapp.Documents.Open(fullpath, ReadOnly: true);
        doc.Compare(newFullpath, author ?? missing);
        doc.Close(WdSaveOptions.wdDoNotSaveChanges); // Close the original document
        var dialog = wordapp.Dialogs[WdWordDialog.wdDialogFileSummaryInfo];
        // Pre-set the save destination by setting the Title in the save dialog.
        // This must be done through reflection, since "dynamic" is only supported in .NET 4
        dialog.GetType().InvokeMember("Title", BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty,
            null, dialog, new object[] { saveName });
        dialog.Execute();
        wordapp.ChangeFileOpenDirectory(saveDir);
        wordapp.Visible = true;
        wordapp.Activate();
      } catch (Exception ex) {
        Logger.LogException(ex);
        MessageBox.Show("Word could not open these documents. Please edit the file manually.", "Error");
        wordapp.Quit();
      }
    }
  }
}
