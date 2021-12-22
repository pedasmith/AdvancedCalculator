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

using System.Diagnostics;
using Windows.UI.Xaml.Shapes;
using Windows.UI;
// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace AdvancedCalculator
{
    public sealed partial class ColorPicker : UserControl
    {
        public string ColorTag { get; set; }
        public IDoAppearance DoAppearance { get; set; }
        Color _PickedColor;
        public Color PickedColor { get { return _PickedColor; } set { _PickedColor = value; SelectColor(); } }

        public ColorPicker()
        {
            DoAppearance = null;
            this.InitializeComponent();
        }

        public ColorPicker(Color init)
            :this()
        {
            Init(init);
        }

        public void Init(Color init)
        {
            _PickedColor = init;
            double slider = ByteOpacityToSlider(PickedColor.A);
            uiOpacity.Value = slider;
            SelectColor();
        }

        public bool ColorMatches(Color A, Color B)
        {
            bool Retval = (A.R == B.R) && (A.G == B.G) && (A.B == B.B);
            return Retval;
        }

        public void DeselectAll()
        {
            foreach (var child in uiColorGrid.Children)
            {
                var border = child as Border;
                border.BorderThickness = new Thickness (0);
            }
        }

        public void SelectColor()
        {
            if (uiColorGrid == null) return;
            DeselectAll();
            foreach (var child in uiColorGrid.Children)
            {
                var border = child as Border;
                var rect = border.Child as Rectangle;
                Color A = (rect.Fill as SolidColorBrush).Color;
                if (ColorMatches (A, _PickedColor))
                {
                    border.BorderThickness = new Thickness(2);
                }
            }
        }


        private void OnChangeColor(object sender, TappedRoutedEventArgs e)
        {
            var r = sender as Shape;
            var sb = r.Fill as SolidColorBrush;
            PickedColor = sb.Color;
            SetOpacity();
            if (DoAppearance != null)
            {
                DoAppearance.SetColor(ColorTag, PickedColor);
            }

        }

        const double byteMin = 8.0;
        const double byteMax = 64.0;
        static byte SliderToByteOpacity(double slider)
        {
            // slider is 1..10
            double val = (slider - 1.0) / 9.0; // 0..1
            val *= (byteMax - byteMin);
            val += byteMin;
            return (byte)Math.Round(val);
        }

        static double ByteOpacityToSlider(byte op)
        {
            double val = op;
            val -= byteMin;
            val /= (byteMax - byteMin);
            val *= 9.0;
            val += 1.0;
            return val;
        }

        static int TestOpacityConversion_One(byte startValue)
        {
            int NError = 0;
            double val = ByteOpacityToSlider(startValue);
            byte finalValue = SliderToByteOpacity(val);
            if (startValue != finalValue)
            {
                Debug.WriteLine("Error: ColorPicker: TestOpacityConversion ({0}) --> {1} --> {2}", startValue, val, finalValue);
                NError++;
            }
            return NError;
        }

        public static int TestOpacityConversion()
        {
            int NError = 0;
            for (byte b = 0; b < 128; b++)
            {
                NError += TestOpacityConversion_One(b);
            }
            return NError;
        }

        private void SetOpacity()
        {
            if (uiOpacity == null) return; // Initializing...

            // Convert 1..10 into 8..64
            _PickedColor.A = SliderToByteOpacity(uiOpacity.Value);
            /*
            double op = uiOpacity.Value;
            op = (op - 1) / 9; // 0..1
            op = (op * (64.0 - 8.0)) + 8.0;
            _PickedColor.A = (byte)op;
             */
        }

        private void OnChangeOpacity(object sender, RangeBaseValueChangedEventArgs e)
        {
            SetOpacity();
            if (DoAppearance != null)
            {
                DoAppearance.SetColor(ColorTag, PickedColor);
            }
        }
    }
}
