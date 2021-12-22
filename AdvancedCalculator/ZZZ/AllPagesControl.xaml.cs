using BCBasic;
using Shipwreck.Utilities;
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
    public sealed partial class AllPagesControl : UserControl, IUpdateKeyboardDisplays
    {
        public AllPagesControl()
        {
            this.InitializeComponent();
        }

        public CalculatorButtonColors cbc { get; set; }

        ICalculatorConnection _calculatorConnection = null;
        public ICalculatorConnection GetCalculatorConnection()
        {
            if (_calculatorConnection == null)
            {
                //GetOrCreate("uiCalculatorConnectionLibraryPopupAlign");
                GetOrCreate("uiCalculatorLibraryConnectionPopupAlign");
            }
            return _calculatorConnection;
        }

        private CalculatorConnectionControl uiCalculatorConnectionPopupAlign { get; set; } = null;

        string mainPageAlignment = "Left";
        string simpleCalculatorDisplaySpecifier = "";
        SimpleCalculator savedSimpleCalculator;
        Dice savedDice;
        CalculatorLog savedLog;
        IMemoryButtonHandler savedMemoryButtonHandler;
        Object savedSimpleCalculatorSource;
        IShare savedShared;
        IGetAppDetails savedAppDetails;
        ICalculator savedCalculator;
        IDoBack savedDoBack;
        IObjectValue savedExternal1;
        MemoryConverter savedMemoryConverter;


        public void InitAllPages(MainPage mainPage, SimpleCalculator simpleCalculator, MemoryConverter memoryConverter, Dice dice, CalculatorLog log)
        {
            simpleCalculator.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "DisplaySpecifier" && s is SimpleCalculator)
                {
                    simpleCalculatorDisplaySpecifier = (s as SimpleCalculator).DisplaySpecifier;
                }
            };
            savedSimpleCalculator = simpleCalculator;

            savedDice = dice;
            savedLog = log;
            savedMemoryConverter = memoryConverter;
            savedMemoryButtonHandler = mainPage;
            savedSimpleCalculatorSource = mainPage;
            savedShared = mainPage;
            savedAppDetails = mainPage;
            savedCalculator = mainPage;
            savedDoBack = mainPage;
            savedExternal1 = mainPage;

            mainPageAlignment = mainPage.Alignment;
            mainPage.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "Alignment" && s is MainPage)
                {
                    mainPageAlignment = (s as MainPage).Alignment;
                }
            };


            this.LayoutUpdated += (s, e) =>
            {
                var totWidth = this.ActualWidth;
                if (totWidth > 3000)
                {
                    ; // spot for debugging.
                }
                var C234Width = totWidth; //- uiMainGrid.ColumnDefinitions[0].ActualWidth; 
                if (uiCalculatorConnectionPopupAlign != null)
                {
                    uiCalculatorConnectionPopupAlign.SetAvailableWidth(C234Width);
                }
            };

        }

        List<UserControl> ControlsToUpdateForKeyboardChange = new List<UserControl>();
        public void UpdateKeyboardDisplays()
        {
            string erase = "";
            //string potential = "0123456789ABCDEF";
            if (CalculatorInView() == "Programmer_")
            {
                switch (simpleCalculatorDisplaySpecifier)
                {
                    case "base2": erase = "23456789ABCDEF"; break;
                    case "base8": erase = "89ABCDEF"; break;
                    case "base10": erase = "ABCDEF"; break;
                    default: erase = ""; break;
                }
            }
            else
            {
                erase = "ABCDEF";
            }
            cbc.SetColors(uiMainGrid, erase);
            foreach (var uc in ControlsToUpdateForKeyboardChange)
            {
                cbc.SetColors(uc, erase);
            }

        }


        public string CalculatorInView()
        {
            string Retval = "";
            var pgmr = Get("uiCalculatorProgrammerAlign");
            if (pgmr != null)
            {
                if (pgmr.Visibility == Visibility.Visible)
                {
                    Retval = "Programmer_";
                }
            }
            return Retval;
        }

        string previousNonPopupSetColumnName = "uiCalculatorAlign"; // Initialize to something nice.
        string previousPopupSetColumnName = ""; // Is reset if the new item isn't a popup.

        public bool DisplayPage(string Name)
        {
            if (Name == null) return false;

            // Handle popups.  A popup flips from visible to not visible when its button is pressed.
            // When it flips to not visible, it shows the last non-popup (kind of like a stack, but
            // subtly different).  Example uiCalculatorConnectionPopupAlign
            bool isPopup = Name.Contains("Popup");
            if (!isPopup)
            {
                previousNonPopupSetColumnName = Name;
                previousPopupSetColumnName = ""; // reset
            }
            else if (Name == previousPopupSetColumnName) // It's already up! Take it down!
            {
                Name = previousNonPopupSetColumnName;
                previousPopupSetColumnName = ""; // reset
                isPopup = false;
            }
            else
            {
                previousPopupSetColumnName = Name; // Remember what was set!
            }

            bool Retval = false;
            GetOrCreate(Name);
            foreach (var item in uiMainGrid.Children)
            {
                var fe = item as FrameworkElement;
                if (fe == null) continue;

                string tag = fe.Tag as string;
                if (tag == null) tag = "";

                bool setVisible = IsGridMatch(fe, Name); //  fe.Name == Name;
                bool alreadyVisible = fe.Visibility == Visibility.Visible;

                var visibility = (setVisible || tag == "ALL") ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
                fe.Visibility = visibility;
                if (setVisible) Retval = true;
                if (setVisible && Name.EndsWith("Align"))
                {
                    switch (mainPageAlignment)
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
            return Retval;
        }

        public async Task SetStringAsync (string pageName, string partName, string value)
        {
            var page = GetOrCreate(pageName);
            switch (pageName)
            {
                case "uiConversionsUnicodeDataAlign":
                    await (page as UnicodeTableControl).SetStringAsync(partName, value);
                    break;
                case "uiCalculatorQuickConnectionPopupAlign":
                    await (page as CalculatorConnectionControl).SetStringAsync(partName, value);
                    break;
                case "uiCalculatorSigmaConnectionPopupAlign":
                    await (page as CalculatorConnectionControl).SetStringAsync(partName, value);
                    break;
            }
        }


        // Pass this, e.g., "uiCalcualtor
        private FrameworkElement Get(string name)
        {
            foreach (var child in uiMainGrid.Children)
            {
                FrameworkElement fe = child as FrameworkElement;
                if (fe == null) continue;
                if (IsGridMatch(fe, name))
                {
                    return fe; // found it!
                }
            }
            return null;
        }

        private void Specialize(FrameworkElement fe, string name)
        {
            switch (name)
            {
                case "uiCalculatorLibraryConnectionPopupAlign":
                    {
                        var cc = fe as CalculatorConnectionControl;
                        if (cc != null)
                        {
                            cc.StartLibrary();
                        }
                    }
                    break;
                case "uiCalculatorNewProgramConnectionPopupAlign":
                    {
                        var cc = fe as CalculatorConnectionControl;
                        if (cc != null)
                        {
                            cc.StartQuickProgram();
                        }
                    }
                    break;
                case "uiCalculatorQuickConnectionPopupAlign":
                    {
                        var cc = fe as CalculatorConnectionControl;
                        if (cc != null)
                        {
                            cc.StartQuickProgram();
                        }
                    }
                    break;
                case "uiCalculatorSigmaConnectionPopupAlign":
                    {
                        var cc = fe as CalculatorConnectionControl;
                        if (cc != null)
                        {
                            cc.StartSigma();
                        }
                    }
                    break;
            }
        }
        private bool IsCalculatorControl(string name)
        {
            switch (name)
            {
                case "uiCalculatorQuickConnectionPopupAlign":
                case "uiCalculatorSigmaConnectionPopupAlign":
                case "uiCalculatorNewProgramConnectionPopupAlign":
                case "uiCalculatorLibraryConnectionPopupAlign":
                    return true;
            }
            return false;
        }

        private bool IsGridMatch (FrameworkElement fe, string name)
        {
            bool isCalculatorConnectionControl = IsCalculatorControl(name);
            bool isMatch = fe.Name == name || (isCalculatorConnectionControl && fe is CalculatorConnectionControl);
            return isMatch;
        }
        static UnicodeTableControl DBGUnicode = null;
        public FrameworkElement GetOrCreate(string name)
        {
            bool isCalculatorConnectionControl = IsCalculatorControl(name);

            foreach (var child in uiMainGrid.Children)
            {
                FrameworkElement fe = child as FrameworkElement;
                if (fe == null) continue;
                if (IsGridMatch(fe, name)) // (fe.Name == name || (isCalculatorConnectionControl && fe is CalculatorConnectionControl))
                {
                    // NOTE: seems like an old comment.  Specialize is not async.
                    // this seems lame; we're kicking off this task to specialize.  This whole method should be async, 
                    // but that messes up the CalculatorControl initialization.  Perhaps that can be moved?
                    //Task t = null;
                    Specialize(fe, name);
                    return fe; // found it!
                }
            }

            FrameworkElement el = null;
            var verticalAlignment = VerticalAlignment.Top;
            switch (name)
            {
                case "uiCalculatorAlign":
                    el = new KeyboardCalculatorFourPanelControl();
                    break;
                case "uiAdvancedWideAlign":
                    el = new KeyboardAdvancedWideFourPanelControl();
                    break;
                case "uiAdvancedNarrowAlign":
                    el = new KeyboardAdvancedNarrowFourPanelControl();
                    break;
                case "uiCalculatorProgrammerAlign":
                    el = new KeyboardProgrammerFourPanelControl();
                    break;
                case "uiLogoAlign":
                    el = new LogoControl();
                    break;
                case "uiColumnStatsAlign":
                    el = new ColumnStatsControl();
                    break;
                case "uiConstantsAlign":
                    el = new KeyboardConstantsFourPanelControl();
                    break;
                case "uiDateAlign":
                    el = new DateControl();
                    break;
                case "uiFeedback":
                    el = new FeedbackFourPanelControl();
                    break;
                case "uiFormatAlign":
                    el = new KeyboardFormatFourPanelControl();
                    break;
                case "uiMemoryAlign":
                    el = new MemoryControl();
                    break;
                case "uiConversionsArea":
                    el = new ConversionsAreaControl();
                    break;
                case "uiConversionsAsciiTableAlign":
                    el = new AsciiTableControl();
                    break;
                case "uiConversionsEnergy":
                    el = new ConversionsEnergyControl();
                    break;
                case "uiConversionsLength":
                    el = new ConversionsLengthControl();
                    break;
                case "uiConversionsTemperature":
                    el = new ConversionsTemperatureControl();
                    break;
                case "uiConversionsUnicodeDataAlign":
                    el = new UnicodeTableControl();
                    DBGUnicode = (UnicodeTableControl)el;
                    break;
                case "uiConversionsUSFarmVolume":
                    el = new ConversionsUSFarmVolumeControl();
                    break;
                case "uiConversionsWeight":
                    el = new ConversionsWeightControl();
                    break;
                case "uiGeometryCircle":
                    el = new GeometryCircleControl();
                    break;
                case "uiGeometryRightTriangle":
                    el = new GeometryRightTriangleControl();
                    break;
                case "uiGeometrySlope":
                    el = new GeometrySlopeControl();
                    break;
                case "uiElectricalEngineeringVIR":
                    el = new ElectricalEngineeringVIRControl();
                    break;
                case "uiElectricalEngineeringResistor":
                    el = new ElectricalEngineeringResistorControl();
                    break;
                case "uiElectricalEngineeringCapacitor":
                    el = new ElectricalEngineeringCapacitorControl();
                    break;
                case "uiElectricalEngineeringColorCode":
                    el = new ElectricalEngineeringColorCodeControl();
                    break;
                case "uiFinancialCAGR":
                    el = new FinancialCAGRControl();
                    break;
                case "uiFinancialMortgage":
                    el = new FinancialMortgageControl();
                    break;
                case "uiFinancialWACC":
                    el = new FinancialWACCControl();
                    break;
                case "uiHealthBMIAdult":
                    el = new HealthBMIAdultControl();
                    break;
                case "uiHealthBMIChildren":
                    el = new HealthBMIChildrenControl();
                    break;
                case "uiHealthIdealHeartRate":
                    el = new HealthIdealHeartRateControl();
                    break;
                case "uiGamesCoin":
                    el = new GamesCoinControl();
                    break;
                case "uiGamesDice4":
                    el = new GamesDice4Control();
                    break;
                case "uiGamesDice6":
                    el = new GamesDice6Control();
                    break;
                case "uiGamesDice6_1":
                    el = new GamesDice6_1Control();
                    break;
                case "uiGamesDice6_2":
                    el = new GamesDice6_2Control();
                    break;
                case "uiGamesDice8":
                    el = new GamesDice8Control();
                    break;
                case "uiGamesDice12":
                    el = new GamesDice12Control();
                    break;
                case "uiGamesDice20":
                    el = new GamesDice20Control();
                    break;
                case "uiHealthPulseCounter":
                    el = new HealthPulseCounterControl();
                    break;
                case "uiCalculatorQuickConnectionPopupAlign":
                    el = uiCalculatorConnectionPopupAlign ?? new CalculatorConnectionControl();
                    verticalAlignment = VerticalAlignment.Stretch;
                    break;
                case "uiCalculatorSigmaConnectionPopupAlign":
                    el = uiCalculatorConnectionPopupAlign ?? new CalculatorConnectionControl();
                    verticalAlignment = VerticalAlignment.Stretch;
                    break;
                case "uiCalculatorNewProgramConnectionPopupAlign":
                    el = uiCalculatorConnectionPopupAlign ?? new CalculatorConnectionControl();
                    verticalAlignment = VerticalAlignment.Stretch;
                    break;
                case "uiCalculatorLibraryConnectionPopupAlign":
                    el = uiCalculatorConnectionPopupAlign ?? new CalculatorConnectionControl();
                    verticalAlignment = VerticalAlignment.Stretch;
                    break;
                case "uiSource":
                    el = new Shipwreck.UIControls.DisplaySourceCode();
                    break;
                case "uiPurchase":
                    el = new Shipwreck.UIControls.PurchaseControl();
                    break;
            }
            if (el == null) return el;
            el.Name = name;
            el.HorizontalAlignment = HorizontalAlignment.Center;
            el.VerticalAlignment = verticalAlignment;
            el.Visibility = Visibility.Collapsed;
            int ninit = 0;
            if (el is IInitializeAppDetails)
            {
                (el as IInitializeAppDetails).Initialize(savedAppDetails);
                ninit++;
            }
            if (el is IInitializeCalculator)
            {
                (el as IInitializeCalculator).Initialize(savedSimpleCalculator);
                ninit++;
            }
            if (el is IInitializeCalculatorAndKeyboard && el is UserControl)
            {
                (el as IInitializeCalculatorAndKeyboard).Initialize(savedLog, savedSimpleCalculator, this);
                ControlsToUpdateForKeyboardChange.Add(el as UserControl);
                UpdateKeyboardDisplays();
                ninit++;
            }
            if (el is IInitializeCalculatorAndKeyboardAndButtonList && el is UserControl)
            {
                var cc = GetCalculatorConnection();

                (el as IInitializeCalculatorAndKeyboardAndButtonList).Initialize(savedLog, savedSimpleCalculator, this, cc);
                ControlsToUpdateForKeyboardChange.Add(el as UserControl);
                UpdateKeyboardDisplays();
                ninit++;
            }
            if (el is IInitializeDice)
            {
                (el as IInitializeDice).Initialize(savedDice);
                ninit++;
            }
            if (el is IInitializeMemory)
            {
                (el as IInitializeMemory).Inialize(savedMemoryButtonHandler, savedSimpleCalculatorSource);
                ninit++;
            }
            if (el is IInitializeShare)
            {
                (el as IInitializeShare).Initialize(savedShared);
                ninit++;
            }
            if (el is CalculatorConnectionControl)
            {
                ninit++;
                SetBCBasic(el as CalculatorConnectionControl);
            }
            if (el is Shipwreck.UIControls.DisplaySourceCode)
            {
                ninit++; // It's initialized earlier
                Task t = (el as Shipwreck.UIControls.DisplaySourceCode).InitFileList(new List<string>()
                    {
                        "Bluetooth.cs",
                        "BluetoothDevices.cs",
                        "DOTTI.cs",
                        "Hexiwear.cs",
                        "MagicLight.cs",
                        "NOTTI.cs",
                        "SensorTag2541.cs",
                        "BluetoothUtilities.cs",
                        "ObjectList.cs",
                        "SourceCode.zip",
                    });
            }


            if (ninit != 1)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: {name} incorrect initializion count; should be 1 but instead is {ninit}.");
            }

            // There are three named things which are each just the same calculator connection control.
            if (!uiMainGrid.Children.Contains(el))
            {
                uiMainGrid.Children.Add(el);
            }
            
            if (el is CalculatorConnectionControl)
            {
                (el as CalculatorConnectionControl).AfterLoaded +=  (s, e) =>
                {
                    Specialize(el, name);
                };
            }

            return el;
        }

        private void SetBCBasic(CalculatorConnectionControl cc)
        {
            if (_calculatorConnection == null)
            {
                _calculatorConnection = cc;
                uiCalculatorConnectionPopupAlign = cc;
            }
            Canvas.SetZIndex(cc, 10); // Set the calculator stuff to be on top.

            // Note: there's too much special-casing going on here!
            cc.Calculator = savedCalculator;
            cc.ParentDoBack = savedDoBack;
            cc.ExternalConnections.AddExternal(savedExternal1);
            cc.ExternalConnections.AddExternal(savedMemoryConverter);
            cc.SetAvailableWidth(this.ActualWidth);
        }

    }
}
