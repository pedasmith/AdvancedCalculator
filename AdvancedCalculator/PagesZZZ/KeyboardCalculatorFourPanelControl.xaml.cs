using BCBasic;
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
    public sealed partial class KeyboardCalculatorFourPanelControl : UserControl, IInitializeCalculatorAndKeyboardAndButtonList
    {
        public KeyboardCalculatorFourPanelControl()
        {
            this.InitializeComponent();
        }

        public void Initialize(CalculatorLog log, SimpleCalculator simpleCalculator, IUpdateKeyboardDisplays updateKeyboardDisplays, ICalculatorConnection cc)
        {
            uiMain.Initialize();
            this.Loaded += (s, e) =>
            {
                //var vb = (uiMain.ItemMain as Viewbox);
                //var kcc = (vb.Child as KeyboardCalculatorControl);
                //var p = uiMain.ItemMain as Grid;
                //var kcc = p.Children[0] as KeyboardCalculatorControl;
                var kcc = uiMain.ItemMain as KeyboardCalculatorControl;
                (kcc).Initialize(log, simpleCalculator, updateKeyboardDisplays, cc);
            };
        }
    }
}
