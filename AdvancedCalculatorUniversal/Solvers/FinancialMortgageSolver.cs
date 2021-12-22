using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EquationSolver;
using System.Diagnostics;

namespace AdvancedCalculator
{
    public class FinancialMortgageSolver : SolverINPC
    {
        public FinancialMortgageSolver()
        {
            Equations.Add(new Equation("Payment", new List<string>() { "Interest", "NPayments", "Principle" }, 
                () => { 
                    double Retval = Principle / NPayments;
                    if (Math.Abs(Interest) > .00001)
                    {
                        double CasioRetval = Principle / ((1.0 - Math.Pow(1.0 + Interest, -NPayments))/Interest);
                        double RN = Math.Pow(1.0 + Interest, NPayments);
                        double WikiRetval = Principle * (Interest * RN) / (RN - 1);
                        if (Double.IsNaN(CasioRetval) && Double.IsNaN(WikiRetval))
                        {
                        }
                        else if (OutsideDelta (CasioRetval, WikiRetval))
                        {
                            Debug.WriteLine("?Casio == {0} Wiki={1}", CasioRetval, WikiRetval);
                        }
                        Retval = CasioRetval;
                    }
                    return Retval;
                }));

            Equations.Add(new Equation("Interest", new List<string>(){ "Payment", "NPayments", "Principle"},
                () => { return this.SolveFor("Interest", "Payment", -10000, 10000); }));

            Equations.Add(new Equation("NPayments", new List<string>() { "Payment", "Interest", "Principle" },
                () => { return this.SolveFor("NPayments", "Payment", 1, 99*12); }));

            Equations.Add(new Equation("Principle", new List<string>() { "Payment", "Interest", "NPayments" },
                () => { return this.SolveFor("Principle", "Payment", 0, 1000000); }));


            Equations.Add(new Equation("Interest", "InterestPerYear", () => { return (InterestPerYear / 100.0) / 12.0; }));
            Equations.Add(new Equation("InterestPerYear", "Interest", () => { return Interest * 12 * 100; }));

            Equations.Add(new Equation("NPayments", "YearsOfPayments", () => { return YearsOfPayments * 12; }));
            Equations.Add(new Equation("YearsOfPayments", "NPayments", () => { return NPayments / 12; }));

            InitEquivLists();
            Interest = 0.05;
        }


        private double _Interest = 0;
        public double Interest { get { return _Interest; } set { if (value == _Interest) return; _Interest = value; OnPropertyChanged("Interest"); } }

        private double _InterestPerYear = 0;
        public double InterestPerYear { get { return _InterestPerYear; } set { if (value == _InterestPerYear) return; _InterestPerYear = value; OnPropertyChanged("InterestPerYear"); } }

        private double _Payment = 0;
        public double Payment { get { return _Payment; } set { if (value == _Payment) return; _Payment = value; OnPropertyChanged("Payment"); } }

        private double _NPayments = 12*30;
        public double NPayments { get { return _NPayments; } set { if (value == _NPayments) return; _NPayments = value; OnPropertyChanged("NPayments"); } }

        private double _YearsOfPayments = 30;
        public double YearsOfPayments { get { return _YearsOfPayments; } set { if (value == _YearsOfPayments) return; _YearsOfPayments = value; OnPropertyChanged("YearsOfPayments"); } }

        private double _Principle = 100000.0;
        public double Principle { get { return _Principle; } set { if (value == _Principle) return; _Principle = value; OnPropertyChanged("Principle"); } }

    }
}
