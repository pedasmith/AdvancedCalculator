using AdvancedCalculator.Solvers;
using NetworkToolkit;
using Statistics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using enumUtilities;
// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236


namespace AdvancedCalculator
{
    public class GraphTypeConverter : EnumValueConverter<ColumnStatsControl.GraphType> { }

    public sealed partial class ColumnStatsControl : UserControl, IInitializeCalculator, INotifyPropertyChanged
    {
        public ColumnStatsControl()
        {
            this.DataContext = this;
            this.InitializeComponent();
            this.Loaded += (s, e) =>
            {
                uiData0.Text = "7\n7\n5\n6\n8";
                uiData1.Text = "16\n1\n10\n2\n4";
                SelectGraph();
            };
        }

        public enum GraphType
        {
            [enumUtilities.Display("No charts")]
            None,
            [enumUtilities.Display("Boxplots (compare)")]
            BoxplotsCompare,
            [enumUtilities.Display("Boxplots (independant)")]
            BoxplotsIndependant,
            [enumUtilities.Display("Linear Regression")]
            XYGraph,

        };
        private GraphType _CurrGraphType = GraphType.BoxplotsCompare;
        public GraphType CurrGraphType
        {
            get { return _CurrGraphType; }
            set
            {
                if (value == _CurrGraphType) return;
                _CurrGraphType = value;
                SelectGraph();
                NotifyPropertyChanged();
            }
        }

