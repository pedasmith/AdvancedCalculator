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

using System.Reflection;
using System.Diagnostics;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238
using System.ComponentModel;
using NetworkToolkit;
using Windows.UI.ViewManagement;
using System.Windows.Input;
using Windows.UI.Xaml.Documents;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using System.Collections.ObjectModel;
using Shipwreck.Utilities;
using BCBasic;
using System.Runtime.CompilerServices;
using Windows.UI.Popups;
using Windows.Storage;
using Shipwreck;
using System.Threading;

namespace AdvancedCalculator
{
    public interface IShare
    {
        string ShareTitle { get; set; }
        string ShareString { get; set; }
    }
    public interface IDoAppearance
    {
        void DismissAppearancePopup();
        String ColorScheme { get; set; }
        void SetColor(string tag, Windows.UI.Color color);
        Windows.UI.Color GetColor(string tag);
        void SetFont(string value);
        string GetFont();
    }
    public interface IUpdateKeyboardDisplays
    {
        void UpdateKeyboardDisplays();
    }
    public interface IInitializeCalculator
    {
        void Initialize(SimpleCalculator simpleCalculator);
    }
    public interface IInitializeDice
    {
        void Initialize(Dice dice);
    }
    public interface IInitializeCalculatorAndKeyboard
    {
        void Initialize(CalculatorLog log, SimpleCalculator simpleCalculator, IUpdateKeyboardDisplays updateKeyboardDisplays);
    }
    public interface IInitializeCalculatorAndKeyboardAndButtonList
    {
        void Initialize(CalculatorLog log, SimpleCalculator simpleCalculator, IUpdateKeyboardDisplays updateKeyboardDisplays, ICalculatorConnection calculatorConnection);
    }
    public interface IInitializeMemory
    {
        void Inialize(IMemoryButtonHandler mbh, object SimpleCalculatorParent);
    }
    public interface IInitializeShare
    {
        void Initialize(IShare share);
    }
    public interface IInitializeAppDetails
    {
        void Initialize(IGetAppDetails details);
    }


    public sealed partial class MainPage : Page, INotifyPropertyChanged, IMemoryButtonHandler, IShare, IDoAppearance, IGetAppDetails, ICalculator, IObjectValue, IDoBack, IDoStopProgram
    {
        Dictionary<String, SolverWPFMetro> SolversList = new Dictionary<String, SolverWPFMetro>();
        //public ObservableCollection<WordDefinition> WordDefinitions { get; set; }
        CalculatorLog Log = new CalculatorLog();
        public Background MainBackground { get; set; }

        public MainPage()
        {
            try
            {
                Log.WriteWithTime("MainPage: called\r\n");
                (App.Current as App).mainPage = this;

                Log.WriteWithTime("MainPage: doing: new SimpleCalculator\r\n");
                simpleCalculator = new SimpleCalculator();
                simpleCalculator.Share = this;

                //WordDefinitions = new ObservableCollection<WordDefinition>();

                cbc.SetColorScheme(CalculatorButtonColors.ColorScheme.Colorful);

                Log.WriteWithTime("MainPage: doing: component\r\n");
                this.InitializeComponent();

                // See https://social.msdn.microsoft.com/Forums/en-US/c7633964-8744-44ae-b5ee-dafc3bb1d534/how-do-i-set-the-default-window-size-of-universal-windows-apps-on-the-desktop?forum=wpdevelop
                ApplicationView.PreferredLaunchViewSize = new Size(1080, 1920);
                ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

                //Youtube says: https://support.google.com/youtube/answer/6375112
                //2160p: 3840x2160
                //1440p: 2560x1440
                //1080p: 1920x1080
                //720p: 1280x720
                //480p: 854x480
                //360p: 640x360
                //240p: 426x240
                uiMainPage.DataContext = this;
                MainBackground = new Background();
                //MainBackground.Redraw(uiBackground);

                dice = new Dice();

                Log.WriteWithTime("MainPage: doing: make solvers\r\n");

                Log.WriteWithTime("MainPage: doing: orientation\r\n");
                SetOrientation(); // Set to the correct value.
                Window.Current.SizeChanged += Current_SizeChanged;

                uiAllPages.cbc = cbc;
                uiAllPages.InitAllPages(this, simpleCalculator, memoryConverter, dice, Log);
                uiAllPages.DisplayPage("uiCalculatorAlign");

                SetOrientation(CurrOrientation);
                MakeMenuVisible();
                Window.Current.CoreWindow.KeyUp += OnKeyUp;
                Window.Current.CoreWindow.KeyDown += OnKeyDown;

                // Set the background to dark green
                Windows.UI.Color bck = Windows.UI.Colors.Green;
                bck.A = 0x20;
                BackgroundColor = bck;


                //Name="uiMainCalculator" -- was set in the main display box 
                //bool result = uiMainCalculator.Focus(Windows.UI.Xaml.FocusState.Programmatic);
                //result = uiMainCalculator.Focus(Windows.UI.Xaml.FocusState.Programmatic);

                Log.WriteWithTime("MainPage: doing: aps (auto property save)\r\n");
                aps = new AutoPropertySave();
                aps.Init(this); // Initialize this first so that the calculator can initialize with more state (e.g., what's in view)
                aps.Init(simpleCalculator);
                aps.Init(MainBackground);
                simpleCalculator.DoSaveMemoryObject = aps;
                if (CalculatorInView() == "Programmer_")
                {
                    simpleCalculator.DisplaySpecifier = "base10";
                    simpleCalculator.DisplaySize = "4";
                }
                Log.WriteWithTime("MainPage: doing: keyboard\r\n");
                MainBackground.Redraw(uiBackground);


                //
                // Hook up the programmable bits
                //
                memoryConverter.simpleCalculator = simpleCalculator;


                // Set up in the .InitializeProgrammable calls now

                CalculatorConnection = uiAllPages.GetCalculatorConnection(); // uiCalculatorConnectionPopupAlign;
                CalculatorConnection.SetDoStopProgram(this);

                simpleCalculator.CalculatorConnection = this.CalculatorConnection;

                uiAllPages.UpdateKeyboardDisplays(); // Uses the ControlsToUpdateForKeyboardChange...

                uiAppearancePopup1.DataContext = this;
                uiAppearancePopup1.SimpleCalculator = simpleCalculator;
                uiAppearancePopup1.DoAppearance = this;
                uiAppearancePopup1.Init(MainBackground);

                // NOTE: Appearance is a proper button flyout now.
                //uiAppearancePopup2.SimpleCalculator = simpleCalculator;
                //uiAppearancePopup2.DoAppearance = this;
                //uiAppearancePopup2.Init(MainBackground);

                DataTransferManager.GetForCurrentView().DataRequested += OnShareDataRequested;

                int testResult = Test();
                testResult += ColorPicker.TestOpacityConversion();
                testResult += BCBasic.RunTimeLibrary.RTLCsvRfc4180.TestParseCsv();
                testResult += SimpleCalculator.TestDoubleToEngineering();
                testResult += BCBasic.RunTimeLibrary.InterpolationLibrary.Test();
                // testResult += Edit.ParserTest.Test(); //TODO: must not be enabled when for a RELEASE
                if (testResult == 0)
                {
                    Log.WriteWithTime("MainPage: doing: test results: PASS\r\n");
                }
                else
                {
                    Log.WriteWithTime("MainPage: doing: test results: FAIL: NError={0}\r\n", testResult);
                }

                Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested += MainPage_BackRequested;

                simpleCalculator.ClearDebugKeyString();
                Log.WriteWithTime("MainPage: end\r\n");
            }
            catch (Exception ex)
            {
                Log.WriteWithTime("ERROR: MainPage: Exception!\r\n");
                Log.WriteWithTime("MainPage: exception: message: {0}\r\n", (ex.Message != null ? ex.Message : "?message is null"));
                Log.WriteWithTime("MainPage: exception: stack: {0}\r\n", (ex.StackTrace != null ? ex.StackTrace : "?stack trace is null"));
            }

            this.LayoutUpdated += (s, e) =>
            {
                AutoshowMenuScrollbar();
            };
#if BLUETOOTH
            {
                // For the Bluetooth calculator only, allow purchasing and source code access.
                // The regular calculator doens't do that.
                this.Loaded += (s, e) =>
                {
                    uiSourceButton.Visibility = Visibility.Visible;
                    // As of 2019-03-08, IOT version is FREE
                    // // // NOTE: uiPurchaseButton.Visibility = Visibility.Visible;
                };
            }
#endif
            this.Loaded += (s, e) =>
            {
                Log.WriteWithTime("MainPage: Loaded: Start\r\n");
                Features.Init();
                ShowCorrectTrialInfo();
                Log.WriteWithTime("MainPage: Loaded: End\r\n");
            };



            /* NOTE: I don't like the screen shot facility; it can't take a picture with the title bars. */
#if TAKE_SCREENSHOPTS
            {
                this.Loaded += async (s, e) =>
                {
                    await TakeScreenshots.TakeScreenshotsAsync(this, uiAllPages);
                };
            }
#endif
        }

