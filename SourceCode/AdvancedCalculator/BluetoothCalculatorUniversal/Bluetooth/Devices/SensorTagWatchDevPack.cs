using BCBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;

namespace AdvancedCalculator.Bluetooth.Devices
{
    // This class is like a specialization of a specialization; it will
    // allow other devices (like the SensorTag) to have an "add on" experience.
    // The SensorTagWatchDevPack is an add-on for the "real" Bluetooth device.
    // The 'parent' object will be a SensorTag1350 or SensorTag2650 (or more, in the future)
    class SensorTagWatchDevPack // LCD Display (Watch) DevPack add-on for SensorTag
    {

        BluetoothDevice Device = null;
        BluetoothLEDevice Ble = null;
        IObjectValue Parent = null; // Will be SensorTag1350 or SensorTag2650
        public SensorTagWatchDevPack(IObjectValue parent, BluetoothDevice device, BluetoothLEDevice ble)
        {
            Device = device;
            Ble = ble;
        }

        // LCD Display (Watch) Service for SensorTag
        // http://processors.wiki.ti.com/index.php/CC26xx_LCD#LCD_in_the_SensorTag_application
        // http://processors.wiki.ti.com/index.php/Display_DevPack_User_Guide
        // https://git.ti.com/sensortag-20-android/sensortag-20-android/trees/master/sensortag20/BleSensorTag/src/main/java/com/example/ti/ble/sensortag
        private string WatchService = "f000ad00-0451-4000-b000-000000000000";
        private string WatchData = "f000ad01-0451-4000-b000-000000000000";
        private string WatchConfig = "f000ad02-0451-4000-b000-000000000000";

        public async Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            switch (name)
            {
                // The watch screen is 12 lines by 16 columns

                case "WatchClearLine":
                    {
                        if (!BCObjectUtilities.CheckArgs(1, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var row = (await ArgList[0].EvalAsync(context)).AsDouble;

                        var buffer = new byte[] { 4, (byte)row };
                        var iostatus = await BluetoothDevice.DoWriteBytes(Ble, WatchService, WatchConfig, buffer, Retval);

                        return iostatus;
                    }

                case "WatchCls":
                    {
                        if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        byte[] buffer = { 3 };
                        var iostatus = await BluetoothDevice.DoWriteBytes(Ble, WatchService, WatchConfig, buffer, Retval);
                        buffer = new byte[] { 6, 0, 0 };
                        iostatus = await BluetoothDevice.DoWriteBytes(Ble, WatchService, WatchConfig, buffer, Retval);
                        return iostatus;
                    }


                case "WatchInvert":
                    {
                        if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        byte[] buffer = { 5 };
                        var iostatus = await BluetoothDevice.DoWriteBytes(Ble, WatchService, WatchConfig, buffer, Retval);
                        return iostatus;
                    }

                case "WatchOff":
                    {
                        if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        byte[] buffer = { 1 };
                        var iostatus = await BluetoothDevice.DoWriteBytes(Ble, WatchService, WatchConfig, buffer, Retval);
                        return iostatus;
                    }

                case "WatchOn":
                    {
                        if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        byte[] buffer = { 2 };
                        var iostatus = await BluetoothDevice.DoWriteBytes(Ble, WatchService, WatchConfig, buffer, Retval);
                        return iostatus;
                    }

                case "WatchPrint":
                    {
                        if (!BCObjectUtilities.CheckArgs(1, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var text = (await ArgList[0].EvalAsync(context)).AsString;
                        RunResult.RunStatus iostatus = RunResult.RunStatus.OK;

                        var buffer = System.Text.UTF8Encoding.UTF8.GetBytes(text);
                        var bufferList = Utilities.BluetoothUtilities.TextToByteArray(text);
                        foreach (var textBuffer in bufferList)
                        {
                            iostatus = await BluetoothDevice.DoWriteBytes(Ble, WatchService, WatchData, textBuffer, Retval);
                        }
                        return iostatus;
                    }

                case "WatchPrintAt":
                    {
                        if (!BCObjectUtilities.CheckArgs(3, 3, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var row = (await ArgList[0].EvalAsync(context)).AsDouble;
                        var col = (await ArgList[1].EvalAsync(context)).AsDouble;
                        var text = (await ArgList[2].EvalAsync(context)).AsString;
                        RunResult.RunStatus iostatus = RunResult.RunStatus.OK;

                        var buffer = new byte[] { 6, (byte)row, (byte)col };
                        iostatus = await BluetoothDevice.DoWriteBytes(Ble, WatchService, WatchConfig, buffer, Retval);

                        var bufferList = Utilities.BluetoothUtilities.TextToByteArray(text);
                        foreach (var textBuffer in bufferList)
                        {
                            iostatus = await BluetoothDevice.DoWriteBytes(Ble, WatchService, WatchData, textBuffer, Retval);
                        }

                        return iostatus;
                    }


                default:
                    await Task.Delay(0);
                    BCValue.MakeNoSuchMethod(Retval, Parent, name);
                    return RunResult.RunStatus.ErrorContinue;
            }
        }
    }
}
