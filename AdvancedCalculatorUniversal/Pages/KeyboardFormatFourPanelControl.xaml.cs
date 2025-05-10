using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace AdvancedCalculator
{
    public sealed partial class KeyboardFormatFourPanelControl : UserControl, IInitializeCalculatorAndKeyboard
    {
        public KeyboardFormatFourPanelControl()
        {
            this.InitializeComponent();
        }

        public void Initialize(CalculatorLog log, SimpleCalculator simpleCalculator, IUpdateKeyboardDisplays updateKeyboardDisplays)
        {
            uiMain.Initialize();
            this.Loaded += (s,e) => {
                (uiMain.ItemMain as KeyboardFormatControl).Initialize(log, simpleCalculator, updateKeyboardDisplays);
            };
        }
    }
}
