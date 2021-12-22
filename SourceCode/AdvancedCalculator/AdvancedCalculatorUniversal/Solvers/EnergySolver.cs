using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EquationSolver;

namespace AdvancedCalculator
{
    public class EnergySolver : SolverINPC
    {
        public EnergySolver()
        {
            Equations.Add(new Equation("Joules", "Ergs", () => { return Ergs / Conversions.ERGS_PER_JOULE; }));
            Equations.Add(new Equation("Ergs", "Joules", () => { return Joules * Conversions.ERGS_PER_JOULE; }));

            Equations.Add(new Equation("Joules", "KilowattHours", () => { return KilowattHours * Conversions.JOULES_PER_KILOWATTHOUR; }));
            Equations.Add(new Equation("KilowattHours", "Joules", () => { return Joules / Conversions.JOULES_PER_KILOWATTHOUR; }));

            Equations.Add(new Equation("Joules", "BTUs", () => { return BTUs * Conversions.JOULES_PER_BTU; }));
            Equations.Add(new Equation("BTUs", "Joules", () => { return Joules / Conversions.JOULES_PER_BTU; }));

            Equations.Add(new Equation("BTUs", "Therms", () => { return Therms * Conversions.BTUS_PER_THERM; }));
            Equations.Add(new Equation("Therms", "BTUs", () => { return BTUs / Conversions.BTUS_PER_THERM; }));

            Equations.Add(new Equation("Joules", "Calories", () => { return Calories * Conversions.JOULES_PER_CALORIE; }));
            Equations.Add(new Equation("Calories", "Joules", () => { return Joules / Conversions.JOULES_PER_CALORIE; }));

            Equations.Add(new Equation("Joules", "KCals", () => { return KCals * Conversions.JOULES_PER_KCAL; }));
            Equations.Add(new Equation("KCals", "Joules", () => { return Joules / Conversions.JOULES_PER_KCAL; }));

            Equations.Add(new Equation("KCals", "Donuts", () => { return Donuts * Conversions.KCALS_PER_DOUNT; }));
            Equations.Add(new Equation("Donuts", "KCals", () => { return KCals / Conversions.KCALS_PER_DOUNT; }));

            InitEquivLists();

        }

        private double _Joules = Double.NaN;
        public double Joules { get { return _Joules; } set { if (value == _Joules) return; _Joules = value; OnPropertyChanged("Joules"); } }

        private double _Ergs = Double.NaN;
        public double Ergs { get { return _Ergs; } set { if (value == _Ergs) return; _Ergs = value; OnPropertyChanged("Ergs"); } }

        private double _KilowattHours = Double.NaN;
        public double KilowattHours { get { return _KilowattHours; } set { if (value == _KilowattHours) return; _KilowattHours = value; OnPropertyChanged("KilowattHours"); } }

        private double _BTUs = Double.NaN;
        public double BTUs { get { return _BTUs; } set { if (value == _BTUs) return; _BTUs = value; OnPropertyChanged("BTUs"); } }

        private double _Therms = Double.NaN;
        public double Therms { get { return _Therms; } set { if (value == _Therms) return; _Therms = value; OnPropertyChanged("Therms"); } }

        private double _Calories = Double.NaN;
        public double Calories { get { return _Calories; } set { if (value == _Calories) return; _Calories = value; OnPropertyChanged("Calories"); } }

        private double _KCals = Double.NaN;
        public double KCals { get { return _KCals; } set { if (value == _KCals) return; _KCals = value; OnPropertyChanged("KCals"); } }

        private double _Donuts = Double.NaN;
        public double Donuts { get { return _Donuts; } set { if (value == _Donuts) return; _Donuts = value; OnPropertyChanged("Donuts"); } }

    }
}