        //
        // All the code needed to run the tips & nags.
        // Features are in features.cs
        // Actual UI for tips is in the TrialPopup.xaml control.
        //
        List<Tip> AllTips = null; // tips have to be randomized to be used.
        List<Tip> SortedTip = new List<Tip>()
        {
            // The first four tips don't change position.
            new Tip("Please visit the Best Calculator website.  It's got tips and links to help you get the most out of Best Calculator.", "https://bestcalculator.wordpress.com/"),
            new Tip("The calculator comes with a complete PDF version of the manual", "MANUAL"),
            new Tip("You can buy the Best Calculator manual on Amazon.", "https://www.amazon.com/gp/product/1517450675/sr=1-3/qid=1443976891/ref=olp_product_details?ie=UTF8&me=&qid=1443976891&sr=1-3"),
            new Tip("Have trouble concentrating on your work?  Try the Low-Distraction work timer, also from Shipwreck Software", "https://www.microsoft.com/en-us/store/p/low-distraction-work-timer/9nblggh5r6z1"),

            new Tip("How do percent keys work?  A programmer describes many of the types of percent keys", "https://blogs.msdn.microsoft.com/oldnewthing/20080110-00/?p=23853"),
            new Tip("Calculate $7.50 + 8% tax like this: 7.5 + 8 % = and get the answer 8.1", "uiCalculatorAlign"),
            new Tip("What is 2 + 3 * 4?  Wikipedia lists how different calculators handle equation input", "https://en.wikipedia.org/wiki/Calculator_input_methods"),
            new Tip("CE (Clear Entry) will erase just the current number but leave your calculation along.  C (Clear) will clear the whole calculation.", "uiCalculatorAlign"),
            new Tip("The EE (Exponent entry) key lets you handle big numbers.  3.34 EE 22 is 3.34 × 10²²", "uiCalculatorAlign"),
            new Tip("0.7734 is Hello, upside down.  Best Calculator is the ONLY calculator app that can show numbers upside down.  Tap Appearance and select Upside-Down.", ""),
            new Tip("Unicode contains lots of fun characters like Fried Shrimp. Best Calculator lets you search for them!", "uiConversionsSubMenu|uiConversionsUnicodeDataAlign"),
            new Tip("Unicode for flaming duck?  🔥 🦆 🔥", "uiConversionsSubMenu|uiConversionsUnicodeDataAlign"),
            new Tip("Need to convert lengths?  Best Calculator has a bunch of conversions!", "uiConversionsSubMenu|uiConversionsLength"),
            new Tip("Need to convert areas?  Best Calculator has a bunch of conversions!", "uiConversionsSubMenu|uiConversionsArea"),
            new Tip("Need to convert from Celsius to Fahrenheit temperatures?  Best Calculator has a bunch of conversions!", "uiConversionsSubMenu|uiConversionsTemperature"),
            new Tip("Need to convert weights?  Best Calculator has a bunch of conversions!", "uiConversionsSubMenu|uiConversionsWeight"),
            new Tip("Gold is measured in troy ounces which are heavier than regular ounces.  A troy pound, however, is lighter than a regular pound.", "uiConversionsSubMenu|uiConversionsWeight"),
            new Tip("Need to convert energy?  Best Calculator has a bunch of conversions!", "uiConversionsSubMenu|uiConversionsEnergy"),
            new Tip("Got statistics questions?  Best Calculator has the easiest to use statistics", "uiColumnStatsAlign"),
            new Tip("Box plots (box and whisker diagrams) are awesome, and Best Calculator makes them easy", "uiColumnStatsAlign"),
            new Tip("Robust statistics?  Classical statistics?  Best Calculator makes them both easy!", "uiColumnStatsAlign"),
            new Tip("Linear regression?  Best Calculator makes it easy!", "uiColumnStatsAlign"),
            new Tip("Use the sample standard deviation when your data is just a sample of the entire world.  Use population standard deviation when you've sampled the entire world", "uiColumnStatsAlign"),
            new Tip("RSD (Relative Standard Deviation) tells you how 'spread out' your sample is", "uiColumnStatsAlign"),
            new Tip("Welch's t-test is more often valid than the Student's t-test (and is never less valid)", "uiColumnStatsAlign"),
            new Tip("Are you a programmer?  Looking for decimal, hex and more?  Try the Programmer's calculator", "uiCalculatorProgrammerAlign"),
            new Tip("Counting bits? Try the B+ key in the Programmer's calculator", "uiCalculatorProgrammerAlign"),
            new Tip("Byte-swabbing? There's a key for that in the Programmer's calculator", "uiCalculatorProgrammerAlign"),
            new Tip("How many days apart are two date?  How can you convert different calendar systems?  Best Calculator includes a date converter and calculator!", "uiDateAlign"),
            new Tip("Some calculators can save one memory item.  Best Calculator has ten memory cells that you can name.  The saved values even roam between your machines.", "uiMemoryAlign"),
            new Tip("In the calculator →M will save the current value into memory cell #0", "uiCalculatorAlign"),
            new Tip("In the calculator M→ will copy the current memory into the display", "uiCalculatorAlign"),
            new Tip("In the memory page, ←▣ will copy the memory value to the calculator value", "uiMemoryAlign"),
            new Tip("In the memory page, →▣ will copy the current calculator value into the memory cell", "uiMemoryAlign"),
            new Tip("In the memory page, you can increment and decrement any value", "uiMemoryAlign"),
            new Tip("To make a random numbers from 0 to 1, press rnd in the advanced page", "uiAdvancedWideAlign"),
            new Tip("To make a random number from 0 to 1, enter N into the calculator and press rnd N", "uiAdvancedWideAlign"),
            new Tip("The advanced page lets you calculate natural logs, base-10 logs, and base-2 logs", "uiAdvancedWideAlign"),
            new Tip("Need to know the remainder? Use the mod key on the advanced page", "uiAdvancedWideAlign"),
            new Tip("Use the  d→r key to convert degrees to radians ", "uiAdvancedWideAlign"),
            new Tip("Best Calculator includes powerful number formatting capabilities", "uiFormatAlign"),
            new Tip("Nₐ is Avogadro’s number (about 6.022x1023)", "uiConstantsAlign"),
            new Tip("gₙ (about 9.8) is the standard gravity in metric units", "uiConstantsAlign"),


            new Tip("PRINT \"HELLO WORLD\" is a very short BASIC program", "uiCalculatorQuickConnectionPopupAlign"),
            new Tip("FOR i=1 TO 10 ... NEXT i is a BC BASIC loop", "uiCalculatorQuickConnectionPopupAlign"),
            new Tip("You don't need line numbers to program in BC BASIC", "uiCalculatorQuickConnectionPopupAlign"),
            new Tip("Your BC BASIC programs will roam to all of your devices", "uiCalculatorQuickConnectionPopupAlign"),
            new Tip("LET a = 10 assigns the value of 10 to variable a in BC BASIC", "uiCalculatorQuickConnectionPopupAlign"),
            new Tip("Run your programs quickly with the user-assignable keys", "uiCalculatorQuickConnectionPopupAlign"),
            new Tip("FUNCTION myfunctionname() starts a function definition in BC BASIC", "uiCalculatorQuickConnectionPopupAlign"),
            new Tip("BC BASIC keywords are all uppercase (LET PRINT FOR etc)", "uiCalculatorQuickConnectionPopupAlign"),
            new Tip("", "uiCalculatorQuickConnectionPopupAlign"),
            new Tip("", "uiCalculatorQuickConnectionPopupAlign"),
            new Tip("", "uiCalculatorQuickConnectionPopupAlign"),
            new Tip("", "uiCalculatorQuickConnectionPopupAlign"),
            new Tip("", "uiCalculatorQuickConnectionPopupAlign"),
            new Tip("", "uiCalculatorQuickConnectionPopupAlign"),
            new Tip("", ""),
            new Tip("", ""),
            new Tip("", ""),
            new Tip("", ""),
            new Tip("", ""),
        };

#pragma warning disable 0414
        static double DaysBetweenNags = 4.7;
        static double DaysBetweenTips = 1.7;

