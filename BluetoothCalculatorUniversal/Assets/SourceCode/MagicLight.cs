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
    class MagicLight : IObjectValue
    {
        // MagicLight and FluxLight each send data to service ffe5 characteristic ffe9.
        // The bytes sent are a sort of CRC-checked string as is communicating over a lossy channel.
        const string ServiceString = "ffe5";
        const string CharacteristicString = "ffe9";
        
        BluetoothLEDevice Device = null;
        public MagicLight(BluetoothLEDevice device)
        {
            Device = device;
        }

        public string PreferredName { get { return "MagicLight"; } }

        public IList<string> GetNames() { return new List<string>() { "Methods" }; }

        public BCValue GetValue(string name)
        {
            switch (name)
            {
                case "Methods":
                    return new BCValue("GetName,SetColor,SetOff,SetOn,ToString");
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

        private async Task<RunResult.RunStatus> SetRGB(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            // From original app: var buffer = (new byte[] { 86, data.R, data.G, data.B, 0, 256 - 16, 256 - 86 }).AsBuffer(); //86==0x56
            byte r = (byte)(await ArgList[0].EvalAsync(context)).AsDouble;
            byte g = (byte)(await ArgList[1].EvalAsync(context)).AsDouble;
            byte b = (byte)(await ArgList[2].EvalAsync(context)).AsDouble;
            byte[] buffer = new byte[7] { 86, r, g, b, 0, 256 - 16, 256 - 86 };
            return await BluetoothDevice.DoWriteBytes(Device, ServiceString, CharacteristicString, buffer, Retval);
        }

        private async Task<RunResult.RunStatus> SetOnOff(bool on, BCValue Retval)
        {
            // From original app: var buffer = (new byte[] { 256 - 52, (data.On ? (byte)35 : (byte)36), 51 }).AsBuffer(); //35=0x23 36=0x24

            byte[] buffer = new byte[3] { 256 - 52, (on ? (byte)35 : (byte)36), 51 };
            return await BluetoothDevice.DoWriteBytes(Device, ServiceString, CharacteristicString, buffer, Retval);
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

                case "SetColor": // MagicLight / Flux is very weird.  Service ffe5 characteristic ffe9
                    if (!BCObjectUtilities.CheckArgs(3, 3, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(0, "red", 0, 255, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(1, "green", 0, 255, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(2, "blue", 0, 255, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await SetRGB(context, name, ArgList, Retval);

                case "SetOff": // MagicLight / Flux is very weird.  Service ffe5 characteristic ffe9
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await SetOnOff(false, Retval); // 0==off

                case "SetOn": // MagicLight / Flux is very weird.  Service ffe5 characteristic ffe9
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await SetOnOff(true, Retval); // 1==on


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
