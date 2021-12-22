using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if DESKTOP
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
#else
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
#endif
using EquationSolver;

namespace NetworkToolkit
{
    public class SolverWPFMetro
    {
        public SolverINPC eq1 { get; set; }
        public SolverINPC eq2 { get; set; }
        public SolverINPC eq3 { get; set; }
#if DESKTOP
#else
        //NetworkToolkit.Common.LayoutAwarePage pageRoot;
#endif
        public SolverWPFMetro(SolverINPC eq, Panel grid)
        {
            eq1 = eq;
            eq2 = eq1;
            eq3 = eq1;

            AutoSetup(grid);
            eq1.RecentlySet.Clear();
            eq2.RecentlySet.Clear();
            eq3.RecentlySet.Clear();
        }


        public void ClearTextboxes(Panel grid)
        {
            foreach (UIElement child in grid.Children)
            {
                if (child is Panel)
                {
                    AutoSetup((Panel)child);
                }
                if (child is TextBox)
                {
                    ((TextBox)child).Text = "";
                }
            }
        }

        public void AutoSetup(Panel grid)
        {
            foreach (UIElement child in grid.Children)
            {
                if (child is Panel)
                {
                    AutoSetup((Panel)child);
                }
                if (child is Control)
                {
                    Control control = (Control)child;
                    String fullName = (control.Name.Contains("auto") || control.Tag==null) ? control.Name : control.Tag as string;
                    String Name = NameFromName(fullName);
                    SolverINPC eq = EqFromName(Name);
                    if (fullName.Contains("auto"))
                    {
                        if (child is TextBox)
                        {
                            eq.AddElement(Name, (TextBox)control);
                            var tb = control as TextBox;
                            tb.KeyUp += Textbox_Changed;
                        }
                        else if (child is ComboBox)
                        {
                            ((ComboBox)control).SelectionChanged += ComboBox_SetEq;
                        }
                    }
                }
            }
        }

        public void AutoInitialize(Panel grid)
        {
            foreach (UIElement child in grid.Children)
            {
                if (child is Panel)
                {
                    AutoSetup((Panel)child);
                }
                if (child is Control)
                {
                    Control control = (Control)child;
                    //String fullName = control.Name.Contains("auto") ? control.Name : control.Tag as string;
                    String fullName = (control.Name.Contains("auto") || control.Tag == null) ? control.Name : control.Tag as string;
                    String Name = NameFromName(fullName);
                    SolverINPC eq = EqFromName(Name);
                    if (child is TextBox)
                    {
                        double Value;
                        bool ok = Double.TryParse(((TextBox)child).Text, out Value);
                        if (ok)
                        {
                            SetValue(Name, Value);
                        }
                    }
                }
            }
        }
#if DESKTOP
#else
        /*
        public void AutoSetup(LayoutAwarePage root)
        {
            //pageRoot = root;
            UIElement el = root.Content;
            AutoSetup((Panel)el);
            AutoInitialize((Panel)el);
        }
         */
        public void AutoSetup(UIElement el)
        {
            //pageRoot = root;
            //UIElement el = root.Content;
            AutoSetup((Panel)el);
            AutoInitialize((Panel)el);
        }
#endif

        private double GetValue(SelectionChangedEventArgs e, double defaultValue)
        {
            try
            {
                ComboBoxItem cbi = ((ComboBoxItem)(e.AddedItems[0]));
                string str = (string)cbi.DataContext;
                if (str == "") str = cbi.Content.ToString();
                double value = Double.Parse(str);
                return value;
            }
            catch (Exception) // Catch both parsing errors and selection errors
            {
                return defaultValue;
            }
        }


        private string GetValue(SelectionChangedEventArgs e, string defaultValue)
        {
            try
            {
                ComboBoxItem cbi = ((ComboBoxItem)(e.AddedItems[0]));
                string str = "";
                try
                {
                    str = (string)cbi.DataContext;
                    if (str == "") str = cbi.Content.ToString();
                }
                catch (Exception)
                {
                    str = cbi.Content.ToString();
                }
                string value = str;
                return value;
            }
            catch (Exception) // Catch both parsing errors and selection errors
            {
                return defaultValue;
            }
        }


        private void SetValue(string Name, double Value)
        {
            if (Name == "Principle")
            {
                Name = "Principle";
            }
            SolverINPC eq = EqFromName(Name);
            eq.SetByName(Name, Value);
            eq.UpdateTextBox(Name);
            eq.SetRecent(Name);
        }



