using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EquationSolver;
using System.Diagnostics;

namespace AdvancedCalculator
{
    public class HealthIdealHeartRateSolver : SolverINPC
    {
        public HealthIdealHeartRateSolver()
        {
            // http://www.cdc.gov/physicalactivity/everyone/measuring/heartrate.html
            // An estimate of a person's maximum age-related heart rate can be obtained by subtracting the 
            // person's age from 220. For example, for a 50-year-old person, the estimated maximum age-related 
            // heart rate would be calculated as 220 - 50 years = 170 beats per minute (bpm). 

            Equations.Add(new Equation("MaxHeartRate", "Age", () => { return 220 - Age; }));
            Equations.Add(new Equation("Age", "MaxHeartRate", () => { return 220 - MaxHeartRate; }));


            Equations.Add(new Equation("ModerateLowHeartRate", "MaxHeartRate", () => { return MaxHeartRate * 0.50; }));
            Equations.Add(new Equation("ModerateHighHeartRate", "MaxHeartRate", () => { return MaxHeartRate * 0.70; }));
            Equations.Add(new Equation("VigorousLowHeartRate", "MaxHeartRate", () => { return MaxHeartRate * 0.70; }));
            Equations.Add(new Equation("VigorousHighHeartRate", "MaxHeartRate", () => { return MaxHeartRate * 0.85; }));

            Equations.Add(new Equation("MaxHeartRate", "ModerateLowHeartRate", () => { return ModerateLowHeartRate / 0.50; }));
            Equations.Add(new Equation("MaxHeartRate", "ModerateHighHeartRate", () => { return ModerateHighHeartRate / 0.70; }));
            Equations.Add(new Equation("MaxHeartRate", "VigorousLowHeartRate", () => { return VigorousLowHeartRate / 0.70; }));
            Equations.Add(new Equation("MaxHeartRate", "VigorousHighHeartRate", () => { return VigorousHighHeartRate / 0.85; }));

            InitEquivLists();

        }


        private double _Age = 0;
        public double Age { get { return _Age; } set { if (value == _Age) return; _Age = value; OnPropertyChanged("Age"); } }

        private double _MaxHeartRate = 0;
        public double MaxHeartRate { get { return _MaxHeartRate; } set { if (value == _MaxHeartRate) return; _MaxHeartRate = value; OnPropertyChanged("MaxHeartRate"); } }

        private double _ModerateLowHeartRate = 0;
        public double ModerateLowHeartRate { get { return _ModerateLowHeartRate; } set { if (value == _ModerateLowHeartRate) return; _ModerateLowHeartRate = value; OnPropertyChanged("ModerateLowHeartRate"); } }

        private double _ModerateHighHeartRate = 0;
        public double ModerateHighHeartRate { get { return _ModerateHighHeartRate; } set { if (value == _ModerateHighHeartRate) return; _ModerateHighHeartRate = value; OnPropertyChanged("ModerateHighHeartRate"); } }

        private double _VigorousLowHeartRate = 0;
        public double VigorousLowHeartRate { get { return _VigorousLowHeartRate; } set { if (value == _VigorousLowHeartRate) return; _VigorousLowHeartRate = value; OnPropertyChanged("VigorousLowHeartRate"); } }

        private double _VigorousHighHeartRate = 0;
        public double VigorousHighHeartRate { get { return _VigorousHighHeartRate; } set { if (value == _VigorousHighHeartRate) return; _VigorousHighHeartRate = value; OnPropertyChanged("VigorousHighHeartRate"); } }


    }
}
