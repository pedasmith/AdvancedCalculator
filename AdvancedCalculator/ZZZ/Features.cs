// Provides a list of the features that have been purchased.  The
// source-code feature and the copy-data feature are considered to be
// different even though there's only one thing to buy (the all-access item)
// This one is shared between Best Calculator and Best Calculator Bluetooth
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Store;


namespace Shipwreck
{
    class Features
    {
        static bool debug = true; // NOTE: This statement MUST be false for shipping!
        static string strFullAccess = "FullAccess"; // strFullAccess is the name of the in-app purchase as described in the store.

        public enum TrialState { InTrial, OverTrial, Paid };
        public static TrialState CurrTrialState { get; set; } = TrialState.Paid; // default to safe

        //
        // Variables etc to handle trial period
        //
        private static int TrialTimeInSeconds = 30 * 24 * 60 * 60; // 30 days, 24 hours/day, 60 minutes/hour, 60 seconds/minute
        static Windows.Storage.ApplicationDataContainer Settings = Windows.Storage.ApplicationData.Current.LocalSettings; // don't roam the first use value.
        static string FIRSTUSETIME = "FirstUseTime"; // Note: while debugging also used  FirstUseTimeTestA


        static LicenseInformation license;
        static bool didInit = false;

        public delegate void PaidChangedEventHandler();
        public static event PaidChangedEventHandler PaidChanged;
        public static void InvokePaidChanged()
        {
            if (PaidChanged != null)
            {
                PaidChanged.Invoke();
            }
        }

        static DateTimeOffset FirstRun;
        public static double DaysSinceFirstRun()
        {
            Init();
            var delta = DateTimeOffset.UtcNow - FirstRun;
            var Retval = delta.TotalDays;
            if (Retval < 0) Retval = 0;
            if (Retval > 5000) Retval = 5000; // no point in going more since the max I'll check is like 30 days.
            // This is to handle weirdo cases like "the computer time got completely reset"

            return Retval;
        }

        public static async Task PurchaseFullAccess()
        {
            //PurchaseResults result ;
            ProductPurchaseStatus status;
            try
            {
                if (debug)
                {
                    var result = await CurrentAppSimulator.RequestProductPurchaseAsync(strFullAccess);
                    status = result.Status;
                }
                else
                {
                    var result = await CurrentApp.RequestProductPurchaseAsync(strFullAccess);
                    status = result.Status;
                }
            }
            catch (Exception)
            {
                status = ProductPurchaseStatus.NotFulfilled;
            }
            switch (status)
            {
                case ProductPurchaseStatus.AlreadyPurchased:
                case ProductPurchaseStatus.Succeeded:
                    await Messages.PopupThanksForPurchase();
                    InitAccessNow();
                    break;
                case ProductPurchaseStatus.NotFulfilled:
                case ProductPurchaseStatus.NotPurchased:
                    break;
            }
            Features.InvokePaidChanged();

        }

        public static void Init()
        {
            if (didInit) return;
            didInit = true;
            bool FullAccessIsActive = false;
            try
            {
                // The store is wonky; never let it fail.
                // If it throws, assume the user has access (but set a flag, so it can be detected)
                license = debug ? CurrentAppSimulator.LicenseInformation : CurrentApp.LicenseInformation;
                FullAccessIsActive = license.ProductLicenses[strFullAccess].IsActive;


                FullAccessIsActive = true; // // // TODO: must not be present for shipping!
            }
            catch (Exception ex)
            {
                AdvancedCalculator.BCBasic.RunTimeLibrary.RTLSystemX.AddError($"Exception while contacting store is {ex.Message}");
                FullAccessIsActive = true;
                // NOTE: on shipping, default is TRUE not FALSE
                // that way people who purchase but run into exception are covered.
            }

            // Init to default-false
            HasSaveToClipboard = false;
            HasSourceCode = false;
            HasNoAds = false;
            HasAnyPurchases = false;

            TrialState newTrialState = TrialState.Paid;
            var now = DateTimeOffset.UtcNow; // Do everything in UTC?
            if (Settings.Values.ContainsKey(FIRSTUSETIME) && Settings.Values[FIRSTUSETIME] is DateTimeOffset)
                FirstRun = (DateTimeOffset)Settings.Values[FIRSTUSETIME];
            else FirstRun = DateTimeOffset.MinValue;

            if (FirstRun == DateTimeOffset.MinValue)
            {
                Settings.Values[FIRSTUSETIME] = now;
                FirstRun = now;
                IsFirstRun = true;
            }
            var delta = (now - FirstRun).TotalSeconds;


            if (FullAccessIsActive)
            {
                newTrialState = TrialState.Paid;
                HaveAll();
            }
            else if (delta < TrialTimeInSeconds)
            {
                HaveAll();
                newTrialState = TrialState.InTrial;
            }
            else
            {
                newTrialState = TrialState.OverTrial;
            }
            CurrTrialState = newTrialState;
            SetHaveAny();
        }

        private static void HaveAll()
        {
            HasBluetooth = true;
            HasFileSystemAccess = true;
            HasSourceCode = true;
            HasSaveToClipboard = true;
            HasNoAds = true;
            SetHaveAny();
        }

        private static void SetHaveAny()
        {
            if (HasSourceCode || HasSaveToClipboard || HasNoAds || HasBluetooth || HasFileSystemAccess)
            {
                HasAnyPurchases = true;
            }
        }

        // Used both when the user pays for something
        // and when the user enters a code.
        public static void InitAccessNow()
        {
            HaveAll();
            HasAnyPurchases = true;
            Features.InvokePaidChanged();
        }


        public static bool IsDebug { get { return debug; } }
        public static bool HasAnyPurchases { get; internal set; }
        public static bool IsFirstRun { get; internal set; } = false;

        // Add new values in alphabetical order and update HaveAll and SetHaveAny.
        public static bool HasBluetooth { get; internal set; }
        public static bool HasFileSystemAccess { get; internal set; }
        public static bool HasNoAds { get; internal set; }
        public static bool HasSaveToClipboard { get; internal set; }
        public static bool HasSourceCode { get; internal set; }
    }
}
