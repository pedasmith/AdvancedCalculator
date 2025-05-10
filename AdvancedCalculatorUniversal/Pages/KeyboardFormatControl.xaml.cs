using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace AdvancedCalculator
{
    public sealed partial class KeyboardFormatControl : UserControl
    {
        public KeyboardFormatControl()
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


        private void OnDisplayFormatButton(object sender, RoutedEventArgs e)
        {
            string val = ((sender as Button).Tag as string).Split('|')[1];
            simpleCalculator.DisplaySpecifier = val;
            updateKeyboardDisplays.UpdateKeyboardDisplays();
        }

        private void OnDisplayPrecisionButton(object sender, RoutedEventArgs e)
        {
            string val = ((sender as Button).Tag as string).Split('|')[1];
            simpleCalculator.DisplayPrecision = val;
        }

        private void OnButton(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;
            var value = button.Tag as string;
            if (value == null) return;
            simpleCalculator.DoButton(value);
        }
    }
}
