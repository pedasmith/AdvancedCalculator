using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Edit
{
    public sealed partial class EditGotoDialog : ContentDialog
    {
        public EditGotoDialog(string startingText, GotoType gotoType)
        {
            this.InitializeComponent();
            this.Loaded += (s, e) =>
            {
                switch (gotoType)
                {
                    case GotoType.Line: uiGoto.SelectedIndex = 0; break;
                    case GotoType.Character: uiGoto.SelectedIndex = 1; break;
                }
                Text = startingText;
                uiText.Text = Text;
                uiText.SelectAll();

            };
        }
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text", typeof(string), typeof(EditGotoDialog), new PropertyMetadata(default(string)));

        public enum GotoType {  Line, Character }
        public GotoType CurrGotoType { get; private set; } = GotoType.Line;
        private void SetCurrGotoType()
        {
            var value = (uiGoto.SelectedItem as FrameworkElement).Tag as string;
            CurrGotoType = (value == "line") ? GotoType.Line : GotoType.Character;
        }
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); SetCurrGotoType();  }
        }
        public bool ExitViaReturn { get; internal set; } = false;

        private void ContentDialog_FindClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // For no obvious reason the Text value isn't set correctly.
            var txt = uiText.Text;
            var setText = Text;
            if (txt != setText)
            {
                Text = txt;
            }
            SetCurrGotoType();
        }

        private void ContentDialog_CancelClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                Text = uiText.Text;
                ExitViaReturn = true;
                Hide();
            }
        }
    }
}
