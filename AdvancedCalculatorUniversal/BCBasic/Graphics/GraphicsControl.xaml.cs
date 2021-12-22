using BCBasic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using Windows.UI;
using BCBasic.Graphics;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace AdvancedCalculator.BCBasic.Graphics
{
    public sealed partial class GraphicsControl : UserControl, IObjectValue, IGraphics
    {
        public enum GraphicsType { Window, FullScreen }
        public GraphicsType CurrGraphicsType { get; internal set; } = GraphicsControl.GraphicsType.Window;
        private bool MustSetBackground = false;
        public GraphicsControl(GraphicsType gtype = GraphicsType.Window, BCColor background = null, BCColor foreground = null)
        {
            this.InitializeComponent();
            CurrGraphicsType = gtype;
            var state = new GraphicsState(this);
            var pixelMap = new GraphicsMap(state.Map);
            Maps.Add("pixel", pixelMap);
            if (background != null) { state.Background = background.Brush; MustSetBackground = true; }
            if (foreground != null) { state.Stroke = foreground.Brush; }
            States.Push(state);
            if (MustSetBackground) this.Loaded += GraphicsControl_Loaded;
        }

        public void SetAlignment()
        {
            this.HorizontalAlignment = HorizontalAlignment.Left;
            this.VerticalAlignment = VerticalAlignment.Top;
        }
#if NEVER_EVER_DEFINED
        //TODO: In progress: make a window object. I've needed it for ages!
        public GraphicsControl(GraphicsControl parent, double w, double h)
        {
            this.InitializeComponent();
            CurrGraphicsType = GraphicsType.Window;
            var state = new GraphicsState(parent);
            States.Push(state);
            MustSetBackground = true;
            this.Loaded += GraphicsControl_Loaded;
        }
#endif
        private void GraphicsControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (MustSetBackground)
            {
                uiBackground.Background = States.Peek().Background;
            }
        }

        public string PreferredName => "Graphics";

        public void Dispose()
        {
        }

        public IList<string> GetNames() { return new List<string>() { "H,Methods,W" }; }

        public BCValue GetValue(string name)
        {
            switch (name)
            {
                case "H": return new BCValue(H);
                case "W": return new BCValue(W);
                case "Methods":
                    return new BCValue("Arc,Canvas,Circle,ClearGoTo,Cls,GoTo,Line,LineTo,Polygon,Pop,Push,Rectangle,Button,Slider,Text,GraphXY,GraphY,SetPosition,SetScale,SetScaleWindow,SetSize,Update,UseScale,ToString");
            }
            return BCValue.MakeNoSuchProperty(this, name);
        }

        public void InitializeForRun()
        {
        }

        // Convert XY into pixel XY values
        private Dictionary<string, GraphicsMap> Maps = new Dictionary<string, GraphicsMap>();
        public class GraphicsMap
        {
            public GraphicsMap(GraphicsControl gc)
            {
                XMap = new GraphicsScaleMapLinear() { ScaleMin = 0, ScaleMax = gc.W, PixelMin = 0, PixelMax = gc.W };
                YMap = new GraphicsScaleMapLinear() { ScaleMin = 0, ScaleMax = gc.H, PixelMin = 0, PixelMax = gc.H };
                YPixelHeight = gc.H;
            }
#if NEVER_EVER_DEFINED
            // "P" values are pixel (the window) and "S" value are the input scale units
            public GraphicsMap(double XP1, double YP1, double XP2, double YP2, double XS1, double YS1, double XS2, double YS2)
            {
                XMap = new GraphicsScaleMapLinear() { ScaleMin = XS1, ScaleMax = XS2, PixelMin = XP1, PixelMax = XP2 };
                YMap = new GraphicsScaleMapLinear() { ScaleMin = YS1, ScaleMax = YS2, PixelMin = YP1, PixelMax = YP2 };
            }
#endif
            public GraphicsMap()
            {
                XMap = new GraphicsScaleMapLinear() { ScaleMin = 0.0, ScaleMax = 1.0, PixelMin = 0.0, PixelMax = 1.0 };
                YMap = new GraphicsScaleMapLinear() { ScaleMin = 0.0, ScaleMax = 1.0, PixelMin = 0.0, PixelMax = 1.0 };
            }

            public GraphicsMap(GraphicsMap value)
            {
                XMap = new GraphicsScaleMapLinear(value.XMap);
                YMap = new GraphicsScaleMapLinear(value.YMap);
                YPixelHeight = value.YPixelHeight;
            }
            public void SetPadding(double padding)
            {
                XMap.PixelMin += padding;
                XMap.PixelMax -= padding;
                YMap.PixelMin += padding;
                YMap.PixelMax -= padding;
            }
            public void SetMap(double x1, double y1, double x2, double y2)
            {
                XMap.ScaleMin = x1;
                YMap.ScaleMin = y1;
                XMap.ScaleMax = x2;
                YMap.ScaleMax = y2;
            }
            public void SetMapWindow(double x1, double y1, double x2, double y2)
            {
                XMap.PixelMin = x1;
                YMap.PixelMin = y1;
                XMap.PixelMax = x2;
                YMap.PixelMax = y2;
            }
            public double YPixelHeight = 0.0; // When non-zero, the Y values will be correctly flipped so that Y=0 is on the bottom
            public double XMax { get { return XMap.PixelMax; } set { XMap.PixelMax = value; } }
            public double YMax { get { return YMap.PixelMax; } set { YMap.PixelMax = value; } }

            private GraphicsScaleMapLinear XMap = null;
            private GraphicsScaleMapLinear YMap = null;
            public double XY2X(double x, double y, double H, double W)
            {
                return XMap.Convert(x);
            }
            public double XY2Y(double x, double y, double H, double W)
            {
                // Calculates an inverted Y where 0 is at the top, not the bottom.
                var Retval = YMap.Convert(y);
                if (YPixelHeight > 0) Retval = YPixelHeight - Retval;
                return Retval;
            }
            // Helper class for GraphicsMap. It's used to map X (min,max) into X'(min,max) (and Y, too)
            public class GraphicsScaleMapLinear
            {
                public double ScaleMin = 0.0;
                public double ScaleMax = 1.0;
                public double PixelMin = 0.0;
                public double PixelMax = 1.0; // will actually be e.g. XMax or YMax

                public GraphicsScaleMapLinear()
                {
                    ; // doesn't need to do anything.
                }
                public GraphicsScaleMapLinear(GraphicsScaleMapLinear value)
                {
                    ScaleMin = value.ScaleMin;
                    ScaleMax = value.ScaleMax;
                    PixelMin = value.PixelMin;
                    PixelMax = value.PixelMax;
                }

                private double ScaleRange { get { return ScaleMax - ScaleMin; } }
                private bool NoScale { get { return ScaleMin == 0.0 && ScaleMax == 0.0; } }
                private bool ScaleValid { get { return ScaleRange != 0.0; } }
                private double PixelRange { get { return PixelMax - PixelMin; } }
                public double Convert(double value)
                {
                    double scaledValue = NoScale ? value : (ScaleValid ? (value-ScaleMin) / ScaleRange : ScaleMin);
                    double pixelValue = (scaledValue * PixelRange) + PixelMin;
                    return pixelValue;
                }
            }
        }

        public class GraphicsState
        {
            public GraphicsState() { Map = new GraphicsMap(); }
            public GraphicsState(GraphicsControl gc)
            {
                Map = new GraphicsMap(gc);
            }
            public GraphicsMap Map;
            // All of the state values

            public double BrushWidth = 1.0;
            public Brush Stroke = new SolidColorBrush(Colors.Black);
            public Brush Fill = new SolidColorBrush(Colors.Black);
            public Brush Background = new SolidColorBrush(Colors.Black);
            public GraphOptions GraphOptions = new GraphOptions();

            public GraphicsState Dup()
            {
                var Retval = new GraphicsState();
                Retval.BrushWidth = BrushWidth;
                Retval.Stroke = Stroke;
                Retval.Fill = Fill;
                Retval.Background = Background;
                Retval.Map = new GraphicsMap(Map);
                Retval.GraphOptions = GraphOptions.Dup();
                return Retval;
            }

            public double XY2X(double x, double y, double H, double W)
            {
                return Map.XY2X(x, y, H, W);
            }
            public double XY2Y(double x, double y, double H, double W)
            {
                return Map.XY2Y(x, y, H, W);
            }
#if NEVER_EVER_DEFINED
            public double XY2X(double x, double y, double H, double W)
            {
                var ratio = (XMax == XMin) ? 0.0 : (x - XMin) / (XMax - XMin);
                var retval = ratio * (W - XPadding * 2) + XPadding;
                return retval;
            }
            public double XY2Y(double x, double y, double H, double W)
            {
                var ratio = (YMax == YMin) ? 0.0 : (y - YMin) / (YMax - YMin);
                // Convert top to bottom.  0-->1 and 1-->0
                ratio = 1.0 - ratio;
                var retval = ratio * (H - YPadding * 2) + YPadding;
                return retval;
            }
#endif

        }
        Stack<GraphicsState> States = new Stack<GraphicsState>();
        List<AutoGraph> AutoGraphs = new List<AutoGraph>();
        Dictionary<string, int> AutoGraphNames = new Dictionary<string, int>();

        public void Cls()
        {
            saved_x1 = double.NaN;
            saved_y1 = double.NaN;
            uiCanvas.Children.Clear();
        }

#if NEVER_EVER_DEFINED
        public void OLDCircle(double x, double y, double xradius, double yradius)
        {
            var state = States.Peek();
            var dX1 = state.XY2X(x - xradius, y, H, W);
            var dY1 = state.XY2Y(x, y - yradius, H, W);
            var dX2 = state.XY2X(x + xradius, y, H, W);
            var dY2 = state.XY2Y(x, y + yradius, H, W);
            var shape = new Windows.UI.Xaml.Shapes.Ellipse()
            {
                Width = Math.Abs(dX2 - dX1),
                Height = Math.Abs(dY2 - dY1),
                StrokeThickness = state.BrushWidth,
                Fill = state.Fill,
                Stroke = state.Stroke
            };
            uiCanvas.Children.Add(shape);
            Canvas.SetLeft(shape, Math.Min(dX1, dX2));
            Canvas.SetTop(shape, Math.Min(dY1, dY2));
        }
#endif
        public void UpdateLine (int index, double x1, double y1, double x2, double y2)
        {
            if (index >= uiCanvas.Children.Count)
            {
                return; // Nope, not a line
            }
            var line = uiCanvas.Children[index] as Windows.UI.Xaml.Shapes.Line;
            if (line == null)
            {
                return;
            }

            var state = States.Peek();
            var dX1 = state.XY2X(x1, y1, H, W);
            var dY1 = state.XY2Y(x1, y1, H, W);
            var dX2 = state.XY2X(x2, y2, H, W);
            var dY2 = state.XY2Y(x2, y2, H, W);

            line.X1 = dX1;
            line.Y1 = dY1;
            line.X2 = dX2;
            line.Y2 = dY2;
        }
        public void Line(double x1, double y1, double x2, double y2)
        {
            var state = States.Peek();
            var dX1 = state.XY2X(x1, y1, H, W);
            var dY1 = state.XY2Y(x1, y1, H, W);
            var dX2 = state.XY2X(x2, y2, H, W);
            var dY2 = state.XY2Y(x2, y2, H, W);
            var shape = new Windows.UI.Xaml.Shapes.Line() {
                X1 = dX1, Y1 = dY1, X2=dX2, Y2=dY2,
                StrokeThickness = state.BrushWidth, Stroke = state.Stroke };
            uiCanvas.Children.Add(shape);
        }
#if NEVER_EVER_DEFINED
        public void JUNK_RectangleXXXGraphicsPrimitiveRectangle(double x1, double y1, double x2, double y2)
        {
            var state = States.Peek();
            var dX1 = state.XY2X(x1, y1, H, W);
            var dY1 = state.XY2Y(x1, y1, H, W);
            var dX2 = state.XY2X(x2, y2, H, W);
            var dY2 = state.XY2Y(x2, y2, H, W);
            var shape = new Windows.UI.Xaml.Shapes.Rectangle()
            {
                Width = Math.Abs(dX2 - dX1),
                Height = Math.Abs(dY2 - dY1),
                StrokeThickness = state.BrushWidth,
                Fill = state.Fill,
                Stroke = state.Stroke
            };
            uiCanvas.Children.Add(shape);
            Canvas.SetLeft(shape, Math.Min (dX1, dX2));
            Canvas.SetTop(shape, Math.Min (dY1, dY2));
        }
#endif

        public GraphicsState CurrState {  get { return States.Peek(); } set { States.Push(value); } }
        public double H { get { return uiCanvas.Height; } }
        public double W { get { return uiCanvas.Width; } }
        public void Pop()
        {
            States.Pop();
            if (States.Count == 0) States.Push(new GraphicsState(this));
        }
        public void Push()
        {
            var dup = States.Peek().Dup();
            States.Push(dup);
        }

        public void Update()
        {
            if (AutoGraphs.Count > 0)
            {
                Cls();
                foreach (var ag in AutoGraphs)
                {
                    ag.Display(this);
                }
            }
        }
        
        public void SetPosition(double x, double y)
        {
            this.Margin = new Thickness(x, y, 0, 0);
        }

        public void SetSize (double h, double w)
        {
            if (h < 0) h = 0;
            if (w < 0) w = 0;

            var state = States.Peek();
            bool IsSameSize = (state.Map.YMax == uiCanvas.Height && state.Map.XMax == uiCanvas.Width);
            // Take into account the paddding, border brush width but not margin 
            var bdr = uiBackground;
            var lmargin = bdr.Padding.Left + bdr.BorderThickness.Left;
            var rmargin = bdr.Padding.Right+ bdr.BorderThickness.Right;
            var tmargin = bdr.Padding.Top + bdr.BorderThickness.Top;
            var bmargin = bdr.Padding.Bottom + bdr.BorderThickness.Bottom;

            var ph = h - tmargin - bmargin;
            var pw = w - lmargin - rmargin;
            if (ph >= 0 && pw >= 0)
            {
                uiCanvas.Height = ph;
                uiCanvas.Width = pw;
                if (IsSameSize)
                {
                    state.Map.YMax = ph;
                    state.Map.XMax = pw;
                    state.Map.YPixelHeight = ph;
                    state.Map.SetMap(0, 0, w, h);
                }
            }
        }

        private double saved_x1 = double.NaN;
        private double saved_y1 = double.NaN;

        public async Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            switch (name)
            {
                case "Arc":
                    {
                        if (!BCObjectUtilities.CheckArgs(6, 6, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        // if (!await BCObjectUtilities.CheckArgValue(0, "X1", 1, 25, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var x = (await ArgList[0].EvalAsync(context)).AsDouble;
                        var y = (await ArgList[1].EvalAsync(context)).AsDouble;
                        var inner = (await ArgList[2].EvalAsync(context)).AsDouble;
                        var outer = (await ArgList[3].EvalAsync(context)).AsDouble;
                        var ang1 = (await ArgList[4].EvalAsync(context)).AsDouble;
                        var ang2 = (await ArgList[5].EvalAsync(context)).AsDouble;

                        var x1 = x - outer;
                        var y1 = y - outer;
                        var x2 = x + outer;
                        var y2 = y + outer;

                        var shape = new GraphicsPrimitiveArc(uiCanvas, States.Peek(), H, W, x, y, inner, outer, ang1, ang2);
                        saved_x1 = x2;
                        saved_y1 = y2;
                        Retval.AsObject = shape;
                        return RunResult.RunStatus.OK;
                    }
                case "Button":
                    {
                        if (!BCObjectUtilities.CheckArgs(6, 6, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;

                        var x1 = (await ArgList[0].EvalAsync(context)).AsDouble;
                        var y1 = (await ArgList[1].EvalAsync(context)).AsDouble;
                        var x2 = (await ArgList[2].EvalAsync(context)).AsDouble;
                        var y2 = (await ArgList[3].EvalAsync(context)).AsDouble;
                        var txt = (await ArgList[4].EvalAsync(context)).AsString;
                        var fnc = (await ArgList[5].EvalAsync(context)).AsString;

                        var shape = new GraphicsPrimitiveButton(context, uiCanvas, States.Peek(), H, W, x1, y1, x2, y2, txt, fnc);
                        saved_x1 = x2;
                        saved_y1 = y2;
                        Retval.AsObject = shape;
                        return RunResult.RunStatus.OK;
                    }
#if BLUETOOTH
#if !WINDOWS8
                case "Camera":
                    {
                        if (!BCObjectUtilities.CheckArgs(5, 5, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        // if (!await BCObjectUtilities.CheckArgValue(0, "X1", 1, 25, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;

                        var cam = (await ArgList[0].EvalAsync(context)).AsObject;
                        var x1 = (await ArgList[1].EvalAsync(context)).AsDouble;
                        var y1 = (await ArgList[2].EvalAsync(context)).AsDouble;
                        var x2 = (await ArgList[3].EvalAsync(context)).AsDouble;
                        var y2 = (await ArgList[4].EvalAsync(context)).AsDouble;
                        //Rectangle(x1, y1, x2, y2);
                        var shape = new GraphicsPrimitiveCamera(uiCanvas, States.Peek(), H, W, x1, y1, x2, y2);
                        saved_x1 = x2;
                        saved_y1 = y2;
                        Retval.AsObject = shape;
                        return RunResult.RunStatus.OK;
                    }
#endif
#endif
#if NEVER_EVER_DEFINED
                    // NOTE: This might well be implemented in the future. I do need a way to make child
                    // objects of the main graphics object. It's just that right now isn't the time.
                    // Added and removed, July 2018
                case "Canvas":
                    {
                        if (!BCObjectUtilities.CheckArgs(4, 4, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        // if (!await BCObjectUtilities.CheckArgValue(0, "X1", 1, 25, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;

                        var x1 = (await ArgList[0].EvalAsync(context)).AsDouble;
                        var y1 = (await ArgList[1].EvalAsync(context)).AsDouble;
                        var x2 = (await ArgList[2].EvalAsync(context)).AsDouble;
                        var y2 = (await ArgList[3].EvalAsync(context)).AsDouble;

                        //var shape = new GraphicsPrimitiveRectangle(uiCanvas, States.Peek(), H, W, x1, y1, x2, y2);
                        // NOTE: set the background/foreground to the States.Peek().Background + Foreground ? 
                        var shape = new GraphicsControl(GraphicsType.Window);
                        shape.Width = (x2 - x1);
                        shape.Height = (y2 - y1);
                        uiCanvas.Children.Add(shape);
                        Canvas.SetLeft(shape, x1);
                        Canvas.SetTop(shape, y1);

                        Retval.AsObject = shape;
                        return RunResult.RunStatus.OK;
                    }
#endif
                case "Clear":
                    // Remove all children
                    uiCanvas.Children.Clear();
                    return RunResult.RunStatus.OK;

                case "Circle":
                case "Ellipse":
                    {
                        if (!BCObjectUtilities.CheckArgs(3, 4, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        // if (!await BCObjectUtilities.CheckArgValue(0, "X1", 1, 25, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var x = (await ArgList[0].EvalAsync(context)).AsDouble;
                        var y = (await ArgList[1].EvalAsync(context)).AsDouble;
                        var xradius = (await ArgList[2].EvalAsync(context)).AsDouble;
                        var yradius = ArgList.Count > 3 ? (await ArgList[3].EvalAsync(context)).AsDouble : xradius;

                        var x1 = x - xradius;
                        var y1 = y - yradius;
                        var x2 = x + xradius;
                        var y2 = y + yradius;

                        var shape = new GraphicsPrimitiveEllipse(uiCanvas, States.Peek(), H, W, x1, y1, x2, y2);
                        saved_x1 = x;
                        saved_y1 = y;
                        Retval.AsObject = shape;
                        return RunResult.RunStatus.OK;
                    }

                case "ClearGoTo":
                    saved_x1 = Double.NaN;
                    saved_y1 = Double.NaN;
                    return RunResult.RunStatus.OK;

                case "Cls":
                    Cls();
                    return RunResult.RunStatus.OK;
                case "GoTo":
                    {
                        if (!BCObjectUtilities.CheckArgs(2, 2, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        // if (!await BCObjectUtilities.CheckArgValue(0, "X1", 1, 25, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;

                        saved_x1 = (await ArgList[0].EvalAsync(context)).AsDouble;
                        saved_y1 = (await ArgList[1].EvalAsync(context)).AsDouble;
                        return RunResult.RunStatus.OK;
                    }
#if BLUETOOTH
#if !WINDOWS8
                case "Image":
                    {
                        if (!BCObjectUtilities.CheckArgs(4, 4, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        // if (!await BCObjectUtilities.CheckArgValue(0, "X1", 1, 25, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;

                        var x1 = (await ArgList[0].EvalAsync(context)).AsDouble;
                        var y1 = (await ArgList[1].EvalAsync(context)).AsDouble;
                        var x2 = (await ArgList[2].EvalAsync(context)).AsDouble;
                        var y2 = (await ArgList[3].EvalAsync(context)).AsDouble;
                        //Image(x1, y1, x2, y2);
                        var shape = new GraphicsPrimitiveImage(uiCanvas, States.Peek(), H, W, x1, y1, x2, y2);
                        saved_x1 = x2;
                        saved_y1 = y2;
                        Retval.AsObject = shape;
                        return RunResult.RunStatus.OK;
                    }
#endif
#endif
                case "Line":
                    {
                        if (!BCObjectUtilities.CheckArgs(4, 4, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        // if (!await BCObjectUtilities.CheckArgValue(0, "X1", 1, 25, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;

                        var x1 = (await ArgList[0].EvalAsync(context)).AsDouble;
                        var y1 = (await ArgList[1].EvalAsync(context)).AsDouble;
                        var x2 = (await ArgList[2].EvalAsync(context)).AsDouble;
                        var y2 = (await ArgList[3].EvalAsync(context)).AsDouble;
                        Line(x1, y1, x2, y2);
                        saved_x1 = x2;
                        saved_y1 = y2;
                        return RunResult.RunStatus.OK;
                    }
                case "LineTo":
                    {
                        if (!BCObjectUtilities.CheckArgs(2, 2, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        // if (!await BCObjectUtilities.CheckArgValue(0, "X1", 1, 25, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;


                        var x2 = (await ArgList[0].EvalAsync(context)).AsDouble;
                        var y2 = (await ArgList[1].EvalAsync(context)).AsDouble;

                        if (!Double.IsNaN(saved_x1) && !Double.IsNaN(saved_y1))
                        {
                            Line(saved_x1, saved_y1, x2, y2);
                        }
                        saved_x1 = x2;
                        saved_y1 = y2;
                        return RunResult.RunStatus.OK;
                    }
                case "Polygon":
                    {
                        if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        // if (!await BCObjectUtilities.CheckArgValue(0, "X1", 1, 25, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;


                        var shape = new GraphicsPrimitivePolygon(uiCanvas, States.Peek(), H, W);
                        Retval.AsObject = shape;
                        return RunResult.RunStatus.OK;
                    }
                case "Pop":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    Pop();
                    return RunResult.RunStatus.OK;
                case "Push":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    Push();
                    return RunResult.RunStatus.OK;

                case "Rectangle":
                    {
                        if (!BCObjectUtilities.CheckArgs(4, 4, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        // if (!await BCObjectUtilities.CheckArgValue(0, "X1", 1, 25, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;

                        var x1 = (await ArgList[0].EvalAsync(context)).AsDouble;
                        var y1 = (await ArgList[1].EvalAsync(context)).AsDouble;
                        var x2 = (await ArgList[2].EvalAsync(context)).AsDouble;
                        var y2 = (await ArgList[3].EvalAsync(context)).AsDouble;
                        //Rectangle(x1, y1, x2, y2);
                        var shape = new GraphicsPrimitiveRectangle(uiCanvas, States.Peek(), H, W, x1, y1, x2, y2);
                        saved_x1 = x2;
                        saved_y1 = y2;
                        Retval.AsObject = shape;
                        return RunResult.RunStatus.OK;
                    }
                case "SetPosition":
                    {
                        if (!BCObjectUtilities.CheckArgs(2, 2, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var x = (await ArgList[0].EvalAsync(context)).AsDouble;
                        var y = (await ArgList[1].EvalAsync(context)).AsDouble;
                        SetPosition(x, y);
                        return RunResult.RunStatus.OK;
                    }
                case "SetSize":
                    {
                        if (!BCObjectUtilities.CheckArgs(2, 2, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var h = (await ArgList[0].EvalAsync(context)).AsDouble;
                        var w = (await ArgList[1].EvalAsync(context)).AsDouble;
                        SetSize(h, w);
                        return RunResult.RunStatus.OK;
                    }
                case "SetMoved":
                    {
                        if (!BCObjectUtilities.CheckArgs(1, 2, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var function = (await ArgList[0].EvalAsync(context)).AsString;
                        var arg = ArgList.Count > 1 ? (await ArgList[1].EvalAsync(context)) : new BCValue();

                        var td = new PointerData() { Context = context, FunctionName = function, Arg = arg };
                        OnMovedCall.Add(td);
                        return RunResult.RunStatus.OK;
                    }

                case "SetPressed":
                case "SetTapped": // SetTapped is the old name
                    // g.SetTapped ("MyTapFunction", "arg")
                    {
                        if (!BCObjectUtilities.CheckArgs(1, 2, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var function = (await ArgList[0].EvalAsync(context)).AsString;
                        var arg = ArgList.Count > 1 ? (await ArgList[1].EvalAsync(context)) : new BCValue();

                        var td = new PointerData() { Context = context, FunctionName = function, Arg =arg };
                        OnPressedCall.Add(td);
                        return RunResult.RunStatus.OK;
                    }
                case "SetReleased":
                    {
                        if (!BCObjectUtilities.CheckArgs(1, 2, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var function = (await ArgList[0].EvalAsync(context)).AsString;
                        var arg = ArgList.Count > 1 ? (await ArgList[1].EvalAsync(context)) : new BCValue();

                        var td = new PointerData() { Context = context, FunctionName = function, Arg = arg };
                        OnReleasedCall.Add(td);
                        return RunResult.RunStatus.OK;
                    }

                case "Slider":
                    {
                        if (!BCObjectUtilities.CheckArgs(6, 6, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;

                        var x1 = (await ArgList[0].EvalAsync(context)).AsDouble;
                        var y1 = (await ArgList[1].EvalAsync(context)).AsDouble;
                        var x2 = (await ArgList[2].EvalAsync(context)).AsDouble;
                        var y2 = (await ArgList[3].EvalAsync(context)).AsDouble;
                        var txt = (await ArgList[4].EvalAsync(context)).AsString;
                        var fnc = (await ArgList[5].EvalAsync(context)).AsString;

                        var shape = new GraphicsPrimitiveSlider(context, uiCanvas, States.Peek(), H, W, x1, y1, x2, y2, txt, fnc);
                        saved_x1 = x2;
                        saved_y1 = y2;
                        Retval.AsObject = shape;
                        return RunResult.RunStatus.OK;
                    }
                case "Text":
                    {
                        if (!BCObjectUtilities.CheckArgs(6, 6, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;

                        var x1 = (await ArgList[0].EvalAsync(context)).AsDouble;
                        var y1 = (await ArgList[1].EvalAsync(context)).AsDouble;
                        var x2 = (await ArgList[2].EvalAsync(context)).AsDouble;
                        var y2 = (await ArgList[3].EvalAsync(context)).AsDouble;
                        var txt = (await ArgList[4].EvalAsync(context)).AsString;
                        var size = (await ArgList[5].EvalAsync(context)).AsDouble;

                        var shape = new GraphicsPrimitiveText(uiCanvas, States.Peek(), H, W, x1, y1, x2, y2, txt, size);
                        saved_x1 = x2;
                        saved_y1 = y2;
                        Retval.AsObject = shape;
                        return RunResult.RunStatus.OK;
                    }

                    //
                    // The scale methods: SetScale SetScaleWindows UseScale
                    //
                case "SetScale":
                    {
                        if (!BCObjectUtilities.CheckArgs(5, 5, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;

                        var scaleName = (await ArgList[0].EvalAsync(context)).AsString;
                        var x1 = (await ArgList[1].EvalAsync(context)).AsDouble;
                        var y1 = (await ArgList[2].EvalAsync(context)).AsDouble;
                        var x2 = (await ArgList[3].EvalAsync(context)).AsDouble;
                        var y2 = (await ArgList[4].EvalAsync(context)).AsDouble;
                        if (!Maps.ContainsKey(scaleName))
                        {
                            var newMap = new GraphicsMap(States.Peek().Map); // Duplicate the current map
                            Maps.Add(scaleName, newMap);
                        }
                        var map = Maps[scaleName];
                        map.SetMap(x1, y1, x2, y2);
                        return RunResult.RunStatus.OK;
                    }

                case "SetScaleWindow":
                    {
                        if (!BCObjectUtilities.CheckArgs(5, 5, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;

                        var scaleName = (await ArgList[0].EvalAsync(context)).AsString;
                        var x1 = (await ArgList[1].EvalAsync(context)).AsDouble;
                        var y1 = (await ArgList[2].EvalAsync(context)).AsDouble;
                        var x2 = (await ArgList[3].EvalAsync(context)).AsDouble;
                        var y2 = (await ArgList[4].EvalAsync(context)).AsDouble;
                        if (!Maps.ContainsKey (scaleName))
                        {
                            var newMap = new GraphicsMap(States.Peek().Map); // Duplicate the current map
                            Maps.Add(scaleName, newMap);
                        }
                        var map = Maps[scaleName];
                        map.SetMapWindow(x1, y1, x2, y2);
                        return RunResult.RunStatus.OK;
                    }

                case "UseScale":
                    {
                        if (!BCObjectUtilities.CheckArgs(1, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;

                        var scaleName = (await ArgList[0].EvalAsync(context)).AsString;
                        if (!Maps.ContainsKey(scaleName))
                        {
                            Retval.SetError(32, $"No such map {scaleName}");
                            return RunResult.RunStatus.ErrorContinue;
                        }
                        else
                        {
                            var state = States.Peek();
                            var map = Maps[scaleName];
                            state.Map = map;
                        }
                        return RunResult.RunStatus.OK;
                    }

                    //
                    // The Graph routines: Update GraphY GraphXY
                    //
                case "Update":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    Update();
                    return RunResult.RunStatus.OK;
                case "GraphXY": // Array with X and Y values
                    {
                        if (!BCObjectUtilities.CheckArgs(1, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var data = (await ArgList[0].EvalAsync(context)).AsArray;
                        var autograph = new AutoGraphXY(data);
                        AutoGraphs.Add(autograph);
                        autograph.Display(this);
                        return RunResult.RunStatus.OK;
                    }
                case "GraphY": // Array with just Y values (X values are the index)
                    {
                        if (!BCObjectUtilities.CheckArgs(1, 2, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var data = (await ArgList[0].EvalAsync(context)).AsArray;
                        var graphName = "";
                        if (ArgList.Count > 1) graphName = (await ArgList[1].EvalAsync(context)).AsString;
                        AutoGraph autograph = null;
                        if (graphName != "")
                        {
                            if (AutoGraphNames.ContainsKey (graphName))
                            {
                                int graphIndex = AutoGraphNames[graphName];
                                if (graphIndex < AutoGraphs.Count)
                                {
                                    autograph = AutoGraphs[graphIndex];
                                    autograph.UpdateData(this, data);
                                }
                                else
                                {
                                    ;
                                }
                                // NOTE: else what happens?
                            }
                            else
                            {
                                // Add to the list of autographs with names
                                autograph = new AutoGraphY(data);
                                AutoGraphs.Add(autograph);
                                int graphIndex = AutoGraphs.Count - 1;
                                AutoGraphNames[graphName] = graphIndex;
                                autograph.Display(this);
                            }
                        }
                        else
                        {
                            // Add to the list of autographs
                            autograph = new AutoGraphY(data);
                            AutoGraphs.Add(autograph);
                            autograph.Display(this);
                        }
                        return RunResult.RunStatus.OK;
                    }
                default:
                    await Task.Delay(0);
                    BCValue.MakeNoSuchMethod(Retval, this, name);
                    return RunResult.RunStatus.ErrorContinue;
            }
        }

        public void RunComplete()
        {
        }

        public void SetValue(string name, BCValue value)
        {
            switch (name)
            {
                case "Background":
                    {
                        var color = new BCColor(value.AsString);
                        MustSetBackground = false; // this take priority.
                        uiBackground.Background = color.Brush;
                        break;
                    }
                case "Border":
                    {
                        var color = new BCColor(value.AsString);
                        uiBackground.BorderBrush = color.Brush;
                        break;
                    }
                case "Fill":
                    {
                        var g = States.Peek();
                        if (value.CurrentType == BCValue.ValueType.IsDouble)
                        {
                            var color = new BCColor(value.AsInt);
                            g.Fill = color.Brush;
                        }
                        else
                        {
                            var color = new BCColor(value.AsString);
                            g.Fill = color.Brush;
                        }
                        break;
                    }
                case "Footer":
                    uiFoot.Text = value.AsString;
                    uiFoot.Foreground = States.Peek().Stroke;
                    break;
                case "Stroke":
                    {
                        var g = States.Peek();
                        if (value.CurrentType == BCValue.ValueType.IsDouble)
                        {
                            var color = new BCColor(value.AsInt);
                            g.Stroke = color.Brush;
                        }
                        else
                        {
                            var color = new BCColor(value.AsString);
                            g.Stroke = color.Brush;
                        }
                        break;
                    }
                case "Thickness":
                    {
                        var g = States.Peek();
                        g.BrushWidth = value.AsDouble;
                        break;
                    }
                case "Title":
                    uiTitle.Text = value.AsString;
                    uiTitle.Foreground = States.Peek().Stroke;
                    break;
                case "XAxisVisible":
                    {
                        var g = States.Peek();
                        g.GraphOptions.XAxisVisible = value.AsDoubleToBoolean;
                        break;
                    }
                case "YAxisMax":
                    {
                        var g = States.Peek();
                        g.GraphOptions.YAxisMax = value.AsDouble;
                        break;
                    }
                case "YAxisMin":
                    {
                        var g = States.Peek();
                        g.GraphOptions.YAxisMin = value.AsDouble;
                        break;
                    }
            }
        }

        class PointerData
        {
            public string FunctionName { get; set; }
            public BCValue Arg { get; set; }
            public BCRunContext Context { get; set; }
        }
        List<PointerData> OnMovedCall = new List<PointerData>();
        List<PointerData> OnPressedCall = new List<PointerData>();
        List<PointerData> OnReleasedCall = new List<PointerData>();

        private void DoPointerCall(List<PointerData>  functions, PointerRoutedEventArgs e)
        {
            var pos = e.GetCurrentPoint(uiCanvas);
            if (functions.Count > 0) e.Handled = true;
            foreach (var call in functions)
            {
                if (call.FunctionName != "" && call.Context != null)
                {
                    var arglist = new List<IExpression>() {
                                new ObjectConstant(this),
                                new NumericConstant(pos.Position.X),
                                new NumericConstant(uiCanvas.Height - pos.Position.Y),
                                new ObjectConstant(call.Arg)
                            };
                    call.Context.ProgramRunContext.AddCallback(call.Context, call.FunctionName, arglist);
                }
            }
        }

        private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            DoPointerCall(OnMovedCall, e);
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            DoPointerCall(OnPressedCall, e);
        }

        private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            DoPointerCall(OnReleasedCall, e);
        }
    }
}
