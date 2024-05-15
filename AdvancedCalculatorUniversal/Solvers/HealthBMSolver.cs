using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EquationSolver;

namespace AdvancedCalculator
{
    public class HealthBMISolver : SolverINPC
    {
        //GRAMS_PER_OUNCE
        public static double KILOGRAMS_PER_OUNCE = (Conversions.GRAMS_PER_OUNCE / 1000.0);
        public static double KILOGRAMS_PER_POUND = KILOGRAMS_PER_OUNCE * Conversions.OUNCES_PER_POUND;
        public static double METERS_PER_INCH = Conversions.CENTIMETERS_PER_INCH / 100.0;

        // 180 pounds = 81.6466 kilograms; 1 pounds = 0.453592
        public HealthBMISolver()
        {
            // http://www.cdc.gov/healthyweight/assessing/bmi/
            Equations.Add(new Equation("BMI", new List<String>() { "WeightKilograms", "HeightMeters" }, () => { return WeightKilograms / (_HeightMeters * _HeightMeters); }));
            Equations.Add(new Equation("WeightKilograms", new List<String>() { "BMI", "HeightMeters" }, () => { return BMI * (_HeightMeters * _HeightMeters); }));
            Equations.Add(new Equation("HeightMeters", new List<String>() { "BMI", "WeightKilograms" }, () => { return Math.Sqrt(WeightKilograms / BMI); }));


            Equations.Add(new Equation("WeightPounds", "WeightKilograms", () => { return WeightKilograms / KILOGRAMS_PER_POUND; }));
            Equations.Add(new Equation("WeightKilograms", "WeightPounds", () => { return WeightPounds * KILOGRAMS_PER_POUND; }));

            Equations.Add(new Equation("HeightJustInches", "HeightMeters", () => { return HeightMeters / METERS_PER_INCH; }));
            Equations.Add(new Equation("HeightMeters", "HeightJustInches", () => { return HeightJustInches * METERS_PER_INCH; }));

            Equations.Add(new Equation("HeightInches", "HeightJustInches", () => { return HeightJustInches % Conversions.INCHES_PER_FOOT; }));
            Equations.Add(new Equation("HeightFeet", "HeightJustInches", () => { return Math.Floor(HeightJustInches / Conversions.INCHES_PER_FOOT); }));
            Equations.Add(new Equation("HeightJustInches", new List<String>() { "HeightInches", "HeightFeet" }, () => { return HeightInches + HeightFeet * Conversions.INCHES_PER_FOOT; }));

            //Equations.Add(new Equation("AgeYears", "AgeMonths", () => { return AgeMonths / 12.0; }));
            //Equations.Add(new Equation("AgeMonths", "AgeYears", () => { return AgeYears * 12.0; }));
            Equations.Add(new Equation("AgeMonths", new List<String>() { "AgeMonthPart", "AgeYearPart" }, () => { return (AgeYearPart*12) + AgeMonthPart; }));


            Equations.Add(new Equation("BMIChildUpperPercent", new List<String>() { "BMI", "AgeMonths", "Gender" }, 
                () => {
                    double lowBand = 0;
                    double highBand = 100;
                    InitBmiChildTable();
                    //double ageMonths = Age * 12;
                    int startRow = 0; // Table starts with boy (gender=1) and then has girls (gender=2)
                    if (Gender == 2)
                    {
                        startRow = childTable.FindRowMatch("Sex", Gender, 0, 0);
                    }

                    int rowLow = childTable.FindRowLTE("Agemos", AgeMonths, -1, startRow);
                    int rowHigh = childTable.FindRowGTE("Agemos", AgeMonths, -1, startRow);
                    if (rowLow < 0 || rowHigh < 0 || Double.IsNaN (BMI))
                    {
                        SetBMIChildInterpretation(0, 100, -1);
                        SetBMIChildBand(lowBand, highBand, -1);
                        return -1;
                    }

                    double ageLow = childTable.GetDouble("Agemos", rowLow, 0);
                    double ageHigh = childTable.GetDouble("Agemos", rowHigh, 0);
                    double interpolateRatio = CsvTable.CalculateInterpolateRatio (ageLow, AgeMonths, ageHigh);

                    // columns: Sex	Agemos	L	M	S	P3	P5	P10	P25	P50	P75	P85	P90	P95	P97
                    int[] potentials = new int[] { 3, 5, 10, 25, 50, 75, 85, 90, 95, 97 };
                    double lowValue = 0;
                    foreach (int percent in potentials)
                    {
                        var colName = string.Format ("P{0}", percent); // e.g., P75
                        var value = childTable.GetDoubleInterpolate(colName, rowLow, interpolateRatio, rowHigh, 0.0);
                        if (BMI < value)
                        {
                            highBand = percent;
                            double valueRatio = (lowValue == 0) ? -1.0 : CsvTable.CalculateInterpolateRatio(lowValue, BMI, value);
                            SetBMIChildInterpretation(lowBand, highBand, valueRatio);
                            SetBMIChildBand(lowBand, highBand, valueRatio);
                            return percent;
                        }
                        else
                        {
                            lowValue = value;
                            lowBand = percent;
                        }
                    }
                    SetBMIChildBand(lowBand, highBand, -1);
                    SetBMIChildInterpretation(lowBand, highBand, -1); // don't have a good interpolation
                    return 100;
                })); 

            InitEquivLists();
        }

