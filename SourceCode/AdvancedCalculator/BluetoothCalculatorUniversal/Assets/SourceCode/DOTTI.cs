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
    class DOTTI: IObjectValue
    {
        BluetoothLEDevice Device = null;
        public DOTTI(BluetoothLEDevice device)
        {
            Device = device;
        }

        public string PreferredName { get { return "DOTTI"; } }

        public IList<string> GetNames() { return new List<string>() { "Methods" }; }

        public BCValue GetValue(string name)
        {
            switch (name)
            {
                case "Methods":
                    return new BCValue("ChangeMode,GetName,GetPower,LoadScreenFromMemory,SaveScreenToMemory,SetAnimationSpeed,SetColumn,SetName,SetNameArbitrary,SetPanel,SetPixel,SetRow,SyncTime,ToString");
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

        private async Task<RunResult.RunStatus> CommonDottiCall(byte cmd1, byte cmd2, BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            var serviceString = "fff0";
            var characteristicString = "fff3";
            byte[] buffer = new byte[ArgList.Count + 2]; // { 0x07, 0x03, col, r, g, b };
            buffer[0] = cmd1;
            buffer[1] = cmd2;
            for (int i=0; i<ArgList.Count; i++)
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
            if (name == "SetName" && nameString != "Dotti" && !nameString.EndsWith("-Dotti"))
            {
                name += "-Dotti";
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

        /* The documentation for the Dotti device gives a complex mechanism
         * to make a hex version of the name string.  The actual device simply
         * takes in a string and a length. This method is left in the code as a
         * reminder of the incorrect documentation.
         */
        public async Task<RunResult.RunStatus> SetNameAsDocumented(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            var serviceString = "fff0";
            var characteristicString = "fff5"; // This is the only documented fff5 command
            var nameString = (string)(await ArgList[0].EvalAsync(context)).AsString;
            var nameHexLen = nameString.Length * 2 + 2;
            byte[] buffer = new byte[nameHexLen]; //  { (byte)nameHexLen, 0x02, };
            buffer[0] = (byte)nameHexLen;
            buffer[1] = 0x02;
            for (int i = 0; i < nameString.Length; i++)
            {
                buffer[i * 2 + 2] = (byte)(((nameString[i] >> 4) & 0x0f) + '0');
                buffer[i * 2 + 3] = (byte)(((nameString[i] >> 0) & 0x0f) + '0');
            }

            return await BluetoothDevice.DoWriteBytes(Device, serviceString, characteristicString, buffer, Retval);
        }

        public async Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            switch (name)
            {
                // Standard calls that are propertly supported by DOTTI.
                // Manufacturer is "supported", but the result is just "Manufacturer Name"
                // which isn't very interesting.
                case "GetName":
                    // The Get() functions are non-cached
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await BluetoothDevice.ReadString(Device, "1800", "2a00", BluetoothCacheMode.Uncached, Retval);

                case "GetPower":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await BluetoothDevice.ReadByte(Device, "180f", "2a19", BluetoothCacheMode.Uncached, Retval);

                // These are all of the DOTTI specific calls
                case "ChangeMode":
                    if (!BCObjectUtilities.CheckArgs(1, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(0, "mode", 0, 5, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await CommonDottiCall(0x04, 0x05, context, name, ArgList, Retval);
                    //return await ChangeMode(context, name, ArgList, Retval);

                case "LoadScreenFromMemory":
                    if (!BCObjectUtilities.CheckArgs(2, 2, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await CommonDottiCall(0x06, 0x08, context, name, ArgList, Retval);
                    //return await LoadScreenFromMemory(context, name, ArgList, Retval);

                case "SaveScreenToMemory":
                    if (!BCObjectUtilities.CheckArgs(2, 2, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await CommonDottiCall(0x06, 0x07, context, name, ArgList, Retval);
                    //return await SaveScreenToMemory(context, name, ArgList, Retval);

                case "SetAnimationSpeed":
                    if (!BCObjectUtilities.CheckArgs(1, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(0, "speed", 1, 6, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await CommonDottiCall(0x04, 0x15, context, name, ArgList, Retval);
                    //return await SetAnimationSpeed(context, name, ArgList, Retval);

                case "SetColumn":
                    if (!BCObjectUtilities.CheckArgs(4, 4, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(0, "column", 1, 8, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await CommonDottiCall(0x07, 0x03, context, name, ArgList, Retval);
                //return await SetColumn(context, name, ArgList, Retval);

                case "SetName":
                case "SetNameArbitrary":
                    if (!BCObjectUtilities.CheckArgs(1, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await SetName(context, name, ArgList, Retval);

                case "SetPanel":
                    if (!BCObjectUtilities.CheckArgs(3, 3, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await CommonDottiCall(0x06, 0x01, context, name, ArgList, Retval);
                    //return await SetPanel(context, name, ArgList, Retval);

                case "SetPixel":
                    if (!BCObjectUtilities.CheckArgs(5, 5, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(0, "row", 1, 8, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(1, "column", 1, 8, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    {
                        // Update the arglist.  The x,y value needs to be converted to pixel 1..64
                        var x = (await ArgList[0].EvalAsync(context)).AsDouble;
                        var y = (await ArgList[1].EvalAsync(context)).AsDouble;
                        var r = (await ArgList[2].EvalAsync(context)).AsDouble;
                        var g = (await ArgList[3].EvalAsync(context)).AsDouble;
                        var b = (await ArgList[4].EvalAsync(context)).AsDouble;
                        var pixel = ((x - 1) + (y - 1)*8) + 1;
                        var newlist = new List<IExpression>() {new NumericConstant (pixel),new NumericConstant(r), new NumericConstant(g), new NumericConstant(b)};
                        return await CommonDottiCall(0x07, 0x02, context, name, newlist, Retval);
                    }
                //return await SetPixel(context, name, ArgList, Retval);

                case "SetRow":
                    if (!BCObjectUtilities.CheckArgs(4, 4, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(0, "row", 1, 8, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await CommonDottiCall(0x07, 0x04, context, name, ArgList, Retval);
                    //return await SetRow(context, name, ArgList, Retval);

                case "SyncTime":
                    if (!BCObjectUtilities.CheckArgs(3, 3, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(0, "hour", 0, 24, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(1, "minute", 0, 60, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(2, "second", 0, 60, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await CommonDottiCall(0x06, 0x09, context, name, ArgList, Retval);
                    //return await SetTime(context, name, ArgList, Retval);

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
