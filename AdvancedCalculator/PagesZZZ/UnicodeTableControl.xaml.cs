using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
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
    public sealed partial class UnicodeTableControl : UserControl, IInitializeShare
    {
        UnicodeDictionary unicodeDictionary { get; set; }
        ObservableCollection<UnicodeData> unicodeAllFound { get; set; }
        int unicodeCurrentStart { get; set; }
        bool dictionaryIsInit = false;
        public UnicodeTableControl()
        {
            unicodeAllFound = new ObservableCollection<UnicodeData>();
            this.InitializeComponent();
            Loaded += async (s, e) =>
            {
                uiUnicodeSearchInfo = uiMain.ItemMain.FindName("uiUnicodeSearchInfo") as TextBlock;
                uiUnicodeList = uiMain.ItemMain.FindName("uiUnicodeList") as StackPanel;
                uiSearchUnicodeNext = uiMain.ItemMain.FindName("uiSearchUnicodeNext") as Button;
                uiSearchUnicodePrev = uiMain.ItemMain.FindName("uiSearchUnicodePrev") as Button;

                unicodeDictionary = new UnicodeDictionary();
                await unicodeDictionary.Init();
                dictionaryIsInit = true;
                //var str = GenerateMicrosoftSymbols(); // NOTE: don't do this for shipping; it's a waste of time.
            };
            this.SizeChanged += UnicodeTableControl_SizeChanged;
        }

        private void UnicodeTableControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var preferredWidth = 650;
            var w = e.NewSize.Width;
            if (w < preferredWidth)
            {
                uiUnicodeList.MaxWidth = w;
                uiUnicodeList.MinWidth = 200;
            }
            else
            {
                uiUnicodeList.MaxWidth = preferredWidth;
                uiUnicodeList.MinWidth = preferredWidth;
            }
        }

        IShare Share;
        public void Initialize(IShare share) //SimpleCalculator simpleCalculator)
        {
            //this.simpleCalculator = simpleCalculator;
            Share = share;
            uiMain.Initialize();
            Loaded += (s, e) =>
            {
                //solver = new SolverWPFMetro(new TemperatureSolver(), uiMain.ItemMain as Grid);
            };
        }

        // Call this to do a search.  partname should be "search" (Was DoSEarchAsync())
        public async Task SetStringAsync(string partname, string searchstr)
        {
            while (!dictionaryIsInit)
            {
                await Task.Delay(100);
            }
            switch (partname)
            {
                case "search":
                    uiSearchBox.Text = searchstr;
                    DoSearchUnicode(searchstr);
                    break;
            }
        }
        private string LastUnicodeSearch = "";
        private void DoSearchUnicode(string searchstr)
        {
            LastUnicodeSearch = searchstr;
            unicodeAllFound.Clear();
            unicodeDictionary.Search(searchstr, unicodeAllFound);
            unicodeCurrentStart = 0;
            if (unicodeAllFound.Count == 0)
            {
                string message = "No unicode chars found";
                uiUnicodeSearchInfo.Text = message;
            }
            else
            {
                string message = String.Format("{0} unicode chars found", unicodeAllFound.Count);
                uiUnicodeSearchInfo.Text = message;
            }
            DoDisplayUnicode();
        }

        const int UnicodeListMaxRow = 10;
        private void DoDisplayUnicode()
        {
            while (unicodeCurrentStart > unicodeAllFound.Count)
            {
                unicodeCurrentStart -= UnicodeListMaxRow;
            }
            if (unicodeCurrentStart < 0)
            {
                unicodeCurrentStart = 0;
            }
            int canDisplay = unicodeAllFound.Count - unicodeCurrentStart;
            int N = (int)Math.Min(canDisplay, UnicodeListMaxRow);
            uiUnicodeList.Children.Clear();
            //uiUnicodeList.Items.Clear();
            for (int i = unicodeCurrentStart; i < unicodeCurrentStart + N; i++)
            {
                var entry = new AsciiPlusUnicodeControl(unicodeAllFound[i]);
                uiUnicodeList.Children.Add(entry);
            }
            bool prevVisible = (unicodeCurrentStart > 0);
            uiSearchUnicodePrev.Visibility = prevVisible ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
            bool nextVisible = ((unicodeCurrentStart + UnicodeListMaxRow) < unicodeAllFound.Count);

            uiSearchUnicodeNext.Visibility = nextVisible ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
            SetAllUnicodeForClipboard();
        }

        private void OnSearchUnicode(object sender, RoutedEventArgs e)
        {
            var search = sender as TextBox;
            if (search == null)
            {
                return;
            }
            DoSearchUnicode(search.Text);
        }

        private void OnSearchUnicodeNext(object sender, RoutedEventArgs e)
        {
            unicodeCurrentStart += UnicodeListMaxRow;
            DoDisplayUnicode(); // Will adjust start to be correct
        }
        private void OnSearchUnicodePrev(object sender, RoutedEventArgs e)
        {
            unicodeCurrentStart -= UnicodeListMaxRow;
            DoDisplayUnicode(); // Will adjust start to be correct

        }

        private void OnSearchUnicodeTextChanged(object sender, TextChangedEventArgs e)
        {
            //if (e.Changes.Count == 0) return;
            var search = sender as TextBox;
            if (search == null)
            {
                return;
            }
            DoSearchUnicode(search.Text);
        }

        private void OnUnicodeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count < 1) return;
            string str = "";
            foreach (var item in e.AddedItems)
            {
                var uc = item as AsciiPlusUnicodeControl;
                if (uc == null) continue;
                str += uc.Data.UnicodeString;
            }

            Share.ShareTitle = "Unicode characters from Best Calculator";
            Share.ShareString = str;

        }


        private void SetAllUnicodeForClipboard()
        {
            var str = "";
            foreach (var item in uiUnicodeList.Children)
            {
                var apuc = item as AsciiPlusUnicodeControl;
                if (apuc != null)
                {
                    str += $"{apuc.Data.UnicodeString} {apuc.Data.NameUC} {apuc.Data.UPlusNameWindows}\n";
                }
            }

            Share.ShareTitle = String.Format("Unicode character {0} from Best Calculator", LastUnicodeSearch);
            Share.ShareString = str;
        }

        private void OnUnicodeListClicked(object sender, TappedRoutedEventArgs e)
        {
            // Find the AsciiPlusUnicodeControl 
            FrameworkElement tapped = e.OriginalSource as FrameworkElement;
            AsciiPlusUnicodeControl found = null;

            while (found == null)
            {
                if (tapped == null) break;
                found = tapped as AsciiPlusUnicodeControl;
                if (found == null)
                {
                    tapped = tapped.Parent as FrameworkElement;
                }
            }
            if (found != null)
            {
                foreach (var item in uiUnicodeList.Children)
                {
                    var apuc = item as AsciiPlusUnicodeControl;
                    if (apuc != null)
                    {
                        apuc.Selected = false;
                    }
                }
                found.Selected = true;
                Share.ShareTitle = String.Format("Unicode character {0} from Best Calculator", found.Data.UPlusName);
                Share.ShareString = found.Data.UnicodeString;
            }
        }

        private string GenerateMicrosoftSymbols()
        {
            string Retval = "";

            //var symbols = typeof(Symbol).GetMembers(BindingFlags.Public);
            var type = typeof(Symbol);
            var values = System.Enum.GetValues(type);
            var namelist = new SortedDictionary<int, string>();
            foreach (var value in values)
            {
                //int value = symbol.GetType().Name();
                var name = System.Enum.GetName(type, value);
                namelist.Add((int)value, name);
            }

            foreach (var entry in namelist)
            {
                // E001;CHECK MARK;So;0;ON;;;;;N;;;;;
                Retval += $"{entry.Key:X4};APPBAR GLYPH {entry.Value.ToUpper()};So;0;ON;;;;;N;;;;;\n";
            }

            return Retval;
        }

    }
}
