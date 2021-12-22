using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Media3D;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Graphics
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        Graph3DWorld World { get; set; } = new Graph3DWorld();
        //Graph3DContainer g;
        //Camera cam = null;
        public MainPage()
        {
            this.InitializeComponent();
            var gtest = new GraphicTest();
            int nerror = gtest.Test();
            this.Loaded += MainPage_Loaded;
        }

        private void DrawABunchOfStuff()
        {
            //g = new Graph3DContainer();
            //CommonFigures.Triangle(g);
            CommonFigures.Flower(World.WorldContainer, 3.0, 4);
            CommonFigures.Axes(World.WorldContainer, 50);

            var pot = new Graph3DContainer();
            pot.SetPosition(12, -0.5, 12);
            CommonFigures.Flower(pot, 1.5, 17);
            World.WorldContainer.Add(pot);
            World.WorldContainer.Add(new Graph3DLine(new MyPoint(0, 0, 0), new MyPoint(12, -0.5, 12)));

            World.WorldContainer.Add(new Graph3DCylinder(20.0, 20.0, 16.0, 13));

            //cam = new Camera();
            //World.Camera.screenWidth = uiGraphics.ActualWidth;
            //World.Camera.screenHeight = uiGraphics.ActualHeight;
            //World.Camera.canvasWidth = 3;
            //World.Camera.canvasHeight = 3;
            World.Camera.Canvas = uiGraphics;
        }

        private void DrawExample3(Graph3DWorld world)
        {
            //g = new Graph3DContainer();
            world.WorldContainer.Add(new Graph3DLine(new MyPoint(-30, 0, 0), new MyPoint(30, 0, 0)));
            world.WorldContainer.Add(new Graph3DLine(new MyPoint(-30, 0, 10), new MyPoint(30, 0, 10)));
            world.WorldContainer.Add(new Graph3DLine(new MyPoint(-30, 0, 20), new MyPoint(30, 0, 20)));
            world.WorldContainer.Add(new Graph3DLine(new MyPoint(-30, 0, 30), new MyPoint(30, 0, 30)));
            world.WorldContainer.Add(new Graph3DLine(new MyPoint(-30, 0, 40), new MyPoint(30, 0, 40)));

            var p1 = new Graph3DContainer();
            world.WorldContainer.Add(p1);
            var c1 = p1.Add(new Graph3DCylinder(7, 7, 5, 21));
            p1.SetPosition(0, 0, 0);
            c1.Fill = new SolidColorBrush(Colors.LightBlue);

            var p2 = new Graph3DContainer();
            world.WorldContainer.Add(p2);
            var c2 = p2.Add(new Graph3DCylinder(7, 4, 5, 21));
            p2.SetPosition(15, 0, 0);
            c2.Fill = new SolidColorBrush(Colors.DarkBlue);

            var p3 = new Graph3DContainer();
            world.WorldContainer.Add(p3);
            var c3 = p3.Add(new Graph3DCylinder(7, 11, 5, 21));
            p3.SetPosition(15, 0, 15);
            c3.Fill = new SolidColorBrush(Colors.DarkGreen);

            var p4 = new Graph3DContainer();
            world.WorldContainer.Add(p4);
            var c4 = p4.Add(new Graph3DCylinder(7, 0, 5, 21));
            p4.SetPosition(0, 0, 15);
            c4.Fill = new SolidColorBrush(Colors.LightGreen);

            world.Camera = new Camera();
            //world.Camera.screenWidth = uiGraphics.ActualWidth;
            //world.Camera.screenHeight = uiGraphics.ActualHeight;
            //world.Camera.canvasWidth = 3;
            //world.Camera.canvasHeight = 3;
            world.Camera.PointTo = p4; // Gotta point to something. Point to the light green cylinder.
            world.Camera.Canvas = uiGraphics;
        }

        private void DrawExample4(Graph3DWorld world)
        {
            world.Camera = new Camera();
            var p1 = new Graph3DContainer();
            world.WorldContainer.Add(p1);
            var c1 = p1.Add(new Graph3DCylinder(5, 0, 10, 21));
            p1.SetPosition(0, 0, 0);
            c1.Fill = new SolidColorBrush(Colors.LightBlue);
            p1.Rotate(Graph3DContainer.Axis.Z, Math.PI / 4.0); // 45 degrees

            world.Camera.PointTo = p1; // Gotta point to something. Point to the light blue cone
            world.Camera.Canvas = uiGraphics;
        }

        private void DrawExample5(Graph3DWorld world)
        {
            world.Camera = new Camera();

            var pcenter = new Graph3DContainer();
            world.WorldContainer.Add(pcenter);
            pcenter.SetPosition(17, 0, 0);
            world.WorldContainer.Update(Graph3DContainer.UpdatePhase.Preupdate);

            var p1 = new Graph3DContainer();
            world.WorldContainer.Add(p1);
            p1.SetPosition(0, 0, 0);
            p1.AddAnimation(new GraphicRotateAnimation(Graph3DContainer.Axis.X, Math.PI / 22));
            var c1 = p1.Add(new Graph3DCylinder(5, 0, 10, 14));
            c1.Fill = new SolidColorBrush(Colors.LightBlue);
            

            var p2 = new Graph3DContainer();
            world.WorldContainer.Add(p2);
            p2.SetPosition(25, 0, 0);
            p2.AddAnimation(new GraphicRotateAnimation(Graph3DContainer.Axis.Y, Math.PI / 33));
            var c2 = p2.Add(new Graph3DCylinder(5, 0, 10, 4));
            c2.Fill = new SolidColorBrush(Colors.MediumOrchid);

            var p3 = new Graph3DContainer();
            world.WorldContainer.Add(p3);
            p3.SetPosition(50, 0, 0);
            p3.AddAnimation(new GraphicRotateAnimation(Graph3DContainer.Axis.Z, Math.PI / 44));
            var c3 = p3.Add(new Graph3DCylinder(5, 0, 10, 14));
            c3.Fill = new SolidColorBrush(Colors.Green);

            world.Camera.PointTo = pcenter; // Gotta point to something. Point to the light blue cone
            world.Camera.Canvas = uiGraphics;
        }

        private void DrawExample6(Graph3DWorld world)
        {
            world.Camera = new Camera();

            var pcenter = new Graph3DContainer();
            world.WorldContainer.Add(pcenter);
            pcenter.SetPosition(0, 0, 0);
            world.WorldContainer.Update(Graph3DContainer.UpdatePhase.Preupdate);

            var joint = new Graph3DContainer();
            world.WorldContainer.Add(joint);

            var p1 = new Graph3DContainer();
            joint.Add(p1);
            p1.SetPosition(0, 0, 0);
            //p1.AddAnimation(new GraphicRotateAnimation(Graph3DContainer.Axis.X, Math.PI / 22));
            int NX = 12;
            int NZ = 12;
            double MAXHEIGHT = 20.0;
            double NCYCLE = 2.0;
            double DAMPING = 2.0;
            double[][] values = new double[NZ][];
            for (int z = 0; z < NZ; z++)
            {
                values[z] = new double[NX];
                for (int x = 0; x < NX; x++)
                {
                    var dx = 2.0 * (0.5 - ((double)x / (double)NX));
                    var dz = 2.0 * (0.5 - ((double)z / (double)NZ));
                    var d = Math.Sqrt(dx * dx + dz * dz);
                    var ang = d * NCYCLE * Math.PI;
                    var rawh = Math.Cos(ang);
                    var h = rawh * MAXHEIGHT * Math.Exp(-DAMPING * d); // MAGIC: -3 (must be negative for damping)
                    values[z][x] = h; 
                }
            }
            var c1 = p1.Add(new Graph3DGraph (values, 5.0, 5.0));
            c1.Fill = new SolidColorBrush(Colors.LightBlue);
            p1.AddAnimation(new GraphicRotateAnimation(Graph3DContainer.Axis.Y, 0.1));

            p1.Add(new Graph3DLine(new MyPoint(0, 0, 0), new MyPoint(20, 0, 0)));
            p1.Add(new Graph3DLine(new MyPoint(0, 0, 0), new MyPoint(0, 20, 0)));
            p1.Add(new Graph3DLine(new MyPoint(0, 0, 0), new MyPoint(0, 0, 20)));

            joint.Rotate(Graph3DContainer.Axis.X, -Math.PI/10.0);
            joint.Rotate(Graph3DContainer.Axis.Z, -Math.PI/10.0);

            world.Camera.PointTo = pcenter; 
            world.Camera.Canvas = uiGraphics;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            DrawExample6(World);
            await World.AnimationStartAsync();
            DoDisplay(); // Update the camera coordinates
        }

        private void OnGo(object sender, RoutedEventArgs e)
        {
            DoDisplay();
        }

        private void OnUpdate(object sender, RangeBaseValueChangedEventArgs e)
        {
            DoDisplay();
        }
        private void OnCheckChange(object sender, RoutedEventArgs e)
        {
            DoDisplay();
        }

        private void DoDisplay()
        { 
            if (World.Camera == null) return;

            //var pointTo = g.Find("stem").Points[0]; // point to the bottom of the stem.
            //var pointTo = new MyPoint(20+12, 10, 12+12); // about where the flower is?
            //pointTo = new MyPoint(0.0, 0.0, 0.0);
            World.Camera.SetPosition(uiCameraX.Value, uiCameraY.Value, uiCameraZ.Value);
            //World.WorldContainer.Update(Graph3DContainer.UpdatePhase.Preupdate);
            //if (uiPointAt.IsChecked.Value)
            //{
            //    World.Camera.UpdatePointTo();
            //}
            //uiGraphics.Children.Clear();
            //World.WorldContainer.DrawDisplay(World.Camera, uiGraphics);
        }

    }
}