        static string LASTNAGTIME = "LastNagTime";
        static string LASTTIPTIME = "LastTipTime";
        static Windows.Storage.ApplicationDataContainer Settings = Windows.Storage.ApplicationData.Current.LocalSettings; // don't roam the tip value
        public void ShowCorrectTrialInfo()
        {
            // Here are the available screens.
            // Regular calculator:
            // 1. First time: Welcome to Best Calculator.  Simple + features + programmable
            // 2. Else: Show tip 
            // 
            //
            // With Bluetooth version
            // 1. First time: Welcome to Best Calculator.  Program in Bluetooth is available for 30 days, then you have to purchase
            // 2. Every 5 days after trial: nag to update
            // 3. Every time after trial: trial period is over. update.
            // 4. Else: Show tip

            Features.Init();
            uiTrialPopup.theMainPage = this;

            var daysSinceFullRun = Features.DaysSinceFirstRun();
            var daysSinceNag = daysSinceFullRun;
            if (Settings.Values.ContainsKey(LASTNAGTIME) && Settings.Values[LASTNAGTIME] is DateTimeOffset)
            {
                var dt = (DateTimeOffset)Settings.Values[LASTNAGTIME];
                daysSinceNag = (DateTimeOffset.UtcNow - dt).TotalDays;
            }

            var daysSinceTip = daysSinceFullRun;
            if (Settings.Values.ContainsKey(LASTTIPTIME) && Settings.Values[LASTTIPTIME] is DateTimeOffset)
            {
                var dt = (DateTimeOffset)Settings.Values[LASTTIPTIME];
                daysSinceTip = (DateTimeOffset.UtcNow - dt).TotalDays;
            }

            //
            // Pick the right thing to do.
            //
            var runtype = TrialPopup.PopupType.NoPopup;
            if (Features.IsFirstRun)
            {
                // Show the first run screen (two different ones)
                runtype = TrialPopup.PopupType.FirstFree;
                //NOTE: Now that IOT version is free, don't show the FirstBluetooth etc screens.
#if ZZZBLUETOOTH
                runtype = TrialPopup.PopupType.FirstBluetooth;
#endif
            }
#if ZZZBLUETOOTH
            else if (Features.CurrTrialState == Features.TrialState.InTrial && daysSinceNag > DaysBetweenNags)
            {
                // Show a nag to purchase screen.
                // Only the Bluetooth version has nags.
                runtype = TrialPopup.PopupType.PurchaseBluetoothNag;
                Settings.Values[LASTNAGTIME] = DateTimeOffset.UtcNow;
            }
#endif
#if ZZZBLUETOOTH
            else if (Features.CurrTrialState == Features.TrialState.OverTrial && daysSinceNag > DaysBetweenNags)
            {
                // Show the "what you are missing" screen.
                // Only applied to Bluetooth 
                runtype = TrialPopup.PopupType.PurchaseBluetoothNag; // Just the regular nag; there isn't a special one for after the trial period.
                Settings.Values[LASTNAGTIME] = DateTimeOffset.UtcNow;
            }
#endif
            else if (daysSinceTip > DaysBetweenTips)
            {
                // Can't think of what to do?  Show a tip.
                // Choice #1: Only show a tip every () days?
                // Choice #2: Let the user turn tips off
                runtype = TrialPopup.PopupType.Tip;
                Settings.Values[LASTTIPTIME] = DateTimeOffset.UtcNow;
                InitTips();

            }
            else // Show nothing.
            {
                runtype = TrialPopup.PopupType.NoPopup;
            }

            uiTrialPopup.theMainPage = this;
            uiTrialPopup.AllTips = AllTips;
            uiTrialPopup.ShowPopup(runtype);
        }

        // Make a randomized version of the SortedTips.  SortedTips has two problems:
        // 1. Tips are deliberately duplicated so they show up more
        // 2. Tips are clumpy and will look bad to the user.
        // The first 4 tips are always shown in order.
        private void InitTips()
        {
            if (AllTips == null)
            {
                AllTips = new List<Tip>();
                Random r = new Random(932); // It's not actually random at all.  But it is arbitrary.
                                            // The sorted tips tend to be "clumpy" (all of the conversion stuff together).
                                            //
                for (int i = 0; i < 4; i++)
                {
                    AllTips.Add(SortedTip[0]);
                    SortedTip.RemoveAt(0);
                }
                while (SortedTip.Count > 0)
                {
                    var idx = r.Next(SortedTip.Count);
                    var tip = SortedTip[idx];
                    if (tip.Text != "") // Clear out the blank ones.
                    {
                        AllTips.Add(SortedTip[idx]);
                    }
                    SortedTip.RemoveAt(idx);
                }
            }
        }


        public async Task JumpTo(string text)
        {
            switch (text)
            {
                case "MANUAL":
                    {
                        var uri = new Uri("ms-appx:///Assets/BestCalculatorBasicReference.pdf");
                        var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
                        var op = new Windows.System.LauncherOptions() { DesiredRemainingView = Windows.UI.ViewManagement.ViewSizePreference.UseHalf };
                        await Windows.System.Launcher.LaunchFileAsync(file);
                    }
                    break;
                default:
                    if (text.StartsWith("http"))
                    {
                        var uri = new Uri(text);
                        await Windows.System.Launcher.LaunchUriAsync(uri);
                    }
                    else
                    {
                        var items = text.Split(new char[] { '|' }, 2);
                        if (SelectMain != items[0])
                        {
                            SelectMain = items[0];
                        }
                        if (items.Length > 1)
                        {
                            SelectSub = items[1];
                            if (items[1] == "uiConversionsUnicodeDataAlign")
                            {
                                // Try setting to Fried Shrimp
                                var unicode = uiAllPages.GetOrCreate(items[1]) as UnicodeTableControl;
                                await unicode?.SetStringAsync("search", "Fried Shrimp");
                            }
                        }
                    }
                    break;
            }
        }

        //
        // The back button switches between the MENU and a recent PAGE.
        // In a PAGE?  Back switches to menu mode
        // In a MENU?  Back goes to an appropriate PAGE
        //   - Algorithm #1: what's appropriate?  the most recent PAGE.
        //   - Algorithm #2: go to the second-most recent page -- lets people flip between the main calc and advanced.

        private void MainPage_BackRequested(object sender, Windows.UI.Core.BackRequestedEventArgs e)
        {
            // Back will switch between 
            switch (CurrScreenVisibility) // this is the old state, which we are changing.
            {
                case ScreenVisibilityState.Init:
                    break;
                case ScreenVisibilityState.MenuAndPage:
                    break;
                case ScreenVisibilityState.MenuOnly:
                    MakeMenuVisible(false);
                    var newpage = GetRecentPage(1);
                    SelectMain = newpage; // let the user flip between calc and advanced calc.
                    break;
                case ScreenVisibilityState.PageOnly:
                    MakeMenuVisible(true);
                    break;
            }
            e.Handled = true;
        }

        public string CalculatorInView()
        {
            var Retval = uiAllPages.CalculatorInView();
            return Retval;
        }
        MemoryConverter memoryConverter = new MemoryConverter();

        private int Test()
        {
            int NError = 0;
            NError += Statistics.StatisticsClassical.Test();
            NError += Statistics.StatisticsIQR.Test();
            return NError;
        }

        public string ShareTitle { get; set; }
        public string ShareString { get; set; }

