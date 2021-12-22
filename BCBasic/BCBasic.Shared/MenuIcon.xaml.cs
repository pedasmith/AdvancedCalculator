using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace BCBasic
{
    public sealed partial class MenuIcon : UserControl, INotifyPropertyChanged
    {
        private string _AltText = "";
        public string AltText { get { return _AltText; } set { if (value == _AltText) return; _AltText = value; NotifyPropertyChanged(); } }

        private double _IconFontSize = 35;
        public double IconFontSize { get { return _IconFontSize; } set { if (value == _IconFontSize) return; _IconFontSize = value; NotifyPropertyChanged(); } }

        private string _MenuText = "!!";
        public string MenuText { get { return _MenuText; } set { if (value == _MenuText) return; _MenuText = value; NotifyPropertyChanged(); } }

        private string _ConfirmText = "";
        public string ConfirmText { get { return _ConfirmText; } set { if (value == _ConfirmText) return; _ConfirmText = value; NotifyPropertyChanged(); } }




        // Using a DependencyProperty as the backing store for MenuTextEZ.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MenuTextEZProperty =
            DependencyProperty.Register("MenuTextEZ", typeof(string), typeof(MenuIcon), new PropertyMetadata("", (s, e) => { (s as MenuIcon).OnChangedMenuTextEZ(); })); 

        //private string _MenuTextEZ = "!!";
        public string MenuTextEZ 
        {
            get { return (string)GetValue(MenuTextEZProperty); } 
            set 
            { 
                if (value == MenuTextEZ) return;
                SetValue(MenuTextEZProperty, value);
                OnChangedMenuTextEZ();
                NotifyPropertyChanged(); 
            } 
        }

        private void OnChangedMenuTextEZ()
        {
            switch (MenuTextEZ)
            {
                case "[_]": MenuText = ""; break; // APP BAR WINDOW
                case "+": MenuText = ""; break; // ADD

                case "oldABCD": MenuText = "🄰 🄱\n🄲 🄳"; IconFontSize /= 1.8; break;  // SQUARED LATIN CAPITAL LETTER A
                case "t1_ABCD": MenuText = "🅿🆁🅾🅶\n❶❷❸❹"; IconFontSize /= 1.8; break;  // COMBINING ENCLOSING SQUARE (?)
                case "ABCD": MenuText = "⦉𝟏𝟐–\n𝟑𝟒𝟓⦊"; IconFontSize /= 1.4; break;  // Z NOTATION LEFT BINDING ; BRACKET MATHMATICAL BOLD DIGIT
                case "BACK": MenuText = ""; break; // APPBAR BACK ARROW
                case "BOOK": MenuText = "📓"; break; //NOTEBOOK 
                case "CLS": MenuText = "⎚"; break; // CLEAR SCREEN SYMBOL (not an appbar glyph)
                case "COPY": MenuText = ""; break; // APPBAR EMAIL COPY PAGE
                case "DELETE": MenuText = ""; break; // APPBAR GLYPH TRASH
                case "EDIT": MenuText = ""; break; // APPBAR GLYPH EDIT
                case "F+": MenuText = ""; break; // FONT SIZE INCREASE
                case "F-": MenuText = ""; IconFontSize -= 10; break; // FONT SIZE DECREASE
                case "FLAGS": MenuText = "🎌"; break; // CROSSED FLAGS 
                case "GEAR": MenuText = ""; break; //SETTINGS GEAR
                case "HELP": MenuText = ""; break; //APPBAR HELP QUESTION MARK
                case "HOME": MenuText = ""; break; //APPBAR GLYPH OPEN IN WEB GLOBE
                case "LOCKED": MenuText = ""; break; //APPBAR GLYPH LOCK
                case "PRETTYPRINT": MenuText = ""; IconFontSize /= 1.2; break; // APPBAR GLYPH EDIT ALIGN LEFT 
                case "READ": MenuText = ""; break; // APPBAR EDIT OPEN FILE PAGE
                case "RUN": MenuText = ""; break; //MEDIA PLAY
                case "SAVE": MenuText = ""; break; //APPBAR GLYPH SAVE
                case "SAVEAS": MenuText = ""; break; //APPBAR GLYPH SAVE AS
                case "STOP": MenuText = "❕"; break; //WHITE EXCLAMATION MARK ORNAMENT
                case "S+": MenuText = ""; break; // ADD
                case "S-": MenuText = ""; break; // REMOVE?
                case "TEST": MenuText = "✔"; break; //HEAVY CHECK MARK (dingbats)
                case "UNLOCK": MenuText = ""; break; // MICROSOFT SYMBOL APPBAR GLYPH UNLOCK
                case "X": MenuText = ""; break; // CANCEL
                default: MenuText = "!YYY!"; break; // ever gets called?
            }
        }

        public Brush HighlightBrush = new SolidColorBrush(Colors.Gray);
        private Brush _NormalBrush = null;
        public Brush NormalBrush { get { return _NormalBrush; } set { bool isNormal = (uiMain.Background == _NormalBrush); _NormalBrush = value; if (isNormal) uiMain.Background = NormalBrush; } }

        public MenuIcon()
        {
            // No -- doesn't work to set this.Context = this
            // The {Bind } stuff has to distinguish between bindings set by the internals of
            // the user control and the ones set when you create the user control.
            // Instead, you have have to (this.Content as FrameworkElement).DataContext = this;
            // Binding and UserControls:
            // http://blog.jerrynixon.com/2013/07/solved-two-way-binding-inside-user.html
            this.InitializeComponent();
            (this.Content as FrameworkElement).DataContext = this;
            NormalBrush = uiMain.Background;

            PropertyChanged += (s, e) =>
            {
                switch (e.PropertyName)
                {
                    case "ConfirmText":
                        if (ConfirmText != "")
                        {
                            // Make a flyout
                            var sp = new StackPanel();
                            sp.Children.Add(new TextBlock() { Text = ConfirmText, FontSize = 20, TextWrapping = TextWrapping.Wrap, MaxWidth= 300 });
                            var b = new Button() { Content = "Yes" };
                            b.Tapped += OnConfirmButtonTapped;
                            sp.Children.Add(b);
                            confirmFlyoutOk = false;
                            confirmFlyout = new Flyout();
                            confirmFlyout.Content = sp;
                            confirmFlyout.Closed += (ss, ee) => { confirmFlyoutIsShown = false; };

                            FlyoutBase.SetAttachedFlyout(uiButton, confirmFlyout);
                        }
                        break;
                }
            };
        }

        void OnConfirmButtonTapped(object sender, TappedRoutedEventArgs e)
        {
            confirmFlyoutOk = true;
            confirmFlyoutIsShown = false;
            confirmFlyout.Hide();
        }

        private Flyout confirmFlyout = null;
        private bool confirmFlyoutIsShown = false;
        private bool confirmFlyoutOk = false;
        public async Task<bool> DoConfirmAsync()
        {
            bool Retval = false;
            if (confirmFlyout != null)
            {
                confirmFlyoutIsShown = true;
                confirmFlyout.ShowAt(uiButton);

                while (confirmFlyoutIsShown)
                {
                    await Task.Delay (100);
                }
                Retval = confirmFlyoutOk;
            }
            return Retval;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void OnPress(object sender, PointerRoutedEventArgs e)
        {
            uiMain.Background = HighlightBrush;
        }

        private void OnRelease(object sender, PointerRoutedEventArgs e)
        {
            uiMain.Background = NormalBrush;
        }

        private void OnExited(object sender, PointerRoutedEventArgs e)
        {
            uiMain.Background = NormalBrush;
        }

        public void SetForeground(Brush brush)
        {
            uiButton.Foreground = brush;
        }
    }
}
