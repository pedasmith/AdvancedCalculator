using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace AdvancedCalculator
{
    public sealed partial class KeyboardAdvancedNarrowFourPanelControl : UserControl, IInitializeCalculatorAndKeyboard
    {
        public KeyboardAdvancedNarrowFourPanelControl()
        {
            this.InitializeComponent();
        }

        public void Initialize(CalculatorLog log, SimpleCalculator simpleCalculator, IUpdateKeyboardDisplays updateKeyboardDisplays)
        {
            uiMain.Initialize();
            this.Loaded += (s, e) =>
            {
                //var control = (uiMain.ItemMain as Viewbox).Child as KeyboardAdvancedControl;
                //control.Initialize(log, simpleCalculator, updateKeyboardDisplays);
                (uiMain.ItemMain as KeyboardAdvancedControl).Initialize(log, simpleCalculator, updateKeyboardDisplays);
            };
        }
    }
}
