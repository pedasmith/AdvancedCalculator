using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace BCBasic
{
    public interface IDoBack
    {
        void DoBack(object param);
    }
    public sealed partial class BCLibraryControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public IDoRunProgam ProgramRunner { get; set; } = null;
        public ICalculator Calculator = null;
        public BCGlobalConnections ExternalConnections { get; set; }
        
        private BCLibrary _Library = null;
        public BCLibrary Library { get { return _Library; } internal set { _Library = value; if (BCButtonList != null) BCButtonList.Attach(Library); NotifyPropertyChanged(); } }

        private ButtonToProgramList _BCButtonList = null;
        public ButtonToProgramList BCButtonList { get { return _BCButtonList; } set { _BCButtonList = value; if (Library != null) BCButtonList.Attach(Library); NotifyPropertyChanged(); } }


        //
        // The package and program currently being examined.
        //
        private BCPackage _Package = null;
        public BCPackage Package { get { return _Package; } set { _Package = value; NotifyPropertyChanged(); } }

        private BCProgram _Program = null;
        public BCProgram Program { get { return _Program; } set { _Program = value; NotifyPropertyChanged(); } }

        private BCProgram _Sigma = null;
        public BCProgram Sigma { get { return _Sigma; } set { _Sigma = value; NotifyPropertyChanged(); } }


        //private enum BackAction { CloseEntire, ShowPackages };
        //BackAction CurrBackAction = BackAction.CloseEntire;

        //
        //
        //
        public void SetBack(IDoBack function, object parameter)
        {
            MainBackFunction = function;
            BackParameter = parameter;
        }
        private IDoBack MainBackFunction = null;
        private object BackParameter = null;
        Stack<FrameworkElement> BackAction = new Stack<FrameworkElement>();

        public enum DisplayType { Library, Sigma, Quick };
        private DisplayType _CurrDisplayType = DisplayType.Library;
        public DisplayType CurrDisplayType { get { return _CurrDisplayType; } set { if (value == _CurrDisplayType) return; _CurrDisplayType = value; NotifyPropertyChanged(); } }
        private Edit.SyntaxHighlightEditorBCBasicAdapter EditSigmaCodeAdapter = null;
        private Edit.SyntaxHighlightEditorBCBasicAdapter EditProgramCodeAdapter = null;
        public BCLibraryControl()
        {
            this.DataContext = this;
            this.InitializeComponent();
            this.Loaded += async (ss, ee) =>
            {
                await DoStopConstantlyCompileCodeAsync(); // Stop it if needed.
                ConstantlyCompileCode = StartConstantlyCompileCode(); // The continouslycompile task
                EditProgramCodeAdapter = new Edit.SyntaxHighlightEditorBCBasicAdapter(uiEditProgramCode, "Edit");
                EditSigmaCodeAdapter = new Edit.SyntaxHighlightEditorBCBasicAdapter(uiEditSigmaCode, "Sigma");
                // hook uiEditProgramCode and Program.Code
                // hook uiEditSigmaCode and Sigma.Code
                EditProgramCodeAdapter.F5 += (s, e) =>
                {
                    OnRunEditProgram(null, null);
                };
                EditProgramCodeAdapter.F7 += (s, e) =>
                {
                    OnTestEditProgramT(null, null);
                };
                EditProgramCodeAdapter.Escape += (s, e) =>
                {
                    StopProgram();
                };
                EditSigmaCodeAdapter.F5 += (s, e) =>
                {
                    OnRunEditSigma(null, null);
                };
                // Sigma doesn't have the ability to run tests.
                EditSigmaCodeAdapter.Escape += (s, e) =>
                {
                    StopProgram();
                };
            };
            uiPackageMergeConflict.OnDialogDismiss += uiPackageMergeConflict_OnDialogDismiss;
        }

        private void StopProgram()
        {
            if (ProgramRunCts != null) ProgramRunCts.Cancel(); // Should stop the program.
            if (ProgramRunner != null) ProgramRunner.CloseScreen(); // NOTE: Really close screen when the ! stop button is pressed?
        }

        enum ProgramState {  Running, Stopped };
        private void SetupProgramUI(ProgramState state)
        {
            var vis = state == ProgramState.Running ? Visibility.Visible : Visibility.Collapsed;
            uiStopProgram.Visibility = vis;
        }


        private List<FrameworkElement> PotentialVisible = null; 
        private FrameworkElement CurrVisible()
        {
            if (PotentialVisible == null)
            {
                PotentialVisible = new List<FrameworkElement>() { uiLibraryProperties, uiPackages, uiPrograms, uiButtonListGrid, uiEditProgramMetadata, uiShowProgramMetadata, uiEditProgram, uiEditSigma, uiEditPackage, uiShowPackage };
            }
            foreach (var fe in PotentialVisible)
            {
                if (fe.Visibility == Visibility.Visible) return fe;
            }
            return null;
        }

        private void MakeVisible(FrameworkElement el, bool addToBackStack = true)
        {
            var curr = CurrVisible();

            if (curr == el)
            {
                this.UpdateLayout();
                return;// already visible!
            }
            if (curr != null)
            {
                if (addToBackStack) BackAction.Push(curr);
                curr.Visibility = Visibility.Collapsed;
            }

            el.Visibility = Visibility.Visible;
        }

        private void OnPackageClicked(object sender, TappedRoutedEventArgs e)
        {
            var package = (sender as FrameworkElement).DataContext as BCPackage;
            if (package == null) return;
            Package = package;
            Program = (Package.Programs.Count) > 0 ? Package.Programs[0] : null;

            MakeVisible(uiPrograms);
            //CurrBackAction = BackAction.ShowPackages;
        }

        private async void OnBack(object sender, RoutedEventArgs e)
        {
            await DoBackAsync();
        }
        private async void OnBackT(object sender, TappedRoutedEventArgs e)
        {
            await DoBackAsync();
        }


        private async Task DoBackAsync ()
        {
            if (BackAction.Count == 0)
            {
                foreach (var item in Library.Packages)
                {
                    if (item.MustSave)
                    {
                        await item.SaveAsync(Library);
                    }
                }
                if (MainBackFunction != null)
                {
                    MainBackFunction.DoBack(BackParameter);
                }
            }
            else
            {
                MakeVisible(BackAction.Pop(), false);
            }
        }



        // Simple-to-call mechanism to stop the constantly compiling code.
        async Task DoStopConstantlyCompileCodeAsync()
        {
            if (ConstantlyCompileCode != null && !ConstantlyCompileCode.IsCompleted)
            {
                StopConstantlyCompileCode = true;
                int count = 0;
                while (StopConstantlyCompileCode && count < 5) // Wait, but if it drags on, kill it.
                {
                    await Task.Delay(100);
                    count++;
                }
            }
            ConstantlyCompileCode = null;
            StopConstantlyCompileCode = false;
        }

        Task ConstantlyCompileCode = null;
        bool StopConstantlyCompileCode = false;
        static int NConstant = 0;
        static int NLoop = 0;
        Task StartConstantlyCompileCode()
        {
            Task t = Task.Run(async () =>
            {
                bool reallyCompile = true; // TODO: should be true when shipping
                if (reallyCompile) await Task.Delay(2000); // Wait until the app has started
                string previousProgram = "";
                while (StopConstantlyCompileCode == false)
                {
                    var localLoop = NLoop++;
                    var taskCompletionSource = new TaskCompletionSource<bool>();
                    await uiMainGrid.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        var localConstant = NConstant++;
                        var code = EditProgramCodeAdapter.Text; // it's not in Program.Code yet.  Might be null at the start of the program.
                        if (code != null && code != "" && code != previousProgram && reallyCompile)
                        {
                            previousProgram = code;
                            var program = await DoCompileProgramAsync(code);
                        }

                        code = EditSigmaCodeAdapter.Text; // it's not in Program.Code yet.  Might be null at the start of the program.
                        if (code != null && code != "" && reallyCompile)
                        {
                            var program = await DoCompileSigmaAsync(code);
                        }
                        taskCompletionSource.SetResult(true);
                    });
                    await taskCompletionSource.Task;
                    var waitMS = 1000;
                    var waitSlice = 100; // wait 100 ms at a time
                    for (int i = 0; i < waitMS; i += waitSlice)
                    {
                        await Task.Delay(waitSlice);
                        if (StopConstantlyCompileCode) break;
                    }
                }
                // Let everyone know we're stopping.
                StopConstantlyCompileCode = false;
            });
            return t;
        }

        private async void OnSaveProgram(object sender, RoutedEventArgs e)
        {
            await DoSaveProgram();
        }

        private async void OnSaveProgramT(object sender, TappedRoutedEventArgs e)
        {
            await DoSaveProgram();
        }

        private async Task DoSaveProgram()
        {
            var code = EditProgramCodeAdapter.Text;
            if (code == null || code == "")
            {
                ; // should not really happen
            }
            Program.Code = code;

            await Package.SaveAsync(Library); // Auto-save the package back to the original file.  Locked packages are read-only and don't save.
        }

        private async void OnSaveSigmaT(object sender, TappedRoutedEventArgs e)
        {
            await DoSaveSigma();
        }

        private async Task DoSaveSigma()
        { 
            var code = EditSigmaCodeAdapter.Text;
            Sigma.Code = code;

            await Package.SaveAsync(Library); // Auto-save the package back to the original file.  Locked packages are read-only and don't save.
        }

        private void OnNewPackage(object sender, RoutedEventArgs e)
        {
            var p = new BCPackage() { Name = Library.NewPackageName(), Description = "New Package for you to update with programs", IsEditable=true };
            Library.Packages.Add(p);
            uiPackageList.SelectedItem = p;
        }
        private void OnNewPackageT(object sender, TappedRoutedEventArgs e)
        {
            var p = new BCPackage() { Name = Library.NewPackageName(), Description = "New Package for you to update with programs", IsEditable = true };
            Library.Packages.Insert (0, p); // Insert at the start
            uiPackageList.SelectedItem = p;
        }

        public BCPackage CreatePackageIfNeeded (string name, string description)
        {
            var p = Library.GetPackage(name);
            if (p == null)
            {
                p = new BCPackage() { Name = name, Description = description, IsEditable = true };
                Library.Packages.Insert(0, p); // Insert at the start
            }
            //uiPackageList.SelectedItem = p;
            return p;
        }

        private void OnLibraryProperties(object sender, TappedRoutedEventArgs e)
        {
            MakeVisible(uiLibraryProperties);

        }

        BCPackage TempImportPackage = null;
        private async void OnReadPackage(object sender, TappedRoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".bcbasic");
            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                var newPackage = new BCPackage();
                var packageOk = await BCPackageImport.InitFromFile(file, newPackage);
                newPackage.Filename = file.Name;
                newPackage.IsEditable = true;
                if (!packageOk)
                {
                    // Error is in the description
                    var d = new MessageDialog(newPackage.Description, "Unable to read that package");
                    await d.ShowAsync();
                }
                else
                {
                    // 
                    // Check for merge conflicts
                    //
                    var oldPackage = Library.GetPackage(newPackage.Name);
                    //var nameExists = Library.PackageExists(package.Name);
                    if (oldPackage != null)
                    {
                        newPackage.Filename = oldPackage.Filename;
                        newPackage.IsEditable = true; // we're going to add it to the user's directory, and that's definitely editable.
                        uiPackageMergeConflict.DiffPackage(oldPackage, newPackage);
                        TempImportPackage = newPackage;
                        uiPopupImportPackageMergeConflict.IsOpen = true;
                        // Continues in uiPackageMergeConflict_OnDialogDismiss
                    }
                    else
                    {
                        // The new package name doesn't conflict with an existing package.
                        newPackage.MustSave = true;
                        bool filenameExists = Library.FilenameExists(newPackage.Filename);
                        if (filenameExists)
                        {
                            newPackage.Filename = null;
                        }
                        Library.Packages.Insert (0, newPackage);
                        await newPackage.SaveAsync(Library);

                        await DoBackAsync(); // Remove the library properties dialog (since it just has an import menu and nothing else)
                    }
                }
            }
        }

        async void uiPackageMergeConflict_OnDialogDismiss(object sender, ImportConflictControl.DismissArgs e)
        {

            uiPopupImportPackageMergeConflict.IsOpen = false;
            await DoBackAsync(); // Remove the library properties dialog (since it just has an import menu and nothing else)

            var newPackage = TempImportPackage;
            var oldPackage = Library.GetPackage(newPackage.Name);

            if (newPackage == null || oldPackage == null) return; // should never happen.

            switch (e.ConflictResolution)
            {
                case ImportConflictControl.ConflictResolution.Cancel:
                    // Do nothing; the import was cancelled.
                    break;

                case ImportConflictControl.ConflictResolution.ImportNew:
                    newPackage.MustSave = true; 
                    await newPackage.SaveAsync(Library);

                    Library.Packages.Remove(oldPackage);
                    Library.Packages.Insert(0, newPackage);
                    break;

                case ImportConflictControl.ConflictResolution.MergePreferNew:
                    {
                        oldPackage.MergeMetadata(newPackage);
                        foreach (var newProgram in newPackage.Programs)
                        {
                            var oldProgram = oldPackage.GetProgram(newProgram.Name);
                            if (oldProgram != null)
                            {
                                var idx = oldPackage.Programs.IndexOf (oldProgram);
                                oldPackage.Programs.RemoveAt(idx);
                                oldPackage.Programs.Insert(idx, newProgram);
                            }
                            else
                            {
                                oldPackage.Programs.Add(newProgram);
                            }
                        }
                    }
                    break;

                case ImportConflictControl.ConflictResolution.MergePreferOriginal:
                    {
                        // Taken from the MergePreferNew and then modified to be "Prefer Original"

                        // Whatever the new metadaa is, the old is better
                        foreach (var newProgram in newPackage.Programs)
                        {
                            var oldProgram = oldPackage.GetProgram(newProgram.Name);
                            if (oldProgram != null)
                            {
                                // If there's an old program, keep it!
                            }
                            else
                            {
                                // The new program doesn't exist in the old?  Add it in!
                                oldPackage.Programs.Add(newProgram);
                            }
                        }
                    }
                    break;

                case ImportConflictControl.ConflictResolution.RenameOriginalImportNew:
                    {
                        var oldFirstTemplate = "Old " + oldPackage.Name;
                        var oldBackupTemplate = "Old ({0}) " + oldPackage.Name;
                        var newOldName = Library.NewPackageName(oldFirstTemplate, oldBackupTemplate);

                        oldPackage.Name = newOldName;
                        oldPackage.Filename = null;
                        oldPackage.MustSave = true;
                        await (oldPackage.SaveAsync(Library));

                        if (Library.FilenameExists (newPackage.Filename))
                        {
                            newPackage.Filename = null;
                        }
                        Library.Packages.Insert(0, newPackage);
                        await newPackage.SaveAsync(Library);
                    }
                    break;

                default:
                    break;
            }
        }




        private void OnEditPackageT(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            var p = (sender as FrameworkElement).Tag as BCPackage;
            Package = p;
            MakeVisible(p.IsEditable ? uiEditPackage : uiShowPackage); 
        }


        private async void OnCloseEditPackage(object sender, RoutedEventArgs e)
        {
            await DoBackAsync();
            //uiEditPackage.IsOpen = false;
        }

        private void OnAddNewProgram(object sender, TappedRoutedEventArgs e)
        {
            var f = new BCProgram() { Name = "NewProgram", Description = "A new program for you to edit", Code = "value = Calculator.Value\r\nretval=value * value\r\nSTOP retval\r\n" };
            Package.Programs.Insert(0, f);
            Program = f;
        }

        private void OnAddNewProgramT(object sender, TappedRoutedEventArgs e)
        {
            var f = new BCProgram() { Name = "NewProgram", Description = "A new program for you to edit", Code = "value = Calculator.Value\r\nretval=value * value\r\nSTOP retval\r\n" };
            Package.Programs.Insert(0, f);
            Program = f;
        }


        private async void OnCloseEditProgram(object sender, RoutedEventArgs e)
        {
            //uiEditProgram.IsOpen = false;
            MakeVisible(uiEditProgram);

            var p = Program;
            if (p == null) return;
            if (p.Package == null) return;
            if (!p.Package.IsEditable) return;

            await p.Package.SaveAsync(Library);
        }

        private void OnEditProgramMetadata(object sender, RoutedEventArgs e)
        {
            var p = (sender as FrameworkElement).Tag as BCProgram;
            //if (!p.IsEditable) return; // can't edit a locked package
            Program = p;
            if (p.Package.IsEditable)
            {
                //uiEditProgramMetadata.IsOpen = true;
                MakeVisible(uiEditProgramMetadata);
            }
            else
            {
                MakeVisible(uiShowProgramMetadata);
                //uiShowProgramMetadata.IsOpen = true;
            }
        }

        private void OnEditProgramMetadataT(object sender, TappedRoutedEventArgs e)
        {
            var p = (sender as FrameworkElement).Tag as BCProgram;
            Program = p;
            if (p.Package.IsEditable)
            {
                MakeVisible(uiEditProgramMetadata);
            }
            else
            {
                MakeVisible(uiShowProgramMetadata);
            }

        }

        private async void OnDeleteProgramT(object sender, TappedRoutedEventArgs e)
        {
            var p = Program;
            if (p == null) return;
            if (p.Package == null) return;
            if (!p.Package.IsEditable) return;
            var mi = sender as MenuIcon;
            if (mi == null) return; // can never happen.

            var confirm = await mi.DoConfirmAsync();
            if (confirm)
            {
                p.Package.Programs.Remove(p);
                await p.Package.SaveAsync(Library);
            }
            await DoBackAsync();
        }
        private async void OnTestLibraryT(object sender, TappedRoutedEventArgs e)
        {
            var tr = new TestRunner();
            ProgramRunCts = new CancellationTokenSource();

            var result = await tr.RunLibraryTestsAsync (ExternalConnections, Library, ProgramRunCts.Token);
            await ShowTestResults(result);
        }


        private async void OnTestPackageT(object sender, TappedRoutedEventArgs e)
        {
            var p = (sender as FrameworkElement).Tag as BCProgram;
            await DoTestPackage(Package);
        }

        private async Task DoTestPackage(BCPackage p)
        {
            var tr = new TestRunner();
            ProgramRunCts = new CancellationTokenSource();

            var result = await tr.RunPackageTestsAsync(ExternalConnections, Library, ProgramRunCts.Token, p);
            await ShowTestResults(result);
        }

        private async void OnTestEditProgramT(object sender, TappedRoutedEventArgs e)
        {
            await DoSaveProgram();
            await DoTestProgram(Program);
        }

        private async void OnTestProgramT(object sender, TappedRoutedEventArgs e)
        {
            var p = (sender as FrameworkElement).Tag as BCProgram;
            await DoTestProgram(p);
        }

        private async Task DoTestProgram(BCProgram p)
        {
            var tr = new TestRunner();
            ProgramRunCts = new CancellationTokenSource();

            var result = await tr.RunProgramTestsAsync(ExternalConnections, Library, ProgramRunCts.Token, p.Package, p);
            var oldvalue = EditProgramCodeAdapter.SetFocusOnEditor(false);
            await ShowTestResults(result);
            if (oldvalue) EditProgramCodeAdapter.SetFocusOnEditor(oldvalue);
        }

        private async Task ShowTestResults(TestRunner.TestResult result)
        {
#if !WINDOWS8
            var tb = new TextBlock() { Text = result.ToString(), IsTextSelectionEnabled = true };
            var sv = new ScrollViewer() { Content = tb, MaxHeight = 400, MaxWidth = 600, HorizontalScrollMode = ScrollMode.Enabled, HorizontalScrollBarVisibility = ScrollBarVisibility.Visible };
            var md = new ContentDialog() { Title = result.Title(), PrimaryButtonText = "OK", Content = sv }; // NOTE: 2017 version allows default button? , DefaultButton = ContentDialogButton.Primary };
            bool canSetFocus = md.Focus(FocusState.Programmatic);
            await md.ShowAsync();
#else
            // The Windows 8 version isn't nearly as nice.
            var d = new MessageDialog(result.ToString(), result.Title());
            await d.ShowAsync();
#endif
        }

        CancellationTokenSource ProgramRunCts;
        // Attached to the [RUN] button on the library
        private async void OnRunProgram(object sender, RoutedEventArgs e)
        {
            var p = (sender as FrameworkElement).Tag as BCProgram;
            await DoRunProgram(p);
        }

        private async void OnRunProgramT(object sender, TappedRoutedEventArgs e)
        {
            var p = (sender as FrameworkElement).Tag as BCProgram;
            await DoRunProgram(p);
        }

        Edit.INeedToKnowWhenRunning EditorForRunning = null;
        private async Task DoRunProgram(BCProgram p)
        {
            try
            {
                //var p = (sender as FrameworkElement).Tag as BCProgram;
                EditorForRunning = uiEditProgramCode;
                if (EditorForRunning != null) EditorForRunning.Start();
                if (p == null) return;
                string code = p.Code;

                var compileResults = await BCBasicProgram.CompileAsync(ExternalConnections, ExternalConnections.DoConsole, p, code);
                var program = compileResults.Program;
                UpdateErrors(compileResults.Errors);

                if (program != null && compileResults.Errors.Count == 0)
                {
                    //program.BCContext.ExternalConnections.DoConsole = BCContext.ExternalConnections.DoConsole; // Copy the console over.
                    ProgramRunCts = new CancellationTokenSource();
                    SetupProgramUI(ProgramState.Running);
                    await ProgramRunner.DoRunProgramAsync(program, ProgramRunCts, false);
                    SetupProgramUI(ProgramState.Stopped);
                }
            }
            catch (Exception ex)
            {
                BCRunContext.AddError($"Program stopped running ({ex.Message})");
            }
            finally
            {
                if (EditorForRunning != null) EditorForRunning.Stop();
            }
        }


        private async void OnRunClsT(object sender, TappedRoutedEventArgs e)
        {
            BCColor background = new BCColor(0);
            BCColor foreground = new BCColor(7); // 7==white
            await ExternalConnections.DoConsole.ClsAsync(background, foreground, BCGlobalConnections.ClearType.Cls);
        }

        /* This was from the old button.
                private async void OnEditProgram(object sender, RoutedEventArgs e)
                {
                    //uiEditProgramMetadata.IsOpen = false;
                    await DoBackAsync();
                    MakeVisible(uiEditProgram);
                    //uiEditProgram.IsOpen = true;
                }
         */

        private async void OnEditProgramT(object sender, TappedRoutedEventArgs e)
        {
            await DoBackAsync();
            EditProgramCodeAdapter.Text = Program.Code; // Not clear why the two-way binding isn't working.
            MakeVisible(uiEditProgram);
        }

        public void DoEditProgram()
        {
            if (Program == null) return; // Can happen on initialization
            EditProgramCodeAdapter.Text = Program.Code; // Not clear why the two-way binding isn't working.
            MakeVisible(uiEditProgram);
        }

        public void DoEditSigma()
        {
            EditSigmaCodeAdapter.Text = Sigma.Code; // Not clear why the two-way binding isn't working.
            MakeVisible(uiEditSigma);
        }

        private async void OnBookButtonT(object sender, TappedRoutedEventArgs e)
        {
            var uri = new Uri("https://www.amazon.com/BC-BASIC-Reference-manual-tutorial/dp/1517450675");
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }

        // The shortcut edit is from the list of programs; that should definitely stay up.
        private void OnEditShortcutProgramT(object sender, TappedRoutedEventArgs e)
        {
            EditProgramCodeAdapter.Text = Program.Code; // Not clear why the two-way binding isn't working.
            MakeVisible(uiEditProgram);
        }

        private void OnProgramButton(object sender, RoutedEventArgs e)
        {
            //uiButtonPopup.IsOpen = true;
            MakeVisible(uiButtonListGrid);
            //CurrBackAction = BackAction.ShowPackages;
        }
        private void OnProgramButtonT(object sender, TappedRoutedEventArgs e)
        {
            MakeVisible(uiButtonListGrid);
        }
        private async void OnHelpButtonT(object sender, TappedRoutedEventArgs e)
        {
            var uri = new Uri("ms-appx:///Assets/BestCalculatorBasicReference.pdf");
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            var op = new Windows.System.LauncherOptions() { DesiredRemainingView = Windows.UI.ViewManagement.ViewSizePreference.UseHalf };
            await Windows.System.Launcher.LaunchFileAsync(file);

            // Can't do launchUri because it doesn't know how to launch ms-appx files
        }
        private void OnStopButtonT(object sender, TappedRoutedEventArgs e)
        {
            StopProgram();
        }
        private async void OnWebButtonT(object sender, TappedRoutedEventArgs e)
        {
            var uri = new Uri("https://bestcalculator.wordpress.com/");
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }


        /*
        private void OnCloseButtonPopup(object sender, RoutedEventArgs e)
        {
            uiButtonPopup.IsOpen = false;

        }
         */
        // List of compiler errors
        List<CompileError> Errors = new List<CompileError>();
        private void UpdateErrors(IEnumerable<CompileError> errors)
        {
            if (!errors.SequenceEqual(Errors))
            {
                Errors.Clear();
                foreach (var item in errors) { Errors.Add(item); }
            }
        }

        static int NCompile = 0;
        static int CurrCompile = 0;
        private async Task<BCBasicProgram> DoCompileProgramAsync (string code)
        {
            uiEditProgramError.Text = "";
            if (ExternalConnections == null) return null; // because we're not fully initialized.

            var oldConsole = ExternalConnections.DoConsole;
            int localNCompile = NCompile++;
            CurrCompile++;
            bool isBCScreenControl = oldConsole is CalculatorConnectionControl;
            var console = new ConsoleAsTextblock() { text = uiEditProgramError };
            uiEditProgramError.Visibility = Visibility.Visible;

            var compileResults = await BCBasicProgram.CompileAsync(ExternalConnections, console, Program, code);
            UpdateErrors(compileResults.Errors);
            CurrCompile--;
            return compileResults.Program;
        }

        private async Task<BCBasicProgram> DoCompileSigmaAsync(string code)
        {
            uiEditSigmaError.Text = "";
            if (ExternalConnections == null) return null; // because we're not fully initialized.

            var oldConsole = ExternalConnections.DoConsole;
            var console = new ConsoleAsTextblock() { text = uiEditSigmaError };
            uiEditSigmaError.Visibility = Visibility.Visible;

            var modifiedCode = Edit.ProgramParser.RemoveFirstEquals(code);
            var compileResults = await BCBasicProgram.CompileAsync(ExternalConnections, console, Sigma, modifiedCode);
            UpdateErrors(compileResults.Errors);

            return compileResults.Program;
        }



        // Attached to the [RUN] button on the edit program control
        private async void OnRunEditProgramT(object sender, TappedRoutedEventArgs e)
        {
            var result = await DoRunEditProgram();
        }
        private async void OnRunEditProgram(object sender, RoutedEventArgs e)
        {
            await DoRunEditProgram();
        }
        private async Task<RunResult> DoRunEditProgram()
        {
            RunResult Retval;
            try
            {
                EditorForRunning = uiEditProgramCode;
                if (EditorForRunning != null) EditorForRunning.Start();
                await DoSaveProgram();

                var program = await DoCompileProgramAsync(Program.Code);
                if (program == null)
                {
                    Retval = new RunResult() { Status = RunResult.RunStatus.ErrorStop, Error = "Program did not compile" };
                }
                else
                {
                    var errors = program.GetErrorStatements();
                    if (errors.Count > 0)
                    {
                        Retval = new RunResult() { Status = RunResult.RunStatus.ErrorStop, Error = $"{errors[0].StatementError}\nLine {errors[0].SourceCodeLine}" };
                    }
                    else
                    {
                        if (ProgramRunner.ProgramCurrentlyRunning(program))
                        {
                            Retval = new RunResult() { Status = RunResult.RunStatus.ErrorStop, Error = $"A program is already running." };
                        }
                        else
                        {
                            ProgramRunCts = new CancellationTokenSource();
                            SetupProgramUI(ProgramState.Running);
                            Retval = await ProgramRunner.DoRunProgramAsync(program, ProgramRunCts, false); // false=clear variables
                            SetupProgramUI(ProgramState.Stopped);
                        }
                    }
                }

                await DisplayReturnValueAsync(Retval);
            }
            catch (Exception ex)
            {
                Retval = new RunResult() { Status = RunResult.RunStatus.ErrorStop, Error = $"Program caused an exception {ex.Message}" };
            }
            finally
            {
                if (EditorForRunning != null) EditorForRunning.Stop();
            }
            return Retval;
        }

        private async void OnRunEditSigmaT(object sender, TappedRoutedEventArgs e)
        {
            var result = await DoRunEditSigma();
        }
        private async void OnRunEditSigma(object sender, RoutedEventArgs e)
        {
            await DoRunEditSigma();
        }
        private async Task<RunResult> DoRunEditSigma()
        {
            RunResult Retval;
            try
            {
                EditorForRunning = uiEditSigmaCode;
                if (EditorForRunning != null) EditorForRunning.Start();

                await DoSaveSigma();
                var program = await DoCompileSigmaAsync(Sigma.Code);
                if (program == null)
                {
                    Retval = new RunResult() { Status = RunResult.RunStatus.ErrorStop, Error = "Program did not compile" };
                }
                else
                {
                    int start = 0;
                    int end = 0;
                    bool startOk = Int32.TryParse(uiSigmaStart.Text, out start);
                    bool endOk = Int32.TryParse(uiSigmaEnd.Text, out end);
                    if (!startOk || !endOk)
                    {
                        if (!startOk)
                            Retval = new RunResult() { Status = RunResult.RunStatus.ErrorStop, Error = "Start must be a number" };
                        else
                            Retval = new RunResult() { Status = RunResult.RunStatus.ErrorStop, Error = "End must be a number" };
                    }
                    else
                    {
                        ProgramRunCts = new CancellationTokenSource();
                        SetupProgramUI(ProgramState.Running);
                        double total = 0.0;
                        var nvalue = new BCValue(0.0);

                        var message = "";
                        for (int n = start; n <= end && !ProgramRunCts.IsCancellationRequested; n++)
                        {
                            program.BCContext.ClearValues();
                            nvalue.AsDouble = (double)n;
                            program.BCContext.Set("n", nvalue);
                            var loopval = await ProgramRunner.DoRunProgramAsync(program, ProgramRunCts, true); // true  keep variables because we need the 'n' variable
                            if (loopval != null)
                            {
                                total += loopval.Result.AsDouble;
                                message = loopval.GetMessage(message);
                            }
                            else
                            {
                                // Note: on failure, keep going.
                            }
                        }
                        Retval = new RunResult() { Status = RunResult.RunStatus.OK, Result = new BCValue(total), Message = message };
                        SetupProgramUI(ProgramState.Stopped);
                    }
                }

                await DisplayReturnValueAsync(Retval);
            }
            catch (Exception ex)
            {
                Retval = new RunResult() { Status = RunResult.RunStatus.ErrorStop, Error = $"Sigma caused an exception {ex.Message}" };
            }
            finally
            {
                if (EditorForRunning != null) EditorForRunning.Stop();
            }

            return Retval;
        }

        private async Task DisplayReturnValueAsync(RunResult Retval)
        { 
            if (Retval.IsSilent)
            {
                return; // just return without popping a dialog box; e.g. with STOP SILENT
            }

            MessageDialog md = null;
            switch (Retval.Status)
            {
                case RunResult.RunStatus.ErrorContinue:
                case RunResult.RunStatus.ErrorStop:
                    md = new MessageDialog(Retval.ToString());
                    md.Commands.Add(new UICommand("OK", null, 1));
                    break;

                case RunResult.RunStatus.OK:
                    md = new MessageDialog(Retval.ToString(), Retval.GetMessage("Program Result"));
                    md.Commands.Add(new UICommand("OK", null, 1));
                    // Phone MessageDialog can only support 2 commands.
                    if (Calculator != null) md.Commands.Add(new UICommand("→▣", null, 3)); // We might not have a calculator...
                    else md.Commands.Add(new UICommand("Copy", null, 2));
                    break;
            }
            try
            {
                var mdresult = await md.ShowAsync();
                var cmd = mdresult == null ? 0 : (int)mdresult.Id;
                switch (cmd)
                {
                    case 2: // "COPY"
                        var data = new DataPackage();
                        data.Properties.Title = "BC BASIC computed value from Best Calculator";
                        data.SetText(Retval.ToString());
                        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(data);
                        break;
                    case 3: // "TOCALC"
                        if (Calculator != null)
                        {
                            double d = 0.0;
                            bool isDouble = Double.TryParse(Retval.ToString(), out d);
                            if (isDouble)
                            {
                                Calculator.NumericValue = d;
                            }
                            else
                            {
                                Calculator.MessageValue = Retval.ToString();
                            }
                        }
                        break;
                }
            }
            catch (Exception)
            {
                ; ;
            }
        }

        public void InitializeExternals(BCGlobalConnections externalConnections)
        {
            EditProgramCodeAdapter.InitializeExternals(externalConnections);
        }
        private async void OnCloseEditProgramMetadata(object sender, RoutedEventArgs e)
        {
            //uiEditProgramMetadata.IsOpen = false;
            await DoBackAsync();
        }

        private async void OnCloseShowProgramMetadata(object sender, RoutedEventArgs e)
        {
            //uiShowProgramMetadata.IsOpen = false;
            await DoBackAsync();
        }

        DataPackage CurrentClipboard = null;
        private void OnCopyProgram(object sender, RoutedEventArgs e)
        {
            CurrentClipboard = new DataPackage() { RequestedOperation = DataPackageOperation.Copy };
            CurrentClipboard.SetText(Program.Code);
            Clipboard.SetContent(CurrentClipboard);
        }

        private void OnCopyProgramT(object sender, TappedRoutedEventArgs e)
        {
            CurrentClipboard = new DataPackage() { RequestedOperation = DataPackageOperation.Copy };
            CurrentClipboard.SetText(Program.Code);
            Clipboard.SetContent(CurrentClipboard);
        }

        // Used to save as (export) a package
        private async void OnSaveAsPackage(object sender, TappedRoutedEventArgs e)
        {
            // Old way: JSON
            // New way (December 2017): MD (markdown) format!
            var fulltext = MdWriter.PackageToMd(Package);
            //var json = Package.ToJson();
            //var str = json.Stringify();
            //str = str.Replace("\",\"", "\",\n\""); // Add in CR
            var savePicker = new FileSavePicker();
            //fsp.DefaultFileExtension = ".bcbasic";
            savePicker.FileTypeChoices.Add("BC Basic", new List<string>() { ".bcbasic" });
            savePicker.SuggestedFileName = Package.Filename;
            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                await FileIO.WriteTextAsync(file, fulltext);
            }
        }

        // Copies a read-only package as a user package
        private async void OnUnlockPackage(object sender, TappedRoutedEventArgs e)
        {
            var originalName = Package.Name;
            for (int i=1; i<999; i++)
            {
                var newName = i == 1 ? $"My {originalName}" : $"My {originalName} ({i})";
                if (!Library.PackageExists (newName))
                {
                    // Copy the package over!
                    var newPackage = Package.Duplicate();
                    newPackage.Name = newName;
                    newPackage.IsEditable = true; // Yes, the user can edit this new package!
                    Library.Packages.Insert(0, newPackage);
                    await newPackage.SaveAsync(Library);
                    var cd = new ContentDialog()
                    {
                        Title = "Copied package",
                        Content = $"Copied to {newName}",
                        PrimaryButtonText = "OK"
                    };
                    await cd.ShowAsync();
                    return; // <=========================================== early return
                }
            }
            // Total failure; could not duplicate the package.
            var cderror = new ContentDialog()
            {
                Title = "Error while copying package",
                Content = $"Package {originalName} has already been copied too many times!",
                PrimaryButtonText = "OK"
            };
            await cderror.ShowAsync();
        }

        private async void OnDeletePackage(object sender, TappedRoutedEventArgs e)
        {
            var mi = sender as MenuIcon;
            if (mi == null) return;
            var name = Package.Name;

            var confirm = await mi.DoConfirmAsync();
            if (confirm)
            {
                var package = Package;
                Library.Packages.Remove(package);
                if (package.Directory != null) // Might be null if the package was just created and has never been saved.
                {
                    var file = await package.Directory.GetFileAsync(package.Filename);
                    if (file != null)
                    {
                        await file.DeleteAsync();
                    }
                }
            }
            await DoBackAsync();
        }

        private void OnInsertCode(object sender, RoutedEventArgs e)
        {
            var str = (sender as Button).Tag as string;
            if (str == "" || str == null) str = (sender as Button).Content as string;
            EditProgramCodeAdapter.Insert(str);
            // Make the flyout go away (if possible)
            var parent = (sender as FrameworkElement)?.Parent;
            while (parent != null && !(parent is Popup))
            {
                parent = (parent as FrameworkElement)?.Parent;
            }
            if (parent != null && parent is Popup)
            {
                (parent as Popup).IsOpen = false;
            }
        }

        private void OnInsertSigma(object sender, RoutedEventArgs e)
        {
            var str = (sender as Button).Tag as string;
            if (str == "" || str == null) str = (sender as Button).Content as string;
            EditSigmaCodeAdapter.Insert(str);
        }

        public async Task SetStringAsync(string partName, string value)
        {
            await Task.Delay(0); // silence the compiler warning.
            switch (partName)
            {
                case "program":
                    EditProgramCodeAdapter.Text = value;
                    break;
                case "sigma":
                    EditSigmaCodeAdapter.Text = value;
                    break;
            }
        }


        private async void OnWriteHTMLPackage(object sender, TappedRoutedEventArgs e)
        {
            var fname = Package.Filename + ".html";

            var picker = new FileSavePicker();
            picker.SuggestedFileName = fname;
            picker.FileTypeChoices.Add("HTML", new List<string>() { ".html" });
            picker.FileTypeChoices.Add("Markdown", new List<string>() { ".md" });
            picker.FileTypeChoices.Add("WORD", new List<string>() { ".rtf" });
            var file = await picker.PickSaveFileAsync();
            int count = 0;
            if (file != null)
            {
                try
                {
                    switch (file.FileType)
                    {
                        case ".html":
                            {
                                //var txt = Package.ToJson().Stringify();
                                //var fulltext = HTML_PRE + txt + HTML_POST;
                                var fulltext = HtmlWriter.PackageToHtml(Package);
                                await FileIO.WriteTextAsync(file, fulltext);
                                count++;
                            }
                            break;
                        case ".md":
                            {
                                var fulltext = MdWriter.PackageToMd(Package);
                                await FileIO.WriteTextAsync(file, fulltext);
                                count++;
                            }
                            break;
                        case ".rtf":
                            {
                                var fulltext = RTFWriter.PackageToRtf(Package);
                                await FileIO.WriteTextAsync(file, fulltext);
                                count++;
                            }
                            break;
                    }
                }
                catch (Exception)
                {
                    ; // Skip over failures...
                }
            }
            // Show how many files were created
            var md = new MessageDialog($"Saved {count} BCBASIC package files as documentation");
            md.Title = "Saved files";
            await md.ShowAsync();
        }

        private async void OnWriteHTML(object sender, TappedRoutedEventArgs e)
        {
            var fname = "Library" + ".html";

            var picker = new FileSavePicker();
            picker.SuggestedFileName = fname;
            picker.FileTypeChoices.Add("HTML", new List<string>() { ".html" });
            picker.FileTypeChoices.Add("WORD", new List<string>() { ".rtf" });
            var file = await picker.PickSaveFileAsync();
            int count = 0;

            if (file != null)
            {
                switch (file.FileType)
                {
                    case ".html":
                        {
                            var fulltext = HtmlWriter.LibraryToHtml(Library);
                            await FileIO.WriteTextAsync(file, fulltext);

                        }
                        break;
                    case ".rtf":
                        {
                            var fulltext = RTFWriter.LibraryToRtf(Library);
                            await FileIO.WriteTextAsync(file, fulltext);
                            count++;
                        }
                        break;
                }
            }
            // Show how many files were created
            var md = new MessageDialog($"Saved BCBASIC package files as documentation");
            md.Title = "Saved files";
            await md.ShowAsync();
        }

        // Lists all of the flags in the current program
        private void OnShowFlag(object sender, RoutedEventArgs e)
        {
            var sp = new StackPanel();
            int lineNumber = 1;
            foreach (var line in Program.CodeAsLines())
            {
                if (BCProgram.ContainsFlag (line))
                {
                    var entry = new Grid();
                    entry.Children.Add(new TextBlock() { Text = $"{lineNumber}\t{line}" });
                    sp.Children.Add(entry);
                }
                lineNumber++;
            }
            if (sp.Children.Count == 0)
            {
                // None found.  Tell the user that there are none.
                var tb = new TextBlock() { Text = "This program has no flags" };
                sp.Children.Add(tb);
            }

            uiCodePopup.Visibility = Visibility.Visible;
            uiCodePopupTitle.Text = "Code Flags";
            uiCodePopupContent.Content = sp;
        }

        private void OnDismissCodePopup(object sender, RoutedEventArgs e)
        {
            uiCodePopup.Visibility = Visibility.Collapsed;
        }

        private void OnShowProgramsWithFlags(object sender, RoutedEventArgs e)
        {
            var sp = new StackPanel();

            foreach (var program in Package.Programs)
            {
                int lineNumber = 1;
                foreach (var line in program.CodeAsLines())
                {
                    if (BCProgram.ContainsFlag(line))
                    {
                        var entry = new Grid();
                        entry.Children.Add(new TextBlock() { Text = $"{program.Name}\t{lineNumber}\t{line}" });
                        sp.Children.Add(entry);
                    }
                    lineNumber++;
                }
            }
            if (sp.Children.Count == 0)
            {
                // None found.  Tell the user that there are none.
                var tb = new TextBlock() { Text = "No programs with flags" };
                sp.Children.Add(tb);
            }
            uiPackagePopup.Visibility = Visibility.Visible;
            uiPackagePopupTitle.Text = "Programs with code flags";
            uiPackagePopupContent.Content = sp;
        }

        private void OnDismissPackagePopup(object sender, RoutedEventArgs e)
        {
            uiPackagePopup.Visibility = Visibility.Collapsed;

        }

        private void OnSigmaTapped(object sender, TappedRoutedEventArgs e)
        {
            var le = sender as Edit.LanguageEditor;
            var result = le.Focus(FocusState.Programmatic);
            le.SetFocusOnEditor(true);
        }
    }

    // This is a really simplistic "console" that isn't suitable for pretty much any purpose.
    class ConsoleAsTextblock : IConsole
    {
        public TextBlock text = null;

        public async Task ClsAsync(BCColor backgroundColor, BCColor foregroundColor, BCGlobalConnections.ClearType clearType)
        {
            await Task.Delay(0);
            if (text != null) text.Text = "";
        }

        public async Task<string> GetInputAsync(CancellationToken ct, string prompt, string defaultValue)
        {
            if (prompt == null) await Task.Delay(0); // Just to make the compiler be quiet.
            return defaultValue;
        }

        public void Console(string str)
        {
            if (text != null) text.Text += str + "\n";
        }

        public BCColor GetBackground()
        {
            return null;
        }

        public BCColor GetForeground()
        {
            return null;
        }

        public string GetAt(int row, int col, int nchar)
        {
            return ""; // Always return blank
        }

        public void PrintAt(PrintExpression.PrintSpaceType pst, string str, int row, int col)
        {
            if (text != null)
            {
                switch (pst)
                {
                    case PrintExpression.PrintSpaceType.Newline: text.Text += "\n" + str; break;
                    case PrintExpression.PrintSpaceType.NoSpace: text.Text += str; break;
                    case PrintExpression.PrintSpaceType.Tab: text.Text += "    " + str; break;
                    case PrintExpression.PrintSpaceType.At: text.Text += " " + str; break;
                }
            }
        }

        // This is a null INKEYS implementation; it just returns a blank string
        // as if the user hadn't typed anything at all.
        public string Inkeys()
        {
            return "";
        }
    }
}
