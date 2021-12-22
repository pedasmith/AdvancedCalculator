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
    class GraphicsPrimitiveText : GraphicsPrimitiveRectangleShape
    {
        public GraphicsPrimitiveText(Canvas uiCanvas, GraphicsState state, double H, double W, double x1, double y1, double x2, double y2, string text, double size)
            : base(uiCanvas, state, H, W, x1, y1, x2, y2)
        {
            // Do the bits that make is a rectangle.
            var dX1 = state.XY2X(x1, y1, H, W);
            var dY1 = state.XY2Y(x1, y1, H, W);
            var dX2 = state.XY2X(x2, y2, H, W);
            var dY2 = state.XY2Y(x2, y2, H, W);

            text = BCValue.ReplaceNewline(text);
            fe = new TextBlock()
            {
                Text = text,
                FontSize = size,
                Width = Math.Abs(dX2 - dX1),
                Height = Math.Abs(dY2 - dY1),
                Foreground = state.Fill,
            };
            uiCanvas.Children.Add(fe);
            Canvas.SetLeft(fe, Math.Min(dX1, dX2));
            Canvas.SetTop(fe, Math.Min(dY1, dY2));
        }

        public override string PreferredName { get { return "Text"; } }

        public override void Dispose()
        {
        }

        public override IList<string> GetNames()
        {
            var theseMethods = new List<string>() { "FontSize", "Text" };
            var allMethods = theseMethods.Concat(base.GetNames()).ToList();
            return allMethods;
        }

        public override BCValue GetValue(string name)
        {
            switch (name)
            {
                case "FontSize": return new BCValue((fe as TextBlock).FontSize);
                case "Text": return new BCValue((fe as TextBlock).Text);
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
                case "Alignment":
                    {
                        var halign = HorizontalAlignment.Center;
                        var valign = VerticalAlignment.Center;
                        var talign = TextAlignment.Left;
                        var str = value.ToString();
                        if (str.Length != 2) return;
                        switch (str[0])
                        {
                            case 'C': halign = HorizontalAlignment.Center; talign = TextAlignment.Center; break;
                            case 'L': halign = HorizontalAlignment.Left; talign = TextAlignment.Left; break;
                            case 'R': halign = HorizontalAlignment.Right; talign = TextAlignment.Right; break;
                            case 'S': halign = HorizontalAlignment.Stretch; talign = TextAlignment.Justify; break;
                        }
                        switch (str[1])
                        {
                            case 'C': valign = VerticalAlignment.Center; break;
                            case 'T': valign = VerticalAlignment.Top; break;
                            case 'B': valign = VerticalAlignment.Bottom; break;
                            case 'S': valign = VerticalAlignment.Stretch; break;
                        }
                        (fe as TextBlock).HorizontalAlignment = halign;
                        (fe as TextBlock).TextAlignment = talign;
                        (fe as TextBlock).VerticalAlignment = valign;
                    }
                    break;
                case "FontSize":
                    (fe as TextBlock).FontSize = value.AsDouble;
                    break;
                case "Text":
                    (fe as TextBlock).Text = BCValue.ReplaceNewline (value.AsString);
                    break;
                default:
                    base.SetValue(name, value);
                    break;
            }

        }
    }
}
