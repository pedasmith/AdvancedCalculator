using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Globalization;
using BCBasic;

namespace AdvancedCalculator
{
    public interface IFormatDouble
    {
        string DoubleToString(double d);
    }
    public class D2S : IFormatDouble
    {
        public string DoubleToString(double d)
        {
            return d.ToString();
        }
    }

    public class SimpleCalculator: INotifyPropertyChanged, IFormatDouble
    {
        public const int NMemory = 20;
        public const string DefaultDisplaySpecifier = "std";
        public ICalculatorConnection CalculatorConnection = null;
        public IShare Share { get; set; }
        CalculatorLog Log = new CalculatorLog();

        private double _preferredDisplayRotation = 0;
        public double PreferredDisplayRotation {  get { return _preferredDisplayRotation; } set { if (value == _preferredDisplayRotation) return;  _preferredDisplayRotation = value;  NotifyPropertyChanged("PreferredDisplayRotation"); } }

        public SimpleCalculator()
        {
            PropertyChanged += SimpleCalculator_PropertyChanged;
            DisplaySpecifier = DefaultDisplaySpecifier;
            DisplayPrecision = "4";
            DisplaySize = "2";
            Memory = new ObservableCollection<string>();
            MemoryNames = new ObservableCollection<string>();
            for (int i = 0; i < NMemory; i++)
            {
                Memory.Add (""); // 0.0);
                MemoryNames.Add (String.Format("Memory{0}", i));
            }
            MemoryNames.CollectionChanged += MemoryNames_CollectionChanged;
            Memory.CollectionChanged += MemoryNames_CollectionChanged;
            VisibleTrigUnits = TrigUnits.Degrees;
            ResultString = "";
            ResultDouble = 0.0;
            PartialResults = 0.0;
            DoClearAll();
            int NError = Test();
            DoClearAll();
        }

        public int MemoryMaxIndex { get {  return SimpleCalculator.NMemory;} }
        
        public int MemoryNameIndex(string name)
        {
            for (int i = 0; i < MemoryNames.Count; i++)
            {
                if (MemoryNames[i] == name)
                {
                    return i;
                }
            }
            return -1;
        }
        public string GetMemoryAt(int index, string defaultValue)
        {
            if (index < 0 || index >= Memory.Count) return defaultValue;
            return Memory[index];
        }


        public DoSaveMemory DoSaveMemoryObject { get; set; }
        private void SaveMemory()
        {
            if (DoSaveMemoryObject != null)
            {
                DoSaveMemoryObject.SaveMemory();
            }
        }

        void MemoryNames_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            SaveMemory();
        }

        void SimpleCalculator_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ErrorMessage")
            {
                if (ErrorMessage == null || ErrorMessage == "") ErrorOpacity = 0.0;
                else ErrorOpacity = 1.0;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        // States in the calculator for chaining.
        // L + M = <result>
        // L + M = <result> = <result + M> = <result + M + M>
        // L + M = <result> + N = <result+N> 
        // L + M = <result> + N = <result+N> = <result+N+N>


        // Future: turn these into a state which I can save
        public double PartialResults { get; set; }
        public string PartialAction { get; set; }
        public bool MustNegate { get; set; }
        public int NPartialInARow { get; set; }
        public string SavedPartialAction { get; set; } // To support chaining.
        public enum Actions { None, Partial, Final, MonoOp, Numerical };
        public  Actions LastAction;


        public enum TrigUnits { Radians, Degrees };
        private TrigUnits _VisibleTrigUnits = TrigUnits.Degrees;
        public  TrigUnits VisibleTrigUnits 
        {
            get { return _VisibleTrigUnits; }
            set
            {
                if (value != _VisibleTrigUnits)
                {
                    _VisibleTrigUnits = value;
                    NotifyPropertyChanged("VisibleTrigUnits");
                    NotifyPropertyChanged("StringTrigUnits");
                }
            }
        }

        // Exists just to support auto-save
        public string StringTrigUnits
        {
            get
            {
                string str = VisibleTrigUnits.ToString();
                return str;
            }
            set
            {
                switch (value)
                {
                    case "Radians": VisibleTrigUnits = TrigUnits.Radians; break;
                    case "Degrees": VisibleTrigUnits = TrigUnits.Degrees; break;
                }
            }
        }

        public String _ErrorMessage;
        public String ErrorMessage
        {
            get { return _ErrorMessage; }
            set
            {
                if (value != _ErrorMessage)
                {
                    _ErrorMessage = value;
                    NotifyPropertyChanged("ErrorMessage");
                }
            }
        }

        public double _ErrorOpacity;
        public double ErrorOpacity
        {
            get { return _ErrorOpacity; }
            set
            {
                if (value != _ErrorOpacity)
                {
                    _ErrorOpacity = value;
                    NotifyPropertyChanged("ErrorOpacity");
                }
            }
        }

        public double _PrecisionOpacity;
        public double PrecisionOpacity
        {
            get { return _PrecisionOpacity; }
            set
            {
                if (value != _PrecisionOpacity)
                {
                    _PrecisionOpacity = value;
                    NotifyPropertyChanged("PrecisionOpacity");
                }
            }
        }

        public double _SizeOpacity;
        public double SizeOpacity
        {
            get { return _SizeOpacity; }
            set
            {
                if (value != _SizeOpacity)
                {
                    _SizeOpacity = value;
                    NotifyPropertyChanged("SizeOpacity");
                }
            }
        }

        public ObservableCollection<string> Memory { get; set; }
        public ObservableCollection<string> MemoryNames { get; set; }

        private Random R = new Random();


        // ResultDouble flows to ResultString -- but not the other way round!
        private string _ResultString;
        public string ResultString
        {
            get { return _ResultString; }

            set
            {
                if (value != _ResultString)
                {
                    _ResultString = value;
                    if (Share != null)
                    {
                        Share.ShareString = value;
                        Share.ShareTitle = "Best Calculator calculated value";
                    }
                    NotifyPropertyChanged("ResultString");
                }
            }
        }

        private double _ResultDouble;
        private bool SetResultDoubleRegardless = false;
        public double ResultDouble
        {
            get { return _ResultDouble; }

            set
            {
                if (value != _ResultDouble || SetResultDoubleRegardless)
                {
                    SetResultDoubleRegardless = true;
                    _ResultDouble = value;
                    NotifyPropertyChanged("ResultDouble");
                    D2S();
                }
            }
        }

        private int baseToNBits(int displayBase)
        {
            switch (displayBase)
            {
                case 16: return 4;
                case 8: return 3;
                case 2: return 1;
            }
            return 4; // default is hex.
        }
        private UInt32 baseToMask(int displayBase)
        {
            switch (displayBase)
            {
                case 16: return 0x0F;
                case 8: return 0x07;
                case 2: return 0x01;
            }
            return 0x0F; // default is hex.
        }
        private string D2SBase(double input, int displayBase)
        {
            UInt32 value = (UInt32)Math.Truncate (input);
            string Retval = "";
            if (DisplaySpecifier == "base10")
            {
                Retval = String.Format("{0}", value);
            }
            else
            {
                UInt32 mask = baseToMask(displayBase);
                int nshift = baseToNBits(displayBase);
                int nloop = (int)Math.Ceiling((double)64 / (double)nshift);
                for (int i = 0; i < nloop; i++)
                {
                    UInt32 nibble = (value & mask);
                    Retval = string.Format("{0:X}", nibble) + Retval;
                    value = value >> nshift;
                    if (value == 0) break; // leading zero suppression.
                }
            }
            return Retval;
        }
        private void D2S()
        {
            switch (DisplaySpecifier)
            {
                case "base16":
                    ResultString = D2SBase(ResultDouble, 16);
                    break;
                case "base10":
                    ResultString = D2SBase(ResultDouble, 10);
                    break;
                case "base8":
                    ResultString = D2SBase(ResultDouble, 8);
                    break;
                case "base2":
                    ResultString = D2SBase(ResultDouble, 2);
                    break;
                case "std":
                    ResultString = DoubleToString(ResultDouble);
                    break;
                case "eng":
                    ResultString = DoubleToEngineering(ResultDouble, DisplayPrecision);
                    break;
                default:
                    ResultString = String.Format(DisplayFormat, ResultDouble);
                    break;
            }
        }

        static string DoubleToEngineering(double value, string displayPrecision)
        {
            string Retval;
            if (double.IsNaN(value)
                || double.IsInfinity(value)
                || double.IsNegativeInfinity(value)
                || double.IsPositiveInfinity(value)
                || value == 0.0
                )
            {
                Retval  = String.Format("{0:" + "F" + displayPrecision + "}", value);
                return Retval;
            }
            bool isNeg = value < 0;
            if (isNeg) value = -value;

            int exp = (int)(Math.Floor(Math.Log10(value) / 3.0) * 3.0);
            int powerToRaise = -exp;
            double newValue = value;
            // Problem: epsilon is something-324
            // The biggest possible number is somethinge306
            // You simply can't do a Math.Power (10, 324), it becomes infiniity.
            if (powerToRaise > 300)
            {
                powerToRaise -= 300;
                newValue = newValue * Math.Pow(10.0, 300);
            }

            newValue = newValue * Math.Pow(10.0, powerToRaise);

            // I don't know when this below is triggered.
            if (newValue >= 1000.0)
            {
                newValue = newValue / 1000.0;
                exp = exp + 3;
            }
            var fmt = "{0:F" + displayPrecision + "}";
            Retval = String.Format (fmt, newValue);
            if (exp != 0) Retval += String.Format("e{0}", exp);
            if (isNeg) Retval = "-" + Retval;
            return Retval;
        }

        private static int TestDoubleToEngineeringOne(double value, string expected)
        {
            var fakePrecision = "4";
            int NError = 0;
            var actual = DoubleToEngineering(value, fakePrecision);
            if (actual != expected)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: DoubleToEngineering({value}) expected {expected} actual {actual}");
                NError++;
            }
            return NError;
        }

