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
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace AdvancedCalculator
{
    public sealed partial class MemoryRegisterControl : UserControl, INotifyPropertyChanged
    {
        public MemoryRegisterControl()
        {
            this.InitializeComponent();
            MemoryName="memName";
        }

        public IMemoryButtonHandler MemoryButtonHandler { get; set; }
        public void SetTextBoxBinding(Binding binding)
        {
            uiValue.SetBinding(TextBox.TextProperty, binding);
        }

        public void SetTextBoxBinding(object Source, string PathValue, string PathName)
        {
            Binding b = new Binding();
            b.Mode = BindingMode.TwoWay;
            b.Source = Source;
            b.Path = new PropertyPath(PathValue);
            uiValue.SetBinding(TextBox.TextProperty, b);

            b = new Binding();
            b.Mode = BindingMode.TwoWay;
            b.Source = Source;
            b.Path = new PropertyPath(PathName);
            uiName.SetBinding(TextBox.TextProperty, b);
        }

        public void Init(IMemoryButtonHandler mbh, object Source, string PathValue, string PathName, string Tag, string Name)
        {
            MemoryButtonHandler = mbh;
            SetTextBoxBinding(Source, PathValue, PathName);
            MemoryTag = Tag;
            MemoryName = Name;
        }

        public static readonly DependencyProperty MemoryTagProperty = 

            DependencyProperty.Register("MemoryTag",
                typeof(string),
                typeof(MemoryRegisterControl),
                new PropertyMetadata("memory99"));
 
        public string MemoryTag
        {
            get { return (string)this.GetValue(MemoryTagProperty); }
            set { this.SetValue(MemoryTagProperty, value); }
        }

        public string MemoryName { get; set; }



        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        private void OnFromCalc(object sender, RoutedEventArgs e)
        {
            MemoryButtonHandler.OnFromCalc(sender, e);

        }
        private void OnToCalc(object sender, RoutedEventArgs e)
        {
            MemoryButtonHandler.OnToCalc(sender, e);

        }

        private void OnMemoryPlus(object sender, RoutedEventArgs e)
        {
            MemoryButtonHandler.OnMemoryPlus(sender, e);

        }

        private void OnMemoryMinus(object sender, RoutedEventArgs e)
        {
            MemoryButtonHandler.OnMemoryMinus(sender, e);

        }
        private void OnMemoryClear(object sender, RoutedEventArgs e)
        {
            var tag = (sender as FrameworkElement).Tag as string;
            var tag2 = (sender as FrameworkElement).Tag as string;
            MemoryButtonHandler.OnMemoryClear(sender, e);
        }

    }
}
