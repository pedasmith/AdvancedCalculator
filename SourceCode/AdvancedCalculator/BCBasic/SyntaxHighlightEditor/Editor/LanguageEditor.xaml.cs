using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Edit
{
    public interface IKeyDown
    {
        void OnKeyDown(MyKeyEventArgs args);
    }
    public interface INeedToKnowWhenRunning
    {
        void Start();
        void Stop();
    }

#if WINDOWS8
    // Needed for the emulated GOTO dialog
    public class EditGotoDialog
    {
        public enum GotoType {  Line, Character }
    }
    public enum CoreTextInputScope { Text };
    public enum CoreTextInputPaneDisplayPolicy { Automatic };

    public class KeyEventArgs
    {
        public bool Handled { get; set; }
        public CorePhysicalKeyStatus KeyStatus { get; set; }
        public VirtualKey VirtualKey { get; set; }
        public string DeviceId { get; set; }
    }

    public class CoreTextCompositionStartedEventArgs
    {

    }
    public class CoreTextCompositionCompletedEventArgs
    {

    }
    public class CoreTextEditContext
    {
        public CoreTextInputPaneDisplayPolicy InputPaneDisplayPolicy { get; set; }
        public CoreTextInputScope InputScope { get; set; }

        public void NotifyFocusEnter()
        {

        }
        public void NotifyFocusLeave()
        {

        }
        public void NotifySelectionChanged(CoreTextRange selection)
        {

        }
        public void NotifyTextChanged(CoreTextRange modifiedRange, int newLength, CoreTextRange newSelection)
        {

        }
#if WINDOWS8
#pragma warning disable 0067
#endif
        public event TypedEventHandler<CoreTextEditContext, CoreTextCompositionCompletedEventArgs> CompositionCompleted;
        public event TypedEventHandler<CoreTextEditContext, CoreTextCompositionStartedEventArgs> CompositionStarted;
        public event TypedEventHandler<CoreTextEditContext, object> FocusRemoved;
        //public event TypedEventHandler<CoreTextEditContext, CoreTextFormatUpdatingEventArgs> FormatUpdating;
        public event TypedEventHandler<CoreTextEditContext, CoreTextLayoutRequestedEventArgs> LayoutRequested;
        public event TypedEventHandler<CoreTextEditContext, CoreTextSelectionRequestedEventArgs> SelectionRequested;
        public event TypedEventHandler<CoreTextEditContext, CoreTextSelectionUpdatingEventArgs> SelectionUpdating;
        public event TypedEventHandler<CoreTextEditContext, CoreTextTextRequestedEventArgs> TextRequested;
        public event TypedEventHandler<CoreTextEditContext, CoreTextTextUpdatingEventArgs> TextUpdating;
        public event TypedEventHandler<CoreTextEditContext, object> NotifyFocusLeaveCompleted;
    }
    public class CoreTextLayoutBounds
    {
        public Rect ControlBounds { get; set; }
        public Rect TextBounds { get; set; }
    }
    public class CoreTextLayoutRequest
    {
        public CoreTextLayoutBounds LayoutBounds { get; set; }
    }
    public class CoreTextLayoutRequestedEventArgs
    {
        public CoreTextLayoutRequest Request { get; set; }
    }
    public struct CoreTextRange
    {
        public int StartCaretPosition { get; set; }
        public int EndCaretPosition { get; set; }
    }
    public class CoreTextSelectionRequest
    {
        public CoreTextRange Selection { get; set; }
    }
    public class CoreTextSelectionRequestedEventArgs
    {
        public CoreTextSelectionRequest Request { get; set; }
    }
    public class CoreTextSelectionUpdatingEventArgs
    {
        public CoreTextRange Selection { get; set; }
    }
    public class CoreTextServicesManager
    {
        static CoreTextServicesManager Singleton = null;
        public static CoreTextServicesManager GetForCurrentView()
        {
            if (Singleton == null) Singleton = new CoreTextServicesManager();
            return Singleton;
        }
        public CoreTextEditContext CreateEditContext()
        {
            return new CoreTextEditContext();
        }
    }
    public class CoreTextTextRequest
    {
        public CoreTextRange Range { get; set; }
        public string Text { get; set; }
    }
    public class CoreTextTextRequestedEventArgs
    {
        public CoreTextTextRequest Request { get; set; }
    }
    public class CoreTextTextUpdatingEventArgs
    {
        public CoreTextRange Range { get; set; }
        public CoreTextRange NewSelection { get; set; }
        public CoreTextRange Selection { get; set; }
        public string Text { get; set; }
    }
    public class CoreWindow
    {
        static CoreWindow Singleton = null;
        public static CoreWindow GetForCurrentThread()
        {
            if (Singleton == null) Singleton = new CoreWindow();
            return Singleton;
        }
        public event TypedEventHandler<CoreWindow, KeyEventArgs> KeyDown;
        bool IsCapsPressed = false;
        bool IsShiftPressed = false;
        bool IsLeftShiftPressed = false;
        bool IsRightShiftPressed = false;
        public CoreVirtualKeyStates GetKeyState(VirtualKey virtualKey)
        {
            if (virtualKey == VirtualKey.Shift)
            {
                if (IsCapsPressed || IsShiftPressed || IsLeftShiftPressed || IsRightShiftPressed)
                {
                    return CoreVirtualKeyStates.Down;
                }
            }
            return CoreVirtualKeyStates.None;
        }
        public void RouteKeyFromUiLinesKeyDown(object sender, KeyRoutedEventArgs args)
        {
            if (KeyDown == null) return;
            var newArgs = new KeyEventArgs()
            {
                Handled = args.Handled,
                KeyStatus = args.KeyStatus,
                VirtualKey = args.Key,
                DeviceId = "(non device id on Windows 8)"
            };
            if (args.Key == VirtualKey.Shift) IsShiftPressed = true;
            if (args.Key == VirtualKey.LeftShift) IsLeftShiftPressed = true;
            if (args.Key == VirtualKey.RightShift) IsRightShiftPressed = true;
            if (args.Key == VirtualKey.CapitalLock) IsCapsPressed = !IsCapsPressed; // It's a lock
            KeyDown(null, newArgs);
        }

        public void RouteKeyFromUiLinesKeyUp(object sender, KeyRoutedEventArgs args)
        {
            if (args.Key == VirtualKey.Shift) IsShiftPressed = false;
            if (args.Key == VirtualKey.LeftShift) IsLeftShiftPressed = false;
            if (args.Key == VirtualKey.RightShift) IsRightShiftPressed = false;
        }
    }

#endif




    public enum FindDirection { Forward, Backwards };
    public interface ILanguageEditor
    {
        // EG, user pressed delete at the start of a line
        void DoCombineLine(Statement statement, int direction, int finalCursorPos);

        // EG, typed a PRINT "DONE"<CR> (but CR can be anywhere, not just the end).
        void DoSplitLine(Statement statement, int position);
    }


    public sealed partial class LanguageEditor : UserControl, ILanguageEditor, INeedToKnowWhenRunning
    {
        public ObservableCollection<Statement> ProgramLines { get; } = new ObservableCollection<Statement>();
        public IKeyDown AdapterKeyDown { get; set; } = null;
        CoreWindow _coreWindow;
        public string EditorName = "(not set)";

        public LanguageEditor()
        {
            this.DataContext = this;
            this.InitializeComponent();
            this.Loaded += LanguageEditor_Loaded;
            _coreWindow = CoreWindow.GetForCurrentThread();
            _coreWindow.KeyDown += CoreWindow_KeyDown;

            this.uiLines.KeyDown += UiLines_KeyDown;
            this.uiLines.KeyUp += UiLines_KeyUp;
        }



        public static void Log (string str)
        {
            // System.Diagnostics.Debug.WriteLine(str); //TODO: must not be enabled when for a RELEASE so we're not spewing output
        }

        private void ItemsPanelRoot_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            Log($"KEY:LanguagEditor:uiLines::ItemsPanelRoot::KeyDown: e {e.Key}");
        }

        private void UiLines_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            Log($"KEY:LanguagEditor:uiLines::KeyDown: args.Key={e.Key}");
            // Supress the arrow keys (they shift the scroll viewer, and it looks bad)
            if (e.Key == VirtualKey.Up || e.Key == VirtualKey.Down)
            {
                e.Handled = true;
            }
            // Ditto left and right when dealing with a very long line
            if (e.Key == VirtualKey.Left || e.Key == VirtualKey.Right)
            {
                e.Handled = true;
            }
            // Ditto Home and End
            if (e.Key == VirtualKey.Home || e.Key == VirtualKey.End)
            {
                e.Handled = true;
            }
            if (uiFindPopup.IsOpen || uiGotoPopup.IsOpen)
            {
                e.Handled = true;
            }
#if WINDOWS8
            CoreWindow.GetForCurrentThread().RouteKeyFromUiLinesKeyDown(sender, e);
#endif
        }
        private void UiLines_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            // Supress the arrow keys (they shift the scroll viewer, and it looks bad)
            if (e.Key == VirtualKey.Up || e.Key == VirtualKey.Down)
            {
                e.Handled = true;
            }
            // Ditto Home and End
            if (e.Key == VirtualKey.Home || e.Key == VirtualKey.End)
            {
                e.Handled = true;
            }
