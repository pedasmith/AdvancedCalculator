using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Statistics
{
    // Given a min and max value, determines a nice display range for the data
    class DrawRange
    {
        public static double NiceNumberInRange(double lowval, double highval)
        {
            if (lowval > highval) return NiceNumberInRange(highval, lowval);
            // x1 < x2, guaranteed

            if (lowval <= 0 && highval >= 0) return 0.0; // zero is an awesome number
            if (lowval <= 0 && highval <= 0) return -NiceNumberInRange(Math.Abs(highval), Math.Abs(lowval));
            if (lowval == highval) return lowval;

            // At this point, lowval and highval are both > 0 and lowval < highval

            var fmt = "00000000000000000000.0000000000000000000";
            var s1 = lowval.ToString(fmt);
            var s2 = highval.ToString(fmt);
            if (s1 == s2) return lowval;

            char[] result = new char[fmt.Length];
            for (int i = 0; i < fmt.Length; i++) result[i] = fmt[i];
            for (int i = 0; i < fmt.Length; i++)
            {
                result[i] = s2[i]; // the bigger one
                if (s1[i] != s2[i])
                {
                    // weirdly, we're all done at this point.
                    break; // all done!
                }
            }
            double Retval = highval;
            bool ok = double.TryParse(new string(result), out Retval);
            if (!ok)
            {
                return highval;
            }
            return Retval;
        }


        private static int TestRangeOne(double v1, double v2, double expected)
        {
            int nerror = 0;
            double actual = NiceNumberInRange(v1, v2);
            if (actual != expected)
            {
                nerror++;
                System.Diagnostics.Debug.WriteLine($"ERROR: NiceNumberInRange ({v1}, {v2}) expected {expected} but got {actual} instead");
            }
            return nerror;
        }

        public static int Test()
        {
            int nerror = 0;

            nerror += TestRangeOne(0, 1, 0);
            nerror += TestRangeOne(2.9, 3.1, 3.0);
            nerror += TestRangeOne(2.92, 2.93, 2.93);
            nerror += TestRangeOne(2.92, 2.93, 2.93);
            nerror += TestRangeOne(2.9277, 2.9377, 2.93);

            nerror += TestRangeOne(-10, 55, 0);

            nerror += TestRangeOne(-2.9, -3.1, -3.0);
            nerror += TestRangeOne(-2.92, -2.93, -2.93);
            nerror += TestRangeOne(-2.92, -2.93, -2.93);
            nerror += TestRangeOne(-2.9277, -2.9377, -2.93);



            //nerror += TestLowOne(-450, -500);
            //nerror += TestLowOne(-850, -1000);
            //nerror += TestLowOne(450, 100);
            //nerror += TestLowOne(850, 500);
            return nerror;
        }
    }

}