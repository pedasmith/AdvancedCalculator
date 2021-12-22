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
    public sealed partial class KeyboardProgrammerControl : UserControl
    {
        public KeyboardProgrammerControl()
        {
            this.InitializeComponent();
        }

        CalculatorLog Log = null;
        SimpleCalculator simpleCalculator = null;
        IUpdateKeyboardDisplays updateKeyboardDisplays = null;
        public void Initialize(CalculatorLog log, SimpleCalculator simpleCalculator, IUpdateKeyboardDisplays updateKeyboardDisplays)
        {
            this.Log = log;
            this.simpleCalculator = simpleCalculator;
            this.updateKeyboardDisplays = updateKeyboardDisplays;
        }


        private void OnButton(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;
            var value = button.Tag as string;
            if (value == null) return;
            // Don't log personal data: 
            //Log.WriteWithTime("Button: " + value + "\r\n");
            simpleCalculator.DoButton(value);
            if (!value.StartsWith("#KEY"))
            {
                Log.WriteWithTime("ERROR: invalid key sequence  " + value);
            }

            this.Focus(Windows.UI.Xaml.FocusState.Programmatic);
        }

        private void OnDisplayFormatButton(object sender, RoutedEventArgs e)
        {
            string val = ((sender as Button).Tag as string).Split('|')[1];
            simpleCalculator.DisplaySpecifier = val;
            updateKeyboardDisplays.UpdateKeyboardDisplays();
        }

        private void OnDisplayPrecisionButton(object sender, RoutedEventArgs e)
        {
            string val = ((sender as Button).Tag as string).Split('|')[1];
            simpleCalculator.DisplayPrecision = val;
        }

        private void OnDisplaySizeButton(object sender, RoutedEventArgs e)
        {
            string val = ((sender as Button).Tag as string).Split('|')[1];
            simpleCalculator.DisplaySize = val;
        }

    }
}
