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
    public sealed partial class KeyboardAdvancedWideFourPanelControl : UserControl, IInitializeCalculatorAndKeyboardAndButtonList
    {
        public KeyboardAdvancedWideFourPanelControl()
        {
            this.InitializeComponent();
        }

        public void Initialize(CalculatorLog log, SimpleCalculator simpleCalculator, IUpdateKeyboardDisplays updateKeyboardDisplays, ICalculatorConnection cc)
        {
            uiMain.Initialize();
            this.Loaded += (s, e) =>
            {
                var g = uiMain.ItemMain as Grid;
                foreach (var child in g.Children)
                {
                    if (child is KeyboardAdvancedControl)
                        (child as KeyboardAdvancedControl).Initialize(log, simpleCalculator, updateKeyboardDisplays);
                    else if (child is KeyboardCalculatorControl)
                        (child as KeyboardCalculatorControl).Initialize(log, simpleCalculator, updateKeyboardDisplays, cc);
                }
            };
        }
    }
}
