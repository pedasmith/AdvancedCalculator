using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace AdvancedCalculator
{
    public sealed partial class GamesCoinControl : UserControl, IInitializeDice
    {
        public GamesCoinControl()
        {
            this.InitializeComponent();

        }

        public void Initialize(Dice dice)
        {
            uiMain.Initialize();
            (uiMain.ItemMain as GamesCoinFlipOneControl).dice = dice;
        }
    }
}
