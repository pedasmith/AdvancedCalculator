using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Media3D;

namespace Graphics
{
    public class MyPoint
    {
        public MyPoint()
        {
            X = 0.0;
            Y = 0.0;
            Z = 0.0;
        }

        public MyPoint(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
        public static MyPoint From(Matrix3D result)
        {
            var Retval = new MyPoint(result.OffsetX, result.OffsetY, result.OffsetZ);
            return Retval;
        }

        public Matrix3D AsMatrix3D()
        {
            Matrix3D Retval = Matrix3D.Identity;
            Retval.OffsetX = X;
            Retval.OffsetY = Y;
            Retval.OffsetZ = Z;
            return Retval;
        }

        public Windows.Foundation.Point AsPoint()
        {
            return new Windows.Foundation.Point(X, Y);
        }

        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public override string ToString()
        {
            return $"[{X:F4}, {Y:F4}, {Z:F4}]";
        }
    }

    public class VectorUtilities
    {
        // cross product is a × b = {aybz - azby ; azbx - axbz ; axby - aybx}.
        public static double[] Cross(double[] A, double[] B)
        {
            return Cross(A[0], A[1], A[2], B[0], B[1], B[2]);
        }

        public static double[] Cross(double ax, double ay, double az, double bx, double by, double bz)
        {
            var Retval = new double[3]
            {
                ay*bz - az*by,
                ax*bz - az*bx,
                ax*by - ay*bx
            };
            return Retval;
        }

        public static double Dot(double[] v1, double[] v2)
        {
            var Retval = v1[0] * v2[0] + v1[1] * v2[1] + v1[2] * v2[2];
            return Retval;
        }

        public static double Length (MyPoint p)
        {
            double length = Math.Sqrt(p.X * p.X + p.Y * p.Y + p.Z * p.Z);
            return length;
        }

        public static double Norm(double x, double y, double z)
        {
            var Retval = Math.Sqrt(x * x + y * y + z * z);
            return Retval;
        }

        public static double[] Normalize(double x, double y, double z)
        {
            var d = Norm(x, y, z);
            if (d == 0) return new double[3] { 0.0, 0.0, 0.0 };
            return new double[3] { x / d, y / d, z / d };
        }

        public static MyPoint PointsToVector(MyPoint p1, MyPoint p2)
        {
            var Retval = new MyPoint(p1.X - p2.X, p1.Y - p2.Y, p1.Z - p2.Z);
            return Retval;
        }


        // Given a list of points which are coplanar but no co-linear,
        // return the normals (as determined by a dot product).
        // Works even if some of the points are overlappings.
        // TODO: always uses the first three points even if that doesn't
        // actuall make sense,
        public static double[] PlanarPointsToNormal (IList<MyPoint> list)
        {
            var v1 = PointsToVector(list[0], list[1]);
            var v2 = PointsToVector(list[0], list[2]);
            var crossnormal = Cross(v1.X, v1.Y, v1.Z, v2.X, v2.Y, v2.Z);
            var normal = Normalize(crossnormal[0], crossnormal[1], crossnormal[2]);
            return normal;
        }
    }

    public class Examples
    {
        public static Matrix3D Example7()
        {
            Matrix3D Retval = new Matrix3D(
                 0.718762, 0.615033, -0.324214, 0,
                -0.393732, 0.744416, 0.539277, 0,
                 0.573024, -0.259959, 0.777216, 0,
                 0.526967, 1.254234, -2.532150, 1
                );
            return Retval;
        }
    }
}
