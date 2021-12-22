using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Statistics
{
    public class StatisticsClassical : INotifyPropertyChanged
    {
        public StatisticsClassical() { Clear(); }
        public StatisticsClassical(IList<double> data)
        {
            Clear();
            Set(data);
        }
        public virtual void Init()
        {
            Clear();
        }

        public void Clear()
        {
            N = 0;
            Mean = 0;
            Total = 0;
            TotalXSquared = 0;
            VarianceEstimate = 0;
            VariancePopulation = 0;
            StandardDeviationEstimate = 0;
            StandardDeviationPopulation = 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
            //UpdateTextBox(name);
        }

        private long _N;
        public long N
        {
            get { return _N; }
            set { if (value == _N) return; _N = value; OnPropertyChanged("N"); }
        }


        private double _Mean;
        public double Mean
        {
            get { return _Mean; }
            set { if (value == _Mean) return; _Mean = value; OnPropertyChanged("Mean"); }
        }

        private double _Total;
        public double Total
        {
            get { return _Total; }
            set { if (value == _Total) return; _Total = value; OnPropertyChanged("Total"); }
        }

        private double _TotalXSquared;
        public double TotalXSquared
        {
            get { return _TotalXSquared; }
            set { if (value == _TotalXSquared) return; _TotalXSquared = value; OnPropertyChanged("TotalSquared"); }
        }


        private double _VarianceEstimate;
        public double VarianceEstimate
        {
            get { return _VarianceEstimate; }
            set { if (value == _VarianceEstimate) return; _VarianceEstimate = value; OnPropertyChanged("VarianceEstimate"); }
        }

        private double _VariancePopulation;
        public double VariancePopulation
        {
            get { return _VariancePopulation; }
            set { if (value == _VariancePopulation) return; _VariancePopulation = value; OnPropertyChanged("VariancePopulation"); }
        }

        private double _StandardDeviationEstimate;
        public double StandardDeviationEstimate
        {
            get { return _StandardDeviationEstimate; }
            set { if (value == _StandardDeviationEstimate) return; _StandardDeviationEstimate = value; OnPropertyChanged("StandardDeviationEstimate"); }
        }

        // Relative standard deviation, RSD.
        // For example, see http://www.fao.org/docrep/w7295e/w7295e08.htm
        public double RSD
        {
            get { return StandardDeviationEstimate / Mean; }
        }

        private double _StandardDeviationPopulation;
        public double StandardDeviationPopulation
        {
            get { return _StandardDeviationPopulation; }
            set { if (value == _StandardDeviationPopulation) return; _StandardDeviationPopulation = value; OnPropertyChanged("StandardDeviationPopulation"); }
        }


        public bool AllRegularNumbers // no NaN or Infinity
        {
            get
            {
                bool retval = true;
                if (double.IsInfinity(N) || double.IsNaN(N)) retval = false;
                if (double.IsInfinity(Mean) || double.IsNaN(Mean)) retval = false;
                if (double.IsInfinity(Total) || double.IsNaN(Total)) retval = false;
                if (double.IsInfinity(TotalXSquared) || double.IsNaN(TotalXSquared)) retval = false;
                if (double.IsInfinity(VarianceEstimate) || double.IsNaN(VarianceEstimate)) retval = false;
                if (double.IsInfinity(VariancePopulation) || double.IsNaN(VariancePopulation)) retval = false;
                if (double.IsInfinity(StandardDeviationEstimate) || double.IsNaN(StandardDeviationEstimate)) retval = false;
                if (double.IsInfinity(StandardDeviationPopulation) || double.IsNaN(StandardDeviationPopulation)) retval = false;

                return retval;
            }
        }
        public void Set(IList<double> list)
        {
            foreach (var value in list)
            {
                SetX(value);
            }
        }

        public void SetX(double X)
        {
            N++;
            Total += X;
            TotalXSquared += X * X;
            Mean = Total / N;
            VarianceEstimate = (TotalXSquared - ((Total * Total) / N)) / (N - 1);
            StandardDeviationEstimate = Math.Sqrt(VarianceEstimate);

            VariancePopulation = (TotalXSquared - ((Total * Total) / N)) / (N);
            StandardDeviationPopulation = Math.Sqrt(VariancePopulation);
        }


        static int TestA()
        {
            int NError = 0;
            StatisticsClassical statX = new StatisticsClassical();
            statX.SetX(4.0);
            statX.SetX(7.0);
            statX.SetX(13.0);
            statX.SetX(16.0);
            if (statX.Mean != 10)
            {
                NError += 1;
                Debug.WriteLine("Error: StatisticsX: TestA: Mean is {0} s.b. {1}", statX.Mean, 10);
            }
            if (statX.VarianceEstimate != 30)
            {
                NError += 1;
                Debug.WriteLine("Error: StatisticsX: TestA: VarianceEstimate is {0} s.b. {1}", statX.VarianceEstimate, 30);
            }


            return NError;
        }
        public static int Test()
        {
            int NError = 0;
            NError += TestA();
            return NError;
        }

        public override string ToString()
        {
            return $"x̄     (mean)\t{Mean}\nN     (count)\t{N}\n𝚺     (sum)\t{Total}\n𝙨     (stddev)\t{StandardDeviationEstimate}\n";
        }

        public string ToString(AdvancedCalculator.IFormatDouble d2s)
        {
            return $"x̄\t(mean)        \t{d2s.DoubleToString(Mean)}\nN\t(count)     \t{d2s.DoubleToString(N)}\n𝚺\t(sum)         \t{d2s.DoubleToString(Total)}\n𝙨\t(sample stddev)\t{d2s.DoubleToString(StandardDeviationEstimate)}\n𝝈ₙ\t(pop. stddev)\t{d2s.DoubleToString(StandardDeviationPopulation)}\nRSD\t(rel. stddev)\t{d2s.DoubleToString(RSD)}\n";
        }

    }

}
