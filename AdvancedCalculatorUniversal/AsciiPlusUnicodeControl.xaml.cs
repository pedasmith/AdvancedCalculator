using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public sealed partial class AsciiPlusUnicodeControl : UserControl
    {
        public AsciiPlusUnicodeControl()
        {
            this.InitializeComponent();
        }

        public AsciiPlusUnicodeControl(UnicodeData data)
        {
            this.InitializeComponent();
            Set(data);
        }

        public UnicodeData Data { get; set; }
        private bool _Selected = false;
        public bool Selected
        {
            get { return _Selected; }
            set {
                if (_Selected == value) return;
                _Selected = value;
                uiBorder.BorderThickness = new Thickness (_Selected ? 4 : 0);
            }
        }

        static FontFamily SymbolFontFamily = null;
        static FontFamily OtherFontFamily = null;
        public void Set(UnicodeData data)
        {
            uiChar.Text = data.UnicodeString;

            //uiDec.Text = Convert.ToString(data.UTF32, 10);
            //uiHex.Text = Convert.ToString(data.UTF32, 16);
            uiOther.Text = data.NeedsUPlusNameWindows ? String.Format ("{0} ({1})", data.UPlusName, data.UPlusNameWindows) : data.UPlusName;
            uiName.Text = data.Name;
            uiBlock.Text = data.Block;
            Data = data;
            if (data.CharacterType == CharacterType.Symbol)
            {
                if (SymbolFontFamily == null)
                {
                    SymbolFontFamily = new FontFamily("Segoe MDL2 Assets,Segoe UI Symbol,Cascadia Code,Cascadia Code NF");
                }
                uiChar.FontFamily = SymbolFontFamily;
                //uiChar.FontFamily = new FontFamily("Segoe MDL2 Assets");
            }
            else
            {
                if (OtherFontFamily == null)
                {
                    OtherFontFamily = new FontFamily("Segoe UI,Segoe MDL2 Assets,Segoe UI Symbol,Cascadia Code,Cascadia Code NF");
                }
                uiChar.FontFamily = OtherFontFamily;
            }
        }

    }
}