        private async Task SetDataAsync(DataPackage data)
        {
            try
            {
                data.Properties.Title = ShareTitle != null ? ShareTitle : "Calculated value from Best Calculator";
                data.SetText(ShareString != null ? ShareString : simpleCalculator.ResultString);
            }
            catch (Exception e)
            {
                var d = new MessageDialog(e.Message, "Error: can't set data");
                await d.ShowAsync();
            }
        }

        private async void OnShareDataRequested(DataTransferManager sender, DataRequestedEventArgs e)
        {
            DataRequest request = e.Request;
            await SetDataAsync(request.Data);
            //request.Data.Properties.Title = ShareTitle != null ? ShareTitle : "Calculated value from Best Calculator";
            //request.Data.Properties.Description = "An example of how to share text.";
            //request.Data.SetText(ShareString != null ? ShareString : simpleCalculator.ResultString);
        }

        private async void OnCopyToClipboard(object sender, RoutedEventArgs e)
        {
            var data = new DataPackage();
            await SetDataAsync(data);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(data);
        }


        private async void OnPasteFromClipboard(object sender, RoutedEventArgs e)
        {
            DataPackageView dataPackageView = Clipboard.GetContent();
            if (dataPackageView.Contains(StandardDataFormats.Text))
            {
                string text = await dataPackageView.GetTextAsync();
                double value = 0.0;
                var status = double.TryParse(text, out value);
                if (!status)
                {
                    var md = new MessageDialog($"That's not a number ({text})");
                    await md.ShowAsync();
                }
                else
                {
                    simpleCalculator.ResultDouble = value;
                }
            }
            else
            {
                var md = new MessageDialog($"There isn't any text to paste");
                await md.ShowAsync();
            }
        }

        public AutoPropertySave aps;
        public void OnSuspend()
        {
            aps.OnSuspend();
        }


        void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            SetOrientation();
            AutoshowMenuScrollbar();
        }

