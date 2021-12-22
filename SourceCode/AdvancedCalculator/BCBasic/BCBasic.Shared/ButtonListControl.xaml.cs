using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace BCBasic
{
    public sealed partial class ButtonListControl : UserControl
    {
        // Requires these in the DataContext:
        //  public BCLibrary Library { get; internal set; }
        //  public ButtonToProgramList ButtonList {get; internal set; }
        //  public BCPackage CurrentPackage


        public ButtonListControl()
        {
            this.InitializeComponent();
        }

        private void OnSelectedProgramChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 1) return;
            var program = e.AddedItems[0] as BCProgram;
            if (program == null) return;
            var keyName = program.KeyName;
            uiKeyName.Text = keyName;
        }


        private async void OnSave(object sender, RoutedEventArgs e)
        {
            await DoSave(sender as FrameworkElement);
        }

        private async void OnSaveT(object sender, TappedRoutedEventArgs e)
        {
            await DoSave(sender as FrameworkElement);
        }

        private async Task DoSave(FrameworkElement sender)
        {
            var button = uiButtonList.SelectedItem as ButtonToProgram;
            var package = uiPackageList.SelectedItem as BCPackage;
            var program = uiProgramList.SelectedItem as BCProgram;
            var buttonlist = (sender as FrameworkElement).Tag as ButtonToProgramList;

            if (buttonlist != null && button != null && package != null && program != null)
            {
                buttonlist.Set(button.Button, package.Name, program.Name, uiKeyName.Text);
                await buttonlist.SaveAsync();
            }
            else
            {
                if (button == null)
                {
                    var d = new MessageDialog("Please select a button to set", "Unable to set button");
                    await d.ShowAsync();
                }
                else if (package == null)
                {
                    var d = new MessageDialog("Please select a package and program to set", "Unable to set button");
                    await d.ShowAsync();
                }
                else if (program == null)
                {
                    var d = new MessageDialog("Please select a program to set", "Unable to set button");
                    await d.ShowAsync();
                }

            }
        }
    }
}
