using BCBasic;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace AdvancedCalculator
{
    public sealed partial class KeyboardCalculatorFourPanelControl : UserControl, IInitializeCalculatorAndKeyboardAndButtonList
    {
        public KeyboardCalculatorFourPanelControl()
        {
            this.InitializeComponent();
        }

        public void Initialize(CalculatorLog log, SimpleCalculator simpleCalculator, IUpdateKeyboardDisplays updateKeyboardDisplays, ICalculatorConnection cc)
        {
            uiMain.Initialize();
            this.Loaded += (s, e) =>
            {
                //var vb = (uiMain.ItemMain as Viewbox);
                //var kcc = (vb.Child as KeyboardCalculatorControl);
                //var p = uiMain.ItemMain as Grid;
                //var kcc = p.Children[0] as KeyboardCalculatorControl;
                var kcc = uiMain.ItemMain as KeyboardCalculatorControl;
                (kcc).Initialize(log, simpleCalculator, updateKeyboardDisplays, cc);
            };
        }
    }
}
