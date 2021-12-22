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
    public sealed partial class KeyboardAdvancedNarrowFourPanelControl : UserControl, IInitializeCalculatorAndKeyboard
    {
        public KeyboardAdvancedNarrowFourPanelControl()
        {
            this.InitializeComponent();
        }

        public void Initialize(CalculatorLog log, SimpleCalculator simpleCalculator, IUpdateKeyboardDisplays updateKeyboardDisplays)
        {
            uiMain.Initialize();
            this.Loaded += (s, e) =>
            {
                //var control = (uiMain.ItemMain as Viewbox).Child as KeyboardAdvancedControl;
                //control.Initialize(log, simpleCalculator, updateKeyboardDisplays);
                (uiMain.ItemMain as KeyboardAdvancedControl).Initialize(log, simpleCalculator, updateKeyboardDisplays);
            };
        }
    }
}
