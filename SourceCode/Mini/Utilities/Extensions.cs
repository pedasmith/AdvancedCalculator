using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Radios;
using Windows.Devices.WiFi;
using Windows.Networking.Connectivity;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace Shipwreck.Utilities
{
    public static class ObservableCollectionsExtensions
    {
        public static void Sort<T>(this ObservableCollection<T> observable) where T : IComparable<T>, IEquatable<T>
        {
            List<T> sorted = observable.OrderBy(x => x).ToList();
            
            int ptr = 0;
            while (ptr < sorted.Count)
            {
                if (!observable[ptr].Equals(sorted[ptr]))
                {
                    T t = observable[ptr];
                    observable.RemoveAt(ptr);
                    observable.Insert(sorted.IndexOf(t), t);
                }
                else
                {
                    ptr++;
                }
            }
        }
    }
    public static class ColorExtensions
    {
        public static Color FromName(string name)
        {
            var property = typeof(Colors).GetRuntimeProperty(name);
            return (Color)property.GetValue(null);
        }

        public static Color Parse(string color)
        {
            var offset = color.StartsWith("#") ? 1 : 0;
            if (offset == 1)
            {
                var a = Byte.Parse(color.Substring(0 + offset, 2), NumberStyles.HexNumber);
                var r = Byte.Parse(color.Substring(2 + offset, 2), NumberStyles.HexNumber);
                var g = Byte.Parse(color.Substring(4 + offset, 2), NumberStyles.HexNumber);
                var b = Byte.Parse(color.Substring(6 + offset, 2), NumberStyles.HexNumber);

                return Color.FromArgb(a, r, g, b);
            }
            return ColorExtensions.FromName(color);
        }
    }

    // FF00 --> 0000FF00 --> 0000FF00-0000-1000-8000-00805f9b34fb
    // BluetoothGuidSuffix = "-0000-1000-8000-00805f9b34fb";
    public class SmallestBluetoothGuid : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is Guid) value = ((Guid)value).ToString();
            if (value is String && value != null)
            {
                var str = value as string;
                bool isBrace = str.StartsWith("{") && str.EndsWith("}");
                if (isBrace) str = str.Substring(1, str.Length - 2);
                if (str.EndsWith("-0000-1000-8000-00805f9b34fb"))
                {
                    str = str.Replace("-0000-1000-8000-00805f9b34fb", "");
                    if (str.StartsWith("0000")) str = str.Substring(4);
                    value = str;
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class InvisibleWhenEmpty: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is IList)
            {
                if ((value as IList).Count == 0) return Visibility.Collapsed;
            }
            else if (value is String)
            {
                if (value == null || (value as String).Length == 0) return Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    // Usage: Visibility="{Binding BthDevices.Count, Mode=TwoWay, Converter={StaticResource VisibleWhenEmpty}}"
    public class VisibleWhenEmpty : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is IList)
            {
                if ((value as IList).Count == 0) return Visibility.Visible;
            }
            else if (value is Int32)
            {
                if (((Int32)value) == 0) return Visibility.Visible;
            }
            else if (value is String)
            {
                if (value == null || (value as String).Length == 0) return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class TimeOfDate : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTimeOffset)
            {
                return ((DateTimeOffset)value).ToString("T");
            }
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class RadioStateOnOffConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is RadioState)
            {
                switch ((RadioState)value)
                {
                    case RadioState.On: return "(on)";
                    case RadioState.Disabled: return "(disabled)";
                    case RadioState.Off: return "(off)";
                    default: return string.Format("({0})", value.ToString());
                }
            }
            return value.ToString();

        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }

    public class RadioStateToBrushConverter : IValueConverter
    {
        public RadioStateToBrushConverter()
        {
            OnColor = "White";
            OffColor = "Gray";
            DisabledColor = "DarkGray";
            DefaultColor = "LimeGreen";
        }
        public string OnColor { get; set; }
        public string OffColor { get; set; }
        public string DisabledColor { get; set; }
        public string DefaultColor { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
                return new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

            if (value is Color)
                return new SolidColorBrush((Color)value);

            if (value is string)
                return new SolidColorBrush(ColorExtensions.Parse((string)value));

            if (value is RadioState)
            {
                switch ((RadioState)value)
                {
                    case RadioState.On: return new SolidColorBrush(ColorExtensions.Parse(OnColor));
                    case RadioState.Disabled: return new SolidColorBrush(ColorExtensions.Parse(DisabledColor));
                    case RadioState.Off: return new SolidColorBrush(ColorExtensions.Parse(OffColor));
                    default: return new SolidColorBrush(ColorExtensions.Parse(DefaultColor));
                }
            }
            if (value is RadioAccessStatus)
            {
                switch ((RadioAccessStatus)value)
                {
                    case RadioAccessStatus.Allowed: return new SolidColorBrush(Colors.Green);
                    case RadioAccessStatus.DeniedByUser: return new SolidColorBrush(Colors.Pink);
                    case RadioAccessStatus.DeniedBySystem: return new SolidColorBrush(Colors.DarkRed);
                    default: return new SolidColorBrush(Colors.Goldenrod);
                }
            }
            throw new NotSupportedException("ColorToBurshConverter only supports converting from Color and String");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }


    public class SignalStrengthToBarsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
                return new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

            string[] numbers = new string[] { "0", "1", "2", "3", "4" };
            string[] graphic = new string[] { "0", "▏", "▎", "▍", "▌" };
            string[] graphic2 = new string[] { "0", "▎", "▎▎", "▎▎▎", "▎▎▎▎" };
            string[] nary= new string[] { "0", "⫿", "⫿⫿", "⫿⫿⫿", "⫿⫿⫿⫿" }; // nary white vertical bar
            string[] medvert = new string[] { "0", "❚⫿⫿⫿", "❚❚⫿⫿", "❚❚❚⫿", "❚❚❚❚" }; // medium vertical bar
            string[] vertbar = new string[] { "0", "▮▯▯▯", "▮▮▯▯", "▮▮▮▯", "▮▮▮▮" }; // white vertical bar
            string[] vertrect = new string[] { "0", "▯▯▯", "▯▯", "▯", "❚❚❚❚" }; // white vertical rectangle
            string[] bars = vertbar;

            if (value is byte)
            {
                var b = (byte)value;
                if (b >= 0 && b < bars.Length) return bars[b];
                return b.ToString();
            }
            throw new NotSupportedException("SignalStrengthToBarsConverter only supports converting from byte");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }


    public class DBmwToPowerConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string retval = "null mw";
            if (value == null)
                return retval;

            if (value is Double)
            {
                var dbmw = (Double)value;
                var p = Math.Pow (10, (dbmw / 10));
                string power = "mw";
                if (p < 1) { p *= 1000; power = "µw"; }
                if (p < 1) { p *= 1000; power = "𝑛w"; }
                if (p < 1) { p *= 1000; power = "pw"; }
                if (p >= 100)
                {
                    retval = string.Format("{0:F0} {1}", p, power);
                }
                else if (p >= 10)
                {
                    retval = string.Format("{0:F1} {1}", p, power);
                }
                else if (p >= 1)
                {
                    retval = string.Format("{0:F2} {1}", p, power);
                }
                else if (p >= 0.010) // e.g., 0.051 pw
                {
                    retval = string.Format("{0:F3} {1}", p, power);
                }
                else
                {
                    retval = string.Format("{0} {1}", p, power);
                }
                return retval;
            }

            throw new NotSupportedException("DBmwToPowerConverter only supports converting from Double");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }


    public class CountInParenthesisConverter : IValueConverter
    {
        public CountInParenthesisConverter()
        {
            MinVisibleValue = 1;
            FormatString = "({0} APs)";
        }
        public double MinVisibleValue { get; set; }
        public string FormatString { get; set; }
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string retval = "";
            if (value == null)
                return retval;

            double v = MinVisibleValue;
            try
            {
                if (value is Double) v = (double)value;
                else if (value is Int32) v = (int)value;
                else v = (double)value;
            }
            catch (Exception)
            {
                throw new NotSupportedException("CountInParenthesisConverter only supports converting from int");
            }
            if (v > MinVisibleValue) retval = string.Format(FormatString, v);
            return retval;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }

    public class BeaconIntervalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string retval = value.ToString();
            if (value == null)
                return retval;

            if (value is TimeSpan)
            {
                var ts = (TimeSpan)value;
                if (ts.TotalSeconds <= 1) // Show as milliseconds
                {
                    retval = string.Format("{0} ms", ts.TotalMilliseconds);
                }
                else if (ts.TotalSeconds <60) // Show as seconds
                {
                    if (ts.Milliseconds == 0)
                    {
                        retval = string.Format("{0:F3} s", ts.TotalSeconds);
                    }
                    else
                    {
                        retval = string.Format("{0:F0} s", ts.TotalSeconds);
                    }
                }
                else
                {
                    retval = ts.ToString();
                }
                return retval;
            }

            throw new NotSupportedException("BeaconIntervalConverter only supports converting from TimeSpan");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }

    // Input is is kilohertz
    public class FrequencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string retval = value.ToString();
            if (value == null)
                return retval;

            if (value is Int32)
            {
                var units = "KHz";
                var d = (Double)(Int32)value;
                if (d > 1000) { d = d / 1000.0; units = "MHz"; }
                if (d > 1000) { d = d / 1000.0; units = "GHz"; }
                if (d > 1000) { d = d / 1000.0; units = "THz"; }
                retval = String.Format("{0:F3} {1}", d, units);
                return retval;
            }

            throw new NotSupportedException("FrequencyConvert only supports converting from Double");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }

    public class IsWiFiDirectConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string retval = value.ToString();
            if (value == null)
                return retval;

            if (value is Boolean)
            {
                var b = (Boolean)value;
                retval = b ? "WiFi Direct" : "";
                return retval;
            }

            throw new NotSupportedException("IsWiFiDirectConverter only supports converting from Boolean");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }

    public class WiFiNetworkKindConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string retval = value.ToString();
            if (value == null)
                return retval;

            if (value is WiFiNetworkKind)
            {
                var kind = (WiFiNetworkKind)value;
                switch (kind)
                {
                    case WiFiNetworkKind.Adhoc: retval = "Adhoc network"; break;
                    case WiFiNetworkKind.Infrastructure: retval = ""; break;
                    case WiFiNetworkKind.Any: retval = "Any network"; break;
                    default: retval = kind.ToString(); break;
                }

                return retval;
            }

            throw new NotSupportedException("IsWiFiDirectConverter only supports converting from Boolean");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }

    public class WiFiPhyKindConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string retval = value.ToString();
            if (value == null)
                return retval;

            if (value is WiFiPhyKind)
            {
                var kind = (WiFiPhyKind)value;
                switch (kind)
                {
                    // Data is from https://support.metageek.com/hc/en-us/articles/202162320-Wi-Fi-Phy-Types
                    case WiFiPhyKind.Dsss: retval = "802.11"; break;
                    case WiFiPhyKind.Fhss: retval = "802.11 (fhss)"; break; // fhss and infra-red are aparently very uncommon.
                    case WiFiPhyKind.IRBaseband: retval = "802.11 (infra-red)"; break;
                    case WiFiPhyKind.Ofdm: retval = "802.11a"; break;
                    case WiFiPhyKind.Hrdsss: retval = "802.11b"; break;
                    case WiFiPhyKind.Erp: retval = "802.11g"; break;
                    case WiFiPhyKind.HT: retval = "802.11n"; break;
                    case WiFiPhyKind.Vht: retval = "802.11ac"; break;

                    case WiFiPhyKind.Unknown: retval = "Unknown wifi type"; break;
                    default: retval = kind.ToString(); break;
                }

                return retval;
            }

            throw new NotSupportedException("IsWiFiDirectConverter only supports converting from WiFiPhyKind");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }


}
