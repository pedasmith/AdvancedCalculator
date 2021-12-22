using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EquationSolver;

namespace AdvancedCalculator
{
    public class GeometrySlopeSolver : SolverINPC
    {
        public GeometrySlopeSolver()
        {
            // Y=MX+B M=(Y-B)/X X=(Y-B)/M B=Y-MX
            Equations.Add(new Equation("Y", new List<string>() { "M", "X", "B" }, () => { return M * X + B; }));
            Equations.Add(new Equation("M", new List<string>() { "Y", "X", "B" }, () => { return (Y-B)/X; }));
            Equations.Add(new Equation("X", new List<string>() { "Y", "M", "B" }, () => { return(Y-B)/M; }));
            Equations.Add(new Equation("B", new List<string>() { "Y", "M", "X" }, () => { return Y-(M*X); }));

            InitEquivLists();

        }

        private double _Y = Double.NaN;
        public double Y { get { return _Y; } set { if (value == _Y) return; _Y = value; OnPropertyChanged("Y"); } }

        private double _M = Double.NaN;
        public double M { get { return _M; } set { if (value == _M) return; _M = value; OnPropertyChanged("M"); } }

        private double _X = Double.NaN;
        public double X { get { return _X; } set { if (value == _X) return; _X = value; OnPropertyChanged("X"); } }

        private double _B = Double.NaN;
        public double B { get { return _B; } set { if (value == _B) return; _B = value; OnPropertyChanged("B"); } }
    }
}
