using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EquationSolver;

namespace AdvancedCalculator
{
    public class GeometryRightTriangleSolver : SolverINPC
    {
        public GeometryRightTriangleSolver()
        {
            Equations.Add(new Equation("Hypotenuse", new List<string>() { "Bottom", "Right" }, () => { return Math.Sqrt(Bottom * Bottom + Far * Far); }));
            Equations.Add(new Equation("Bottom", new List<string>() { "Hypotenuse", "Far" }, () => { return Math.Sqrt((Hypotenuse * Hypotenuse) - (Far * Far)); }));
            Equations.Add(new Equation("Far", new List<string>() { "Hypotenuse", "Bottom" }, () => { return Math.Sqrt((Hypotenuse * Hypotenuse) - (Bottom * Bottom)); }));
            Equations.Add(new Equation("Area", new List<string>() { "Far", "Bottom" }, () => { return (Bottom * Far) / 2.0; }));
            Equations.Add(new Equation("BottomAngleDegrees", new List<string>() { "Far", "Bottom" }, () => { return Math.Atan2(Far, Bottom) * Conversions.DEGREES_PER_RADIAN; }));
            Equations.Add(new Equation("FarAngleDegrees", new List<string>() { "Far", "Bottom" }, () => { return Math.Atan2(Bottom, Far) * Conversions.DEGREES_PER_RADIAN; }));
            Equations.Add(new Equation("BottomAngleRadians", new List<string>() { "Far", "Bottom" }, () => { return Math.Atan2(Far, Bottom); }));
            Equations.Add(new Equation("FarAngleRadians", new List<string>() { "Far", "Bottom" }, () => { return Math.Atan2(Bottom, Far); }));

            InitEquivLists();

        }

        private double _Hypotenuse = Double.NaN;
        public double Hypotenuse { get { return _Hypotenuse; } set { if (value == _Hypotenuse) return; _Hypotenuse = value; OnPropertyChanged("Hypotenuse"); } }

        private double _Bottom = Double.NaN;
        public double Bottom { get { return _Bottom; } set { if (value == _Bottom) return; _Bottom = value; OnPropertyChanged("Bottom"); } }

        private double _Far = Double.NaN;
        public double Far { get { return _Far; } set { if (value == _Far) return; _Far = value; OnPropertyChanged("Far"); } }

        private double _Area = Double.NaN;
        public double Area { get { return _Area; } set { if (value == _Area) return; _Area = value; OnPropertyChanged("Area"); } }

        private double _BottomAngleDegrees = Double.NaN;
        public double BottomAngleDegrees { get { return _BottomAngleDegrees; } set { if (value == _BottomAngleDegrees) return; _BottomAngleDegrees = value; OnPropertyChanged("BottomAngleDegrees"); } }

        private double _FarAngleDegrees = Double.NaN;
        public double FarAngleDegrees { get { return _FarAngleDegrees; } set { if (value == _FarAngleDegrees) return; _FarAngleDegrees = value; OnPropertyChanged("FarAngleDegrees"); } }

        private double _BottomAngleRadians = Double.NaN;
        public double BottomAngleRadians { get { return _BottomAngleRadians; } set { if (value == _BottomAngleRadians) return; _BottomAngleRadians = value; OnPropertyChanged("BottomAngleRadians"); } }

        private double _FarAngleRadians = Double.NaN;
        public double FarAngleRadians { get { return _FarAngleRadians; } set { if (value == _FarAngleRadians) return; _FarAngleRadians = value; OnPropertyChanged("FarAngleRadians"); } }


    }
}
