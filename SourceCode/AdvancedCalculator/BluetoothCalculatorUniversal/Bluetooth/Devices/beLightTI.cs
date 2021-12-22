using AdvancedCalculator.Bluetooth;
using BCBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace AdvancedCalculator.Bluetooth.Devices
{
    class beLight : IObjectValue
    {
        BluetoothLEDevice Device = null;
        public beLight(BluetoothLEDevice device)
        {
            Device = device;
        }

        public string PreferredName { get { return "beLight"; } }

        public IList<string> GetNames() { return new List<string>() { "Methods" }; }

        public BCValue GetValue(string name)
        {
            switch (name)
            {
                case "Methods":
                    return new BCValue("GetName,SetColor,ToString");
            }
            return BCValue.MakeNoSuchProperty(this, name);
        }

        public void InitializeForRun()
        {
        }

        public void RunComplete()
        {
        }

        public void SetValue(string name, BCValue value)
        {
            // Do nothing; cannot set any value.
        }

        private async Task<RunResult.RunStatus> CommonDeviceCall(string serviceString, string characteristicString, BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            byte[] buffer = new byte[ArgList.Count]; 
            for (int i = 0; i < ArgList.Count; i++)
            {
                var b = (byte)(await ArgList[i].EvalAsync(context)).AsDouble;
                buffer[i] = b;
            }
            return await BluetoothDevice.DoWriteBytes(Device, serviceString, characteristicString, buffer, Retval);
        }


        public async Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            switch (name)
            {
                // Standard calls that are propertly supported by DEVICE.
                // Manufacturer is "supported", but the result is just "Manufacturer Name"
                // which isn't very interesting.
                case "GetName":
                    // The Get() functions are non-cached
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await BluetoothDevice.ReadString(Device, "1800", "2a00", BluetoothCacheMode.Uncached, Retval);


                // These are all of the DEVICE specific calls

                case "SetColor": // ffb0/ffb5 called R (RGBW 4 bytes)
                    if (!BCObjectUtilities.CheckArgs(4, 4, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(0, "red", 0, 255, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(1, "green", 0, 255, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(2, "blue", 0, 255, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(3, "white", 0, 255, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await CommonDeviceCall("ffb0", "ffb5", context, name, ArgList, Retval);


                case "ToString":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    Retval.AsString = $"Bluetooth specialization {PreferredName}";
                    return RunResult.RunStatus.OK;

                default:
                    await Task.Delay(0);
                    BCValue.MakeNoSuchMethod(Retval, this, name);
                    return RunResult.RunStatus.ErrorContinue;
            }
        }

        public void Dispose()
        {
        }
    }
}
