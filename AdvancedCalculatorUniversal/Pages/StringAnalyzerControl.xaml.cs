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
    public sealed partial class StringAnalyzerControl : UserControl, IInitializeShare
    {
        UnicodeDictionary unicodeDictionary { get; set; }
        ObservableCollection<UnicodeData> unicodeAllFound { get; set; }
        int unicodeCurrentStart { get; set; }
        //bool dictionaryIsInit = false;
        public StringAnalyzerControl()
        {
            unicodeAllFound = new ObservableCollection<UnicodeData>();
            this.InitializeComponent();
            Loaded += async (s, e) =>
            {
                uiStringAnalyzeInfo = uiMain.ItemMain.FindName("uiStringAnalyzeInfo") as TextBlock;
                uiResultsList = uiMain.ItemMain.FindName("uiResultsList") as StackPanel;

                unicodeDictionary = new UnicodeDictionary();
                await unicodeDictionary.Init();
                //dictionaryIsInit = true;
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
                uiResultsList.MaxWidth = w;
                uiResultsList.MinWidth = 200;
            }
            else
            {
                uiResultsList.MaxWidth = preferredWidth;
                uiResultsList.MinWidth = preferredWidth;
            }
        }

        IShare Share;
        public void Initialize(IShare share) //SimpleCalculator simpleCalculator)
        {
            Share = share;
            uiMain.Initialize();
        }


        private string LastUnicodeSearch = "";
        private void DoAnalyzeString(string searchstr)
        {
            LastUnicodeSearch = searchstr;
            unicodeAllFound.Clear();
            foreach (var ch in searchstr)
            {
                var ustr = $"U+{(uint)ch:X}";
                unicodeDictionary.Search(ustr, unicodeAllFound);
            }
            unicodeCurrentStart = 0;
            if (unicodeAllFound.Count == 0)
            {
                string message = "No unicode chars found";
                uiStringAnalyzeInfo.Text = message;
            }
            else
            {
                string message = String.Format("{0} unicode chars found", unicodeAllFound.Count);
                uiStringAnalyzeInfo.Text = message;
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
            uiResultsList.Children.Clear();
            //uiUnicodeList.Items.Clear();
            for (int i = unicodeCurrentStart; i < unicodeCurrentStart + N; i++)
            {
                var entry = new AsciiPlusUnicodeControl(unicodeAllFound[i]);
                uiResultsList.Children.Add(entry);
            }
        }

        private void OnSearchUnicode(object sender, RoutedEventArgs e)
        {
            var search = sender as TextBox;
            if (search == null)
            {
                return;
            }
            DoAnalyzeString(search.Text);
        }

        private void OnAnalyzeStringTextChanged(object sender, TextChangedEventArgs e)
        {
            //if (e.Changes.Count == 0) return;
            var search = sender as TextBox;
            if (search == null)
            {
                return;
            }
            DoAnalyzeString(search.Text);
        }
    }
}
