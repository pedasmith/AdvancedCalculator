using NetworkToolkit;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace AdvancedCalculator
{
    public sealed partial class HealthBMIAdultControl : UserControl, IInitializeCalculator
    {
        public HealthBMIAdultControl()
        {
            this.InitializeComponent();
        }

        public void Initialize(SimpleCalculator simpleCalculator)
        {
            this.simpleCalculator = simpleCalculator;
            uiMain.Initialize();
            Loaded += (s, e) =>
            {
                solver = new SolverWPFMetro(new HealthBMISolver(), uiMain.ItemMain as Grid);
            };
        }
        private SimpleCalculator simpleCalculator { get; set; }
        SolverWPFMetro solver;

        public void OnFromCalc(object sender, RoutedEventArgs e)
        {
            MainPage.DoFromCalc(sender as Button, simpleCalculator, solver);
        }

        public void OnToCalc(object sender, RoutedEventArgs e)
        {
            MainPage.DoToCalc(sender as Button, simpleCalculator);
        }
    }
}
