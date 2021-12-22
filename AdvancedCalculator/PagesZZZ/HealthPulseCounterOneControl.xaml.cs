using NetworkToolkit;
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
    public sealed partial class HealthPulseCounterOneControl : UserControl
    {
        public HealthPulseCounterOneControl()
        {
            this.DataContext = this;
            this.InitializeComponent();
            healthPulseCounter = new Solvers.TimeCounter();
        }

        public SimpleCalculator simpleCalculator { get; set; }
        private SolverWPFMetro solver = null; // stays null!
        public void OnFromCalc(object sender, RoutedEventArgs e)
        {
            MainPage.DoFromCalc(sender as Button, simpleCalculator, solver);
        }

        public void OnToCalc(object sender, RoutedEventArgs e)
        {
            MainPage.DoToCalc(sender as Button, simpleCalculator);
        }

        public Solvers.TimeCounter healthPulseCounter { get; set; }

        private void OnHealthPulseCounterClear(object sender, RoutedEventArgs e)
        {
            healthPulseCounter.Init();
        }
        private void OnHealthPulseCounterTap(object sender, RoutedEventArgs e)
        {
            healthPulseCounter.IncrementMinutes(); // I care about beats/minute
        }

    }
}
