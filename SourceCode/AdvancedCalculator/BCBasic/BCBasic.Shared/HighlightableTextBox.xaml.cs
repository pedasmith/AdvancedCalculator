using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace BCBasic
{
    public sealed partial class HighlightableTextBox : UserControl, INotifyPropertyChanged
    {
        public HighlightableTextBox()
        {
            // Binding and UserControls:
            // http://blog.jerrynixon.com/2013/07/solved-two-way-binding-inside-user.html

            TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(HighlightableTextBox), new PropertyMetadata (this, OnTextChanged));
            // Removing all Highlight code! HighlightBackgroundColor = Colors.DarkOliveGreen;
            this.InitializeComponent();
            uiText.IsSpellCheckEnabled = false;
            (this.Content as FrameworkElement).DataContext = this;
            this.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "Text")
                {
                    string oldText;
                    uiText.Document.GetText(TextGetOptions.None, out oldText);
                    string oldTextTrim = oldText == null ? null : oldText.Trim();
                    string newText = Text;
                    string newTextTrim = newText == null ? null : newText.Trim();
                    if (oldText != newText)
                    {
                        if (newText == null) newText = "";
                        uiText.Document.SetText(TextSetOptions.None, newText);
                        SetupUpdateColorOnViewChanged();
                    }
                }
            };

            string lastRtf = "";
            uiText.TextChanged += (s, e) => {

                // Do the RTF compare first thanks to a Windows 8.1 memory leak.
                string text;
                uiText.Document.GetText(TextGetOptions.FormatRtf, out text);
                if (text == lastRtf)
                {
                    return; // If it's the same, just leave right away.
                }
                lastRtf = text;

                // Leaks memory on Windows 8.1
                uiText.Document.GetText(TextGetOptions.None, out text);
                var oldValue = Text;
                if (oldValue == null || oldValue.Trim() != text.Trim()) 
                {
                    Text = text;
                    SetupUpdateColorOnViewChanged();
                }
            };
            uiText.KeyDown += (s, e) =>
            {
                switch (e.Key)
                {
                    case VirtualKey.Escape:
                        if (Escape != null) Escape.Invoke(this, new EventArgs());
                        break;
                    case VirtualKey.F5:
                        if (F5 != null) F5.Invoke(this, new EventArgs());
                        break;
                    case VirtualKey.F7:
                        if (F7 != null) F7.Invoke(this, new EventArgs());
                        break;
                }
            };
        }

        bool isSetupUpdateColorOnViewChanged = false;
        bool doingUpdateColor = false;
        private void SetupUpdateColorOnViewChanged()
        {
            if (isSetupUpdateColorOnViewChanged) return;
            var textboxChildren = Children(uiText).ToList();
            var textboxScroll = textboxChildren.FirstOrDefault(x => x is ScrollViewer) as ScrollViewer;
            if (textboxScroll == null) textboxScroll = GetScroll();
            if (textboxScroll != null)
            {
                isSetupUpdateColorOnViewChanged = true;
                textboxScroll.ViewChanged += (s, e) => 
                {
                    if (doingUpdateColor) return;
                    doingUpdateColor = true;
                    UpdateColor();

                    var startRange = uiText.Document.GetRangeFromPoint(new Point(0, 0), PointOptions.None);
                    var sp = startRange.StartPosition;
                    if (sp > 0)
                    {
                        //uiText.Document.Selection.SetRange(sp, sp);
                    }
                    //uiText.Document.Selection.SetRange(startRange.StartPosition, startRange.StartPosition); 
                    doingUpdateColor = false;
                };
            }
        }

        private ScrollViewer GetScroll()
        {
            var grid = (Windows.UI.Xaml.Controls.Grid)Windows.UI.Xaml.Media.VisualTreeHelper.GetChild(uiText, 0);
            if (grid == null) return null;
            for (var i = 0; i <= Windows.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(grid) - 1; i++)
            {
                object obj = Windows.UI.Xaml.Media.VisualTreeHelper.GetChild(grid, i);
                if (!(obj is Windows.UI.Xaml.Controls.ScrollViewer)) continue;
                return ((Windows.UI.Xaml.Controls.ScrollViewer)obj);
            }
            return null;
        }

        // This is used to find the ScrollViewer of the RichEditBox;
        private static IEnumerable<FrameworkElement> Children(FrameworkElement element)
        {
            Func<DependencyObject, List<FrameworkElement>> recurseChildren = null;
            recurseChildren = (parent) =>
            {
                var list = new List<FrameworkElement>();
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);
                    if (child is FrameworkElement)
                        list.Add(child as FrameworkElement);
                    list.AddRange(recurseChildren(child));
                }
                return list;
            };
            var children = recurseChildren(element);
            return children;
        }

        public event EventHandler Escape;
        public event EventHandler F5;
        public event EventHandler F7;

        // List of places that have been highlighted; is generally either 0 or 1 long.
        // Is never deleted, but is often Clered.
        List<ITextRange> HighlightRanges = new List<ITextRange>();

        // Public property for Text.  This get complicated: this is the Text property, 
        // which is backed by the Text dependency property.  
        // But the Text is ALSO backed up by the RichEditBox, and irritatingly, the
        // two texts are similar but not identical (specifically, they differ in the
        // CR at the end of the text).
        // So, we watch both the Text property and the Text dependency property,
        // and flow changes from either into the RTF box.  
        // AND we watch the RTF box, and update the depenendy property accordingly.
        // All along the way, we have to be careful about infinite loops :-)
        public string Text
        {
            get
            {
                var Retval = GetValue(TextProperty) as string;
                return Retval;
            }
            set
            {
                var oldValue = Text;
                if (oldValue != null && oldValue.Trim() == value.Trim()) return;

                SetValue(TextProperty, value);
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("Text"));
            }
        }
        public static void OnTextChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            (obj as HighlightableTextBox).OnTextChanged(args);
        }
        void OnTextChanged(DependencyPropertyChangedEventArgs args)
        {
            PropertyChanged(this, new PropertyChangedEventArgs("Text"));
        }
        //public event DependencyPropertyChangedEventHandler TextChanged;
        public event PropertyChangedEventHandler PropertyChanged;
        public static DependencyProperty TextProperty;