        double lastHeight = Double.MaxValue;
        public void AutoshowMenuScrollbar()
        {
            if (uiMainGrid.ColumnDefinitions[0].Width.Value == 0) // MenuScroller
            {
                return; // When the menu is zero-width, don't bother trying to set the scroll bar visibility.
            }
            double newHeight = this.ActualHeight;
            if (newHeight < 10 || double.IsNaN(newHeight)) return; // can't do anything if we don't know the height

            uiMenuStack.Measure(new Size(Double.MaxValue, Double.MaxValue));
            double neededHeight = uiMenuStack.DesiredSize.Height;
            if (newHeight == lastHeight) return;
            lastHeight = newHeight;
            if (newHeight < neededHeight)
            {
                uiMenuScroller.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            }
            else
            {
                uiMenuScroller.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private CommonButtonMeasures _cbm = new CommonButtonMeasures();
        public CommonButtonMeasures cbm { get { return _cbm; } set { _cbm = value; NotifyPropertyChanged(); } }

        private CalculatorButtonColors _cbc = new CalculatorButtonColors();
        public CalculatorButtonColors cbc { get { return _cbc; } set { _cbc = value; NotifyPropertyChanged(); } }
        private CalculatorButtonColors.ColorScheme _colorScheme;
        public String ColorScheme
        {
            get { return _colorScheme.ToString(); }
            set
            {
                CalculatorButtonColors.ColorScheme scheme = CalculatorButtonColors.ColorScheme.Colorful;
                if (value == "Plain") scheme = CalculatorButtonColors.ColorScheme.Plain;

                if (scheme == _colorScheme) return;
                _colorScheme = scheme;
                cbc.SetColorScheme(_colorScheme);
                uiAllPages.UpdateKeyboardDisplays();
                NotifyPropertyChanged("cbc");
            }
        }

        /*
        private void OnSetColor(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            if (b == null) return;
            string tag = b.Tag as string;
            if (tag == null) return;
            ColorScheme = tag;
        }
         */

        private Windows.UI.Color _BackgroundColor;
        public Windows.UI.Color BackgroundColor
        {
            get { return _BackgroundColor; }
            set
            {
                if (value == _BackgroundColor) return;
                _BackgroundColor = value;
                uiMainGrid.Background = new SolidColorBrush(_BackgroundColor);
                NotifyPropertyChanged();
            }
        }

        public void SetColor(string tag, Windows.UI.Color color)
        {
            switch (tag)
            {
                case "Background":
                    BackgroundColor = color;
                    break;
                case "BackgroundSymbol":
                    MainBackground.TextColor = color;
                    MainBackground.RedrawColor();
                    break;
            }
        }

        public Windows.UI.Color GetColor(string tag)
        {
            switch (tag)
            {
                case "Background": return BackgroundColor;
                case "BackgroundSymbol": return MainBackground.TextColor;
            }
            return BackgroundColor;
        }

        private string _currFont = "Segoe";
        public string GetFont()
        {
            return _currFont;
        }

        public string Font
        {
            get { return GetFont(); }
            set { SetFont(value); }
        }

        // Allowed values: Segoe 7seg Dots Monogram
        public void SetFont (string value)
        {
            var saveValue = value != GetFont();
            switch (value)
            {
                case "Segoe": cbm.SetStandardFont(); break;
                case "7seg": cbm.Set7SegmentFont(); break;
                case "Dots": cbm.SetCarriageDot(); break;
                case "Monogram": cbm.SetCarriageMonogram(); break;
                default:
                    saveValue = false;
                    System.Diagnostics.Debug.WriteLine($"ERROR: SetFont ({value}) is unknown");
                    break;
            }
            if (saveValue)
            {
                _currFont = value;
                NotifyPropertyChanged("Font");
            }
        }

#if NEVER_EVER_DEFINED
        // Appearance is a proper button popup now
        private void OnSetAppearance(object sender, RoutedEventArgs e)
        {
            var position = (sender as Button)
                .TransformToVisual(Window.Current.Content)
                .TransformPoint(new Point());

            uiAppearance.IsOpen = true;
            uiAppearance.HorizontalOffset = position.X;
            uiAppearance.VerticalOffset = Window.Current.Bounds.Height - 300;// 200; // -150;

            //uiAppearance.HorizontalOffset = 0;
            //uiAppearance.VerticalOffset = 0;

            uiAppearance.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Left;
            uiAppearance.VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Top;

            AppearancePopup pop = uiAppearance.Child as AppearancePopup;
            pop.DoAppearance = this;
            pop.Init(MainBackground);
        }
#endif

        public void DismissAppearancePopup()
        {
            //uiAppearance.IsOpen = false;
        }




        public SimpleCalculator simpleCalculator { get; set; }
        public Dice dice { get; set; }

        private ColorDefinitions _colorDefinitions = new ColorDefinitions();
        public ColorDefinitions colorDefinitions { get { return _colorDefinitions; } set { _colorDefinitions = value; } }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            //var newlist = await WordList.Create();
            //WordDefinitions.Clear();
            //foreach (var word in newlist)
            //{
            //    WordDefinitions.Add(word);
            //}
            /* dictionary is not part of Best Calculator plans any more.
            if (uiDictionaryList.Items.Count > 0)
            {
                uiDictionaryList.SelectedIndex = 0;
            }
             */
            // Part of initialization now... uiFeedbackControl.GetAppDetails = this;
        }

        // Returns true iff at least one item was set to or reset to visible in the column
        // Except that "ALL" items don't count (otherwise it would always be true for col 2
        // because col 2 has the "ALL" tagged chevrons).

        private bool SetColumnElement(int requestedColumn, string Name)
        {
            if (Name == null) return false;
            if (requestedColumn == 2)
            {
                ; // Error; should go to uiAllPages.DisplayPage()
            }

            bool Retval = false;
            foreach (var item in uiMainGrid.Children)
            {
                var fe = item as FrameworkElement;
                if (fe == null) continue;

                string tag = (fe.Tag as string) ?? "";

                int column = Grid.GetColumn(fe);
                if (column != requestedColumn) continue;
                bool setVisible = fe.Name == Name;
                bool alreadyVisible = fe.Visibility == Visibility.Visible;

                var visibility =  (setVisible || tag=="ALL") ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
                fe.Visibility = visibility;
                if (setVisible) Retval = true;
                if (setVisible && Name.EndsWith("Align"))
                {
                    switch (Alignment)
                    {
                        case "Left":
                            fe.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Left;
                            break;
                        case "Center":
                            fe.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center;
                            break;
                        case "Right":
                            fe.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Right;
                            break;

                    }
                }
                if (setVisible && item is ICalculatorConnection)
                {
                    (item as ICalculatorConnection).DoProgramButton();
                }
            }
            AutoshowMenuScrollbar();
            return Retval;
        }

        // Looks generic, but is actually super-specific: when the user clicks the <-- button on 
        // the BC Basic library control, this routine is eventually called.  It's job is to get rid
        // of the library popup; it does that by faking a press of the BC BASIC button.
        public void DoBack(object param)
        {
            uiAllPages.DisplayPage ("uiCalculatorLibraryConnectionPopupAlign");
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

        public void DoButton (string value)
        {
            simpleCalculator.DoButton(value);
        }


        // const int NARROW_TO_WIDE_TRANSITION = 1550; as of 2016-05-01
        //const int NARROW_TO_WIDE_TRANSITION = 1650;
        private string WidthType()
        {
            double w = Window.Current.Bounds.Width;
            double h = Window.Current.Bounds.Height;
            var ratio = h > 0 ? w / h : 0.1; // e.g., "2" is very landscape (twice as wide as high).

            var transitionRatio = 1.7;
            return ratio > transitionRatio ? "Wide" : "Narrow";
        }

        private void OnSelectMain(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;
            string value = (string)button.Tag;
            DoSelectMain(value);
        }

        public void DoSelectMain(string value)
        { 
            //int w = this.LayoutRoot.ActualWidth;

            if (value.Contains("Wide")) 
            {
                var preferredWidthType = WidthType();
                value = value.Replace("Wide", preferredWidthType);
            }
            SelectMain = value;
            AutoshowMenuScrollbar();

            // But don't hide if we're just showing a sub-menu
            bool displayLeftMenu = GetScreenSize().DisplayLeftMenu;
            displayLeftMenu = displayLeftMenu || value.EndsWith("SubMenu"); // If we're opening up a submenu, don't hide the menu!
            if (!displayLeftMenu)
            {
                MakeMenuVisible(false);
            }
        }

        // When the screen is being resized, switch from 'wide' to 'narrow' or vice-versa as needed.
        private void ReselectMainForWidth()
        {
            if (SelectMain == null || SelectMain == "") return;
            bool isNarrowOrWide = SelectMain.Contains("Wide") || SelectMain.Contains("Narrow");
            if (!isNarrowOrWide) return;

            double w = Window.Current.Bounds.Width;

            if (SelectMain.Contains("Wide"))
            {
                var preferredWidthType = WidthType();
                var value = SelectMain.Replace("Wide", preferredWidthType);
                SelectMain = value;
            }
            else if (SelectMain.Contains("Narrow"))
            {
                var preferredWidthType = WidthType();
                var value = SelectMain.Replace("Narrow", preferredWidthType);
                SelectMain = value;
            }
        }

        private string _Alignment = "Center";
        public string Alignment
        {
            get { return _Alignment; }
            set
            {
                if (value == _Alignment) return;
                _Alignment = value;
                NotifyPropertyChanged();

                uiAllPages.DisplayPage(SelectMain);
                uiAllPages.DisplayPage(SelectSub);
            }
        }

        private void CollapseAll()
        {
            uiBasicSubMenu.Visibility = Visibility.Collapsed;
            uiConversionsSubMenu.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            uiElectricalEngineeringSubMenu.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            uiFinancialSubMenu.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            uiGamesSubMenu.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            uiGeometrySubMenu.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            uiHealthSubMenu.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        List<string> PageList = new List<string>();
        string GetRecentPage(int countFromTop)
        {
            int idx = PageList.Count - countFromTop - 1; // 0==top (most recent)
            if (idx < 0) idx = 0;
            if (PageList.Count == 0) return "uiCalculatorAlign";
            return PageList[idx];
        }
        private void SetPageList(string page)
        {
            var last = GetRecentPage(0); // Most recent;
            if (last != page)
            {
                PageList.Add(page);
            }
            while (PageList.Count > 2)
            {
                PageList.RemoveAt(0);
            }
        }

        private string _PrevSubmentSelected = "";
        private string _SelectMain;
        public string SelectMain
        {
            get { return _SelectMain; }
            set
            {
                try
                {
                    //if (value == _SelectMain) return;
                    var oldValue = _SelectMain;
                    _SelectMain = value;

                    CollapseAll();
                    if (value.EndsWith("SubMenu"))
                    {
                        if (value == _PrevSubmentSelected)
                        {
                            _PrevSubmentSelected = "";
                            CollapseAll();
                        }
                        else
                        {
                            _PrevSubmentSelected = value;
                            var obj = uiMainGrid.FindName(value) as FrameworkElement;
                            if (obj != null)
                            {
                                // We always get an obj here; the XAML needs to be set up
                                // to do this.
                                obj.Visibility = Windows.UI.Xaml.Visibility.Visible;
                            }
                        }
                    }
                    else
                    {
                        SetPageList(value);
                        uiAllPages.DisplayPage(value);
                    }
                    _SelectSub = null;
                    NotifyPropertyChanged();

                    // Note: move these to the AllPages control?  
                    bool mustSetupKeys = false;
                    const string progCalc = "uiCalculatorProgrammerAlign";
                    if (value == progCalc || oldValue == progCalc)
                    {
                        var displaySpecifier = aps.GetSetting(CalculatorInView() + "DisplaySpecifier");
                        if (displaySpecifier == null)
                        {
                            displaySpecifier = SimpleCalculator.DefaultDisplaySpecifier;
                        }
                        Log.WriteWithTime("SelectMain: old {0} new {1} calcInView {2} displaysecp {3}\r\n", oldValue, value, CalculatorInView(), displaySpecifier);
                        simpleCalculator.DisplaySpecifier = displaySpecifier;
                        mustSetupKeys = true;
                    }
                    if (value == progCalc)
                    {
                        simpleCalculator.DisplaySpecifier = "base10";
                        simpleCalculator.DisplaySize = "4";
                        simpleCalculator.SizeOpacity = 1.0;
                        simpleCalculator.PrecisionOpacity = 0.0;
                    }
                    else
                    {
                        simpleCalculator.SizeOpacity = 0.0;
                        simpleCalculator.PrecisionOpacity = 1.0;
                    }
                    if (mustSetupKeys)
                    {
                        uiAllPages.UpdateKeyboardDisplays();
                    }
                    AutoshowMenuScrollbar();
                }
                catch (Exception ex)
                {
                    Log.WriteWithTime("SelectMain: Exception with " + value + "\r\n");
                    Log.WriteWithTime("SelectMain: exception: message: {0}\r\n", (ex.Message != null ? ex.Message : "?message is null"));
                    Log.WriteWithTime("SelectMain: exception: stack: {0}\r\n", (ex.StackTrace != null ? ex.StackTrace : "?stack trace is null"));
                }

            }
        }

        private void OnSelectSub(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;
            string value = button.Tag as string;
            SelectSub = value;
            _SelectMain = null;
        }
        private string _SelectSub;
        public string SelectSub
        {
            get { return _SelectSub; }
            set
            {
                var setCol2 = uiAllPages.DisplayPage("" + value); 
                if (setCol2)
                {
                    _SelectSub = value;
                    SetOrientation(CurrOrientation);
                    MakeMenuVisible();
                    AutoshowMenuScrollbar();
                    NotifyPropertyChanged();
                }
            }
        }



        private void Set(object obj, string Changed, string NewVal)
        {
            obj.GetType().GetRuntimeProperty(Changed).SetValue(obj, NewVal);
        }


        public static Control GetControlOrChildControl(Panel panel, string Name)
        {
            foreach (UIElement child in panel.Children)
            {
                if (child is Grid)
                {
                    var Retval = GetControlOrChildControl((Grid)child, Name);
                    if (Retval != null) return Retval;
                }
                if (child is Control)
                {
                    Control control = (Control)child;
                    if (control.Name == Name) return control;
                    if (control.Tag as string == Name) return control;
                }
            }
            return null;
        }


        //
        // Is used to find a nearby control with a matching name
        //
        public static FrameworkElement GetClosestControl(Panel panel, UIElement ignore, string Name)
        {
            FrameworkElement Retval = null;
            foreach (UIElement child in panel.Children)
            {
                if (child == ignore) continue;

                if (child is Grid)
                {
                    Retval = GetControlOrChildControl((Grid)child, Name);
                    if (Retval != null) return Retval;
                }
                if (child is FrameworkElement)
                {
                    FrameworkElement control = child as FrameworkElement;
                    if (control.Name == Name) return control;
                    if (control.Tag as String == Name) return control;
                }
            }
            // Nothing.  Let's do the same thing with the parent control.  Yes, this means a bit of duplication
            Panel parent = panel.Parent as Panel;
            if (parent == null) return Retval;
            Retval = GetClosestControl(parent, panel, Name);
            return Retval;
        }



        private SolverWPFMetro FromButton(FrameworkElement item)
        {
            if (item == null) return null;
            var value = item.Tag as String;

            if (value != null && SolversList.ContainsKey(value))
                return SolversList[value];

            FrameworkElement parent = item.Parent as FrameworkElement;
            return FromButton(parent);
        }

        public static void DoFromCalc(Button b, SimpleCalculator simpleCalculator, SolverWPFMetro solver)
        {
            if (b == null) return;
            string Name = b.Tag as string;
            string Value = simpleCalculator.ResultString;
            var tb = MainPage.GetClosestControl(b.Parent as Panel, null, Name) as TextBox;
            // SolverWPFMetro solver = FromButton(sender as FrameworkElement);
            if (solver == null)
            {
                tb.Text = Value; // No solver involved; just set the textbox.
            }
            else
            {
                solver.SetTextbox(tb, Value);
            }
        }

        public void OnFromCalc(object sender, RoutedEventArgs e)
        {
            SolverWPFMetro solver = FromButton(sender as FrameworkElement);
            DoFromCalc(sender as Button, simpleCalculator, solver);
            /*
            Button b = sender as Button;
            if (b == null) return;
            string Name = b.Tag as string;
            string Value = simpleCalculator.ResultString;
            var tb = GetClosestControl (b.Parent as Panel, null, Name) as TextBox; 
            SolverWPFMetro solver = FromButton(sender as FrameworkElement);
            if (solver == null)
            {
                tb.Text = Value; // No solver involved; just set the textbox.
            }
            else
            {
                solver.SetTextbox(tb, Value);
            }
             */
        }

        public static void DoToCalc(Button b, SimpleCalculator simpleCalculator)
        {
            if (b == null) return;
            string Name = b.Tag as string;
            var control = GetClosestControl(b.Parent as Panel, null, Name);
            //var tb = GetClosestControl(b.Parent as Panel, null, Name) as TextBox;
            string Value = null;
            if (control is TextBlock) Value = (control as TextBlock).Text;
            if (control is TextBox) Value = (control as TextBox).Text;
            if (Value != null)
            {
                simpleCalculator.DoClearX();
                simpleCalculator.DoNumericalButton(Value);
            }
        }
        public void OnToCalc(object sender, RoutedEventArgs e)
        {
            DoToCalc(sender as Button, simpleCalculator);
            /*
            Button b = sender as Button;
            if (b == null) return;
            string Name = b.Tag as string;
            var control = GetClosestControl(b.Parent as Panel, null, Name);
            //var tb = GetClosestControl(b.Parent as Panel, null, Name) as TextBox;
            string Value = null;
            if (control is TextBlock) Value = (control as TextBlock).Text;
            if (control is TextBox) Value = (control as TextBox).Text;
            if (Value != null)
            {
                simpleCalculator.DoClearX();
                simpleCalculator.DoNumericalButton(Value);
            }
             */
        }

        private string GetText(FrameworkElement control)
        {
            string Value = null;
            if (control is TextBlock) Value = (control as TextBlock).Text;
            if (control is TextBox) Value = (control as TextBox).Text;
            return Value;
        }


        private void SetText(FrameworkElement control, string Value)
        {
            if (control is TextBlock) (control as TextBlock).Text = Value;
            if (control is TextBox) (control as TextBox).Text = Value;
        }

        public void OnMemoryPlus(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            if (b == null) return;
            string Name = b.Tag as string;
            var control = GetClosestControl(b.Parent as Panel, null, Name);
            int memoryIndex = simpleCalculator.MemoryIndex(control.Tag as string);
            string result = simpleCalculator.MemoryIncrement(memoryIndex, 1);
            SetText(control, result);
        }

        public void OnMemoryMinus(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            if (b == null) return;
            string Name = b.Tag as string;
            var control = GetClosestControl(b.Parent as Panel, null, Name);
            int memoryIndex = simpleCalculator.MemoryIndex(control.Tag as string);
            string result = simpleCalculator.MemoryIncrement(memoryIndex, -1);
            SetText(control, result);
        }
        public void OnMemoryClear(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            if (b == null) return;
            string Name = b.Tag as string;
            var control = GetClosestControl(b.Parent as Panel, null, Name);
            int memoryIndex = simpleCalculator.MemoryIndex(control.Tag as string);
            string result = simpleCalculator.MemorySet(memoryIndex, 0);
            SetText(control, result);
        }

        struct ScreenSize
        {
            public ScreenSize (CommonButtonMeasures.Size size, bool displayLeftMenu)
            {
                Size = size;
                DisplayLeftMenu = displayLeftMenu;
            }
            public CommonButtonMeasures.Size Size;
            public bool DisplayLeftMenu { get; internal set; }
        }

        private ScreenSize GetScreenSize()
        {
            var size = Window.Current.Bounds;

            if (size.Width < 600)
            {
                return new ScreenSize(CommonButtonMeasures.Size.Tiny, false);
            }
            else if (size.Width < 750)
            {
                return new ScreenSize(CommonButtonMeasures.Size.Smaller, false);
            }
            else if (size.Width < 850)
            {
                return new ScreenSize(CommonButtonMeasures.Size.Normal, false);
            }
            else if (size.Width < 1000)
            {
                return new ScreenSize(CommonButtonMeasures.Size.Smaller, true);
            }
            else
            {
                return new ScreenSize (CommonButtonMeasures.Size.Normal, true);
            }
        }

        private void SetOrientation()
        {
            var size = Window.Current.Bounds;
            var orientation = (size.Height >= size.Width) ? Orientation.Portrait : Orientation.Landscape;
            SetOrientation(orientation);
            cbm.Init(GetScreenSize().Size);
            MakeMenuVisible();
            ReselectMainForWidth();

#if NEVER_EVER_DEFINED
            // Old Windows 8.0 code
            switch (ApplicationView.Value)
            {
                case ApplicationViewState.Filled: 
                    SetOrientation(Orientation.Landscape); 
                    cbm.Init(CommonButtonMeasures.Size.Smaller);
                    break;
                case ApplicationViewState.FullScreenLandscape: 
                    SetOrientation(Orientation.Landscape); 
                    cbm.Init(CommonButtonMeasures.Size.Normal);
                    break;
                case ApplicationViewState.FullScreenPortrait: 
                    SetOrientation(Orientation.Portrait); 
                    cbm.Init(CommonButtonMeasures.Size.Normal);
                    break;
                case ApplicationViewState.Snapped: 
                    SetOrientation(Orientation.Portrait); 
                    cbm.Init(CommonButtonMeasures.Size.Tiny);
                    break;
            }
#endif
        }



        enum Orientation { NotSet, Portrait, Landscape };

        // Search through all the child grids in column 2, looking for non-collapsed ones
        private Orientation CurrOrientation = Orientation.NotSet;

        /* This method is not used
        private Orientation OrientationInc(Orientation orientation)
        {
            switch (orientation)
            {
                default:
                case Orientation.Portrait: return Orientation.Landscape;
                case Orientation.Landscape: return Orientation.Portrait;
            }
        }
         */

        private void SetOrientation(Orientation orientation)
        {
            if (orientation == CurrOrientation) return; // already correct.

            CurrOrientation = orientation;
            var requestedColumn = 2;

            foreach (var item in uiMainGrid.Children)
            {
                var grid = item as Grid;
                if (grid == null) continue;

                int column = Grid.GetColumn(grid);
                if (column != requestedColumn) continue;

                if (grid.Visibility == Windows.UI.Xaml.Visibility.Visible)
                {
                    switch (orientation)
                    {
                        case Orientation.Portrait: SetOrientationPortrait(grid); break;
                        case Orientation.Landscape: SetOrientationLandscape(grid); break;
                    }
                }
            }
        }

        private void SetOrientationPortrait(Grid grid)
        {
            //
            // In portrait mode, there are 4 rows
            //

            grid.RowDefinitions.Clear();
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            //grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            grid.ColumnDefinitions.Clear();
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });


            var child0 = grid.Children.Count > 0 ? grid.Children[0] as FrameworkElement : null;
            var child1 = grid.Children.Count > 1 ? grid.Children[1] as FrameworkElement : null;
            var child2 = grid.Children.Count > 2 ? grid.Children[2] as FrameworkElement : null;
            var child3 = grid.Children.Count > 3 ? grid.Children[3] as FrameworkElement : null;

            SetPosition(child0, 0, 0, 1);
            SetPosition(child1, 1, 0, 1);
            SetPosition(child2, 2, 0, 1);
            SetPosition(child3, 3, 0, 1);
        }

        void SetPosition (FrameworkElement element, int row, int col, int colSpan)
        {
            if (element != null)
            {
                Grid.SetRow(element, row);
                Grid.SetColumn(element, col);
                Grid.SetColumnSpan(element, colSpan);
            }
        }

        private void SetOrientationLandscape(Grid grid)
        {
            //
            // In ladscape mode, there are 3 rows; 
            //
            grid.RowDefinitions.Clear();
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            grid.ColumnDefinitions.Clear();
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });


