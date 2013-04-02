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

namespace SciGit_Client
{
  public class BinaryMergeViewer : MergeViewer
  {
    public BinaryMergeViewer(Project p, string filename, string original, string myVersion, string newVersion)
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
      return Util.ReadFile(selectedSide == 0 ? fullpath : newFullpath);
    }

    protected override void Accept(int side) {
      selectedSide = side;
      acceptMe.IsChecked = side == 0;
      acceptThem.IsChecked = side == 1;
      status.Text = "0 unresolved conflicts remaining.";
    }

    protected override void MergeInWord(object sender, RequestNavigateEventArgs e) {
      Util.CompareInWord(fullpath, newFullpath, e.Target, dir, "Updated Version", true);
    }
  }
}
