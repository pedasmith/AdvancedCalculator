using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Shipwreck.Utilities
{
    public interface IGetAppDetails
    {
        string GetAppDetails();
    }

    public interface IHideFlyout
    {
        void DoHideFlyout();
    }

    public sealed partial class FeedbackControl : UserControl
    {
        public string Product { get; set; }
        public IGetAppDetails GetAppDetails = null;
        public IHideFlyout HideFlyout = null;
        public FeedbackControl()
        {
            Product = "MyProduct";
            this.InitializeComponent();
            this.Loaded += (s, e) =>
            {
                // The second time we're up, we need to switch the visibility back.
                uiFeedbackSwitcher.Visibility = Visibility.Visible;
            };
        }
        NetworkingFeedback feedback = new NetworkingFeedback();

        bool amSending = false;
        private async void OnSendFeedbackButton(object sender, RoutedEventArgs e)
        {
            if (amSending)
            {
                uiFeedbackStatus.Text = "...still sending...";
                return;
            }
            amSending = true;
            var subject = uiFeedbackSubject.Text;
            if (subject == "") subject = "no subject";
            var appDetails = "";
            if (GetAppDetails != null) appDetails = GetAppDetails.GetAppDetails();
            var body = "Like:" + uiFeedbackLike.Text + "\n\nDislike:" + uiFeedbackDislike.Text;

            var t = feedback.SendFeedback(Product, subject, body, appDetails, uiFeedbackStatus);
            uiFeedbackStatus.Text += "!!";
            var startText = uiFeedbackStatus.Text;

            while (uiFeedbackStatus.Text == startText) // Yes, this is a little grody :-)
            {
                await Task.Delay(1000);
            }

            await Task.Delay(4000); ; // wait 4 more seconds
            if (HideFlyout != null) HideFlyout.DoHideFlyout();
            amSending = false;
            // We don't actually need to await here: await t;
        }

        private void OnStartReview(object sender, RoutedEventArgs e)
        {
            var package = Windows.ApplicationModel.Package.Current.Id.FamilyName;
            var t = Windows.System.Launcher.LaunchUriAsync(new Uri("ms-windows-store:REVIEW?PFN=" + package));
            // set the var t so that the compiler doesn't complain about not awaiting the task.

        }

        private void OnStartFeedback(object sender, RoutedEventArgs e)
        {
            uiFeedbackSwitcher.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

    }
}
