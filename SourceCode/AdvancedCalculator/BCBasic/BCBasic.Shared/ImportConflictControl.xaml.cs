using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace BCBasic
{
    public sealed partial class ImportConflictControl : UserControl
    {
        public enum ConflictResolution { Cancel, RenameOriginalImportNew, MergePreferOriginal, ImportNew, MergePreferNew };
        public class DismissArgs
        {
            public DismissArgs(ConflictResolution conflictResolution)
            {
                ConflictResolution = conflictResolution;
            }
            public ConflictResolution ConflictResolution;
        }
        public delegate void DismissHandler(object sender, DismissArgs e);

        public event DismissHandler OnDialogDismiss;

        public ImportConflictControl()
        {
            this.InitializeComponent();
        }
        public void DiffPackage(BCPackage a, BCPackage b)
        {
            ImportConflictControl.DiffPackage(uiDiffGrid, a, b);
        }

        public static void DiffPackage(Grid g, BCPackage a, BCPackage b)
        {
            g.Children.Clear();
            g.RowDefinitions.Clear();

            CompareStrings(g, "Name", a.Name, b.Name);
            CompareStrings(g, "Description", a.Description, b.Description);
            CompareStrings(g, "Number of programs", a.Programs.Count.ToString(), a.Programs.Count.ToString());
            CompareStrings(g, "Filename", a.Filename, b.Filename);
        }

        static GridLength autoHeight = new GridLength(0, GridUnitType.Auto);
        private static void CompareStrings (Grid g, string title, string a, string b)
        {
            int r = g.RowDefinitions.Count;
            g.RowDefinitions.Add(new RowDefinition() { Height = autoHeight });
            if (a == b)
            {
                var text = String.Format("{0} is the same for both ({1})", title, a);
                var cell = new TextBlock() { Text = text, FontSize = 16 };
                g.Children.Add(cell);
                Grid.SetRow(cell, r);
                Grid.SetColumn (cell, 0);
                Grid.SetColumnSpan(cell, 2);
            }
            else
            {
                var text = String.Format("{0} is different\n{1}", title, a);
                var cell = new TextBlock() { Text = text, FontSize = 16 };
                g.Children.Add(cell);
                Grid.SetRow(cell, r);
                Grid.SetColumn(cell, 0);

                text = String.Format("{0}", b);
                cell = new TextBlock() { Text = text, FontSize = 16 };
                g.Children.Add(cell);
                Grid.SetRow(cell, r);
                Grid.SetColumn(cell, 1);
            }

            
        }

        private void DoDialogDismiss (ConflictResolution resolution)
        {
            if (OnDialogDismiss != null)
            {
                OnDialogDismiss(this, new DismissArgs(resolution));
            }
        }
        private ConflictResolution ConflictResolutionFromString(string str)
        {
            switch (str)
            {
                case "Cancel": return ConflictResolution.Cancel; 
                case "RenameOriginalImportNew": return ConflictResolution.RenameOriginalImportNew; 
                case "MergePreferOriginal": return ConflictResolution.MergePreferOriginal; 
                case "ImportNew": return ConflictResolution.ImportNew; 
                case "MergePreferNew": return ConflictResolution.MergePreferNew; 
            }
            return ConflictResolution.Cancel; // Not recognized?  Well, may as well do no harm.
        }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            ConflictResolution resolution = ConflictResolution.Cancel;
            foreach (var child in uiResolution.Children)
            {
                var rb = (child as RadioButton);
                if (rb != null)
                {
                    if (rb.IsChecked.HasValue && rb.IsChecked.Value)
                    {
                        resolution = ConflictResolutionFromString(rb.Tag as string);
                        break;
                    }
                }
            }
            DoDialogDismiss(resolution);
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DoDialogDismiss(ConflictResolution.Cancel);
        }

    }
}
