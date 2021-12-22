using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml.Input;

namespace Edit
{
    // Duplicate of KeyEventArgs in a form that can be instantiated.
    public class MyKeyEventArgs
    {
        public MyKeyEventArgs(KeyEventArgs args)
        {
            this.DeviceId = "(none)"; // args.DeviceId;
            this.Handled = args.Handled;
            this.KeyStatus = args.KeyStatus;
            this.VirtualKey = args.VirtualKey;
        }
        public MyKeyEventArgs(KeyRoutedEventArgs args)
        {
            this.DeviceId = "(none)"; // args.DeviceId;
            this.Handled = args.Handled;
            this.KeyStatus = args.KeyStatus;
            this.VirtualKey = args.Key;
        }

        public System.String DeviceId { get; set; }
        public System.Boolean Handled { get; set; }
        public CorePhysicalKeyStatus KeyStatus { get; set; }
        public VirtualKey VirtualKey { get; set; }
    }
}
