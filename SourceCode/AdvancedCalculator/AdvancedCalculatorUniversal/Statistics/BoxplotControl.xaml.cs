using AdvancedCalculator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Statistics
{
    public class BoxplotSettings
    {
        public double BoxWidth { get; set; } = 20;
        public double TickLength { get; set; } = 5;
        public bool ShowP10P90 { get; set; } = true;
    }

    public class GraphMinMax
    {
        public double MinDataValue { get; set; } = 5.0;
        public double MaxDataValue { get; set; } = 60.0;

        public bool AllRegularNumbers // no NaN or Infinity
        {
            get
            {
                bool retval = true;
                if (double.IsInfinity(MinDataValue) || double.IsNaN(MinDataValue)) retval = false;
                if (double.IsInfinity(MaxDataValue) || double.IsNaN(MaxDataValue)) retval = false;

                return retval;
            }
        }
    }





    public sealed partial class BoxplotControl : UserControl
    {
        public BoxplotControl()
        {
            this.InitializeComponent();
        }
        public BoxplotSettings GraphSettings { get; set; } = new BoxplotSettings();
        public GraphMinMax GraphCalculatedMinMax { get; set; } = new GraphMinMax();
        public GraphMinMax GraphAppliedMinMax { get; set; } = new GraphMinMax();
        public StatisticsIQR GraphIQR { get; set; } = new StatisticsIQR();
        public StatisticsClassical GraphClassical { get; set; } = new StatisticsClassical();

        public void Update()
        {
            if (!GraphIQR.AllRegularNumbers || !GraphAppliedMinMax.AllRegularNumbers) return; // Protect our calculations

            double posH = uiCanvas.ActualHeight;

            // Convert lots of data
            double posHigh = dataToPos(GraphIQR.MaxValueInsideFence, GraphAppliedMinMax.MinDataValue, GraphAppliedMinMax.MaxDataValue, posH); ;
            double posLow = dataToPos(GraphIQR.MinValueInsideFence, GraphAppliedMinMax.MinDataValue, GraphAppliedMinMax.MaxDataValue, posH); ;
            double posQ1 = dataToPos(GraphIQR.Q1, GraphAppliedMinMax.MinDataValue, GraphAppliedMinMax.MaxDataValue, posH); ;
            double posQ3 = dataToPos(GraphIQR.Q3, GraphAppliedMinMax.MinDataValue, GraphAppliedMinMax.MaxDataValue, posH); ;
            double posMedian = dataToPos(GraphIQR.Median, GraphAppliedMinMax.MinDataValue, GraphAppliedMinMax.MaxDataValue, posH); 
            double posMeanX = dataToPos(GraphClassical.Mean, GraphAppliedMinMax.MinDataValue, GraphAppliedMinMax.MaxDataValue, posH);
            double posP10 = dataToPos(GraphIQR.P10, GraphAppliedMinMax.MinDataValue, GraphAppliedMinMax.MaxDataValue, posH); ;
            double posP90 = dataToPos(GraphIQR.P90, GraphAppliedMinMax.MinDataValue, GraphAppliedMinMax.MaxDataValue, posH); ;

            double posYMin = dataToPos(GraphAppliedMinMax.MinDataValue, GraphAppliedMinMax.MinDataValue, GraphAppliedMinMax.MaxDataValue, posH);
            double posYMax = dataToPos(GraphAppliedMinMax.MaxDataValue, GraphAppliedMinMax.MinDataValue, GraphAppliedMinMax.MaxDataValue, posH);

            double posLeft = 50; // the left margin

            // Set the heights of the items.
            uiCenter.Y1 = posHigh;
            uiCenter.Y2 = posLow;
            Canvas.SetTop(uiBox, posQ3);
            uiBox.Height = (posQ1 - posQ3);
            uiMedian.Y1 = posMedian;
            uiMedian.Y2 = posMedian;
            uiHighTick.Y1 = posHigh;
            uiHighTick.Y2 = posHigh;
            uiLowTick.Y1 = posLow;
            uiLowTick.Y2 = posLow;
            if (double.IsInfinity(posMeanX) || double.IsNaN(posMeanX))
            {
                uiMean.Visibility = Visibility.Collapsed; // Can't display the mean
            }
            else
            {
                uiMean.Visibility = Visibility.Visible;
                Canvas.SetTop(uiMean, posMeanX);
            }

            // Set the widths and horizontal positions
            double posXCenter =  posLeft + GraphSettings.BoxWidth / 2.0;
            double posLeftPadding = posLeft;  // (GraphSettings.BoxWidth - GraphSettings.BoxWidth) / 2;

            uiCenter.X1 = posXCenter;
            uiCenter.X2 = posXCenter;
            Canvas.SetLeft(uiBox, posLeftPadding);
            uiBox.Width = GraphSettings.BoxWidth;
            uiMedian.X1 = posLeftPadding - GraphSettings.TickLength;
            uiMedian.X2 = posLeftPadding + GraphSettings.BoxWidth + GraphSettings.TickLength;
            uiLowTick.X1 = posXCenter - GraphSettings.TickLength;
            uiLowTick.X2 = posXCenter + GraphSettings.TickLength;
            uiHighTick.X1 = posXCenter - GraphSettings.TickLength;
            uiHighTick.X2 = posXCenter + GraphSettings.TickLength;
            Canvas.SetLeft(uiMean, posXCenter);

            uiMinLabelText.Text = GraphAppliedMinMax.MinDataValue.ToString();
            Canvas.SetTop(uiMinLabelText, posYMin);
            uiMinLabelTick.Y1 = posYMin;
            uiMinLabelTick.Y2 = posYMin;

            uiMaxLabelText.Text = GraphAppliedMinMax.MaxDataValue.ToString();
            Canvas.SetTop(uiMaxLabelText, posYMax);
            uiMaxLabelTick.Y1 = posYMax;
            uiMaxLabelTick.Y2 = posYMax;

            // Always set the p10 and p90 position even if they won't be shown.
            Canvas.SetLeft(uiP10, posXCenter);
            Canvas.SetTop(uiP10, posP10);

            Canvas.SetLeft(uiP90, posXCenter);
            Canvas.SetTop(uiP90, posP90);

            var p10p90vis = GraphSettings.ShowP10P90 ? Visibility.Visible : Visibility.Collapsed;
            uiP10.Visibility = p10p90vis;
            uiP90.Visibility = p10p90vis;
        }

        private double dataToPos (double data, double dataMin, double dataMax, double posH)
        {
            if (dataMax == dataMin) return posH / 2; // Just slap it into the middle
            double dataRatio = (data - dataMin) / (dataMax - dataMin);
            if (dataRatio < 0.0) dataRatio = 0.0;
            if (dataRatio > 1.0) dataRatio = 1.0;
            double pos = posH - 1 - dataRatio * posH;
            return pos;
        }
    }
}
