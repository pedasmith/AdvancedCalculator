using AdvancedCalculator.Bluetooth;
using BCBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace AdvancedCalculator.Bluetooth.Devices
{
    /// <summary>
    /// The Skoobot is a tiny (1 inch cube) mini-robot from 
    /// </summary>
    class Skoobot : IObjectValue
    {
        BluetoothDevice Device = null;
        BluetoothLEDevice Ble = null;
        Bluetooth Bluetooth = null;
        public Skoobot(Bluetooth bluetooth, BluetoothDevice device, BluetoothLEDevice ble)
        {
            Bluetooth = bluetooth;
            Device = device;
            Ble = ble;
        }
        private string RobotService = "00001523-1212-efde-1523-785feabcd123";
        private string CommandCharacteristic = "00001525-1212-efde-1523-785feabcd123";
        private string Byte1Characteristic = "00001524-1212-efde-1523-785feabcd123";
        private string Byte2Characteristic = "00001526-1212-efde-1523-785feabcd123";
        //private string Byte4Characteristic = "00001528-1212-efde-1523-785feabcd123";
        //private string Byte20Characteristic = "00001527-1212-efde-1523-785feabcd123";

        public string PreferredName { get { return "Skoobot"; } }

        public IList<string> GetNames() { return new List<string>() { "Methods" }; }

        public BCValue GetValue(string name)
        {
            switch (name)
            {
                case "Methods":
                    return new BCValue("Left,Left30,Right,Right30,Forward,Backward,Stop,StopTurning,MotorsSleep,PlayBuzzer,RoverMode,FotovoreMode,RoverModeRev,GetAmbient,GetDistance,GetName,ToString");
            }
            return BCValue.MakeNoSuchProperty(this, name);
        }

        public void InitializeForRun()
        {
        }

        public void RunComplete()
        {
            if (DistanceCts != null) DistanceCts.Cancel();
            if (LightCts != null) LightCts.Cancel();
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
            return await BluetoothDevice.DoWriteBytes(Ble, serviceString, characteristicString, buffer, Retval);
        }

        Dictionary<string, byte> CommandToByte = new Dictionary<string, byte>()
        {
            { "Right30", 0x08 },
            { "Left30", 0x09 },
            { "Right", 0x10 },
            { "Left", 0x11 },
            { "Forward", 0x12 },
            { "Backward", 0x13 },
            { "Stop", 0x14 },
            { "StopTurning", 0x15 },
            { "MotorsSleep", 0x16 },
            { "PlayBuzzer", 0x17 },
            { "RequestAmbient", 0x21 }, // triggers the 1-byte callback; 0x21=33
            { "RequestDistance", 0x22 }, // triggers the 2-byte callback; 0x22=34
            { "RoverMode", 0x40 },
            { "FotovoreMode", 0x41 },
            { "RoverModeRev", 0x42 },
        };

        enum AddOrRemove { Add, Remove };
        private async Task<RunResult.RunStatus> SetupCallbacks(AddOrRemove addRemove,
            string serviceString, string characteristicString,
            Windows.Foundation.TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> callback,
            string functionName, BCRunContext context,
            string name, BCValue Retval)
        {
            Bluetooth.AddDeviceToComplete(this); // The ValueChanged must be undone at the end.

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

        private void Distance_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            if (Device.AllGattCallbackData.ContainsKey(sender))
            {
                var callbackData = Device.AllGattCallbackData[sender];
                BCRunContext context = callbackData.Context;

                if (args.CharacteristicValue.Length == 1) // 1 bytes for distance
                {
                    var dr = DataReader.FromBuffer(args.CharacteristicValue);
                    dr.ByteOrder = ByteOrder.LittleEndian;
                    var rawValue = dr.ReadByte(); // C# byts are unsigned
                    // From android code: double inches = val / 20.0;
                    double cm = (((double)rawValue) / 20.0) * 2.54;

                    var list = callbackData.FunctionNameList.ToList();
                    foreach (var functionName in list)
                    {
                        var arglist = new List<IExpression>() { new ObjectConstant(this),
                            new NumericConstant(cm),
                        };
                        context.ProgramRunContext.AddCallback(context, functionName, arglist);
                    }
                }
                // Else there's a big problem
            }
        }

        private void Light_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            if (Device.AllGattCallbackData.ContainsKey(sender))
            {
                var callbackData = Device.AllGattCallbackData[sender];
                BCRunContext context = callbackData.Context;


                if (args.CharacteristicValue.Length == 2) // 2 bytes for light
                {
                    var dr = DataReader.FromBuffer(args.CharacteristicValue);
                    dr.ByteOrder = ByteOrder.BigEndian;
                    var rawValue = dr.ReadUInt16();
                    // The value I get is already in Lux.
                    var Lux = rawValue;



                    var list = callbackData.FunctionNameList.ToList();
                    foreach (var functionName in list)
                    {
                        var arglist = new List<IExpression>() { new ObjectConstant(this),
                            new NumericConstant(Lux),
                        };
                        context.ProgramRunContext.AddCallback(context, functionName, arglist);
                    }
                }
                // Else there's a big problem
            }
        }

        Task DistanceTask = null;
        CancellationTokenSource DistanceCts = null;
        Task LightTask = null;
        CancellationTokenSource LightCts = null;

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

                case "Setup":
                    {
                        if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var status = await SetupCallbacks(AddOrRemove.Add,
                            RobotService, Byte1Characteristic, Distance_ValueChanged, "OnDistance", context, name, Retval);
                        status = await SetupCallbacks(AddOrRemove.Add,
                            RobotService, Byte2Characteristic, Light_ValueChanged, "OnLight", context, name, Retval);

                        return RunResult.RunStatus.OK;
                    }

                case "SetupDistance":
                    {
                        if (DistanceCts != null)
                        {
                            DistanceCts.Cancel();
                            while (DistanceTask != null && DistanceTask.Status == TaskStatus.Running)
                            {
                                await Task.Delay(100);
                            }
                        }

                        if (!BCObjectUtilities.CheckArgs(2, 2, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        if (!await BCObjectUtilities.CheckArgValue(1, "milliseconds", 10, 10000, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        string functionName = (await ArgList[0].EvalAsync(context)).AsString;
                        var period = (int)Math.Ceiling((await ArgList[1].EvalAsync(context)).AsDouble);

                        var status = await SetupCallbacks(AddOrRemove.Add,
                            RobotService, Byte1Characteristic, Distance_ValueChanged, functionName, context, name, Retval);
                        DistanceCts = new CancellationTokenSource();
                        var ct = DistanceCts.Token;

                        DistanceTask = new Task(async () =>
                        {
                            // This will trigger the bluetooth device to get a distance vale
                            // which will cause the distance characteristic notification to trigger.
                            var commandName = "RequestDistance";

                            while (!ct.IsCancellationRequested)
                            {
                                byte[] bytes = new byte[1];
                                bytes[0] = CommandToByte[commandName];
                                var wbstatus = await BluetoothDevice.DoWriteBytes(Ble, RobotService, CommandCharacteristic, bytes, Retval);
                                await TaskTryDelay(period, ct);
                            }
                        });
                        DistanceTask.Start();
                        return RunResult.RunStatus.OK;
                    }


                case "SetupLight":
                    {
                        if (LightCts != null)
                        {
                            LightCts.Cancel();
                            while (LightTask != null && LightTask.Status == TaskStatus.Running)
                            {
                                await Task.Delay(100);
                            }
                        }

                        if (!BCObjectUtilities.CheckArgs(2, 2, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        if (!await BCObjectUtilities.CheckArgValue(1, "onoff", 10, 10000, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        string functionName = (await ArgList[0].EvalAsync(context)).AsString;
                        var period = (int)Math.Ceiling ((await ArgList[1].EvalAsync(context)).AsDouble);

                        var status = await SetupCallbacks(AddOrRemove.Add,
                            RobotService, Byte2Characteristic, Light_ValueChanged, functionName, context, name, Retval);
                        LightCts = new CancellationTokenSource();
                        var ct = LightCts.Token;

                        LightTask = new Task(async () => 
                        {
                            // This will trigger the bluetooth device to get a light vale
                            // which will cause the light characteristic notification to trigger.
                            var commandName = "RequestAmbient";

                            while (!ct.IsCancellationRequested)
                            {
                                byte[] bytes = new byte[1];
                                bytes[0] = CommandToByte[commandName];
                                var wbstatus = await BluetoothDevice.DoWriteBytes(Ble, RobotService, CommandCharacteristic, bytes, Retval);
                                await TaskTryDelay(period, ct);
                            }
                        });
                        LightTask.Start();
                        return RunResult.RunStatus.OK;
                    }


                default:
                    // Do the 
                    if (CommandToByte.ContainsKey (name))
                    {
                        if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;

                        byte[] bytes = new byte[1];
                        bytes[0] = CommandToByte[name];
                        var status = await BluetoothDevice.DoWriteBytes(Ble, RobotService, CommandCharacteristic, bytes, Retval);
                        return status;
                    }
                    else
                    {
                        await Task.Delay(0);
                        BCValue.MakeNoSuchMethod(Retval, this, name);
                        return RunResult.RunStatus.ErrorContinue;
                    }

                case "ToString":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    Retval.AsString = $"Bluetooth specialization {PreferredName}";
                    return RunResult.RunStatus.OK;

            }
        }

        private async Task TaskTryDelay(int ms, CancellationToken ct)
        {
            try
            {
                await Task.Delay(ms, ct);
            }
            catch (Exception)
            {
                ;
            }
        }

        public void Dispose()
        {
        }
    }
}
