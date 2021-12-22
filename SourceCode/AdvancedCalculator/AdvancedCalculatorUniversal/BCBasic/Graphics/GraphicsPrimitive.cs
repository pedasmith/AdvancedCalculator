using BCBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using static AdvancedCalculator.BCBasic.Graphics.GraphicsControl;

namespace BCBasic.Graphics
{
    public class GraphicsPrimitiveRectangleShape : IObjectValue
    {
        internal Windows.UI.Xaml.Shapes.Shape shape; // might be null when e.g. Button or Slider
        internal FrameworkElement fe;
        internal bool SetRotate = false;
        internal Canvas uiCanvas;
        internal double x1, x2, y1, y2, H, W;
        internal double cxd = 0.0;
        internal double cyd = 0.0;
        internal GraphicsState state;
        internal BCValue Data = new BCValue(0); // Arbitrary data the user can specify

        public GraphicsPrimitiveRectangleShape(Canvas uiCanvas, GraphicsState state, double H, double W, double x1, double y1, double x2, double y2)
        {
            this.uiCanvas = uiCanvas;
            this.H = H;
            this.W = W;
            this.state = state;
            this.x1 = x1;
            this.y1 = y1;
            this.x2 = x2;
            this.y2 = y2;
            this.cxd = 0.5;
            this.cyd = 0.5;
        }

        public virtual string PreferredName {  get { return "Shape"; } }

        public virtual void Dispose()
        {
        }

        public virtual IList<string> GetNames()
        {
            return new List<string>() { "Data, CX, CY, CXD, CYD, X1, Y1, X2, Y2, H, W, Opacity, Methods" };
        }

        public virtual BCValue GetValue(string name)
        {
            switch (name)
            {
                case "Data": return Data;
                case "CX": return new BCValue((x1+x2)/2.0); // average
                case "CY": return new BCValue((y1+y2)/2.0); // average
                case "CXD": return new BCValue(cxd);
                case "CYD": return new BCValue(cyd);
                case "X1": return new BCValue(x1);
                case "Y1": return new BCValue(y1);
                case "X2": return new BCValue(x2);
                case "Y2": return new BCValue(y2);
                case "W": return new BCValue(x2-x1);
                case "H": return new BCValue(y2-y1);
                case "Opacity":
                    if (fe != null) return new BCValue (fe.Opacity);
                    return new BCValue(double.NaN);

                case "Methods":
                    return new BCValue("Intersect,ToString");
                case "Name":
                    return new BCValue(PreferredName);
            }
            return BCValue.MakeNoSuchProperty(this, name);
        }

        public virtual void InitializeForRun()
        {
        }

        public virtual async Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            switch (name)
            {
                case "Intersect":
                    if (!BCObjectUtilities.CheckArgs(1, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;

                    var r = (await ArgList[0].EvalAsync(context)).AsObject as GraphicsPrimitiveRectangleShape;
                    if (r == null)
                    {
                        Retval.SetError(1, "argument is not a rectangle");
                        return RunResult.RunStatus.OK;
                    }
                    var overlap = this.x1 < r.x2 && this.x2 > r.x1 && this.y1 < r.y2 && this.y2 > r.y1;
                    Retval.AsDouble = overlap ? 1.0 : 0.0;


                    return RunResult.RunStatus.OK;
                case "ToString":
                    Retval.AsString = $"Object {PreferredName}";
                    return RunResult.RunStatus.OK;
                default:
                    await Task.Delay(0);
                    BCValue.MakeNoSuchMethod(Retval, this, name);
                    return RunResult.RunStatus.ErrorContinue;
            }
        }

        public virtual void RunComplete()
        {
        }

        protected void SetXOrY (string name, BCValue value)
        {
            switch (name)
            {
                case "CX": { var w = x2 - x1; x1 = value.AsDouble - w / 2.0; x2 = x1 + w; break; }
                case "CY": { var h = y2 - y1; y1 = value.AsDouble-h/2.0; y2 = y1 + h; break; }
                case "X1": { var w = x2 - x1; x1 = value.AsDouble; x2 = x1 + w; break; }
                case "Y1": { var h = y2 - y1; y1 = value.AsDouble; y2 = y1 + h; break; }
                case "X2": x2 = value.AsDouble; break;
                case "Y2": y2 = value.AsDouble; break;
            }

            var dX1 = state.XY2X(x1, y1, H, W);
            var dY1 = state.XY2Y(x1, y1, H, W);
            var dX2 = state.XY2X(x2, y2, H, W);
            var dY2 = state.XY2Y(x2, y2, H, W);

            Canvas.SetLeft(fe, Math.Min(dX1, dX2));
            Canvas.SetTop(fe, Math.Min(dY1, dY2));
            if (name == "X2" || name == "Y2")
            {
                fe.Width = Math.Abs(dX2 - dX1);
                fe.Height = Math.Abs(dY2 - dY1);
            }

        }
        public virtual void SetValue(string name, BCValue value)
        {
            switch (name)
            {
                case "CXD": cxd = value.AsDouble; break;
                case "CYD": cyd = value.AsDouble; break;

                case "Data":
                    Data = value;
                    break;

                case "Fill":
                    if (shape != null)
                    {
                        //NOTE: why can obj.Fill be set to a number but
                        // the Stroke and the overall g.Stroke cannot?
                        if (value.CurrentType == BCValue.ValueType.IsDouble)
                        {
                            var color = new BCColor(value.AsInt);
                            shape.Fill = color.Brush;
                        }
                        else
                        {
                            var color = new BCColor(value.AsString);
                            shape.Fill = color.Brush;
                        }
                    }
                    break;

                case "Opacity":
                    if (fe != null)
                    {
                        var opacity = value.AsDouble;
                        fe.Opacity = opacity;
                    }
                    break;

                case "Rotate":
                    {
                        // XAML uses degrees, but the rest of BASIC uses radians.
                        // I choose radians here to be consistant.
                        // XAML also goes clockwise, but counter-clockwise is
                        // the normal math way to rotate.
                        var degrees = -(360.0 * value.AsDouble) / (Math.PI * 2);
                        if (!SetRotate)
                        {
                            SetRotate = true;
                            fe.RenderTransform = new RotateTransform();
                        }
                        //fe.RenderTransformOrigin = new Windows.Foundation.Point(cxd, cyd);
                        (fe.RenderTransform as RotateTransform).Angle = degrees;
                        (fe.RenderTransform as RotateTransform).CenterX = fe.Width *cxd;
                        (fe.RenderTransform as RotateTransform).CenterY = fe.Height *cyd;

                    }
                    break;

                case "Stroke":
                    if (shape != null)
                    {
                        if (value.CurrentType == BCValue.ValueType.IsDouble)
                        {
                            var color = new BCColor(value.AsInt);
                            shape.Stroke = color.Brush;
                        }
                        else
                        {
                            var color = new BCColor(value.AsString);
                            shape.Stroke = color.Brush;
                        }
                    }
                    break;

                case "CX":
                case "CY":
                case "X1":
                case "Y1":
                case "X2":
                case "Y2":
                    SetXOrY(name, value);
                    break;
            }

        }
    }
}
