using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace AdvancedCalculator
{
    public class Background: INotifyPropertyChanged
    {
        // Needs a Grid (passed in to Redraw) which in turn is inside a 
        // ... which is set to UniformToFill
        public Background()
        {
            _TextColor = Windows.UI.Colors.DarkGray;
            _BackgroundText = "🎃";
            _BackgroundText = "🚄🚉|🍴🍵";
            SetDefaultTextVolume();
            _NumberOfRows = 10;
            _NumberOfCols = 20;
            _NumberMargin = 10;
            _NumberOpacity = 32;
        }
        public void SetDefaultTextVolume()
        {
            _BackgroundText = "Cone Volume=⅓𝛑Rh|Sphere Volume=⁴∕₃𝛑R³|Cylinder Volume=𝛑R²h";
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        private Windows.UI.Color _TextColor;
        public Windows.UI.Color TextColor { get { return _TextColor; } set { if (value == _TextColor) return; _TextColor = value; NotifyPropertyChanged("TextColor"); } }

        private string _BackgroundText = "12|34";
        public string BackgroundText { get { return _BackgroundText; } set { if (value == _BackgroundText) return; _BackgroundText = value; NotifyPropertyChanged("BackgroundText"); } }


        private double _NumberOfRows = 10;
        public double NumberOfRows { get { return _NumberOfRows; } set { if (value == _NumberOfRows) return; _NumberOfRows = value; NotifyPropertyChanged("NumberOfRows"); } }

        private double _NumberOfCols = 10;
        public double NumberOfCols { get { return _NumberOfCols; } set { if (value == _NumberOfCols) return; _NumberOfCols = value; NotifyPropertyChanged("NumberOfCols"); } }

        private double _NumberMargin = 10;
        public double NumberMargin { get { return _NumberMargin; } set { if (value == _NumberMargin) return; _NumberMargin = value; NotifyPropertyChanged("NumberMargin"); } }

        private int _NumberOpacity = 10;
        public int NumberOpacity { get { return _NumberOpacity; } set { if (value == _NumberOpacity) return; _NumberOpacity = value; NotifyPropertyChanged("NumberOpacity"); } }

        /*
        public double KMargin { get; set; }
        public int Opacity { get; set; }
        private Windows.UI.Color _TextColor;
        public Windows.UI.Color TextColor { get { return _TextColor; } set { _TextColor = value; } }
        public String BackgroundText { get; set; }
         */
        Grid SavedBackgroundGrid = null;

        public void RedrawColor(Grid uiBackground = null)
        {
            if (uiBackground != null) SavedBackgroundGrid = uiBackground;
            else uiBackground = SavedBackgroundGrid;
            if (uiBackground == null)
            {
                return;
            }
            var color = new Windows.UI.Xaml.Media.SolidColorBrush(TextColor);
            foreach (var child in uiBackground.Children)
            {
                var tb = child as TextBlock;
                tb.Foreground = color;
            }
        }
        public void Redraw(Grid uiBackground = null)
        {
            if (uiBackground != null) SavedBackgroundGrid = uiBackground;
            else uiBackground = SavedBackgroundGrid;
            // UniformToFill is best...
            
            var lines = BackgroundText.Split(new char[] { '|' });
            //int NumberOfCols = (int)(NumberOfRows*10);
            _TextColor.A = (byte)NumberOpacity;
            var color = new Windows.UI.Xaml.Media.SolidColorBrush(TextColor);


            uiBackground.ColumnDefinitions.Clear();
            uiBackground.RowDefinitions.Clear();
            uiBackground.Children.Clear();
            for (int c = 0; c < NumberOfCols; c++)
            {
                uiBackground.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
            }
            for (int r = 0; r < NumberOfRows; r++)
            {
                uiBackground.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
            }
            for (int r = 0; r < NumberOfRows; r++)
            {
                for (int c = 0; c < NumberOfCols; c++)
                {
                    var tb = new TextBlock();
                    tb.Foreground = color;
                    tb.FontSize = 40;
                    for (var i = 0; i < lines.Count(); i++)
                    {
                        if (i > 0)
                        {
                            tb.Inlines.Add(new Windows.UI.Xaml.Documents.LineBreak());
                        }
                        tb.Inlines.Add(new Windows.UI.Xaml.Documents.Run() { Text = lines[i] });
                    }
                    //tb.Inlines.Add(new Windows.UI.Xaml.Documents.Run() { Text = "🍴🍵", FontSize=40, Foreground=color });
                    tb.Margin = new Thickness(NumberMargin);
                    uiBackground.Children.Add(tb);
                    Grid.SetColumn(tb, c);
                    Grid.SetRow(tb, r);
                }
            }
        }
    }
}
