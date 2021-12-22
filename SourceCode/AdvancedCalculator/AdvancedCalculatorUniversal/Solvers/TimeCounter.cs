using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;

namespace AdvancedCalculator.Solvers
{
    public class TimeCounter : Statistics.StatisticsClassical, INotifyPropertyChanged
    {
        public TimeCounter()
        {
            PropertyChanged += TimeCounter_PropertyChanged;
            Init();
        }

        public override void Init()
        {
            Start = DateTime.MinValue;
            base.Init();
        }

        void TimeCounter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Mean")
            {
                InverseMean = Mean==0 ? 0 : 1 / Mean;
                MeanString = string.Format("{0:F2}", Mean);
                InverseMeanString = string.Format("{0:F2}", InverseMean);
            }
        }

        private double _InverseMean;
        public double InverseMean
        {
            get { return _InverseMean; }
            set { if (value == _InverseMean) return; _InverseMean = value; OnPropertyChanged("InverseMean"); }
        }

        private string _InverseMeanString;
        public string InverseMeanString
        {
            get { return _InverseMeanString; }
            set { _InverseMeanString = value; OnPropertyChanged("InverseMeanString"); }
        }

        private string _MeanString;
        public string MeanString
        {
            get { return _MeanString; }
            set { _MeanString = value; OnPropertyChanged("MeanString"); }
        }


        DateTime Start;
        public void IncrementMinutes()
        {
            DateTime End = DateTime.Now;
            if (Start != DateTime.MinValue)
            {
                TimeSpan Delta = End - Start;
                SetX (Delta.TotalMinutes);
            }
           
            Start = End;
        }
    }
}
