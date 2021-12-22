using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphics
{
    class GraphicTest
    {
        private static bool Approx(double d1, double d2)
        {
            var delta = Math.Abs (d1 - d2);
            bool Retval = delta < 0.01;
            return Retval;
        }
        public int TestExample7()
        {
            int NError = 0;
            Graph3DContainer gworld = new Graph3DContainer();
            Graph3DContainer local = new Graph3DContainer();
            local.Parent = gworld;
            local.WorldToLocal = Examples.Example7();
            var mp = new MyPoint(-0.5, 0.5, -0.5);
            var wp = local.ConvertLocalToWorld(mp);

            if (!Approx(wp.X, -0.31))
            {
                NError += 1;
                Debug.WriteLine($"ERROR: Example7: X s.b. {0.31} is {wp.X}");
            }

            if (!Approx(wp.Y, 1.44))
            {
                NError += 1;
                Debug.WriteLine($"ERROR: Example7: Y s.b. {1.44} is {wp.X}");
            }

            if (!Approx(wp.Z, -2.49))
            {
                NError += 1;
                Debug.WriteLine($"ERROR: Example7: Z s.b. {-2.49} is {wp.X}");
            }


            return NError;
        }


        public int Test()
        {
            int NError = 0;
            NError += TestExample7();
            return NError;
        }
    }
}
