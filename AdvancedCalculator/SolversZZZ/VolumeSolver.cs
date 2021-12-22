using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EquationSolver;

namespace AdvancedCalculator
{
    public class VolumeSolver : SolverINPC
    {
        public VolumeSolver()
        {
            Equations.Add(new Equation("CupsDryUSA", "GallonsDryUSA", () => { return GallonsDryUSA * Conversions.CUPS_PER_GALLON_DRY_US; }));
            Equations.Add(new Equation("GallonsDryUSA", "CupsDryUSA", () => { return CupsDryUSA / Conversions.CUPS_PER_GALLON_DRY_US; }));

            Equations.Add(new Equation("PintsDryUSA", "GallonsDryUSA", () => { return GallonsDryUSA * Conversions.PINTS_PER_GALLON_DRY_US; }));
            Equations.Add(new Equation("GallonsDryUSA", "PintsDryUSA", () => { return PintsDryUSA / Conversions.PINTS_PER_GALLON_DRY_US; }));

            Equations.Add(new Equation("QuartsDryUSA", "GallonsDryUSA", () => { return GallonsDryUSA * Conversions.QUARTS_PER_GALLON_DRY_US; }));
            Equations.Add(new Equation("GallonsDryUSA", "QuartsDryUSA", () => { return QuartsDryUSA / Conversions.QUARTS_PER_GALLON_DRY_US; }));

            Equations.Add(new Equation("GallonsDryUSA", "BushelsDryUSA", () => { return BushelsDryUSA * Conversions.GALLONS_PER_BUSHEL_DRY_US; }));
            Equations.Add(new Equation("BushelsDryUSA", "GallonsDryUSA", () => { return GallonsDryUSA / Conversions.GALLONS_PER_BUSHEL_DRY_US; }));

            Equations.Add(new Equation("PecksDryUSA", "BushelsDryUSA", () => { return BushelsDryUSA * Conversions.PECK_PER_BUSHEL_DRY_US; }));
            Equations.Add(new Equation("BushelsDryUSA", "PecksDryUSA", () => { return PecksDryUSA / Conversions.PECK_PER_BUSHEL_DRY_US; }));

            Equations.Add(new Equation("PecksDryUSA", "BushelsDryUSA", () => { return BushelsDryUSA * Conversions.PECK_PER_BUSHEL_DRY_US; }));
            Equations.Add(new Equation("BushelsDryUSA", "PecksDryUSA", () => { return PecksDryUSA / Conversions.PECK_PER_BUSHEL_DRY_US; }));

            Equations.Add(new Equation("Liters", "QuartsDryUSA", () => { return QuartsDryUSA * Conversions.DRY_US_QUARTS_PER_LITER; }));
            Equations.Add(new Equation("QuartsDryUSA", "Liters", () => { return Liters / Conversions.DRY_US_QUARTS_PER_LITER; }));

            Equations.Add(new Equation("Pounds", new List<string>() { "BushelsDryUSA", "PoundsPerBushelDryUSA" }, () => { return BushelsDryUSA * PoundsPerBushelDryUSA; }));
            Equations.Add(new Equation("PoundsPerBushelDryUSA", new List<string>() { "BushelsDryUSA", "Pounds" }, () => { return Pounds / BushelsDryUSA; }));
            Equations.Add(new Equation("BushelsDryUSA", new List<string>() { "Pounds", "PoundsPerBushelDryUSA" }, () => { return Pounds/ PoundsPerBushelDryUSA; }));

            InitEquivLists();

        }

        private double _CupsDryUSA = Double.NaN;
        public double CupsDryUSA { get { return _CupsDryUSA; } set { if (value == _CupsDryUSA) return; _CupsDryUSA = value; OnPropertyChanged("CupsDryUSA"); } }

        private double _PintsDryUSA = Double.NaN;
        public double PintsDryUSA { get { return _PintsDryUSA; } set { if (value == _PintsDryUSA) return; _PintsDryUSA = value; OnPropertyChanged("PintsDryUSA"); } }

        private double _QuartsDryUSA = Double.NaN;
        public double QuartsDryUSA { get { return _QuartsDryUSA; } set { if (value == _QuartsDryUSA) return; _QuartsDryUSA = value; OnPropertyChanged("QuartsDryUSA"); } }

        private double _GallonsDryUSA = Double.NaN;
        public double GallonsDryUSA { get { return _GallonsDryUSA; } set { if (value == _GallonsDryUSA) return; _GallonsDryUSA = value; OnPropertyChanged("GallonsDryUSA"); } }

        private double _PecksDryUSA = Double.NaN;
        public double PecksDryUSA { get { return _PecksDryUSA; } set { if (value == _PecksDryUSA) return; _PecksDryUSA = value; OnPropertyChanged("PecksDryUSA"); } }

        private double _BushelsDryUSA = Double.NaN;
        public double BushelsDryUSA { get { return _BushelsDryUSA; } set { if (value == _BushelsDryUSA) return; _BushelsDryUSA = value; OnPropertyChanged("BushelsDryUSA"); } }

        private double _Liters = Double.NaN;
        public double Liters { get { return _Liters; } set { if (value == _Liters) return; _Liters = value; OnPropertyChanged("Liters"); } }

        private double _PoundsPerBushelDryUSA = Double.NaN;
        public double PoundsPerBushelDryUSA { get { return _PoundsPerBushelDryUSA; } set { if (value == _PoundsPerBushelDryUSA) return; _PoundsPerBushelDryUSA = value; OnPropertyChanged("PoundsPerBushelDryUSA"); } }

        // Yes, this is really a weight -- but it's a link over to the weight section.
        private double _Pounds = Double.NaN;
        public double Pounds { get { return _Pounds; } set { if (value == _Pounds) return; _Pounds = value; OnPropertyChanged("Pounds"); } }
    }
}
