using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Edit
{
    public sealed partial class LanguageEditorLine : UserControl
    {
        public LanguageEditorLine()
        {
            this.InitializeComponent();
        }

        public void AddToLeft(Inline item)
        {
            uiLeft.Inlines.Add(item);
        }

        public void AddToSelection(Inline item)
        {
            uiSelection.Inlines.Add(item);
        }

        public void AddToRight(Inline item)
        {
            uiRight.Inlines.Add(item);
        }

        private bool EndsWithSpace(string text)
        {
            if (text.Length == 0) return false;
            var ch = text[text.Length - 1];
            if (ch != ' ') return false;
            return true;
        }

        private void FixLastRun(TextBlock tb)
        {
            var last = tb.Inlines.LastOrDefault() as Run;
            if (last == null) return; // no runs at all.
            if (EndsWithSpace(last.Text))
            {
                last.Text = last.Text + "̲"; // combining low line "᳕"; or "̈"  abc̈  "̈̈"
            }
        }

        public void FixLastRuns()
        {
            FixLastRun(uiLeft);
            FixLastRun(uiSelection);
            FixLastRun(uiRight);
        }

        private string AsDebugString(TextBlock tb)
        {
            String Retval = "";
            foreach (var inline in tb.Inlines)
            {
                var run = inline as Run;
                if (run !=  null) Retval += run.Text;
                else
                {
                    var span = inline as Span;
                    if (span != null) Retval += "<span>";
                    else Retval += "<other>";
                }
            }
            return Retval;
        }

        private string AsDebugString()
        {
            string Retval = AsDebugString(uiLeft) + "|" + AsDebugString(uiSelection) + AsDebugString(uiRight);
            return Retval;
            

        }

        public FrameworkElement GetSelection()
        {
            LanguageEditor.Log($"LanguageEditorLine:GetSelection for left={uiLeft.Text} selection={uiSelection.Text} right={uiRight.Text}");
            return uiSelection; //NOTE: not quite right; won't work with multi-line output
        }

        public void SetCaretTextVisible (bool isVisible)
        {
            CaretText.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        private int MeasureClick(double distanceFromLeft)
        {
            var sb = new StringBuilder();
            double fontSize = 12;
            foreach (var item in uiLeft.Inlines)
            {
                var itemrun = item as Run;
                if (itemrun != null) sb.Append(itemrun.Text);
                fontSize = itemrun.FontSize;
            }
            foreach (var item in uiSelection.Inlines)
            {
                var itemrun = item as Run;
                if (itemrun != null) sb.Append(itemrun.Text);
                fontSize = itemrun.FontSize;
            }
            foreach (var item in uiRight.Inlines)
            {
                var itemrun = item as Run;
                if (itemrun != null) sb.Append(itemrun.Text);
                fontSize = itemrun.FontSize;
            }
            var text = sb.ToString();
            text = text.Replace("̲", "");
;

            // Short text? return either 0 or 1 as the index.
            if (text.Length < 2)
            {
                if (distanceFromLeft < 4 || text.Length < 1) return 0;
                return 1;
            }

            // Now for the fun. 
            var maxIndex = text.Length;
            var delta = (int)Math.Ceiling((double)(text.Length+1) / 2.0);
            var trialIndex = delta-1; //Must start at delta-1; otherwise you won't get to the zeroth element.
            int maxTries = text.Length;
            while (maxTries > 0)
            {
                maxTries -= 1;
                delta = (int)Math.Ceiling((double)delta / 2.0);
                if (trialIndex > maxIndex) trialIndex = maxIndex;
                if (trialIndex < 0) trialIndex = 0;
                var measure = TextSizeRange(fontSize, text, trialIndex);

                //LanguageEditor.Log ($"MEASURE: trialIndex={trialIndex} left={measure.Item1} right={measure.Item2} distanceFromLeft={distanceFromLeft} newdelta={delta} maxIndex={maxIndex}");
                if (distanceFromLeft < measure.Item1)
                {
                    trialIndex -= delta;
                }
                else if (distanceFromLeft >= measure.Item2)
                {
                    trialIndex += delta;
                }
                else // we found the exact char!
                {
                    // To the left, or to the right?
                    return trialIndex;
                }
            }
            return trialIndex;
        }

        private Tuple<double, double> TextSizeRange(double fontSize, string text, int trialIndex)
        {
            // last.Text = last.Text + "̲"
            var tb = new TextBlock { Text = text.Substring(0, trialIndex), FontSize = fontSize };
            bool mustAddDot = tb.Text.EndsWith(" ");
            if (mustAddDot) tb.Text += ".";
            tb.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            var w1 = tb.DesiredSize.Width;
            double w2 = double.MaxValue;
            if (trialIndex >= text.Length)
            {
                w2 = double.MaxValue;
            }
            else
            {
                tb.Text = text.Substring(0, trialIndex + 1);
                mustAddDot = tb.Text.EndsWith(" ");
                if (mustAddDot) tb.Text += ".";
                tb.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
                w2 = tb.DesiredSize.Width;
            }
            return new Tuple<double, double>(w1, w2);
        }


        private LanguageEditorStatement GetStatement()
        {
            var p1 = this.Parent as StackPanel;
            if (p1 == null) return null;
            var statement = p1.Parent as LanguageEditorStatement;
            if (statement == null) return null;
            return statement;
        }
        // Set the focus on the containing statement.
        // Move the cursor as needed.
        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            LanguageEditor.Log($"PTR:LanguageEditorLine:PointerPressed {e.GetCurrentPoint(this).Position.X}  text={AsDebugString()}");
            var statement = GetStatement();
            if (statement == null) return;
            var status = statement.Focus(FocusState.Pointer);


            var distance = e.GetCurrentPoint(this).Position.X;
            var index = MeasureClick(distance);
            LanguageEditor.Log($"PTR:LanguageEditorLine:PointerPressed returning {index} focusstatus={status}");
            statement.SetPreferredXamlCursorPos(index, index);

            e.Handled = true;
        }

        // Set the focus on the containing statement. Does this duplicate the other pointer pressed? A bit!
        private void OnControlPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var statement = GetStatement();
            var status = statement != null ? statement.Focus(FocusState.Pointer) : false;
            LanguageEditor.Log($"KEY:LanguagEditorLine:OnControlPointerPressed: focusstatus={status}");
        }

        private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            LanguageEditor.Log($"PTR:LanguageEditorLine:PointerReleased {e.GetCurrentPoint(this).Position.X}  text={AsDebugString()}");
            var statement = GetStatement();
            if (statement == null) return;
            var status = statement.Focus(FocusState.Pointer);
            LanguageEditor.Log($"PTR:LanguageEditorLine:PointerRelease returning focusstatus={status}");
            e.Handled = true;
        }
    }
}