        public static int TestDoubleToEngineering()
        {
            int NError = 0;
            NError += TestDoubleToEngineeringOne(0, "0.0000");
            NError += TestDoubleToEngineeringOne(1, "1.0000");
            NError += TestDoubleToEngineeringOne(2, "2.0000");
            NError += TestDoubleToEngineeringOne(3, "3.0000");
            NError += TestDoubleToEngineeringOne(10, "10.0000");
            NError += TestDoubleToEngineeringOne(999, "999.0000");
            NError += TestDoubleToEngineeringOne(1000, "1.0000e3");

            NError += TestDoubleToEngineeringOne(1.234E21, "1.2340e21");

            NError += TestDoubleToEngineeringOne(-1, "-1.0000");
            NError += TestDoubleToEngineeringOne(-999, "-999.0000");
            NError += TestDoubleToEngineeringOne(-1000, "-1.0000e3");


            NError += TestDoubleToEngineeringOne(0.1, "100.0000e-3");
            NError += TestDoubleToEngineeringOne(0.02, "20.0000e-3");
            NError += TestDoubleToEngineeringOne(0.003, "3.0000e-3");
            NError += TestDoubleToEngineeringOne(0.0004, "400.0000e-6");
            NError += TestDoubleToEngineeringOne(0.00005, "50.0000e-6");

            NError += TestDoubleToEngineeringOne(double.NaN, "NaN");
            NError += TestDoubleToEngineeringOne(double.PositiveInfinity, "∞");
            NError += TestDoubleToEngineeringOne(double.NegativeInfinity, "-∞");
            NError += TestDoubleToEngineeringOne(double.Epsilon, "4.9407e-324");
            NError += TestDoubleToEngineeringOne(double.MaxValue, "179.7693e306");
            NError += TestDoubleToEngineeringOne(double.MinValue, "-179.7693e306");

            return NError;
        }
        public string DoubleToString(double d)
        {
            string Retval = d.ToString();
            var dp = Int32.Parse(DisplayPrecision);
            string fmt = "F" + DisplayPrecision;
            fmt = "###################0.";
            for (int i = 0; i < dp; i++) fmt += "#";
            const int MaxTotalLength = 12; // Reign in the size -- this is what it is for phones.
            const int MaxIntLength = 12;
            Retval = d.ToString(fmt);
            // If more than __ long, or more than __ + precision, use
            // exp. instead
            if (Retval.Length > MaxTotalLength || Retval.Length > (MaxIntLength + dp))
            {
                fmt = "E" + DisplayPrecision;
                Retval = d.ToString(fmt);
            }
            var decimalPoint = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            if (Retval.EndsWith(decimalPoint)) // Remove trailing periods
            {
                Retval = Retval.Remove(Retval.Length - decimalPoint.Length);
            }
            return Retval;
        }

        public string DisplayFormat
        {
            get { return "{0:" + DisplaySpecifier + DisplayPrecision + "}"; }
            // DisplayFormat = "{0:E4}";
        }

        private string _DisplaySpecifier;
        public string DisplaySpecifier // G E F N P base2/8/10/16
        {
            get { return _DisplaySpecifier; }

            set
            {
                if (value == null)
                {
                    Log.WriteWithTime("ERROR: SimpleCalculator: Set DisplaySpecifier: set is null\r\n");
                    // Force a reset
                    return;
                }
                if (value != _DisplaySpecifier)
                {
                    _DisplaySpecifier = value;
                    NotifyPropertyChanged("DisplaySpecifier");
                    NotifyPropertyChanged("DisplaySpecifierUI");
                    NotifyPropertyChanged("DisplayFormat");
                    if (ShouldConvertSize())
                    {
                        var result = ConvertSize(ResultDouble);
                        ResultDouble = result;
                    }
                    D2S(); 
                }
            }
        }

        public string DisplaySpecifierUI
        {
            get 
            {
                switch (DisplaySpecifier)
                {
                    case "std": return "Standard";
                    case "eng": return "Engineering";
                    case "G": return "Compact";
                    case "E": return "Exponent";
                    case "F": return "Fixed";
                    case "N": return "Natural";
                    case "P": return "Percent";

                    case "base8": return "Octal";
                    case "base10": return "Base10"; // Theory: calling it "decimal" confuses people
                    case "base16": return "Hex";
                    case "base2": return "Binary";
                }
                return "?";
            }

        }


        private string _DisplayPrecision = "4";
        public string DisplayPrecision
        {
            get { return _DisplayPrecision; }

            set
            {
                if (value != _DisplayPrecision)
                {
                    _DisplayPrecision = value;
                    NotifyPropertyChanged("DisplayPrecision");
                    NotifyPropertyChanged("DisplayFormat");
                    D2S();
                }
            }
        }

        /*
        public void SwitchToProgrammers()
        {
            DisplaySize = "4";
            DisplaySpecifier = "base10";
            if (ShouldConvertSize())
            {
                var result = ConvertSize(ResultDouble);
                ResultDouble = result;
            }
            D2S();
        }
         */

        private string _DisplaySize;
        public string DisplaySize
        {
            get { return _DisplaySize; }

            set
            {
                if (value != _DisplaySize)
                {
                    _DisplaySize = value;
                    NotifyPropertyChanged("DisplaySize");
                    NotifyPropertyChanged("DisplaySizeUI");
                    NotifyPropertyChanged("DisplayFormat");
                    if (ShouldConvertSize())
                    {
                        var result = ConvertSize(ResultDouble);
                        ResultDouble = result;
                    }

                    D2S();
                }
            }
        }

        public string DisplaySizeUI
        {
            get
            {
                switch (DisplaySize)
                {
                    case "1": return "BYTE";
                    case "2": return "WORD";
                    case "4": return "DWORD";
                    //case "8": return "QUAD";
                }
                return "";
            }
        }

