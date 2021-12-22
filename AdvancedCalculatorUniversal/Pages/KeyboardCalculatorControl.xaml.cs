using BCBasic;
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
    public sealed partial class KeyboardCalculatorControl : UserControl, IInitializeCalculatorAndKeyboardAndButtonList
    {
        public KeyboardCalculatorControl()
        {
            this.InitializeComponent();
            this.SizeChanged += (s, e) =>
            {
                uiGrid.Measure(new Size(Double.MaxValue, Double.MaxValue));
                var h = uiGrid.DesiredSize.Height;
                this.Height = h;
            };
            this.Loaded += async (s, e) =>
            {
                if (BCButtonList != null)
                {
                    await UpdateButtonsAsync();
                    BCButtonList.ButtonList.CollectionChanged += async (ss, ee) =>
                    {
                        await UpdateButtonsAsync();
                    };
                    BCButtonList.OnUpdate += async (ss) => { await UpdateButtonsAsync(); };
                }
            };
        }

        private async Task UpdateButtonsAsync()
        {
            if (BCButtonList == null) return;

            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
           {
               UpdateButtons();
           });
        }

        private void UpdateButtons()
        {
            if (BCButtonList == null) return;
            foreach (var child in uiGrid.Children)
            {
                var b = child as Button;
                if (b == null) continue;
                var tag = b.Tag as string;
                if (tag == null) continue;
                if (tag.StartsWith("#KEYprog|bP"))
                {
                    var buttonName = tag.Substring(10);
                    foreach (var button in BCButtonList.ButtonList)
                    {
                        if (button.Button == buttonName)
                        {
                            if (b.Content is Viewbox)
                            {
                                var vb = b.Content as Viewbox;
                                var tb = vb.Child as TextBlock;
                                if (tb != null) tb.Text = button.KeyName;
                                if (button.KeyName.Length < 6)
                                {
                                    vb.Stretch = Stretch.None;
                                }
                                else
                                {
                                    vb.Stretch = Stretch.Uniform;
                                }
                            }
                            else
                            {
                                b.Content = button.KeyName;
                            }
                        }
                    }
                }
            }
        }

        private ButtonToProgramList BCButtonList { get; set; } // wires up the buttons list to actual button names
        CalculatorLog Log = null;
        SimpleCalculator simpleCalculator = null;
        IUpdateKeyboardDisplays updateKeyboardDisplays = null;
        public void Initialize(CalculatorLog log, SimpleCalculator simpleCalculator, IUpdateKeyboardDisplays updateKeyboardDisplays, ICalculatorConnection cc)
        {
            this.Log = log;
            this.simpleCalculator = simpleCalculator;
            this.updateKeyboardDisplays = updateKeyboardDisplays;
            this.BCButtonList = cc.GetButtonToProgramList();
            BCButtonList.OnUpdate += async (ss) => { await UpdateButtonsAsync(); };
            UpdateButtons();
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

        /*
        private void OnProg(object sender, RoutedEventArgs e)
        {
            if (uiCalculatorConnection != null) uiCalculatorConnection.DoProgramButton();
        }
         */
    }
}
