using EquationSolver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedCalculator.Solvers
{
    internal class FinancialValueOverTime : SolverINPC
    {
        public FinancialValueOverTime()
        {
            Equations.Add(new Equation("StartIndex", new List<string>() { "StartDate" },
                () =>
                {
                    double Retval = CalculateIndex(StartDate);
                    return Retval;
                }));
            Equations.Add(new Equation("EndIndex", new List<string>() { "EndDate" },
                () =>
                {
                    double Retval = CalculateIndex(EndDate);
                    return Retval;
                }));
            Equations.Add(new Equation("MoneyRatio", new List<string>() { "StartIndex", "EndIndex" },
                () =>
                {
                    double Retval = EndIndex / StartIndex;
                    return Retval;
                }));
            Equations.Add(new Equation("StartDollars", new List<string>() { "MoneyRatio", "EndDollars" },
                () =>
                {
                    double Retval = EndDollars / MoneyRatio;
                    var sds = StartDate.ToString("yyyy-MM-dd");
                    var eds = EndDate.ToString("yyyy-MM-dd");
                    report2 = $"${EndDollars:F0} in {eds} was worth ${Retval:F0} in {sds}";
                    return Retval;
                }));

            Equations.Add(new Equation("EndDollars", new List<string>() { "MoneyRatio", "StartDollars" },
                () =>
                {
                    double Retval = StartDollars * MoneyRatio;
                    var sds = StartDate.ToString("yyyy-MM-dd");
                    var eds = EndDate.ToString("yyyy-MM-dd");
                    report1 = $"${StartDollars:F0} in {sds} is worth ${Retval:F0} in {eds}";
                    return Retval;
                }));

        }
        const string DateName = "DATE";
        const string ValueName = "CPIAUCSL";
        CsvTable moneyHistory = new CsvTable();
        public async Task InitAsync()
        {
            await moneyHistory.ReadAsync("ValueOfMoneyTables", "CPIAUCSL.csv");
            moneyHistory.InitColumnDoubleValueFromDate(DateName, 0.0);
            moneyHistory.InitColumnDoubleValue(ValueName, 1.0);
        }

        private (int, int) GetDateRows(CsvTable table, double dateBinary)
        {
            var rowLo = table.FindRowLTE(DateName, dateBinary);
            var rowHi = table.FindRowGTE(DateName, dateBinary);
            return (rowLo, rowHi);
        }

        public double CalculateIndex(DateTime date)
        {
            var dateBinary = (double)date.ToBinary();
            var (rowLo, rowHi) = GetDateRows(moneyHistory, dateBinary);
            var rowLoValue = moneyHistory.GetDouble(DateName, rowLo, 1);
            var rowHiValue = moneyHistory.GetDouble(DateName, rowHi, 1);
            var rowRatio = CsvTable.CalculateInterpolateRatio(rowLoValue, dateBinary, rowHiValue);
            var retval = moneyHistory.GetDoubleInterpolate(ValueName, rowLo, rowRatio, rowHi, 1.0);
            return retval;
        }

        public void ZZZCalculate()
        {
            StartIndex = CalculateIndex(StartDate);
            EndIndex = CalculateIndex(EndDate);

            MoneyRatio = EndIndex / StartIndex;
            EndDollars = StartDollars * MoneyRatio;
            StartDollars = EndDollars / MoneyRatio;

            var sds = StartDate.ToString("yyyy-MM-dd");
            var eds = EndDate.ToString("yyyy-MM-dd");
            report1 = $"${StartDollars} in {sds} is worth ${EndDollars:F0} in {eds}";
            report2 = $"${EndDollars} in {eds} was worth ${StartDollars:F0} in {sds}";
            ;
        }



#if NEVER_EVER_DEFINED

            //var moneyHistory = new CsvTable();
            // Add a new column which is the dates converted to ???

    private void ZZZDELETEME()
        {
            var startDateBinary = (double)StartDate.ToBinary();
            //var startRowLo = moneyHistory.FindRowLTE(DateName, startDateBinary);
            //var startRowHi = moneyHistory.FindRowGTE(DateName, startDateBinary);
            var (startRowLo, startRowHi) = GetDateRows(moneyHistory, startDateBinary);
            //var startDateCalculate = moneyHistory.Get(DateName, rowLo, "");
            var startRowLoValue = moneyHistory.GetDouble(DateName, startRowLo, 1);
            var startRowHiValue = moneyHistory.GetDouble(DateName, startRowHi, 1);
            var startRowRatio = CsvTable.CalculateInterpolateRatio(startRowLoValue, startDateBinary, startRowHiValue);
            StartIndex = moneyHistory.GetDoubleInterpolate(ValueName, startRowLo, startRowRatio, startRowHi, 1.0);

            //var endRowLo = moneyHistory.FindRowLTE(DateName, endDateBinary);
            //var endRowHi = moneyHistory.FindRowGTE(DateName, endDateBinary);
            //var endDateCalculate = moneyHistory.Get(DateName, endRowHi, "");
            var (endRowLo, endRowHi) = GetDateRows(moneyHistory, endDateBinary);
            var endRowLoValue = moneyHistory.GetDouble(DateName, endRowLo, 1);
            var endRowHiValue = moneyHistory.GetDouble(DateName, endRowHi, 1);
            var endRowRatio = CsvTable.CalculateInterpolateRatio(endRowLoValue, endDateBinary, endRowHiValue);
            var EndIndex = moneyHistory.GetDoubleInterpolate(ValueName, endRowLo, endRowRatio, endRowHi, 1.0);
            }
#endif

        public string report1 { get; internal set; }
        public string report2 { get; internal set; }

        private double _MoneyRatio;
        public double MoneyRatio { get { return _MoneyRatio; } set { if (_MoneyRatio == value) return; _MoneyRatio = value; OnPropertyChanged("MoneyRatio"); } }

        private double _StartIndex;
        public double StartIndex { get { return _StartIndex; } set { if (_StartIndex == value) return; _StartIndex = value; OnPropertyChanged("StartIndex"); } }
        private double _EndIndex;
        public double EndIndex { get { return _EndIndex; } set { if (_EndIndex == value) return; _EndIndex = value; OnPropertyChanged("EndIndex"); } }

        private DateTime _StartDate;
        public DateTime StartDate { get { return _StartDate; } set { if (_StartDate == value) return; _StartDate = value; OnPropertyChanged("StartDate"); } }

        private DateTime _EndDate;
        public DateTime EndDate { get { return _EndDate; } set { if (_EndDate == value) return; _EndDate = value; OnPropertyChanged("EndDate"); } }
        private double _StartDollars;
        public double StartDollars { get { return _StartDollars; } set { if (_StartDollars == value) return; _StartDollars = value; OnPropertyChanged("StartDollars"); } }
        private double _EndDollars;
        public double EndDollars { get { return _EndDollars; } set { if (_EndDollars == value) return; _EndDollars = value; OnPropertyChanged("EndDollars"); } }

    }
}