        public int DisplaySizeInt // 1 2 4
        {
            get
            {
                int val = Int32.Parse(DisplaySize); 
                return val;
            }
        }

        public int DisplaySizeBitsInt // 1 2 4 ==> 8 16 32
        {
            get
            {
                int val = 8 * Int32.Parse(DisplaySize);
                return val;
            }
        }


        public bool ShouldConvertSize()
        {
            bool Retval = DisplaySpecifier.StartsWith("base");
            return Retval;
        }
        public long ConvertSize(double value)
        {
            long Retval = (long)value;
            switch (DisplaySize)
            {
                case "1": Retval = Retval & 0xFF; break;
                case "2": Retval = Retval & 0xFFFF; break;
                case "4": Retval = Retval & 0xFFFFFFFF; break;
            }
            return Retval;
        }






        private int TestSequence(string sequence, double expectedResult, double allowableError = .000001)
        {
            int NError = 0;

            DoButton(sequence);

            bool Ok = false;
            if (Double.IsNaN(expectedResult))
            {
                Ok = Double.IsNaN(ResultDouble);
            }
            else if (Double.IsPositiveInfinity (expectedResult))
            {
                Ok = Double.IsPositiveInfinity(ResultDouble);
            }
            else 
            {
                Double delta = Math.Abs(expectedResult - ResultDouble);
                Double deltaPercent = Math.Abs(ResultDouble) < 0.0000001 ? delta : delta / Math.Abs(ResultDouble);
                Ok = ResultDouble == expectedResult;
                if (!Ok && deltaPercent <= allowableError)
                {
                    Ok = true;
                }
            }
            if (!Ok)
            {
                NError++;
                Log.WriteWithTime("Test: SimpleCalculator: sequence {0} expected {1} actual {2}\r\n", sequence, expectedResult, ResultDouble);

            }
            return NError;
        }

