using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Graphics
{
    public abstract class GraphicItem
    {
        public string Name = null;
        public MyPoint[] Points = null;
        public Graph3DContainer Space; // NOTE: why is this called Space?
        public Brush Stroke;
        public Brush Fill;
        public virtual void SetGraph3DParent(Graph3DContainer space) { Space = space; }
    }
    // bottom is at y=0, and centered around the point x=0,z=0
    public class Graph3DCylinder : GraphicItem
    {
        // The points is first all the bottom points (the first point is duplicated)
        // then all of the top points (the first point is duplicated)
        // e.g.: when nside=3 (so like a triangle) the points are
        // [bottom] 0 1 2 0 [top] 0 1 2 0
        // There are a total of (nside+1) * 2 points.
        private double BottomRadius;
        private double TopRadius;
        private double Length;
        private int NSides;
        public Graph3DCylinder(double bottomRadius, double topRadius, double length, int nsides)
        {
            BottomRadius = bottomRadius;
            TopRadius = topRadius;
            Length = length;
            NSides = nsides;

            Points = new MyPoint[(nsides+1) * 2]; // angle=0 is duplicated first and last to make the math easier
            for (int side=0; side<nsides; side++)
            {
                double angle = (double)side * (2.0 * Math.PI) / (double)nsides;
                double bottomX = Math.Sin(angle) * BottomRadius;
                double bottomZ = Math.Cos(angle) * BottomRadius;
                double topX = Math.Sin(angle) * TopRadius;
                double topZ = Math.Cos(angle) * TopRadius;
                Points[side] = new MyPoint(bottomX, 0.0, bottomZ);
                Points[side + nsides + 1] = new MyPoint(topX, length, topZ);
            }
            Points[nsides] = Points[0];
            Points[nsides * 2 + 1] = Points[nsides + 1];
        }
        public void Draw(List<DrawableItem> resultList, GraphicLight light, Camera camera, Graph3DContainer g, Brush defaultFill)
        {
            for (int side = 0; side < NSides; side++)
            {
                var worldPoints = new List<MyPoint>();
                var screenPoints = new List<MyPoint>();

                worldPoints.Add (g.ConvertLocalToWorld(Points[side]));
                worldPoints.Add (g.ConvertLocalToWorld(Points[side + 1]));
                worldPoints.Add (g.ConvertLocalToWorld(Points[side +1 + NSides + 1]));
                worldPoints.Add(g.ConvertLocalToWorld(Points[side + NSides + 1]));
                //var sp1 = camera.ConvertWorldToScreen(wp1);
                //var sp2 = camera.ConvertWorldToScreen(wp2);
                //var sp3 = camera.ConvertWorldToScreen(wp3);
                //var sp4 = camera.ConvertWorldToScreen(wp4);
                foreach (var w in worldPoints)
                {
                    screenPoints.Add(camera.ConvertWorldToScreen(w));
                }

                var brush = Fill ?? defaultFill;
                var poly = new Polygon();
                foreach (var s in screenPoints)
                {
                    poly.Points.Add(s.AsPoint());
                }
                var zlist = from s in screenPoints orderby s.Z select s.Z;

                var normal = VectorUtilities.PlanarPointsToNormal(worldPoints);
                var camvec = VectorUtilities.Normalize(-1, -1, 1); //TODO: use the real vector
                var lightStrength = VectorUtilities.Dot(normal, camvec);
                System.Diagnostics.Debug.WriteLine($"strength={lightStrength} [{normal[0]},{normal[1]},{normal[2]}]");
                if (lightStrength > 1)
                {
                    ;
                }
                //NOTE: these colors are picked to validate the lightness...
                byte val = (byte)Math.Min(255, (256 * lightStrength));
                if (lightStrength > 0)
                {
                    brush = new SolidColorBrush(new Windows.UI.Color() { R = val, G = val, B = 0, A = 255 });
                }
                else
                {
                    brush = new SolidColorBrush(new Windows.UI.Color() { R = 0, G = 0, B = val, A = 255 });
                }

                //poly.Points.Add(sp1.AsPoint());
                //poly.Points.Add(sp2.AsPoint());
                //poly.Points.Add(sp4.AsPoint());
                //poly.Points.Add(sp3.AsPoint());
                poly.Fill = brush;
                //canvas.Children.Add(poly);
                resultList.Add(new DrawableItem(zlist.First(), poly));
            }
        }
    }

    public class Graph3DGraph : GraphicItem
    {
        double DeltaX;
        double DeltaZ;
        int NX;
        int NZ;
        public Graph3DGraph(double[][] values, double deltaX, double deltaZ)
        {
            DeltaX = deltaX;
            DeltaZ = deltaZ;
            NX = values.Length;
            NZ = values[0].Length;
            double maxX = DeltaX * NX;
            double maxZ = DeltaZ * NZ;
            Points = new MyPoint[NX * NZ];
            for (int x = 0; x<NX; x++)
            {
                for (int z=0; z<NZ; z++)
                {
                    var value = values[z][x];
                    var index = z * NX + x;
                    Points[index] = new MyPoint(x * deltaX - maxX/2.0, value, z * deltaZ - maxZ/2.0);
                }
            }
        }
        public void Draw(List<DrawableItem> resultList, GraphicLight light, Camera camera, Graph3DContainer g, Brush defaultFill)
        {
            for (int x = 0; x<NX-1; x++)
            {
                for (int z=0; z<NZ-1; z++)
                {
                    var index = z * NX + x;
                    var worldPoints = new List<MyPoint>();
                    worldPoints.Add(g.ConvertLocalToWorld(Points[index]));
                    worldPoints.Add(g.ConvertLocalToWorld(Points[index+1]));
                    worldPoints.Add(g.ConvertLocalToWorld(Points[index+1+NX]));
                    worldPoints.Add(g.ConvertLocalToWorld(Points[index+NX]));

                    var screenPoints = new List<MyPoint>();
                    foreach (var w in worldPoints)
                    {
                        screenPoints.Add(camera.ConvertWorldToScreen(w));
                    }

                    var brush = Fill ?? defaultFill;
                    var poly = new Polygon();
                    foreach (var s in screenPoints)
                    {
                        poly.Points.Add(s.AsPoint());
                    }

                    //
                    // NOTE: this crappy code is shared with the code from Cylinder.
                    // it should be abstracted away.
                    //
                    var zlist = from s in screenPoints orderby s.Z select s.Z;

                    var normal = VectorUtilities.PlanarPointsToNormal(worldPoints);
                    var camvec = VectorUtilities.Normalize(-1, -1, 1); //TODO: use the real vector
                    var lightStrength = VectorUtilities.Dot(normal, camvec);
                    //System.Diagnostics.Debug.WriteLine($"strength={lightStrength} [{normal[0]},{normal[1]},{normal[2]}]");
                    if (lightStrength > 1)
                    {
                        ;
                    }
                    //NOTE: these colors are picked to validate the lightness...
                    byte val = (byte)Math.Min(255, (256 * lightStrength));
                    if (lightStrength > 0)
                    {
                        brush = new SolidColorBrush(new Windows.UI.Color() { R = val, G = val, B = 0, A = 255 });
                    }
                    else
                    {
                        brush = new SolidColorBrush(new Windows.UI.Color() { R = 0, G = 0, B = val, A = 255 });
                    }
                    poly.Fill = brush;
                    resultList.Add(new DrawableItem(zlist.First(), poly));
                }
            }
        }
    }

    public class Graph3DLine : GraphicItem
    {
        public Graph3DLine(MyPoint p1, MyPoint p2)
        {
            Points = new MyPoint[2] { p1, p2 };
        }

        //TODO: cull invisible lines (or draw them funny)
        public void Draw(List<DrawableItem> resultList, Graph3DContainer g, Camera camera, Brush CurrBrush)
        {
            var world = new List<MyPoint>();
            var screen = new List<MyPoint>();

            var l3d = this as Graph3DLine;
            world.Add (g.ConvertLocalToWorld(l3d.Points[0]));
            world.Add (g.ConvertLocalToWorld(l3d.Points[1]));

            //var sp1 = camera.ConvertWorldToScreen(wp1);
            //var sp2 = camera.ConvertWorldToScreen(wp2);

            foreach (var w in world)
            {
                screen.Add(camera.ConvertWorldToScreen(w));
            }
            var zlist = from s in screen orderby s.Z select s.Z;

            var brush = l3d.Stroke ?? CurrBrush;
            var line = new Line()
            {
                X1 = screen[0].X,
                Y1 = screen[0].Y,
                X2 = screen[1].X,
                Y2 = screen[1].Y,
                Stroke = brush
            };
            resultList.Add(new DrawableItem(zlist.First(), line));

            //canvas.Children.Add(line);
        }
    }
    public class Graph3DPoint : GraphicItem
    {
        public Graph3DPoint(MyPoint p1)
        {
            Points = new MyPoint[] { p1 };
        }
    }
}
