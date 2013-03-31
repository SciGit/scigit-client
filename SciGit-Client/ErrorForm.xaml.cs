using System.Windows;
using System;
using System.Diagnostics;
using SciGit_Client.Properties;

namespace SciGit_Client
{
  /// <summary>
  /// Interaction logic for ErrorForm.xaml
  /// </summary>
  public partial class ErrorForm : Window
  {
    public ErrorForm(Exception e) {
      InitializeComponent();
      Style = (Style)FindResource(typeof(Window));

      while (e.InnerException != null) e = e.InnerException;
      errorDetails.Text = e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace;
    }

    public static void Show(Exception e) {
      var form = new ErrorForm(e);
      form.Show();
    }

    public static bool? ShowDialog(Window w, Exception e) {
      var form = new ErrorForm(e);
      return w.Dispatcher.Invoke(new Func<bool?>(form.ShowDialog)) as bool?;
    }

    private void ClickReport(object sender, EventArgs e) {
      string email = (string)Settings.Default["SciGitEmail"];
      string content = errorDetails.Text;
      Process.Start(String.Format("mailto:{0}?subject={1}&body={2}",
        email, Uri.EscapeDataString("Error report"), Uri.EscapeDataString(content)));
      Close();
    }

    private void ClickClose(object sender, EventArgs e) {
      Close();
    }
  }
}