#if NEVER_EVER_DEFINED
        public Color HighlightBackgroundColor { get; set; }
        private Color OldColor;

        private void ClearOldRanges()
        {
            foreach (var range in HighlightRanges)
            {
                range.CharacterFormat.BackgroundColor = OldColor;
            }
            HighlightRanges.Clear();
        }

        public void CancelHighlights()
        {
            ClearOldRanges();
            uiText.Document.ApplyDisplayUpdates();
        }
        public static int LastIndexOfCrLf(string text, int pos)
        {
            int cr = text.LastIndexOf('\r', pos);
            int lf = text.LastIndexOf('\n', pos);
            return Math.Max(cr, lf);
        }
        public static int IndexOfCrLf(string text, int pos)
        {
            int cr = text.IndexOf('\r', pos);
            int lf = text.IndexOf('\n', pos);
            if (cr < 0) return lf;
            if (lf < 0) return cr;
            return Math.Min(cr, lf);
        }
        public void HighlightLineAt(int pos)
        {
            var text = Text;
            var start = LastIndexOfCrLf(text, pos);
            if (start < 0) start = 0;
            var end = IndexOfCrLf(text, pos);
            if (end < 0) end = text.Length;
            Highlight(start, (end - start), pos);
        }

        public void Highlight(int start, int length, int pos)
        {
            ClearOldRanges();
            var range = uiText.Document.GetRange(start, start + length);
            OldColor = range.CharacterFormat.BackgroundColor;
            range.CharacterFormat.BackgroundColor = HighlightBackgroundColor;
            HighlightRanges.Add(range);

            uiText.Document.Selection.StartPosition = pos;
            uiText.Document.Selection.EndPosition = pos;


            uiText.Document.ApplyDisplayUpdates();
            uiText.Focus(FocusState.Programmatic);
        }
