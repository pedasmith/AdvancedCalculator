using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EquationSolver;

namespace AdvancedCalculator
{
    public class ElectricalEngineeringResistorSolver : SolverINPC
    {
        public ElectricalEngineeringResistorSolver()
        {
            // Calculates up to 3 resistors in series (s1, s2, s3) or in parallel (p1, p2, p3)            // If you swap series for parallel, you get a 
            Equations.Add(new Equation("Series", new List<string>() { "S1", "S2", "S3" }, () => { return S1 + S2 + S3; }));
            Equations.Add(new Equation("Parallel", new List<string>() { "P1", "P2", "P3" }, () => { return 1.0 / ((1.0/P1) + (1.0/P2) + (1.0/P3)); }));

            InitEquivLists();
        }


        private double _S1 = 0;
        public double S1 { get { return _S1; } set { if (value == _S1) return; _S1 = value; OnPropertyChanged("S1"); } }

        private double _S2 = 0;
        public double S2 { get { return _S2; } set { if (value == _S2) return; _S2 = value; OnPropertyChanged("S2"); } }

        private double _S3 = 0;
        public double S3 { get { return _S3; } set { if (value == _S3) return; _S3 = value; OnPropertyChanged("S3"); } }

        private double _Series = 0;
        public double Series { get { return _Series; } set { if (value == _Series) return; _Series = value; OnPropertyChanged("Series"); } }

        private double _P1 = Double.MaxValue;
        public double P1 { get { return _P1; } set { if (value == _P1) return; _P1 = value; OnPropertyChanged("P1"); } }

        private double _P2 = Double.MaxValue;
        public double P2 { get { return _P2; } set { if (value == _P2) return; _P2 = value; OnPropertyChanged("P2"); } }

        private double _P3 = Double.MaxValue;
        public double P3 { get { return _P3; } set { if (value == _P3) return; _P3 = value; OnPropertyChanged("P3"); } }

        private double _Parallel = Double.MaxValue;
        public double Parallel { get { return _Parallel; } set { if (value == _Parallel) return; _Parallel = value; OnPropertyChanged("Parallel"); } }
    }
}
