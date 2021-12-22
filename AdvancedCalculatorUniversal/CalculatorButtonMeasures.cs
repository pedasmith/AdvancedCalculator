// Demonstrate public field, INPC, and auto-save
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using Windows.UI.Xaml.Media;

namespace AdvancedCalculator
{
    public class CommonButtonMeasures : INotifyPropertyChanged
    {
        public enum Size { NotSet, Normal, Smaller, Tiny, SmallerOld, TinyOld };

        private Size CurrSize = Size.NotSet;

        public CommonButtonMeasures()
        {
            Init(Size.Normal);
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public void Init(Size size)
        {
            if (size == CurrSize) return;
            CurrSize = size;
            switch (size)
            {
                case Size.Normal:
                case Size.Smaller:
                case Size.Tiny:
                    FontSize = 40;
                    SmallFontSize = 30;
                    TinyFontSize = 24;
                    ResultFontSize = 60;
                    LabelFontSize = 30;
                    SmallLabelFontSize = 20;
                    DiceFontSize = 400;
                    SmallDiceFontSize = 200;
                    FeedbackFontSize = 20;
                    ButtonHeight = 100;
                    ButtonWidth = 100;
                    ButtonWidthPartWide = ButtonWidth * 1.00;
                    ButtonWidthWide = ButtonWidth * 1.5;
                    BorderThickness = 2;
                    break;
                case Size.SmallerOld:
                    FontSize = 32;
                    SmallFontSize = 24;
                    TinyFontSize = 20;
                    ResultFontSize = 48;
                    LabelFontSize = 24;
                    LabelFontSize = 18;
                    DiceFontSize = 320;
                    SmallDiceFontSize = 160;
                    FeedbackFontSize = 20;
                    ButtonHeight = 80;
                    ButtonWidth = 80;
                    ButtonWidthPartWide = ButtonWidth * 1.00;
                    ButtonWidthWide = ButtonWidth * 1.5;
                    BorderThickness = 2;
                    break;
                case Size.TinyOld:
                    FontSize = 16;
                    SmallFontSize = 8;
                    TinyFontSize = 6;
                    ResultFontSize = 30;
                    LabelFontSize = 15;
                    SmallLabelFontSize = 10;
                    DiceFontSize = 160;
                    SmallDiceFontSize = 80;
                    FeedbackFontSize = 20;
                    ButtonHeight = 50;
                    ButtonWidth = 50;
                    ButtonWidthPartWide = ButtonWidth * 1.00;
                    ButtonWidthWide = ButtonWidth * 1.5;
                    BorderThickness = 1;
                    break;
            }
            // Dice get their own sizing scheme.
            switch (size)
            {
                case Size.Normal:
                    DiceFontSize = 400;
                    SmallDiceFontSize = 200;
                    break;
                case Size.Smaller:
                    DiceFontSize = 320;
                    SmallDiceFontSize = 160;
                    break;
                case Size.Tiny:
                    DiceFontSize = 160;
                    SmallDiceFontSize = 80;
                    break;
            }
        }
        private double _FontSize = 40;
        public double FontSize
        {
            get { return _FontSize; }
            set
            {
                if (value != _FontSize)
                {
                    _FontSize = value;
                    NotifyPropertyChanged("FontSize");
                }
            }
        }
        private double _SmallFontSize = 40;
        public double SmallFontSize
        {
            get { return _SmallFontSize; }
            set
            {
                if (value != _SmallFontSize)
                {
                    _SmallFontSize = value;
                    NotifyPropertyChanged("SmallFontSize");
                }
            }
        }
        private double _TinyFontSize = 40;
        public double TinyFontSize
        {
            get { return _TinyFontSize; }
            set
            {
                if (value != _TinyFontSize)
                {
                    _TinyFontSize = value;
                    NotifyPropertyChanged("TinyFontSize");
                }
            }
        }


        private double _ResultFontSize = 40;
        public double ResultFontSize
        {
            get { return _ResultFontSize; }
            set
            {
                if (value != _ResultFontSize)
                {
                    _ResultFontSize = value;
                    NotifyPropertyChanged("ResultFontSize");
                }
            }
        }
        private double _LabelFontSize = 20;
        public double LabelFontSize
        {
            get { return _LabelFontSize; }
            set
            {
                if (value != _LabelFontSize)
                {
                    _LabelFontSize = value;
                    NotifyPropertyChanged("LabelFontSize");
                }
            }
        }

        private double _SmallLabelFontSize = 20;
        public double SmallLabelFontSize
        {
            get { return _SmallLabelFontSize; }
            set
            {
                if (value != _SmallLabelFontSize)
                {
                    _SmallLabelFontSize = value;
                    NotifyPropertyChanged("SmallLabelFontSize");
                }
            }
        }

        static private FontFamily _StdFontFamily = new FontFamily("Segoe UI");
        private FontFamily _FontFamily = _StdFontFamily;
        public FontFamily FontFamily {  get { return _FontFamily; } set { if (value == _FontFamily) return;  _FontFamily = value;  NotifyPropertyChanged("FontFamily"); } }
        public void SetStandardFont()
        {
            FontFamily = _StdFontFamily;
        }
        public void Set7SegmentFont()
        {
            FontFamily = new FontFamily("ms-appx:///Assets/Fonts/Segment.ttf#Segment,Quartz MS Regular,Segoe UI");
        }
        public void SetCarriageDot()
        {
            FontFamily = new FontFamily("ms-appx:///Assets/Fonts/ElectricCarriageDots.ttf#Electric Carriage,Segoe UI");
        }
        public void SetCarriageMonogram()
        {
            FontFamily = new FontFamily("ms-appx:///Assets/Fonts/Monogram Electric Carriage.ttf#Monogram Carriage Call,Segoe UI");
        }
        private double _FeedbackFontSize = 20;
        public double FeedbackFontSize
        {
            get { return _FeedbackFontSize; }
            set
            {
                if (value != _FeedbackFontSize)
                {
                    _FeedbackFontSize = value;
                    NotifyPropertyChanged("FeedbackFontSize");
                }
            }
        }

        private double _BorderThickness = 1;
        public double BorderThickness
        {
            get { return _BorderThickness; }
            set
            {
                if (value != _BorderThickness)
                {
                    _BorderThickness = value;
                    NotifyPropertyChanged("BorderThickness");
                }
            }
        }
        private double _ButtonWidth = 40;
        public double ButtonWidth
        {
            get { return _ButtonWidth; }
            set
            {
                if (value != _ButtonWidth)
                {
                    _ButtonWidth = value;
                    NotifyPropertyChanged("ButtonWidth");
                    NotifyPropertyChanged("ButtonWidthDouble");
                }
            }
        }

        private double _ButtonWidthWide = 40;
        public double ButtonWidthWide
        {
            get { return _ButtonWidthWide; }
            set
            {
                if (value != _ButtonWidthWide)
                {
                    _ButtonWidthWide = value;
                    NotifyPropertyChanged("ButtonWidthWide");
                    NotifyPropertyChanged("ButtonWidthWideDouble");
                }
            }
        }

        private double _ButtonWidthPartWide = 40;
        public double ButtonWidthPartWide
        {
            get { return _ButtonWidthPartWide; }
            set
            {
                if (value != _ButtonWidthPartWide)
                {
                    _ButtonWidthPartWide = value;
                    NotifyPropertyChanged("ButtonWidthPartWide");
                }
            }
        }



        public double ButtonWidthDouble
        {
            get { return ButtonWidth * 2; }

        }

        private double _ButtonHeight = 40;
        public double ButtonHeight
        {
            get { return _ButtonHeight; }
            set
            {
                if (value != _ButtonHeight)
                {
                    _ButtonHeight = value;
                    NotifyPropertyChanged("ButtonHeight");
                }
            }
        }

        private double _DiceFontSize = 40;
        public double DiceFontSize
        {
            get { return _DiceFontSize; }
            set
            {
                if (value != _DiceFontSize)
                {
                    _DiceFontSize = value;
                    NotifyPropertyChanged("DiceFontSize");
                }
            }
        }


        private double _SmallDiceFontSize = 40;
        public double SmallDiceFontSize
        {
            get { return _SmallDiceFontSize; }
            set
            {
                if (value != _SmallDiceFontSize)
                {
                    _SmallDiceFontSize = value;
                    NotifyPropertyChanged("SmallDiceFontSize");
                }
            }
        }


    }
}
