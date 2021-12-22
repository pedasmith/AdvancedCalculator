using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EquationSolver;

namespace AdvancedCalculator
{
    public class TemperatureSolver : SolverINPC
    {
        public TemperatureSolver()
        {
            Equations.Add(new Equation("DegreesF", "DegreesC", () => { return DegreesC * Conversions.DEGREESF_PER_DEGREEC + Conversions.DEGREESF_OFFSET; }));
            Equations.Add(new Equation("DegreesC", "DegreesF", () => { return (DegreesF - Conversions.DEGREESF_OFFSET) / Conversions.DEGREESF_PER_DEGREEC; }));

            Equations.Add(new Equation("DegreesKelvin", "DegreesC", () => { return DegreesC + Conversions.DEGREESKELVIN_OFFSET; }));
            Equations.Add(new Equation("DegreesC", "DegreesKelvin", () => { return DegreesKelvin - Conversions.DEGREESKELVIN_OFFSET; }));

            Equations.Add(new Equation("DegreesKelvin", "DegreesRankine", () => { return DegreesRankine / Conversions.DEGREESF_PER_DEGREEC; }));
            Equations.Add(new Equation("DegreesRankine", "DegreesKelvin", () => { return DegreesKelvin * Conversions.DEGREESF_PER_DEGREEC; }));


            InitEquivLists();

        }

        private double _DegreesF = Double.NaN;
        public double DegreesF { get { return _DegreesF; } set { if (value == _DegreesF) return; _DegreesF = value; OnPropertyChanged("DegreesF"); } }

        private double _DegreesC = Double.NaN;
        public double DegreesC { get { return _DegreesC; } set { if (value == _DegreesC) return; _DegreesC = value; OnPropertyChanged("DegreesC"); } }

        private double _DegreesKelvin = Double.NaN;
        public double DegreesKelvin { get { return _DegreesKelvin; } set { if (value == _DegreesKelvin) return; _DegreesKelvin = value; OnPropertyChanged("DegreesKelvin"); } }

        private double _DegreesRankine = Double.NaN;
        public double DegreesRankine { get { return _DegreesRankine; } set { if (value == _DegreesRankine) return; _DegreesRankine = value; OnPropertyChanged("DegreesRankine"); } }

    }
}