        public int Test()
        {
            int NError = 0;
            string cmd;

            NError += TestSequence("aC|p-|n12|p/|n4|f=", -3); // - 12 / 4 --> -3
            NError += TestSequence("aC|p-|n12|p/|p-|n4|f=", 3); // - 12 / - 4 --> 3

            NError += TestSequence("aC|n2|p+|n3|p*|n4|f=", 20);

            NError += TestSequence("aC", 0);
            NError += TestSequence("aC|n1", 1);
            NError += TestSequence("aC|n1|aC", 0);

            NError += TestSequence("aC|n5|p-|n1|f=", 4);
            NError += TestSequence("aC|n5|p-|n1|f=|f=", 3);
            NError += TestSequence("aC|n5|p-|n1|f=|f=|p/|n2|f=", 1.5);

            // Chaining calculations together
            NError += TestSequence("aC|n.55|ox**2", 0.3025);
            NError += TestSequence("aC|a(|n50|px**y|n3|a)|f=", 125000);
            NError += TestSequence("aC|n.55|ox**2|p*|a(|n50|px**y|n3|a)|f=", 37812.5);
            NError += TestSequence("aC|n.55|ox**2|p*|a(|n50|px**y|n3|a)|p/|a(|n24|p*|n14.2|ox**2|a)|f=", 7.8135331944719981); 
            NError += TestSequence("aC|n.55|ox**2|p*|a(|n50|px**y|n3|a)|p/|a(|n24|p*|n14.2|ox**2|a)|f=", 7.8135331944719981);
            NError += TestSequence("aC|n7|p-|n2|p-|n1|f=", 4);
            NError += TestSequence("aC|n25|p/|p-|n5|f=", -5);
            //Test the percent key
            NError += TestSequence("aC|n72|p-|n20|o%|p+|n5|o%|f=", 60.48);
            NError += TestSequence("aC|n20|o%", 0.20);

            // After an =, entering a number starts a new calculation
            NError += TestSequence("aC|n5|p-|n1|f=|n7|p+|n8|f=", 15);

            //NError += TestSequence("n1p+n2f=f=", "5");
            //NError += TestSequence("n1p+n2f=f=p/n2f=", 2.5);

            // Quick test for each of the operators
            NError += TestSequence("aC|n2|p+|n4|f=", 6);
            NError += TestSequence("aC|n2|p-|n4|f=", -2);
            NError += TestSequence("aC|n2|p*|n4|f=", 8);
            NError += TestSequence("aC|n2|p/|n4|f=", 0.5);

            //
            // Chaining e.g. 2 + 3 * 4
            //
            NError += TestSequence("aC|n2|p+|n3|p*|n4|f=", 20);

            // x**y
            cmd = "x**y";
            NError += TestSequence("aC|n10|p" + cmd + "|n3|f=", 1000.0);
            NError += TestSequence("aC|n-10|p" + cmd + "|n3|f=", -1000.0);
            NError += TestSequence("aC|n10|p" + cmd + "|n-3|f=", 0.001);
            NError += TestSequence("aC|n-10|p" + cmd + "|n-3|f=", -0.001);

            NError += TestSequence("aC|n10|p" + cmd + "|n3|f=", 1000.0);
            NError += TestSequence("aC|n0|p" + cmd + "|n3|f=", 0);
            NError += TestSequence("aC|n10|p" + cmd + "|n0|f=", 1.0);
            NError += TestSequence("aC|n0|p" + cmd + "|n0|f=", 1.0);

            NError += TestSequence("aC|n10|p" + cmd + "|n3|f=", 1000);
            NError += TestSequence("aC|cNaN|p" + cmd + "|n3|f=", Double.NaN);
            NError += TestSequence("aC|n10|p" + cmd + "|cNaN|f=", Double.NaN);
            NError += TestSequence("aC|cNaN|p" + cmd + "|cNaN|f=", Double.NaN);

            NError += TestSequence("aC|n10|p" + cmd + "|n3|f=", 1000);
            NError += TestSequence("aC|c∞|p" + cmd + "|n3|f=", Double.PositiveInfinity);
            NError += TestSequence("aC|n10|p" + cmd + "|c∞|f=", Double.PositiveInfinity);
            NError += TestSequence("aC|c∞|p" + cmd + "|c∞|f=", Double.PositiveInfinity);

            NError += TestSequence("aC|n10|p" + cmd + "|n3|f=", 1000);
            NError += TestSequence("aC|c-∞|p" + cmd + "|n3|f=", Double.NegativeInfinity);
            NError += TestSequence("aC|n10|p" + cmd + "|c-∞|f=", 0);
            NError += TestSequence("aC|c-∞|p" + cmd + "|c-∞|f=", 0.0);

            // "y√x"
            cmd = "y√x";
            NError += TestSequence("aC|n1000|p" + cmd + "|n3|f=", 10.0);
            NError += TestSequence("aC|n-1000|p" + cmd + "|n3|f=", Double.NaN);
            NError += TestSequence("aC|n0.0010|p" + cmd + "|n-3|f=", 10.0);
            NError += TestSequence("aC|n-0.001|p" + cmd + "|n-3|f=", Double.NaN);

            NError += TestSequence("aC|n1000|p" + cmd + "|n3|f=", 10.0);
            NError += TestSequence("aC|n0|p" + cmd + "|n3|f=", 0);
            NError += TestSequence("aC|n1.0|p" + cmd + "|n0|f=", 1.0);
            NError += TestSequence("aC|n1|p" + cmd + "|n0|f=", 1.0);

            NError += TestSequence("aC|n1000|p" + cmd + "|n3|f=", 10);
            NError += TestSequence("aC|cNaN|p" + cmd + "|n3|f=", Double.NaN);
            NError += TestSequence("aC|n10|p" + cmd + "|cNaN|f=", Double.NaN);
            NError += TestSequence("aC|cNaN|p" + cmd + "|cNaN|f=", Double.NaN);

            NError += TestSequence("aC|n1000|p" + cmd + "|n3|f=", 10);
            NError += TestSequence("aC|c∞|p" + cmd + "|n3|f=", Double.PositiveInfinity);
            NError += TestSequence("aC|n10|p" + cmd + "|c∞|f=", 1.0);
            NError += TestSequence("aC|c∞|p" + cmd + "|c∞|f=", 1.0);

            NError += TestSequence("aC|n1000|p" + cmd + "|n3|f=", 10);
            NError += TestSequence("aC|c-∞|p" + cmd + "|n3|f=", Double.PositiveInfinity);
            NError += TestSequence("aC|n10|p" + cmd + "|c-∞|f=", 1.0);
            NError += TestSequence("aC|c-∞|p" + cmd + "|c-∞|f=", 1.0);



            //
            // Trig tests
            //
            cmd = "sin";
            NError += TestSequence("aC|n45|o"+cmd, 0.70710678118);
            NError += TestSequence("aC|n-45|o"+cmd, -0.70710678118);
            NError += TestSequence("aC|n0|o"+cmd, 0);
            NError += TestSequence("aC|n90|o"+cmd, 1);
            NError += TestSequence("aC|cNaN|o"+cmd, Double.NaN);
            NError += TestSequence("aC|c∞|o"+cmd, Double.NaN);
            NError += TestSequence("aC|c-∞|o"+cmd, Double.NaN);

            cmd = "cos";
            NError += TestSequence("aC|n45|o"+cmd, 0.70710678118);
            NError += TestSequence("aC|n-45|o"+cmd, 0.70710678118);
            NError += TestSequence("aC|n0|o"+cmd, 1);
            NError += TestSequence("aC|n90|o"+cmd, 0);
            NError += TestSequence("aC|cNaN|o"+cmd, Double.NaN);
            NError += TestSequence("aC|c∞|o"+cmd, Double.NaN);
            NError += TestSequence("aC|c-∞|o"+cmd, Double.NaN);

            cmd = "tan";
            NError += TestSequence("aC|n45|o"+cmd, 1);
            NError += TestSequence("aC|n-45|o"+cmd, -1);
            NError += TestSequence("aC|n0|o"+cmd, 0);
            NError += TestSequence("aC|n90|o" + cmd, 16331778728383844.0, 0.0001); // ?Double.PositiveInfinity);
            //NError += TestSequence("aC|n90|o" + cmd, 16331239351950000.0); // ?Double.PositiveInfinity);
            NError += TestSequence("aC|cNaN|o" + cmd, Double.NaN);
            NError += TestSequence("aC|c∞|o"+cmd, Double.NaN);
            NError += TestSequence("aC|c-∞|o"+cmd, Double.NaN);

            //
            // Various functions
            //

            // √
            cmd = "√";
            NError += TestSequence("aC|n10|o"+cmd, Math.Sqrt(10));
            NError += TestSequence("aC|n0|o"+cmd, 0);
            NError += TestSequence("aC|n-2|o"+cmd, Double.NaN);
            NError += TestSequence("aC|cNaN|o"+cmd, Double.NaN);
            NError += TestSequence("aC|c∞|o"+cmd, Double.PositiveInfinity);
            NError += TestSequence("aC|c-∞|o"+cmd, Double.NaN);

            // 3√
            cmd = "3√";
            NError += TestSequence("aC|n27|o"+cmd, 3.0);
            NError += TestSequence("aC|n0|o"+cmd, 0);
            NError += TestSequence("aC|n-2|o"+cmd, Double.NaN);
            NError += TestSequence("aC|cNaN|o"+cmd, Double.NaN);
            NError += TestSequence("aC|c∞|o"+cmd, Double.PositiveInfinity);
            NError += TestSequence("aC|c-∞|o"+cmd, Double.NaN);


            // d→r
            cmd = "d→r";
            NError += TestSequence("aC|n135|o"+cmd, Math.PI*.75);
            NError += TestSequence("aC|n0|o"+cmd, 0);
            NError += TestSequence("aC|n-90|o"+cmd, -Math.PI/2.0);
            NError += TestSequence("aC|cNaN|o"+cmd, Double.NaN);
            NError += TestSequence("aC|c∞|o"+cmd, Double.PositiveInfinity);
            NError += TestSequence("aC|c-∞|o"+cmd, Double.NegativeInfinity);



            // mod
            NError += TestSequence("aC|n10|pmod|n4|f=", 2); // modulo
            NError += TestSequence("aC|n-10|pmod|n4|f=", 2); // modulo

            // abs
            cmd = "abs";
            NError += TestSequence("aC|n10|o"+cmd, 10); 
            NError += TestSequence("aC|n-10|o"+cmd, 10); 
            NError += TestSequence("aC|c∞|o"+cmd, Double.PositiveInfinity); 
            NError += TestSequence("aC|c-∞|o"+cmd, Double.PositiveInfinity); 
            NError += TestSequence("aC|cNaN|o"+cmd, Double.NaN);


            cmd = "floor";
            NError += TestSequence("aC|n10.8|o" + cmd, 10);
            NError += TestSequence("aC|n-10.8|o" + cmd, -11);
            NError += TestSequence("aC|c∞|o" + cmd, Double.PositiveInfinity);
            NError += TestSequence("aC|c-∞|o" + cmd, Double.NegativeInfinity);
            NError += TestSequence("aC|cNaN|o" + cmd, Double.NaN);

            cmd = "integer"; // almost like floor
            NError += TestSequence("aC|n10.8|o" + cmd, 10);
            NError += TestSequence("aC|n-10.8|o" + cmd, -10); // floor is -11
            NError += TestSequence("aC|c∞|o" + cmd, Double.PositiveInfinity);
            NError += TestSequence("aC|c-∞|o" + cmd, Double.NegativeInfinity);
            NError += TestSequence("aC|cNaN|o" + cmd, Double.NaN);

            cmd = "remainder";
            NError += TestSequence("aC|n10.8|o" + cmd, 0.8);
            NError += TestSequence("aC|n-10.8|o" + cmd, 0.2); // floor is -11, floor+remainder==original
            NError += TestSequence("aC|c∞|o" + cmd, Double.NaN);
            NError += TestSequence("aC|c-∞|o" + cmd, Double.NaN);
            NError += TestSequence("aC|cNaN|o" + cmd, Double.NaN);

            cmd = "frac";
            NError += TestSequence("aC|n10.8|o" + cmd, 0.8);
            NError += TestSequence("aC|n-10.8|o" + cmd, 0.2); // floor is -11, floor+remainder==original
            NError += TestSequence("aC|c∞|o" + cmd, Double.NaN);
            NError += TestSequence("aC|c-∞|o" + cmd, Double.NaN);
            NError += TestSequence("aC|cNaN|o" + cmd, Double.NaN);


            cmd = "ceil";
            NError += TestSequence("aC|n10.8|o" + cmd, 11);
            NError += TestSequence("aC|n-10.8|o" + cmd, -10);
            NError += TestSequence("aC|c∞|o" + cmd, Double.PositiveInfinity);
            NError += TestSequence("aC|c-∞|o" + cmd, Double.NegativeInfinity);
            NError += TestSequence("aC|cNaN|o" + cmd, Double.NaN);

            cmd = "round";
            NError += TestSequence("aC|n10.8|o" + cmd, 11);
            NError += TestSequence("aC|n10.4|o" + cmd, 10);
            NError += TestSequence("aC|n10.5|o" + cmd, 10); // Round to even
            NError += TestSequence("aC|n11.5|o" + cmd, 12);
            NError += TestSequence("aC|n-10.8|o" + cmd, -11);
            NError += TestSequence("aC|c∞|o" + cmd, Double.PositiveInfinity);
            NError += TestSequence("aC|c-∞|o" + cmd, Double.NegativeInfinity);
            NError += TestSequence("aC|cNaN|o" + cmd, Double.NaN);




            cmd = "log10";
            NError += TestSequence("aC|n1|o" + cmd, 0);
            NError += TestSequence("aC|n10|o" + cmd, 1);
            NError += TestSequence("aC|n100|o" + cmd, 2);
            NError += TestSequence("aC|n2|p+|n0|o" + cmd, Double.NegativeInfinity);
            NError += TestSequence("aC|n2|p+|n-10|o" + cmd, Double.NaN);
            NError += TestSequence("aC|n2|p+|c∞|o" + cmd, Double.PositiveInfinity);
            NError += TestSequence("aC|n2|p+|c-∞|o" + cmd, Double.NaN);
            NError += TestSequence("aC|n2|p+|cNaN|o" + cmd, Double.NaN);

            cmd = "log2";
            NError += TestSequence("aC|n1|o" + cmd, 0);
            NError += TestSequence("aC|n2|o" + cmd, 1);
            NError += TestSequence("aC|n1024|o" + cmd, 10);
            NError += TestSequence("aC|n2|p+|n0|o" + cmd, Double.NegativeInfinity);
            NError += TestSequence("aC|n2|p+|n-10|o" + cmd, Double.NaN);
            NError += TestSequence("aC|n2|p+|c∞|o" + cmd, Double.PositiveInfinity);
            NError += TestSequence("aC|n2|p+|c-∞|o" + cmd, Double.NaN);
            NError += TestSequence("aC|n2|p+|cNaN|o" + cmd, Double.NaN);


            cmd = "alog10";
            NError += TestSequence("aC|n1|o" + cmd, 10);
            NError += TestSequence("aC|n10|o" + cmd, 10000000000.0);
            NError += TestSequence("aC|n2|p+|n0|o" + cmd, 1);
            NError += TestSequence("aC|n2|p+|n-2|o" + cmd, 0.01);
            NError += TestSequence("aC|n2|p+|c∞|o" + cmd, Double.PositiveInfinity);
            NError += TestSequence("aC|n2|p+|c-∞|o" + cmd, 0);
            NError += TestSequence("aC|n2|p+|cNaN|o" + cmd, Double.NaN);


            cmd = "alog2";
            NError += TestSequence("aC|n1|o" + cmd, 2);
            NError += TestSequence("aC|n10|o" + cmd, 1024);
            NError += TestSequence("aC|n2|p+|n0|o" + cmd, 1);
            NError += TestSequence("aC|n2|p+|n-2|o" + cmd, 0.25);
            NError += TestSequence("aC|n2|p+|c∞|o" + cmd, Double.PositiveInfinity);
            NError += TestSequence("aC|n2|p+|c-∞|o" + cmd, 0);
            NError += TestSequence("aC|n2|p+|cNaN|o" + cmd, Double.NaN);


            // Factorial
            cmd = "x!";
            NError += TestSequence("aC|n1|o"+cmd, 1);
            NError += TestSequence("aC|n2|o"+cmd, 2);
            NError += TestSequence("aC|n4|o"+cmd, 24);
            NError += TestSequence("aC|n8|o"+cmd, 40320);
            NError += TestSequence("aC|n10|o"+cmd, 3628800);
            NError += TestSequence("aC|n18|o"+cmd, 6.402373705728E+15);
            NError += TestSequence("aC|n20|o"+cmd, 2.43290200817664E+18);
            NError += TestSequence("aC|n30|o"+cmd, 2.6525285981219103E+32);
            NError += TestSequence("aC|n40|o"+cmd, 8.1591528324789768E+47);
            NError += TestSequence("aC|n50|o"+cmd, 3.0414093201713376E+64);
            NError += TestSequence("aC|n60|o"+cmd, 8.3209871127413916E+81);
            NError += TestSequence("aC|n2000|o"+cmd, Double.PositiveInfinity);

            NError += TestSequence("aC|n0|o"+cmd, 0);
            NError += TestSequence("aC|n4.45|o"+cmd, 24);
            NError += TestSequence("aC|n-4|o"+cmd, Double.NaN);

            NError += TestSequence("aC|n1|n2|n3", 123);

            // RandN
            cmd = "RandN";
            NError += TestSequence("aC|n1|o"+cmd, 1);
            NError += TestSequence("aC|n2|p+|n0|o"+cmd, Double.NaN);
            NError += TestSequence("aC|n2|p+|n-10|o"+cmd, Double.NaN);
            NError += TestSequence("aC|n2|p+|c∞|o"+cmd, Double.PositiveInfinity);
            NError += TestSequence("aC|n2|p+|c-∞|o"+cmd, Double.NaN);
            NError += TestSequence("aC|n2|p+|cNaN|o"+cmd, Double.NaN);


            // Parenthesis operations
            NError += TestSequence("aC|a(|n1|p+|n2|a)", 3);  // (1+2) --> 3
            NError += TestSequence("aC|n4|p*|a(|n1|p+|n2|a)", 3);  // 4*(1+2) --> 3 because there is no equal sign
            NError += TestSequence("aC|n4|p*|a(|n1|p+|n2|a)|f=", 12);  // 4*(1+2)= --> 12 

            NError += TestSequence("aC|a(|n1|p+|n2|aC|a)", 0);  // (1+2 CA ) --> 0 from the CA

            // Test with some operators like tan
            NError += TestSequence("aC|a(|n5|p*|n9|a)|otan", 1);

            // Multiple Parenthesis.
            NError += TestSequence("aC|n12|p*|a(|a(|n5|p*|n9|a)|otan|a)|f=", 12); // 12*((5*9) tan) --> 12
            NError += TestSequence("aC|n4|p*|a(|a(|n2|p*|n3|a)|a)|f=", 24); // 4*((2*3)) --> 24

            // Using the - key for negative numbers
            NError += TestSequence("aC|n12|p/|p-|n4|f=", -3); // 12 / - 4 --> -3
            NError += TestSequence("aC|n12|p/|p-|p-|n4|f=", 3); // 12 / - - 4 --> 3 [two negatives in a row]
            NError += TestSequence("aC|n12|p/|p-|p-|p-|n4|f=", -3); // 12 / - - - 4 --> -3 [three negatives in a row]
            NError += TestSequence("aC|n12|p/|p-|a(|n4|a)|f=", -3); // 12 / - ( 4 ) --> -3
            NError += TestSequence("aC|n12|p/|p-|a(|n6|p-|n2|a)|f=", -3); // 12 / - ( 6 - 2 ) --> -3
            NError += TestSequence("aC|n12|p/|p-|a(|n1|p-|p-|n3|a)|f=", -3); // 12 / - ( 1 - - 3 ) --> -3
            NError += TestSequence("aC|n12|p/|a(|n1|p-|p-|n3|a)|f=", 3); // 12 / ( 1 - - 3 ) --> 3
            NError += TestSequence("aC|p-|n12|p/|n4|f=", -3); // - 12 / 4 --> -3
            NError += TestSequence("aC|p-|n12|p/|p-|n4|f=", 3); // - 12 / - 4 --> 3


            DoClearAll();
            if (NError == 0) ErrorMessage = "System test: pass";
            else ErrorMessage = string.Format("System test: FAIL: {0} errors", NError);
            return NError;
        }

