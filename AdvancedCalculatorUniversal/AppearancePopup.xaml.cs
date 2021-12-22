using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public sealed partial class AppearancePopup : UserControl, IDoAppearance
    {
        public AppearancePopup()
        {
            this.InitializeComponent();
        }
        public Background MainBackground { get; set; }
        public IDoAppearance DoAppearance { get; set; }
        public SimpleCalculator SimpleCalculator { get; set; }
        public void Init(Background background)
        {
            MainBackground = background;
            this.DataContext = this;
            string font = DoAppearance.GetFont();
            foreach (var item in uiSelectFont.Items)
            {
                var cbi = item as ComboBoxItem;
                if (cbi == null) continue;
                var fname = cbi.Tag as string;
                if (fname == font)
                {
                    uiSelectFont.SelectedItem = item;
                }
            }
        }

        private void ApplyBackground()
        {
            MainBackground.BackgroundText = uiBackgroundText.Text;
            int N = (int)MainBackground.NumberOfRows;
            bool ok = Int32.TryParse(uiBackgroundNumberOfRows.Text, out N);
            if (N > 10) N = 10;
            if (N < 1) N = 1;
            MainBackground.NumberOfRows = N;
            MainBackground.Redraw(); // Already has a saved grid
        }

#if NEVER_EVER_DEFINED
        private void OnAppearanceApply(object sender, RoutedEventArgs e)
        {
            ApplyBackground();
            if (DoAppearance != null)
            {
                DoAppearance.DismissAppearancePopup();
            }
        }
#endif

        private void OnSetColor(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            if (b == null) return;
            string tag = b.Tag as string;
            if (tag == null) return;
            if (DoAppearance != null)
            {
                DoAppearance.ColorScheme = tag;
            }
            //ColorScheme = tag;
        }

        private void OnBackgroundApply(object sender, TextChangedEventArgs e)
        {
            //OnAppearanceApply(null, null);
            ApplyBackground();
        }

        private void StartChangeColor(string colorTag, Button sender, Windows.UI.Color color)
        {
            uiColorPicker.ColorTag = colorTag;
            uiColorPicker.DoAppearance = this;
            uiColorPicker.Init(color);
            uiColorPickerPopup.IsOpen = true;

            var position = (sender as Button)
                .TransformToVisual(Window.Current.Content)
                .TransformPoint(new Point());

            uiColorPickerPopup.IsOpen = true;
            uiColorPickerPopup.HorizontalOffset = 0;
            //uiColorPickerPopup.VerticalOffset = -250;
            int val = -300;
            uiColorPickerPopup.VerticalOffset = val; //  Window.Current.Bounds.Height - 300;// 200; // -150;

            //uiAppearance.HorizontalOffset = 0;
            //uiAppearance.VerticalOffset = 0;

            uiColorPickerPopup.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Left;
            uiColorPickerPopup.VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Top;
        }

        private void OnChangeSymbolColor(object sender, RoutedEventArgs e)
        {
            string tag = "BackgroundSymbol";
            StartChangeColor(tag, sender as Button, GetColor (tag));
        }

        private void OnChangeBackgroundColor(object sender, RoutedEventArgs e)
        {
            string tag = "Background";
            StartChangeColor(tag, sender as Button, GetColor (tag));
        }


        public void DismissAppearancePopup()
        {
        }

        public string ColorScheme
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public void SetColor(string tag, Windows.UI.Color color)
        {
            DoAppearance.SetColor(tag, color);
        }
        public Windows.UI.Color GetColor(string tag)
        {
            return DoAppearance.GetColor(tag);
        }

        private void OnButton(object sender, RoutedEventArgs e)
        {
            if (SimpleCalculator == null) return;
            var button = sender as Button;
            if (button == null) return;
            var value = button.Tag as string;
            if (value == null) return;
            SimpleCalculator.DoButton(value);
        }

        private void OnSelectFont(object sender, SelectionChangedEventArgs e)
        {
            if (DoAppearance == null) return;
            if (e.AddedItems.Count < 1) return;
            var font = (e.AddedItems[0] as ComboBoxItem).Tag as string;
            DoAppearance.SetFont(font);
        }

        public void SetFont(string value)
        {
            DoAppearance.SetFont(value);
        }
        public string GetFont()
        {
            return DoAppearance.GetFont();
        }
    }
}
