using System;
using System.Collections.Generic;

using EquationSolver;

namespace AdvancedCalculator
{
    public class FinancialCAGRSolver : SolverINPC
    {
        public FinancialCAGRSolver()
        {
            Equations.Add(new Equation("CAGR", new List<string>() { "ValueRatio", "NYears" },
                () =>
                {
                    double Retval = Math.Pow(ValueRatio, 1.0 / NYears) - 1.0;
                    return Retval;
                }));
            Equations.Add(new Equation("ValueRatio", new List<string>() { "CAGR", "NYears" },
                () =>
                {
                    double Retval = Math.Pow((CAGR+1.0), NYears);
                    return Retval;
                }));
            Equations.Add(new Equation("ValueRatio", new List<string>() { "StartingValue", "EndingValue" }, () => { return EndingValue / StartingValue; }));
            Equations.Add(new Equation("StartingValue", new List<string>() { "ValueRatio", "EndingValue" }, () => { return EndingValue / ValueRatio; }));
            Equations.Add(new Equation("EndingValue", new List<string>() { "ValueRatio", "StartingValue" }, () => { return StartingValue * ValueRatio; }));
            Equations.Add(new Equation("CAGRPercent", "CAGR", () => { return CAGR * 100.0; }));
            Equations.Add(new Equation("CAGR", "CAGRPercent", () => { return CAGRPercent / 100.0; }));

            InitEquivLists();
        }


        private double _StartingValue = 0;
        public double StartingValue { get { return _StartingValue; } set { if (value == _StartingValue) return; _StartingValue = value; OnPropertyChanged("StartingValue"); } }

        private double _EndingValue = 0;
        public double EndingValue { get { return _EndingValue; } set { if (value == _EndingValue) return; _EndingValue = value; OnPropertyChanged("EndingValue"); } }

        private double _ValueRatio = 0;
        public double ValueRatio { get { return _ValueRatio; } set { if (value == _ValueRatio) return; _ValueRatio = value; OnPropertyChanged("ValueRatio"); } }

        private double _NYears = 0;
        public double NYears { get { return _NYears; } set { if (value == _NYears) return; _NYears = value; OnPropertyChanged("NYears"); } }

        private double _CAGR = 0;
        public double CAGR { get { return _CAGR; } set { if (value == _CAGR) return; _CAGR = value; OnPropertyChanged("CAGR"); } }

        private double _CAGRPercent = 0;
        public double CAGRPercent { get { return _CAGRPercent; } set { if (value == _CAGRPercent) return; _CAGRPercent = value; OnPropertyChanged("CAGRPercent"); } }


    }
}