        // value is either degrees or radians depending on VisibleTrigUnits.
        private double AsRadians(double value)
        {
            double Retval = value;
            switch (VisibleTrigUnits)
            {
                case TrigUnits.Degrees: Retval = value * Math.PI / 180.0; break;
            }
            return Retval;
        }

        private double AsVisibleAngle(double value)
        {
            double Retval = value;
            switch (VisibleTrigUnits)
            {
                case TrigUnits.Degrees: Retval = value * 180.0 / Math.PI; break;
            }
            return Retval;
        }

        public void DoClearAll()
        {
            PartialResults = 0.0;
            PartialAction = "";
            SavedPartialAction = "";
            ResultString = "0";
            ResultDouble = 0.0;
            LastAction = Actions.None;
            ParenDataList.Clear();
        }

        public void DoClearX()
        {
            _ResultDouble = 0.0; // Don't update the string.
            ResultString = "";
        }

        public void DoButton(string sequence)
        {
            string[] commands = sequence.Split('|');
            foreach (string command in commands)
            {
                char cmd = command[0];
                string value = command.Substring(1);

                // Handle the user pressing "12 / - 4" -- the - in - 4 is the second partial in a row.
                switch (cmd)
                {
                    case 'p': NPartialInARow++; break;
                    case '#': break; // comments don't change anything.
                    default: NPartialInARow = 0; break;
                }

                switch (cmd)
                {
                    case 'a': DoAction(value); break;
                    case 'b': if (CalculatorConnection != null) CalculatorConnection.DoRunButtonProgramAsync(value); break;
                    case 'n': DoNumericalButton(value); break;
                    case 'p': DoPartialButton(value); break; // + - MOD etc
                    case 'f': DoFinalButton(value); break; // only final is = sign
                    case 'c': DoConstant(value); break;
                    case 'o': DoMonoOp(value); break;
                    case '#': break; // comment
                    default:
                        Log.WriteWithTime("ERROR: Button: unknown command " + value);
                        break;

                }

            }
        }

