using AdvancedCalculator.Bluetooth;
using BCBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;
using static AdvancedCalculator.Bluetooth.BluetoothDevice;

namespace AdvancedCalculator.Bluetooth.Devices
{
    // The 1350 has the same basic interface as the 2650.
    // The 2650 was made first, but the 1350 was supported first in Best Calculator, IOT edition,
    // so the implementation is in the 1350 class.
    class SensorTag2650 : SensorTag1350
    {
        public SensorTag2650(BluetoothDevice device, BluetoothLEDevice ble)
            :base(device, ble)
        {
        }

        public override string PreferredName { get { return "SensorTag2650"; } }

    }
}
