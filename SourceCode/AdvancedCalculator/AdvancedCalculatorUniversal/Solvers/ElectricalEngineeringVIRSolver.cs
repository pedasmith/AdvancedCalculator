using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EquationSolver;

namespace AdvancedCalculator
{
    public class ElectricalEngineeringVIRSolver : SolverINPC
    {
        public ElectricalEngineeringVIRSolver()
        {
            // Does the classic Volts = Current * Resitance, measured in volts, amps and ohms, (V=IR)
            // plus power = current * volts measured in watts, amps, and volts. (P=IV)
            // V=IR
            // P=IV (and therefore P=I**2R)
            Equations.Add(new Equation("Volts", new List<string>() { "Amps", "Ohms" }, () => { return Amps * Ohms; }));
            Equations.Add(new Equation("Amps", new List<string>() { "Volts", "Ohms" }, () => { return Volts / Ohms; }));
            Equations.Add(new Equation("Ohms", new List<string>() { "Volts", "Amps" }, () => { return Volts / Amps; }));

            Equations.Add(new Equation("Watts", new List<string>() { "Volts", "Amps" }, () => { return Volts * Amps; }));
            Equations.Add(new Equation("Amps", new List<string>() { "Watts", "Volts" }, () => { return Watts / Volts; }));
            Equations.Add(new Equation("Volts", new List<string>() { "Watts", "Amps" }, () => { return Watts / Amps; }));

            Equations.Add(new Equation("Amps", "MilliAmps", () => { return MilliAmps / 1000.0; }));
            Equations.Add(new Equation("MilliAmps", "Amps", () => { return Amps * 1000.0; }));
            
            InitEquivLists();

        }


        private double _Volts = Double.NaN;
        public double Volts { get { return _Volts; } set { if (value == _Volts) return; _Volts = value; OnPropertyChanged("Volts"); } }

        private double _Amps = Double.NaN;
        public double Amps { get { return _Amps; } set { if (value == _Amps) return; _Amps = value; OnPropertyChanged("Amps"); } }

        private double _MilliAmps = Double.NaN;
        public double MilliAmps { get { return _MilliAmps; } set { if (value == _MilliAmps) return; _MilliAmps = value; OnPropertyChanged("MilliAmps"); } }

        private double _Ohms = Double.NaN;
        public double Ohms { get { return _Ohms; } set { if (value == _Ohms) return; _Ohms = value; OnPropertyChanged("Ohms"); } }

        private double _Watts = Double.NaN;
        public double Watts { get { return _Watts; } set { if (value == _Watts) return; _Watts = value; OnPropertyChanged("Watts"); } }


    }
}
