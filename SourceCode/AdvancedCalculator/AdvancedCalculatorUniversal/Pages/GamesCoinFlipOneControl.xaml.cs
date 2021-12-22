using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

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
