using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace AdvancedCalculator
{
    public sealed partial class KeyboardConstantsControl : UserControl
    {
        public KeyboardConstantsControl()
        {
            this.InitializeComponent();
        }

        CalculatorLog Log = null;
        SimpleCalculator simpleCalculator = null;
        IUpdateKeyboardDisplays updateKeyboardDisplays = null;
        public void Initialize(CalculatorLog log, SimpleCalculator simpleCalculator, IUpdateKeyboardDisplays updateKeyboardDisplays)
        {
            this.Log = log;
            this.simpleCalculator = simpleCalculator;
            this.updateKeyboardDisplays = updateKeyboardDisplays;
        }


        private void OnButton(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;
            var value = button.Tag as string;
            if (value == null) return;
            // Don't log personal data: 
            //Log.WriteWithTime("Button: " + value + "\r\n");
            simpleCalculator.DoButton(value);
            if (!value.StartsWith("#KEY"))
            {
                Log.WriteWithTime("ERROR: invalid key sequence  " + value);
            }

            this.Focus(Windows.UI.Xaml.FocusState.Programmatic);
        }

    }
}