        public void DoAction(string value)
        {
            ErrorMessage = "";
            switch (value)
            {
                case "⌫":
                    if (ResultString.Length >= 1)
                    {
                        ResultString = ResultString.Substring(0, ResultString.Length - 1);
                        double val = 0;
                        bool ConvertOk = Double.TryParse(ResultString, out val);
                        if (ConvertOk) _ResultDouble = val; // Don't update the string!
                    }
                    break;
                case "C": DoClearAll(); break;
                case "ClearAll": DoClearAll(); break;
                case "ClearX": DoClearX(); break;
                case "radians": VisibleTrigUnits = TrigUnits.Radians; break;
                case "degrees": VisibleTrigUnits = TrigUnits.Degrees; break;
                case "→M": Memory[0] = string.Format("{0:R}", ResultDouble); break; // ResultDouble; break; // ResultString; break;
                case "M-":
                case "M+":
                    {
                        double v1 = ResultDouble;
                        //double.TryParse(ResultString, out v1);

                        double v2; // = Memory[0];
                        double.TryParse(Memory[0], out v2);

                        double val = (value=="M+") ? v2 + v1 : v2 - v1;
                        string newvalue = String.Format("{0:R}", val);
                        Memory[0] = newvalue; //  val; // newvalue;

                    }
                    break;
                case "(": PushParenData(); break;
                case ")": PopParenData(); break;
                case "☟": PreferredDisplayRotation = 180; break;
                case "☝": PreferredDisplayRotation = 0; break;
                default:
                    Log.WriteWithTime("ERROR: Action: unknown command " + value);
                    break;
            }
        }

        public int MemoryIndex (string tag) // memory0, etc --> 0
        {
            int Retval = -1;
            if (tag.StartsWith ("memory"))
            {
                string str = tag.Replace ("memory", "");
                int value = 0;
                bool Ok = Int32.TryParse (str, out value);
                if (Ok)
                {
                    Retval = value;
                }
            }
            return Retval;
        }

        public string MemoryIncrement(int memoryIndex, double increment)
        {
            string Retval = "";
            double parseValue;
            string startValue = Memory[memoryIndex];
            if (startValue == "") startValue = "0";
            bool Ok = double.TryParse(startValue, out parseValue);
            if (Ok)
            {
                parseValue += increment;
                Retval = String.Format("{0:R}", parseValue);
                Memory[memoryIndex] = Retval;
            }
            return Retval;
        }

        public string MemorySet(int memoryIndex, double value)
        {
            string Retval = String.Format("{0:R}", value);
            Memory[memoryIndex] = Retval;
            return Retval;
        }

        public string MemorySet(int memoryIndex, string value)
        {
            Memory[memoryIndex] = value;
            return value;
        }

        List<string> keyPressData = new List<string>();
        private void DebugKeyStream(string format, params string[] args)
        {
            var str = string.Format(format, args);
            keyPressData.Add(str);
            if (keyPressData.Count > 20)
            {
                keyPressData.RemoveAt(0);
            }
        }
        public string GetDebugKeyString ()
        {
            var str = string.Join(" ", keyPressData);
            return str;
        }
        public void ClearDebugKeyString()
        {
            keyPressData.Clear();
        }

        public void DoNumericalButton(string value)
        {
            DebugKeyStream("{0}", value);
            ErrorMessage = ""; // Clear the old error message.
            if (value == "EE") value = "E"; // special case: E can be EITHER like "1.2E8" for exp or 12EB for hex.
            // The calc will send EE for the exp case, and it gets converted to EE here.
            var newResultString = ResultString + value;
            if (LastAction == Actions.Partial || LastAction == Actions.MonoOp || LastAction == Actions.Final || LastAction == Actions.None)
            {
                _ResultDouble = 0; // Don't update the string
                newResultString = value;
                if (MustNegate && newResultString[0] != '-')
                {
                    newResultString = "-" + newResultString;
                }
            }
            MustNegate = false;
            double val = 0;
            // DisplaySpecifier base2 base8 base10 base16
            bool Ok = true;
            try
            {
                switch (DisplaySpecifier)
                {
                    case "base2": val = Convert.ToUInt32(newResultString, 2); break;
                    case "base8": val = Convert.ToUInt32(newResultString, 8); break;
                    case "base10": val = Convert.ToUInt32(newResultString, 10); break;
                    case "base16": val = Convert.ToUInt32(newResultString, 16); break;
                    default:
                        bool EndsWithE = newResultString.EndsWith("E") || newResultString.EndsWith("E-") || newResultString.EndsWith(".");

                        NumberStyles styles = NumberStyles.Float | NumberStyles.AllowThousands;
                        var format = CultureInfo.InvariantCulture;

                        Ok = Double.TryParse(newResultString, styles, format, out val);
                        if (!Ok && EndsWithE)
                        {
                            // Must be a number like 1.2E  (or might be 1.2E3E)
                            // Or might be . (as in, about to be .1)
                            // Try adding a '0' at the end and retry
                            // 1.2E -->1.2E0 which is OK and 1.2E3E --> 1.2E3E0 which is not Ok
                            // Similarly, .-->.0 which is OK and 1.2.-->1.2.0 which is not Ok (but which isn't possible anyway)
                            Ok = Double.TryParse(newResultString+"0", styles, format, out val);
                        }
                        break;
                }
            }
            catch (Exception)
            {
                Ok = false;
            }


            if (Ok)
            {
                ResultString = newResultString;
                if (ShouldConvertSize())
                {
                    _ResultDouble = ConvertSize(val);
                }
                else
                {
                    _ResultDouble = val; // Don't update string.
                }
                LastAction = Actions.Numerical;
            }
            else
            {
                // Don't change anything -- act as if this input never happened.
                // (Except that we'll tell the user)
                ErrorMessage = "Invalid input";
            }
        }

        public static uint RotateLeft(uint value, int count)
        {
            return (value << count) | (value >> (32 - count));
        }

        public static uint RotateRight(uint value, int count)
        {
            return (value >> count) | (value << (32 - count));
        }

        public static ushort RotateLeft(ushort value, int count)
        {
            return (ushort)((value << count) | (value >> (16 - count)));
        }

        public static ushort RotateRight(ushort value, int count)
        {
            return (ushort)((value >> count) | (value << (16 - count)));
        }

        public static byte RotateLeft(byte value, int count)
        {
            return (byte)((value << count) | (value >> (8 - count)));
        }

        public static byte RotateRight(byte value, int count)
        {
            return (byte)((value >> count) | (value << (8 - count)));
        }

