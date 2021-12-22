using System;
using Windows.Foundation.Metadata;
using Windows.System;
using Windows.UI.ApplicationSettings;

// Modified (slightly) from http://mtaulty.com/CommunityServer/blogs/mike_taultys_blog/archive/2013/03/11/windows-store-xaml-app-adding-a-privacy-page.aspx
namespace Utilities
{
    internal static class PrivacyPolicy
    {
        // ID is e.g. PRIVACY_ID
        // UserName is e.g. "Privacy"

        /* In the project, add a reference to Universal WindowsExtensions Windows Desktop Extensions for the UWP.  
         * If you don’t do this, then the SettingsPane that’s used in PrivacyPolicy.cs won’t show up correctly.
        */
        public static void Initialise(string ID, string UserDisplayString, Uri settingsUri)
        {
            if (ApiInformation.IsApiContractPresent("Windows.UI.ApplicationSettings.ApplicationsSettingsContract", 1, 0))
            {
#pragma warning disable 618
                SettingsPane settingsPane = SettingsPane.GetForCurrentView();

                settingsPane.CommandsRequested += (s, e) =>
                {
                    SettingsCommand settingsCommand = new SettingsCommand(
                      ID, // NB: should be in a constant
                      UserDisplayString,    // NB: should be in a resource for internationalisation
                      command =>
                      {
                          // If we don't set the variable, c# complains that we aren't awaiting the result.
                          var t = Launcher.LaunchUriAsync(settingsUri);
                      }
                    );
                    e.Request.ApplicationCommands.Add(settingsCommand);
                };
            }
        }
    }
}