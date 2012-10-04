using System.Windows;
using System;

namespace SciGit_Client
{
  /// <summary>
  /// Interaction logic for ErrorForm.xaml
  /// </summary>
  public partial class ErrorForm : Window
  {
    public ErrorForm(string errorText, bool canRetry = false) {
      InitializeComponent();

      errorDetails.Text = errorText;
      retry.Visibility = canRetry ? Visibility.Visible : Visibility.Collapsed;
    }

    public static bool? ShowDialog(Window w, string errorText, bool canRetry = false) {
      var form = new ErrorForm(errorText, canRetry);
      return w.Dispatcher.Invoke(new Func<bool?>(form.ShowDialog)) as bool?;
    }

    public static void FatalError(Window w, string errorText, bool canRetry = false) {
      ShowDialog(w, errorText, canRetry);
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
