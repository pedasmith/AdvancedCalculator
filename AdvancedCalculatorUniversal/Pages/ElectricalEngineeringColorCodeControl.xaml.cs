using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace AdvancedCalculator
{
    public sealed partial class ElectricalEngineeringColorCodeControl : UserControl, IInitializeCalculator
    {
        public ElectricalEngineeringColorCodeControl()
        {
            this.InitializeComponent();
        }

        public void Initialize(SimpleCalculator simpleCalculator)
        {
            uiMain.Initialize();
        }
    }
}
