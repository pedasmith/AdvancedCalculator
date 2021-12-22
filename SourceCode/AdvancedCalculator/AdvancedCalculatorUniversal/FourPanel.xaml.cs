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
    public sealed partial class FourPanel : UserControl
    {
        public FrameworkElement ItemTop { get; set; }
        public FrameworkElement ItemMain { get; set; }
        public FrameworkElement ItemRight { get; set; }
        public FrameworkElement ItemBottom { get; set; }
        private ScrollBarVisibility ZZZVerticalScrollBarVisibility
        {
            get { return uiScrollViewer.VerticalScrollBarVisibility; }
            set { uiScrollViewer.VerticalScrollBarVisibility = value; }
        }
        public void Initialize()
        {
            if (uiGrid.Children.Count == 1) // 1 for the scrollviewer;
            {
                if (ItemTop != null) uiGrid.Children.Add(ItemTop);

                if (ItemMain != null) uiScrollGrid.Children.Add(ItemMain);
                if (ItemRight != null) uiScrollGrid.Children.Add(ItemRight);
                if (ItemBottom != null) uiScrollGrid.Children.Add(ItemBottom);
                SetWidthState();

            }
            SetOrientation();
        }

        enum WidthState {  Narrow, Regular };
        private void SetWidthState()
        {
            // Must force the states to be triggered based on width.
            var w = Window.Current.Bounds.Width;
            var ws = w >= 850 ? WidthState.Regular : WidthState.Narrow;
            foreach (var child in uiScrollGrid.Children)
            {
                SetWidthState(child as FrameworkElement, ws);
            }
            //var pstate = w >= 850 ? "FullWidth" : "NarrowWidth";
            //var groups = VisualStateManager.GetVisualStateGroups(this);
        }

        private void SetWidthState (FrameworkElement el, WidthState ws)
        {
            if (el == null) return;
            var tag = el.Tag as string;
            if (tag == null) tag = "";

            if (tag.StartsWith("AutoWidth|Column=0"))
            {
                var g = el as Grid;

                // switch between 140 and 200 wide on the first column.
                if (g != null && g.ColumnDefinitions.Count >= 1)
                {
                    var w = ws == WidthState.Narrow ? 140.0 : 200.0;
                    g.ColumnDefinitions[0].Width = new GridLength(w);
                }
            }
            else if (tag.StartsWith("AutoWidth|Text"))
            {
                var w = ws == WidthState.Narrow ? 300.0 : 600.0;
                el.MaxWidth = w;
            }
            else if (tag.StartsWith("AutoWidth|Feedback"))
            {
                var ww = Window.Current.Bounds.Width;
                var w = ws == WidthState.Narrow ? Math.Min (ww, 600) : 600.0;
                el.MaxWidth = w;
                el.Width = w;
            }
            else if (el is Panel)
            {
                var p = el as Panel;
                foreach (var child in p.Children)
                {
                    SetWidthState(child as FrameworkElement, ws);
                }
            }
            else if (el is Border)
            {
                var b = el as Border;
                SetWidthState(b.Child as FrameworkElement, ws);
            }
        }
        public FourPanel()
        {
            this.InitializeComponent();
            SetOrientation();
            Window.Current.SizeChanged += (s, e) => 
            {
                SetOrientation();
                SetHScrollbar(e.Size.Width);
                SetWidthState();
            };
            SetHScrollbar(Window.Current.Bounds.Width);
            SetWidthState();
        }

        private void SetHScrollbar(double width)
        {
            const int ScrollWidth = 800; //NOTE: set this width correctly
            var hvis = (width < ScrollWidth) ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
            uiScrollViewer.HorizontalScrollBarVisibility = hvis;
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
