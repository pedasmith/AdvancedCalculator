using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Media3D;

namespace Graphics
{
    public class Camera
    {
        public Canvas Canvas { get; set; }
        public MyPoint Position { get; set; }
        public Matrix3D WorldToCamera { get; set; }
        public double canvasWidth { get; set; } = 1;
        public double canvasHeight { get; set; } = 1;

        public double screenWidth { get { return Canvas.ActualWidth; } }
        public double screenHeight { get { return Canvas.ActualHeight; } }

        public Graph3DContainer PointTo { get; set; } = null;
        public MyPoint PointToWorldPosition { get; set; } = null;

        // Camera normally has a camera-to-world matrix; invert is to get worldToCamera.
        // That should only be done once!
        public MyPoint ConvertWorldToScreen(MyPoint p)
        {
            MyPoint Retval = new MyPoint();
            var pWorld = p.AsMatrix3D();
            var pCamera = pWorld* WorldToCamera;

            Retval.X = pCamera.OffsetZ == 0 ? 0 : pCamera.OffsetX / pCamera.OffsetZ;
            Retval.Y = pCamera.OffsetZ == 0 ? 0 : pCamera.OffsetY / -pCamera.OffsetZ;
            Retval.Z = 1; // 1==visible 0=not visible
            if (pCamera.OffsetZ == 0 || Math.Abs(Retval.X) > canvasWidth || Math.Abs(Retval.Y) > canvasHeight)
            {
                Retval.Z = 0;
                // May as well continue with the calculations.
                // That way we can show "ghost" values and calculations.
            }
            // Normalize to be 0..1
            Retval.X = (Retval.X + canvasWidth / 2.0) / canvasWidth;
            Retval.Y = (Retval.Y + canvasHeight / 2.0) / canvasHeight;

            // Convert to screen co-ords
            Retval.X = Retval.X * screenWidth;
            Retval.Y = Retval.Y * screenHeight;
            Retval.Z = p.Z;

            return Retval;
        }

        public void Display()
        {
            if (Canvas == null) return;
        }

        public void SetPosition(double x, double y, double z)
        {
            Matrix3D newPosition = Matrix3D.Identity;
            newPosition.OffsetX = x;
            newPosition.OffsetY = y;
            newPosition.OffsetZ = z;
            WorldToCamera = newPosition;

            Position = MyPoint.From(WorldToCamera);
        }

        public void UpdatePointTo()
        {
            if (PointTo != null && PointTo.Points != null && PointTo.Points.Length >= 1)
            {
                // From PointTo generate the PointToWorldPosition value
                var p = PointTo.ConvertLocalToWorld(PointTo.Points[0]);
                PointToWorldPosition = p;
            }
            if (PointToWorldPosition == null) return; // Nothing to point to.

            var rot = Matrix3D.Identity;
            // Calculating the point-at is a little tricky.
            // I used the math from https://www.fastgraph.com/makegames/3drotation/

            if (Position == null) Position = MyPoint.From(WorldToCamera);

            var deltaX = PointToWorldPosition.X - Position.X;
            var deltaY = PointToWorldPosition.Y - Position.Y;
            var deltaZ = PointToWorldPosition.Z - Position.Z;

            deltaX = -deltaX;
            deltaY = -deltaY;
            deltaZ = -deltaZ;

            // M31 to M33 is the Out vector
            var outHat = VectorUtilities.Normalize(deltaX, deltaY, deltaZ);
            rot.M31 = outHat[0];
            rot.M32 = outHat[1];
            rot.M33 = outHat[2];

            // The up vector.  In practice this is never changed.
            var upw = new double[3] { 0.0, 1.0, 0.0 };

            //var d = upX * m.M31 + upY * m.M32 + upZ * m.M33;
            var d = VectorUtilities.Dot(outHat, upw);
            var upHat = VectorUtilities.Normalize(upw[0] - d * outHat[0], upw[1] - d * outHat[1], upw[2] - d * outHat[2]);
            rot.M21 = upHat[0];
            rot.M22 = upHat[1];
            rot.M23 = upHat[2];

            // Right vector is UP x OUT (cross product)
            // cross product is a × b = {aybz - azby ; azbx - axbz ; axby - aybx}.
            var right = VectorUtilities.Cross(upHat, outHat);
            rot.M11 = right[0];
            rot.M12 = right[1];
            rot.M13 = right[2];

            //rot.OffsetX = rot.OffsetX;
            //rot.OffsetY = rot.OffsetY;
            //rot.OffsetZ = rot.OffsetZ;

            var tr = Matrix3D.Identity;
            tr.OffsetX = Position.X;
            tr.OffsetY = Position.Y;
            tr.OffsetZ = Position.Z;

            var m = (rot * tr);
            m.Invert();
            WorldToCamera = m;
        }
    }
}
