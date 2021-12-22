using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace BCBasic
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BCScreenControl : Page, IConsole
    {
        public enum ScreenType
        {
            //XMINI, MINI, VIC20, COCO, ZX81, C64, VT100
            XSMALL, SMALL, MEDIMUM, LARGE
        }
        public class ScreenData
        {
            public ScreenData(ScreenType screenType, int ncol, int nrow) { ScreenType = screenType; NCols = ncol; NRows = nrow; }
            public ScreenType ScreenType;
            public int NCols;
            public int NRows;
        }
        // Orded by size
        static List<ScreenData> ScreenList = new List<ScreenData>()
        {
            { new ScreenData (ScreenType.XSMALL, 20, 10) },
            { new ScreenData (ScreenType.SMALL, 40, 12) },
            { new ScreenData (ScreenType.MEDIMUM, 60, 16) },
            { new ScreenData (ScreenType.LARGE, 80, 24) },
            /* Actual screen sizes, but I'm going a different way now...
            { new ScreenData (ScreenType.XMINI,20, 5) },
            { new ScreenData (ScreenType.MINI, 20, 10) },
            { new ScreenData (ScreenType.VIC20, 22, 23) },
            { new ScreenData (ScreenType.COCO, 32, 16) },
            { new ScreenData (ScreenType.ZX81, 32, 24) },
            { new ScreenData (ScreenType.C64, 40, 25) },
            { new ScreenData (ScreenType.VT100, 80, 24) },
            */
        };
        class FixedScreenBuffer
        {
            char[][] Buffer;
            public int LastRow { get; set; }
            public int LastCol { get; set; }
            public int MaxRow { get; set; }
            public int MaxCol { get; set; }

            public FixedScreenBuffer()
            {
                // Biggest set if C64 (25 rows) and VT100 (80 cols)
                int AbsMaxRow = 25; 
                int AbsMaxCol = 80;

                Buffer = new char[AbsMaxRow][];
                for (int r = 0; r < Buffer.Length; r++)
                {
                    Buffer[r] = new char[AbsMaxCol];
                }

                Cls();
            }
            // Space chars 
            // ' ' U+20 space
            // U+A0 is NO BREAK SPACE
            // ' ' u+2007 FIGURE SPACE
            // ' ' U+205f MEDIUM MATHEMATICAL SPACE : no longer works
            // ' ' U+202F NARROW NO-BREAK SPACE
            const char SPACE = ' ';
            const char NOBREAKSPACE = '\u00A0';
            const char FIGURESPACE = '\u2007';
            const char MEDMATHSPACE = '\u205f';
            const char NARROWNOBREAKSPACE = '\u202f';
            private char[] FillChars = new char[] { ' ' };
            public void Cls()
            {
                for (int r = 0; r < Buffer.Length; r++)
                {
                    ClearLine(r);
                }
                LastRow = -1;
                LastCol = -1;
            }

            public void ClearLine(int r)
            {
                for (int c = 0; c < Buffer[r].Length; c++)
                {
                    Buffer[r][c] = FillChars[c % FillChars.Length]; // was just a plain '.';
                }
            }

            public void ClearLines(int startRow, int endRow)
            {
                for (int r = startRow; r <= endRow; r++)
                {
                    ClearLine(r);
                }
            }

            public void Scroll(int rowstart, int rowend)
            {
                var oldrow = Buffer[rowstart];
                for (int r=rowstart; r<rowend; r++)
                {
                    Buffer[r] = Buffer[r + 1];
                }
                Buffer[rowend] = oldrow;
                ClearLine(rowend);
            }

            public string GetAt (int row, int col, int nchar)
            {
                if (row < 0 || row >= Buffer.Length)
                {
                    return null; // is too big.
                }
                if (col < 0 || col >= Buffer[row].Length)
                {
                    return null; // is too far over
                }
                if (nchar < 1)
                {
                    return null; // is too small
                }
                var Retval = new string(Buffer[row], col, nchar);
                return Retval;
            }

            public void PrintAt(PrintExpression.PrintSpaceType pst, string str, int row, int col)
            {

                if (row >= Buffer.Length)
                {
                    return; // ERROR: what error for overages?
                }
                if (row < 0) return;

                str = str.Replace("\r\n", "\n").Replace("\r", "\n"); // convert \r \r\n and \n into plain \n
                int cindex = col;
                for (int c = 0; c < str.Length; c++)
                {
                    //int cindex = col + c;

                    if (cindex < 0) continue;
                    if (str[c] == '\n')
                    {
                        cindex = 0;
                        row++;
                        if (row >= Buffer.Length)
                        {
                            return; // too big!
                        }
                        continue;
                    }
                    if (cindex >= Buffer[row].Length)
                    {
                        continue; // we might get a \n later on.
                    }

                    Buffer[row][cindex] = str[c];

                    LastRow = row;
                    LastCol = cindex;
                    cindex++;
                }
            }

            public override string ToString()
            {
                string Retval = "";
                for (int r = 0; r < MaxRow; r++)
                {
                    Retval += new string(Buffer[r]).Substring(0, MaxCol) + "\r\n";
                }
                Retval = Retval.Replace(' ', FillChars[0]); // Replace normal space with Medium Mathematical Space U+205F
                // Why?  Because the TextBlock, darn it, doesn't bother to display stretches of blanks!
                // Update: works when you also set IsTextSelectionEnabled!
                // Update 3: Nope, that doesn't have an effect any more!
                return Retval;
            }
        }

        FixedScreenBuffer ScreenBuffer = new FixedScreenBuffer();
        private ScreenData CurrScreenData;
        public void SetScreen(ScreenData sd)
        {
            CurrScreenData = sd;
            ScreenBuffer.MaxRow = CurrScreenData.NRows;
            ScreenBuffer.MaxCol = CurrScreenData.NCols;
        }
        public int LastCol { get { return ScreenBuffer.LastCol; } }
        public int LastRow { get { return ScreenBuffer.LastRow; } }

        public int NCols { get { return CurrScreenData.NCols; } }
        public int NRows { get { return CurrScreenData.NRows; } }

        // Shockingly hard to get tehe right sizes here. And technically we should wait until the screen is visible.
        public double GW { get { return uiBackground.ActualWidth; } } 
        public double GH { get { return uiBackground.ActualHeight - (uiConsoleViewer.Visibility == Visibility.Visible ? uiConsoleViewer.ActualHeight : 0); } } 
        public bool FirstMeasure { get { return NMeasure > 0 && uiBackground.ActualWidth >0; } }

        Windows.UI.Core.CoreCursor stockCursor = null;
        Windows.UI.Core.CoreCursor moveCursor = null;
        Windows.Storage.ApplicationDataContainer roamingSettings;
        public BCScreenControl()
        {
            roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            this.InitializeComponent();
            SetScreen(ScreenList[ScreenList.Count-1]);
            Restore();
            this.Loaded += (s, e) =>
            {
                stockCursor = Window.Current.CoreWindow.PointerCursor;
                moveCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.SizeAll, 2);
                var txt = ScreenBuffer == null ? "(starting)" : ScreenBuffer.ToString();
                var tlen = txt.Length;
                uiFixedScreen.Text = txt;
                DoMeasure();
            };
        }

        int NMeasure = 0;
        private void DoMeasure()
        {
            NMeasure += 1;

            FrameworkElement el = uiMainGrid;

            el.Width = double.NaN; // Gotta forget these (otherwise the measure just returns them)
            el.Height = double.NaN;
            el.Measure(new Size(double.MaxValue, double.MaxValue));
            //var w = el.DesiredSize.Width;
            //var h = el.DesiredSize.Height;

            //var txt = string.Format("{0}:{1}x{2}", NMeasure, h, w);
            //uiSize.Text = txt;

            // Force the values!
            //el.Width = w;
            //el.Height = h;

            // Force the screen size
            var strb = new StringBuilder();
            for (int i = 0; i < ScreenBuffer.MaxCol; i++) strb.Append("N"); // A nice average size character.

            var tb = new TextBlock();
            tb.FontSize = uiFixedScreen.FontSize;
            tb.FontFamily = uiFixedScreen.FontFamily;
            tb.Text = strb.ToString();
            tb.Measure(new Windows.Foundation.Size(9999, 9999));
            var minw = tb.ActualWidth;
            uiFixedScreen.MinWidth = minw;
            uiConsoleViewer.MinWidth = minw;
        }

        private void ShowScreenSize()
        {
            if (ScreenBuffer != null)
            {
                var txt = String.Format("{0}x{1} ({2})", ScreenBuffer.MaxRow, ScreenBuffer.MaxCol, this.uiFixedScreen.FontSize);
                uiScreenSize.Text = txt;
            }
        }
        public void SetConsoleMode()
        {
            //uiProgram.Visibility = Visibility.Collapsed;
            uiBackground.Visibility = Visibility.Visible;
            uiConsoleViewer.Visibility = Visibility.Visible;
            uiFixedScreen.Visibility = Visibility.Collapsed;
        }
        public void SetEditMode()
        {
            //uiProgram.Visibility = Visibility.Visible;
            uiBackground.Visibility = Visibility.Collapsed;
        }
        public void SetRunMode()
        {
            //uiProgram.Visibility = Visibility.Collapsed;
            uiBackground.Visibility = Visibility.Visible;
            uiConsoleViewer.Visibility = Visibility.Visible;
            uiFixedScreen.Visibility = Visibility.Visible;
            DoMeasure();
        }
        /*
        public string GetBCBasicProgram()
        {
            return uiProgram.Text;
        }
         */

        List<IGraphics> AllGraphics = new List<IGraphics>();
        public void AddGraphics (IGraphics graphics)
        {
            graphics.SetAlignment();
            uiGraphics.Children.Add((FrameworkElement)graphics);
            AllGraphics.Add(graphics);
        }

        // Called by the PAUSE function
        public void Update()
        {
            foreach (var item in AllGraphics)
            {
                item.Update();
            }
        }



        public async Task ClsAsync(BCColor backgroundColor, BCColor foregroundColor, BCGlobalConnections.ClearType clearType)
        {
            var gotFocus = this.Focus(FocusState.Programmatic);
            if (clearType == BCGlobalConnections.ClearType.Cls)
            {
                uiConsole.Text = "";
                ScreenBuffer.Cls();
                uiFixedScreen.Text = ScreenBuffer.ToString();
                uiGraphics.Children.Clear();
                AllGraphics.Clear();
                ShowScreenSize();

                const int MAX_LOOPS_TO_WAIT = 100;
                int nloop = 0;
                while (!FirstMeasure && nloop < MAX_LOOPS_TO_WAIT) 
                {
                    nloop++;
                    await Task.Delay(1);
                }
            }
            if (backgroundColor != null)
            {
                uiBorder.Background = backgroundColor.Brush;
                uiBackground.Background = backgroundColor.Brush;
            }
            if (foregroundColor != null)
            {
                uiScreenSize.Foreground = foregroundColor.Brush;
                uiFlyout.Foreground = foregroundColor.Brush;
                uiClose.SetForeground (foregroundColor.Brush);
                uiFixedScreen.Foreground = foregroundColor.Brush;
                uiConsole.Foreground = foregroundColor.Brush;
            }
        }

        public BCColor GetBackground()
        {
            var scb = uiBorder.Background as SolidColorBrush;
            if (scb == null) return null;
            return new BCBasic.BCColor (scb.Color);
        }

        public BCColor GetForeground()
        {
            var scb = uiFixedScreen.Foreground as SolidColorBrush;
            if (scb == null) return null;
            return new BCBasic.BCColor(scb.Color);
        }

        public void Console(string str)
        {
            uiConsole.Text += str + "\n";
        }

        public void ClearLine(int row)
        {
            ScreenBuffer.ClearLine(row);
            ScreenBuffer.LastRow = row - 1;
            uiFixedScreen.Text = ScreenBuffer.ToString();
        }

        public void ClearLines(int rowStart, int rowEnd)
        {
            ScreenBuffer.ClearLines(rowStart, rowEnd);
            ScreenBuffer.LastRow = rowStart - 1;
            uiFixedScreen.Text = ScreenBuffer.ToString();
        }
        public string GetAt(int row, int col, int nchar)
        {
            return ScreenBuffer.GetAt(row, col, nchar);
        }
        public void PrintAt(PrintExpression.PrintSpaceType pst, string str, int row, int col)
        {
            if (row == -1 && col == -1)
            {
                // Otherwise must print at the best location.  This is 
                // either at the next col which is a multiple of 16 from
                // the last col printed, or on the next row at column 0.
                var nextRow = ScreenBuffer.LastRow;
                if (pst == PrintExpression.PrintSpaceType.Newline) nextRow++;
                int nextCol = 0;
                switch (pst)
                {
                    case PrintExpression.PrintSpaceType.Newline: nextCol = 0; break;
                    case PrintExpression.PrintSpaceType.NoSpace: nextCol = ScreenBuffer.LastCol + 1; break;
                    case PrintExpression.PrintSpaceType.Tab: nextCol = ScreenBuffer.LastCol < 0 ? 0 : (ScreenBuffer.LastCol + 1 + 16) & ~0x0F; break; // clear the last 4 digits
                }
                if (nextCol < 0) nextCol = 0;
                else if (nextCol >= ScreenBuffer.MaxCol)
                {
                    nextRow++;
                    nextCol = 0;
                }
                while (nextRow >= ScreenBuffer.MaxRow) // zero or one times...  MaxRow is like "24" for 24 lines.
                {
                    ScreenBuffer.Scroll(0, ScreenBuffer.LastRow); // Scroll the whole thing.
                    nextRow--;
                }

                // Note that we do not scroll the PRINT AT statement.
                ScreenBuffer.PrintAt(PrintExpression.PrintSpaceType.At, str, nextRow, nextCol);
            }
            else
            {
                ScreenBuffer.PrintAt(PrintExpression.PrintSpaceType.At, str, row, col);
            }
            uiFixedScreen.Text = ScreenBuffer.ToString();
        }

        Popup uiInputPopup = null;
        public async Task<string> GetInputAsync(CancellationToken ct, string prompt, string defaultValue)
        {
            uiInputPopup = new Popup();
            uiInputPopup.LayoutUpdated += UiInputPopup_LayoutUpdated;

            var content = new StackPanel();
            content.Children.Add(new TextBlock() { Text = prompt, FontSize = 22 });
            var tb = new TextBox() { Text = defaultValue, FontSize = 22, MinWidth=200, MaxWidth=600, TextWrapping=TextWrapping.Wrap, AcceptsReturn=true };
            tb.SelectionStart = Math.Max (0, tb.Text.Length); // add some logic if length is 0
            tb.SelectionLength = 0;
            tb.TextChanged += OnTextInputDialog;
            content.Children.Add(tb);
            var close = new Button() { Content = "OK" };
            close.Click += OnCloseInputDialogClicked;
            content.Children.Add(close);

            var b = new Border() { BorderBrush = new SolidColorBrush(Colors.Green), BorderThickness = new Thickness(2), Margin = new Thickness(10), Padding = new Thickness(10), Background=new SolidColorBrush(Colors.Black) };
            b.Child = content;
            uiInputPopup.Child = b;
            uiInputPopup.IsOpen = true;

            tb.Focus(FocusState.Programmatic);
            while (uiInputPopup.IsOpen && !ct.IsCancellationRequested)
            {
                await Task.Delay(50);
            }
            uiInputPopup.IsOpen = false; // bring it down when input is cancelled.
            return tb.Text;
        }

        private void UiInputPopup_LayoutUpdated(object sender, object e)
        {
            var popup = uiInputPopup;
            if (popup == null) return;
            var gdChild = popup.Child as FrameworkElement;
            if (gdChild.ActualWidth == 0 && gdChild.ActualHeight == 0)
            {
                return;
            }

            double ActualHorizontalOffset = popup.HorizontalOffset;
            double ActualVerticalOffset = popup.VerticalOffset;

            double NewHorizontalOffset = (Window.Current.Bounds.Width - gdChild.ActualWidth) / 2;
            double NewVerticalOffset = (Window.Current.Bounds.Height - gdChild.ActualHeight) / 2;

            var approxEqual = BCBasicMathFunctions.ApproxEqual(ActualHorizontalOffset, NewHorizontalOffset, 2.0) 
                && BCBasicMathFunctions.ApproxEqual(ActualVerticalOffset, NewVerticalOffset);

            if (!approxEqual)
            {
                popup.HorizontalOffset = NewHorizontalOffset;
                popup.VerticalOffset = NewVerticalOffset;
            }
        }

        private void OnTextInputDialog(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;

            if ((sender as TextBox).Text.EndsWith ("\r\n"))
            {
                tb.Text = tb.Text.TrimEnd();

                // Find the Popup parent of the button.
                FrameworkElement fe = sender as FrameworkElement;
                while (fe != null)
                {
                    if (fe is Popup)
                    {
                        (fe as Popup).IsOpen = false;
                        return;
                    }
                    fe = fe.Parent as FrameworkElement;
                }
            }
        }

        private void OnCloseInputDialogClicked(object sender, RoutedEventArgs e)
        {
            // Find the Popup parent of the button.
            FrameworkElement fe = sender as FrameworkElement;
            while (fe != null)
            {
                if (fe is Popup)
                {
                    (fe as Popup).IsOpen = false;
                    return;
                }
                fe = fe.Parent as FrameworkElement;
            }
            //uiInputDialog.IsOpen = false;

            //if (uiInputPopup != null) uiInputPopup.IsOpen = false;
        }

        private double Clamp (double size, double min, double max)
        {
            return Math.Min(Math.Max(size, min), max);
        }

        const int MIN_FONT_SIZE = 6;
        const int MAX_FONT_SIZE = 60;
        private void OnFontSize(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                double delta = 0.0;
                double.TryParse((sender as FrameworkElement).Tag as string, out delta);
                uiConsole.FontSize = Clamp (uiConsole.FontSize+delta, MIN_FONT_SIZE, MAX_FONT_SIZE-20);
                uiFixedScreen.FontSize = Clamp(uiFixedScreen.FontSize + delta, MIN_FONT_SIZE, MAX_FONT_SIZE+20);
                DoMeasure();
                roamingSettings.Values["ConsoleFontSize"] = uiConsole.FontSize;
                roamingSettings.Values["ScreenFontSize"] = uiFixedScreen.FontSize;
                ShowScreenSize();
            }
            catch (Exception)
            {
                // Happens when you try to make a font too small.
            }
        }

        private void OnConsoleToggle(object sender, TappedRoutedEventArgs e)
        {
            var obj = uiConsoleViewer;
            var vis = obj.Visibility == Windows.UI.Xaml.Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
            obj.Visibility = vis;
            roamingSettings.Values["ConsoleVisibility"] = (int)vis;
            DoMeasure();
        }

        private void OnScreenSize(object sender, TappedRoutedEventArgs e)
        {
            double delta = 0.0;
            double.TryParse((sender as FrameworkElement).Tag as string, out delta);

            // Get the next size up or down....
            var idx = 0;
            for (idx = 0; idx < ScreenList.Count; idx++)
            {
                if (ScreenList[idx].ScreenType == CurrScreenData.ScreenType) break;
            }
            if (idx >= ScreenList.Count) idx = 2; // wasn't found; pick something at random.
            if (delta > 0) delta = 1; else delta = -1;
            idx += (int)delta;
            if (idx < 0) idx = 0;
            if (idx >= ScreenList.Count) idx = ScreenList.Count - 1;
            SetScreen(ScreenList[idx]);
            uiFixedScreen.Text = ScreenBuffer.ToString();
            DoMeasure();
            ShowScreenSize();
            roamingSettings.Values["ScreenTypeIndex"] = idx;
        }

        private void OnScreenClose(object sender, TappedRoutedEventArgs e)
        {
            this.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }
        public void DoScreenOpen()
        {
            this.Visibility = Windows.UI.Xaml.Visibility.Visible;
            DoMeasure();
            ShowScreenSize();
        }

        private void OnBorderPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (moveCursor != null) Window.Current.CoreWindow.PointerCursor = moveCursor;
        }

        private void OnBorderPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (stockCursor != null) Window.Current.CoreWindow.PointerCursor = stockCursor;
        }

        private void OnMainGridPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (stockCursor != null) Window.Current.CoreWindow.PointerCursor = stockCursor;
        }

        private void OnMainGridPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (moveCursor != null) Window.Current.CoreWindow.PointerCursor = moveCursor;
        }

        bool trackingMovement = false;
        Point trackingPoint;
        private void OnBorderPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (Window.Current.CoreWindow.PointerCursor == moveCursor)
            {
                trackingMovement = true;
                var p = this.Parent as FrameworkElement;
                trackingPoint = e.GetCurrentPoint(p).Position;
                trackingPoint.X -= this.Margin.Left;
                trackingPoint.Y -= this.Margin.Top;
                (sender as FrameworkElement).CapturePointer(e.Pointer);
            }
        }

        private void OnBorderPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (trackingMovement)
            {
                var p = this.Parent as FrameworkElement;
                var currPoint = e.GetCurrentPoint(p).Position;
                var posX = currPoint.X - trackingPoint.X;
                var posY = currPoint.Y - trackingPoint.Y;
                this.Margin = new Thickness(posX, posY, 0, 0);
                roamingSettings.Values["ScreenMarginX"] = posX;
                roamingSettings.Values["ScreenMarginY"] = posY;
            }
            trackingMovement = false;
        }

        private void OnBorderPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!trackingMovement) return;

            // Move the windows!
            var p = this.Parent as FrameworkElement;
            var currPoint = e.GetCurrentPoint(p).Position;
            var posX = currPoint.X - trackingPoint.X;
            var posY = currPoint.Y - trackingPoint.Y;
            this.Margin = new Thickness(posX, posY, 0, 0);
        }


        public void MoveOnScreen(double parentW, double parentH)
        {
            if (parentH == 0 || parentW == 0) return;
            if (this.ActualHeight == 0 && this.ActualWidth == 0) return;
            if (this.ActualHeight == 0 || this.ActualWidth == 0)
            {
                // We are way off.  Move all the way to the left.
                var newX = 0.0;
                var newY = 0.0;
                this.Margin = new Thickness(newX, newY, 0, 0);
            }

            // Move the screen so that it's on-screen!
            var maxX = Math.Max(0, (parentW - this.ActualWidth));
            var maxY = Math.Max(0, (parentH - this.ActualHeight));
            if (this.Margin.Left > maxX || this.Margin.Top > maxY)
            {
                var newX = Math.Min(maxX, this.Margin.Left);
                var newY = Math.Min(maxY, this.Margin.Top);
                this.Margin = new Thickness(newX, newY, 0, 0);
            }
            else if ((this.Margin.Left + this.ActualWidth) < 50) // give is a little bit of margin; can't be entire off the screen.
            {
                var newX = 0;
                var newY = 0; 
                this.Margin = new Thickness(newX, newY, 0, 0);
            }
        }

        string restoreIssues = "";
        private void Restore()
        {
            restoreIssues = "";
            try
            {
                if (roamingSettings.Values.ContainsKey("ScreenTypeIndex"))
                {
                    var idx = (int)roamingSettings.Values["ScreenTypeIndex"];
                    var screen = ScreenList[idx];
                    SetScreen(screen);
                }
            }
            catch (Exception)
            {
                // Can't restore (e.g., because there's a newer chunk of data)?  No problem, just ignore.
                restoreIssues += "type ";
            }

            try
            {
                if (roamingSettings.Values.ContainsKey("ScreenMarginX"))
                {
                    var posX = (double)roamingSettings.Values["ScreenMarginX"];
                    var posY = (double)roamingSettings.Values["ScreenMarginY"];
                    this.Margin = new Thickness(posX, posY, 0, 0);
                }
            }
            catch (Exception)
            {
                // Can't restore (e.g., because there's a newer chunk of data)?  No problem, just ignore.
                restoreIssues += "Pos ";
            }


            try
            {
                if (roamingSettings.Values.ContainsKey("ScreenFontSize"))
                {
                    var cf = (double)roamingSettings.Values["ConsoleFontSize"];
                    var sf = (double)roamingSettings.Values["ScreenFontSize"];
                    uiConsole.FontSize = cf;
                    uiFixedScreen.FontSize = sf;
                }
            }
            catch (Exception)
            {
                // Can't restore (e.g., because there's a newer chunk of data)?  No problem, just ignore.
                restoreIssues += "Font ";
            }


            try
            {
                if (roamingSettings.Values.ContainsKey("ConsoleVisibility"))
                {
                    var vis = (Visibility)roamingSettings.Values["ConsoleVisibility"];
                    uiConsoleViewer.Visibility = vis;
                }
            }
            catch (Exception)
            {
                // Can't restore (e.g., because there's a newer chunk of data)?  No problem, just ignore.
                restoreIssues += "Vis ";
            }
            ShowScreenSize();
            DoMeasure();

        }

        private void OnTextLayoutUpdated(object sender, object e)
        {
            // The console width should be the same width as the text area.
            if (uiFixedScreen == null) return;
            var w = uiFixedScreen.ActualWidth;
            if (w < 10 || w > 9999) return;

            uiConsoleViewer.MinWidth = w;
        }

        bool HaveDownKey = false;
        Queue<string> AllKeys = new Queue<string>();
        public string Inkeys()
        {
            var gotFocus = this.Focus(FocusState.Programmatic);
            if (AllKeys.Count == 0) return "";

            var Retval = AllKeys.Dequeue();
            return Retval;
        }

        private void OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            var key = e.Key.ToString();
            var keyValue = (char)(int)e.Key;
            if (e.Key == VirtualKey.Space) key = " ";
            else if (keyValue >= 'A' && keyValue <= 'Z') key = keyValue.ToString();
            else if (keyValue >= '0' && keyValue <= '9') key = keyValue.ToString();
            e.Handled = true;
            // The first key (e.g., the first F5 to run a program) will come here without a corresponding up-key.
            // It needs to be suppressed, but only if it's the first up and there wasn't a corresponding down.
            if (HaveDownKey)
            {
                AllKeys.Enqueue(key);
            }
            HaveDownKey = false;
        }

        private void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            HaveDownKey = true;
            e.Handled = true;
        }
    }
}