        public void Initialize(SimpleCalculator simpleCalculator)
        {
            this.simpleCalculator = simpleCalculator;
            uiMain.Initialize();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private string _DataTitle0 = "Data";
        public string DataTitle0 { get { return _DataTitle0; } set { if (value == _DataTitle0) return; _DataTitle0 = value; NotifyPropertyChanged(); } }

        private string _DataTitle1 = "Data";
        public string DataTitle1 { get { return _DataTitle1; } set { if (value == _DataTitle1) return; _DataTitle1 = value; NotifyPropertyChanged(); } }

        private void SetDataTitle (string title, int index)
        {
            switch (index)
            {
                case 0: DataTitle0 = title; break;
                case 1: DataTitle1 = title; break;
            }
        }

        private SimpleCalculator simpleCalculator { get; set; }
        SolverWPFMetro solver = null;
        public IFormatDouble d2s { get { if (simpleCalculator != null) return simpleCalculator; return new D2S(); }  }

        public void OnFromCalc(object sender, RoutedEventArgs e)
        {
            MainPage.DoFromCalc(sender as Button, simpleCalculator, solver);
        }

        public void OnToCalc(object sender, RoutedEventArgs e)
        {
            MainPage.DoToCalc(sender as Button, simpleCalculator);
        }

        private void OnDataChanged0(object sender, TextChangedEventArgs e)
        {
            DoRecalculate(uiData0.Text, uiRobustResults0, uiClassicalResults0, uiCompareResults1, uiRegressionResults1, uiBoxplot0, 0);
            RecalculateBoxplotRanges();
        }

        private void OnDataChanged1(object sender, TextChangedEventArgs e)
        {
            DoRecalculate(uiData1.Text, uiRobustResults1, uiClassicalResults1, uiCompareResults1, uiRegressionResults1, uiBoxplot1, 1);
            RecalculateBoxplotRanges();
        }

        HashSet<BoxplotControl> ActiveBoxplots = new HashSet<BoxplotControl>();
        List<List<double>> ActiveDataSets = new List<List<double>>() { null, null };
        private void DoRecalculate(string str, Run tbRobust, Run tbClassical, Run tbCompare, Run tbRegression, BoxplotControl boxplot, int dataIndex) // dataIndex is 0 or 1 for the left and right items
        {
            var lines = str.Split(new char[] { '\n' });
            List<double> data = new List<double>();
            ActiveDataSets[dataIndex] = data;

            bool setTitle = false;
            foreach (var rawline in lines)
            {
                double value = 0.0;
                var line = rawline.Trim();
                if (line == "" || line == null)
                {
                    // do nothing
                }
                else
                {
                    bool isNumeric = Double.TryParse(line, out value);
                    if (!isNumeric)
                    {
                        TimeSpan obj;
                        isNumeric = TimeSpan.TryParse(line, out obj);
                        if (isNumeric)
                        {
                            value = (double)obj.Ticks / (double)TimeSpan.TicksPerSecond;
                        }
                    }
                    if (!isNumeric)
                    {
                        DateTime obj;
                        isNumeric = DateTime.TryParse(line, out obj);
                        if (isNumeric)
                        {
                            value = (double)obj.Ticks / (double)TimeSpan.TicksPerSecond;
                        }
                    }
                    if (isNumeric)
                    {
                        data.Add(value);
                    }
                    else if (!setTitle)
                    {
                        setTitle = true;
                        if (line.StartsWith("#")) line = line.Substring(1).Trim();
                        SetDataTitle(line, dataIndex);
                    }
                }
            }
            if (!setTitle) SetDataTitle("Data", dataIndex);

            if (data.Count >= 2)
            {
                ActiveBoxplots.Add(boxplot);
                var iqr = new StatisticsIQR();

                iqr.Set(data);
                var x = new StatisticsClassical();
                x.Set(data);

                tbRobust.Text = iqr.ToString(d2s);
                tbClassical.Text = x.ToString(d2s);

                boxplot.GraphIQR = iqr;
                boxplot.GraphClassical = x;
                boxplot.GraphSettings.ShowP10P90 = data.Count >= 10; // No point in showing them for tiny data sets.

                // Recalculate all of the boxplot min/max ranges
                double range = iqr.MaxValue - iqr.MinValue;
                double minmargin = range * .15;
                double maxmargin = range * .25;
                double mindata = DrawRange.NiceNumberInRange(iqr.MinValue - minmargin, iqr.MinValue - maxmargin);
                double maxdata = DrawRange.NiceNumberInRange(iqr.MaxValue + minmargin, iqr.MaxValue + maxmargin);

                // Always set all the graphs types -- you know, just in case.
                boxplot.GraphAppliedMinMax.MinDataValue = mindata;
                boxplot.GraphAppliedMinMax.MaxDataValue = maxdata;
                boxplot.GraphCalculatedMinMax.MinDataValue = mindata;
                boxplot.GraphCalculatedMinMax.MaxDataValue = maxdata;
                boxplot.Update();

                // And the linear regression, if appropriate
                if (ActiveDataSets[0] != null && ActiveDataSets[1] != null)
                {
                    var statCompare = new StatisticsTTest();
                    var results = statCompare.Set(ActiveDataSets[0], ActiveDataSets[1], 0.05); // Arguable, desired p-value should be passed in
                    tbCompare.Text = results.ToString(d2s);

                    var statR = new StatisticsRegression();
                    statR.Set(ActiveDataSets[0], ActiveDataSets[1]);
                    tbRegression.Text = statR.ToString(d2s);

                    uiXYGraph.TitleX = DataTitle0;
                    uiXYGraph.TitleY = DataTitle1;
                    uiXYGraph.Set(ActiveDataSets[0], ActiveDataSets[1]);
                }
            }
            else
            {
                // Clear away data when it's not being used
                ActiveBoxplots.Remove(boxplot);
                ActiveDataSets[dataIndex] = null; // No data
                tbClassical.Text = "(not calculated)";
                tbRobust.Text = "(not calculated)";
                tbCompare.Text = "(not calculated)";
                tbRegression.Text = "(not calculated)";
            }
        }

        private void RecalculateBoxplotRanges()
        {
            if (CurrGraphType == GraphType.BoxplotsIndependant)
            {
                foreach (var boxplot in ActiveBoxplots)
                {
                    boxplot.GraphAppliedMinMax.MinDataValue = boxplot.GraphCalculatedMinMax.MinDataValue;
                    boxplot.GraphAppliedMinMax.MaxDataValue = boxplot.GraphCalculatedMinMax.MaxDataValue;
                    boxplot.Update();
                }
            }
            else
            {
                var rangeList = new List<Double>();
                double minRange = Double.MaxValue;
                double maxRange = Double.MinValue;
                foreach (var boxplot in ActiveBoxplots)
                {
                    if (boxplot.GraphCalculatedMinMax.AllRegularNumbers)
                    {
                        minRange = Math.Min(boxplot.GraphCalculatedMinMax.MinDataValue, minRange);
                        maxRange = Math.Max(boxplot.GraphCalculatedMinMax.MaxDataValue, maxRange);
                    }
                }

                if (minRange < Double.MaxValue && maxRange > double.MinValue)
                {
                    foreach (var boxplot in ActiveBoxplots)
                    {
                        boxplot.GraphAppliedMinMax.MinDataValue = minRange;
                        boxplot.GraphAppliedMinMax.MaxDataValue = maxRange;
                        boxplot.Update();
                    }
                }
            }
        }

        private void SelectGraph()
        {
            switch (CurrGraphType)
            {
                case GraphType.None:
                    uiBoxplot0.Visibility = Visibility.Collapsed;
                    uiBoxplot1.Visibility = Visibility.Collapsed;
                    uiXYGraph.Visibility = Visibility.Collapsed;
                    break;
                case GraphType.BoxplotsCompare:
                    uiBoxplot0.Visibility = Visibility.Visible;
                    uiBoxplot1.Visibility = Visibility.Visible;
                    uiXYGraph.Visibility = Visibility.Collapsed;
                    RecalculateBoxplotRanges();
                    break;
                case GraphType.BoxplotsIndependant:
                    uiBoxplot0.Visibility = Visibility.Visible;
                    uiBoxplot1.Visibility = Visibility.Visible;
                    uiXYGraph.Visibility = Visibility.Collapsed;
                    RecalculateBoxplotRanges();
                    break;
                case GraphType.XYGraph:
                    uiBoxplot0.Visibility = Visibility.Collapsed;
                    uiBoxplot1.Visibility = Visibility.Collapsed;
                    uiXYGraph.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}
