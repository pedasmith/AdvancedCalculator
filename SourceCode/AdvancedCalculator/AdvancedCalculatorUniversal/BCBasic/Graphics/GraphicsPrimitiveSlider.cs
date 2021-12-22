using BCBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using static AdvancedCalculator.BCBasic.Graphics.GraphicsControl;

namespace BCBasic.Graphics
{
    class GraphicsPrimitiveSlider : GraphicsPrimitiveRectangleShape
    {
        BCRunContext Context;
        string Function;
        Slider slider = null;
        public GraphicsPrimitiveSlider(BCRunContext context, Canvas uiCanvas, GraphicsState state, double H, double W, double x1, double y1, double x2, double y2, string txt, string fnc)
            : base(uiCanvas, state, H, W, x1, y1, x2, y2)
        {
            Context = context;
            Function = fnc;
            // Do the bits that make is a rectangle.
            var dX1 = state.XY2X(x1, y1, H, W);
            var dY1 = state.XY2Y(x1, y1, H, W);
            var dX2 = state.XY2X(x2, y2, H, W);
            var dY2 = state.XY2Y(x2, y2, H, W);
            var isLandscape = Math.Abs(dX2 - dX1) > Math.Abs(dY2 - dY1);

            slider = new Windows.UI.Xaml.Controls.Slider()
            {
                Width = Math.Abs(dX2 - dX1),
                Height = Math.Abs(dY2 - dY1),
                Header = txt,
                Minimum = 0,
                Maximum = 255,
                Orientation = isLandscape ? Orientation.Horizontal : Orientation.Vertical,
            };
            slider.ValueChanged += Slider_ValueChanged;
            fe = slider;
            shape = null;
            uiCanvas.Children.Add(fe);
            Canvas.SetLeft(fe, Math.Min(dX1, dX2));
            Canvas.SetTop(fe, Math.Min(dY1, dY2));
        }

        private void Slider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            var args = new List<IExpression>() { new ObjectConstant(this), new NumericConstant (e.NewValue) };
            Context.ProgramRunContext.AddCallback(Context, Function, args);
        }

        public override string PreferredName { get { return "Slider"; } }

        public override IList<string> GetNames()
        {
            var theseMethods = new List<string>() { "Max", "Min", "Text", "Value" };
            var allMethods = theseMethods.Concat(base.GetNames()).ToList();
            return allMethods;
        }

        public override BCValue GetValue(string name)
        {
            switch (name)
            {
                case "Min": return new BCValue((fe as Slider).Minimum);
                case "Max": return new BCValue((fe as Slider).Maximum);
                case "Text": return new BCValue((fe as Slider).Header as string);
                case "Value": return new BCValue((fe as Slider).Value);
            }
            return base.GetValue(name);
        }


        public override void SetValue(string name, BCValue value)
        {
            switch (name)
            {
                case "Max":
                    slider.Maximum = value.AsDouble;
                    break;
                case "Min":
                    slider.Minimum = value.AsDouble;
                    break;
                case "Text":
                    slider.Header = value.AsString;
                    break;
                case "Value":
                    slider.Value = value.AsDouble;
                    break;
                default:
                    base.SetValue(name, value);
                    break;
            }
        }
    }
}
