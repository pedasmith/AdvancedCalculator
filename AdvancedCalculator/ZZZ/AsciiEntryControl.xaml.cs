using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public sealed partial class AsciiEntryControl : UserControl
    {
        public AsciiEntryControl()
        {
            this.InitializeComponent();
        }

        public AsciiEntryControl(char value)
        {
            this.InitializeComponent();
            Set(value);
        }

        public void Set(char value)
        {
            int val = Convert.ToInt32(value);

            if (val < 32)
            {
                int unicode_version = val + 0x2400;
                uiChar.Text = Convert.ToString (Convert.ToChar(unicode_version));
            }
            else if (val == 127)
            {
                int unicode_version = 0x2421;
                uiChar.Text = Convert.ToString(Convert.ToChar(unicode_version));
            }
            else
            {
                var str = Convert.ToString(value);
                uiChar.Text = str;
            }

            uiDec.Text = Convert.ToString(val,10);
            uiHex.Text = Convert.ToString(val, 16);
            if (val < 256) // Octal
            {
                uiOther.Text = Convert.ToString(val, 8);
            }
            else // Unicode style
            {
                uiOther.Text = "U+" + Convert.ToString(val, 16);
            }
        }

    }
}
