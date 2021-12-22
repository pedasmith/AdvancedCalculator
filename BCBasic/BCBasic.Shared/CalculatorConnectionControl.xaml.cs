using Shipwreck; // From Features
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace BCBasic
{
    public interface ICalculator
    {
        double NumericValue { get; set; }
        string MessageValue { get; set; }
        string ChangedMessageValue { get; set; } // MessageValue includes things like "System test: PASS".  The updated one only includes messages set from BASIC.
    }

    public interface ICalculatorConnection
    {
        void DoProgramButton();
        Task DoRunButtonProgramAsync(string tag);
        ButtonToProgramList GetButtonToProgramList();
        void SetDoStopProgram(IDoStopProgram dostop);
    }



    public interface IDoRunProgam
    {
        Task<RunResult> DoRunProgramAsync(BCBasicProgram program, CancellationTokenSource cts, bool keepValues);
        bool ProgramCurrentlyRunning(BCBasicProgram program);
        void CloseScreen();
    }


    public sealed partial class CalculatorConnectionControl : UserControl, ICalculatorConnection, IConsole, IDoBack, IDoRunProgam
    {
        public static FrameworkElement BasicFrameworkElement = null;
        public static int AACOUNT_static = 0;
        public int AAACount { get; set; } = AACOUNT_static++;

        BCLibrary Library;
        public ButtonToProgramList BCButtonList { get; set; }
        public ICalculator Calculator = null;
        public IDoBack ParentDoBack = null; // used to pull down the package library popup
        public IDoStopProgram DoStopProgram = null;
        public void SetDoStopProgram(IDoStopProgram dostop) { DoStopProgram = dostop; }
        public BCGlobalConnections ExternalConnections = new BCGlobalConnections();
        public AdvancedCalculator.BCBasic.Graphics.GraphicsControl FullScreenWindow { get; set; } = null;
        private Border FullScreenWindowX { get; set; } = null;
        public event RoutedEventHandler AfterLoaded;

        // This method can be called any time.
        public void EnsurePaidValues()
        {
#if BLUETOOTH
#if !WINDOWS8
            Features.Init();
            var hasBluetooth = Features.HasBluetooth;
#else
            var hasBluetooth = true;
#endif
            if (hasBluetooth)
            {
                // If Bluetooth isn't enabled, don't add it in.
                // Bluetooth stands for all of the additions
                if (!ExternalConnections.Externals.ContainsKey("Bluetooth"))
                {
                    ExternalConnections.Externals.Add("Bluetooth", new AdvancedCalculator.Bluetooth.Bluetooth());
                }

                if (!ExternalConnections.Externals.ContainsKey("File"))
                {
                    ExternalConnections.Externals.Add("File", new AdvancedCalculator.BCBasic.RunTimeLibrary.RTLFile());
                }

                if (!ExternalConnections.Externals.ContainsKey("Gopher"))
                {
                    ExternalConnections.Externals.Add("Gopher", new AdvancedCalculator.BCBasic.RunTimeLibrary.RTLGopher());
                }

                if (!ExternalConnections.Externals.ContainsKey("Http"))
                {
                    ExternalConnections.Externals.Add("Http", new AdvancedCalculator.BCBasic.RunTimeLibrary.RTLHttp());
                }
#if !WINDOWS8
                //NOTE: for now, Windows 8 has no sensors.
                if (!ExternalConnections.Externals.ContainsKey("Sensor"))
                {
                    ExternalConnections.Externals.Add("Sensor", new AdvancedCalculator.BCBasic.RunTimeLibrary.RTLSensor());
                }
#endif
            }
#endif
        }

        public CalculatorConnectionControl()
        {
            BasicFrameworkElement = this; // So that we can run some updates on the UI thread
            this.InitializeComponent();
            this.Loaded += async (s, e) =>
            {
                ExternalConnections.DoConsole = this;
                ExternalConnections.Externals.Add("Data", new AdvancedCalculator.BCBasic.RunTimeLibrary.RTLData());
                ExternalConnections.Externals.Add("DateTime", new AdvancedCalculator.BCBasic.RunTimeLibrary.RTLDateTime());
                ExternalConnections.Externals.Add("Math", new AdvancedCalculator.BCBasic.MathValues());
                ExternalConnections.Externals.Add("Screen", new AdvancedCalculator.BCBasic.Graphics.ScreenValues(this));
                ExternalConnections.Externals.Add("String", new AdvancedCalculator.BCBasic.RunTimeLibrary.RTLString());
                ExternalConnections.Externals.Add("System", new AdvancedCalculator.BCBasic.RunTimeLibrary.RTLSystemX());



#if BLUETOOTH
#if WINDOWS8
                // Windows 8 version does it differently: it's always potentially enabled
                // but the user has to call the special "System.Init()" function
                // NOTE: make System.Init()
                //NOTE: can't make this work in Windows 8 :-( ExternalConnections.Externals.Add("Bluetooth", new AdvancedCalculator.Bluetooth.Bluetooth(this));
                ExternalConnections.Externals.Add("File", new AdvancedCalculator.BCBasic.RunTimeLibrary.RTLFile());
                ExternalConnections.Externals.Add("Http", new AdvancedCalculator.BCBasic.RunTimeLibrary.RTLHttp());
#else
                EnsurePaidValues();
#endif
#endif

                ExternalConnections.CurrMediaElement = uiMediaForSpeak;

                Library = new BCLibrary();
                BCButtonList = new ButtonToProgramList();

                await Library.InitAsync(BCButtonList);
                uiBCLibraryControl.Library = Library;
                uiBCLibraryControl.BCButtonList = BCButtonList;
                uiBCLibraryControl.SetBack(this, uiLibraryPopup);
                uiBCLibraryControl.ProgramRunner = this; // IDoRunProgram
                uiBCLibraryControl.Calculator = Calculator;
                uiBCLibraryControl.ExternalConnections = ExternalConnections;
                uiBCLibraryControl.InitializeExternals(ExternalConnections); // actually just sets up syntax highlighting.
                ExternalConnections.DoStopProgram = this.DoStopProgram;

                Start(delayedStart); // If it's Nothing, Start returns quickly.
                if (AfterLoaded != null)
                {
                    AfterLoaded.Invoke(this, e);
                }
#if !WINDOWS8
                Features.PaidChanged += EnsurePaidValues;
#endif
            };
            this.LayoutUpdated += (s, e) =>
            {
                uiScreen.MoveOnScreen(this.ActualWidth, this.ActualHeight);
            };
        }
        public ButtonToProgramList GetButtonToProgramList()
        {
            return BCButtonList;
        }
        enum StartWhat { Nothing, Quick, Sigma };
        StartWhat delayedStart = StartWhat.Nothing;

        public async Task SetStringAsync(string partName, string value)
        {
            switch (partName)
            {
                case "program":
                    await uiBCLibraryControl.SetStringAsync(partName, value);
                    break;
                case "sigma":
                    await uiBCLibraryControl.SetStringAsync(partName, value);
                    break;
            }
        }

        private void Start(StartWhat what)
        {
            // We might be called early in program initialization before the uiBCLibraryControl is fully created. 
            // In that case, set the delayedStart value and this will be re-called when Initialized is complete.
            if (uiBCLibraryControl.Library == null)
            {
                delayedStart = what;
                return;
            }
            if (what == StartWhat.Nothing) return; // don't have to do anything.

            var package = uiBCLibraryControl.CreatePackageIfNeeded("My BCBASIC Programs", "Quick programs that you have created");
            var pname = what == StartWhat.Quick ? $"My Program" : $"My Sigma";
            var p = package.GetProgram(pname);
            if (p == null)
            {
                p = new BCProgram();
                var programCode = @"
x=1 / SIN(Calculator.Value)
STOP x
REM CoSecant calculation
REM STOP x sets the calculator to the x value
               ";
                var sigmaCode = "=n";
                p.Code = what == StartWhat.Quick ? programCode : sigmaCode;
                p.Name = pname;
                p.Description = "Quick program that you can edit.";
                package.Programs.Insert(0, p);
                Task t = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => { await package.SaveAsync(Library); }).AsTask();
            }

            uiBCLibraryControl.Package = package;
            switch (what)
            {
                case StartWhat.Quick:
                    uiBCLibraryControl.Program = p;
                    uiBCLibraryControl.DoEditProgram();
                    break;
                case StartWhat.Sigma:
                    uiBCLibraryControl.Sigma = p;
                    uiBCLibraryControl.DoEditSigma();
                    break;
            }

        }
        public void StartLibrary()
        {
            uiBCLibraryControl.DoEditProgram();
        }


        public void StartQuickProgram()
        {
            Start(StartWhat.Quick);
        }

        public void StartSigma()
        {
            Start(StartWhat.Sigma);
        }

        public void SetAvailableWidth(double w)
        {
            if (w == 0)
            {
                return;
            }
            if (w > 3000)
            {
                ; // Spot for debugging
            }
            uiGrid.Width = w;
            uiScreen.MoveOnScreen(this.ActualWidth, this.ActualHeight);
        }

        private void OpenLibrary()
        {
            uiLibraryPopup.Visibility = Visibility.Visible;
        }

        private void CloseLibrary()
        {
            uiLibraryPopup.Visibility = Visibility.Collapsed;
        }

        private void OpenScreen()
        {
            if (this.Visibility == Visibility.Collapsed)
            {
                CloseLibrary(); // Just show the screen
                this.Visibility = Visibility.Visible;
            }
            uiScreen.Visibility = Visibility.Visible;
        }

        public BCScreenControl Screen { get { return uiScreen; } }
        // In addition to being called internally, is also called by the library when the user presses Escape.

        public void CloseScreen()
        {
            uiScreen.Visibility = Visibility.Collapsed;
        }

        public void DoProgramButton()
        {
            //uiLibraryPopup.IsOpen = true;
            OpenLibrary();
        }

        Dictionary<string, Task> RunningPrograms = new Dictionary<string, Task>();
        Dictionary<string, CancellationTokenSource> RunningProgramCancellationSource = new Dictionary<string, CancellationTokenSource>();
        public async Task DoRunButtonProgramAsync(string tag) // tag is, e.g., "A" for the A button.
        {
            foreach (var item in BCButtonList.ButtonList)
            {
                if (item.Button == tag)
                {
                    var package = Library.GetPackage(item.Package);
                    if (package == null) return;

                    if (RunningPrograms.ContainsKey(tag))
                    {
                        var cts = RunningProgramCancellationSource[tag];
                        cts.Cancel();
                    }
                    else
                    {
                        CancellationTokenSource cts = new CancellationTokenSource();
                        RunningProgramCancellationSource[tag] = cts;
                        DoStopProgram?.SetStopProgramCancellationTokenSource(cts);
                        DoStopProgram?.SetStopProgramVisibility(Visibility.Visible);

                        // Actually run the program!
                        var task = package.RunByNameAsync(ExternalConnections, item.Program, cts);


                        RunningPrograms[tag] = task;
                        var preturn = await task;
                        DoStopProgram?.SetStopProgramVisibility(Visibility.Collapsed);
                        RunningPrograms.Remove(tag);
                        if (preturn != null && Calculator != null)
                        {
                            switch (preturn.CurrentType)
                            {
                                case BCValue.ValueType.IsNoValue: break; // do nothing
                                case BCValue.ValueType.IsDouble: if (Calculator != null) Calculator.NumericValue = preturn.AsDouble; break;
                                case BCValue.ValueType.IsString: if (Calculator != null) Calculator.MessageValue = preturn.AsString; break;
                            }
                        }
                    }
                    /*
                    var prog = package.GetProgram(item.Program);
                    if (prog == null) return;
                    string code = prog.Code;
                    var program = BCBasicProgram.Compile(BCContext, code);
                    await DoRunProgramAsync(program);
                     */
                    break;
                }
            }
        }
        public bool ProgramCurrentlyRunning(BCBasicProgram program) //BCBasicProgram program)
        {
            if (program == null) return false;
            var package = program.BCProgram.Package;
            return package.ProgramAlreadyRunning();
        }

        public async Task<RunResult> DoRunProgramAsync (BCBasicProgram program, CancellationTokenSource cts, bool keepVariables = false)
        {

            if (program == null)
            {
                return new RunResult() { Status = RunResult.RunStatus.ErrorStop, Error = "No program to run" };
            }
            var package = program.BCProgram.Package;

            await package.RunAsync (ExternalConnections, program, cts, keepVariables);

            var preturn = program.BCContext.ReturnValue;
            if (preturn != null && Calculator != null)
            {
                switch (preturn.CurrentType)
                {
                    case BCValue.ValueType.IsNoValue: break; // do nothing
                    case BCValue.ValueType.IsDouble: Calculator.NumericValue = preturn.AsDouble; break;
                    case BCValue.ValueType.IsError: Calculator.MessageValue = preturn.AsString; break;
                    case BCValue.ValueType.IsString: Calculator.MessageValue = preturn.AsString; break;
                }
            }
            var message = Calculator?.ChangedMessageValue ?? "";
            bool isSilent = program.BCContext.IsSilent;
            bool useLast = preturn == null || preturn.CurrentType == BCValue.ValueType.IsNoValue;
            var last = useLast 
                ? program.BCContext.LastAssignment 
                : preturn;
            if (last == null) last = new BCValue ("");
            var Retval = new RunResult() { Status = RunResult.RunStatus.OK, Result = last, Message = message, IsSilent = isSilent };
            return Retval;
        }


        public async Task ClsAsync(BCColor backgroundColor, BCColor foregroundColor, BCGlobalConnections.ClearType clearType)
        {
            if (Calculator != null) Calculator.MessageValue = "";
            OpenScreen();
            await uiScreen.ClsAsync(backgroundColor, foregroundColor, clearType);
        }

        public async Task<string> GetInputAsync(CancellationToken ct, string prompt, string defaultValue)
        {
            // Used to have to open the screen first: OpenScreen();
            return await uiScreen.GetInputAsync(ct, prompt, defaultValue);
        }

        public void Console(string str)
        {
            uiScreen.Console(str);
        }

        public BCColor GetBackground()
        {
            return uiScreen.GetBackground();
        }

        public BCColor GetForeground()
        {
            return uiScreen.GetForeground();
        }

        public string GetAt(int row, int col, int nchar)
        {
            return uiScreen.GetAt(row, col, nchar);
        }

        public void PrintAt(PrintExpression.PrintSpaceType pst, string str, int row, int col)
        {
            //throw new NotImplementedException();
            uiScreen.PrintAt(pst, str, row, col);
            //uiScreenPopup.IsOpen = true;
            OpenScreen();
        }

        /*
        private void OnCloseScreen(object sender, RoutedEventArgs e)
        {
            CloseScreen(); // uiScreenPopup.IsOpen = false;
        }
         */

        /*
        private void OnCloseLibrary(object sender, RoutedEventArgs e)
        {
            uiLibraryPopup.IsOpen = false;
        }
         */

        void IDoBack.DoBack(object param)
        {
            if (param == uiLibraryPopup) if (ParentDoBack != null) ParentDoBack.DoBack(param); else CloseLibrary();
            if (param == uiScreenPopup) CloseScreen();

            //var popup = param as Popup;
            //if (popup != null) popup.IsOpen = false; // close the popup.
        }

        public string Inkeys()
        {
            // Just return the screen's Inkeys
            var Retval = uiScreen.Inkeys();
            return Retval;
        }

        public void RemoveFullScreenWindow()
        {
            lock (this)
            {
                if (FullScreenWindow != null)
                {
                    uiGrid.Children.Remove(FullScreenWindow);
                    uiGrid.Children.Remove(FullScreenWindowX);
                    FullScreenWindow = null;
                    FullScreenWindowX = null;
                }
            }
        }
        public void AddFullScreenWindow(AdvancedCalculator.BCBasic.Graphics.GraphicsControl graphicsWindow)
        {
            lock (this)
            {
                if (FullScreenWindow == null)
                {
                    FullScreenWindow = graphicsWindow;
                    FullScreenWindow.SetSize(this.ActualHeight, this.ActualWidth);

                    var tb = new TextBlock()
                    {
                        Text = "X",
                        Foreground = new SolidColorBrush(Colors.White),
                        FontSize = 20.0,
                        Margin = new Thickness(10.0, 3.0, 10.0, 3.0),
                    };
                    var border = new Border()
                    {
                        Background = new SolidColorBrush(Colors.Black),
                        VerticalAlignment = VerticalAlignment.Top,
                        HorizontalAlignment = HorizontalAlignment.Right,
                    };
                    border.Child = tb;
                    FullScreenWindowX = border;
                    FullScreenWindowX.Tapped += FullScreenWindowX_Tapped;
                    uiGrid.Children.Add(FullScreenWindow);
                    uiGrid.Children.Add(FullScreenWindowX);
                }
            }
        }

        private int InFullScreenWindowXTappedCount = 0;
        private async void FullScreenWindowX_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            lock (this)
            {
                if (InFullScreenWindowXTappedCount > 0)
                {
                    InFullScreenWindowXTappedCount += 1;
                    return;
                }
                else
                {
                    InFullScreenWindowXTappedCount = 1;
                }
            }
 
            for (int i=100; i>=0; i-= 10)
            {
                lock (this)
                {
                    if (FullScreenWindow != null)
                    {
                        FullScreenWindow.Opacity = ((double)i) / 100.0;
                    }
                    else
                    {
                        InFullScreenWindowXTappedCount = 0;
                        return; // The window is gone; just exit!
                    }
                }
                await Task.Delay(50);
            }

            while (InFullScreenWindowXTappedCount > 0)
            {
                lock (this)
                {
                    if (FullScreenWindow != null)
                    {
                        FullScreenWindow.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        InFullScreenWindowXTappedCount = 0;
                    }
                }
                await Task.Delay(5 * 1000); // Wait 5 seconds
                lock (this)
                {
                    InFullScreenWindowXTappedCount -= 1;
                }
            }

            lock (this)
            {
                if (FullScreenWindow != null)
                {
                    FullScreenWindow.Opacity = 1.0;
                    FullScreenWindow.Visibility = Visibility.Visible;
                }
            }
        }


        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            lock (this)
            {
                if (FullScreenWindow != null)
                {
                    FullScreenWindow.SetSize(e.NewSize.Height, e.NewSize.Width);
                }
            }
        }
    }
}