#if WINDOWS8
            CoreWindow.GetForCurrentThread().RouteKeyFromUiLinesKeyUp(sender, e);
#endif
        }

        public EditorProgram EditorProgram { get; set; }
        private void LanguageEditor_Loaded(object sender, RoutedEventArgs e)
        {
            EditorProgram = new EditorProgram();
            uiEditorPanel.Background = EditorProgram.Language.BackgroundBrush;
            ProgramLines.Clear();
            uiLines.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDownHandler), true);

            //this.uiLines.ItemsPanelRoot.KeyDown += ItemsPanelRoot_KeyDown;
        }


#region Selection
        List<Statement> _currSelectedItems = new List<Statement>();


        private IList<Statement> GetSelectedItems()
        {
            return _currSelectedItems;
        }

        private Statement GetSelectedItemNewest()
        {
            var currSelect = GetSelectedItems();
            var mostRecent = currSelect.Count >= 1 ? currSelect[currSelect.Count - 1] : null;
            return mostRecent;
        }
        private Statement GetSelectedItemOldest()
        {
            if (_currSelectedItems.Count >= 1) return _currSelectedItems[0];
            return null;
        }
        /// <summary>
        /// Undoes the existing selection. Will return the current cursor position of the
        /// selected line that has internal focus (or -1 if none)
        /// </summary>
        /// <returns></returns>
        private int UnselectAll()
        {
            int cursorPos = -1;
            var list = GetSelectedItems();
            foreach (var statement in list)
            {
                statement.XamlShouldBeFocused = false;
                statement.XamlShouldSetCursorPos = -1;
                statement.XamlShouldSetEndCursorPos = -1;
                statement.XamlHaveSettings = true;

                var xamlStatement = statement?.XamlStatement;
                if (xamlStatement != null)
                {
                    if (xamlStatement.HasInternalFocus()) cursorPos = xamlStatement.GetCursorPos();
                    xamlStatement.UpdateFromStatementSettings();
                    xamlStatement.RemoveInternalFocus();
                }
            }
            list.Clear();
            return cursorPos;
        }

        private int IndexOfStatement (Statement statement)
        {
            for (int i=0; i< ProgramLines.Count; i++)
            {
                var line = ProgramLines[i];
                if (line == statement)
                {
                    return i;
                }
            }
            return -1;
        }

        private void SelectListOne (Statement statement, int cursorPos)
        {
            SelectList(new List<Statement>() { statement }, cursorPos);
        }
        private void SelectList (List<Statement> addedItems, int cursorPos)
        {
            var list = GetSelectedItems();
            foreach (var item in addedItems)
            {
                var statement = item as Statement;
                statement.XamlShouldBeFocused = true; // Set to true here, but force caret invisible elsewhere.
                if (statement.XamlShouldSetCursorPos < 0)
                {
                    // If it's already set, don't set again.
                    statement.XamlShouldSetCursorPos = cursorPos;
                }
                LanguageEditor.Log($"LanguageEditor:SelectList: the XamlShouldSetCursorPos is {statement.XamlShouldSetCursorPos}");
                statement.XamlHaveSettings = true;

                var xamlStatement = statement?.XamlStatement;
                if (xamlStatement != null) xamlStatement.UpdateFromStatementSettings();
                list.Add(statement);
            }
            if (addedItems.Count > 0)
            {
                mostRecentStatement = addedItems[addedItems.Count - 1] as Statement;
                if (mostRecentStatement == null)
                {
                    LanguageEditor.Log($"LanguageEditor:SelectList: mostRecentStatement set to null??");
                }
            }
        }

        private void FixupCaret()
        {
            var items = GetSelectedItems();
            bool forceInvisible = items.Count > 1;
            foreach (var item in items)
            {
                var statement = item as Statement;
                var xamlStatement = statement?.XamlStatement;
                if (xamlStatement != null)
                {
                    xamlStatement.SetForceCaretTextInvisible(forceInvisible);
                }
                else if (statement != null)
                {
                    statement.XamlShouldSetForceCaretTextInvisible = forceInvisible; // This had always been set false -- why? just a bug?
                    statement.XamlHaveSettings = true;
                }
            }
        }

        private Statement FrameworkElementToStatement (Object obj)
        {
            var fe = obj as FrameworkElement;
            while (fe != null && fe.Parent != null)
            {
                fe = fe.Parent as FrameworkElement;
            }
            // now fe points to the LanguageEditorStatement (or we're in some alternate reality).
            if (fe == null) return null;
            var les = fe as LanguageEditorStatement;
            if (les == null) return null;
            var statement = les.Tag as Statement;
            if (statement == null) return null; // more alternate reality
            return statement;
        }

        private void OnSelectItem(object sender, TappedRoutedEventArgs e)
        {
            Statement statement = FrameworkElementToStatement(e.OriginalSource);
            if (statement == null) return;

            var isShift = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
            var isCtrl = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);

            var addedItems = new List<Statement>() { statement };
            if (isCtrl)
            {
                // ^SELECT means to add this item to the end of the SelectList
                SelectList(addedItems, 0);
            }
            else if (isShift)
            {
                // SHIFT-SELECT means to add a range of statements
                // What's the index of the most recently selected item?

                var mostRecent = GetSelectedItemNewest();
                if (mostRecent == null)
                {
                    var cursorPos = UnselectAll();
                    SelectList(addedItems, cursorPos);
                }
                else
                {
                    var startIndex = IndexOfStatement(mostRecent);
                    var endIndex = IndexOfStatement(statement);
                    var delta = (startIndex < endIndex) ? 1 : -1;
                    addedItems.Clear();
                    for (int i = startIndex; i!=endIndex; i+=delta)
                    {
                        addedItems.Add(ProgramLines[i]);
                    }
                    addedItems.Add(statement);

                    var cursorPos = 0; // Don't unselect!
                    UnselectAll(); // Otherwise the first line to be selected is double-counted.
                    SelectList(addedItems, cursorPos);
                }
            }
            else
            {
                // Otherwise, clear the current selection and start over from scratch.
                var cursorPos = UnselectAll();
                SelectList(addedItems, cursorPos);
            }
            FixupCaret();
        }
