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
    class NOTTI : IObjectValue
    {
        BluetoothLEDevice Device = null;
        public NOTTI(BluetoothLEDevice device)
        {
            Device = device;
        }

        public string PreferredName { get { return "NOTTI"; } }

        public IList<string> GetNames() { return new List<string>() { "Methods" }; }

        public BCValue GetValue(string name)
        {
            switch (name)
            {
                case "Methods":
                    return new BCValue("AlarmSetting,ChangeMode,GetName,GetPower,SetAlarmTime,SetColor,SetColorCustom,SetName,SetNameArbitrary,SyncTime,ToString");
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

        private async Task<RunResult.RunStatus> CommonNottiCall(byte cmd1, byte cmd2, BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            var serviceString = "fff0";
            var characteristicString = "fff3";
            byte[] buffer = new byte[ArgList.Count + 2]; // { 0x07, 0x03, col, r, g, b };
            buffer[0] = cmd1;
            buffer[1] = cmd2;
            for (int i = 0; i < ArgList.Count; i++)
            {
                var b = (byte)(await ArgList[i].EvalAsync(context)).AsDouble;
                buffer[i + 2] = b;
            }
            return await BluetoothDevice.DoWriteBytes(Device, serviceString, characteristicString, buffer, Retval);
        }
        public async Task<RunResult.RunStatus> SetName(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            var serviceString = "fff0";
            var characteristicString = "fff5"; // This is the only documented fff5 command
            var nameString = (string)(await ArgList[0].EvalAsync(context)).AsString;
            if (name == "SetName" && nameString != "Notti" && !nameString.EndsWith("-Notti"))
            {
                name += "-Notti";
            }
            var nameHexLen = nameString.Length + 2;
            byte[] buffer = new byte[nameHexLen]; //  { (byte)nameHexLen, 0x02, };
            buffer[0] = (byte)nameHexLen;
            buffer[1] = 0x02;
            for (int i = 0; i < nameString.Length; i++)
            {
                buffer[i + 2] = (byte)nameString[i];
            }

            return await BluetoothDevice.DoWriteBytes(Device, serviceString, characteristicString, buffer, Retval);
        }


        public async Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            switch (name)
            {
                // Standard calls that are propertly supported by NOTTI.
                // Manufacturer is "supported", but the result is just "Manufacturer Name"
                // which isn't very interesting.
                case "GetName":
                    // The Get() functions are non-cached
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await BluetoothDevice.ReadString(Device, "1800", "2a00", BluetoothCacheMode.Uncached, Retval);

                case "GetPower":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await BluetoothDevice.ReadByte(Device, "180f", "2a19", BluetoothCacheMode.Uncached, Retval);

                // These are all of the NOTTI specific calls
                case "AlarmSetting":
                    // NOTTI specific; 
                    if (!BCObjectUtilities.CheckArgs(5, 5, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(0, "mode", 0, 2, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    // 1=R 2=G 3=B don't need to be checked
                    if (!await BCObjectUtilities.CheckArgValue(4, "waketime", 1, 10, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await CommonNottiCall(0x08, 0x15, context, name, ArgList, Retval);

                case "ChangeMode": // Same as DOTTI but the meanings are different
                    if (!BCObjectUtilities.CheckArgs(1, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(0, "mode", 0, 2, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await CommonNottiCall(0x04, 0x05, context, name, ArgList, Retval);

                case "SetAlarmTime":
                    // NOTTI specific; H M only (no seconds)
                    if (!BCObjectUtilities.CheckArgs(2, 2, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(0, "hour", 0, 24, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(1, "minute", 0, 60, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await CommonNottiCall(0x05, 0x03, context, name, ArgList, Retval);

                case "SetName": // Same as DOTTI but a different suffix (-Notti)
                case "SetNameArbitrary":
                    if (!BCObjectUtilities.CheckArgs(1, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await SetName(context, name, ArgList, Retval);

                case "SetColor": // Same as DOTTI
                    if (!BCObjectUtilities.CheckArgs(3, 3, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await CommonNottiCall(0x06, 0x01, context, name, ArgList, Retval);

                case "SetColorCustom": // NOTTI specific 6 bytes: RGB RGB
                    if (!BCObjectUtilities.CheckArgs(6, 6, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await CommonNottiCall(0x09, 0x08, context, name, ArgList, Retval);


                case "SyncTime":
                    // DOTTI is 06 09, but on NOTTI it's 06 02
                    if (!BCObjectUtilities.CheckArgs(3, 3, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(0, "hour", 0, 24, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(1, "minute", 0, 60, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(2, "second", 0, 60, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await CommonNottiCall(0x06, 0x02, context, name, ArgList, Retval);

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
