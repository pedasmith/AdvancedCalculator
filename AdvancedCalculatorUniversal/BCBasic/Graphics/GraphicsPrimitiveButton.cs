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
    class GraphicsPrimitiveButton : GraphicsPrimitiveRectangleShape
    {
        BCRunContext Context;
        string Function;
        Button button = null;
        public GraphicsPrimitiveButton(BCRunContext context, Canvas uiCanvas, GraphicsState state, double H, double W, double x1, double y1, double x2, double y2, string txt, string fnc)
            : base(uiCanvas, state, H, W, x1, y1, x2, y2)
        {
            Context = context;
            Function = fnc;
            // Do the bits that make is a rectangle.
            var dX1 = state.XY2X(x1, y1, H, W);
            var dY1 = state.XY2Y(x1, y1, H, W);
            var dX2 = state.XY2X(x2, y2, H, W);
            var dY2 = state.XY2Y(x2, y2, H, W);

            button = new Windows.UI.Xaml.Controls.Button()
            {
                Background= new SolidColorBrush (Colors.Gray),
                Foreground = new SolidColorBrush(Colors.White),
                Width = Math.Abs(dX2 - dX1),
                Height = Math.Abs(dY2 - dY1),
                Content = txt,
            };
            button.Click += Button_Click;
            fe = button;
            shape = null;
            uiCanvas.Children.Add(fe);
            Canvas.SetLeft(fe, Math.Min(dX1, dX2));
            Canvas.SetTop(fe, Math.Min(dY1, dY2));
        }

        private void Button_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var args = new List<IExpression>() { new ObjectConstant (this) };
            Context.ProgramRunContext.AddCallback(Context, Function, args);
        }

        public override string PreferredName { get { return "Button"; } }

        public override IList<string> GetNames()
        {
            var theseMethods = new List<string>() { "Text" };
            var allMethods = theseMethods.Concat(base.GetNames()).ToList();
            return allMethods;
        }

        public override BCValue GetValue(string name)
        {
            switch (name)
            {
                case "Text": return new BCValue ((fe as Button).Content as string);

            }
            return base.GetValue(name);
        }


        public override void SetValue(string name, BCValue value)
        {
            switch (name)
            {
                case "Text":
                    (fe as Button).Content = value.AsString;
                    break;
                default:
                    base.SetValue(name, value);
                    break;
            }

        }
    }
}