#endregion

        // Special handler for key down. 
        private async void OnKeyDownHandler(object sender, KeyRoutedEventArgs e)
        {
            MyKeyEventArgs args = new MyKeyEventArgs(e);

            var statement = GetSelectedItemOldest();
            if (statement == null) return;
            if (e.Key == VirtualKey.Enter)
            {
                try
                {
                    if (statement.XamlStatement != null)
                    {
                        await statement.XamlStatement.CoreWindow_KeyDown(null, new MyKeyEventArgs(e));
                    }
                    AdapterKeyDown?.OnKeyDown(new MyKeyEventArgs(e));
                }
                catch (Exception)
                {
                    ;
                }
            }
        }
        bool ProgramIsRunning = false;
        public void Start()
        {
            var statement = mostRecentStatement;
            var items = GetSelectedItems();
            if (items.Count <= 1)
            {
                statement = GetSelectedItemOldest();
            }
            statement?.XamlStatement?.RemoveInternalFocus();
            ProgramIsRunning = true;
        }
        public void Stop()
        {
            var statement = mostRecentStatement;
            var items = GetSelectedItems();
            if (items.Count <= 1)
            {
                statement = GetSelectedItemOldest();
            }
            statement?.XamlStatement?.SetInternalFocus();
            ProgramIsRunning = false;
        }

        // Is the current focus in THIS editor? There are multiple editors possible 
        // (e.g., a program versus the sigma editor)
        private bool FocusIsInEditor(FrameworkElement start)
        {
            var el = start;
            while (el != null)
            {
                var elAsItemsControl = el as ItemsControl;
                if (elAsItemsControl == uiLines)
                {
                    // Focus element is in the editor.
                    return true;
                }
                var p = VisualTreeHelper.GetParent(el);
                el =  p as FrameworkElement;
            }
            return false;
        }
        Statement _mostRecentStatement = null;
        Statement mostRecentStatement
        {
            get { return _mostRecentStatement; }
            set { if (value == null) _mostRecentStatement = null; _mostRecentStatement = value; }
        }

        // Can be called externally to turn focus on and off.
        public bool SetFocusOnEditor (bool focus)
        {
            var statement = mostRecentStatement;
            if (GetSelectedItems().Count <= 1)
            {
                statement = GetSelectedItemOldest();
            }
            if (statement == null)
            {
                return false;
            }
            var oldvalue = statement?.XamlStatement?.SetActLikeHasFocus(focus);
            return oldvalue.HasValue ? oldvalue.Value : false;
        }
        public async void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            var el = FocusManager.GetFocusedElement();
            var focus = FocusIsInEditor(el as FrameworkElement);

            if (EditorName == "Sigma")
            {
                ;
            }
            Log($"KEY:LanguageEditor:CoreWindow_KeyDown: {EditorName} [FIRST] [FOCUS={focus}] args {args.VirtualKey} mostRecentStatement=<<{mostRecentStatement?.AsSimpleText}>> selectedItems.Count={GetSelectedItems().Count} ProgramIsRunning {ProgramIsRunning} ()");
            if (!focus)
            {
                return; // We don't have focus, so we shouldn't handle the character.
            }

            // FOR EXAMPLE: if the program is running with INKEY$, and the user
            // presses a key, don't add it to the program!
            if (ProgramIsRunning || uiFindPopup.IsOpen || uiGotoPopup.IsOpen)
            {
                //args.Handled = true;
                return;
            }
            var statement = mostRecentStatement;
            if (GetSelectedItems().Count <= 1)
            {
                statement = GetSelectedItemOldest();
            }
            if (statement == null)
            {
                Log($"KEY:LanguageEditor:CoreWindow_KeyDown: {EditorName} statement is null; returning.");
                return;
            }

            int nSelected = GetSelectedItems().Count;

            // Keep track of the index of the newest selected item at the start of the loop.
            // It will be used at the end of the loop to ensure that there's at least one
            // selected statement.
            var newest = GetSelectedItemNewest();
            int newestIndex = (newest == null) ? -1 : ProgramLines.IndexOf(newest);
            

            bool isShift = (_coreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down));
            bool isCtrl = (_coreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down));

            bool handled = false;
            switch (args.VirtualKey)
            {
                case VirtualKey.F: // ^F is Find
#if WINDOWS8
                    if (isCtrl)
                    {

                        var lastFindString = GetLastFind();
                        uiFindPopupText.Text = lastFindString;
                        statement?.XamlStatement?.SetActLikeHasFocus(false);
                        uiFindPopup.IsOpen = true;
                        uiFindPopupText.Focus(FocusState.Programmatic);
                        while (uiFindPopup.IsOpen)
                        {
                            await System.Threading.Tasks.Task.Delay(100);
                        }
                        statement?.XamlStatement?.SetActLikeHasFocus(true);
                        var findString = uiFindPopupText.Text;
                        if (Windows8FindOk)
                        {
                            FindRequested(statement, findString, FindDirection.Forward);
                        }
                        handled = true;
                    }
#else
                    if (isCtrl )
                    {
                        var lastFindString = GetLastFind();
                        var findDialog = new EditFindDialog(lastFindString);
                        statement?.XamlStatement?.SetActLikeHasFocus(false);
                        var result = await findDialog.ShowAsync();
                        statement?.XamlStatement?.SetActLikeHasFocus(true);
                        if (findDialog.ExitViaReturn || result == ContentDialogResult.Primary)
                        {
                            var findString = findDialog.Text;
                            FindRequested(statement, findString, FindDirection.Forward);
                        }
                        handled = true;
                    }
#endif
                    break;

                case VirtualKey.G: // ^G is Goto (goto line, line#, or function name)
#if WINDOWS8
                    if (isCtrl)
                    {
                        uiFindPopupText.Text = GetLastGoto();
                        statement?.XamlStatement?.SetActLikeHasFocus(false);
                        uiGotoPopup.IsOpen = true;
                        uiGotoPopupText.Focus(FocusState.Programmatic);
                        while (uiFindPopup.IsOpen)
                        {
                            await System.Threading.Tasks.Task.Delay(100);
                        }
                        statement?.XamlStatement?.SetActLikeHasFocus(true);
                        var gotoString = uiGotoPopupText.Text;
                        if (Windows8GotoOk)
                        {
                            GotoRequested(gotoString, EditGotoDialog.GotoType.Line);
                        }
                        handled = true;
                    }
#else
                    if (isCtrl)
                    {
                        {
                            var lastGotoString = GetLastGoto();
                            var lastGotoType = GetLastGotoType();
                            var gotoDialog = new EditGotoDialog(lastGotoString, lastGotoType);
                            statement?.XamlStatement?.SetActLikeHasFocus(false);
                            var result = await gotoDialog.ShowAsync();
                            statement?.XamlStatement?.SetActLikeHasFocus(true);
                            if (gotoDialog.ExitViaReturn || result == ContentDialogResult.Primary)
                            {
                                var gotoString = gotoDialog.Text;
                                GotoRequested(gotoString, gotoDialog.CurrGotoType);
                            }
                        }
                    handled = true;
                    }
#endif
                    break;

                case VirtualKey.F3: // search again (shift-F3=backwards)
                    if (args.Handled) return;
                    //if (Statement != null && Statement.Editor != null)
                    {
                        args.Handled = true;
                        var lastFindString = GetLastFind();
                        var direction = isShift ? FindDirection.Backwards : FindDirection.Forward;
                        FindRequested(statement, lastFindString, direction);
                        handled = true;
                    }
                    break;
                case VirtualKey.C: // ^C copy
                    if (isCtrl)
                    {
                        if (nSelected <= 1)
                        {
                            await statement?.XamlStatement.CoreWindow_KeyDown(sender, new MyKeyEventArgs(args));
                        }
                        else
                        {
                            var text = GetSelectedText();
                            var dp = new DataPackage();
                            dp.SetText(text);
                            dp.RequestedOperation = DataPackageOperation.Copy;
                            Clipboard.SetContent(dp);
                        }
                        handled = true;
                    }
                    break;

                case VirtualKey.V: // ^V paste
                    if (isCtrl)
                    {
                        var shouldReplaceLines = false;
                        var replaceText = "";
                        if (nSelected <= 1)
                        {
                            DataPackageView dataPackageView = Clipboard.GetContent();
                            if (dataPackageView.Contains(StandardDataFormats.Text))
                            {
                                string text = await dataPackageView.GetTextAsync();
                                var hasCR = text.Contains('\n');
                                if (statement?.XamlStatement == null)
                                {
                                    shouldReplaceLines = true;
                                }
                                if (hasCR)
                                {
                                    replaceText = statement?.XamlStatement.CalculateEffectOfReplacingText(text);
                                    shouldReplaceLines = true;
                                }
                                else
                                {
                                    // Otherwise the statement can handle it.
                                    await statement?.XamlStatement.CoreWindow_KeyDown(sender, new MyKeyEventArgs(args));
                                }
                            }
                        }
                        else
                        {
                            DataPackageView dataPackageView = Clipboard.GetContent();
                            if (dataPackageView.Contains(StandardDataFormats.Text))
                            {
                                replaceText = await dataPackageView.GetTextAsync();
                                shouldReplaceLines = true;
                            }
                        }

                        if (shouldReplaceLines)
                        {
                            var duplist = GetSelectedItems();

                            // What's the earliest selected statement? They aren't always in order!
                            var earliestIdx = Int32.MaxValue;
                            foreach (var item in duplist)
                            {
                                var lineIdx = ProgramLines.IndexOf(item as Statement);
                                if (lineIdx < earliestIdx) earliestIdx = lineIdx;
                            }

                            //var firstline = GetSelectedItems()[0] as Statement;
                            //var idx = earliestIdx;
                            if (earliestIdx < 0 || earliestIdx > ProgramLines.Count)  break; // <========================== SelectedItems isn't part of ProgramLines??

                            // Remove all of the selected items
                            foreach (var item in duplist)
                            {
                                var itemStatement = item as Statement;
                                ProgramLines.Remove(itemStatement);
                            }
                            UnselectAll();

                            var idx = earliestIdx;
                            var statements = Parse(replaceText);
                            foreach (var item in statements)
                            {
                                ProgramLines.Insert(idx++, item);
                            }
                            var newidx = Math.Min(earliestIdx, ProgramLines.Count - 1);
                            if (newidx >= 0)
                            {
                                SelectListOne(ProgramLines[newidx], 0);
                                FixupCaret();
                            }
                        }
                        handled = true;
                    }
                    break;

                case VirtualKey.X: // ^X cut
                    if (isCtrl)
                    {
                        if (nSelected <= 1)
                        {
                            await statement?.XamlStatement.CoreWindow_KeyDown(sender, new MyKeyEventArgs(args));
                        }
                        else
                        {
                            var text = GetSelectedText();
                            var dp = new DataPackage();
                            dp.SetText(text);
                            dp.RequestedOperation = DataPackageOperation.Copy;
                            Clipboard.SetContent(dp);
                            //Make dup of list. Otherwise, when we remove the statements,
                            //the selectedItems enum shrinks!
                            var duplist = GetSelectedItems();
                            foreach (var item in duplist)
                            {
                                var itemStatement = item as Statement;
                                ProgramLines.Remove(itemStatement);
                            }
                            UnselectAll();
                        }
                        handled = true;
                    }
                    break;
                case VirtualKey.Delete:
                    if (nSelected <= 1)
                    {
                        if (statement != null && statement.XamlStatement != null)
                        {
                            await statement.XamlStatement.CoreWindow_KeyDown(sender, new MyKeyEventArgs(args));
                        }
                    }
                    else
                    {
                        // Means we should delete all of the lines.
                        var duplist = GetSelectedItems();
                        foreach (var item in duplist)
                        {
                            var itemStatement = item as Statement;
                            ProgramLines.Remove(itemStatement);
                        }
                        UnselectAll();
                    }
                    handled = true;
                    break;
                case (VirtualKey)219: // ^[ = indent = Left square bracket.
                    if (isCtrl)
                    {
                        if (nSelected <= 1)
                        {
                            await statement?.XamlStatement.CoreWindow_KeyDown(sender, new MyKeyEventArgs(args));
                        }
                        else
                        {
                            foreach (var item in GetSelectedItems())
                            {
                                var currStatement = item as Statement;
                                await currStatement?.XamlStatement.CoreWindow_KeyDown(sender, new MyKeyEventArgs(args));
                            }
                        }
                        FixupCaret();
                        args.Handled = true;
                        handled = true;
                    }
                    break;
                case (VirtualKey)221: // ^] = un-indent = Right square bracket.
                    if (isCtrl)
                    {
                        if (nSelected <= 1)
                        {
                            await statement?.XamlStatement.CoreWindow_KeyDown(sender, new MyKeyEventArgs(args));
                        }
                        else
                        {
                            foreach (var item in GetSelectedItems())
                            {
                                var currStatement = item as Statement;
                                await currStatement?.XamlStatement.CoreWindow_KeyDown(sender, new MyKeyEventArgs(args));
                            }
                        }
                        FixupCaret();
                        args.Handled = true;
                        handled = true;
                    }
                    break;

                case VirtualKey.Down:
                    {
                        Log($"KEY:LanguageEditor:KeyDown {Name} key=<<{args.VirtualKey}>> for statement {statement?.AsSimpleText}");
                        var selectionType = isShift ? SelectionType.AddToSelection : SelectionType.NewSelection;
                        SelectWithArrowKeys(SelectDirection.Down, selectionType);
                        handled = true;
                    }
                    break;

                case VirtualKey.Up:
                    {
                        Log($"KEY:LanguageEditor:KeyUp {Name} key=<<{args.VirtualKey}>> for statement {statement?.AsSimpleText}");
                        var selectionType = isShift ? SelectionType.AddToSelection : SelectionType.NewSelection;
                        SelectWithArrowKeys(SelectDirection.Up, selectionType);
                        handled = true;
                    }
                    break;
            }

            if (!handled)
            {
                try
                {
                    Log($"KEY:LanguageEditor:KeyDown {args.VirtualKey} for statement <<{statement?.AsSimpleText}>>");
                    if (statement != null && statement.XamlStatement != null)
                    {
                        // Will happen because there's both a program editor and a sigma editor.
                        // Only one will have a statement.
                        await statement.XamlStatement.CoreWindow_KeyDown(sender, new MyKeyEventArgs(args));
                    }
                }
                catch (Exception)
                {
                    ;
                }
            }

            // At the end, the ProgramLines array has to hold at least one item!
            if (ProgramLines.Count == 0)
            {
                var statements = Parse("\n");
                foreach (var s in statements) // I'm only expecting one :-)
                {
                    ProgramLines.Add(s);
                    SelectListOne(s, 0);
                }
            }




            if (GetSelectedItems().Count == 0)
            {
                // Select at least one item.
                if (ProgramLines.Contains (newest))
                {
                    SelectListOne(newest, 0);
                }
                else if (newestIndex >= 0 && newestIndex < ProgramLines.Count)
                {
                    SelectListOne(ProgramLines[newestIndex], 0);
                }
                else if (ProgramLines.Count > 0) // Always true because we just added a new line!
                {
                    SelectListOne(ProgramLines[ProgramLines.Count-1], 0); // Must be after the last line?
                }
            }

            // We also make sure it's in view
            bool doScrollIntoView = true; // normally we do the scroll into view
            if (args.VirtualKey == VirtualKey.LeftShift || args.VirtualKey == VirtualKey.RightShift || args.VirtualKey == VirtualKey.Shift)
            {
                doScrollIntoView = false;
            }
            newest = GetSelectedItemNewest();
            if (newest != null && doScrollIntoView) // Always true because we just selected an item
            {
                newestIndex = ProgramLines.IndexOf(newest);
                if (newestIndex >= 0)
                {
                    ScrollToEnsureInView(newestIndex);
                }
            }



            // Send the key to the Adapter in case it's needed.
            // The smart programmer will not overlap the keys.
            // The adapter uses ESCAPE F5 and F7
            AdapterKeyDown?.OnKeyDown(new MyKeyEventArgs(args));
        }


        /// <summary>
        /// Adds to the selection either up (-1) or down (+1)
        /// </summary>
        /// <param name="amount"></param>
        /// 
        enum SelectDirection {  Up, Down }
        enum SelectionType {  NewSelection, AddToSelection }
        private void SelectWithArrowKeys(SelectDirection direction, SelectionType selectionType)
        {
            var mostRecent = GetSelectedItemNewest();
            if (mostRecent == null) return; // Nothing to do if nothing is selected?
            var idx = IndexOfStatement(mostRecent);
            if (idx < 0) return; // There's a problem in the space-time continuum.
            idx += (direction == SelectDirection.Up ? -1 : 1);
            if (idx < 0 || idx >= ProgramLines.Count) return; // Already on the first/last line
            var newStatement = ProgramLines[idx];
            var currPos = -1;
            if (selectionType == SelectionType.NewSelection) currPos = UnselectAll(); // Remove the old selection
            SelectListOne(newStatement, currPos);
            FixupCaret();
        }

        private string GetSelectedText()
        {
            var sb = new StringBuilder();
            foreach (var item in GetSelectedItems())
            {
                var statement = item as Statement;
                sb.Append(statement.AsSimpleText);
                sb.Append("\r\n");
            }
            Log($"KEY:LanguageEditor:GetSelectedText {sb.ToString()}");
            return sb.ToString();
        }

        public void DoCombineLine(Statement statement, int direction, int finalCursorPos)
        {
            var idx = ProgramLines.IndexOf(statement);
            var nextidx = idx + direction;
            if (nextidx >= 0 && nextidx < ProgramLines.Count)
            {
                var prev = ProgramLines[idx + direction];
                var prevText = prev.AsSimpleText;
                var combined = (direction < 0) ? prevText + statement.AsSimpleText : statement.AsSimpleText + prevText;
                prev.SetText(combined);
                var goalCursorPos = (direction < 0) ? prevText.Length + finalCursorPos : finalCursorPos;
                prev.XamlShouldSetCursorPos = goalCursorPos;
                ProgramLines.RemoveAt(idx);
                UnselectAll();
                var goalSelectedStatement = ProgramLines[(nextidx < idx) ? nextidx : idx];
                // Will be set by the SelectListOne: GetSelectedItems().Add(goalSelectedStatement);
                SelectListOne(goalSelectedStatement, goalCursorPos);
            }
        }

        public void DoSplitLine(Statement statement, int position)
        {
            var line = statement.AsSimpleText;
            var idx = ProgramLines.IndexOf(statement);
            if (position < 0 || position > line.Length) return;
            if (idx < 0) return;

            if (line.Length == 0)
            {
                // User typed <CR>[blank line]
                // Just insert a blank line after the existing blank line
                // It's a special case of both position==0 and position==line.length
                var newStatement = new Statement(this, statement.LanguageColors, statement.Lexer, "");
                ProgramLines.Insert(idx+1, newStatement);

                var cursorPos = UnselectAll();
                SelectListOne(newStatement, 0);
            }
            else if (position == 0)
            {
                // User typed <CR>10 PRINT "At start"
                // Just insert a blank line.
                var newStatement = new Statement(this, statement.LanguageColors, statement.Lexer, "");
                ProgramLines.Insert(idx, newStatement);

                var cursorPos = UnselectAll();
                SelectListOne(newStatement, 0);
            }
            else if (position == line.Length)
            {
                // User typed 10 PRINT "At end"<CR>
                // Just insert a blank line.
                var newStatement = new Statement(this, statement.LanguageColors, statement.Lexer, "");
                ProgramLines.Insert(idx+1, newStatement);

                var cursorPos = UnselectAll();
                SelectListOne(newStatement, 0);
            }
            else
            {
                // User typed 10 PRINT <CR> "Rest of line"; split the line 
                var left = new Statement(this, statement.LanguageColors, statement.Lexer, line.Substring(0, position));
                var right = new Statement(this, statement.LanguageColors, statement.Lexer, line.Substring(position));
                ProgramLines.RemoveAt(idx);
                ProgramLines.Insert(idx, left);
                ProgramLines.Insert(idx+1, right);

                var cursorPos = UnselectAll();
                SelectListOne(right, 0); 
            }
        }

        // User pressed ^F on a statement. Start the find there.
        private string LastFindString = "";
        public void FindRequested(Statement startingStatement, string findString, FindDirection direction = FindDirection.Forward)
        {
            int idx = ProgramLines.IndexOf(startingStatement);
            int startPos = -1;
            int endPos = -1;
            int foundIndex = -1;
            switch (direction)
            {
                case FindDirection.Backwards:
                    if (idx <= 0) idx = 0;
                    else idx -= 1;
                    for (int i = idx; i >=0  && foundIndex < 0; i--)
                    {
                        var line = ProgramLines[i].AsSimpleText;
                        startPos = line.IndexOf(findString, 0, StringComparison.CurrentCultureIgnoreCase);
                        if (startPos >= 0)
                        {
                            endPos = startPos + findString.Length;
                            foundIndex = i;
                        }
                    }
                    break;
                case FindDirection.Forward:
                    if (idx < 0) idx = 0;
                    else idx += 1;
                    for (int i = idx; i < ProgramLines.Count && foundIndex < 0; i++)
                    {
                        var line = ProgramLines[i].AsSimpleText;
                        startPos = line.IndexOf(findString, 0, StringComparison.CurrentCultureIgnoreCase);
                        if (startPos >= 0)
                        {
                            endPos = startPos + findString.Length;
                            foundIndex = i;
                        }
                    }
                    break;
            }
            if (foundIndex >= 0)
            {
                var statement = ProgramLines[foundIndex];
                statement.XamlShouldSetCursorPos = startPos;
                statement.XamlShouldSetEndCursorPos = endPos;
                statement.XamlShouldBeFocused = true;
                statement.XamlHaveSettings = true;

                var xamlStatement = statement?.XamlStatement;
                if (xamlStatement != null) xamlStatement.UpdateFromStatementSettings();

                ScrollToEnsureInView(foundIndex);

                UnselectAll();
                GetSelectedItems().Add(statement);
            }
            LastFindString = findString;
        }

        public string GetLastFind()
        {
            return LastFindString;
        }


        // User pressed ^G which is an absolute goto.
        private EditGotoDialog.GotoType LastGotoType = EditGotoDialog.GotoType.Line;
        private string LastGotoString = "";

        // Might be: "50" meaning goto "50 REM this is line 50"
        // OR might be "45" meaning "goto line 45 of the file"
        // OR might be "DoHelp" meaning "goto the definition of FUNCTION DoHelp"
        public void GotoRequested(string gotoString, EditGotoDialog.GotoType gotoType)
        {
            int startPos = -1;
            int endPos = -1;
            int foundIndex = -1;
            int gotoValue = -1;
            bool isAllNumeric = Int32.TryParse (gotoString, out gotoValue);

            // Look for lines like "50 REM loop top"
            if (isAllNumeric)
            {
                var gotoBasicLineNumber = gotoString + " ";
                for (int i = 0; i < ProgramLines.Count && foundIndex < 0; i++)
                {
                    var line = ProgramLines[i].AsSimpleText;
                    startPos = line.IndexOf(gotoBasicLineNumber, 0, StringComparison.Ordinal); //.InvariantCulture);
                    if (startPos == 0) // Must start with the value.
                    {
                        endPos = startPos + gotoString.Length; // don't include the space.
                        foundIndex = i;
                    }
                }
            }

            if (foundIndex < 0 && !isAllNumeric)
            {
                var gotoBasicFunction = $"FUNCTION {gotoString}";
                for (int i = 0; i < ProgramLines.Count && foundIndex < 0; i++)
                {
                    var line = ProgramLines[i].AsSimpleText;
                    startPos = line.IndexOf(gotoBasicFunction, 0, StringComparison.Ordinal); //.InvariantCulture);
                    if (startPos == 0) // Must start with the value.
                    {
                        startPos += "FUNCTION ".Length;
                        endPos = startPos + gotoString.Length;
                        foundIndex = i;
                    }
                }
            }

            // Goto the nth line in the file.
            if (foundIndex < 0 && isAllNumeric)
            {
                switch (gotoType)
                {
                    case EditGotoDialog.GotoType.Line:
                        // Small and large values are still used.
                        if (gotoValue == 0) gotoValue = 1;
                        else if (gotoValue > ProgramLines.Count) gotoValue = ProgramLines.Count;

                        // GO TO is 1-based, but ProgrmLines is zero based.
                        if (gotoValue >= 1 && gotoValue <= ProgramLines.Count)
                        {
                            foundIndex = gotoValue-1;
                        }
                        break;
                    case EditGotoDialog.GotoType.Character:
                        var currLineEnd = 0;
                        for (int i=0; i<ProgramLines.Count && foundIndex < 0; i++)
                        {
                            var statement = ProgramLines[i];
                            currLineEnd += statement.AsSimpleText.Length;
                            if (currLineEnd > gotoValue)
                            {
                                foundIndex = i;
                            }
                        }
                        break;
                }
            }

            if (foundIndex >= 0)
            {
                var statement = ProgramLines[foundIndex];
                statement.XamlShouldSetCursorPos = startPos;
                statement.XamlShouldSetEndCursorPos = endPos;
                statement.XamlShouldBeFocused = true;
                statement.XamlHaveSettings = true;

                var xamlStatement = statement?.XamlStatement;
                if (xamlStatement != null) xamlStatement.UpdateFromStatementSettings();

                ScrollToEnsureInView(foundIndex);

                UnselectAll();
                GetSelectedItems().Add(statement);
            }
            LastGotoString = gotoString;
            LastGotoType = gotoType;
        }

        private void ScrollToEnsureInView(int lineIndex)
        {
            var ypos = 0.0;
            for (int i = 0; i < lineIndex; i++)
            {
                var s = ProgramLines[i];
                ypos += LanguageEditorStatement.CalculateHeight(s.NLine);
                if (s.NLine > 1)
                {
                    ; // Just a hook for debugging.
                }
            }
            var yStart = uiScroll.VerticalOffset;
            var yEnd = yStart + uiScroll.ActualHeight;
            var isVisible = ypos >= yStart && ypos <= yEnd;
            if (!isVisible)
            {
                // figure out the preferred offset.
                var ymiddle = ypos - uiScroll.ActualHeight / 2;
                if (ymiddle < 0) ymiddle = 0;
                uiScroll.ChangeView(null, ymiddle, null);
                uiScroll.UpdateLayout(); // Just in case we get called quickly before the VerticalOffset is updated.
            }
        }

        public string GetLastGoto()
        {
            return LastGotoString;
        }
        public EditGotoDialog.GotoType GetLastGotoType()
        {
            return LastGotoType;
        }

        public string GetText()
        {
            var sb = new StringBuilder();
            foreach (var line in ProgramLines)
            {
                var str = line.AsSimpleText;
                sb.Append(str);
                sb.Append('\n');
            }
            return sb.ToString();
        }

        public void InsertText (string programText)
        {
            var statement = GetSelectedItemOldest();
            if (statement == null) return;
            var xamlStatement = statement?.XamlStatement;
            if (xamlStatement != null)
            {
                xamlStatement.InsertText(programText);
            }
        }

        private IList<Statement> Parse (string programText)
        {
            var Retval = new List<Statement>();
            var lines = EditorProgram.Lexer.SplitIntoLines(programText);
            foreach (var tuple in lines)
            {
                var line = tuple.Item2;
                var statement = new Statement(this, EditorProgram.Language, EditorProgram.Lexer, line);
                Retval.Add(statement);
            }
            return Retval;
        }
        public void SetText (string programText)
        {
            var statements = Parse(programText);
            ProgramLines.Clear();
            foreach (var statement in statements)
            {
                ProgramLines.Add(statement);
            }
            Statement firstStatement = statements.FirstOrDefault();
            if (firstStatement != null) SelectListOne(firstStatement, 0);
        }

        bool Windows8FindOk = false;
        private void Windows8OnFindOK(object sender, RoutedEventArgs e)
        {
            Windows8FindOk = true;
            uiFindPopup.IsOpen = false;
        }

        private void Windows8OnFindCancel(object sender, RoutedEventArgs e)
        {
            Windows8FindOk = false;
            uiFindPopup.IsOpen = false;
        }

        bool Windows8GotoOk = false;
        private void Windows8OnGotoOK(object sender, RoutedEventArgs e)
        {
            Windows8GotoOk = true;
            uiGotoPopup.IsOpen = false;
        }

        private void Windows8OnGotoCancel(object sender, RoutedEventArgs e)
        {
            Windows8GotoOk = false;
            uiGotoPopup.IsOpen = false;
        }

        private void OnScrollPanelPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var status = this.Focus(FocusState.Pointer);
            Log($"KEY:LanguagEditor:uiLines::OnScrollPanelPointerPressed: focus status={status} ok={Windows8FindOk}/{Windows8GotoOk}");
        }
    }
}
