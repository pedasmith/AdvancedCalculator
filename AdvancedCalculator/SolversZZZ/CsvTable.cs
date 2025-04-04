﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedCalculator
{
    public class CsvTable
    {
        public CsvTable()
        {
        }
        public List<string> Headers = new List<string>();
        public List<string[]> Data = new List<string[]>();
        public Dictionary<string, List<double>> ColumnDoubleValues = new Dictionary<string,List<double>>();

        public async void Read()
        {
            Headers.Clear();
            Data.Clear();
            ColumnDoubleValues.Clear();

            // Chart is the CSV from http://www.cdc.gov/growthcharts/cdc_charts.htm
            var folder = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFolderAsync("Assets");
            var file = await folder.GetFileAsync("bmiagerev.csv");
            var lines = await Windows.Storage.FileIO.ReadLinesAsync(file);

            if (lines.Count > 0)
            {
                var line = split(lines[0]);
                foreach (var item in line)
                {
                    Headers.Add(item);
                }
            }
            for (int i = 1; i < lines.Count; i++)
            {
                Data.Add(split(lines[i]));
            }
        }

        public static double CalculateInterpolateRatio(double valueLow, double value, double valueHigh)
        {
            if (value < valueLow) return 0;
            if (value > valueHigh) return 1;
            if (valueLow == valueHigh) return .5;
            double range = valueHigh - valueLow;
            double Retval = (value - valueLow) / range;
            return Retval;
        }

        public static double Interpolate(double valueLow, double interpolateRatio, double valueHigh)
        {
            if (interpolateRatio < 0) return valueLow;
            if (interpolateRatio > 1) return valueHigh;
            if (valueLow == valueHigh) return valueLow; // hardly matters which
            double range = valueHigh - valueLow;
            double Retval = valueLow + (interpolateRatio * range);
            return Retval;
        }



        public int Col(string colName)
        {
            int Retval = Headers.IndexOf (colName);
            return Retval;
        }
        public void InitColumnDoubleValue(string colName, double defaultValue)
        {
            if (ColumnDoubleValues.ContainsKey (colName)) return;

            // Find the column name
            int col = Col (colName);
            if (col < 0) return;
            List<double> values = new List<double>();
            for (int r = 0; r < Data.Count; r++)
            {
                double val;
                bool ok = double.TryParse(Data[r][col], out val);
                if (!ok) val = defaultValue;
                values.Add(val);
            }
            ColumnDoubleValues.Add(colName, values);
        }

        public int FindRowMatch(string colName, double valueToFind, int defaultValue = -1, int startRow = 0)
        {
            InitColumnDoubleValue(colName, 0.0);
            if (!ColumnDoubleValues.ContainsKey(colName)) return defaultValue;
            List<double> values = ColumnDoubleValues[colName];
            for (int r = startRow; r < values.Count; r++)
            {
                if (values[r] == valueToFind)
                {
                    return r;
                }
            }
            return defaultValue;
        }

        public int FindRowLTE(string colName, double valueToFind, int defaultValue = -1, int startRow = 0)
        {
            if (startRow < 0) return defaultValue;
            InitColumnDoubleValue(colName, 0.0);
            if (!ColumnDoubleValues.ContainsKey(colName)) return defaultValue;
            List<double> values = ColumnDoubleValues[colName];
            double bestMatch = 0;
            int bestRow = -1;
            for (int r = startRow; r < values.Count; r++)
            {
                if (values[r] <= valueToFind)
                {
                    if (bestRow == -1 || values[r] > bestMatch)
                    {
                        bestRow = r;
                        bestMatch = values[r];
                    }
                }
            }
            if (bestRow == -1) bestRow = defaultValue;
            return bestRow;
        }

        public int FindRowGTE(string colName, double valueToFind, int defaultValue = -1, int startRow = 0)
        {
            if (startRow < 0) return defaultValue;
            InitColumnDoubleValue(colName, 0.0);
            if (!ColumnDoubleValues.ContainsKey(colName)) return defaultValue;
            List<double> values = ColumnDoubleValues[colName];
            double bestMatch = 0;
            int bestRow = -1;
            for (int r = startRow; r < values.Count; r++)
            {
                if (values[r] >= valueToFind)
                {
                    if (bestRow == -1 || values[r] < bestMatch)
                    {
                        bestRow = r;
                        bestMatch = values[r];
                    }
                }
            }
            if (bestRow == -1) bestRow = defaultValue;
            return bestRow;
        }


        public string Get(string colName, int row, string defaultValue)
        {
            int col = Col(colName);
            if (col < 0) return defaultValue;
            if (row < 0 || row >= Data.Count) return defaultValue;
            if (col >= Data[row].Count()) return defaultValue;
            var Retval = Data[row][col];
            return Retval;
        }

        public double GetDouble(string colName, int row, double defaultValue)
        {
            string val = Get(colName, row, defaultValue.ToString());
            double Retval;
            bool ok = Double.TryParse(val, out Retval);
            if (!ok) return defaultValue;
            return Retval;
        }

        public double GetDoubleInterpolate(string colName, int rowLow, double interpolateRatio, int rowHigh, double defaultValue)
        {
            double valueLow = GetDouble(colName, rowLow, defaultValue);
            double valueHigh = GetDouble(colName, rowHigh, defaultValue);
            double Retval = Interpolate(valueLow, interpolateRatio, valueHigh);
            return Retval;
        }


        char[] comma = new char[1] { ',' };
        string[] split(string line)
        {
            return line.Split(comma);
        }

    }
}
