using Shipwreck.Utilities;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace AdvancedCalculator
{
    public sealed partial class FeedbackFourPanelControl : UserControl, IInitializeAppDetails
    {
        public FeedbackFourPanelControl()
        {
            this.InitializeComponent();
        }
        public void Initialize(IGetAppDetails details)
        {
            uiMain.Initialize();
            FeedbackControl fbc = null;
            if (uiMain.ItemMain is Border)
            {
                fbc = (uiMain.ItemMain as Border).Child as Shipwreck.Utilities.FeedbackControl;
            }
            else
            {
                fbc = uiMain.ItemMain as Shipwreck.Utilities.FeedbackControl;
            }
            fbc.GetAppDetails = details;
        }
    }
}
