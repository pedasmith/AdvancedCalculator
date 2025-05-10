using BCBasic;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace AdvancedCalculator
{
    public sealed partial class KeyboardAdvancedWideFourPanelControl : UserControl, IInitializeCalculatorAndKeyboardAndButtonList
    {
        public KeyboardAdvancedWideFourPanelControl()
        {
            this.InitializeComponent();
        }

        public void Initialize(CalculatorLog log, SimpleCalculator simpleCalculator, IUpdateKeyboardDisplays updateKeyboardDisplays, ICalculatorConnection cc)
        {
            uiMain.Initialize();
            this.Loaded += (s, e) =>
            {
                var g = uiMain.ItemMain as Grid;
                foreach (var child in g.Children)
                {
                    if (child is KeyboardAdvancedControl)
                        (child as KeyboardAdvancedControl).Initialize(log, simpleCalculator, updateKeyboardDisplays);
                    else if (child is KeyboardCalculatorControl)
                        (child as KeyboardCalculatorControl).Initialize(log, simpleCalculator, updateKeyboardDisplays, cc);
                }
            };
        }
    }
}
