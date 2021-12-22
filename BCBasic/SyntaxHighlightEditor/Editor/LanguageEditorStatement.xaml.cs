using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using System.Text;
using System.Collections.Generic;

#if !WINDOWS8
using Windows.UI.Text.Core;
#endif
// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Edit
{
    public sealed partial class LanguageEditorStatement : UserControl
    {
        static Brush FocusBorderBrush = LanguageColors.SelectedBorderBrush; // new SolidColorBrush(Colors.Green);
        static Brush FocusBackgroundBrush = LanguageColors.SelectedBrush; // new SolidColorBrush(Colors.DarkRed);

        public static double CalculateHeight (int nlines)
        {
#if WINDOWS8
            return (21.5 * nlines) + 0; // Border is 1 px top + 1 px bottom but isn't part of WINDOWS8
#else
            return (21.5 * nlines) + 2; // Border is 1 px top + 1 px bottom
#endif
        }

        // The _editContext lets us communicate with the input system.
        CoreTextEditContext _editContext;

        // We will use a plain text string to represent the
        // content of the custom text edit control.
        string _text
        {
            get
            {
                if (Statement == null) return String.Empty;
                return Statement.AsSimpleText;
            }
            set
            {
                if (Statement != null)
                {
                    Statement.SetText(value);
                    UpdateTextAndCaret();
                    LanguageEditor.Log($"LanguageEditorStatement:_text set {value}");
                }
            }
        }


        // If the _selection starts and ends at the same point,
        // then it represents the location of the caret (insertion point).
        CoreTextRange _selection;
        private int _LineWithCursor = 0;
        public int LineWithCaret
        {
            get { if (!_actLikeHasFocus) return -1; if (HasSelection()) return -1;  return _LineWithCursor; }
            set { _LineWithCursor = value; }
        }

        // _actLikeHasFocus keeps track of whether our control acts like it has focus.
        bool _actLikeHasFocus = false;
        public bool SetActLikeHasFocus(bool newValue)
        {
            var oldvalue = _actLikeHasFocus;
            _actLikeHasFocus = newValue;
            return oldvalue;
        }
        private bool MustSetFocusOnLoaded = false;

        // If there is a nonempty selection, then _extendingLeft is true if the user
        // is using shift+arrow to adjust the starting point of the selection,
        // or false if the user is adjusting the ending point of the selection.
        bool _extendingLeft = false;

        // The input pane object indicates the visibility of the on screen keyboard.
        // Apps can also ask the keyboard to show or hide.
        InputPane _inputPane;
        CoreWindow _coreWindow;


        Statement OldStatement = null;
        Statement Statement = null;
        private bool CaretIsForcedInvisbile = false;


        static int NDataContextChanged = 0;
        public LanguageEditorStatement()
        {
            this.InitializeComponent();
#if !WINDOWS8
            uiBorderPanel.BorderThickness = new Thickness(1);
            uiBorderPanel.CornerRadius = new CornerRadius(2);
#endif

            this.DataContextChanged += (s, e) =>
            {
                NDataContextChanged += 1;
                var statement = this.DataContext as Statement;
                if (statement == null) return;
                if (statement != OldStatement)
                {
                    LanguageEditor.Log($"KEY:LanguageEditorStatement:DataContextChanged newstatement {statement.AsSimpleText}");
                    SetStatement(statement);
                }
                else
                {
                    LanguageEditor.Log($"KEY:LanguageEditorStatement:DataContextChanged but statement is same {statement.AsSimpleText}");
                    ; // setting it to itself?
                }
                OldStatement = Statement;
            };

            this.Loaded += (s, e) =>
            {
                if (MustSetFocusOnLoaded)
                {
                    bool didFocus = this.Focus(FocusState.Programmatic);
                    LanguageEditor.Log($"LanguageEditorStatement:MustSetFocusOnLoaded {Name} focusstatus={didFocus}");
                }
            };
            this.LostFocus += (s, e) =>
            {
                var oldStatement = e.OriginalSource as LanguageEditorStatement;
                var oldText = oldStatement?._text ?? "(other)";
                LanguageEditor.Log($"LanguageEditorStatement:LostFocus {Name} text={_text} oldText={oldText} tree::{GetCurrentFocus()}");
                _editContext.NotifyFocusLeave();
                RemoveEditContextCallbacks();
            };
            this.GotFocus += (s, e) =>
            {
                LanguageEditor.Log($"LanguageEditorStatement:GotFocus {Name} text={_text}");
                SetEditContextCallbacks();
                _editContext.NotifyFocusEnter();
            };


            // The CoreTextEditContext processes text input, but other keys are
            // the apps's responsibility.
            _coreWindow = CoreWindow.GetForCurrentThread();

            SetEditContextCallbacks();

            // Set our initial UI.
            UpdateTextAndCaret();
            UpdateFocusUI();
        }

        private void SetEditContextCallbacks()
        {
            LanguageEditor.Log($"LanguageEditorStatement:SetEditContextCallbacks {Name}");

            // Create a CoreTextEditContext for our custom edit control.
            CoreTextServicesManager manager = CoreTextServicesManager.GetForCurrentView();
            _editContext = manager.CreateEditContext();

            // Get the Input Pane so we can programmatically hide and show it.
            _inputPane = InputPane.GetForCurrentView();

            // For demonstration purposes, this sample sets the Input Pane display policy to Manual
            // so that it can manually show the software keyboard when the control gains focus and
            // dismiss it when the control loses focus. If you leave the policy as Automatic, then
            // the system will hide and show the Input Pane for you. Note that on Desktop, you will
            // need to implement the UIA text pattern to get expected automatic behavior.
            _editContext.InputPaneDisplayPolicy = CoreTextInputPaneDisplayPolicy.Automatic;

            // Set the input scope to Text because this text box is for any text.
            // This also informs software keyboards to show their regular
            // text entry layout.  There are many other input scopes and each will
            // inform a keyboard layout and text behavior.
            _editContext.InputScope = CoreTextInputScope.Text;

            // The system raises this event to request a specific range of text.
            _editContext.TextRequested += EditContext_TextRequested;

            // The system raises this event to request the current selection.
            _editContext.SelectionRequested += EditContext_SelectionRequested;

            // The system raises this event when it wants the edit control to remove focus.
            _editContext.FocusRemoved += EditContext_FocusRemoved;

            // The system raises this event to update text in the edit control.
            _editContext.TextUpdating += EditContext_TextUpdating;

            // The system raises this event to change the selection in the edit control.
            _editContext.SelectionUpdating += EditContext_SelectionUpdating;

            // The system raises this event when it wants the edit control
            // to apply formatting on a range of text.
            // (formatting is seperate) _editContext.FormatUpdating += EditContext_FormatUpdating;

            // The system raises this event to request layout information.
            // This is used to help choose a position for the IME candidate window.
            _editContext.LayoutRequested += EditContext_LayoutRequested;

            // The system raises this event to notify the edit control
            // that the string composition has started.
            _editContext.CompositionStarted += EditContext_CompositionStarted;

            // The system raises this event to notify the edit control
            // that the string composition is finished.
            _editContext.CompositionCompleted += EditContext_CompositionCompleted;

            // The system raises this event when the NotifyFocusLeave operation has
            // completed. Our sample does not use this event.
            _editContext.NotifyFocusLeaveCompleted += EditContext_NotifyFocusLeaveCompleted;
        }

        private void RemoveEditContextCallbacks()
        {
            LanguageEditor.Log($"LanguageEditorStatement:RemoveEditContextCallbacks {Name}");

            // The system raises this event to request a specific range of text.
            _editContext.TextRequested -= EditContext_TextRequested;

            // The system raises this event to request the current selection.
            _editContext.SelectionRequested -= EditContext_SelectionRequested;

            // The system raises this event when it wants the edit control to remove focus.
            _editContext.FocusRemoved -= EditContext_FocusRemoved;

            // The system raises this event to update text in the edit control.
            _editContext.TextUpdating -= EditContext_TextUpdating;

            // The system raises this event to change the selection in the edit control.
            _editContext.SelectionUpdating -= EditContext_SelectionUpdating;

            // The system raises this event when it wants the edit control
            // to apply formatting on a range of text.
            // (formatting is seperate) _editContext.FormatUpdating -= EditContext_FormatUpdating;

            // The system raises this event to request layout information.
            // This is used to help choose a position for the IME candidate window.
            _editContext.LayoutRequested -= EditContext_LayoutRequested;

            // The system raises this event to notify the edit control
            // that the string composition has started.
            _editContext.CompositionStarted -= EditContext_CompositionStarted;

            // The system raises this event to notify the edit control
            // that the string composition is finished.
            _editContext.CompositionCompleted -= EditContext_CompositionCompleted;

            // The system raises this event when the NotifyFocusLeave operation has
            // completed. Our sample does not use this event.
            _editContext.NotifyFocusLeaveCompleted -= EditContext_NotifyFocusLeaveCompleted;
        }

        private void EditContext_NotifyFocusLeaveCompleted(CoreTextEditContext sender, object args)
        {
            LanguageEditor.Log($"LanguageEditorStatement:EditContext_NotifyFocusLeaveCompleted");
        }

        private Run[] SplitRun (Run run, int split)
        {
            if (split > run.Text.Length)
            {
                split = run.Text.Length;
            }
            var left = new Run()
            {
                Text = run.Text.Substring(0, split),
                Foreground = run.Foreground
            };
            var right = new Run()
            {
                Text = run.Text.Substring(split),
                Foreground = run.Foreground
            };
            var Retval = new Run[2] { left, right };
            return Retval;
        }

        private bool IsAllSpaces(string text)
        {
            if (text.Length == 0) return false;
            foreach (var ch in text)
            {
                if (ch != ' ') return false;
            }
            return true;
        }

        private void SetCursorPos(int cursorPos, int endCursorPos = -1)
        {
            var txt = Statement.AsSimpleText;
            var maxPos = txt.Length;
            if (cursorPos > maxPos) cursorPos = maxPos; // Only as far as the end!
            _selection.StartCaretPosition = cursorPos;
            _selection.EndCaretPosition = endCursorPos >= 0 ? endCursorPos : cursorPos;
            LanguageEditor.Log($"LanguageEditorStatement:SetCursorPos {cursorPos}::{endCursorPos}  & call UpdateTextAndCaret()");
            UpdateTextAndCaret();

            LanguageEditor.Log($"LanguageEditorStatement:SetCursorPos & call NotifySelectionChanged()");
            _editContext.NotifySelectionChanged(_selection);
        }
        public int GetCursorPos ()
        {
            return _selection.StartCaretPosition;
        }

        // GIVEN the statement
        // divide the statement texts words into three runs: Left, Selection, and Right
        // based on the current selection
        private void UpdateTextRuns()
        {
            //Statement statement = Statement;
            //int cursorPos = _selection.StartCaretPosition;
            //if (Statement == null) return;
            var runs = Statement != null ? Statement.AsInlines : new List<Inline>();

            uiBorderPanel.Children.Clear();
            var currLine = new LanguageEditorLine();
            var currLineIndex = 0;
            var onLeft = true;
            var hasNoSplit = !_actLikeHasFocus && !HasSelection(); 

            var currW = 0;
            foreach (var rrun in runs)
            {
                var textRun = rrun as Run;
                var runW = (textRun == null ? 0 : textRun.Text.Length);
                var isLineBreak = rrun is LineBreak;
                var nextW = currW + runW;

                if (isLineBreak)
                {
                    uiBorderPanel.Children.Add(currLine);
                    currLine = new LanguageEditorLine();
                    currLineIndex++;
                    nextW += 1; // take into account the \n in the text.
                }
                else if (nextW < _selection.StartCaretPosition || hasNoSplit)
                {
                    // Cursor is either right after this run or is beyond this run.
                    // In either case, add to the left area.
                    currLine.AddToLeft(rrun);
                }
                else if (currW >= _selection.EndCaretPosition)
                {
                    // Cursor is to the right of the new. Put the whole run into the right area 
                    currLine.AddToRight(rrun);
                    if (onLeft)
                    {
                        // Was on left, but now on right? This is where the cursor is!
                        onLeft = false;
                        LineWithCaret = currLineIndex;
                    }
                }
                else // if (nextW > _selection.StartCaretPosition)
                {
                    // need to split the run into two.
                    // Is there anything to go into the left area?
                    if (currW < _selection.StartCaretPosition)
                    {
                        var leftSplit = SplitRun(textRun, _selection.StartCaretPosition - currW);
                        currLine.AddToLeft(leftSplit[0]);
                        var leftlen = leftSplit[0].Text.Length;
                        currW += leftlen;
                        runW -= leftlen;
                        textRun = leftSplit[1]; // the remainder of the run.
                    }
                    // The rest is split into the selection area and the right area.
                    var remainder = _selection.EndCaretPosition - currW;
                    if (remainder <= 0) // No selection at all; all goes into right area
                    {
                        currLine.AddToRight(textRun);
                    }
                    else if (remainder > runW)
                    {
                        // All goes into the selection
                        currLine.AddToSelection(textRun);
                    }
                    else // Must split between Selection and Right
                    {
                        var selectSplit = SplitRun(textRun, remainder);
                        currLine.AddToSelection(selectSplit[0]);
                        currLine.AddToRight(selectSplit[1]);
                    }

                    // Was on left, but now on right? This is where the cursor is!
                    onLeft = false;
                    LineWithCaret = currLineIndex;
                }
                currW = nextW;
            }

            uiBorderPanel.Children.Add(currLine);

            foreach (var item in uiBorderPanel.Children)
            {
                var line = item as LanguageEditorLine;
                line.FixLastRuns();
            }
        }

        private void SetStatement(Statement statement)
        {
            CaretIsForcedInvisbile = false;
            RemoveInternalFocus();
            statement.XamlStatement = this;
            Statement = statement;
            this.Tag = statement; // cross-link the two. (Later: Really? The "Statement = statement" isn't enough to "link" them?
            UpdateTextAndCaret();
            UpdateFromStatementSettings();
        }

        // The statement might have been selected before the data binding could
        // be done. In that case, do the settings "late"
        public void UpdateFromStatementSettings()
        {
            if (Statement == null) return;
            LanguageEditor.Log($"LanguageEditorStatement:UpdateFromStatementSettings");

            if (Statement.XamlHaveSettings)
            {
                Statement.XamlHaveSettings = false;
                if (Statement.XamlShouldBeFocused) SetInternalFocus();
                else RemoveInternalFocus();
                // Remove the selection
                if (HasSelection())
                {
                    SetCursorPos(_selection.StartCaretPosition, -1);
                }
                if (Statement.XamlShouldSetCursorPos >= 0)
                {
                    SetCursorPos(Statement.XamlShouldSetCursorPos, Statement.XamlShouldSetEndCursorPos);
                    Statement.XamlShouldSetCursorPos = -1;
                    Statement.XamlShouldSetEndCursorPos = -1;
                }
                if (Statement.XamlShouldSetForceCaretTextInvisible.HasValue)
                {
                    SetForceCaretTextInvisible(Statement.XamlShouldSetForceCaretTextInvisible.Value);
                    Statement.XamlShouldSetForceCaretTextInvisible = null;
                }
            }
        }

        public void SetPreferredXamlCursorPos(int startPos, int endPos)
        {
            LanguageEditor.Log($"PTR:LanguageEditorStatement:SetPreferredXamlCursorPos {startPos}::{endPos} actLikeFocus {_actLikeHasFocus}");
            if (Statement == null) return;
            if (_actLikeHasFocus)
            {
                SetCursorPos(startPos, endPos);
                //Used to be this other code. But it doesn't work when you set the
                // cursor to a line that's already selected.
                //_selection.StartCaretPosition = startPos;
                //_selection.EndCaretPosition = endPos;
                //UpdateTextAndCaret();
            }
            else
            {
                Statement.XamlHaveSettings = true;
                Statement.XamlShouldSetCursorPos = startPos;
                Statement.XamlShouldSetEndCursorPos = endPos;
            }
        }

#region Focus management
        void ZZZCoreWindow_PointerPressed(CoreWindow sender, PointerEventArgs args)
        {
            // See whether the pointer is inside or outside the control.
            Rect contentRect = GetElementRect(uiBorderPanel);
            if (contentRect.Contains(args.CurrentPoint.Position))
            {
                // Tell XAML that this element has focus, so we don't have two
                // focus elements. That is the extent of our integration with XAML focus.
                Focus(FocusState.Programmatic);

                SetInternalFocus();
            }
            else
            {
                // The user tapped outside the control. Remove focus.
                RemoveInternalFocus();
            }
        }

        public void SetInternalFocus()
        {
            _editContext.NotifyFocusEnter();

            if (!_actLikeHasFocus)
            {
                LanguageEditor.Log($"LanguageEditorStatement:SetInternalFocus called");

                // Update internal notion of focus.
                SetActLikeHasFocus(true);

                // How many times should I set the focus? SO MANY TIMES!
                bool didSet = this.Focus(FocusState.Programmatic);
                if (!didSet) MustSetFocusOnLoaded = true;
                LanguageEditor.Log($"FOCUS: Set focus on {Statement.AsSimpleText} focusstatus={didSet}");

                // Update the UI.
                UpdateTextAndCaret();
                UpdateFocusUI();

                // Notify the CoreTextEditContext that the edit context has focus,
                // so it should start processing text input.
                _editContext.NotifyFocusEnter();
            }

#if !WINDOWS8
            // Ask the software keyboard to show.  The system will ultimately decide if it will show.
            // For example, it will not show if there is a keyboard attached.
            // Windows 8: no TryShow available.
            _inputPane.TryShow();
#endif
        }

        public void RemoveInternalFocus()
        {
            if (_actLikeHasFocus)
            {
                LanguageEditor.Log($"LanguageEditorStatement:RemoveInternalFocus called");

                //Notify the system that this edit context is no longer in focus
                _editContext.NotifyFocusLeave();
                RemoveInternalFocusWorker();
            }
        }

        public bool HasInternalFocus()
        {
            return _actLikeHasFocus;
        }

        void RemoveInternalFocusWorker()
        {
            //Update the internal notion of focus
            SetActLikeHasFocus(false);

#if !WINDOWS8
            // Ask the software keyboard to dismiss.
            _inputPane.TryHide();
#endif

            // Update our UI.
            UpdateTextAndCaret();
            UpdateFocusUI();
        }

        void EditContext_FocusRemoved(CoreTextEditContext sender, object args)
        {
            LanguageEditor.Log($"EDITCONTEXT:FocusRemoved");
            RemoveInternalFocusWorker();
        }

        void Element_Unloaded(object sender, RoutedEventArgs e)
        {
            // Used to unset the .Keydown and .PointerPressed values (CoreWindow_Keydown, etc.)
        }

#endregion

#region Text management

        public void InsertText (string str)
        {
            CoreTextRange range = NormalizeSelection(_selection);
            ReplaceText(range, str);
            LanguageEditor.Log($"KEY:LanguageEditorStatement:InsertText {str}");
        }

        public string CalculateEffectOfReplacingText (string text)
        {
            CoreTextRange modifiedRange = NormalizeSelection(_selection);
            var newText = _text.Substring(0, modifiedRange.StartCaretPosition) +
              text +
              _text.Substring(modifiedRange.EndCaretPosition);
            LanguageEditor.Log($"KEY:LanguageEditorStatement:CalculateEffectOfReplacingText {text} ==> {newText}");
            return newText;
        }

        // Replace the text in the specified range.
        void ReplaceText(CoreTextRange modifiedRange, string text)
        {
            // Modify the internal text store.
            _text = _text.Substring(0, modifiedRange.StartCaretPosition) +
              text +
              _text.Substring(modifiedRange.EndCaretPosition);

            // Move the caret to the end of the replacement text.
            _selection.StartCaretPosition = modifiedRange.StartCaretPosition + text.Length;
            _selection.EndCaretPosition = _selection.StartCaretPosition;

            // Update the selection of the edit context.  There is no need to notify the system
            // of the selection change because we are going to call NotifyTextChanged soon.
            UpdateTextAndCaret();

            // Let the CoreTextEditContext know what changed.
            LanguageEditor.Log($"KEY:LanguageEditorStatement:ReplaceText {text} [and NotifyTextChanged]");
            _editContext.NotifyTextChanged(modifiedRange, text.Length, _selection);
        }

        bool HasSelection()
        {
            return _selection.StartCaretPosition != _selection.EndCaretPosition;
        }

        // Change the selection without notifying CoreTextEditContext of the new selection.
        void SetSelection(CoreTextRange selection)
        {
            if (selection.StartCaretPosition > selection.EndCaretPosition)
            {
                var start = selection.StartCaretPosition;
                selection.StartCaretPosition = selection.EndCaretPosition;
                selection.EndCaretPosition = start;
            }
            _selection = selection;
            LanguageEditor.Log($"KEY:LanguageEditorStatement:SetSelection {selection.StartCaretPosition}::{selection.EndCaretPosition} [and UpdateTextAndCaret]");
            UpdateTextAndCaret();
        }

        // Change the selection and notify CoreTextEditContext of the new selection.
        void SetSelectionAndNotify(CoreTextRange selection)
        {
            SetSelection(selection);
            LanguageEditor.Log($"KEY:LanguageEditorStatement:SetSelectionAndNotify {selection.StartCaretPosition}::{selection.EndCaretPosition} [and NotifySelectionChanged]");
            _editContext.NotifySelectionChanged(_selection);
        }

        // Return the specified range of text. Note that the system may ask for more text
        // than exists in the text buffer.
        void EditContext_TextRequested(CoreTextEditContext sender, CoreTextTextRequestedEventArgs args)
        {
            CoreTextTextRequest request = args.Request;
            if (request.Range.StartCaretPosition >= _text.Length)
            {
                LanguageEditor.Log($"EDITCONTEXT:TextRequested: StartCaretPosition {request.Range.StartCaretPosition} > _text.Length {_text.Length} ({_text})");
                request.Text = "";
                return;
            }

            request.Text = _text.Substring(
                request.Range.StartCaretPosition,
                Math.Min(request.Range.EndCaretPosition, _text.Length) - request.Range.StartCaretPosition);
            LanguageEditor.Log($"KEY:LanguageEditorStatement:TextRequested {request.Range.StartCaretPosition}::{request.Range.EndCaretPosition}  {request.Text}");

        }

        // Return the current selection.
        void EditContext_SelectionRequested(CoreTextEditContext sender, CoreTextSelectionRequestedEventArgs args)
        {
            CoreTextSelectionRequest request = args.Request;
            request.Selection = _selection;
            LanguageEditor.Log($"KEY:LanguageEditorStatement:SelectionRequested {_selection.StartCaretPosition}");
        }

        void EditContext_TextUpdating(CoreTextEditContext sender, CoreTextTextUpdatingEventArgs args)
        {
            LanguageEditor.Log($"KEY:LanguageEditorStatement:TextUpdating {args.Range.StartCaretPosition}::{args.Range.EndCaretPosition}   {args.Text}");
            //            CoreTextRange range = NormalizeSelection(_selection);

            CoreTextRange range = NormalizeSelection(args.Range);
            string newText = args.Text;
            CoreTextRange newSelection = args.NewSelection;

            // Modify the internal text store.
            _text = _text.Substring(0, range.StartCaretPosition) +
                newText +
                _text.Substring(Math.Min(_text.Length, range.EndCaretPosition));

            // You can set the proper font or direction for the updated text based on the language by checking
            // args.InputLanguage.  We will not do that in this sample.

            // Modify the current selection.
            newSelection.EndCaretPosition = newSelection.StartCaretPosition;

            // Update the selection of the edit context. There is no need to notify the system
            // because the system itself changed the selection.
            SetSelection(newSelection);
        }

        void EditContext_SelectionUpdating(CoreTextEditContext sender, CoreTextSelectionUpdatingEventArgs args)
        {
            LanguageEditor.Log($"KEY:LanguageEditorStatement:SelectionUpdating {args.Selection.StartCaretPosition}");
            // Set the new selection to the value specified by the system.
            CoreTextRange range = args.Selection;

            // Update the selection of the edit context. There is no need to notify the system
            // because the system itself changed the selection.
            SetSelection(range);
        }
#endregion

#region Formatting and layout


        static Rect ScaleRect(Rect rect, double scale)
        {
            rect.X *= scale;
            rect.Y *= scale;
            rect.Width *= scale;
            rect.Height *= scale;
            return rect;
        }

        void EditContext_LayoutRequested(CoreTextEditContext sender, CoreTextLayoutRequestedEventArgs args)
        {
            LanguageEditor.Log($"       EditContext: LayoutRequested for {Name}");

            CoreTextLayoutRequest request = args.Request;

            // Get the screen coordinates of the entire control and the selected text.
            // This information is used to position the IME candidate window.

            // First, get the coordinates of the edit control and the selection
            // relative to the Window.
            Rect contentRect = GetElementRect(uiBorderPanel);
            Rect selectionRect = GetElementRect((uiBorderPanel.Children[0] as LanguageEditorLine).GetSelection());

            // Next, convert to screen coordinates in view pixels.
            Rect windowBounds = Window.Current.CoreWindow.Bounds;
            contentRect.X += windowBounds.X;
            contentRect.Y += windowBounds.Y;
            selectionRect.X += windowBounds.X;
            selectionRect.Y += windowBounds.Y;

            // Finally, scale up to raw pixels.
            double scaleFactor = 1.0;
#if !WINDOWS8
            scaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
#endif

            contentRect = ScaleRect(contentRect, scaleFactor);
            selectionRect = ScaleRect(selectionRect, scaleFactor);

            // This is the bounds of the selection.
            // Note: If you return bounds with 0 width and 0 height, candidates will not appear while typing.
            if (selectionRect.Height == 0) selectionRect.Height = contentRect.Height;
            if (selectionRect.Width == 0) selectionRect.Width = 2; // NOTE: why 2? Why not?
            request.LayoutBounds.TextBounds = selectionRect;

            //This is the bounds of the whole control
            request.LayoutBounds.ControlBounds = contentRect;
            LanguageEditor.Log($"EDITCONTEXT:LayoutRequested result TextBound {selectionRect.X},{selectionRect.Y},W={selectionRect.Width},H={selectionRect.Height}    ControlBounds TextBound {contentRect.X},{contentRect.Y},W={contentRect.Width},H={contentRect.Height}");

        }
#endregion

#region Input
        // This indicates that an IME has started composition.  If there is no handler for this event,
        // then composition will not start.
        void EditContext_CompositionStarted(CoreTextEditContext sender, CoreTextCompositionStartedEventArgs args)
        {
            LanguageEditor.Log($"EDITCONTEXT:CompositionStarted");
        }

        void EditContext_CompositionCompleted(CoreTextEditContext sender, CoreTextCompositionCompletedEventArgs args)
        {
            LanguageEditor.Log($"EDITCONTEXT:CompositionCompleted");
        }
        private CoreTextRange NormalizeSelection (CoreTextRange selection)
        {
            CoreTextRange range = selection;
            var str = _text;
            if (range.StartCaretPosition > str.Length) range.StartCaretPosition = str.Length;
            if (range.StartCaretPosition < 0) range.StartCaretPosition = 0;
            if (range.EndCaretPosition > str.Length) range.EndCaretPosition = str.Length;
            if (range.EndCaretPosition < 0) range.EndCaretPosition = 0;
            return range;
        }

        private string ConvertKeyToKeyboardKey(MyKeyEventArgs args, bool isShift)
        {
            // See http://cherrytree.at/misc/vk.htm for a table of keys
            string Retval = "";
            if (args.VirtualKey >= VirtualKey.A && args.VirtualKey <= VirtualKey.Z)
            {
                char startChar = isShift ? 'A' : 'a';
                var code = (char)((args.VirtualKey - VirtualKey.A) + startChar);
                Retval = code.ToString();
            }
            else if (args.VirtualKey >= VirtualKey.Number0 && args.VirtualKey <= VirtualKey.Number9)
            {
                if (isShift)
                {
                    switch (args.VirtualKey)
                    {
                        case VirtualKey.Number0: Retval = ")"; break;
                        case VirtualKey.Number1: Retval = "!"; break;
                        case VirtualKey.Number2: Retval = "@"; break;
                        case VirtualKey.Number3: Retval = "#"; break;
                        case VirtualKey.Number4: Retval = "$"; break;
                        case VirtualKey.Number5: Retval = "^"; break;
                        case VirtualKey.Number6: Retval = "&"; break;
                        case VirtualKey.Number7: Retval = "*"; break;
                        case VirtualKey.Number8: Retval = "*"; break;
                        case VirtualKey.Number9: Retval = "("; break;
                    }
                }
                else
                {
                    var code = (char)((args.VirtualKey - VirtualKey.Number0) + '0');
                    Retval = code.ToString();
                }
            }
            else if (args.VirtualKey == VirtualKey.Space)
            {
                Retval = " ";
            }
            else if (args.VirtualKey >= VirtualKey.NumberPad0 && args.VirtualKey <= VirtualKey.NumberPad9)
            {
                var code = (char)((args.VirtualKey - VirtualKey.NumberPad0) + '0');
                Retval = code.ToString();
            }
            else if (args.VirtualKey == (VirtualKey)186)
            {
                Retval = isShift ? ":" : ";";
            }
            else if (args.VirtualKey == (VirtualKey)187)
            {
                Retval = isShift ? "+" : "=";
            }
            else if (args.VirtualKey == (VirtualKey)188)
            {
                Retval = isShift ? "<" : ",";
            }
            else if (args.VirtualKey == (VirtualKey)189)
            {
                Retval = isShift ? "_" : "-";
            }
            else if (args.VirtualKey == (VirtualKey)190)
            {
                Retval = isShift ? ">" : ".";
            }
            else if (args.VirtualKey == (VirtualKey)191)
            {
                Retval = isShift ? "?" : "/";
            }
            else if (args.VirtualKey == (VirtualKey)192)
            {
                Retval = isShift ? "~" : "`";
            }
            else if (args.VirtualKey == (VirtualKey)219)
            {
                Retval = isShift ? "{" : "[";
            }
            else if (args.VirtualKey == (VirtualKey)220)
            {
                Retval = isShift ? "|" : "\\";
            }
            else if (args.VirtualKey == (VirtualKey)221)
            {
                Retval = isShift ? "}" : "]";
            }
            else if (args.VirtualKey == (VirtualKey)222)
            {
                Retval = isShift ? "\"" : "'";
            }
            return Retval;
        }

        private string GetCurrentFocus()
        {
            var sb = new StringBuilder();
            var el = FocusManager.GetFocusedElement() as FrameworkElement;
            while (el != null)
            {
                if (el.Name == "")
                {
                    sb.Append($"::FEType={el.GetType().ToString()} ");
                }
                else
                {
                    sb.Append($"::Name={el.Name} ");
                }
                el = el.Parent as FrameworkElement;
            }
            return sb.ToString();
        }

        public async Task CoreWindow_KeyDown(CoreWindow sender, MyKeyEventArgs args)
        {
            // Do not process keyboard input if the custom edit control does not
            // have focus. Can happen when there are pop-ups like Find and Goto.
            if (!_actLikeHasFocus)
            {
                return;
            }
            LanguageEditor.Log($"LanguageEditorStatement:CoreWindow_KeyDown:focus tree::{GetCurrentFocus()}");


            // This holds the range we intend to operate on, or which we intend
            // to become the new selection. Start with the current selection.
            CoreTextRange range = NormalizeSelection(_selection);

            // For the purpose of this sample, we will support only the left and right
            // arrow keys and the backspace key. A more complete text edit control
            // would also handle keys like Home, End, and Delete, as well as
            // hotkeys like Ctrl+V to paste.
            //
            // Note that this sample does not properly handle surrogate pairs
            // nor does it handle grapheme clusters.

            bool isShift = (_coreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down));
            bool isCtrl = (_coreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down));
            bool handled = false;

            switch (args.VirtualKey)
            {
                case VirtualKey.Space:
                    // Seriously ridiculous: text input can't handle a space!
                    // That is, when the user presses the space bar, the Windows text 
                    // composition functions don't actually do anything.
                    LanguageEditor.Log($"KEY:LanguageEditorStatement:CoreWindow_KeyDown w/Space. Call ReplaceText ({range.StartCaretPosition}) space");
                    LanguageEditor.Log($"KEY:LanguageEditorStatement:CoreWindow_KeyDown statement is <<{Statement.AsSimpleText}>>");

                    // Spaces work now? ReplaceText(range, " ");
                    break;

                case VirtualKey.A: //^A select all
                    if (isCtrl && Statement != null)
                    {
                        var text = _text;
                        range.StartCaretPosition = 0;
                        range.EndCaretPosition = text.Length;
                        SetSelectionAndNotify(range);
                        handled = true;
                    }
                    break;
                case VirtualKey.C: // ^C copy
                    if (isCtrl && Statement != null)
                    {
                        // No selection does nothing!
                        if (HasSelection())
                        {
                            var text = _text;
                            var selectedText = text.Substring(range.StartCaretPosition, range.EndCaretPosition - range.StartCaretPosition);
                            var dp = new DataPackage();
                            dp.SetText(selectedText);
                            dp.RequestedOperation = DataPackageOperation.Copy;
                            Clipboard.SetContent(dp);
                            handled = true;
                        }
                    }
                    break;

                case VirtualKey.V: // ^V paste
                    if (isCtrl && Statement != null)
                    {
                        DataPackageView dataPackageView = Clipboard.GetContent();
                        if (dataPackageView.Contains(StandardDataFormats.Text))
                        {
                            string text = await dataPackageView.GetTextAsync();
                            ReplaceText(range, text);
                            handled = true;
                        }
                    }
                    break;

                case VirtualKey.X: // ^X cut
                    if (isCtrl && Statement != null)
                    {
                        // No selection does nothing!
                        if (HasSelection())
                        {
                            var text = _text;
                            var selectedText = text.Substring(range.StartCaretPosition, range.EndCaretPosition - range.StartCaretPosition);
                            var dp = new DataPackage();
                            dp.SetText(selectedText);
                            dp.RequestedOperation = DataPackageOperation.Copy;
                            Clipboard.SetContent(dp);
                            ReplaceText(range, "");
                            handled = true;
                        }
                    }
                    break;


                case VirtualKey.Enter:
                    if (Statement != null && Statement.Editor != null)
                    {
                        if (HasSelection())
                        {
                            ReplaceText(range, "");
                        }
                        var idx = range.StartCaretPosition;
                        args.Handled = true;
                        Statement.Editor.DoSplitLine(Statement, idx);
                        handled = true;
                    }
                    break;

                // Backspace
                case VirtualKey.Back:
                    // If there is a selection, then delete the selection.
                    if (HasSelection())
                    {
                        // Set the text in the selection to nothing.
                        ReplaceText(range, "");
                        handled = true;
                    }
                    else
                    {
                        if (range.StartCaretPosition == 0)
                        {
                            Statement.Editor.DoCombineLine(Statement, -1, range.StartCaretPosition);
                        }
                        else
                        {
                            // Delete the character to the left of the caret, if one exists,
                            // by creating a range that encloses the character to the left
                            // of the caret, and setting the contents of that range to nothing.
                            // HOWEVER if the character to the left is a "low" character, delete
                            // both it and the next char as well (because the two together
                            // form a surrogate pair)
                            range.StartCaretPosition = Math.Max(0, range.StartCaretPosition - 1);
                            var leftchar = _text[range.StartCaretPosition];
                            if (Lexer.IsLowSurrogate (leftchar))
                            {
                                var llindex = Math.Max(0, range.StartCaretPosition - 1);
                                var leftleftchar = _text[llindex];
                                if (Lexer.IsHighSurrogate (leftleftchar)) // if the index didn't change then can't be high surrogate.
                                {
                                    range.StartCaretPosition = llindex;
                                }
                            }
                            ReplaceText(range, "");
                        }
                        handled = true;
                    }
                    break;
                case VirtualKey.Delete:
                    // If there is a selection, then delete the selection.
                    // If there is a selection, then delete the selection.
                    if (HasSelection())
                    {
                        // Set the text in the selection to nothing.
                        ReplaceText(range, "");
                    }
                    else
                    {
                        var line = Statement.AsSimpleText;
                        // HOWEVER if the character to the right is a "high" character, delete
                        // both it and the next char as well (because the two together
                        // form a surrogate pair)
                        if (range.StartCaretPosition == line.Length) // end is 1 beyond the line
                        {
                            Statement.Editor.DoCombineLine(Statement, 1, range.StartCaretPosition);
                        }
                        else
                        {
                            range.EndCaretPosition = range.StartCaretPosition+1;
                            var rightchar = _text[range.EndCaretPosition-1];
                            if (Lexer.IsHighSurrogate(rightchar) && range.EndCaretPosition < _text.Length)
                            {
                                var rightrightchar = _text[range.EndCaretPosition];
                                if (Lexer.IsLowSurrogate (rightrightchar))
                                {
                                    range.EndCaretPosition += 1;
                                }

                            }

                            ReplaceText(range, "");
                        }
                    }
                    handled = true;
                    break;


                // I don't get Home and End (swallowed up by the LanguageEditor listview)

                // Nope, too hard: case (VirtualKey)191: // ^/ = REM/un-REM
                case (VirtualKey)219: // ^[ = indent = Left square bracket.
                    if (isCtrl)
                    {
                        _text = "    " + _text;
                        range.StartCaretPosition += 4;
                        range.EndCaretPosition += 4;
                        _selection = range;
                        UpdateTextRuns();
                        handled = true;
                    }
                    break;
                case (VirtualKey)221: // ^] = un-indent = Right square bracket.
                    if (isCtrl)
                    {
                        if (_text.StartsWith("    "))
                        {
                            _text = _text.Substring(4);
                            range.StartCaretPosition -= 4;
                            range.EndCaretPosition -= 4;
                            _selection = range;
                            UpdateTextRuns();
                            handled = true;
                        }
                    }
                    break;


                // Left arrow
                case VirtualKey.Home:
                case VirtualKey.Left:
                    {
                        var toEnd = args.VirtualKey == VirtualKey.Home;
                        // If this is the start of a selection, then remember which edge we are adjusting.
                        // Do it even when we don't have the shift key down because we use this with
                        // the non-selection GetDistance.
                        if (!HasSelection())
                        {
                            _extendingLeft = true;
                        }

                        // If the shift key is down, then adjust the size of the selection.
                        if (isShift)
                        {
                            // Adjust the selection and notify CoreTextEditContext.
                            if (toEnd)
                            {
                                range.StartCaretPosition = 0;
                                SetSelectionAndNotify(range);
                            }
                            else
                            {
                                var distance = GetDistance(_extendingLeft, isCtrl, true, range);
                                AdjustSelectionEndpoint(-distance);
                            }
                        }
                        else
                        {
                            // The shift key is not down. If there was a selection, then snap the
                            // caret at the left edge of the selection.
                            if (HasSelection())
                            {
                                if (toEnd)
                                {
                                    range.StartCaretPosition = 0;
                                }
                                range.EndCaretPosition = range.StartCaretPosition;
                                SetSelectionAndNotify(range);
                            }
                            else
                            {
                                // There was no selection. Move the caret left one code unit if possible.
                                var distance = GetDistance(_extendingLeft, isCtrl, true, range);
                                var newLeft = Math.Max(0, range.StartCaretPosition - distance);
                                if (toEnd) newLeft = 0;
                                range.StartCaretPosition = newLeft;
                                range.EndCaretPosition = range.StartCaretPosition;
                                SetSelectionAndNotify(range);
                            }
                        }
                    }
                    handled = true;
                    break;

                // Right arrow and END key
                case VirtualKey.End:
                case VirtualKey.Right:
                    {
                        var toEnd = args.VirtualKey == VirtualKey.End;

                        // If this is the start of a selection, then remember which edge we are adjusting.
                        // Do it even when we don't have the shift key down because we use this with
                        // the non-selection GetDistance.
                        if (!HasSelection())
                        {
                            _extendingLeft = false;
                        }
                        // If the shift key is down, then adjust the size of the selection.
                        if (isShift)
                        {
                            // Adjust the selection and notify CoreTextEditContext.
                            if (toEnd)
                            {
                                range.EndCaretPosition = _text.Length;
                                SetSelectionAndNotify(range);
                            }
                            else
                            {
                                var distance = GetDistance(_extendingLeft, isCtrl, false, range);
                                AdjustSelectionEndpoint(distance);
                            }
                        }
                        else
                        {
                            // The shift key is not down. If there was a selection, then snap the
                            // caret at the right edge of the selection.
                            if (HasSelection())
                            {
                                if (toEnd)
                                {
                                    range.EndCaretPosition = _text.Length;
                                }
                                range.StartCaretPosition = range.EndCaretPosition;
                                SetSelectionAndNotify(range);
                            }
                            else
                            {
                                // There was no selection. Move the caret right one code unit if possible.
                                var distance = GetDistance(_extendingLeft, isCtrl, false, range);
                                var newStart = Math.Min(_text.Length, range.StartCaretPosition + distance);
                                if (toEnd) newStart = _text.Length;
                                range.StartCaretPosition = newStart;
                                range.EndCaretPosition = range.StartCaretPosition;
                                SetSelectionAndNotify(range);
                            }
                        }
                        handled = true;
                    }
                    break;
            }
            if (!handled)
            {
#if WINDOWS8
                // WINDOWS8 doesn't have the fancy CoreWindow text handling. Instead we just handle it ourselves.
                // The result doesn't work with international keyboards and keys very well (to put it mildly!)
                LanguageEditor.Log($"KEY:LanguageEditorStatement:CoreWindow_KeyDown statement is <<{Statement.AsSimpleText}>>");
                if (!isCtrl)
                {
                    string replace = ConvertKeyToKeyboardKey(args, isShift);
                    if (replace != "")
                    {
                        ReplaceText(range, replace);
                        handled = true;
                    }
                }
#endif

            }
        }

        int GetDistance (bool workOnStart, bool isCtrl, bool checkToTheLeft, CoreTextRange range)
        {
            var distance = 1;
            var startPos = workOnStart ? range.StartCaretPosition : range.EndCaretPosition;
            if (!isCtrl)
            {
                var pos = checkToTheLeft ? startPos-1 : startPos;
                var text = _text;
                if (pos >= 0 && pos < text.Length)
                {
                    var ch = text[pos];
                    if (checkToTheLeft && Lexer.IsLowSurrogate(ch)) distance = 2;
                    else if (!checkToTheLeft && Lexer.IsHighSurrogate(ch)) distance = 2;
                }
            }
            if (isCtrl)
            {
                if (checkToTheLeft) distance = GetLeftDistance(startPos-1);
                else distance = GetRightDistance(startPos);
            }
            return distance;
        }

        int GetLeftDistance(int startPos)
        {
            if (startPos < 0) return 0;
            var distance = 1;
            var txt = _text;
            if (startPos >= txt.Length) startPos = txt.Length - 1;
            var startType = Char.IsLetterOrDigit(txt[startPos]);
            for (int i = startPos -1; i >= 0; i--)
            {
                var thisType = Char.IsLetterOrDigit(txt[i]);
                if (thisType != startType) break;
                distance += 1;
            }
            return distance;
        }

        int GetRightDistance (int startPos)
        {
            var distance = 1;
            var txt = _text;
            if (startPos >= txt.Length) return 0;
            if (startPos >= txt.Length) startPos = txt.Length - 1;
            var startType = Char.IsLetterOrDigit(txt[startPos]);
            for (int i = startPos + 1; i < txt.Length; i++)
            {
                var thisType = Char.IsLetterOrDigit(txt[i]);
                if (thisType != startType) break;
                distance += 1;
            }
            return distance;
        }

        // Adjust the active endpoint of the selection in the specified direction.
        void AdjustSelectionEndpoint(int direction)
        {
            CoreTextRange range = _selection;
            if (_extendingLeft)
            {
                range.StartCaretPosition = Math.Max(0, range.StartCaretPosition + direction);
            }
            else
            {
                range.EndCaretPosition = Math.Min(_text.Length, range.EndCaretPosition + direction);
            }

            SetSelectionAndNotify(range);
        }
