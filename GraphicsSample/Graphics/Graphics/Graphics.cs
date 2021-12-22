using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Media3D;
using Windows.UI.Xaml.Shapes;

namespace Graphics
{

    public class Graph3DWorld
    {
        public Graph3DContainer WorldContainer { get; set; } = new Graph3DContainer();
        public Camera Camera { get; set; }
        public GraphicLight Light { get; set; }

        CancellationTokenSource cts = null;
        Task AnimationTask = null;
        public async Task AnimationStartAsync()
        {
            if (AnimationTask != null)
            {
                await AnimationStopAsync();
            }
            cts = new CancellationTokenSource();
            AnimationTask = AnimationLoop(cts.Token);
        }


        public async Task AnimationStopAsync()
        {
            if (AnimationTask == null) return;
            cts.Cancel();
            while (AnimationTask.Status == TaskStatus.RanToCompletion)
            {
                await Task.Delay(25);
            }
            AnimationTask = null;
        }

        private async Task AnimationLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                DoFrame();
                await Task.Delay(50);
            }
        }
        public void DoFrame()
        {
            if (Camera == null || Camera.Canvas == null) return;

            try
            {
                Camera.Canvas.Children.Clear();
                WorldContainer.Update(Graph3DContainer.UpdatePhase.Preupdate);
                WorldContainer.Update(Graph3DContainer.UpdatePhase.Update);
                Camera.UpdatePointTo();
                List<DrawableItem> resultList = new List<DrawableItem>();
                WorldContainer.DrawDisplay(resultList, Light, Camera);
                var slist = from item in resultList orderby item.Z descending select item.Item;
                foreach (var item in slist)
                {
                    Camera.Canvas.Children.Add(item);
                }
            }
            catch (Exception)
            {
                ;
            }
        }
    }

    public class Graph3DContainer: GraphicItem
    {
        public enum Axis {  X, Y, Z};
        public Graph3DContainer Parent { get; set; } = null;

        private Matrix3D RotateMatrix { get; set; } = Matrix3D.Identity;
        private Matrix3D TranslateMatrix { get; set; } = Matrix3D.Identity;

        public Matrix3D WorldToLocal { get; internal set; } = Matrix3D.Identity;
        private void UpdateWorldToLocal()
        {
            var m = (RotateMatrix* TranslateMatrix);
            WorldToLocal = m;
        }
        public Matrix3D LocalToWorld{  get { var inv = WorldToLocal; inv.Invert(); return inv; } }
        public MyPoint ConvertLocalToWorld (MyPoint p)
        {
            Matrix3D point = p.AsMatrix3D();

            Matrix3D result = point * WorldToLocal;
            for (var parent = Parent; parent != null; parent = parent.Parent)
            {
                result = result * parent.WorldToLocal;
            }

            var Retval = MyPoint.From(result);
            return Retval;
        }

        List<GraphicItem> AllItems = new List<GraphicItem>();
        List<GraphicAnimation> AllAnimations = new List<GraphicAnimation>();
        public void Clear()
        {
            AllItems.Clear();
            AllAnimations.Clear();
        }
        public GraphicItem Add(GraphicItem item)
        {
            item.SetGraph3DParent(this);
            AllItems.Add(item);
            return item;
        }

        public GraphicAnimation AddAnimation(GraphicAnimation item)
        {
            AllAnimations.Add(item);
            return item;
        }

        public void DrawDisplay(List<DrawableItem> resultList, GraphicLight light, Camera camera)
        {
            foreach (var item in AllItems)
            {
                DrawOne(resultList, light, camera, item);
            }
        }

        public GraphicItem Find(string name)
        {
            if (this.Name == name) return this;
            foreach (var item in AllItems)
            {
                if (item.Name == name) return item;
                if (item is Graph3DContainer)
                {
                    var potentialFind = (item as Graph3DContainer).Find(name);
                    if (potentialFind != null) return potentialFind;
                }
            }
            return null;
        }

        public void Rotate (Axis axis, double angleInRadians)
        {
            var rot = Matrix3D.Identity;
            switch (axis)
            {
                case Axis.X:
                    rot.M22 = Math.Cos(angleInRadians);
                    rot.M23 = -Math.Sin(angleInRadians);
                    rot.M32 = Math.Sin(angleInRadians);
                    rot.M33 = Math.Cos(angleInRadians);
                    break;
                case Axis.Y:
                    rot.M11 = Math.Cos(angleInRadians);
                    rot.M13 = Math.Sin(angleInRadians);
                    rot.M31 = Math.Sin(angleInRadians);
                    rot.M33 = -Math.Cos(angleInRadians);
                    break;
                case Axis.Z:
                    rot.M11 = Math.Cos(angleInRadians);
                    rot.M12 = -Math.Sin(angleInRadians);
                    rot.M21 = Math.Sin(angleInRadians);
                    rot.M22 = Math.Cos(angleInRadians);
                    break;
            }
            this.RotateMatrix = rot;
            UpdateWorldToLocal();
        }

        public void SetPosition (double x, double y, double z)
        {
            Matrix3D newPosition = Matrix3D.Identity;
            newPosition.OffsetX = x;
            newPosition.OffsetY = y;
            newPosition.OffsetZ = z;
            this.TranslateMatrix = newPosition;
            UpdateWorldToLocal();
        }

        public enum UpdatePhase {  Preupdate, Update }
        public void Update(UpdatePhase phase)
        {
            switch (phase)
            {
                case UpdatePhase.Preupdate:
                    foreach (var item in AllItems)
                    {
                        if (item is Graph3DContainer)
                        {
                            (item as Graph3DContainer).Update(phase);
                        }
                    }
                    if (Points == null || Points.Length != 1)
                    {
                        Points = new MyPoint[1];
                    }
                    Points[0] = MyPoint.From(WorldToLocal);
                    break;
                case UpdatePhase.Update:
                    foreach (var item in AllItems)
                    {
                        if (item is Graph3DContainer)
                        {
                            (item as Graph3DContainer).Update(phase);
                        }
                    }
                    // Now update myself.
                    foreach (var item in AllAnimations)
                    {
                        if (item is GraphicRotateAnimation)
                        {
                            var anim = item as GraphicRotateAnimation;
                            anim.CurrAngleInRadians += anim.AngleInRadians;
                            Rotate(anim.Axis, anim.CurrAngleInRadians);
                        }
                    }
                    break;
            }

        }

        Brush CurrStroke = new SolidColorBrush(Colors.Purple);
        Brush CurrFill = new SolidColorBrush(Colors.LightGoldenrodYellow);
        private void DrawOne(List<DrawableItem> resultList, GraphicLight light, Camera camera, GraphicItem item)
        {
            if (item is Graph3DCylinder)
            {
                (item as Graph3DCylinder).Draw(resultList, light, camera, this, CurrFill);
            }
            if (item is Graph3DGraph)
            {
                (item as Graph3DGraph).Draw(resultList, light, camera, this, CurrFill);
            }
            else if (item is Graph3DLine)
            {
                (item as Graph3DLine).Draw(resultList, this, camera, CurrStroke);
            }
            else if (item is Graph3DContainer)
            {
                (item as Graph3DContainer).DrawDisplay(resultList, light, camera);
            }
        }

        public override void SetGraph3DParent(Graph3DContainer space)
        {
            Parent = space;
        }
    }
}
