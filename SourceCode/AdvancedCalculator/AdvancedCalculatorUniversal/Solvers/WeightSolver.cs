using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EquationSolver;

namespace AdvancedCalculator
{
    // This is Earth, so I hapily convert between mass and weight :-).
    public class WeightSolver : SolverINPC
    {
        public WeightSolver()
        {
            Equations.Add(new Equation("Ounces", "Pounds", () => { return Pounds * Conversions.OUNCES_PER_POUND; }));
            Equations.Add(new Equation("Pounds", "Ounces", () => { return Ounces / Conversions.OUNCES_PER_POUND; }));

            Equations.Add(new Equation("Pounds", "ShortTons", () => { return ShortTons * Conversions.POUNDS_PER_SHORT_TON; }));
            Equations.Add(new Equation("ShortTons", "Pounds", () => { return Pounds / Conversions.POUNDS_PER_SHORT_TON; }));

            Equations.Add(new Equation("Pounds", "LongTons", () => { return LongTons * Conversions.POUNDS_PER_LONG_TON; }));
            Equations.Add(new Equation("LongTons", "Pounds", () => { return Pounds / Conversions.POUNDS_PER_LONG_TON; }));

            Equations.Add(new Equation("OuncesRemainder", "Pounds", () =>
            {
                double truncate = Math.Truncate(Pounds);
                double remainder = Pounds - truncate;
                return remainder * Conversions.OUNCES_PER_POUND;
            }));

            Equations.Add(new Equation("PoundsRemainder", "Pounds", () =>
            {
                double truncate = Math.Truncate(Pounds);
                return truncate;
            }));

            Equations.Add(new Equation("Ounces", "Grams", () => { return Grams / Conversions.GRAMS_PER_OUNCE; }));
            Equations.Add(new Equation("Grams", "Ounces", () => { return Ounces * Conversions.GRAMS_PER_OUNCE; }));

            Equations.Add(new Equation("Grams", "Kilograms", () => { return Kilograms * 1000; }));
            Equations.Add(new Equation("Kilograms", "Grams", () => { return Grams / 1000; }));


            // Tonne==metric ton
            Equations.Add(new Equation("Kilograms", "Tonnes", () => { return Tonnes * Conversions.KILOGRAMS_PER_TONNE; }));
            Equations.Add(new Equation("Tonnes", "Kilograms", () => { return Kilograms / Conversions.KILOGRAMS_PER_TONNE; }));

            Equations.Add(new Equation("Tonnes", "MMTs", () => { return MMTs * 100000; }));
            Equations.Add(new Equation("MMTs", "Tonnes", () => { return Tonnes / 1000000; }));

            Equations.Add(new Equation("Grains", "Ounces", () => { return Ounces * Conversions.GRAINS_PER_OUNCE; }));
            Equations.Add(new Equation("Ounces", "Grains", () => { return Grains / Conversions.GRAINS_PER_OUNCE; }));

            Equations.Add(new Equation("Grains", "TroyOunces", () => { return TroyOunces * Conversions.GRAINS_PER_TROY_OUNCE; }));
            Equations.Add(new Equation("TroyOunces", "Grains", () => { return Grains / Conversions.GRAINS_PER_TROY_OUNCE; }));

            Equations.Add(new Equation("TroyOunces", "TroyPounds", () => { return TroyPounds * Conversions.TROY_OUNCES_PER_TROY_POUND; }));
            Equations.Add(new Equation("TroyPounds", "TroyOunces", () => { return TroyOunces / Conversions.TROY_OUNCES_PER_TROY_POUND; }));


            Equations.Add(new Equation("Tola", "Grams", () => { return Grams / Conversions.GRAMS_PER_TOLA; }));
            Equations.Add(new Equation("Grams", "Tola", () => { return Tola * Conversions.GRAMS_PER_TOLA; }));

            Equations.Add(new Equation("Ser", "Tola", () => { return Tola / Conversions.TOLA_PER_SER; }));
            Equations.Add(new Equation("Tola", "Ser", () => { return Ser * Conversions.TOLA_PER_SER; }));

            Equations.Add(new Equation("Maund", "Ser", () => { return Ser / Conversions.SER_PER_MAUND; }));
            Equations.Add(new Equation("Ser", "Maund", () => { return Maund * Conversions.SER_PER_MAUND; }));

            InitEquivLists();
        }

        private double _Ounces = Double.NaN;
        public double Ounces { get { return _Ounces; } set { if (value == _Ounces) return; _Ounces = value; OnPropertyChanged("Ounces"); } }

        private double _OuncesRemainder = Double.NaN;
        public double OuncesRemainder { get { return _OuncesRemainder; } set { if (value == _OuncesRemainder) return; _OuncesRemainder = value; OnPropertyChanged("OuncesRemainder"); } }

        private double _PoundsRemainder = Double.NaN;
        public double PoundsRemainder { get { return _PoundsRemainder; } set { if (value == _PoundsRemainder) return; _PoundsRemainder = value; OnPropertyChanged("PoundsRemainder"); } }

        private double _Pounds = Double.NaN;
        public double Pounds { get { return _Pounds; } set { if (value == _Pounds) return; _Pounds = value; OnPropertyChanged("Pounds"); } }

        private double _ShortTons = Double.NaN;
        public double ShortTons { get { return _ShortTons; } set { if (value == _ShortTons) return; _ShortTons = value; OnPropertyChanged("ShortTons"); } }

        private double _LongTons = Double.NaN;
        public double LongTons { get { return _LongTons; } set { if (value == _LongTons) return; _LongTons = value; OnPropertyChanged("LongTons"); } }

        private double _Grams = Double.NaN;
        public double Grams { get { return _Grams; } set { if (value == _Grams) return; _Grams = value; OnPropertyChanged("Grams"); } }

        private double _Kilograms = Double.NaN;
        public double Kilograms { get { return _Kilograms; } set { if (value == _Kilograms) return; _Kilograms = value; OnPropertyChanged("Kilograms"); } }

        private double _Tonnes = Double.NaN;
        public double Tonnes { get { return _Tonnes; } set { if (value == _Tonnes) return; _Tonnes = value; OnPropertyChanged("Tonnes"); } }

        private double _MMTs = Double.NaN;
        public double MMTs { get { return _MMTs; } set { if (value == _MMTs) return; _MMTs = value; OnPropertyChanged("MMTs"); } }

        private double _Grains = Double.NaN;
        public double Grains { get { return _Grains; } set { if (value == _Grains) return; _Grains = value; OnPropertyChanged("Grains"); } }

        private double _TroyOunces = Double.NaN;
        public double TroyOunces { get { return _TroyOunces; } set { if (value == _TroyOunces) return; _TroyOunces = value; OnPropertyChanged("TroyOunces"); } }

        private double _TroyPounds = Double.NaN;
        public double TroyPounds { get { return _TroyPounds; } set { if (value == _TroyPounds) return; _TroyPounds = value; OnPropertyChanged("TroyPounds"); } }

        private double _Tola = Double.NaN;
        public double Tola { get { return _Tola; } set { if (value == _Tola) return; _Tola = value; OnPropertyChanged("Tola"); } }

        private double _Ser = Double.NaN;
        public double Ser { get { return _Ser; } set { if (value == _Ser) return; _Ser = value; OnPropertyChanged("Ser"); } }

        private double _Maund = Double.NaN;
        public double Maund { get { return _Maund; } set { if (value == _Maund) return; _Maund = value; OnPropertyChanged("Maund"); } }


    }
}