        public static UInt32 RotateRight(UInt32 value, int count, int nbytes)
        {
            switch (nbytes)
            {
                case 1: return RotateRight((byte)value, count); 
                case 2: return RotateRight((ushort)value, count); 
                case 4: return RotateRight((uint)value, count); 
            }
            return 0;
        }

        public static UInt32 RotateLeft(UInt32 value, int count, int nbytes)
        {
            switch (nbytes)
            {
                case 1: return RotateLeft((byte)value, count);
                case 2: return RotateLeft((ushort)value, count);
                case 4: return RotateLeft((uint)value, count);
            }
            return 0;
        }

        public static UInt32 BitCount(UInt32 value, int nbits)
        {
            UInt32 Retval = 0;
            UInt32 test = 1;
            for (int i = 0; i < nbits; i++)
            {
                if ((value & test) != 0) Retval++;
                test <<= 1;
            }
            return Retval;
        }

        public double DoNumericalButtonEnd(bool potentialChain = false)
        {
            ErrorMessage = "";
            double v1 = ResultDouble;
            //double.TryParse(ResultString, out v1);

            double v2 = PartialResults;
            //double.TryParse(PartialResults, out v2);

            double result = 0;
            //string resultText;
            // When just entering values, must calculate PARTIAL - RESULT
            // When I chain calculations, must calculate RESULT - PARTIAL
            if (LastAction == Actions.Numerical) 
            {
                PartialResults = ResultDouble;
            }
            //else if (LastAction == Actions.Partial)
            //{
            //    //PartialResults = ResultDouble;
            //    PartialAction = PartialAction;
            //}
            else if (potentialChain && LastAction == Actions.Final)
            {
                double v = v1;
                v1 = v2;
                v2 = v;
            }

            switch (PartialAction)
            {
                case "": result = v1; break; // no action e.g. 1=
                case "+": result = v2 + v1; break;
                case "-": result = v2 - v1; break;
                //case "×": result = v2 * v1; break;
                case "*": result = v2 * v1; break;
                //case "÷": result = v2 / v1; break;
                case "/": result = v2 / v1; break;
                case "mod":
                    {
                        double div = v2 / v1;
                        double trunc = Math.Floor(div);
                        result = v2 - (trunc * v1);
                    }
                    break;
                case "x**y": result = Math.Pow(v2, v1); break;
                case "y√x": result = Math.Pow(v2, 1/v1); break;

                case "and": result = AsInteger(v2) & AsInteger(v1); break;
                case "or": result = AsInteger(v2) | AsInteger(v1); break;
                case "xor": result = AsInteger(v2) ^ AsInteger(v1); break;
                case "rotleft": result = RotateLeft(AsInteger(v2), (int)AsInteger(v1), DisplaySizeInt); break;
                case "rotright": result = RotateRight(AsInteger(v2), (int)AsInteger(v1), DisplaySizeInt); break;
                case "shiftleft": result = AsInteger(v2) << (int)AsInteger(v1); break;
                case "shiftright": result = AsInteger(v2) >> (int)AsInteger(v1); break;
                default:
                    Log.WriteWithTime("ERROR: Partial: unknown command " + PartialAction);
                    break;
            }
            SavedPartialAction = PartialAction;
            PartialAction = "";
            //resultText = string.Format("{0}", result);
            //return resultText;
            if (ShouldConvertSize())
            {
                result = ConvertSize(result);
            }

            return result;
        }

        public void DoPartialButton(string value)
        {
            DebugKeyStream("({0})", value);
            if (value == "-" && ResultString.EndsWith("E"))
            {
                // Special override: treat 1E- as if 1E-2, not 1E - 2
                DoNumericalButton(value);
                return;
            }
            ErrorMessage = "";
            if (value == "-" && NPartialInARow > 1)
            {
                MustNegate = !MustNegate; // Normally user might type "12 / - 4" .  This handles "12 / - - 4" which should be 12 / 4.  Also, 12 / - - - 4 ==> 12 / -4
                LastAction = Actions.Partial;
                return; // Don't update PartialResults or PartialAction
            }
            else if (value == "-" && LastAction == Actions.None) // This sets us up so that CA - 12 should display -1 as soon as the user pressed 1
            {
                MustNegate = true;
                LastAction = Actions.Partial;
                return; // Don't update PartialResults or PartialAction
            }
            else if (PartialAction != "") 
            {
                ResultDouble = DoNumericalButtonEnd(false);
            }
            LastAction = Actions.Partial;
            PartialResults = ResultDouble;
            PartialAction = value;
        }

        public UInt32 AsInteger(double rval) 
        {
            UInt32 Retval;
            if (rval < 0)
            {
                Int32 neg = (Int32)Math.Truncate(rval);
                byte[] bytes = BitConverter.GetBytes(neg);
                Retval = BitConverter.ToUInt32(bytes, 0);
            }
            else
            {
                Retval = (UInt32)Math.Truncate(rval);
            }
            return Retval;
        }

        private void swab(byte[] bytes, int index1, int index2)
        {
            byte t = bytes[index1];
            bytes[index1] = bytes[index2];
            bytes[index2] = t;
        }

        public UInt32 swab(UInt32 Value, int NBytes)
        {
            byte[] bytes = BitConverter.GetBytes(Value);
            switch (NBytes)
            {
                case 1: break;
                case 2: swab(bytes, 0, 1); break;
                case 4: swab(bytes, 0, 3); swab(bytes, 1, 2); break;
            }
            UInt32 Retval = BitConverter.ToUInt32(bytes, 0);
            return Retval;
        }

        public static UInt32 comp2(UInt32 value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Int32 signed = BitConverter.ToInt32(bytes, 0);
            signed = -signed;
            bytes = BitConverter.GetBytes(signed);
            UInt32 Retval = BitConverter.ToUInt32(bytes, 0);
            return Retval;
        }


        public static UInt16 comp2(UInt16 value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Int16 signed = BitConverter.ToInt16(bytes, 0);
            signed = (Int16)(-signed);
            bytes = BitConverter.GetBytes(signed);
            UInt16 Retval = BitConverter.ToUInt16(bytes, 0);
            return Retval;
        }
        public static Byte comp2(Byte value)
        {
            SByte signed = Convert.ToSByte (value);
            signed = (SByte)(-signed);
            Byte Retval = Convert.ToByte(signed);
            return Retval;
        }

        public static UInt32 comp2(UInt32 value, int NBytes)
        {
            switch (NBytes)
            {
                case 4: return comp2(value);
                case 2: return comp2((UInt16)value);
                case 1: return comp2((Byte)value);
            }
            return 0;
        }




