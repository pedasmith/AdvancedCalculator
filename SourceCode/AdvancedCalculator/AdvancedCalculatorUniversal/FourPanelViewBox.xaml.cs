using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace AdvancedCalculator
{
    public sealed partial class FourPanelViewbox : UserControl
    {
        public FrameworkElement ItemTop { get; set; }
        public FrameworkElement ItemMain { get; set; }
        public FrameworkElement ItemRight { get; set; }
        public FrameworkElement ItemBottom { get; set; }
        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get { return ScrollBarVisibility.Hidden; }
            set {  }
        }
        public void Initialize()
        {
            if (uiGrid.Children.Count == 1) // 1 for the scrollviewer;
            {
                if (ItemTop != null) uiGrid.Children.Add(ItemTop);

                if (ItemMain != null) uiScrollGrid.Children.Add(ItemMain);
                if (ItemRight != null) uiScrollGrid.Children.Add(ItemRight);
                if (ItemBottom != null) uiScrollGrid.Children.Add(ItemBottom);
            }
            SetOrientation();
        }
        public FourPanelViewbox()
        {
            this.InitializeComponent();
            SetOrientation();
            Window.Current.SizeChanged += (s, e) => { SetOrientation(); };
        }

        enum Orientation { NotSet, Portrait, Landscape };
        private Orientation CurrOrientation = Orientation.NotSet;

        private void SetOrientation()
        {
            var size = Window.Current.Bounds;
            var orientation = (size.Height >= size.Width) ? Orientation.Portrait : Orientation.Landscape;
            SetOrientation(orientation);
        }

        private void SetOrientation(Orientation orientation)
        {
            Grid grid = uiGrid;
            if (ItemMain == null) return; // nothing is set!
            //NOTE: re-enable? if (orientation == CurrOrientation) return; // already correct.

            CurrOrientation = orientation;
            //var requestedColumn = 2;


            switch (orientation)
            {
                case Orientation.Portrait: SetOrientationPortrait(grid); break;
                case Orientation.Landscape: SetOrientationLandscape(grid); break;
            }
        }
        private void SetOrientationPortrait(Grid grid)
        {
            SetPosition(ItemTop, 0, 0, 1);

            // uiScrollGrid
            SetPosition(ItemMain, 1, 0, 1);
            SetPosition(ItemRight, 2, 0, 1);
            SetPosition(ItemBottom, 3, 0, 1);
        }



        private void SetOrientationLandscape(Grid grid)
        {
            SetPosition(ItemTop, 0, 0, 1);

            // uiScrollGrid
            SetPosition(ItemMain, 1, 0, 1);
            SetPosition(ItemRight, 1, 1, 1);
            SetPosition(ItemBottom, 2, 0, 2);
        }

        void SetPosition(FrameworkElement element, int row, int col, int colSpan)
        {
            if (element != null)
            {
                Grid.SetRow(element, row);
                Grid.SetColumn(element, col);
                Grid.SetColumnSpan(element, colSpan);
            }
        }
    }
}
