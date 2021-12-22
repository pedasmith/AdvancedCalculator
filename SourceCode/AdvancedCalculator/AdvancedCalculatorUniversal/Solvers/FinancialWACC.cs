using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EquationSolver;
using System.Diagnostics;

namespace AdvancedCalculator
{
    public class FinancialWACCSolver : SolverINPC
    {
        public FinancialWACCSolver()
        {
            Equations.Add(new Equation("WACC", new List<string>() { "EquityValue", "DebtValue", "CostOfEquity", "CostOfDebt", "TaxRate" },
                () =>
                {
                    double V = EquityValue + DebtValue;
                    double Retval = ((EquityValue / V) * (CostOfEquity/100)) + ((DebtValue / V) * (CostOfDebt/100)) * (1 - (TaxRate/100));
                    Retval = Retval * 100;
                    return Retval;
                }));
            InitEquivLists();
        }



        private double _EquityValue = 0;
        public double EquityValue { get { return _EquityValue; } set { if (value == _EquityValue) return; _EquityValue = value; OnPropertyChanged("EquityValue"); } }

        private double _DebtValue = 0;
        public double DebtValue { get { return _DebtValue; } set { if (value == _DebtValue) return; _DebtValue = value; OnPropertyChanged("DebtValue"); } }

        private double _CostOfEquity = 0;
        public double CostOfEquity { get { return _CostOfEquity; } set { if (value == _CostOfEquity) return; _CostOfEquity = value; OnPropertyChanged("CostOfEquity"); } }

        private double _CostOfDebt = 0;
        public double CostOfDebt { get { return _CostOfDebt; } set { if (value == _CostOfDebt) return; _CostOfDebt = value; OnPropertyChanged("CostOfDebt"); } }

        private double _TaxRate = 0;
        public double TaxRate { get { return _TaxRate; } set { if (value == _TaxRate) return; _TaxRate = value; OnPropertyChanged("TaxRate"); } }

        private double _WACC = 0;
        public double WACC { get { return _WACC; } set { if (value == _WACC) return; _WACC = value; OnPropertyChanged("WACC"); } }


    }
}