        SolverINPC EqFromName(string Name)
        {
            SolverINPC eq = eq1;
            if (Name.Contains("eq2")) eq = eq2;
            if (Name.Contains("eq3")) eq = eq3;
            return eq;
        }

        String NameFromName(string Name)
        {
            Name = Name.Replace("eq3", "").Replace("eq2", "").Replace("eq1", "").Replace("tb", "").Replace("cb", "").Replace("auto", "");
            return Name;
        }

        private void ComboBox_SetEq(object sender, SelectionChangedEventArgs e)
        {
            ComboBox control = sender as ComboBox;
            //String fullName = control.Name.Contains("auto") ? control.Name : control.Tag as string;
            //String Name = ((ComboBox)sender).Name;
            String fullName = (control.Name.Contains("auto") || control.Tag == null) ? control.Name : control.Tag as string;
            SolverINPC eq = EqFromName(fullName);
            string Name = NameFromName(fullName);
            var textBoxControl = control.SelectedItem as TextBlock;
            if (textBoxControl == null) return; // because we first remove the old selection then add a new one.
            var strValue = textBoxControl.Tag as string;

            if (eq.NameIsDouble(Name))
            {
                //eq.SetByName(Name, GetValue(e, eq.GetByNameDouble(Name)));
                double value;
                bool ok = Double.TryParse(strValue, out value);
                if (ok)
                {
                    double OldValue = eq.GetByNameDouble(Name);
                    if (OldValue != value)
                    {
                        // PII; never ship with this on: AdvancedCalculator.Log.WriteWithTime("Solver: setting {0} to {1}\r\n", Name, value);
                        eq.SetByName(Name, value);
                        eq.SetRecent(Name);
                    }
                    //SetNoErrorColor(control);
                }
                else
                {
                    //SetErrorColor(control);
                }
            }
            else
            {
                eq.SetByName(Name, GetValue(e, eq.GetByNameString(Name)));
            }
            eq.UpdateTextBox(Name);
            eq.SetRecent(Name);
            //((ComboBox)sender).SelectedIndex = -1;
        }

        public void SetTextbox (TextBox tb, String NewValue)
        {
            tb.Text = NewValue;
            Textbox_Changed (tb, null);
        }

        private void Textbox_Changed(object sender, /* TextChangedEventArgs */ object e)
        {
            TextBox control = sender as TextBox;
            //String fullName = control.Name.Contains("auto") ? control.Name : control.Tag as string;
            //string tbName = tbsender.Name;
            String fullName = (control.Name.Contains("auto") || control.Tag == null) ? control.Name : control.Tag as string;
            SolverINPC eq = EqFromName(fullName);
            string Name = NameFromName(fullName);
            string strValue = control.Text;
            if (eq.NameIsDouble(Name))
            {
                double value;
                bool ok = Double.TryParse(strValue, out value);
                if (ok)
                {
                    double OldValue = eq.GetByNameDouble(Name);
                    if (OldValue != value)
                    {
                        // PII; never ship with this on: AdvancedCalculator.Log.WriteWithTime("Solver: setting {0} to {1}\r\n", Name, value);
                        eq.SetByName(Name, value);
                        eq.SetRecent(Name);
                    }
                    SetNoErrorColor(control);
                }
                else
                {
                    SetErrorColor(control);
                }
            }
            else
            {
                Int32 value;
                bool ok = true;
                if (Name.Contains("Decimal")) ok = Int32.TryParse(strValue, out value);
                if (Name.Contains("Hex")) ok = Int32.TryParse(strValue, System.Globalization.NumberStyles.HexNumber, null, out value);
                if (ok)
                {
                    eq.SetByName(Name, strValue);
                    SetNoErrorColor(control);
                }
                else
                {
                    SetErrorColor(control);
                }
                eq.SetRecent(Name);
            }
        }
        private void SetNoErrorColor(TextBox tbsender)
        {
            tbsender.BorderBrush = null;
        }

        private void SetErrorColor(TextBox tbsender)
        {
#if DESKTOP
                    var color = System.Windows.Media.Colors.Red;
#else
            var color = Windows.UI.Colors.Red;
#endif
            tbsender.BorderBrush = new SolidColorBrush(color);
        }

    }
}
