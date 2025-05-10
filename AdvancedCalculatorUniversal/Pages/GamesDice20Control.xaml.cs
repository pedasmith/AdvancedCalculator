using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace AdvancedCalculator
{
    public sealed partial class GamesDice20Control : UserControl, IInitializeDice
    {
        public GamesDice20Control()
        {
            this.InitializeComponent();
        }

        public void Initialize(Dice dice)
        {
            uiMain.Initialize();
            this.dice = dice;
        }

        private Dice dice;
        private void onGamesDiceRoll(object sender, RoutedEventArgs e)
        {
            dice.onGamesDiceRoll(sender, e);
        }
    }
}
