using AdvancedCalculator.Pages;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace AdvancedCalculator
{
    public sealed partial class StringConversionControl : IInitializeCalculator
    {
        public StringConversionControl()
        {
            this.InitializeComponent();
        }

        public void Initialize(SimpleCalculator simpleCalculator)
        {
            uiMain.Initialize(); // set up the FourPanel display
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateText();
        }
        private void OnConversionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateText();
        }

        private void UpdateText()
        {
            // Happens at initialization time
            if (uiInputType == null) return;
            if (uiOutputType == null) return;
            if (uiTo == null) return;

            var txt = uiFrom.Text;
            var inputMethod = (uiInputType.SelectedItem as ComboBoxItem)?.Tag as string;
            var outputMethod = (uiOutputType?.SelectedItem as ComboBoxItem)?.Tag as string;
            var bytes = new List<byte>();
            string errstr = StringConvert.ConvertInput(inputMethod, txt, ref bytes);


            string tostr = "";
            if (errstr == "")
            {
                tostr = StringConvert.ConvertOutput(outputMethod, bytes);
                if (String.IsNullOrEmpty(tostr))
                {
                    errstr = "(No conversion results)";
                }
            }

            uiTo.Text = (errstr == "") ? tostr : errstr;
        }

    }
}
