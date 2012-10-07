using System.Windows;
using System;

namespace SciGit_Client
{
  /// <summary>
  /// Interaction logic for ErrorForm.xaml
  /// </summary>
  public partial class ErrorForm : Window
  {
    public ErrorForm(Exception e) {
      InitializeComponent();

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

    public static void FatalError(Window w, Exception e) {
      ShowDialog(w, e);
      Environment.Exit(1);
    }

    private void ClickReport(object sender, EventArgs e) {
      
    }

    private void ClickClose(object sender, EventArgs e) {
      Close();
    }
  }
}
