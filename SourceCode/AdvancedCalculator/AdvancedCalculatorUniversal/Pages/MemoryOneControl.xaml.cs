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
    public sealed partial class MemoryOneControl : UserControl
    {
        public MemoryOneControl()
        {
            this.InitializeComponent();
        }

        public void Initialize(IMemoryButtonHandler mbh, object Source)
        {
            uiMemory0.Init(mbh, Source, "simpleCalculator.Memory[0]", "simpleCalculator.MemoryNames[0]", "memory0", "mem0");
            uiMemory1.Init(mbh, Source, "simpleCalculator.Memory[1]", "simpleCalculator.MemoryNames[1]", "memory1", "mem1");
            uiMemory2.Init(mbh, Source, "simpleCalculator.Memory[2]", "simpleCalculator.MemoryNames[2]", "memory2", "mem2");
            uiMemory3.Init(mbh, Source, "simpleCalculator.Memory[3]", "simpleCalculator.MemoryNames[3]", "memory3", "mem3");
            uiMemory4.Init(mbh, Source, "simpleCalculator.Memory[4]", "simpleCalculator.MemoryNames[4]", "memory4", "mem4");
            uiMemory5.Init(mbh, Source, "simpleCalculator.Memory[5]", "simpleCalculator.MemoryNames[5]", "memory5", "mem5");
            uiMemory6.Init(mbh, Source, "simpleCalculator.Memory[6]", "simpleCalculator.MemoryNames[6]", "memory6", "mem6");
            uiMemory7.Init(mbh, Source, "simpleCalculator.Memory[7]", "simpleCalculator.MemoryNames[7]", "memory7", "mem7");
            uiMemory8.Init(mbh, Source, "simpleCalculator.Memory[8]", "simpleCalculator.MemoryNames[8]", "memory8", "mem8");
            uiMemory9.Init(mbh, Source, "simpleCalculator.Memory[9]", "simpleCalculator.MemoryNames[9]", "memory9", "mem9");
        }
    }
}
