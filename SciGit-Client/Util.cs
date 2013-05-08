#pragma warning disable 0467 // disable interop method ambiguity warnings

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Office.Interop.Word;
using MessageBox = System.Windows.MessageBox;
using MessageBoxOptions = System.Windows.MessageBoxOptions;
using Window = System.Windows.Window;

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

    public static string GetTempPath() {
      return PathCombine(Path.GetTempPath(), "SciGit");
    }

    public static string[] ArraySlice(string[] a, int start, int length) {
      length = Math.Min(length, a.Length - start);
      var ret = new string[length];
      for (int i = 0; i < length; i++) {
        ret[i] = a[start + i];
      }
      return ret;
    }

    // Shows the message box on top.
    public static DialogResult ShowMessageBox(string content, string title, MessageBoxButtons buttons = MessageBoxButtons.OK) {
      return System.Windows.Forms.MessageBox.Show(content, title, buttons, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.DefaultDesktopOnly);
    }

    public static void CompareInWord(string fullpath, string newFullpath, string saveName, string saveDir, string author, bool save = false) {
      Object missing = Type.Missing;
      try {
        var wordapp = new Microsoft.Office.Interop.Word.Application();
        try {
          var doc = wordapp.Documents.Open(fullpath, ReadOnly: true);
          doc.Compare(newFullpath, author ?? missing);
          doc.Close(WdSaveOptions.wdDoNotSaveChanges); // Close the original document
          var dialog = wordapp.Dialogs[WdWordDialog.wdDialogFileSummaryInfo];
          // Pre-set the save destination by setting the Title in the save dialog.
          // This must be done through reflection, since "dynamic" is only supported in .NET 4
          dialog.GetType().InvokeMember("Title", BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty,
                                        null, dialog, new object[] {saveName});
          dialog.Execute();
          wordapp.ChangeFileOpenDirectory(saveDir);
          if (!save) {
            wordapp.ActiveDocument.Saved = true;
          }

          wordapp.Visible = true;
          wordapp.Activate();

          // Simple hack to bring the window to the front.
          wordapp.ActiveWindow.WindowState = WdWindowState.wdWindowStateMinimize;
          wordapp.ActiveWindow.WindowState = WdWindowState.wdWindowStateMaximize;
        } catch (Exception ex) {
          Logger.LogException(ex);
          ShowMessageBox("Word could not open these documents. Please edit the file manually.", "Error");
          wordapp.Quit();
        }
      } catch (Exception ex) {
        Logger.LogException(ex);
        ShowMessageBox("Could not start Microsoft Word. Office 2003 or higher is required.", "Could not start Word");
      }
    }

    // Read the file, accounting for cases where the file doesn't exist or the file is in use.
    public static string ReadFile(string filename) {
      while (true) {
        try {
          var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
          var sr = new StreamReader(fs, Encoding.Default);
          return sr.ReadToEnd();
        } catch (Exception ex) {
          Logger.LogException(ex);
          return null;
        }
      }
    }

    public static void HandleException(Exception ex) {
      Logger.LogException(ex);
      new ErrorForm(ex).ShowDialog();
      Environment.Exit(1);
    }
  }
}
