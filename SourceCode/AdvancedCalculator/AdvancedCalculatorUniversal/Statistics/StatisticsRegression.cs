using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Statistics
{
    class StatisticsRegression
    {
        public StatisticsRegression() { Clear();  }
        public StatisticsRegression(IList<double> X, IList<double> Y)
        {
            Clear();
            Set(X, Y);
        }
        public double SumXY { get; set; }
        public double SumX { get; set; }
        public double SumY { get; set; }
        public double SumX2 { get; set; }
        public double SumY2 { get; set; }
        public double Count { get; set; }

        public double Slope { get { if (SumX2 == 0) return 0; return (Count * SumXY - (SumX * SumY)) / (Count * SumX2 - SumX * SumX); }  }
        public double Intercept { get { if (Count == 0) return 0; return (SumY - Slope * SumX) / Count; } }
        public double Correlation { get { if (YClassical.StandardDeviationEstimate == 0) return 0; return Slope * XClassical.StandardDeviationEstimate / YClassical.StandardDeviationEstimate; } }
        public double ErrorRegression
        {
            get
            {
                if (Count <= 2) return 0;
                var var = (1 - Math.Pow(Correlation, 2)) * Math.Pow(YClassical.StandardDeviationPopulation, 2) * Count / (Count - 2);
                return Math.Sqrt(var);
            }
        }

        public double ErrorSlope
        {
            get
            {
                double div = Math.Pow(XClassical.StandardDeviationPopulation, 2) * Count;
                if (div == 0) return 0;
                return ErrorRegression / Math.Sqrt (div);
            }
        }

        public string Error { get; set; }

        private StatisticsClassical XClassical = new StatisticsClassical();
        private StatisticsClassical YClassical = new StatisticsClassical();


        public override string ToString()
        {
            return ToString(new AdvancedCalculator.D2S());
        }

        public string ToString(AdvancedCalculator.IFormatDouble d2s)
        {
            if (Error != null && Error != "") return Error;
            return $"Slope         \t{d2s.DoubleToString(Slope)}\nIntercept \t{d2s.DoubleToString(Intercept)}\nCorrelation\t{d2s.DoubleToString(Correlation)}\nStdErr Line\t{d2s.DoubleToString(ErrorRegression)}\nStdErr Slope\t{d2s.DoubleToString(ErrorSlope)}\n";
        }


        public void Clear()
        {
            SumXY = 0;
            SumX = 0;
            SumY = 0;
            SumX2 = 0;
            SumY2 = 0;
            Count = 0;
        }


        public void Set(IList<double> X, IList<double> Y)
        {
            Clear();
            XClassical.Clear();
            YClassical.Clear();
            if (X.Count != Y.Count)
            {
                Error = "There must be the same number \nof X and Y values";
                return;
            }
            for (int i=0; i<X.Count; i++)
            {
                Count++;
                SumXY += X[i] * Y[i];
                SumX += X[i];
                SumY += Y[i];
                SumX2 += X[i] * X[i];
                SumY2 += Y[i] * Y[i];
            }
            XClassical.Set(X);
            YClassical.Set(Y);
        }

        public double EstimateY (double x)
        {
            // y = mx + b where m=slope and b=intercept
            var y = Slope * x + Intercept;
            return y;
        }

    }

}