            var child0 = grid.Children.Count > 0 ? grid.Children[0] as FrameworkElement : null;
            var child1 = grid.Children.Count > 1 ? grid.Children[1] as FrameworkElement : null;
            var child2 = grid.Children.Count > 2 ? grid.Children[2] as FrameworkElement : null;
            var child3 = grid.Children.Count > 3 ? grid.Children[3] as FrameworkElement : null;

            SetPosition(child0, 0, 0, 2);
            SetPosition(child1, 1, 0, 1);
            SetPosition(child2, 1, 1, 1);
            SetPosition(child3, 2, 0, 2);
        }

        private void OnTiny(object sender, RoutedEventArgs e)
        {
            cbm.Init(CommonButtonMeasures.Size.Tiny); 
        }

        private void OnNormal(object sender, RoutedEventArgs e)
        {
            cbm.Init(CommonButtonMeasures.Size.Normal); 
        }
        private void OnSmaller(object sender, RoutedEventArgs e)
        {
            cbm.Init(CommonButtonMeasures.Size.Smaller);
        }



        private void MakeMenuVisible()
        {
            bool displayLeftMenu = GetScreenSize().DisplayLeftMenu;
            MakeMenuVisible(displayLeftMenu);
        }


        enum ScreenVisibilityState { Init, MenuOnly, PageOnly, MenuAndPage }
        private ScreenVisibilityState CurrScreenVisibility = ScreenVisibilityState.Init;
        private void MakeMenuVisible(bool makeMenuVisible)
        {
            if (makeMenuVisible)
            {
                uiMainGrid.ColumnDefinitions[0].Width = GridLength.Auto;
                uiMainGrid.ColumnDefinitions[1].Width = GridLength.Auto;
                uiOpenButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                uiCloseButton.Visibility = Windows.UI.Xaml.Visibility.Visible;

                // Should we hide the calculator buttons?  
                // Answer: yes if the screen is too narrow
                var w = uiMainGrid.ActualWidth;
                bool widthInvalid = (Double.IsNaN(w) || w < 10);
                // Note: is there a better width?
                bool visible = (w >= 600)  ||widthInvalid; // If we don't know if the buttons should be hidden, don't hide them.
                SetPageVisibility(visible);
                CurrScreenVisibility = visible ? ScreenVisibilityState.MenuAndPage : ScreenVisibilityState.MenuOnly;
            }
            else
            {
                uiMainGrid.ColumnDefinitions[0].Width = new GridLength(0);
                uiMainGrid.ColumnDefinitions[1].Width = new GridLength(0);
                uiCloseButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                uiOpenButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
                SetPageVisibility(true); // definitely visible!
                CurrScreenVisibility = ScreenVisibilityState.PageOnly;
            }
        }

        private void SetPageVisibility (bool Visible)
        {
            if (Visible)
            {
                uiAllPages.Visibility = Visibility.Visible;
                uiMainGrid.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star); // "*" width;
            }
            else
            {
                uiAllPages.Visibility = Visibility.Collapsed; // Important to set to collapsed.  Otherwise the uiAllPages gets a new layout and is allowed to be very wide (8189 pixels) which is not reset when we display the pages again.
                uiMainGrid.ColumnDefinitions[2].Width = new GridLength(0);
            }
        }
        private void OnOpenButtons(object sender, RoutedEventArgs e)
        {
            MakeMenuVisible(true);
        }
        private void OnCloseButtons(object sender, RoutedEventArgs e)
        {
            MakeMenuVisible(false);
        }

        bool isLeftShift = false;
        bool isRightShift = false;
        bool isShift { get { return isLeftShift || isRightShift; } }

        private void OnKeyDown(object sender, Windows.UI.Core.KeyEventArgs e)
        {
            var key = e.VirtualKey;
            if (key == Windows.System.VirtualKey.Shift) isLeftShift = true;
            else if (key == Windows.System.VirtualKey.RightShift) isRightShift = true;
        }

        private void OnKeyUp(object sender, Windows.UI.Core.KeyEventArgs e)
        {
            var el = Windows.UI.Xaml.Input.FocusManager.GetFocusedElement();
            var key = e.VirtualKey;
            char value = (char)key;
            string str = new string(value, 1);
            switch (key)
            {
                case Windows.System.VirtualKey.Shift: isLeftShift = false; break;
                case Windows.System.VirtualKey.RightShift: isRightShift = false; break;
                case Windows.System.VirtualKey.Tab: 
                case Windows.System.VirtualKey.Control:
                    break;
                default:
                    break;
            }
            // If the focus is in a textbox, then I don't want to grab the key.
            // But before then, be sure to handle shift keys -- otherwise, a down-shift
            // in a text box counts towards setting shift, but the corresponding
            // shift-up does not.
            if (el is TextBox) return;

            // Fix the keyboard scan clumsiness.  It's like writing a modern app, but in the 80's.
            if (!isShift && e.VirtualKey == (Windows.System.VirtualKey)187 ) key = Windows.System.VirtualKey.Enter;
            if (isShift && e.VirtualKey == (Windows.System.VirtualKey)187) key = Windows.System.VirtualKey.Add;
            if (isShift && e.VirtualKey == Windows.System.VirtualKey.Number8) key = Windows.System.VirtualKey.Multiply;
            if (!isShift && e.VirtualKey == (Windows.System.VirtualKey)189) key = Windows.System.VirtualKey.Subtract;
            if (!isShift && e.VirtualKey == (Windows.System.VirtualKey)190) key = Windows.System.VirtualKey.Decimal;
            if (!isShift && e.VirtualKey == (Windows.System.VirtualKey)191) key = Windows.System.VirtualKey.Divide;
            if (e.VirtualKey == Windows.System.VirtualKey.NumberPad0) key = Windows.System.VirtualKey.Number0;
            if (e.VirtualKey == Windows.System.VirtualKey.NumberPad1) key = Windows.System.VirtualKey.Number1;
            if (e.VirtualKey == Windows.System.VirtualKey.NumberPad2) key = Windows.System.VirtualKey.Number2;
            if (e.VirtualKey == Windows.System.VirtualKey.NumberPad3) key = Windows.System.VirtualKey.Number3;
            if (e.VirtualKey == Windows.System.VirtualKey.NumberPad4) key = Windows.System.VirtualKey.Number4;
            if (e.VirtualKey == Windows.System.VirtualKey.NumberPad5) key = Windows.System.VirtualKey.Number5;
            if (e.VirtualKey == Windows.System.VirtualKey.NumberPad6) key = Windows.System.VirtualKey.Number6;
            if (e.VirtualKey == Windows.System.VirtualKey.NumberPad7) key = Windows.System.VirtualKey.Number7;
            if (e.VirtualKey == Windows.System.VirtualKey.NumberPad8) key = Windows.System.VirtualKey.Number8;
            if (e.VirtualKey == Windows.System.VirtualKey.NumberPad9) key = Windows.System.VirtualKey.Number9;


            e.Handled = true;

            switch (key)
            {
                default:
                    e.Handled = false;
                    break;



                case Windows.System.VirtualKey.Number0:
                    if (!isShift) simpleCalculator.DoNumericalButton("0");
                    else simpleCalculator.DoAction(")");
                    break;
                case Windows.System.VirtualKey.Number1: if (!isShift) simpleCalculator.DoNumericalButton("1"); break;
                case Windows.System.VirtualKey.Number2: if (!isShift) simpleCalculator.DoNumericalButton("2"); break;
                case Windows.System.VirtualKey.Number3: if (!isShift) simpleCalculator.DoNumericalButton("3"); break;
                case Windows.System.VirtualKey.Number4: if (!isShift) simpleCalculator.DoNumericalButton("4"); break;
                case Windows.System.VirtualKey.Number5:
                    if (!isShift) simpleCalculator.DoNumericalButton("5");
                    else simpleCalculator.DoMonoOp("%");
                    break;
                case Windows.System.VirtualKey.Number6: if (!isShift) simpleCalculator.DoNumericalButton("6"); break;
                case Windows.System.VirtualKey.Number7: if (!isShift) simpleCalculator.DoNumericalButton("7"); break;
                case Windows.System.VirtualKey.Number8: if (!isShift) simpleCalculator.DoNumericalButton("8"); break;
                case Windows.System.VirtualKey.Number9:
                    if (!isShift) simpleCalculator.DoNumericalButton("9");
                    else simpleCalculator.DoAction("(");
                    break;

                case Windows.System.VirtualKey.Decimal: if (!isShift) simpleCalculator.DoNumericalButton("."); break;


                case Windows.System.VirtualKey.C: simpleCalculator.DoAction("C"); break;

                case Windows.System.VirtualKey.Add: simpleCalculator.DoPartialButton("+"); break;

                case Windows.System.VirtualKey.Subtract: simpleCalculator.DoPartialButton("-"); break;
                case Windows.System.VirtualKey.Multiply: simpleCalculator.DoPartialButton("*"); break;
                case Windows.System.VirtualKey.Divide: simpleCalculator.DoPartialButton("/"); break;



                case Windows.System.VirtualKey.Enter: simpleCalculator.DoFinalButton("="); break;
            }
        }



