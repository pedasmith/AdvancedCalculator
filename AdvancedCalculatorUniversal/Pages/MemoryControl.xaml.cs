using Windows.UI.Xaml.Controls;

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
