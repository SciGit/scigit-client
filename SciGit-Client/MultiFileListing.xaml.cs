using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SciGit_Client
{
  /// <summary>
  /// Interaction logic for MultiFileListing.xaml
  /// </summary>
  public partial class MultiFileListing : UserControl
  {
    public List<Action<int>> SelectionHandlers = new List<Action<int>>();
    private FileListing[] listings;

    public MultiFileListing() {
      InitializeComponent();

      listings = new FileListing[] {updatedListing, createdListing, deletedListing};
      for (int i = 0; i < listings.Length; i++) {
        int localI = i;
        listings[i].SelectionHandlers.Add(x => ItemSelected(localI, x));
      }
    }

    public void AddFile(int listing, string name) {
      listings[listing].AddFile(name);
    }

    public void Clear() {
      foreach (var listing in listings) {
        listing.Clear();
      }
    }

    public void Select(int index) {
      for (int i = 0; i < listings.Length; i++) {
        if (index < listings[i].GetSize()) {
          listings[i].Select(index);
          break;
        } else {
          index -= listings[i].GetSize();
        }
      }
    }

    private void ItemSelected(int listing, int index) {
      int sum = 0;
      for (int i = 0; i < listing; i++) {
        sum += listings[i].GetSize();
      }
      foreach (var handler in SelectionHandlers) {
        handler(sum + index);
      }
      for (int i = 0; i < listings.Length; i++) {
        if (i != listing) {
          listings[i].ClearSelection();
        }
      }
    }
  }
}
