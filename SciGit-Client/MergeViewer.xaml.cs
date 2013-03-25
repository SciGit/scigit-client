using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace SciGit_Client
{
  /// <summary>
  /// Interaction logic for DiffViewer.xaml
  /// </summary>
  public partial class MergeViewer : UserControl
  {
    protected Project project;
    protected string gitFilename;
    protected string dir, fullpath, newFullpath;
    protected bool manual;
    protected int selectedSide = -1, deletedSide = -1;
    
    public MergeViewer(Project p, string filename, string original, string myVersion, string newVersion) {
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
      if (newFullpath != null && File.Exists(newFullpath)) {
        File.Delete(newFullpath);
      }
    }

    public virtual bool Finished() {
      return false;
    }

    public virtual string GetMergeResult() {
      return null;
    }

    protected void CreateFiles(string filename, string myVersion, string newVersion) {
      messageMe.Visibility = Visibility.Visible;
      messageNew.Visibility = Visibility.Visible;
      string projectDir;
      projectDir = ProjectMonitor.GetProjectDirectory(project);
      
      string winFilename = gitFilename.Replace('/', System.IO.Path.DirectorySeparatorChar);
      fullpath = Util.PathCombine(projectDir, winFilename);
      dir = System.IO.Path.GetDirectoryName(fullpath);
      string name = System.IO.Path.GetFileName(fullpath);
      if (myVersion != null) {
        CreateMessage(ref messageMe, name, fullpath, "your");
        File.WriteAllText(fullpath, myVersion, Encoding.Default);
        acceptMe.Content = "Accept " + filename;
      } else {
        messageMe.Text = "You deleted this file.";
        acceptMe.Content = "Accept deletion";
      }
      // Copy updated text into a new, temporary file.
      string newFilename = System.IO.Path.GetFileNameWithoutExtension(name) + ".sciGitUpdated" +
          System.IO.Path.GetExtension(filename);
      newFullpath = Util.PathCombine(dir, newFilename);
      if (newVersion != null) {
        CreateMessage(ref messageNew, newFilename, newFullpath, "the updated");
        File.WriteAllText(newFullpath, newVersion, Encoding.Default);
        acceptThem.Content = "Accept " + newFilename;
      } else {
        messageMe.Text = "This file was deleted in the updated version.";
        acceptThem.Content = "Accept deletion";
      }
    }

    protected void CreateMessage(ref TextBlock text, string filename, string path, string pronoun) {
      string ext = System.IO.Path.GetExtension(filename);
      text.Inlines.Clear();
      if (manual) {
        text.Inlines.Add("Manually merging files. ");
      } else {
        text.Inlines.Add("This is a ");
        text.Inlines.Add(ext == ".doc" || ext == ".docx" ? "Word document" : "binary file");
        text.Inlines.Add(". ");
      }
      text.Inlines.Add("Please open the file ");
      var fakeUri = new Uri("http://asdf.com");
      var link = new Hyperlink(new Run(filename)) { NavigateUri = fakeUri, TargetName = path };
      link.RequestNavigate += OpenFile;
      text.Inlines.Add(link);
      text.Inlines.Add(" to edit " + pronoun + " version");
      if (ext == ".doc" || ext == ".docx") {
        text.Inlines.Add(" or ");
        link = new Hyperlink(new Run("merge")) {
          NavigateUri = fakeUri,
          TargetName = System.IO.Path.GetFileNameWithoutExtension(path)
        };
        link.RequestNavigate += MergeInWord;
        text.Inlines.Add(link);
        text.Inlines.Add(" the files in Word and save the result in the desired file.");
      } else {
        text.Inlines.Add(".");
      }
    }

    protected virtual void OpenFile(object sender, RequestNavigateEventArgs e) {
      Process.Start(e.Target);
    }

    protected virtual void MergeInWord(object sender, RequestNavigateEventArgs e) {
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

    protected virtual void ClickEditMe(object sender, RoutedEventArgs e) {
    }

    protected virtual void ClickEditThem(object sender, RoutedEventArgs e) {
    }

    protected virtual void ClickRevertMe(object sender, RoutedEventArgs e) {
    }

    protected virtual void ClickRevertThem(object sender, RoutedEventArgs e) {
    }

    protected virtual void ClickManualMerge(object sender, RoutedEventArgs e) {
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e) {

    }
  }
}
