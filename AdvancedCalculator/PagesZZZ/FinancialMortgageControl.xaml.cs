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
    public sealed partial class FinancialMortgageControl : UserControl, IInitializeCalculator
    {
        public FinancialMortgageControl()
        {
            this.InitializeComponent();
        }

        public void Initialize(SimpleCalculator simpleCalculator)
        {
            this.simpleCalculator = simpleCalculator;
            uiMain.Initialize();
            Loaded += (s, e) =>
            {
                solver = new SolverWPFMetro(new FinancialMortgageSolver(), uiMain.ItemMain as Grid);
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