#endif

        public void Insert(string str)
        {
            var sel = uiText.Document.Selection;
            sel.SetText(TextSetOptions.None, str);
            sel.MoveRight(TextRangeUnit.Character, str.Length, false); // move to the right!
        }

        Color TextColor = Colors.AntiqueWhite;

        // Inspired by Obivion http://eclipsecolorthemes.org/?view=theme&id=1
        static class SynxtaxColors
        {
            // String is e.g., #1E1E1E
            public static Color Parse(string str)
            {
                Color Retval = new Color();
                int intval = 0;
                Int32.TryParse (str.Substring (1, 2), System.Globalization.NumberStyles.AllowHexSpecifier, null, out intval); Retval.R = (byte)intval;
                Int32.TryParse (str.Substring (3, 2), System.Globalization.NumberStyles.AllowHexSpecifier, null, out intval); Retval.G = (byte)intval;
                Int32.TryParse (str.Substring (5, 2), System.Globalization.NumberStyles.AllowHexSpecifier, null, out intval); Retval.B = (byte)intval;
                return Retval;
            }
            public static Color Background = Parse("#1E1E1E"); // black
            public static Color REM = Parse("#C7DD0C"); // yellow

            public static Color Color = Parse("#D197D9"); // violet from interface
            public static Color Numbers = Parse("#7FB347"); // green
            public static Color Strings = Parse("#FFC600"); // dark yellow
            public static Color Variable = Parse("#79ABFF"); // darkish light blue

            public static Color External = Parse("#FFC600"); // Sort of peach
            public static Color Keyword = Parse("#FFFFFF"); // white

            public static Color Math = Parse ("#D197D9"); // violet from interface
        };

        Dictionary<string, Color> ColorMap = new Dictionary<string, Color>()
        {
            { "_NONE_", Colors.White },
            { "Rem_Statement", SynxtaxColors.REM },

            { "LINE_NUMBER", SynxtaxColors.Numbers},
            { "NUMBER", SynxtaxColors.Numbers},

            { "STRING", SynxtaxColors.Strings},
            { "SMARTQUOTESTRING", SynxtaxColors.Strings },

            { "COLOR", SynxtaxColors.Color},
            { "VARIABLE", SynxtaxColors.Variable},

            { "Bluetooth.", SynxtaxColors.External},
            //{ "Calculator.", SynxtaxColors.External},
            //{ "File.", SynxtaxColors.External},
            //{ "Graphics.", SynxtaxColors.External},
            //{ "Math.", SynxtaxColors.External},
            //{ "Memory.", SynxtaxColors.External},
            //{ "Screen.", SynxtaxColors.External},
            { "SINCLAIROP", SynxtaxColors.Math},
            { "ROOT", SynxtaxColors.Math},
            { "POWER", SynxtaxColors.Math},


            { "BEEP", SynxtaxColors.Keyword},
            { "CALL", SynxtaxColors.Keyword},
            { "CLS", SynxtaxColors.Keyword},
            { "CONSOLE", SynxtaxColors.Keyword},
            { "DIM", SynxtaxColors.Keyword},
            { "DUMP", SynxtaxColors.Keyword},
            { "ELSE", SynxtaxColors.Keyword},
            { "END", SynxtaxColors.Keyword},
            { "FOR", SynxtaxColors.Keyword},
            { "FOREVER", SynxtaxColors.Keyword},
            { "FUNCTION", SynxtaxColors.Keyword},
            { "GLOBAL", SynxtaxColors.Keyword},
            { "GOSUB", SynxtaxColors.Keyword},
            { "GOTO", SynxtaxColors.Keyword},
            { "IF", SynxtaxColors.Keyword},
            { "IMPORT", SynxtaxColors.Keyword},
            { "INPUT", SynxtaxColors.Keyword},
            { "LET", SynxtaxColors.Keyword},
            { "NEXT", SynxtaxColors.Keyword},
            { "PAPER", SynxtaxColors.Keyword},
            { "PAUSE", SynxtaxColors.Keyword},
            { "PLAY", SynxtaxColors.Keyword},
            { "PRINT", SynxtaxColors.Keyword},
            { "RAND", SynxtaxColors.Keyword},
            { "RETURN", SynxtaxColors.Keyword},
            { "SPEAK", SynxtaxColors.Keyword},
            { "STOP", SynxtaxColors.Keyword},

            { "AT", SynxtaxColors.Keyword},
            { "DEFAULT", SynxtaxColors.Keyword},
            { "FROM", SynxtaxColors.Keyword},
            { "FUNCTIONS", SynxtaxColors.Keyword},
            { "LIST", SynxtaxColors.Keyword},
            { "PROMPT", SynxtaxColors.Keyword},
            { "STEP", SynxtaxColors.Keyword},
            { "THEN", SynxtaxColors.Keyword},
            { "TO", SynxtaxColors.Keyword},
            { "VOICE", SynxtaxColors.Keyword},
            { "VOICES", SynxtaxColors.Keyword},
            { "WAIT", SynxtaxColors.Keyword},

        };

        private bool IsExternalsInitialized = false;
        public void InitializeExternals(BCGlobalConnections externalConnections)
        {
            if (IsExternalsInitialized) return;
            IsExternalsInitialized = true;
            foreach (var item in externalConnections.Externals)
            {
                // e.g. { "Bluetooth.", SynxtaxColors.External},
                ColorMap.Add(item.Key, SynxtaxColors.External);
            }
        }
        static Color BackgroundColor = SynxtaxColors.Background;
        static Brush BackgroundBrush = new SolidColorBrush(BackgroundColor);

        public void RemoveColor()
        {
            var dcolor = ColorMap["_NONE_"];
            var r = uiText.Document.GetRange(0, Int32.MaxValue);
            r.CharacterFormat.ForegroundColor = dcolor;
        }

