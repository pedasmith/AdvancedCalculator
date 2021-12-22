using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EquationSolver;

namespace AdvancedCalculator
{
    public class AreaSolver : SolverINPC
    {
        public AreaSolver()
        {
            Equations.Add(new Equation("Inches", "Centimeters", () => { return Centimeters / (Conversions.CENTIMETERS_PER_INCH * Conversions.CENTIMETERS_PER_INCH); }));
            Equations.Add(new Equation("Centimeters", "Inches", () => { return Inches * (Conversions.CENTIMETERS_PER_INCH * Conversions.CENTIMETERS_PER_INCH); }));

            Equations.Add(new Equation("Inches", "Feet", () => { return Feet * (Conversions.INCHES_PER_FOOT * Conversions.INCHES_PER_FOOT); }));
            Equations.Add(new Equation("Feet", "Inches", () => { return Inches / (Conversions.INCHES_PER_FOOT * Conversions.INCHES_PER_FOOT); }));

            Equations.Add(new Equation("Miles", "Feet", () => { return Feet / (Conversions.FEET_PER_MILE * Conversions.FEET_PER_MILE); }));
            Equations.Add(new Equation("Feet", "Miles", () => { return Miles * (Conversions.FEET_PER_MILE * Conversions.FEET_PER_MILE); }));

            Equations.Add(new Equation("Miles", "Acres", () => { return Acres / Conversions.ACRES_PER_SQUARE_MILE; }));
            Equations.Add(new Equation("Acres", "Miles", () => { return Miles * Conversions.ACRES_PER_SQUARE_MILE; }));

            Equations.Add(new Equation("Yards", "Feet", () => { return Feet / (Conversions.FEET_PER_YARD * Conversions.FEET_PER_YARD); }));
            Equations.Add(new Equation("Feet", "Yards", () => { return Yards * ( Conversions.FEET_PER_YARD *  Conversions.FEET_PER_YARD); }));

            Equations.Add(new Equation("Meters", "Centimeters", () => { return Centimeters / (100 * 100); }));
            Equations.Add(new Equation("Centimeters", "Meters", () => { return Meters * (100 * 100); }));

            Equations.Add(new Equation("Meters", "Kilometers", () => { return Kilometers * (1000 * 1000); }));
            Equations.Add(new Equation("Kilometers", "Meters", () => { return Meters / (1000 * 1000); }));

            Equations.Add(new Equation("Meters", "Hectares", () => { return Hectares * Conversions.SQUARE_METERS_PER_HECTARE; }));
            Equations.Add(new Equation("Hectares", "Meters", () => { return Meters / Conversions.SQUARE_METERS_PER_HECTARE; }));

            InitEquivLists();

        }

        private double _Inches = Double.NaN;
        public double Inches { get { return _Inches; } set { if (value == _Inches) return; _Inches = value; OnPropertyChanged("Inches"); } }

        private double _Feet = Double.NaN;
        public double Feet { get { return _Feet; } set { if (value == _Feet) return; _Feet = value; OnPropertyChanged("Feet"); } }

        private double _Yards = Double.NaN;
        public double Yards { get { return _Yards; } set { if (value == _Yards) return; _Yards = value; OnPropertyChanged("Yards"); } }

        private double _Miles = Double.NaN;
        public double Miles { get { return _Miles; } set { if (value == _Miles) return; _Miles = value; OnPropertyChanged("Miles"); } }

        private double _Hectares = Double.NaN;
        public double Hectares { get { return _Hectares; } set { if (value == _Hectares) return; _Hectares = value; OnPropertyChanged("Hectares"); } }

        private double _Acres = Double.NaN;
        public double Acres { get { return _Acres; } set { if (value == _Acres) return; _Acres = value; OnPropertyChanged("Acres"); } }

        private double _Centimeters = Double.NaN;
        public double Centimeters { get { return _Centimeters; } set { if (value == _Centimeters) return; _Centimeters = value; OnPropertyChanged("Centimeters"); } }

        private double _Meters = Double.NaN;
        public double Meters { get { return _Meters; } set { if (value == _Meters) return; _Meters = value; OnPropertyChanged("Meters"); } }

        private double _Kilometers = Double.NaN;
        public double Kilometers { get { return _Kilometers; } set { if (value == _Kilometers) return; _Kilometers = value; OnPropertyChanged("Kilometers"); } }

    }
}
