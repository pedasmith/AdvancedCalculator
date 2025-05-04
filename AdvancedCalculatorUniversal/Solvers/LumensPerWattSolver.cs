using EquationSolver;
using System;

namespace AdvancedCalculator
{
    public class LumensPerWattSolver : SolverINPC
    {
        public LumensPerWattSolver()
        {
            // Conversion where the common value is watts, and the lumens for different
            // technologies are provided. This wasn't useful and is commented out.
#if NOT_USEFUL
            Equations.Add(new Equation("LumensGasMantle", "Watts", () => { return Watts * Conversions.LUMENS_PER_WATT_MIN_GAS_MANTLE; }));
            Equations.Add(new Equation("Watts", "LumensGasMantle", () => { return LumensGasMantle / Conversions.LUMENS_PER_WATT_MIN_GAS_MANTLE; }));

            Equations.Add(new Equation("LumensHalogen", "Watts", () => { return Watts * Conversions.LUMENS_PER_WATT_MIN_HALOGEN; }));
            Equations.Add(new Equation("Watts", "LumensHalogen", () => { return LumensHalogen / Conversions.LUMENS_PER_WATT_MIN_HALOGEN; }));

            Equations.Add(new Equation("LumensLed", "Watts", () => { return Watts * Conversions.LUMENS_PER_WATT_MIN_LED; }));
            Equations.Add(new Equation("Watts", "LumensLed", () => { return LumensLed / Conversions.LUMENS_PER_WATT_MIN_LED; }));

            Equations.Add(new Equation("LumensTungsten", "Watts", () => { return Watts * Conversions.LUMENS_PER_WATT_MIN_TUNGSTEN; }));
            Equations.Add(new Equation("Watts", "LumensTungsten", () => { return LumensTungsten / Conversions.LUMENS_PER_WATT_MIN_TUNGSTEN; }));
#endif

            // Conversion where the common value is lumens, and the watts for different
            // technologies are provided. 
            Equations.Add(new Equation("GasMantleWatts", "Lumens", () => { return Lumens / Conversions.LUMENS_PER_WATT_MIN_GAS_MANTLE; }));
            Equations.Add(new Equation("Lumens", "GasMantleWatts", () => { return GasMantleWatts * Conversions.LUMENS_PER_WATT_MIN_GAS_MANTLE; }));

            Equations.Add(new Equation("HalogenWatts", "Lumens", () => { return Lumens / Conversions.LUMENS_PER_WATT_MIN_HALOGEN; }));
            Equations.Add(new Equation("Lumens", "HalogenWatts", () => { return HalogenWatts * Conversions.LUMENS_PER_WATT_MIN_HALOGEN; }));

            Equations.Add(new Equation("LedWatts", "Lumens", () => { return Lumens / Conversions.LUMENS_PER_WATT_MIN_LED; }));
            Equations.Add(new Equation("Lumens", "LedWatts", () => { return LedWatts * Conversions.LUMENS_PER_WATT_MIN_LED; }));

            Equations.Add(new Equation("TungstenWatts", "Lumens", () => { return Lumens / Conversions.LUMENS_PER_WATT_MIN_TUNGSTEN; }));
            Equations.Add(new Equation("Lumens", "TungstenWatts", () => { return TungstenWatts * Conversions.LUMENS_PER_WATT_MIN_TUNGSTEN; }));


            InitEquivLists();

        }

        // Given a certain number of Lumens, how many watts would different technologies use?
        // Typical usage: enter the number of TungestenWatts to calculate Lumens, and from that the watts for all
        // other values are calculated and displayed. Displayed to the user: number of lumens, and number of watts for
        // different tec.

        private double _GasMantleWatts = Double.NaN;
        public double GasMantleWatts { get { return _GasMantleWatts; } set { if (value == _GasMantleWatts) return; _GasMantleWatts = value; OnPropertyChanged("GasMantleWatts"); } }

        private double _HalogenWatts = Double.NaN;
        public double HalogenWatts { get { return _HalogenWatts; } set { if (value == _HalogenWatts) return; _HalogenWatts = value; OnPropertyChanged("HalogenWatts"); } }

        private double _LedWatts = Double.NaN;
        public double LedWatts { get { return _LedWatts; } set { if (value == _LedWatts) return; _LedWatts = value; OnPropertyChanged("LedWatts"); } }

        private double _TungstenWatts = Double.NaN;
        public double TungstenWatts { get { return _TungstenWatts; } set { if (value == _TungstenWatts) return; _TungstenWatts = value; OnPropertyChanged("TungstenWatts"); } }

        private double _Lumens = Double.NaN;
        public double Lumens { get { return _Lumens; } set { if (value == _Lumens) return; _Lumens = value; OnPropertyChanged("Lumens"); } }



        private double _Watts = Double.NaN;
        public double Watts { get { return _Watts; } set { if (value == _Watts) return; _Watts = value; OnPropertyChanged("Watts"); } }



        private double _LumensGasMantle = Double.NaN;
        public double LumensGasMantle { get { return _LumensGasMantle; } set { if (value == _LumensGasMantle) return; _LumensGasMantle = value; OnPropertyChanged("LumensGasMantle"); } }

        private double _LumensHalogen = Double.NaN;
        public double LumensHalogen { get { return _LumensHalogen; } set { if (value == _LumensHalogen) return; _LumensHalogen = value; OnPropertyChanged("LumensHalogen"); } }

        private double _LumensLed = Double.NaN;
        public double LumensLed { get { return _LumensLed; } set { if (value == _LumensLed) return; _LumensLed = value; OnPropertyChanged("LumensLed"); } }

        private double _LumensTungsten = Double.NaN;
        public double LumensTungsten { get { return _LumensTungsten; } set { if (value == _LumensTungsten) return; _LumensTungsten = value; OnPropertyChanged("LumensTungsten"); } }

    }
}