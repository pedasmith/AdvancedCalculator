using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
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
    public sealed partial class XYGraphControl : UserControl
    {
        public XYGraphControl()
        {
            this.InitializeComponent();
        }

        public GraphMinMax GraphAppliedMinMaxX { get; set; } = new GraphMinMax();
        public GraphMinMax GraphAppliedMinMaxY { get; set; } = new GraphMinMax();
        public GraphMinMax GraphCalculatedMinMaxX { get; set; } = new GraphMinMax();
        public GraphMinMax GraphCalculatedMinMaxY { get; set; } = new GraphMinMax();

        public string TitleX { get; set; } = "Data";
        public string TitleY { get; set; } = "Response";

        private double dataToPosX(double data, double posW)
        {
            double dataMin = GraphAppliedMinMaxX.MinDataValue;
            double dataMax = GraphAppliedMinMaxX.MaxDataValue;
            if (dataMin == dataMax) return posW / 2;

            double dataRatio = (data - dataMin) / (dataMax - dataMin);
            if (dataRatio < 0.0) dataRatio = 0.0;
            if (dataRatio > 1.0) dataRatio = 1.0;
            double pos = dataRatio * posW;
            return pos;
        }

        private double dataToPosY(double data, double posH)
        {
            double dataMin = GraphAppliedMinMaxY.MinDataValue;
            double dataMax = GraphAppliedMinMaxY.MaxDataValue;
            if (dataMin == dataMax) return posH / 2;

            double dataRatio = (data - dataMin) / (dataMax - dataMin);

            if (dataRatio < 0.0) dataRatio = 0.0;
            if (dataRatio > 1.0) dataRatio = 1.0;
            double pos = posH - 1 - dataRatio * posH;
            return pos;
        }

        public void Set (IList<double> X, IList<double> Y)
        {
            // Children 0..8 (inclusive) are part of the graph.
            // Everything else should be removed;
            const int FIRST_DATA_POINT=10;
            for (int i=uiCanvas.Children.Count - 1; i>FIRST_DATA_POINT; i--)
            {
                uiCanvas.Children.RemoveAt(i);
            }

            var N = Math.Min(X.Count, Y.Count); // Extra points are simply ignored.
            Brush fill = new SolidColorBrush(Colors.White);

            StatisticsIQR iqrX = new StatisticsIQR(X);
            StatisticsIQR iqrY = new StatisticsIQR(Y);

            if (!iqrX.AllRegularNumbers || !iqrY.AllRegularNumbers) return;


            double rangeX = iqrX.MaxValue - iqrX.MinValue;
            double minmarginX = rangeX * .15;
            double maxmarginX = rangeX * .25;
            double mindataX = DrawRange.NiceNumberInRange(iqrX.MinValue - minmarginX, iqrX.MinValue - maxmarginX);
            double maxdataX = DrawRange.NiceNumberInRange(iqrX.MaxValue + minmarginX, iqrX.MaxValue + maxmarginX);

            double rangeY = iqrY.MaxValue - iqrY.MinValue;
            double minmarginY = rangeY * .15;
            double maxmarginY = rangeY * .25;
            double mindataY = DrawRange.NiceNumberInRange(iqrY.MinValue - minmarginY, iqrY.MinValue - maxmarginY);
            double maxdataY = DrawRange.NiceNumberInRange(iqrY.MaxValue + minmarginY, iqrY.MaxValue + maxmarginY);

            GraphAppliedMinMaxX.MinDataValue = mindataX;
            GraphAppliedMinMaxX.MaxDataValue = maxdataX;
            GraphCalculatedMinMaxX.MinDataValue = mindataX;
            GraphCalculatedMinMaxX.MaxDataValue = maxdataX;

            GraphAppliedMinMaxY.MinDataValue = mindataY;
            GraphAppliedMinMaxY.MaxDataValue = maxdataY;
            GraphCalculatedMinMaxY.MinDataValue = mindataY;
            GraphCalculatedMinMaxY.MaxDataValue = maxdataY;

            uiY0LabelText.Text = GraphAppliedMinMaxY.MinDataValue.ToString(); ;
            uiY1LabelText.Text = GraphAppliedMinMaxY.MaxDataValue.ToString(); ;

            uiX0LabelText.Text = GraphAppliedMinMaxX.MinDataValue.ToString(); ;
            uiX1LabelText.Text = GraphAppliedMinMaxX.MaxDataValue.ToString(); ;


            double posH = uiCanvas.ActualHeight;
            double posW = uiCanvas.ActualWidth;

            for (int i=0; i<N; i++)
            {
                double dataX = X[i];
                double dataY = Y[i];
                double posX = dataToPosX(dataX, posW);
                double posY = dataToPosY(dataY, posH);

                var point = new Windows.UI.Xaml.Shapes.Rectangle();
                point.Width = 3;
                point.Height = 3;
                point.Fill = fill;
                uiCanvas.Children.Add(point);
                Canvas.SetLeft(point, posX - 1);
                Canvas.SetTop(point, posY - 1);
            }

            // Update the regression line
            StatisticsRegression regression = new StatisticsRegression(X, Y);
            var posX1 = dataToPosX(iqrX.MinValue, posW);
            var posY1 = dataToPosY(regression.EstimateY(iqrX.MinValue), posH);
            if (double.IsNaN (posY1)) posY1 = dataToPosY(iqrX.MinValue, posH);

            var posX2 = dataToPosX(iqrX.MaxValue, posW);
            var posY2 = dataToPosY(regression.EstimateY(iqrX.MaxValue), posH);
            if (double.IsNaN(posY2)) posY2 = dataToPosY(iqrX.MaxValue, posH);

            uiRegressionLine.X1 = posX1;
            uiRegressionLine.Y1 = posY1;

            uiRegressionLine.X2 = posX2;
            uiRegressionLine.Y2 = posY2;

            uiTitleX.Text = TitleX;
            uiTitleY.Text = TitleY;
        }
    }
}
