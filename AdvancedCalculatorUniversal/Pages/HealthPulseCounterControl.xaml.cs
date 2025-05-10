using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace AdvancedCalculator
{
    public sealed partial class HealthPulseCounterControl : UserControl, IInitializeCalculator
    {
        public HealthPulseCounterControl()
        {
            this.InitializeComponent();
        }

        public void Initialize(SimpleCalculator simpleCalculator)
        {
            uiMain.Initialize();
            Loaded += (s, e) =>
            {
                (uiMain.ItemMain as HealthPulseCounterOneControl).simpleCalculator = simpleCalculator;
            };
        }
        private SimpleCalculator simpleCalculator { get; set; }
    }
}