#endregion

#region UI
        void UpdateFocusUI()
        {
            var caretVisible = CaretIsVisible;
#if !WINDOWS8
            uiBorderPanel.BorderBrush = _actLikeHasFocus ? FocusBorderBrush : null;
#endif
            uiBorderPanel.Background = _actLikeHasFocus ? FocusBackgroundBrush : null;
            for (int i=0; i<uiBorderPanel.Children.Count; i++)
            {
                var line = uiBorderPanel.Children[i] as LanguageEditorLine;
                line.SetCaretTextVisible(caretVisible && LineWithCaret == i);
            }
        }

        public void SetText(string value)
        {
            _text = value;
            UpdateTextAndCaret();
            RemoveInternalFocus();
        }

        private bool CaretIsVisible
        {
            get
            {
                if (HasSelection()) return false;
                if (!_actLikeHasFocus) return false;
                if (CaretIsForcedInvisbile) return false;
                return true;
             }
        }

        public void SetForceCaretTextInvisible(bool newValue)
        {
            if (newValue == true)
            {
                ;
            }
            LanguageEditor.Log($"LanguageEditorStatement:SetForceCaretTextInvisible {newValue} {Name} oldvalue={CaretIsForcedInvisbile} CaretIsVisible={CaretIsVisible}({HasSelection()}/{_actLikeHasFocus}/{CaretIsForcedInvisbile})");
            CaretIsForcedInvisbile = newValue;
            var caretVisible = CaretIsVisible;

            for (int i = 0; i < uiBorderPanel.Children.Count; i++)
            {
                var line = uiBorderPanel.Children[i] as LanguageEditorLine;
                line.SetCaretTextVisible(caretVisible && LineWithCaret == i);
            }
        }

        void UpdateTextAndCaret()
        {
            UpdateTextRuns(); // Bounce over to the "real" update!
            var caretVisible = CaretIsVisible;
            LanguageEditor.Log($"LanguageEditorStatement:UpdateTextAndCaret: CaretIsVisible={CaretIsVisible} NChild={uiBorderPanel.Children.Count} LineWithCaret={LineWithCaret}");
            LanguageEditor.Log($"LanguageEditorStatement:UpdateTextAndCaret caret state CaretIsVisible={CaretIsVisible}({HasSelection()}/{_actLikeHasFocus}/{CaretIsForcedInvisbile})");

            for (int i = 0; i < uiBorderPanel.Children.Count; i++)
            {
                var line = uiBorderPanel.Children[i] as LanguageEditorLine;
                line.SetCaretTextVisible(caretVisible && LineWithCaret == i);
            }

            // The raw materials we have are a string (_text) and information about
            // where the caret/selection is (_selection). We can render the control
            // any way we like.

        }

        static Rect GetElementRect(FrameworkElement element)
        {
            GeneralTransform transform = element.TransformToVisual(null);
            Point point = transform.TransformPoint(new Point());
            return new Rect(point, new Size(element.ActualWidth, element.ActualHeight));
        }
#endregion
    }
}
