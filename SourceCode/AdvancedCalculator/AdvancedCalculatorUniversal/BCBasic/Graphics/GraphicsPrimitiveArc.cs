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
    class GraphicsPrimitiveArc : GraphicsPrimitiveRectangleShape
    {
        private double CX { get; set; }
        private double CY { get; set; }
        private double InnerR { get; set; }
        private double OuterR { get; set; }
        private double Ang1 { get; set; }
        private double Ang2 { get; set; }

        public GraphicsPrimitiveArc(Canvas uiCanvas, GraphicsState state, double H, double W, double cx, double cy, double innerR, double outerR, double ang1, double ang2)
            : base(uiCanvas, state, H, W, cx-innerR, cy-innerR, cx+outerR, cy+outerR)
        {
            CX = cx;
            CY = cy;
            InnerR = innerR;
            OuterR = outerR;
            Ang1 = ang1;
            Ang2 = ang2;
            while (Ang2 < Ang1) Ang2 += Math.PI * 2.0;

            // Do the bits that make is a rectangle.
            var dX1 = state.XY2X(CX, CY, H, W);
            var dY1 = state.XY2Y(CX, CY, H, W);

            shape = new Windows.UI.Xaml.Shapes.Polygon()
            {
                StrokeThickness = state.BrushWidth,
                Fill = state.Fill,
                Stroke = state.Stroke
            };
            MakePoints();

            fe = shape;
            uiCanvas.Children.Add(shape);
            //Canvas.SetLeft(shape, dX1);
            //Canvas.SetTop(shape, dY1);
        }

        private void MakePoints()
        {
            int npts = (int)Math.Ceiling(Math.Abs(Ang2 - Ang1) * 10.0); // MAGIC: 10 is a pretty arbitrary value. 
                                                                        // A 90-degree arc has about 1.4*10 (15) points, which seems to be OK.
            PointCollection pc = new PointCollection();
            for (int i = 0; i < npts; i++)
            {
                double angle = ((double)i * ((Ang2 - Ang1) / ((double)npts-1.0))) + Ang1;
                double x = CX + Math.Cos(angle) * InnerR;
                double y = CY + Math.Sin(angle) * InnerR;
                var dX = state.XY2X(x, y, H, W);
                var dY = state.XY2Y(x, y, H, W);
                pc.Add(new Windows.Foundation.Point(dX, dY));
            }
            for (int i = npts-1; i >= 0; i--)
            {
                double angle = ((double)i * ((Ang2 - Ang1) / ((double)npts-1.0))) + Ang1;
                double x = CX + Math.Cos(angle) * OuterR;
                double y = CY + Math.Sin(angle) * OuterR;
                var dX = state.XY2X(x, y, H, W);
                var dY = state.XY2Y(x, y, H, W);
                pc.Add(new Windows.Foundation.Point(dX, dY));
            }
            (shape as Polygon).Points = pc;
        }

        public override string PreferredName { get { return "Arc"; } }

        public override void Dispose()
        {
        }

        public override IList<string> GetNames()
        {
            var theseMethods = new List<string>() { "Methods", "CX", "CY", "InnerR", "OuterR", "Ang1", "Ang2" };
            var allMethods = theseMethods.Concat(base.GetNames()).ToList();
            return allMethods;
        }

        public override BCValue GetValue(string name)
        {
            switch (name)
            {
                case "CX": return new BCValue(CX);
                case "CY": return new BCValue(CY);
                case "InnerR": return new BCValue(InnerR);
                case "OuterR": return new BCValue(OuterR);
                case "Ang1": return new BCValue(Ang1);
                case "Ang2": return new BCValue(Ang2);
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
                case "CX": CX = value.AsDouble; break;
                case "CY": CY = value.AsDouble; break;
                case "InnerR": InnerR = value.AsDouble; break;
                case "OuterR": OuterR = value.AsDouble; break;
                case "Ang1": Ang1 = value.AsDouble; break;
                case "Ang2": Ang2 = value.AsDouble; break;
                default:
                    base.SetValue(name, value);
                    break;
            }
            MakePoints();
        }
    }
}
