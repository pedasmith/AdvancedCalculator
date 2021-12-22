using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedCalculator.BCBasic.RunTimeLibrary
{
    // Interpolation requires SortedList which doesn't exist in Windows 8.
    public class InterpolationLibrary
    {
        public InterpolationLibrary()
        {
        }
        public InterpolationLibrary(double x1, double y1, double x2, double y2)
        {
            AddPoint(x1, y1);
            AddPoint(x2, y2);
        }

#if !WINDOWS8
        SortedList<double, double> Points = new SortedList<double, double>();
#else
        SortedDictionary<double, double> Points = new SortedDictionary<double, double>();
#endif
        public void AddPoint(double x, double y)
        {
            if (Points.ContainsKey(x))
            {
                Points[x] = y;
            }
            else
            {
                Points.Add(x, y);
            }
        }

        public double Interpolate (double x)
        {
            if (Points.Count == 0) return x;
            if (Points.Count == 1) return Points.ElementAt(0).Value;
            double x1 = x;
            double y1 = x;
            double x2 = x;
            double y2 = x;
            foreach (var item in Points)
            {
                if (item.Key == x)
                {
                    return item.Value; // Found it!
                }
                else if (item.Key >= x)
                {
                    x2 = item.Key;
                    y2 = item.Value;
                    break;
                }
                else
                {
                    x1 = item.Key;
                    y1 = item.Value;
                    x2 = item.Key;
                    y2 = item.Value;
                }
            }
            // At this point we have x1 < x < x2
            var ratio = (x - x1) / (x2 - x1);
            var y = (ratio * (y2 - y1)) + y1;

            return y;
        }

        public int TestOne(double x, double expected)
        {
            int NError = 0;
            var actual = Interpolate(x);
            var delta = Math.Abs(actual - expected);
            if (delta > 0.001)
            {
                NError++;
                System.Diagnostics.Debug.WriteLine($"ERROR: Interpolation (x) says {actual} but should be {expected}");
            }
            return NError;
        }
        public static int Test()
        {
            int NError = 0;

            var list0 = new InterpolationLibrary();
            NError += list0.TestOne(0, 0);
            NError += list0.TestOne(1, 1);
            NError += list0.TestOne(-3, -3);

            var list1 = new InterpolationLibrary();
            list1.AddPoint(10, 1);
            NError += list1.TestOne(0, 1);
            NError += list1.TestOne(1, 1);
            NError += list1.TestOne(-3, 1);

            var list2 = new InterpolationLibrary();
            list1.AddPoint(0, 0);
            list1.AddPoint(255, 255);
            NError += list2.TestOne(0, 0);
            NError += list2.TestOne(1, 1);
            NError += list2.TestOne(255, 255);
            NError += list2.TestOne(500, 500);

            var list4 = new InterpolationLibrary();
            list4.AddPoint(0, 0);
            list4.AddPoint(255, 255);
            list4.AddPoint(127, 0);
            list4.AddPoint(128, 255);
            NError += list4.TestOne(0, 0);
            NError += list4.TestOne(1, 0);
            NError += list4.TestOne(127.00001, 0.00255);
            NError += list4.TestOne(127.99999, 254.99745);
            NError += list4.TestOne(255, 255);
            NError += list4.TestOne(500, 255);

            return NError;
        }
    }
}
