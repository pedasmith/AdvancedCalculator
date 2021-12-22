using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace BCBasic
{

    //
    // Emulates what a calculator needs in order to have some programming buttons.
    //
    public sealed partial class SampleCalculatorControl : UserControl, INotifyPropertyChanged, ICalculator, IObjectValue
    {

        private double _NumericValue = 0.0;
        public double NumericValue { get { return _NumericValue; } set { if (value == _NumericValue) return; _NumericValue = value; NotifyPropertyChanged(); } }

        private string _MessageValue = "calculator";
        public string MessageValue { get { return _MessageValue; } set { ChangedMessageValue = value; if (value == _MessageValue) return; _MessageValue = value; NotifyPropertyChanged(); } }

        private string _ChangedMessageValue = "";
        public string ChangedMessageValue { get { return _ChangedMessageValue; } set { if (value == _ChangedMessageValue) return; _ChangedMessageValue = value; NotifyPropertyChanged(); } }

        public ICalculatorConnection CalculatorConnection;

        public SampleCalculatorControl()
        {
            this.DataContext = this;
            this.InitializeComponent();

        }

        private void OnProg(object sender, RoutedEventArgs e)
        {
            if (CalculatorConnection != null) CalculatorConnection.DoProgramButton();
        }

        private async void OnKey(object sender, RoutedEventArgs e)
        {
            var key = (sender as Button).Tag as string;
            if (key == null) return;
            if (CalculatorConnection != null) await CalculatorConnection.DoRunButtonProgramAsync(key);
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public string PreferredName
        {
            get { return "Calculator"; }
        }

        public BCValue GetValue(string name)
        {
            switch (name)
            {
                case "Value": return new BCValue(NumericValue); 
            }
            return new BCValue(double.NaN); ;
        }

        public void SetValue(string name, BCValue value)
        {
            switch (name)
            {
                case "Message":
                    MessageValue = value.AsString;
                    break;
                case "Value":
                    NumericValue = value.AsDouble;
                    break;
            }
        }

        public IList<string> GetNames()
        {
            return new List<string>() { "Message", "Value" };
        }

        public void InitializeForRun()
        {
        }

        public void RunComplete()
        {
        }

        // IObjectValue can also call a function by name
        public async Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            switch (name)
            {
                case "ToString":
                    Retval.AsString = $"Object {PreferredName}";
                    return RunResult.RunStatus.OK;
                default:
                    await Task.Delay(0);
                    BCValue.MakeNoSuchMethod(Retval, this, name);
                    return RunResult.RunStatus.ErrorContinue;
            }
        }

        public void Dispose()
        {
        }
    }
}
