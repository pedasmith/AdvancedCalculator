using System;
using System.Collections.Generic;

namespace Statistics
{
    class TTestResults
    {
        public string TestName { get; set; }
        public double t { get; set; } // t-statistic
        public double df { get; set; } // degrees of freedom
        public double p { get; set; }
        public double desiredPvalue { get; set; }
        public string explanation { get; set; }

        public override string ToString()
        {
            return ToString(new AdvancedCalculator.D2S());
        }
        public string ToString(AdvancedCalculator.IFormatDouble d2s)
        {
            if (p <= desiredPvalue)
            {
                explanation = $"The two values are probably DIFFERENT.\nThe p-value {d2s.DoubleToString(p)} is <= target {d2s.DoubleToString(desiredPvalue)}";
            }
            else
            {
                explanation = $"The two values are possibly the same.\nThe p-value {d2s.DoubleToString(p)} > target {d2s.DoubleToString(desiredPvalue)}";
            }

            return $"{explanation}\ntest\t{TestName}\np\t{d2s.DoubleToString(p)}\ndf\t{d2s.DoubleToString(df)}\nt\t{d2s.DoubleToString(t)}\n";
        }
    }

    class StatisticsTTest
    {
        public TTestResults Set(IList<double> x, IList<double> y, double desiredPvalue = 0.05)
        {
            var Retval = new TTestResults();
            Retval.TestName = "Welch's t-test";

            // Welch's t-test for two samples
            double sumX = 0.0; // calculate means
            double sumY = 0.0;

            for (int i = 0; i < x.Count; ++i)
                sumX += x[i];

            for (int i = 0; i < y.Count; ++i)
                sumY += y[i];

            int n1 = x.Count;
            int n2 = y.Count;
            double meanX = sumX / n1;
            double meanY = sumY / n2;

            double sumXminusMeanSquared = 0.0; // calculate (sample) variances
            double sumYminusMeanSquared = 0.0;

            for (int i = 0; i < n1; ++i)
                sumXminusMeanSquared += (x[i] - meanX) * (x[i] - meanX);

            for (int i = 0; i < n2; ++i)
                sumYminusMeanSquared += (y[i] - meanY) * (y[i] - meanY);

            double varX = sumXminusMeanSquared / (n1 - 1);
            double varY = sumYminusMeanSquared / (n2 - 1);

            // t statistic
            double top = (meanX - meanY);
            double bot = Math.Sqrt((varX / n1) + (varY / n2));
            double t = top / bot;


            // df mildly tricky
            // http://www.statsdirect.com/help/default.htm#parametric_methods/unpaired_t.htm
            double num = ((varX / n1) + (varY / n2)) *
              ((varX / n1) + (varY / n2));
            double denomLeft = ((varX / n1) * (varX / n1)) / (n1 - 1);
            double denomRight = ((varY / n2) * (varY / n2)) / (n2 - 1);
            double denom = denomLeft + denomRight;
            double df = num / denom;

            double p = Student(t, df); // cumulative two-tail density

            var results = $"MEAN(x)={meanX.ToString("F2")}\nMEAN(y)={meanY.ToString("F2")}\nt={t.ToString("F4")}\ndf={df.ToString("F3")}\np-value={p.ToString("F5")}\n\n";

            var meaning = (p < 0.01) ? "These ARE LIKELY different\n\n" : "these seem to be the same\n\n";
            //Console.WriteLine("mean of x = " + meanX.ToString("F2"));
            //Console.WriteLine("mean of y = " + meanY.ToString("F2"));
            //Console.WriteLine("t = " + t.ToString("F4"));
            //Console.WriteLine("df = " + df.ToString("F3"));
            //Console.WriteLine("p-value = " + p.ToString("F5"));

            //Explain();

            Retval.df = df;
            Retval.t = t;
            Retval.p = p;
            Retval.desiredPvalue = desiredPvalue;

            return Retval;
        }

