using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.ComponentModel;
#if DESKTOP
using System.Windows.Controls;
#else
using Windows.UI.Xaml.Controls;
#endif
using System.Reflection;
using System.Diagnostics;

namespace EquationSolver
{
    public class SolverINPC : Solver, INotifyPropertyChanged
    {
        //public Dictionary<string, TextBox> TextBoxes = new Dictionary<string, TextBox>();
        //public Dictionary<string, string> TextBoxFormats = new Dictionary<string, string>();
        public SolverINPC()
        {
            PropertyChanged += Solve;
            PropertyChanged += UpdateTextBox;
        }
        int InSolve = 0;
        public void Solve(object Sender, PropertyChangedEventArgs e)
        {
            string name = e.PropertyName;
            Solve(name);
        }

        string suppressTextboxUpdate = "";
        new public void Solve(string Changed)
        {
            if (InSolve == 0) suppressTextboxUpdate = Changed;
            //if (InSolve > 0) OnPropertyChanged(Changed);
            InSolve++;
            base.Solve(Changed);
            InSolve--;
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
            //UpdateTextBox(name);
        }


        public class SolverUIElement
        {
            public string Name { get; set; }
            public TextBox TextBox { get; set; }
            public string TextBoxFormat { get; set; }
        }
        public List<SolverUIElement> Elements = new List<SolverUIElement>();
        public void AddElement(string Name, TextBox TextBox)
        {
            Elements.Add(new SolverUIElement() { Name = Name, TextBox = TextBox, TextBoxFormat = null });
        }
        public string DefaultTextBoxFormat = "{0:F2}";
        public void UpdateTextBox(Object Sender, PropertyChangedEventArgs e)
        {
            string name = e.PropertyName;
            if (name == suppressTextboxUpdate)
            {
                name = null;
            }
            else
            {
                UpdateTextBox(name);
            }
        }

        public void UpdateTextBox(string name)
        {
            foreach (var element in Elements)
            {
                if (element.Name == name)
                {
                    if (NameIsDouble(name))
                    {
                        string format = element.TextBoxFormat != null ? element.TextBoxFormat : DefaultTextBoxFormat;
                        double value = GetByNameDouble (name);
                        string str = string.Format(format, value);
                        element.TextBox.Text = str;
                    }
                    else
                    {
                        string value = GetByNameString (name);
                        element.TextBox.Text = value;
                    }
                }
            }
        }
    }
}