#if NEVER_EVER_DEFINED
        class RangeData
        {
            public string Text;
            public string TokenType;
            public int StartPos;
            public int EndPos;
            public double Top;
            public int Hit;
            public bool Exception;
            public override string ToString()
            {
                return $"{Hit},{Top},{TokenType},{StartPos},{EndPos},{Text}\n";
            }
        }
        class AllRangeData
        {
            public int NException = 0;
            public void Clear()
            {
                AllData.Clear();
                NException++;
            }
            private List<RangeData> AllData = new List<RangeData>();
            public void Add(ITextRange range, string tokenType)
            {
                var rd = new RangeData();
                rd.Text = range.Text;
                rd.TokenType = tokenType;
                rd.StartPos = range.StartPosition;
                rd.EndPos = range.EndPosition;
                rd.Exception = false;

                Point point = new Point(-1, -1);
                Rect visible = new Rect() { X = 0, Y = 0, Height = 10, Width = 200 };
                int hit = 0;

                if (NException < 5)
                {
                    try
                    {
                        range.GetPoint(HorizontalCharacterAlignment.Left, VerticalCharacterAlignment.Baseline, PointOptions.AllowOffClient, out point);
                        //range.GetRect(PointOptions.Start, out visible, out hit);
                    }
                    catch (Exception)
                    {
                        rd.Exception = true;
                        NException++;
                    }
                }

                rd.Top = visible.Top;
                rd.Hit = hit;
                AllData.Add(rd);
            }
            public override string ToString()
            {
                var sb = new StringBuilder();
                foreach (var range in AllData)
                {
                    sb.Append(range.ToString());
                }
                return sb.ToString();
            }
        }
#endif
        //AllRangeData CompileRangeData = new AllRangeData();

        // Returns the amount of time this routine took.  That's important because on
        // phone the time can be very large (at which point we turn off coloring)
        int firstVisiblePosition = 0;
        int lastVisiblePosition = int.MaxValue;
        TinyPG.ParseTree MostRecentParseTree = null;
        public double Color(TinyPG.ParseTree tree, double MAXTIME) 
        {
            MostRecentParseTree = tree;
            var startTime = DateTime.UtcNow;
            //CompileRangeData.Clear();
            uiText.Background = BackgroundBrush;
            RemoveColor();
            if (tree.Nodes.Count == 0)
            {
            }

            uiText.Document.BatchDisplayUpdates();
            uiText.Document.BeginUndoGroup();
            var startRange = uiText.Document.GetRangeFromPoint(new Point(0, 0), PointOptions.None);
            var endRange = uiText.Document.GetRangeFromPoint(new Point(0, 2000), PointOptions.None); // Note: totally making up the position!

            firstVisiblePosition = startRange.StartPosition;
            lastVisiblePosition = endRange.StartPosition;

            foreach (var node in tree.Nodes)
            {
                // Jump into the private recursive method.
                ColorNode(node, startTime, MAXTIME);
            }
            uiText.Document.EndUndoGroup();
            uiText.Document.ApplyDisplayUpdates();
            var endTime = DateTime.UtcNow;
            var deltaTime = endTime.Subtract(startTime);
            return deltaTime.TotalSeconds;
        }

        private void UpdateColor()
        {
            if (MostRecentParseTree == null) return;
            Color(MostRecentParseTree, 99.99); // TODO: what's the right color time?
        }



        //
        // The private version is the recursing one!
        // 
        private void ColorNode(TinyPG.ParseNode node, DateTime startTime, double MAXTIME)
        {
            var tt = node.Token.Type.ToString();
            if (node.Token.StartPos < lastVisiblePosition && node.Token.EndPos > firstVisiblePosition)
            {
                if (tt == "VARIABLE") 
                {
                    // Fix the case -- originally this supported ALL UPPERCASE extensions.
                    if (node.Token.Text.ToUpper().StartsWith("CALCULATOR.")) tt = "Calculator.";
                    else if (node.Token.Text.ToUpper().StartsWith("MATH.")) tt = "Math.";
                    else if (node.Token.Text.ToUpper().StartsWith("MEMORY.")) tt = "Memory.";
                }

                if (ColorMap.ContainsKey(tt))
                {
                    var r = uiText.Document.GetRange(node.Token.StartPos, node.Token.EndPos);
                    r.CharacterFormat.ForegroundColor = ColorMap[tt];
                    //NOTE: remove all of this compileRangeData stuff... CompileRangeData.Add(r, tt);
                }
            }
            foreach (var child in node.Nodes)
            {
                ColorNode(child, startTime, MAXTIME);
            }
        }

        private async void OnPaste(object sender, TextControlPasteEventArgs e)
        {
            var content = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
            foreach (var fmt in content.AvailableFormats)
            {
                ;
            }
            //var rtf = await content.GetRtfAsync();
            //var html = await content.GetHtmlFormatAsync();
            var text = await content.GetTextAsync();
            var dp = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dp.SetText(text);
            //Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dp);
            // Do not set the handled property.  All we're doing is fiddling with 
            // with the clipboard content so that it get formatted our way and
            // loses whatever formatting it might have had.
        }
    }
}
