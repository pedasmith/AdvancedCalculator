using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Media;
using static Graphics.Graph3DContainer;

namespace Graphics
{
    //TODO: to make cubes, unicorns, etc.
    public class CommonFigures
    {
        public static void Axes(Graph3DContainer g, double length)
        {
            var c0 = new MyPoint(0, 0, 0);
            var p1 = new MyPoint(length, 0, 0);
            var p2 = new MyPoint(0, length, 0);
            var p3 = new MyPoint(0, 0, length);
            g.Add(new Graph3DLine(c0, p1) { Stroke = new SolidColorBrush(Colors.Red) });
            g.Add(new Graph3DLine(c0, p2) { Stroke = new SolidColorBrush(Colors.Green) });
            g.Add(new Graph3DLine(c0, p3) { Stroke = new SolidColorBrush(Colors.Blue) });
        }

        public static void Flower(Graph3DContainer g, double r = 1.0, int npetal = 4)
        {
            var p0 = new MyPoint(0, 0, 0);
            var stemBottom = new MyPoint(20, 0, 10);
            var stemTop = new MyPoint(stemBottom.X, 10, stemBottom.Z);

            for (int petal = 0; petal < npetal; petal++)
            {
                var angle = (double)petal * (Math.PI * 2.0 / (double)npetal);
                var x = r * Math.Cos(angle);
                var z = r * Math.Sin(angle);
                var c0 = new MyPoint(stemTop.X+x, stemTop.Y, stemTop.Z+z);
                g.Add(new Graph3DLine(stemTop, c0));
            }


            g.Add(new Graph3DLine(p0, stemBottom));
            g.Add(new Graph3DLine(stemBottom, stemTop) { Name = "stem" });
        }

        public static void Triangle(Graph3DContainer g)
        {
            var p1 = new MyPoint(10, 10, 0);
            var p2 = new MyPoint(100, 20, 0);
            var p3 = new MyPoint(70, 70, 0);
            g.Add(new Graph3DLine(p1, p2));
            g.Add(new Graph3DLine(p2, p3));
            g.Add(new Graph3DLine(p3, p1));
        }
    }
}
