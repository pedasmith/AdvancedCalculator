using System;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace AdvancedCalculator
{
    public sealed partial class AsciiTableControl : UserControl, IInitializeCalculator
    {
        public AsciiTableControl()
        {
            this.InitializeComponent();
        }
        public void Initialize(SimpleCalculator simpleCalculator)
        {
            uiMain.Initialize();
            InitializeAsciiTable();
        }
        static bool AsciiTableIsInitialized = false;
        private void InitializeAsciiTable()
        {
            if (AsciiTableIsInitialized) return;
            AsciiTableIsInitialized = true;

            string special = "±≥α∞\u263a\u2603\u27f2\u2b24\u30e9\u20ac";

            var grid = uiConversionsAsciiTableGrid;
            if (grid == null) grid = uiMain.ItemMain as Grid;
            const int NROW = 16;
            for (int value = 0; value < 128; value++)
            {
                var row = value % NROW;
                var col = value / NROW;
                char ch = Convert.ToChar(value);
                var entry = new AsciiEntryControl(ch);
                grid.Children.Add(entry);
                Grid.SetColumn(entry, col);
                Grid.SetRow(entry, row);
            }

            for (int i = 0; i < special.Length; i++)
            {
                var col = 9;
                var row = i;
                char ch = special[i];
                var entry = new AsciiEntryControl(ch);
                grid.Children.Add(entry);
                Grid.SetColumn(entry, col);
                Grid.SetRow(entry, row);
            }
        }
    }
}