#if NEVER_EVER_DEFINED
        private void OnDisplaySizeButton(object sender, RoutedEventArgs e)
        {
            string val = ((sender as Button).Tag as string).Split('|')[1];
            simpleCalculator.DisplaySize = val;
        }


        private void OnDisplayFormatButton(object sender, RoutedEventArgs e)
        {
            string val = ((sender as Button).Tag as string).Split('|')[1];
            simpleCalculator.DisplaySpecifier = val;
            UpdateKeyboardDisplays();
        }


        private void OnDisplayPrecisionButton(object sender, RoutedEventArgs e)
        {
            string val = ((sender as Button).Tag as string).Split('|')[1];
            simpleCalculator.DisplayPrecision = val;
        }
#endif

        private void OnSetAlignment(object sender, RoutedEventArgs e)
        {
            var b = sender as FrameworkElement;
            String value = b.Tag as string;
            Alignment = value;
        }

        private void OnSave(object sender, RoutedEventArgs e)
        {
            aps.OnSuspend();
        }

        private void OnRestore(object sender, RoutedEventArgs e)
        {
            aps.Init(simpleCalculator);
        }





        public string GetAppDetails()
        {
            var Retval = string.Format("Result={0}={1} PartialResults={2} DisplayFormat={3} DisplaySpec={4} DisplayPrecision={5} KeyStream={6}", 
                simpleCalculator.ResultString,
                simpleCalculator.ResultDouble,
                simpleCalculator.PartialResults,
                simpleCalculator.DisplayFormat,
                simpleCalculator.DisplaySpecifier,
                simpleCalculator.DisplayPrecision,
                simpleCalculator.GetDebugKeyString()
                );
            return Retval;
        }


        //
        // All the bits needed for the programmable calcualtor.
        //
        public double NumericValue { get { return simpleCalculator.ResultDouble; } set { if (value == NumericValue) return; simpleCalculator.ResultDouble = value; NotifyPropertyChanged(); } }

        public string MessageValue { get { return simpleCalculator.ErrorMessage; } set { if (value == MessageValue) return; simpleCalculator.ErrorMessage = value; NotifyPropertyChanged(); } }
        public string ChangedMessageValue { get; set; }
        public ICalculatorConnection CalculatorConnection = null;

