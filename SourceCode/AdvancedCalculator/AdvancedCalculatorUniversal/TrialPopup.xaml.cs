using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace AdvancedCalculator
{
    public class Tip
    {
        public Tip(string text, string link) { Text = text; Link = link; }
        public string Text;
        public string Link;
    }
    public sealed partial class TrialPopup : UserControl
    {
        public List<Tip> AllTips = null;
        public MainPage theMainPage = null;
        public TrialPopup()
        {
            this.InitializeComponent();
        }

        // Is linked to the OK button.
        private void OnDismissTrial(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Collapsed;
            // theMainPage?.ShowCorrectTrialInfo(); //NOTE: can't ship like this; it lets you show all the tips at startup so they can be checked.
        }

        private async void OnGoto(object sender, RoutedEventArgs e)
        {
            await theMainPage?.JumpTo((sender as Button).Tag as string);
            this.Visibility = Visibility.Collapsed;
        }

        public enum PopupType { NoPopup, FirstFree, FirstBluetooth, PurchaseBluetoothNag, Tip };
        public void ShowPopup (PopupType popupType)
        {
            FrameworkElement title = uiTitle;
            FrameworkElement textblock = uiFirstDay;
            Visibility thisVisibility = Visibility.Visible;
            var buttonText = "Try now!";
            var buttonTag = "";

            switch (popupType)
            {
                default:
                case PopupType.FirstFree:
                    title = uiTitle;
                    textblock = uiFirstDay;
                    buttonText = "Show the manual";
                    buttonTag = "MANUAL";
                    break;
                case PopupType.FirstBluetooth:
                    title = uiTitleBT;
                    textblock = uiFirstDayBT;
                    buttonText = "Show the manual";
                    buttonTag = "MANUAL";
                    break;
                case PopupType.NoPopup:
                    thisVisibility = Visibility.Collapsed;
                    textblock = null;
                    break;
                case PopupType.PurchaseBluetoothNag:
                    title = uiTitleBuyNow;
                    textblock = uiPurchaseBT;
                    buttonText = "Purchase Now!";
                    buttonTag = "uiPurchase";
                    break;
                case PopupType.Tip:
                    title = uiTitle;
                    textblock = uiTipHolder;
                    ShowTip();
                    break;
            }
            this.Visibility = thisVisibility;

            if (popupType != PopupType.Tip) // The tip sets the uiGoThere values
            {
                uiGoThere.Content = buttonText;
                uiGoThere.Tag = buttonTag;
            }

            ShowTitleAndText(title, textblock);
        }

        private void ShowTitleAndText (FrameworkElement title, FrameworkElement textblock)
        {
            uiTitle.Visibility = (title == uiTitle) ? Visibility.Visible : Visibility.Collapsed;
            uiTitleBT.Visibility = (title == uiTitleBT) ? Visibility.Visible : Visibility.Collapsed;
            uiTitleBuyNow.Visibility = (title == uiTitleBuyNow) ? Visibility.Visible : Visibility.Collapsed;

            uiFirstDay.Visibility = (textblock == uiFirstDay) ? Visibility.Visible : Visibility.Collapsed;
            uiFirstDayBT.Visibility = (textblock == uiFirstDayBT) ? Visibility.Visible : Visibility.Collapsed;
            uiPurchaseBT.Visibility = (textblock == uiPurchaseBT) ? Visibility.Visible : Visibility.Collapsed;
            uiTipHolder.Visibility = (textblock == uiTipHolder) ? Visibility.Visible : Visibility.Collapsed;

            var button = uiGoThere;
            if (uiGoThere.Visibility == Visibility.Collapsed) button = uiOk;
            button.Focus(FocusState.Programmatic);
        }


        bool DebugFirstTip = false; //NOTE: ship with this set to FALSE
        Random rand = new Random();
        static string TIPSETTING = "TipIndex";
        static Windows.Storage.ApplicationDataContainer Settings = Windows.Storage.ApplicationData.Current.LocalSettings; // don't roam the tip value
        //int startTipIndex = -1;

        private void ShowTip()
        {
            if (!Settings.Values.ContainsKey(TIPSETTING))
            {
                Settings.Values[TIPSETTING] = -1; // last tip shown
            }

            if (DebugFirstTip) // Just for debugging: set the tips to start at the first tip each time.
            {
                Settings.Values[TIPSETTING] = -1; // last tip shown 
                DebugFirstTip = false;
            }
            var index = ((int)Settings.Values[TIPSETTING] + 1) % AllTips.Count; // Increment and wrap.
            Settings.Values[TIPSETTING] = index;

            if (AllTips[index].Text == "")
            {
                this.Visibility = Visibility.Collapsed;
                return; // we're done with tips!
            }
            SetupTip(AllTips[index].Text, AllTips[index].Link);
        }


        // Sets the text  but does not set the visibility.
        private void SetupTip(string text, string next)
        {
            uiTipHolder.Text = text;
            uiGoThere.Tag = next;

            if (next.StartsWith("http"))
            {
                uiGoThere.Visibility = Visibility.Visible;
                uiGoThere.Content = "Go there";
                uiGoThere.Focus(FocusState.Programmatic);
            }
            else if (next == "") // no next means no button.
            {
                uiGoThere.Visibility = Visibility.Collapsed;
                uiOk.Focus(FocusState.Programmatic);
            }
            else
            {
                uiGoThere.Visibility = Visibility.Visible;
                uiGoThere.Content = "Try it now!";
                uiGoThere.Focus(FocusState.Programmatic);
            }
        }
    }
}
