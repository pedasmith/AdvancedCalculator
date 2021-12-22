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
    class GraphicsPrimitivePolygon : GraphicsPrimitiveRectangleShape
    {
        public GraphicsPrimitivePolygon(Canvas uiCanvas, GraphicsState state, double H, double W)
            : base(uiCanvas, state, H, W, 0, 0, W, H)
        {

            shape = new Windows.UI.Xaml.Shapes.Polygon()
            {
                StrokeThickness = state.BrushWidth,
                Fill = state.Fill,
                Stroke = state.Stroke
            };

            fe = shape;
            uiCanvas.Children.Add(shape);
        }

        private void MakePoints()
        {
            PointCollection pc = new PointCollection();
            (shape as Polygon).Points = pc;
        }

        public override string PreferredName { get { return "Polygon"; } }

        public override void Dispose()
        {
        }

        public override IList<string> GetNames()
        {
            var theseMethods = new List<string>() { };
            var allMethods = theseMethods.Concat(base.GetNames()).ToList();
            return allMethods;
        }

        public override BCValue GetValue(string name)
        {
            return base.GetValue(name);
        }

        public override void InitializeForRun()
        {
        }

        public override async Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            switch (name)
            {
                case "AddPoints":
                case "SetPoints":
                    PointCollection pc = name == "AddPoints" ? (shape as Polygon).Points : new PointCollection();
                    if (ArgList.Count == 1) // must be an array?
                    {
                        var arg = await ArgList[0].EvalAsync(context);
                        if (arg.IsArray)
                        {
                            var arraylist = arg.AsArray.data;
                            for (int i = 0; i < arraylist.Count - 1; i += 2)
                            {
                                double x = arraylist[i].AsDouble;
                                double y = arraylist[i+1].AsDouble;
                                var dX = state.XY2X(x, y, H, W);
                                var dY = state.XY2Y(x, y, H, W);
                                pc.Add(new Windows.Foundation.Point(dX, dY));
                            }
                        }
                    }
                    else for (int i=0; i<ArgList.Count-1; i+=2)
                    {
                        double x = (await ArgList[i].EvalAsync(context)).AsDouble;
                        double y = (await ArgList[i+1].EvalAsync(context)).AsDouble;
                        var dX = state.XY2X(x, y, H, W);
                        var dY = state.XY2Y(x, y, H, W);
                        pc.Add(new Windows.Foundation.Point(dX, dY));
                    }
                    (shape as Polygon).Points = pc;
                    return RunResult.RunStatus.OK;
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
                default:
                    base.SetValue(name, value);
                    break;
            }
        }
    }
}