#if NEVER_EVER_DEFINED
        private void OnProg(object sender, RoutedEventArgs e)
        {
            if (uiCalculatorConnectionAlign != null) uiCalculatorConnectionAlign.DoProgramButton();
        }

        private async void OnKey(object sender, RoutedEventArgs e)
        {
            var key = (sender as Button).Tag as string;
            if (key == null) return;
            if (uiCalculatorConnectionAlign != null) await uiCalculatorConnectionAlign.DoRunButtonProgramAsync(key);
        }
#endif


        public string PreferredName
        {
            get { return "Calculator"; }
        }

        public BCValue GetValue(string name)
        {
            switch (name)
            {
                case "Value": return new BCValue(simpleCalculator.ResultDouble);
                case "ValueString": return new BCValue(simpleCalculator.ResultString);
            }
            return BCValue.MakeNoSuchProperty(this, name);
        }

        public void SetValue(string name, BCValue value)
        {
            switch (name)
            {
                case "Message": //Calculator.Message
                    ChangedMessageValue = value.AsString;
                    simpleCalculator.ErrorMessage = value.AsString;
                    break;
                case "Value": // Calculator.Value
                    simpleCalculator.ResultDouble = value.AsDouble;
                    break;
            }
        }

        public IList<string> GetNames()
        {
            return new List<string>() { "Message", "Value", "ValueString" };
        }

        public void InitializeForRun()
        {
            ChangedMessageValue = "";
        }

        public void RunComplete()
        {
        }

        public async Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            switch (name)
            {
                case "ToString":
                    Retval.AsString = $"Object {PreferredName}";
                    return RunResult.RunStatus.OK;
                default:
                    await Task.Delay(0);
                    return RunResult.RunStatus.ErrorContinue;
            }
        }


        private async void OnHelp(object sender, RoutedEventArgs e)
        {
            var uri = new Uri("ms-appx:///Assets/BestCalculatorBasicReference.pdf");
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            var op = new Windows.System.LauncherOptions() { DesiredRemainingView = Windows.UI.ViewManagement.ViewSizePreference.UseHalf };
            await Windows.System.Launcher.LaunchFileAsync(file);
        }

        private async void OnPurchase(object sender, RoutedEventArgs e)
        {
            var uri = new Uri("https://www.amazon.com/BC-BASIC-Reference-manual-tutorial/dp/1517450675");
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }

        private async void OnWebsite(object sender, RoutedEventArgs e)
        {
            var uri = new Uri("https://bestcalculator.wordpress.com/");
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }

        public void Dispose()
        {
        }

        CancellationTokenSource StopProgramCancellationTokenSource = null;
        public void SetStopProgramCancellationTokenSource(System.Threading.CancellationTokenSource tokenSource)
        {
            StopProgramCancellationTokenSource = tokenSource;
        }
        // SetStopProgramVisibility
        public void SetStopProgramVisibility(Visibility visibility)
        {
            uiStopProgram.Visibility = visibility;
        }
        private void OnStopProgram(object sender, RoutedEventArgs e)
        {
            if (StopProgramCancellationTokenSource != null)
            {
                StopProgramCancellationTokenSource.Cancel();
            }
        }

    }

    public interface IMemoryButtonHandler
    {
        void OnFromCalc(object sender, RoutedEventArgs e);
        void OnToCalc(object sender, RoutedEventArgs e);
        void OnMemoryPlus(object sender, RoutedEventArgs e);
        void OnMemoryMinus(object sender, RoutedEventArgs e);
        void OnMemoryClear(object sender, RoutedEventArgs e);
    }
}
