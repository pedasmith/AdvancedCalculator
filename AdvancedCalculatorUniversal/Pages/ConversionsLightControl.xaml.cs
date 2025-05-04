using NetworkToolkit;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace AdvancedCalculator
{
    public sealed partial class ConversionsLightControl : UserControl, IInitializeCalculator
    {
        public ConversionsLightControl()
        {
            this.InitializeComponent();
        }

        public void Initialize(SimpleCalculator simpleCalculator)
        {
            this.simpleCalculator = simpleCalculator;
            uiMain.Initialize();
            Loaded += (s, e) =>
            {
                solver = new SolverWPFMetro(new LumensPerWattSolver(), uiMain.ItemMain as Grid);
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
