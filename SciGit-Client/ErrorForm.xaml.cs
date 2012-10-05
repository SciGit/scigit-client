using System.Windows;
using System;

namespace SciGit_Client
{
  /// <summary>
  /// Interaction logic for ErrorForm.xaml
  /// </summary>
  public partial class ErrorForm : Window
  {
    public ErrorForm(Exception e, bool canRetry = false) {
      InitializeComponent();

      while (e.InnerException != null) e = e.InnerException;
      errorDetails.Text = e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace;
      retry.Visibility = canRetry ? Visibility.Visible : Visibility.Collapsed;
    }

    public static void Show(Exception e, bool canRetry = false) {
      var form = new ErrorForm(e, canRetry);
      form.Show();
    }

    public static bool? ShowDialog(Window w, Exception e, bool canRetry = false) {
      var form = new ErrorForm(e, canRetry);
      return w.Dispatcher.Invoke(new Func<bool?>(form.ShowDialog)) as bool?;
    }

    public static void FatalError(Window w, Exception e, bool canRetry = false) {
      ShowDialog(w, e, canRetry);
      Environment.Exit(1);
    }

    private void ClickReport(object sender, EventArgs e) {
      
    }

    private void ClickRetry(object sender, EventArgs e) {

    }

    private void ClickClose(object sender, EventArgs e) {
      Close();
    }
  }
}
