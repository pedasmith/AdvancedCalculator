﻿using Shipwreck.Utilities;
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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace AdvancedCalculator
{
    public sealed partial class FeedbackFourPanelControl : UserControl, IInitializeAppDetails
    {
        public FeedbackFourPanelControl()
        {
            this.InitializeComponent();
        }
        public void Initialize(IGetAppDetails details)
        {
            uiMain.Initialize();
            FeedbackControl fbc = null;
            if (uiMain.ItemMain is Border)
            {
                fbc = (uiMain.ItemMain as Border).Child as Shipwreck.Utilities.FeedbackControl;
            }
            else
            {
                fbc = uiMain.ItemMain as Shipwreck.Utilities.FeedbackControl;
            }
            fbc.GetAppDetails = details;
        }
    }
}
