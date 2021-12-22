using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using System.Diagnostics;

namespace AdvancedCalculator.Solvers
{
    public class StatisticsX : INotifyPropertyChanged
    {
        public virtual void Init()
        {
            N = 0;
            Mean = 0;
            Total = 0;
            TotalXSquared = 0;
            VarianceEstimate = 0;
            StandardDeviationEstimate = 0;
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

        private double _StandardDeviationEstimate;
        public double StandardDeviationEstimate
        {
            get { return _StandardDeviationEstimate; }
            set { if (value == _StandardDeviationEstimate) return; _StandardDeviationEstimate = value; OnPropertyChanged("StandardDeviationEstimate"); }
        }


        public void SetX(double X)
        {
            N++;
            Total += X;
            TotalXSquared += X * X;
            Mean = Total / N;
            VarianceEstimate = (TotalXSquared - ((Total * Total) / N)) / (N - 1);
            StandardDeviationEstimate = Math.Sqrt(VarianceEstimate);
        }

        static int TestA()
        {
            int NError = 0;
            StatisticsX statX = new StatisticsX();
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


    }
}
