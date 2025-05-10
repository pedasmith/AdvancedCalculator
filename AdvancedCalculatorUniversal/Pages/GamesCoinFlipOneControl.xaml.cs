using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace AdvancedCalculator
{
    public sealed partial class GamesCoinFlipOneControl : UserControl
    {
        public GamesCoinFlipOneControl()
        {
            this.InitializeComponent();
        }

        public Dice dice;
        private async void onGamesDiceRoll(object sender, RoutedEventArgs e)
        {
            var tag = (sender as Button).Tag as string;
            if (tag == "1|2|coin")
            {
                uiAnimationFlip.Begin();
                await Task.Delay(300);
                dice.onGamesDiceRoll(sender, e);
            }
            else
            {
                dice.onGamesDiceRoll(sender, e);
            }

        }
    }
}
