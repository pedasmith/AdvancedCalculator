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
    class GraphicsPrimitiveEllipse : GraphicsPrimitiveRectangleShape
    {

        public GraphicsPrimitiveEllipse(Canvas uiCanvas, GraphicsState state, double H, double W, double x1, double y1, double x2, double y2)
            : base(uiCanvas, state, H, W, x1, y1, x2, y2)
        {
            // Do the bits that make is a rectangle.
            var dX1 = state.XY2X(x1, y1, H, W);
            var dY1 = state.XY2Y(x1, y1, H, W);
            var dX2 = state.XY2X(x2, y2, H, W);
            var dY2 = state.XY2Y(x2, y2, H, W);

            shape = new Windows.UI.Xaml.Shapes.Ellipse()
            {
                Width = Math.Abs(dX2 - dX1),
                Height = Math.Abs(dY2 - dY1),
                StrokeThickness = state.BrushWidth,
                Fill = state.Fill,
                Stroke = state.Stroke
            };
            fe = shape;
            uiCanvas.Children.Add(shape);
            Canvas.SetLeft(shape, Math.Min(dX1, dX2));
            Canvas.SetTop(shape, Math.Min(dY1, dY2));
        }

        public override string PreferredName { get { return "Circle"; } }

        public override void Dispose()
        {
        }

        public override IList<string> GetNames()
        {
            var theseMethods = new List<string>() { "Radius" };
            var allMethods = theseMethods.Concat(base.GetNames()).ToList();
            return allMethods;
        }

        public override BCValue GetValue(string name)
        {
            switch (name)
            {
                case "Radius":
                    {
                        var rx = (x1 + x2) / 2;
                        var ry = (y1 + y2) / 2;
                        var r = (rx + ry) / 2; // Average of the X and Y radius value
                        return new BCValue(r);
                    }
            }
            return base.GetValue(name);
        }

        public override void InitializeForRun()
        {
        }

        public override async Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            switch (name)
            {
                default:
                    return await base.RunAsync(context, name, ArgList, Retval);
            }
        }

        public override void RunComplete()
        {
        }


        public override void SetValue(string name, BCValue value)
        {
            switch (name)
            {
                case "Radius":
                    {
                        var r = value.AsDouble;
                        var cx = (x1 + x2) / 2;
                        SetXOrY("X1", new BCValue(cx - r));
                        SetXOrY("X2", new BCValue(cx + r));
                        var cy = (y1 + y2) / 2;
                        SetXOrY("Y1", new BCValue(cy - r));
                        SetXOrY("Y2", new BCValue(cy + r));
                        break;
                    }
                default:
                    base.SetValue(name, value);
                    break;
            }

        }
    }
}
