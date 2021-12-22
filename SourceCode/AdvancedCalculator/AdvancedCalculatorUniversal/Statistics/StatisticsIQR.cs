using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Statistics
{
    public class StatisticsIQR
    {
        public StatisticsIQR() { Clear(); }
        public StatisticsIQR(IList<double> data)
        {
            Clear();
            Set(data);
        }
        public double P90 { get; set; }
        public double Q3 { get; set; }
        public double Median { get; set; }
        public double Q1 { get; set; }
        public double P10 { get; set; }
        public double IQR { get { return Q3 - Q1; } }

        public double MinValue { get; set; }
        public double MaxValue { get; set; }

        public double LowFence { get { return Q1 - 1.5 * IQR; } }
        public double HighFence { get { return Q3 + 1.5 * IQR; } }

        public double MinValueInsideFence { get; set; }
        public double MaxValueInsideFence { get; set; }

        public bool AllRegularNumbers // no NaN or Infinity
        {
            get
            {
                bool retval = true;
                if (double.IsInfinity(P90) || double.IsNaN(P90)) retval = true;
                if (double.IsInfinity(Q3) || double.IsNaN(Q3)) retval = true;
                if (double.IsInfinity(Median) || double.IsNaN(Median)) retval = true;
                if (double.IsInfinity(Q1) || double.IsNaN(Q1)) retval = true;
                if (double.IsInfinity(P10) || double.IsNaN(P10)) retval = true;
                if (double.IsInfinity(MinValue) || double.IsNaN(MinValue)) retval = true;
                if (double.IsInfinity(MaxValue) || double.IsNaN(MaxValue)) retval = true;
                if (double.IsInfinity(MinValueInsideFence) || double.IsNaN(MinValueInsideFence)) retval = true;
                if (double.IsInfinity(MaxValueInsideFence) || double.IsNaN(MaxValueInsideFence)) retval = true;

                return retval;
            }
        }

        public override string ToString()
        {
            return ToString(new AdvancedCalculator.D2S());
        }

        public string ToString(AdvancedCalculator.IFormatDouble d2s)
        {
            return $"P90\t{d2s.DoubleToString(P90)}\nQ3\t{d2s.DoubleToString(Q3)}\nMedian\t{d2s.DoubleToString(Median)}\nQ1\t{d2s.DoubleToString(Q1)}\nP10\t{d2s.DoubleToString(P10)}\n";
        }


        public void Clear()
        {
            P90 = 0;
            Q3 = 0;
            Median = 0;
            Q1 = 0;
            P10 = 0;
            MinValue = 0;
            MaxValue = 0;
            MinValueInsideFence = 0;
            MaxValueInsideFence = 0;
        }

        List<double> Data = new List<double>();
        //Solvers.StatisticsX StatX = new Solvers.StatisticsX();

        public void Set(IList<double> data)
        {
            Data.Clear();
            foreach (var item in data)
            {
                Data.Add(item);
            }
            Data.Sort();

            //
            // Calculations
            //
            MaxValue = Data[Data.Count - 1];
            MinValue = Data[0];
            P90 = Interpolate(0.9 * (Data.Count - 1));
            Q3 = Interpolate(0.75 * (Data.Count - 1));
            Median = Interpolate(0.5 * (Data.Count - 1));
            Q1 = Interpolate(0.25 * (Data.Count - 1));
            P10 = Interpolate(0.1 * (Data.Count - 1));

            // Now figure out the Low/HighValueInsideFence values
            bool done = false;
            for (int i = 0; i < Data.Count - 1 && !done; i++)
            {
                if (Data[i] >= LowFence)
                {
                    MinValueInsideFence = Data[i];
                    done = true;
                }
            }
            if (!done)
            {
                MinValueInsideFence = Q1; // Default to a usable value.
                System.Diagnostics.Debug.WriteLine("ERROR: IQR: no low fence value");
            }

            done = false;
            for (int i = Data.Count - 1; i >= 0 && !done; i--)
            {
                if (Data[i] <= HighFence)
                {
                    MaxValueInsideFence = Data[i];
                    done = true;
                }
            }
            if (!done)
            {
                MaxValueInsideFence = Q3; // Default to a usable value.
                System.Diagnostics.Debug.WriteLine("ERROR: IQR: no high fence value");
            }
        }



        // Example: data is [0,1] and index is .1.  Return .1
        private double Interpolate(double index)
        {
            int lowerIndex = (int)Math.Floor(index);
            double lower = Data[lowerIndex];
            double higher = Data[(int)Math.Ceiling(index)];
            // Note that lower can equal higher is index is already an integer
            double ratio = (index - lowerIndex); // divided by the difference between higher and lower, but that can be zero.  
            // The different will be zero or one, and in the zero case we don't care.
            double dataDelta = higher - lower;
            double Retval = lower + dataDelta * ratio;
            return Retval;
        }

        private static bool Approx(double d1, double d2)
        {
            var delta = Math.Abs(d1 - d2);
            var retval = delta < 0.000001;
            return retval;
        }
        private int Test_Interpolate_One(double index, double expected)
        {
            int nerror = 0;
            double actual = Interpolate(index);
            if (!Approx(actual, expected))
            {
                nerror += 1;
                System.Diagnostics.Debug.WriteLine($"Error: ColumnStats.Interpolate ({index}) expected {expected} but got {actual} instead.");
            }
            return nerror;
        }

        private static int Test_Interpolate()
        {
            int nerror = 0;

            var stats = new StatisticsIQR();
            stats.Set(new List<double>() { 0, 1 });

            nerror += stats.Test_Interpolate_One(0, 0);
            nerror += stats.Test_Interpolate_One(1, 1);
            nerror += stats.Test_Interpolate_One(.5, .5);
            nerror += stats.Test_Interpolate_One(.1, .1);
            return nerror;
        }

        private static int Test_IQR_One(IList<double> list, double eP90, double eQ3, double eMedian, double eQ1, double eP10)
        {
            int nerror = 0;
            var stats = new StatisticsIQR();
            stats.Set(list);

            if (!Approx(stats.P90, eP90))
            {
                nerror += 1;
                System.Diagnostics.Debug.WriteLine($"Error: Stats: P90 expected {eP90} actual was {stats.P90}");
            }

            if (!Approx(stats.Q3, eQ3))
            {
                nerror += 1;
                System.Diagnostics.Debug.WriteLine($"Error: Stats: Q3 expected {eQ3} actual was {stats.Q3}");
            }

            if (!Approx(stats.Median, eMedian))
            {
                nerror += 1;
                System.Diagnostics.Debug.WriteLine($"Error: Stats: Median expected {eMedian} actual was {stats.Median}");
            }

            if (!Approx(stats.Q1, eQ1))
            {
                nerror += 1;
                System.Diagnostics.Debug.WriteLine($"Error: Stats: Q1 expected {eQ1} actual was {stats.Q1}");
            }

            if (!Approx(stats.P10, eP10))
            {
                nerror += 1;
                System.Diagnostics.Debug.WriteLine($"Error: Stats: P10 expected {eP10} actual was {stats.P10}");
            }


            return nerror;
        }


        private static int Test_IQR()
        {
            int nerror = 0;
            nerror += Test_IQR_One(new List<double>() { 0, 1, 2, 3, 4, 5 }, 4.5, 3.75, 2.5, 1.25, 0.5);
            nerror += Test_IQR_One(new List<double>() { 102, 104, 105, 107, 108, 109, 110, 112, 115, 116, 118 }, 116, 113.5, 109, 106, 104);


            return nerror;
        }
        public static int Test()
        {
            int nerror = 0;
            nerror += Test_Interpolate();
            nerror += Test_IQR();
            nerror += DrawRange.Test();
            if (nerror > 0)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: Testing column stats: nerror is {nerror}");
            }
            return nerror;
        }

    }

}
