﻿using System;
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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace AdvancedCalculator
{
    public sealed partial class MemoryControl : UserControl, IInitializeMemory
    {
        public MemoryControl()
        {
            this.InitializeComponent();
        }
        public void Inialize(IMemoryButtonHandler mbh, object Source)
        {
            uiMain.Initialize();
            this.Loaded += (s, e) =>
            {
                (uiMain.ItemMain as MemoryOneControl).Initialize(mbh, Source);
            };
        }
    }
}
