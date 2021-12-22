using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Globalization;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace AdvancedCalculator
{
    public sealed partial class DateControl : UserControl, IInitializeCalculator
    {
        public DateControl()
        {
            this.InitializeComponent();
            this.Loaded += DateCompareControl_Loaded;
        }

        public void Initialize(SimpleCalculator simpleCalculator)
        {
            uiMain.Initialize();
        }

        private void UpdateResults()
        {
            try
            {
                var from = uiFrom.Date;
                var to = uiTo.Date;

                switch (CurrCalcType)
                {
                    case CalcType.Compare:
                        var delta = to.Subtract(from);
                        var startIsEarlier = true;
                        if (delta.Days < 0)
                        {
                            startIsEarlier = false;
                            delta = from.Subtract(to);
                        }

                        var full = "";
                        var days = delta.Days;
                        if (days > 7)
                        {
                            var weeks = Math.Floor((double)days / 7.0);
                            days = days - (int)(weeks * 7);
                            full = $"{weeks} weeks, ";
                        }
                        full += $"{days} days";
                        if (!startIsEarlier)
                        {
                            full += " (in the past)";
                        }
                        uiResultsMain.Text = $"{delta.Days} days";
                        uiResultsFull.Text = full;

                        if (uiFrom.CalendarIdentifier != CalendarIdentifiers.Gregorian)
                        {
                            // Convert the calendar to gregorian version
                            uiResultsFull.Text += $"\n{from.ToString("D")} (Gregorian)";
                        }
                        break;
                    case CalcType.ToGregorian:
                        uiResultsMain.Text = $"{from.ToString("D")}";
                        uiResultsFull.Text = "Converted to the Gregorian calendar";
                        break;

                    case CalcType.AddDay:
                        var ndaystr = uiDaysToAdd.Text;
                        int nday = 0;
                        bool ok = Int32.TryParse(ndaystr, out nday);
                        if (ok)
                        {
                            var newday = from.AddDays(nday);
                            uiResultsMain.Text = $"{newday.ToString("D")}";
                            uiResultsFull.Text = "Results converted to the Gregorian calendar";
                        }
                        else
                        {
                            uiResultsMain.Text = "Can't calculate with that!";
                            uiResultsFull.Text = "Yo must enter a number like 4 or -3";
                        }
                        break;
                }
            }
            catch (Exception)
            {
                // Don't know what the problem is, but I don't want to crash!
            }
        }


        Dictionary<string, string> calendars = new Dictionary<string, string>()
        {
            {  "Gregorian",   CalendarIdentifiers.Gregorian },
            {  "Hebrew",  CalendarIdentifiers.Hebrew },
            {  "Hijri",   CalendarIdentifiers.Hijri }, // No. Strings always in Arabic.
            {  "Japanese",    CalendarIdentifiers.Japanese  }, // No. Strings always in Japanese.
            {  "Julian",  CalendarIdentifiers.Julian }, //    Yes.
            {  "Korean",  CalendarIdentifiers.Korean }, //    Yes.
#if !WINDOWS8
            {  "Persian",  CalendarIdentifiers.Persian }, //    Yes.
#endif
            {  "Taiwan",  CalendarIdentifiers.Taiwan }, //    Yes.
            {  "Thai",    CalendarIdentifiers.Thai }, //  Yes.
            {  "Umm al-Qura",    CalendarIdentifiers.UmAlQura  }, // No. Strings always in Arabic.
        };

        private void DateCompareControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Add in the calendar id
            foreach (var item in calendars)
            {
                uiFromType.Items.Add(item.Key);
                uiToType.Items.Add(item.Key);
            }
            uiFromType.SelectedItem = "Gregorian";
            uiToType.SelectedItem = "Gregorian";
            CurrCalcType = CalcType.Compare;
        }

        private void OnDateChanged(object sender, DatePickerValueChangedEventArgs e)
        {
            UpdateResults();
        }


        private void OnCalendarTypeChange(object sender, SelectionChangedEventArgs e)
        {
            var type = (string)uiFromType.SelectedValue;
            if (type != null)
            {
                var value = calendars[type];
                uiFrom.CalendarIdentifier = value;
            }

            type = (string)uiToType.SelectedValue;
            if (type != null)
            {
                var value = calendars[type];
                uiTo.CalendarIdentifier = value;
            }
            UpdateResults();
        }

        enum CalcType {  Compare, ToGregorian, AddDay};
        CalcType CurrCalcType = CalcType.Compare;


        private void OnActionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (uiSecondDate == null || uiAddDay == null) return;

            var selected = uiAction.SelectedItem as ComboBoxItem;
            switch (selected.Tag as string)
            {
                case "COMPARE":
                    CurrCalcType = CalcType.Compare;
                    uiSecondDate.Visibility = Visibility.Visible;
                    uiAddDay.Visibility = Visibility.Collapsed;
                    break;
                case "TO_GREGORIAN":
                    CurrCalcType = CalcType.ToGregorian;
                    uiSecondDate.Visibility = Visibility.Collapsed;
                    uiAddDay.Visibility = Visibility.Collapsed;
                    break;
                case "ADD_DAYS":
                    CurrCalcType = CalcType.AddDay;
                    uiSecondDate.Visibility = Visibility.Collapsed;
                    uiAddDay.Visibility = Visibility.Visible;
                    break;
            }
            UpdateResults();
        }

        private void OnAddTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateResults();
        }
    }
}
