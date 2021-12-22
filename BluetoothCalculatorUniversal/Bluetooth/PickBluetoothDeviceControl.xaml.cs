using BCBasic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace AdvancedCalculator.Bluetooth
{
    public sealed partial class PickBluetoothDeviceControl : UserControl
    {
        public PickBluetoothDeviceControl()
        {
            this.DataContext = this;
            this.InitializeComponent();
            this.Loaded += (s, e) => {
                if (Devices.Count > 0)
                {
                    uiDeviceList.SelectedIndex = 0;
                }
            };
        }
        public ObservableCollection<IBluetoothDevice> Devices { get; } = new ObservableCollection<IBluetoothDevice>();
        public int Initialize(ObjectList devices)
        {
            uiEditName.Visibility = Visibility.Collapsed;
            Devices.Clear();
            foreach (var obj in devices.data)
            {
                if (obj is IBluetoothDevice)
                {
                    Devices.Add(obj as IBluetoothDevice); 
                }
            }
            return Devices.Count;
        }

        public IBluetoothDevice GetSelected()
        {
            IBluetoothDevice Retval = uiDeviceList.SelectedItem as IBluetoothDevice;
            return Retval;
        }

        IBluetoothDevice editedDevice = null;
        private void OnEditName(object sender, TappedRoutedEventArgs e)
        {
            if (CancelPopup != null)
            {
                CancelPopup.CancelPopup(Reason.EditName);
                return;
            }

            var device = (sender as FrameworkElement).DataContext as IBluetoothDevice;
            if (device == null) return;

            editedDevice = device;
            uiEditNameBox.Text = editedDevice.UserPickedName;
            uiEditName.Visibility = Visibility.Visible;
        }

        private void OnEditNameOK(object sender, RoutedEventArgs e)
        {
            if (editedDevice != null)
            {
                editedDevice.UserPickedName = uiEditNameBox.Text;
            }
            uiEditName.Visibility = Visibility.Collapsed;
            this.DataContext = null;
            this.DataContext = this;
        }

        private void OnEditNameCancel(object sender, RoutedEventArgs e)
        {
            uiEditName.Visibility = Visibility.Collapsed;
        }

        public ICancelPopup CancelPopup { get; set; } = null;
    }

    public enum Reason { None, EditName };
    public interface ICancelPopup
    {
        void CancelPopup(Reason r);
    }
    public class PickDevice : ICancelPopup
    {
        public async Task<RunResult.RunStatus> DoPickDevice(ObjectList list, BCValue Retval)
        {
            var status = RunResult.RunStatus.OK;
            var keepGoing = true;
            while (keepGoing)
            {
                var pickstatus = await DoPickDeviceOne(list, Retval);
                switch (pickstatus)
                {
                    case PickResult.EditName:
                        // Pop up the edit dialog!
                        await DoEditDeviceNameAsync(Retval.AsObject as IBluetoothDevice);
                        break;
                    default:
                        keepGoing = false;
                        break;
                }
            }
            return status;
        }

        private async Task<bool> DoEditDeviceNameAsync(IBluetoothDevice device)
        {
#if !WINDOWS8
            var picker = new TextBox() { MinWidth = 200, Text = device.UserPickedName };
            dialog = new ContentDialog()
            {
                Title = "Edit name",
                MaxWidth = 500,
                Content = picker,
                PrimaryButtonText = "OK",
                IsPrimaryButtonEnabled = true,
                SecondaryButtonText = "Cancel",
                IsSecondaryButtonEnabled = true
            };
            var result = await dialog.ShowAsync();
            switch (result)
            {
                case ContentDialogResult.Primary:
                    device.UserPickedName = picker.Text;
                    break;
            }
#else
            await Task.Delay(0);
#endif
            return true;
        }

        Reason CancelReason = Reason.EditName;
        public void CancelPopup (Reason r)
        {
            switch (r)
            {
                case Reason.EditName:
                    CancelReason = Reason.EditName;
#if !WINDOWS8
                    dialog.Hide();
#endif
                    break;
            }
        }
#if !WINDOWS8
        ContentDialog dialog = null;
#else
        MessageDialog dialog = null;
#endif
        enum PickResult {  Cancel, NoDevice, Selected, Failed, EditName };
        private async Task<PickResult> DoPickDeviceOne(ObjectList list, BCValue Retval)
        {
            var picker = new PickBluetoothDeviceControl();
            var count = picker.Initialize(list);
            if (count == 0)
            {
                var message = new MessageDialog($"No Blueooth devices were found");
                await message.ShowAsync();
                Retval.SetError(3, $"PickDevicesName:: no devices match the string");
                return PickResult.NoDevice;
            }
            else
            {
#if !WINDOWS8
                dialog = new ContentDialog()
                {
                    Title = "Bluetooth Picker",
                    MaxWidth = 500,
                    Content = picker,
                    PrimaryButtonText = "OK",
                    IsPrimaryButtonEnabled = true,
                    SecondaryButtonText = "Cancel",
                    IsSecondaryButtonEnabled = true
                };
                picker.CancelPopup = this;
                CancelReason = Reason.None;
                var result = await dialog.ShowAsync();
                var selected = picker.GetSelected();
                switch (CancelReason)
                {
                    case Reason.EditName:
                        if (selected == null)
                        {
                            Retval.SetError(3, "PickDevicesName:: no devices selected");
                            return PickResult.Failed; // this can't really happen
                        }
                        else
                        {
                            Retval.AsObject = selected;
                            return PickResult.EditName;
                        }
                    default:
                        switch (result)
                        {
                            case ContentDialogResult.Secondary:
                                Retval.SetError(2, "PickDevicesName:: cancel");
                                return PickResult.Cancel;
                            default:
                            case ContentDialogResult.Primary:
                            case ContentDialogResult.None:
                                if (selected == null)
                                {
                                    Retval.SetError(2, "PickDevicesName:: no devices selected");
                                    return PickResult.Cancel;
                                }
                                else
                                {
                                    Retval.AsObject = selected;
                                    return PickResult.Selected;
                                }
                        }
                }
#else
                return PickResult.Cancel;
#endif
            }
        }
    }
}