        private void SetBMIChildBand(double low, double high, double positionRatio)
        {
            bool invalid = low == 0 && high == 100;
            string value = "Invalid";
            if (!invalid)
            {
                value = String.Format("{0}%-{1}%", low, high);
                if (positionRatio >= 0 && positionRatio <= 1)
                {
                    if (positionRatio < .2) value += " (low part of band)";
                    else if (positionRatio > .8) value += " (high part of band)";
                    else value += " (middle of band)";
                }
            }
            BMIChildBand = value;
        }


        private void SetBMIChildInterpretation(double low, double high, double positionRatio) // position is 0..1; 0 means at low, 1 means at high, .5 is in the middle
        {
            /* Interpretation from http://www.cdc.gov/healthyweight/assessing/bmi/childrens_bmi/about_childrens_bmi.html
             * Data retrieved 2/13/2013

                    Weight Status Category	Percentile Range
                    Underweight	Less than the 5th percentile
                    Healthy weight	5th percentile to less than the 85th percentile
                    Overweight	85th to less than the 95th percentile
                    Obese	Equal to or greater than the 95th percentile
             */
            bool invalid = low == 0 && high == 100;
            var str = "Unknown";
            if (invalid) str = "Invalid";
            else if (high <= 5) str = "Underweight";
            else if (high <= 85) str = "Normal";
            else if (high <= 95) str = "Overweight";
            else if (high <= 100) str = "Obese";

            BMIChildInterpretation = str;
        }


        private double _WeightKilograms = Double.NaN;
        public double WeightKilograms { get { return _WeightKilograms; } set { if (value == _WeightKilograms) return; _WeightKilograms = value; OnPropertyChanged("WeightKilograms"); } }

        private double _WeightPounds = Double.NaN;
        public double WeightPounds { get { return _WeightPounds; } set { if (value == _WeightPounds) return; _WeightPounds = value; OnPropertyChanged("WeightPounds"); } }

        private double _HeightMeters = Double.NaN;
        public double HeightMeters { get { return _HeightMeters; } set { if (value == _HeightMeters) return; _HeightMeters = value; OnPropertyChanged("HeightMeters"); } }

        private double _HeightFeet = Double.NaN;
        public double HeightFeet { get { return _HeightFeet; } set { if (value == _HeightFeet) return; _HeightFeet = value; OnPropertyChanged("HeightFeet"); } }

        private double _HeightInches = Double.NaN;
        public double HeightInches { get { return _HeightInches; } set { if (value == _HeightInches) return; _HeightInches = value; OnPropertyChanged("HeightInches"); } }

        private double _HeightJustInches = Double.NaN;
        public double HeightJustInches { get { return _HeightJustInches; } set { if (value == _HeightJustInches) return; _HeightJustInches = value; OnPropertyChanged("HeightJustInches"); } }


        private double _BMI = Double.NaN;
        public double BMI { get { return _BMI; } set { if (value == _BMI) return; _BMI = value; OnPropertyChanged("BMI"); } }

        private double _AgeMonths = Double.NaN;
        public double AgeMonths { get { return _AgeMonths; } set { if (value == _AgeMonths) return; _AgeMonths = value; OnPropertyChanged("AgeMonths"); } }

        private double _AgeMonthPart =0;
        public double AgeMonthPart { get { return _AgeMonthPart; } set { if (value == _AgeMonthPart) return; _AgeMonthPart = value; OnPropertyChanged("AgeMonthPart"); } }

        private double _AgeYearPart = Double.NaN;
        public double AgeYearPart { get { return _AgeYearPart; } set { if (value == _AgeYearPart) return; _AgeYearPart = value; OnPropertyChanged("AgeYearPart"); } }

        // male=1 female=2
        private double _Gender = 2;
        public double Gender { get { return _Gender; } set { if (value == _Gender) return; _Gender = value; OnPropertyChanged("Gender"); } }

        //private string _Gender = "Male";
        //public string Gender { get { return _Gender; } set { if (value == _Gender) return; _Gender = value; OnPropertyChanged("Gender"); } }

        private double _BMIChildUpperPercent = Double.NaN;
        public double BMIChildUpperPercent { get { return _BMIChildUpperPercent; } set { if (value == _BMIChildUpperPercent) return; _BMIChildUpperPercent = value; OnPropertyChanged("BMIChildUpperPercent"); } }

        private string _BMIChildBand = "";
        public string BMIChildBand { get { return _BMIChildBand; } set { if (value == _BMIChildBand) return; _BMIChildBand = value; OnPropertyChanged("BMIChildBand"); } }

        private string _BMIChildInterpretation = "";
        public string BMIChildInterpretation { get { return _BMIChildInterpretation; } set { if (value == _BMIChildInterpretation) return; _BMIChildInterpretation = value; OnPropertyChanged("BMIChildInterpretation"); } }

        CsvTable childTable = null;
        private async void InitBmiChildTable()
        {
            if(childTable != null) return;

            childTable = new CsvTable();
            await childTable.ReadAsync("", "bmiagerev.csv");
        }
    }
}
