using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
    public sealed partial class GamesDice8Control : UserControl, IInitializeDice
    {
        public GamesDice8Control()
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