        public static string Explain()
        {
            string str = @"

The p-value is the probability that the two means are equal.

If the p-value is low, typically less than 0.05 or 0.01, then you conclude there is statistical evidence the two means are different. 

But if the p-value isn't below the 5% or 1% critical value then you conclude there's not enough evidence to say the means are different.
            ";
            return str;
        }

        public static double Student(double t, double df)
        {
            // for large integer df or double df
            // adapted from ACM algorithm 395
            // returns 2-tail p-value
            double n = df; // to sync with ACM parameter name
            double a, b, y;

            t = t * t;
            y = t / n;
            b = y + 1.0;
            if (y > 1.0E-6) y = Math.Log(b);
            a = n - 0.5;
            b = 48.0 * a * a;
            y = a * y;

            y = (((((-0.4 * y - 3.3) * y - 24.0) * y - 85.5) /
              (0.8 * y * y + 100.0 + b) + y + 3.0) / b + 1.0) *
              Math.Sqrt(y);
            return 2.0 * Gauss(-y); // ACM algorithm 209
        }

        public static double Student(double t, int df)
        {
            // adapted from ACM algorithm 395
            // for small integer df, or int df with large t
            int n = df; // to sync with ACM parameter name
            double a, b, y, z;

            z = 1.0;
            t = t * t;
            y = t / n;
            b = 1.0 + y;

            if (n >= 20 && t < n || n > 200) // moderate df and smallish t  ( df >= 20, t < df ) or very large df
            {
                double x = 1.0 * n; // make df a double
                return Student(t, x); // call double df version
            }

            if (n < 20 && t < 4.0)
            {
                a = Math.Sqrt(y);
                y = Math.Sqrt(y);
                if (n == 1)
                    a = 0.0;
            }
            else
            {
                a = Math.Sqrt(b);
                y = a * n;
                for (int j = 2; a != z; j += 2)
                {
                    z = a;
                    y = y * (j - 1) / (b * j);
                    a = a + y / (n + j);
                }
                n = n + 2;
                z = y = 0.0;
                a = -a;
            }

            int sanityCt = 0;
            while (true && sanityCt < 10000)
            {
                ++sanityCt;
                n = n - 2;
                if (n > 1)
                {
                    a = (n - 1) / (b * n) * a + y;
                    continue;
                }

                if (n == 0)
                    a = a / Math.Sqrt(b);
                else // n == 1
                    a = (Math.Atan(y) + a / b) * 0.63661977236; // 2/Pi

                return z - a;
            }

            return -1.0; // error
        } // Student (int df)

        public static double Gauss(double z)
        {
            // input = z-value (-inf to +inf)
            // output = p under Standard Normal curve from -inf to z
            // e.g., if z = 0.0, function returns 0.5000
            // ACM Algorithm #209
            double y; // 209 scratch variable
            double p; // result. called 'z' in 209
            double w; // 209 scratch variable

            if (z == 0.0)
                p = 0.0;
            else
            {
                y = Math.Abs(z) / 2;
                if (y >= 3.0)
                {
                    p = 1.0;
                }
                else if (y < 1.0)
                {
                    w = y * y;
                    p = ((((((((0.000124818987 * w
                      - 0.001075204047) * w + 0.005198775019) * w
                      - 0.019198292004) * w + 0.059054035642) * w
                      - 0.151968751364) * w + 0.319152932694) * w
                      - 0.531923007300) * w + 0.797884560593) * y * 2.0;
                }
                else
                {
                    y = y - 2.0;
                    p = (((((((((((((-0.000045255659 * y
                      + 0.000152529290) * y - 0.000019538132) * y
                      - 0.000676904986) * y + 0.001390604284) * y
                      - 0.000794620820) * y - 0.002034254874) * y
                      + 0.006549791214) * y - 0.010557625006) * y
                      + 0.011630447319) * y - 0.009279453341) * y
                      + 0.005353579108) * y - 0.002141268741) * y
                      + 0.000535310849) * y + 0.999936657524;
                }
            }

            if (z > 0.0)
                return (p + 1.0) / 2;
            else
                return (1.0 - p) / 2;
        } // Gauss()

    }
}
