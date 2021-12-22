using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EquationSolver;

namespace AdvancedCalculator
{
    public class GeometryCircleSolver : SolverINPC
    {
        public GeometryCircleSolver()
        {
            Equations.Add(new Equation("Diameter", "Radius", () => { return Radius * 2; }));
            Equations.Add(new Equation("Radius", "Diameter", () => { return Diameter / 2; }));

            Equations.Add(new Equation("Diameter", "Circumference", () => { return Circumference / Math.PI; }));
            Equations.Add(new Equation("Circumference", "Diameter", () => { return Diameter * Math.PI; }));

            Equations.Add(new Equation("Radius", "Area", () => { return Math.Sqrt (Area / Math.PI); }));
            Equations.Add(new Equation("Area", "Radius", () => { return Radius * Radius * Math.PI; }));


            InitEquivLists();

        }

        private double _Radius = Double.NaN;
        public double Radius { get { return _Radius; } set { if (value == _Radius) return; _Radius = value; OnPropertyChanged("Radius"); } }

        private double _Diameter = Double.NaN;
        public double Diameter { get { return _Diameter; } set { if (value == _Diameter) return; _Diameter = value; OnPropertyChanged("Diameter"); } }

        private double _Circumference = Double.NaN;
        public double Circumference { get { return _Circumference; } set { if (value == _Circumference) return; _Circumference = value; OnPropertyChanged("Circumference"); } }

        private double _Area = Double.NaN;
        public double Area { get { return _Area; } set { if (value == _Area) return; _Area = value; OnPropertyChanged("Area"); } }

    }
}
