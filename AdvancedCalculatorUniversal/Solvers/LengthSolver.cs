using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EquationSolver;

namespace AdvancedCalculator
{
    public class LengthSolver :SolverINPC
    {
        public LengthSolver()
        {
            Equations.Add(new Equation("Inches", "Centimeters", () => { return Centimeters / Conversions.CENTIMETERS_PER_INCH; }));
            Equations.Add(new Equation("Centimeters", "Inches", () => { return Inches * Conversions.CENTIMETERS_PER_INCH; }));

            Equations.Add(new Equation("Inches", "Feet", () => { return Feet * Conversions.INCHES_PER_FOOT; }));
            Equations.Add(new Equation("Feet", "Inches", () => { return Inches / Conversions.INCHES_PER_FOOT; }));

            Equations.Add(new Equation("InchesRemainder", "Feet", () =>
            {
                double truncate = Math.Truncate(Feet);
                double remainder = Feet - truncate;
                return remainder * Conversions.INCHES_PER_FOOT;
            }));

            Equations.Add(new Equation("FeetRemainder", "Feet", () =>
            {
                double truncate = Math.Truncate(Feet);
                return truncate;
            }));

            Equations.Add(new Equation("Miles", "Feet", () => { return Feet / Conversions.FEET_PER_MILE; }));
            Equations.Add(new Equation("Feet", "Miles", () => { return Miles * Conversions.FEET_PER_MILE; }));

            Equations.Add(new Equation("Yards", "Feet", () => { return Feet / Conversions.FEET_PER_YARD; }));
            Equations.Add(new Equation("Feet", "Yards", () => { return Yards * Conversions.FEET_PER_YARD; }));

            Equations.Add(new Equation("Meters", "Millimeters", () => { return Millimeters / 1000; }));
            Equations.Add(new Equation("Millimeters", "Meters", () => { return Meters * 1000; }));

            Equations.Add(new Equation("Meters", "Centimeters", () => { return Centimeters / 100; }));
            Equations.Add(new Equation("Centimeters", "Meters", () => { return Meters * 100; }));

            Equations.Add(new Equation("Meters", "Kilometers", () => { return Kilometers * 1000; }));
            Equations.Add(new Equation("Kilometers", "Meters", () => { return Meters / 1000; }));

            // LegoBrickStuds LegoBrickHeight LegoPlateHeight
            Equations.Add(new Equation("LegoBrickStuds", "Millimeters", () => { return Millimeters / 8; }));
            Equations.Add(new Equation("Millimeters", "LegoBrickStuds", () => { return LegoBrickStuds * 8; }));

            Equations.Add(new Equation("LegoBrickHeight", "Millimeters", () => { return Millimeters / 9.6; }));
            Equations.Add(new Equation("Millimeters", "LegoBrickHeight", () => { return LegoBrickHeight * 9.6; }));

            Equations.Add(new Equation("LegoPlateHeight", "Millimeters", () => { return Millimeters / 3.2; }));
            Equations.Add(new Equation("Millimeters", "LegoPlateHeight", () => { return LegoPlateHeight * 3.2; }));



            InitEquivLists();

        }

        private double _Inches = Double.NaN;
        public double Inches { get { return _Inches; } set { if (value == _Inches) return; _Inches = value; OnPropertyChanged("Inches"); } }

        private double _InchesRemainder = Double.NaN;
        public double InchesRemainder { get { return _InchesRemainder; } set { if (value == _InchesRemainder) return; _InchesRemainder = value; OnPropertyChanged("InchesRemainder"); } }

        private double _FeetRemainder = Double.NaN;
        public double FeetRemainder { get { return _FeetRemainder; } set { if (value == _FeetRemainder) return; _FeetRemainder = value; OnPropertyChanged("FeetRemainder"); } }

        private double _Feet = Double.NaN;
        public double Feet { get { return _Feet; } set { if (value == _Feet) return; _Feet = value; OnPropertyChanged("Feet"); } }

        private double _Yards = Double.NaN;
        public double Yards { get { return _Yards; } set { if (value == _Yards) return; _Yards = value; OnPropertyChanged("Yards"); } }

        private double _Miles = Double.NaN;
        public double Miles { get { return _Miles; } set { if (value == _Miles) return; _Miles = value; OnPropertyChanged("Miles"); } }

        private double _Centimeters = Double.NaN;
        public double Centimeters { get { return _Centimeters; } set { if (value == _Centimeters) return; _Centimeters = value; OnPropertyChanged("Centimeters"); } }

        private double _Millimeters = Double.NaN;
        public double Millimeters { get { return _Millimeters; } set { if (value == _Millimeters) return; _Millimeters = value; OnPropertyChanged("Millimeters"); } }

        private double _Meters = Double.NaN;
        public double Meters { get { return _Meters; } set { if (value == _Meters) return; _Meters = value; OnPropertyChanged("Meters"); } }

        private double _Kilometers = Double.NaN;
        public double Kilometers { get { return _Kilometers; } set { if (value == _Kilometers) return; _Kilometers = value; OnPropertyChanged("Kilometers"); } }

        // https://www.lego.com/en-us/legal/notices-and-policies/fair-play?locale=en-us
        // LEGO® brand building blocks units include
        //      LegoBrickStud is 8mm and is the distance from the center of a "stud" to the nexts stud
        //      LegoBrickHeight is 9.6mm (1.2 times the BlockStud size). This is the height of a brick without the stud height.
        //      LegoPlateHeight is 3.2mm (0.4 times the BlockStud size). Three plates are the height of a block (without the stud height)
        //
        // Sources include:
        // https://www.bartneck.de/2019/04/21/lego-brick-dimensions-and-measurements/
        // https://www.brickingohio.com/blog/lego-geometry-101

        private double _LegoBrickStuds = Double.NaN;
        public double LegoBrickStuds { get { return _LegoBrickStuds; } set { if (value == _LegoBrickStuds) return; _LegoBrickStuds = value; OnPropertyChanged("LegoBrickStuds"); } }

        private double _LegoBrickHeight = Double.NaN;
        public double LegoBrickHeight { get { return _LegoBrickHeight; } set { if (value == _LegoBrickHeight) return; _LegoBrickHeight = value; OnPropertyChanged("LegoBrickHeight"); } }

        private double _LegoPlateHeight = Double.NaN;
        public double LegoPlateHeight { get { return _LegoPlateHeight; } set { if (value == _LegoPlateHeight) return; _LegoPlateHeight = value; OnPropertyChanged("LegoPlateHeight"); } }

    }
}
