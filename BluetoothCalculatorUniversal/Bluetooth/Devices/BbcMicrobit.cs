using AdvancedCalculator.Bluetooth;
using BCBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace AdvancedCalculator.Bluetooth.Devices
{
    class BbcMicrobit : IObjectValue
    {
        BluetoothDevice Device = null;
        BluetoothLEDevice Ble = null;
        public BbcMicrobit(BluetoothDevice device, BluetoothLEDevice ble)
        {
            Device = device;
            Ble = ble;
        }
        private string AccelerometerService = "e95d0753-251d-470a-a062-fa1922dfa9a8";
        private string AccelerometerData = "e95dca4b-251d-470a-a062-fa1922dfa9a8";
        private string AccelerometerPeriod = "e95dfb24-251d-470a-a062-fa1922dfa9a8"; // in milliseconds


        private string ButtonService = "e95d9882-251d-470a-a062-fa1922dfa9a8";
        private string ButtonDataA = "e95dda90-251d-470a-a062-fa1922dfa9a8";
        private string ButtonDataB = "e95dda91-251d-470a-a062-fa1922dfa9a8";


        // IO Service
        // NOTE: not supported in this version.
        //private string IOService = "f000aa64-0451-4000-b000-000000000000";
        //private string IOData = "f000aa65-0451-4000-b000-000000000000";
        //private string IOConfig = "f000aa66-0451-4000-b000-000000000000";

        private string LedService = "e95dd91d-251d-470a-a062-fa1922dfa9a8";
        private string LedPattern = "e95d7b77-251d-470a-a062-fa1922dfa9a8";
        private string LedText = "e95d93ee-251d-470a-a062-fa1922dfa9a8";
        private string LedScrollTime = "e95d0d2d-251d-470a-a062-fa1922dfa9a8"; // in milliseconds


        private string MagnetometerService = "e95df2d8-251d-470a-a062-fa1922dfa9a8";
        private string MagnetometerData = "e95dfb11-251d-470a-a062-fa1922dfa9a8";
        private string MagnetometerBearing = "e95d9715-251d-470a-a062-fa1922dfa9a8";
        private string MagnetometerPeriod = "e95d386c-251d-470a-a062-fa1922dfa9a8"; // in milliseconds

        private string TemperatureService = "e95d6100-251d-470a-a062-fa1922dfa9a8";
        private string TemperatureData = "e95d9250-251d-470a-a062-fa1922dfa9a8";
        private string TemperaturePeriod = "e95d1b25-251d-470a-a062-fa1922dfa9a8"; // in milliseconds

        public string PreferredName { get { return "microbit"; } }

        public IList<string> GetNames() { return new List<string>() { "Methods" }; }

        public BCValue GetValue(string name)
        {
            switch (name)
            {
                case "Methods":
                    return new BCValue("GetName,AccelerometerSetup,ButtonSetup,CompassSetup,MagnetometerSetup,SetLed,TemperatureSetup,ToString,Write");
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

        // Converts a requested period (like, "500 milliseconds")
        // into a valid value. Will the largest value that's smaller than
        // the requested value.  Requests for less than 1 return 1.
        // Valid values are 1, 2, 5, 10, 20, 80, 160 and 640.
        // https://lancaster-university.github.io/microbit-docs/resources/bluetooth/bluetooth_profile.html
        int[] ValidPeriodValues = { 1, 2, 5, 10, 20, 80, 160, 640 };
        private int GetValidPeriod(double request)
        {
            int Retval = 1;
            foreach (var item in ValidPeriodValues)
            {
                if (item <= request && item > Retval) Retval = item;
            }
            return Retval;
        }

        private byte[] AsBytes(int value)
        {
            byte[] Retval = { (byte)(value % 256), (byte)(value / 256) };
            return Retval;
        }

        private async Task<RunResult.RunStatus> CommonDeviceCall(string serviceString, string characteristicString, BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            byte[] buffer = new byte[ArgList.Count];
            for (int i = 0; i < ArgList.Count; i++)
            {
                var b = (byte)(await ArgList[i].EvalAsync(context)).AsDouble;
                buffer[i] = b;
            }
            return await BluetoothDevice.DoWriteBytes(Ble, serviceString, characteristicString, buffer, Retval);
        }

        enum AddOrRemove {  Add, Remove };
        private async Task<RunResult.RunStatus> SetupCallbacks(AddOrRemove addRemove, 
            string serviceString, string characteristicString, 
            Windows.Foundation.TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> callback,
            string functionName, BCRunContext context,
            string name, BCValue Retval)
        {
            Guid serviceGuid = Guid.Parse(BluetoothDevice.StringToGuid(serviceString));
            var service = Ble.GetGattService(serviceGuid);

            Guid characteristicGuid = Guid.Parse(BluetoothDevice.StringToGuid(characteristicString));
            var characteristics = service.GetCharacteristics(characteristicGuid);


            foreach (var characteristic in characteristics)
            {
                if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
                {
                    GattCommunicationStatus btstatus = GattCommunicationStatus.Success;
                    switch (addRemove)
                    {
                        case AddOrRemove.Add:
                            btstatus = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                            characteristic.ValueChanged += callback; //NOTE: really?  Doesn't the Device.AddCallback do this for us?  See sensortag for more
                            Device.AddCallback(serviceString, characteristicString, context, callback, functionName, Retval);
                            break;
                        case AddOrRemove.Remove:
                            btstatus = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
                            characteristic.ValueChanged -= callback; //NOTE: really?  doesn't the Device.RemoveCallback do this for us?
                            Device.RemoveCallback(serviceString, characteristicString, functionName);
                            break;
                    }
                    if (btstatus == GattCommunicationStatus.Success)
                    {
                        Retval.AsString = "OK";
                        return RunResult.RunStatus.OK;
                    }
                    else
                    {
                        Retval.AsString = $"Error: Unable to {name}";
                        return RunResult.RunStatus.ErrorStop;
                    }
                }
            }

            Retval.AsString = $"Error: Unable to {name}";
            return RunResult.RunStatus.ErrorStop;
        }

        private void Acc_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            if (Device.AllGattCallbackData.ContainsKey(sender))
            {
                var callbackData = Device.AllGattCallbackData[sender];
                BCRunContext context = callbackData.Context;


                if (args.CharacteristicValue.Length == 6)
                {
                    var dr = DataReader.FromBuffer(args.CharacteristicValue);
                    dr.ByteOrder = ByteOrder.LittleEndian;
                    var ax = dr.ReadInt16();
                    var ay = dr.ReadInt16();
                    var az = dr.ReadInt16();

                    // Convert the raw data into processed doubles


                    // Units are G force
                    double AccX = (double)ax / 1000.0;
                    double AccY = (double)ay / 1000.0;
                    double AccZ = (double)az / 1000.0;

                    var list = callbackData.FunctionNameList.ToList();
                    foreach (var functionName in list)
                    {
                        var arglist = new List<IExpression>() { new ObjectConstant(this),
                            new NumericConstant(AccX), new NumericConstant(AccY), new NumericConstant(AccZ),
                        };
                        context.ProgramRunContext.AddCallback(context, functionName, arglist);
                        //await CalculatorConnectionControl.BasicFrameworkElement.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                        //    async () => { await context.Functions[functionName].EvalAsync(context, arglist); });
                    }
                }
                // Else there's a big problem
            }
        }


        // buttons are a little different than the other characteristics.
        // There are two buttons with different characteristics but I'm having
        // both of their value changes come here and there is a single callback.
        // That callback will include the press value for both buttons
        int ButtonAValue = 0;
        int ButtonBValue = 0;
        private void Button_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            if (Device.AllGattCallbackData.ContainsKey(sender))
            {
                var callbackData = Device.AllGattCallbackData[sender];
                BCRunContext context = callbackData.Context;


                if (args.CharacteristicValue.Length == 1)
                {
                    var dr = DataReader.FromBuffer(args.CharacteristicValue);
                    dr.ByteOrder = ByteOrder.LittleEndian;
                    var press = dr.ReadByte();

                    if (sender.Uuid == Guid.Parse(ButtonDataA)) ButtonAValue = press;
                    else ButtonBValue = press;

                    var list = callbackData.FunctionNameList.ToList();
                    foreach (var functionName in list)
                    {
                        var arglist = new List<IExpression>() { new ObjectConstant(this),
                            new NumericConstant(ButtonAValue), new NumericConstant(ButtonBValue)
                        };
                        context.ProgramRunContext.AddCallback(context, functionName, arglist);
                        //await CalculatorConnectionControl.BasicFrameworkElement.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                        //    async () => { await context.Functions[functionName].EvalAsync(context, arglist); });
                    }
                }
                // Else there's a big problem
            }
        }

        private void Compass_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            if (Device.AllGattCallbackData.ContainsKey(sender))
            {
                var callbackData = Device.AllGattCallbackData[sender];
                BCRunContext context = callbackData.Context;


                if (args.CharacteristicValue.Length == 2)
                {
                    var dr = DataReader.FromBuffer(args.CharacteristicValue);
                    dr.ByteOrder = ByteOrder.LittleEndian;
                    var bearing = dr.ReadInt16();

                    // Convert the raw data into processed doubles
                    var list = callbackData.FunctionNameList.ToList();
                    foreach (var functionName in list)
                    {
                        var arglist = new List<IExpression>() { new ObjectConstant(this),
                            new NumericConstant(bearing),
                        };
                        context.ProgramRunContext.AddCallback(context, functionName, arglist);
                        //await CalculatorConnectionControl.BasicFrameworkElement.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                        //    async () => { await context.Functions[functionName].EvalAsync(context, arglist); });
                    }
                }
                // Else there's a big problem
            }
        }



        private void Mag_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            if (Device.AllGattCallbackData.ContainsKey(sender))
            {
                var callbackData = Device.AllGattCallbackData[sender];
                BCRunContext context = callbackData.Context;


                if (args.CharacteristicValue.Length == 6)
                {
                    var dr = DataReader.FromBuffer(args.CharacteristicValue);
                    dr.ByteOrder = ByteOrder.LittleEndian;
                    var mx = dr.ReadInt16();
                    var my = dr.ReadInt16();
                    var mz = dr.ReadInt16();

                    // Convert the raw data into processed doubles


                    // Units are G force
                    double MagX = (double)mx;
                    double MagY = (double)my;
                    double MagZ = (double)mz;

                    var list = callbackData.FunctionNameList.ToList();
                    foreach (var functionName in list)
                    {
                        var arglist = new List<IExpression>() { new ObjectConstant(this),
                            new NumericConstant(MagX), new NumericConstant(MagY), new NumericConstant(MagZ),
                        };
                        context.ProgramRunContext.AddCallback(context, functionName, arglist);
                        //await CalculatorConnectionControl.BasicFrameworkElement.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                        //    async () => { await context.Functions[functionName].EvalAsync(context, arglist); });
                    }
                }
                // Else there's a big problem
            }
        }

        private void Temperature_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            if (Device.AllGattCallbackData.ContainsKey(sender))
            {
                var callbackData = Device.AllGattCallbackData[sender];
                BCRunContext context = callbackData.Context;


                if (args.CharacteristicValue.Length == 1)
                {
                    var dr = DataReader.FromBuffer(args.CharacteristicValue);
                    dr.ByteOrder = ByteOrder.LittleEndian;
                    var t = (sbyte)(dr.ReadByte());


                    var list = callbackData.FunctionNameList.ToList();
                    foreach (var functionName in list)
                    {
                        var arglist = new List<IExpression>() { new ObjectConstant(this),
                            new NumericConstant(t),
                        };
                        context.ProgramRunContext.AddCallback(context, functionName, arglist);
                        //await CalculatorConnectionControl.BasicFrameworkElement.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                        //    async () => { await context.Functions[functionName].EvalAsync(context, arglist); });
                    }
                }
                // Else there's a big problem
            }
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
                    return await BluetoothDevice.ReadString(Ble, "1800", "2a00", BluetoothCacheMode.Uncached, Retval);

                case "Close":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    Dispose();
                    return RunResult.RunStatus.OK;

                // These are all of the DEVICE specific calls
                case "AccelerometerSetup":
                    {
                        // name of function to call on change
                        if (!BCObjectUtilities.CheckArgs(3, 3, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        if (!await BCObjectUtilities.CheckArgValue(0, "onoff", 0, 1, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var onoff = (await ArgList[0].EvalAsync(context)).AsDouble;
                        var period = (await ArgList[1].EvalAsync(context)).AsDouble;
                        var periodBytes = AsBytes(GetValidPeriod(period));
                        string functionName = (await ArgList[2].EvalAsync(context)).AsString;

                        var addremove = onoff == 1.0 ? AddOrRemove.Add : AddOrRemove.Remove;
                        RunResult.RunStatus status = RunResult.RunStatus.OK;
                        if (onoff == 1.0) status = await BluetoothDevice.DoWriteBytes(Ble, AccelerometerService, AccelerometerPeriod, periodBytes, Retval);
                        status = await SetupCallbacks(addremove, AccelerometerService, AccelerometerData, Acc_ValueChanged, functionName, context, name, Retval);
                        return status;
                    }

                case "ButtonSetup":
                    {
                        // name of function to call on change
                        if (!BCObjectUtilities.CheckArgs(2, 2, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        if (!await BCObjectUtilities.CheckArgValue(0, "onoff", 0, 1, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var onoff = (await ArgList[0].EvalAsync(context)).AsDouble;
                        string functionName = (await ArgList[1].EvalAsync(context)).AsString;
                        var addremove = onoff == 1.0 ? AddOrRemove.Add : AddOrRemove.Remove;
                        var status = await SetupCallbacks(addremove, ButtonService, ButtonDataA, Button_ValueChanged, functionName, context, name, Retval);
                        status = await SetupCallbacks(addremove, ButtonService, ButtonDataB, Button_ValueChanged, functionName, context, name, Retval);
                        return status;
                    }

                case "CompassSetup":
                    {
                        // name of function to call on change
                        if (!BCObjectUtilities.CheckArgs(3, 3, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        if (!await BCObjectUtilities.CheckArgValue(0, "onoff", 0, 1, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var onoff = (await ArgList[0].EvalAsync(context)).AsDouble;
                        var period = (await ArgList[1].EvalAsync(context)).AsDouble;
                        var periodBytes = AsBytes(GetValidPeriod(period));
                        string functionName = (await ArgList[2].EvalAsync(context)).AsString;
                        var addremove = onoff == 1.0 ? AddOrRemove.Add : AddOrRemove.Remove;
                        RunResult.RunStatus status = RunResult.RunStatus.OK;
                        if (onoff == 1.0) status = await BluetoothDevice.DoWriteBytes(Ble, MagnetometerService, MagnetometerPeriod, periodBytes, Retval);
                        status = await SetupCallbacks(addremove, MagnetometerService, MagnetometerBearing, Compass_ValueChanged, functionName, context, name, Retval);
                        return status;
                    }


                case "MagnetometerSetup":
                    {
                        // name of function to call on change
                        if (!BCObjectUtilities.CheckArgs(3, 3, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        if (!await BCObjectUtilities.CheckArgValue(0, "onoff", 0, 1, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var onoff = (await ArgList[0].EvalAsync(context)).AsDouble;
                        var period = (await ArgList[1].EvalAsync(context)).AsDouble;
                        var periodBytes = AsBytes(GetValidPeriod(period));
                        string functionName = (await ArgList[2].EvalAsync(context)).AsString;
                        var addremove = onoff == 1.0 ? AddOrRemove.Add : AddOrRemove.Remove;
                        RunResult.RunStatus status = RunResult.RunStatus.OK;
                        if (onoff == 1.0) status = await BluetoothDevice.DoWriteBytes(Ble, MagnetometerService, MagnetometerPeriod, periodBytes, Retval);
                        status = await SetupCallbacks(addremove, MagnetometerService, MagnetometerData, Mag_ValueChanged, functionName, context, name, Retval);
                        return status;
                    }

                case "SetLed":
                    {
                        if (!BCObjectUtilities.CheckArgs(5, 5, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        if (!await BCObjectUtilities.CheckArgValue(0, "r1", 0, 31, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        if (!await BCObjectUtilities.CheckArgValue(1, "r2", 0, 31, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        if (!await BCObjectUtilities.CheckArgValue(2, "r3", 0, 31, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        if (!await BCObjectUtilities.CheckArgValue(3, "r4", 0, 31, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        if (!await BCObjectUtilities.CheckArgValue(4, "r5", 0, 31, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;

                        var r1 = (byte)(await ArgList[0].EvalAsync(context)).AsDouble;
                        var r2 = (byte)(await ArgList[1].EvalAsync(context)).AsDouble;
                        var r3 = (byte)(await ArgList[2].EvalAsync(context)).AsDouble;
                        var r4 = (byte)(await ArgList[3].EvalAsync(context)).AsDouble;
                        var r5 = (byte)(await ArgList[4].EvalAsync(context)).AsDouble;

                        byte[] bytes = { r1, r2, r3, r4, r5 };

                        var status = await BluetoothDevice.DoWriteBytes(Ble, LedService, LedPattern, bytes, Retval);
                        return status;
                    }

                case "TemperatureSetup":
                    {
                        // name of function to call on change
                        if (!BCObjectUtilities.CheckArgs(3, 3, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        if (!await BCObjectUtilities.CheckArgValue(0, "onoff", 0, 1, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var onoff = (await ArgList[0].EvalAsync(context)).AsDouble;
                        var period = (await ArgList[1].EvalAsync(context)).AsDouble;
                        var periodBytes = AsBytes(GetValidPeriod(period));
                        string functionName = (await ArgList[2].EvalAsync(context)).AsString;
                        var addremove = onoff == 1.0 ? AddOrRemove.Add : AddOrRemove.Remove;
                        RunResult.RunStatus status = RunResult.RunStatus.OK;
                        if (onoff == 1.0) status = await BluetoothDevice.DoWriteBytes(Ble, TemperatureService, TemperaturePeriod, periodBytes, Retval);
                        status = await SetupCallbacks(addremove, TemperatureService, TemperatureData, Temperature_ValueChanged, functionName, context, name, Retval);
                        return status;
                    }

                case "Write":
                    {
                        if (!BCObjectUtilities.CheckArgs(2, 2, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var text = (await ArgList[0].EvalAsync(context)).AsString;
                        var delay = (await ArgList[1].EvalAsync(context)).AsDouble;
                        byte[] scrollTime = { (byte)(delay % 256), (byte)(delay / 256) };

                        UTF8Encoding utf8Encoding = new UTF8Encoding();
                        var status = await BluetoothDevice.DoWriteBytes(Ble, LedService, LedScrollTime, scrollTime, Retval);
                        status = await BluetoothDevice.DoWriteBytes(Ble, LedService, LedText, utf8Encoding.GetBytes(text), Retval);

                        return status;
                    }



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