        public void DoMonoOp(string value)
        {
            DebugKeyStream("[{0}]", value);
            ErrorMessage = "";
            double rval = ResultDouble;
            //double.TryParse(ResultString, out rval);
            double rvalr = AsRadians(rval);

            string CanBeNanList = "";
            bool CanBeNaN = CanBeNanList.IndexOf("|" + value + "|") >= 0;

            string MustNotBeInfiniteList = "|RandN|";
            bool MustNotBeInfinite = MustNotBeInfiniteList.IndexOf("|" + value + "|") >= 0;

            string MustBeGTE0List = "|√|3√|x!|";
            bool MustBeGTE0 = MustBeGTE0List.IndexOf("|" + value + "|") >= 0;

            string MustBeGTE1List = "|RandN|";
            bool MustBeGTE1 = MustBeGTE1List.IndexOf("|" + value + "|") >= 0;


            double result = 0;
            //string resultText;
            bool AllowLastActionChange = true;
            if (!CanBeNaN && Double.IsNaN(rval))
            {
                ErrorMessage = "Can't do that with a NaN";
                result = rval;
            }
            else if (MustBeGTE0 && !(rval >= 0))
            {
                ErrorMessage = "Can't do that with < 0";
                result = Double.NaN;
            }
            else if (MustBeGTE1 && !(rval >= 1))
            {
                ErrorMessage = "Can't do that with < 1";
                result = Double.NaN;
            }
            else if (MustNotBeInfinite && (double.IsNegativeInfinity(rval) || Double.IsPositiveInfinity(rval)))
            {
                ErrorMessage = "Can't do that with Infinity";
                result = rval;
            }
            else
            {
                switch (value)
                {
                    case "%":
                        // CLEAR 6% ==> 0.06
                        // CLEAR 72 - 20% ==> 14.4 (20% of 72)
                        if (PartialAction == "") result = rval / 100;
                        else result = PartialResults * (rval / 100.0);
                        break; 
                    case "√": result = Math.Sqrt(rval); break;
                    case "3√": result = Math.Pow(rval, 1.0 / 3.0); break;
                    case "±": result = -rval; AllowLastActionChange = false; break;
                    case "1/x": result = 1 / rval; break;
                    case "x**2": result = rval * rval; break;
                    case "x**3": result = rval * rval * rval; break;
                    case "x!":
                        {
                            result = 1;
                            for (int i = 1; i <= Math.Min(rval, 200); i++) // Can't actually calculate beyond here
                            {
                                result = result * i;
                            }
                            if (rval < 1) result = 0;
                        }
                        break;

                    case "not": result = ~AsInteger (rval); break;
                    case "comp2": result = comp2(AsInteger (rval)); break;
                    case "b#": result = BitCount(AsInteger(rval), DisplaySizeBitsInt); break;
                    case "swab": result = swab(AsInteger(rval), DisplaySizeInt);break;
                    case "sin": result = Math.Sin(rvalr); break;
                    case "cos": result = Math.Cos(rvalr); break;
                    case "tan": result = Math.Tan(rvalr); break;

                    case "asin":
                        if (rval < -1 || rval > 1)
                        {
                            ErrorMessage = "Value must be between -1 and 1";
                        }
                        result = AsVisibleAngle(Math.Asin(rval));
                        break;
                    case "acos":
                        if (rval < -1 || rval > 1)
                        {
                            ErrorMessage = "Value must be between -1 and 1";
                        }
                        result = AsVisibleAngle(Math.Acos(rval));
                        break;
                    case "atan":
                        result = AsVisibleAngle(Math.Atan(rval));
                        break;


                    case "log10":
                        if (rval <= 0)
                        {
                            ErrorMessage = "Value must be > 0";
                        }
                        result = Math.Log10(rval);
                        break;
                    case "logE":
                        if (rval <= 0)
                        {
                            ErrorMessage = "Value must be > 0";
                        }
                        result = Math.Log(rval);
                        break;
                    case "log2":
                        if (rval <= 0)
                        {
                            ErrorMessage = "Value must be > 0";
                        }
                        result = Math.Log10(rval) / Math.Log10(2);
                        break;
                    case "alog10": result = Math.Pow(10, rval); break;
                    case "alogE": result = Math.Exp(rval); break;
                    case "alog2": result = Math.Pow(2, rval); break;


                    case "abs": result = Math.Abs(rval); break;
                    case "floor": result = Math.Floor(rval); break;
                    case "round": result = Math.Round(rval); break;
                    case "ceil": result = Math.Ceiling(rval); break;
                    case "integer": result = Math.Truncate(rval); break;
                    case "remainder": result = rval - Math.Floor(rval); break;
                    case "frac": result = rval - Math.Floor(rval); break; // frac is 2016-06-18 rename of remainder; Graham, Knuth & Patashnik 1992.  frac(-1.3) is 0.7 (!)

                    case "RandN":
                        if (rval > Int32.MaxValue)
                        {
                            ErrorMessage = "Can't do that with a value > " + Int32.MaxValue.ToString();
                        }
                        else
                        {
                            result = 1 + R.Next((int)rval);
                        }
                        break; // if N is 2, return either 1 or 2

                    case "d→r": result = rval / Conversions.DEGREES_PER_RADIAN; break;
                    case "r→d": result = rval * Conversions.DEGREES_PER_RADIAN; break;
                    default:
                        Log.WriteWithTime("ERROR: MonoOp: unknown command " + value);
                        break;
                }
            }
            //resultText = string.Format("{0}", result);
            if (AllowLastActionChange) // +/- doesn't set this
            {
                LastAction = Actions.MonoOp;
            }
            // doesn't change the last action?

            //ResultString = resultText;

            if (ShouldConvertSize())
            {
                result = ConvertSize(result);
            }

            ResultDouble = result;
        }

        public void DoConstant(string value)
        {
            DebugKeyStream("{0}", value);
            ErrorMessage = "";
            double result = 0;
            //string resultText = null;
			// http://physics.nist.gov/cuu/Constants/
            switch (value)
            {
                case "c": result = 299792458; break; // speed of light, m/s http://en.wikipedia.org/wiki/Speed_of_light
                case "gₙ": result = 9.80665; break; // standard gravity (earth) http://en.wikipedia.org/wiki/Standard_gravity
                case "e": result = Math.E; break;
                case "Nₐ": result = 6.02214129E23; break; // http://en.wikipedia.org/wiki/Avogadro_constant
                case "R": result = 8.3144621; break; // gas constant PV=nRT http://en.wikipedia.org/wiki/Gas_constant
                case "π": result = Math.PI; break;
                case "NaN": result = Double.NaN; break;
                case "∞": result = double.PositiveInfinity; break;
                case "-∞": result = double.NegativeInfinity; break;
                case "RandDouble": result = R.NextDouble(); break;
                case "M→": double.TryParse(Memory[0], out result); break; //result = Memory[0]; break;
                default:
                    Log.WriteWithTime("ERROR: Constant: unknown constant " + value);
                    break;
            }
            //if (resultText == null)
            //{
            //    resultText = string.Format("{0}", result);
            //}
            //ResultString = resultText;
            LastAction = Actions.Numerical;
            ResultDouble = result;
        }


        public void DoFinalButton(string value)
        {
            DebugKeyStream("={0}", value);
            ErrorMessage = "";
            switch (value)
            {
                case "ClearAll":
                    DoClearAll();
                    break;
                case "=":
                    if (PartialAction == "")
                    {
                        PartialAction = SavedPartialAction;
                    }
                    SetResultDoubleRegardless = true;
                    ResultDouble = DoNumericalButtonEnd(true);
                    break;
                default:
                    Log.WriteWithTime("ERROR: Final: unknown command " + value);
                    break;
            }
            LastAction = Actions.Final;
        }


        class ParenData
        {
            public string PushedPartialAction;
            public double PushedPartialResults;
            public string PushedSavedPartialAction;
            public bool PushedMustNegate;
        }
        List<ParenData> ParenDataList = new List<ParenData>();

        private void PushParenData()
        {
            var save = new ParenData() { PushedPartialAction = PartialAction, PushedPartialResults = PartialResults, PushedSavedPartialAction=SavedPartialAction, PushedMustNegate=MustNegate };
            ParenDataList.Add(save);

            PartialResults = 0.0;
            PartialAction = "";
            SavedPartialAction = "";
            MustNegate = false;
            ResultString = "";
            _ResultDouble = 0.0; // Don't update the string.
            LastAction = Actions.None;
        }

        private void PopParenData()
        {
            if (ParenDataList.Count < 1) return; // nothing special happens if the user just clicks ))))).
            // (2+3) should result in 5
            if (PartialAction != "")
            {
                ResultDouble = DoNumericalButtonEnd(false);
            }

            var saved = ParenDataList.Last();
            ParenDataList.Remove(saved);

            PartialResults = saved.PushedPartialResults;
            PartialAction = saved.PushedPartialAction;
            SavedPartialAction = saved.PushedSavedPartialAction;
            MustNegate = saved.PushedMustNegate;
            if (MustNegate) // e.g., when 12 / - (4) must show -4 on screen after the right paren
            {
                ResultDouble = -ResultDouble;
                MustNegate = false;
            }
        }
    }
}
