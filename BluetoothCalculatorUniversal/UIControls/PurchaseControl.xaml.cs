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

namespace Shipwreck.UIControls
{
    public sealed partial class PurchaseControl : UserControl
    {
        public PurchaseControl()
        {
            this.InitializeComponent();
            Features.Init();  // Might not be initialized yet if the purchase screen is the first screen.
            Init();
            Features.PaidChanged += Init;
        }



        public void Init()
        {
            bool isPaid  = Features.CurrTrialState == Features.TrialState.Paid;
            foreach (var child in uiMain.Children)
            {
                var fe = child as FrameworkElement;
                if (fe == null) continue;
                var tag = (fe.Tag) as string;
                if (tag == null) continue;

                bool vis = true;
                switch (tag)
                {
                    case "free":
                        if (isPaid) vis = false;
                        break;
                    case "paid":
                        if (!isPaid) vis = false;
                        break;
                }
                fe.Visibility = vis ? Visibility.Visible : Visibility.Collapsed;
            }

        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Init();
        }

        private async void OnPurchaseNow(object sender, RoutedEventArgs e)
        {
            await Features.PurchaseFullAccess();
            // Init(); // Need to re-initialize the UI -- will be called via the callback.
        }

        private async void OnTryAccessCode (object sender, RoutedEventArgs e)
        {
            var code = uiAccessCode.Text;
            switch (code)
            {
                /*
                case "redact":
                    App.TheApp.Redact = true;
                    await Messages.PopupRedactedAsync();
                    break;
                case "codegen":
                    MainPage.ShowCodeGenerator();
                    break;
                */

                case "beta":
                    Features.InitAccessNow();
                    await Messages.PopupAccessCodeAcceptedAsync();
                    //Init(); // do not need to call Init(); Init will be called when the feature changes
                    break;
            }
        }

    }
}
