using System;
using System.Threading.Tasks;
using Windows.UI.Popups;

namespace Shipwreck
{
    public class Messages
    {
        public static async Task PopupAccessCodeAcceptedAsync()
        {
            var md = new MessageDialog("The access code has been accepted.", "Thanks!");
            await md.ShowAsync();
        }

        public static async Task PopupThanksForPurchase()
        {
            var md = new MessageDialog("Thanks for your purchase.  All features are now enabled.", "Thank you");
            await md.ShowAsync();
        }

        public static async Task PopupCantCopyDataToClipboardAsync()
        {
            var md = new MessageDialog("You must purchase the FullAccess feature to copy to clipboard.  Go to the Purchase tab to buy this feature.", "Copy to clipboard is blocked");
            await md.ShowAsync();
        }

        public static async Task PopupCantCopySourceCodeToClipboardAsync()
        {
            var md = new MessageDialog("You must purchase the FullAccess feature to copy to clipboard.  Go to the Purchase tab to buy this feature.", "Copy to clipboard is blocked");
            await md.ShowAsync();
        }
        public static async Task PopupCantSaveAsSourceCodeAsync()
        {
            var md = new MessageDialog("You must purchase the FullAccess feature to save source code.  Go to the Purchase tab to buy this feature.", "Save As is blocked");
            await md.ShowAsync();
        }

        public static async Task PopupSaveAsNoFileSpecified()
        {
            var md = new MessageDialog("No file to save to was selected.");
            await md.ShowAsync();
        }

        public static async Task PopupNoAddressWasSpecified()
        {
            var md = new MessageDialog("No address to scan was specified (e.g., http://www.microsoft.com)");
            await md.ShowAsync();
        }

        public static async Task PopupNoServersFound()
        {
            var md = new MessageDialog("No HTTP servers were found.");
            await md.ShowAsync();
        }
        public static async Task PopupRedactedAsync()
        {
            var md = new MessageDialog("Redact mode is now on", "Shhh!");
            await md.ShowAsync();
        }

        public static async Task PopupScanCompleted()
        {
            var md = new MessageDialog("Scan completed.");
            await md.ShowAsync();
        }
    }
}
