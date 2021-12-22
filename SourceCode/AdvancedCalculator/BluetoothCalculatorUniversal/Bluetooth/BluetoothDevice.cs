using AdvancedCalculator.BCBasic.RunTimeLibrary;
using BCBasic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;

namespace AdvancedCalculator.Bluetooth
{
    public interface IBluetoothDevice : IObjectValue
    {
        string UserPickedName { get; set; }
        DeviceInformation di { get; set; }
    }
#if WINDOWS8
    public class BluetoothLEDevice
    {
        public static string GetDeviceSelector() { return ""; }
    }
#endif

    public class BluetoothDevice : IBluetoothDevice, IObjectValue
    {
        public Bluetooth Bluetooth { get; internal set; }
        public DeviceInformation di { get; set; }  = null;
        public BluetoothLEDevice ble { get; internal set; } = null;
        public string UserPickedName
        {
            get
            {
                if (di == null) return PreferredName;
                var index = "NAME_" + di.Id;
                var roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
                if (roamingSettings.Values.ContainsKey(index))
                {
                    var name = roamingSettings.Values[index] as string;
                    if (name != null) return name;
                }
                return di.Name;
            }
            set
            {
                if (di == null) return;
                var index = "NAME_" + di.Id;
                var roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
                if (roamingSettings.Values.ContainsKey(index))
                {
                    roamingSettings.Values[index] = value;
                }
                else
                {
                    roamingSettings.Values.Add(index, value);
                }
            }
        }


        //                 var ble = await BluetoothLEDevice.FromIdAsync(Device.Id);

        public BluetoothDevice(Bluetooth bluetooth, DeviceInformation deviceInformation)
        {
            Bluetooth = bluetooth;
            di = deviceInformation;
        }

        public string PreferredName { get; } = "BluetoothDevice";

        public IList<string> GetNames() { return new List<string>() { "Name" }; }

        public BCValue GetValue(string name)
        {
            switch (name)
            {
                case "Name":
                    return new BCValue(di.Name);
                case "Properties":
                    // The properties item is a special list
                    var str = $"Name:{di.Name} ";
                    foreach (var property in di.Properties)
                    {
                        str += $"{property.Key}:{property.Value?.ToString()}; ";
                    }
                    return new BCValue(str);
                default:
                    var props = di.GetType().GetRuntimeProperties();
                    foreach (var property in props)
                    {
                        if (property.Name == name)
                        {
                            var obj = property.GetValue(di);
                            return new BCValue(obj.ToString());
                        }
                    }
                    foreach (var property in di.Properties)
                    {
                        if (property.Key.Replace('.', '_') == name)
                        {
                            return new BCValue(property.Value?.ToString());
                        }
                    }
                    if (ble != null)
                    {
                        var matchName = name;
                        if (matchName.StartsWith("BLE_")) matchName = matchName.Substring(4);
                        props = ble.GetType().GetRuntimeProperties();
                        foreach (var property in props)
                        {
                            if (property.Name == matchName)
                            {
                                var obj = property.GetValue(ble);
                                return new BCValue(obj.ToString());
                            }
                        }
                    }
                    break;
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


        //
        // This is where you add in support for different GATT devices
        //
        private async Task<RunResult.RunStatus> DoAs(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            if (!BCObjectUtilities.CheckArgs(1, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
            var deviceName = (await ArgList[0].EvalAsync(context)).AsString;
            if (ble == null)
            {
#if !WINDOWS8
                ble = await BluetoothLEDevice.FromIdAsync(di.Id);
#endif
            }
            if (ble == null)
            {
                Retval.SetError (2, $"{PreferredName}.{name}: unable to get the bluetooth device");
                return RunResult.RunStatus.ErrorStop;
            }

            switch (deviceName)
            {
#if !WINDOWS8
                case "beLight": Retval.AsObject = new Devices.beLight(ble); return RunResult.RunStatus.OK;
                case "DOTTI": Retval.AsObject = new Devices.DOTTI(ble); return RunResult.RunStatus.OK;
                case "Hexiwear": Retval.AsObject = new Devices.Hexiwear(ble); return RunResult.RunStatus.OK;
                case "MagicLight": Retval.AsObject = new Devices.MagicLight(ble); return RunResult.RunStatus.OK;
                case "MetaMotion": Retval.AsObject = new Devices.MetaMotion(Bluetooth, ble); return RunResult.RunStatus.OK;
                case "microbit": Retval.AsObject = new Devices.BbcMicrobit(this, ble); return RunResult.RunStatus.OK;
                case "NOTTI": Retval.AsObject = new Devices.NOTTI(ble); return RunResult.RunStatus.OK;
                case "Puck.js": Retval.AsObject = new Devices.PuckJs(this, ble); return RunResult.RunStatus.OK;
                case "SensorTag1350": Retval.AsObject = new Devices.SensorTag1350(this, ble); return RunResult.RunStatus.OK;
                case "SensorTag2541": Retval.AsObject = new Devices.SensorTag2541(this, ble); return RunResult.RunStatus.OK;
                case "SensorTag2650": Retval.AsObject = new Devices.SensorTag2650(this, ble); return RunResult.RunStatus.OK;
                case "Skoobot": Retval.AsObject = new Devices.Skoobot(Bluetooth, this, ble); return RunResult.RunStatus.OK;
#endif
            }
            Retval.SetError (1, $"Error: unknown device type {deviceName}");
            return RunResult.RunStatus.ErrorStop;
        }


        public static async Task<RunResult.RunStatus> DoReadBytes(BluetoothLEDevice Device, string serviceString, string characteristicString, BCValue Retval)
        {
#if !WINDOWS8
            Guid serviceGuid = Guid.Parse(BluetoothDevice.StringToGuid(serviceString));
            var service = Device.GetGattService(serviceGuid);

            Guid characteristicGuid = Guid.Parse(BluetoothDevice.StringToGuid(characteristicString));
            var characteristics = service.GetCharacteristics(characteristicGuid);

            foreach (var characteristic in characteristics)
            {
                if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Read))
                {
                    var status = await BluetoothDevice.DoReadBytes(characteristic, Retval);
                    return status;
                }
            }
#endif
            Retval.SetError (1, $"Error: unable to read characteristic {characteristicString}.");
            return RunResult.RunStatus.ErrorStop;
        }


        public static async Task<RunResult.RunStatus> DoReadBytes(GattCharacteristic characteristic, BCValue Retval)
        {
            try
            {
                var gattRead = await characteristic.ReadValueAsync(BluetoothCacheMode.Uncached);
                if (gattRead.Status == GattCommunicationStatus.Unreachable)
                {
                    Retval.SetError (1, $"ERROR: Unable to read value ");
                    return RunResult.RunStatus.ErrorStop;
                }
                else
                {
                    var str = "";
                    for (uint i = 0; i < gattRead.Value.Length; i++)
                    {
                        var b0 = gattRead.Value.GetByte(i);
                        if (i > 0) str += " ";
                        str += b0.ToString("X2");
                    }

                    Retval.AsString = str;
                }
            }
            catch (Exception ex)
            {
                Retval.SetError (1, $"ERROR: Exception when reading value ");
                RTLSystemX.AddError($"Exception reading BT value is {ex.Message}");
                return RunResult.RunStatus.ErrorStop;
            }
            return RunResult.RunStatus.OK;
        }



        private async Task<RunResult.RunStatus> ReadByte(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            // First param: e.g. 180f to read power data
            // Second param: e.g. 2a19 to get the power

            var serviceString = (await ArgList[0].EvalAsync(context)).AsString;
            var characteristicString = (await ArgList[1].EvalAsync(context)).AsString;

            BluetoothCacheMode readMode = name.Contains("Cached") ? BluetoothCacheMode.Cached : BluetoothCacheMode.Uncached;

            return await ReadByte(ble, serviceString, characteristicString, readMode, Retval);
        }

        public static async Task<RunResult.RunStatus> ReadByte(BluetoothLEDevice ble, string serviceString, string characteristicString, BluetoothCacheMode readMode, BCValue Retval)
        {
#if !WINDOWS8
            try
            {
                Guid serviceGuid = Guid.Parse(StringToGuid(serviceString));
                var service = ble.GetGattService(serviceGuid);

                Guid characteristicGuid = Guid.Parse(StringToGuid(characteristicString));
                var characteristics = service.GetCharacteristics(characteristicGuid);

                foreach (var characteristic in characteristics)
                {
                    if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Read))
                    {
                        var gattRead = await characteristic.ReadValueAsync(readMode);
                        if (gattRead.Status == GattCommunicationStatus.Success)
                        {
                            byte b = gattRead.Value.GetByte(0);
                            Retval.AsDouble = b;
                            return RunResult.RunStatus.OK;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RTLSystemX.AddError($"Exception while reading characteristic is {ex.Message}");
            }
#endif
            Retval.SetError (1, $"Error: unable to read characteristic {characteristicString}.");
            return RunResult.RunStatus.ErrorStop;
        }

        private async Task<RunResult.RunStatus> ReadBytes(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            // First param: e.g. 180f to read power data
            // Second param: e.g. 2a19 to get the power

            var serviceString = (await ArgList[0].EvalAsync(context)).AsString;
            var characteristicString = (await ArgList[1].EvalAsync(context)).AsString;

            BluetoothCacheMode readMode = name.Contains("Cached") ? BluetoothCacheMode.Cached : BluetoothCacheMode.Uncached;

            return await ReadBytesAsync(ble, serviceString, characteristicString, readMode, Retval);
        }

        public static async Task<RunResult.RunStatus> ReadBytesAsync(BluetoothLEDevice ble, string serviceString, string characteristicString, BluetoothCacheMode readMode, BCValue Retval)
        {
            Retval.SetNoValue();
            try
            {
                Guid serviceGuid = Guid.Parse(StringToGuid(serviceString));
                var service = ble.GetGattService(serviceGuid);

                Guid characteristicGuid = Guid.Parse(StringToGuid(characteristicString));
                var characteristics = service.GetCharacteristics(characteristicGuid);

                foreach (var characteristic in characteristics)
                {
                    if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Read))
                    {
                        var gattRead = await characteristic.ReadValueAsync(readMode);
                        if (gattRead.Status == GattCommunicationStatus.Success)
                        {
                            var list = new BCValueList();
                            Retval.AsObject = list;
                            for (uint i=0; i<gattRead.Value.Length; i++)
                            {
                                byte b = gattRead.Value.GetByte(i); // must be unsigned
                                list.data.Add(new BCValue(b));
                            }

                            return RunResult.RunStatus.OK;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RTLSystemX.AddError($"Exception while reading characteristic {characteristicString} is {ex.Message}");
            }

            Retval.SetError (1, $"Error: unable to read characteristic {characteristicString}.");
            return RunResult.RunStatus.ErrorStop;
        }

        public static async Task<RunResult.RunStatus> ReadString(BluetoothLEDevice ble, string serviceString, string characteristicString,  BluetoothCacheMode readMode, BCValue Retval)
        {
            Guid serviceGuid = Guid.Parse(StringToGuid(serviceString));
            var service = ble.GetGattService(serviceGuid);

            Guid characteristicGuid = Guid.Parse(StringToGuid(characteristicString));
            var characteristics = service.GetCharacteristics(characteristicGuid);

            foreach (var characteristic in characteristics)
            {
                if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Read))
                {
                    var gattRead = await characteristic.ReadValueAsync(readMode);
                    if (gattRead.Status == GattCommunicationStatus.Success)
                    {
                        Retval.AsString = Utilities.BluetoothUtilities.GattReadResultToUTF8String(gattRead);
                        return RunResult.RunStatus.OK; 
                    }
                }
            }

            Retval.SetError (1, $"Error: unable to read characteristic {characteristicString}.");
            return RunResult.RunStatus.ErrorStop;
        }

        // This is only called from BASIC by the end developers; it's not used internally.
        private async Task<RunResult.RunStatus> WriteCallbackDescriptor(string serviceString, string characteristicString, int value, BCValue Retval)
        {
            GattClientCharacteristicConfigurationDescriptorValue setValue = GattClientCharacteristicConfigurationDescriptorValue.None;
            switch (value)
            {
                case 0: setValue = GattClientCharacteristicConfigurationDescriptorValue.None; break;
                case 1: setValue = GattClientCharacteristicConfigurationDescriptorValue.Notify; break;
                case 2: setValue = GattClientCharacteristicConfigurationDescriptorValue.Indicate; break;
                default:
                    Retval.SetError (1, $"Error: WriteCallbackDescriptor (,,value) must be 0..2, not {value}");
                    return RunResult.RunStatus.ErrorStop;
            }

            //
            // Now start to get the data!
            //
            Guid serviceGuid = Guid.Parse(BluetoothDevice.StringToGuid(serviceString));
            var service = ble.GetGattService(serviceGuid);

            Guid characteristicGuid = Guid.Parse(BluetoothDevice.StringToGuid(characteristicString));
            var characteristics = service.GetCharacteristics(characteristicGuid);


            foreach (var characteristic in characteristics)
            {
                // Notify is much more common than indicate.  (Indicate requires some kind of confirmation?)
                bool notifyOK = setValue == GattClientCharacteristicConfigurationDescriptorValue.Notify && characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify);
                bool indicateOK = setValue == GattClientCharacteristicConfigurationDescriptorValue.Indicate && characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate);
                if (notifyOK || indicateOK)
                {
                    var btstatus = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(setValue);
                    if (!AllGattCallbackData.ContainsKey(characteristic))
                    {
                        AllGattCallbackData[characteristic] = new GattCallbackData(serviceString, characteristicString, characteristic);
                    }
                    AllGattCallbackData[characteristic].CurrConfiguration = value;
                    if (btstatus == GattCommunicationStatus.Success)
                    {
                        Retval.AsString = "OK";
                        return RunResult.RunStatus.OK; 
                    }
                    Retval.SetError (1, $"Error: WriteCallbackDescriptor: unable to send value to {serviceString}, {characteristicString}");
                    return RunResult.RunStatus.OK;
                }
            }
            Retval.SetError (1, $"Error: WriteCallbackDescription: unable to find characteristic {characteristicString} on service {serviceString}");
            return RunResult.RunStatus.ErrorStop;
        }


        public RunResult.RunStatus AddCallback(Guid service, Guid characteristic, BCRunContext context, Windows.Foundation.TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> changeCallback, string functionName, BCValue Retval)
        {
            return AddCallback(service.ToString(), characteristic.ToString(), context, changeCallback, functionName, Retval);
        }

        public RunResult.RunStatus AddCallback(string serviceString, string characteristicString, BCRunContext context, Windows.Foundation.TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> changeCallback, string functionName, BCValue Retval)
        {
            if (changeCallback == null) changeCallback = StandardValueChanged;
            Guid serviceGuid = Guid.Parse(BluetoothDevice.StringToGuid(serviceString));
            var service = ble.GetGattService(serviceGuid);

            Guid characteristicGuid = Guid.Parse(BluetoothDevice.StringToGuid(characteristicString));
            var characteristics = service.GetCharacteristics(characteristicGuid);

            foreach (var characteristic in characteristics)
            {
                if (!AllGattCallbackData.ContainsKey(characteristic))
                {
                    AllGattCallbackData[characteristic] = new GattCallbackData(serviceString, characteristicString, characteristic);
                }
                var data = AllGattCallbackData[characteristic];

                // And set it call up!
                if (data.Callback == null) // A specialization can override the default callback (e.g., the SensorTag gets the X Y Z values from an accelerometer)
                {
                    data.Callback += changeCallback;
                    data.Characteristic.ValueChanged += changeCallback;
                }
                data.Context = context;
                if (!data.FunctionNameList.Contains(functionName))
                {
                    data.FunctionNameList.Add(functionName); // Same one can't be added twice.
                }

                Retval.AsString = "OK";
                return RunResult.RunStatus.OK;
            }
            Retval.SetError (1, $"Error: AddCallback: unable to find characteristic {characteristicString} on service {serviceString}");
            return RunResult.RunStatus.ErrorStop;
        }

        public RunResult.RunStatus RemoveCallback(Guid service, Guid characteristic, string functionName)
        {
            return RemoveCallback(service.ToString(), characteristic.ToString(), functionName);
        }

        // Remove all callbacks by passing in blank string ("")
        public RunResult.RunStatus RemoveCallback(string serviceString, string characteristicString, string functionName)
        {
            foreach (var item in AllGattCallbackData)
            {
                var data = item.Value;
                var isService = serviceString == "" || data.ServiceString == serviceString;
                var isCharacteristic = characteristicString == "" || data.CharacteristicString == characteristicString;
                if (isService && isCharacteristic)
                {
                    // Remove it from the callback!
                    if (functionName == "")
                    {
                        data.FunctionNameList.Clear();
                    }
                    else if (data.FunctionNameList.Contains(functionName))
                    {
                        data.FunctionNameList.Remove(functionName);
                    }
                    // No more functions, so we don't need the callback hooked up.
                    // Why does't this also remove the Notify?
                    if (data.FunctionNameList.Count == 0)
                    {
                        data.Characteristic.ValueChanged -= data.Callback;
                    }
                }
            }

            return RunResult.RunStatus.OK;
        }



        public class GattCallbackData
        {
            public GattCallbackData(string serviceString, string characteristicString, GattCharacteristic characteristic)
            {
                ServiceString = serviceString;
                CharacteristicString = characteristicString;
                Characteristic = characteristic;
            }
            public GattCallbackData(string serviceString, string characteristicString, Windows.Foundation.TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> callback, BCRunContext context, string functionName)
            {
                ServiceString = serviceString;
                CharacteristicString = characteristicString;
                Callback = callback;
                Context = context;
                FunctionNameList.Add(functionName);
            }
            public Windows.Foundation.TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> Callback;

            public string ServiceString;
            public string CharacteristicString;
            public GattCharacteristic Characteristic;
            public int CurrConfiguration;
            public BCRunContext Context;
            public IList<string> FunctionNameList = new List<String>();
        }

        public Dictionary<GattCharacteristic, GattCallbackData> AllGattCallbackData = new Dictionary<GattCharacteristic, GattCallbackData>();
        private void StandardValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            if (AllGattCallbackData.ContainsKey(sender))
            {
                BCRunContext context = AllGattCallbackData[sender].Context;
                foreach (var functionName in AllGattCallbackData[sender].FunctionNameList)
                {
                    var arglist = new List<IExpression>() { new ObjectConstant(this) };
                    for (uint i = 0; i<args.CharacteristicValue.Length; i++)
                    {
                        var value = args.CharacteristicValue.GetByte(i);
                        arglist.Add(new NumericConstant(value));
                    }
                    context.ProgramRunContext.AddCallback(context, functionName, arglist);
                    //await CalculatorConnectionControl.BasicFrameworkElement.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                    //    async () => { await context.Functions[functionName].EvalAsync(context, arglist); });
                }
            }
            // CHANGED!
        }

        private async Task<RunResult.RunStatus> WriteData(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            // First param: e.g. fff0 to write commands
            // Second param: e.g. fff3 for most DOTTI commands

            var guidStr = (await ArgList[0].EvalAsync(context)).AsString;
            Guid serviceGuid = Guid.Parse(StringToGuid(guidStr));
            var service = ble.GetGattService(serviceGuid);

            var charStr = (await ArgList[1].EvalAsync(context)).AsString;
            Guid characteristicGuid = Guid.Parse(StringToGuid(charStr));
            var characteristics = service.GetCharacteristics(characteristicGuid);


            foreach (var characteristic in characteristics)
            {
                if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Write)
                    || characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.WriteWithoutResponse))
                {
                    return await DoWriteBytes(characteristic, context, name, ArgList, Retval);
                }
            }

            Retval.SetError (1, $"Error {name} is unable to read characteristic {charStr}.");
            return RunResult.RunStatus.ErrorStop;
        }

        private async Task<RunResult.RunStatus> DoWriteBytes(GattCharacteristic characteristic, BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            // First two args are the service and characteristic
            const int arraySkip = 2;
            var buffer = new byte[ArgList.Count - arraySkip];

            for (int i = arraySkip; i < ArgList.Count; i++)
            {
                var val = (await ArgList[i].EvalAsync(context)).AsDouble;
                byte b0 = (byte)val;
                buffer[i - arraySkip] = b0;
            }
            var status = await DoWriteBytes(characteristic, buffer, Retval);
            return status;
        }

        public static async Task<RunResult.RunStatus> DoWriteBytes(BluetoothLEDevice Device, string serviceString, string characteristicString, byte[] buffer, BCValue Retval)
        {
            Guid serviceGuid = Guid.Parse(BluetoothDevice.StringToGuid(serviceString));
            Guid characteristicGuid = Guid.Parse(BluetoothDevice.StringToGuid(characteristicString));
            return await DoWriteBytes(Device, serviceGuid, characteristicGuid, buffer, Retval);
        }

        public static async Task<RunResult.RunStatus> DoWriteBytes(BluetoothLEDevice Device, Guid serviceGuid, Guid characteristicGuid, byte[] buffer, BCValue Retval)
        {
            IReadOnlyList<GattCharacteristic> characteristics = null;
            try
            {
                var service = Device.GetGattService(serviceGuid);

                characteristics = service.GetCharacteristics(characteristicGuid);
            }
            catch (Exception ex)
            {
                Retval.SetError (1, $"ERROR: Can't get service when writing value ");
                RTLSystemX.AddError($"Exception can't get service when writng value {ex.Message}");
                return RunResult.RunStatus.ErrorStop;
            }

            foreach (var characteristic in characteristics)
            {
                if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Write)
                    || characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.WriteWithoutResponse))
                {
                    var status = await BluetoothDevice.DoWriteBytes(characteristic, buffer, Retval);
                    return status;
                }
            }

            Retval.SetError (1, $"ERROR: unable to write characteristic {characteristicGuid.ToString()}.");
            return RunResult.RunStatus.ErrorStop;
        }
        public static async Task<RunResult.RunStatus> DoWriteBytes(GattCharacteristic characteristic, byte[] buffer, BCValue Retval)
        {
            try
            {
                var writeType = characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.WriteWithoutResponse) ? GattWriteOption.WriteWithoutResponse : GattWriteOption.WriteWithResponse;
                var status = await characteristic.WriteValueAsync(buffer.AsBuffer(), writeType);
                if (status == GattCommunicationStatus.Unreachable)
                {
                    Retval.SetError (1, $"ERROR: Unable to write value ");
                    return RunResult.RunStatus.ErrorStop;
                }
                else
                {
                    Retval.AsString = "OK";
                }
            }
            catch (Exception ex)
            {
                Retval.SetError (1, $"ERROR: Exception when writing value ");
                RTLSystemX.AddError($"Exception when writing value is {ex.Message}");
                return RunResult.RunStatus.ErrorStop;
            }
            return RunResult.RunStatus.OK;
        }


        public const string BluetoothGuidSuffix = "-0000-1000-8000-00805f9b34fb";
        public static string StringToGuid(string str)
        {
            // Input is e.g., 180a
            var Retval = str;
            if (Retval.Length == 4) Retval = "0000" + Retval;
            if (Retval.Length == 8) Retval = Retval + BluetoothGuidSuffix;
            return Retval;
        }

        public async Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            switch (name)
            {
                case "As":return await DoAs(context, name, ArgList, Retval);

                case "Init":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (di != null)
                    {
                        ble = await BluetoothLEDevice.FromIdAsync(di.Id);
                    }
                    if (ble == null)
                    {
                        Retval.SetError (2, $"{PreferredName}.{name}: unable to get the bluetooth device");
                        return RunResult.RunStatus.ErrorStop;
                    }
                    Retval.AsString = ble.BluetoothAddress.ToString();
                    return RunResult.RunStatus.OK;

                case "ReadCachedByte":
                case "ReadRawByte":
                    if (!BCObjectUtilities.CheckArgs(2, 2, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (ble == null)
                    {
                        Retval.SetError (2, $"Error: you must call Init() before calling {name}");
                        return RunResult.RunStatus.ErrorStop;
                    }
                    return await ReadByte(context, name, ArgList, Retval);

                case "ReadCachedBytes":
                case "ReadRawBytes":
                    if (!BCObjectUtilities.CheckArgs(2, 2, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (ble == null)
                    {
                        Retval.SetError(2, $"Error: you must call Init() before calling {name}");
                        return RunResult.RunStatus.ErrorStop;
                    }
                    return await ReadBytes(context, name, ArgList, Retval);

                case "ToString":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    Retval.AsString = $"{PreferredName} Name={di.Name}";
                    return RunResult.RunStatus.OK;

                case "WriteBytes":
                    if (!BCObjectUtilities.CheckArgs(3, 99, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (ble == null)
                    {
                        Retval.SetError(1, $"Error: you must call Init() before calling {name}");
                        return RunResult.RunStatus.ErrorStop;
                    }
                    return await WriteData(context, name, ArgList, Retval);

                case "AddCallback":
                    {
                        if (!BCObjectUtilities.CheckArgs(3, 3, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        //0=service (string)
                        //1=characteristic (string)
                        //2=callback name
                        var serviceString = (await ArgList[0].EvalAsync(context)).AsString;
                        var characteristicString = (await ArgList[1].EvalAsync(context)).AsString;
                        var functionName = (await ArgList[2].EvalAsync(context)).AsString;
                        return AddCallback(serviceString, characteristicString, context, null, functionName, Retval);
                    }

                case "RemoveCallback":
                    {
                        if (!BCObjectUtilities.CheckArgs(3, 3, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        //0=service (string)
                        //1=characteristic (string)
                        //2=callback name
                        if (!await BCObjectUtilities.CheckArgValue(3, "value", 0, 2, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var serviceString = (await ArgList[0].EvalAsync(context)).AsString;
                        var characteristicString = (await ArgList[1].EvalAsync(context)).AsString;
                        var functionName = (await ArgList[1].EvalAsync(context)).AsString;
                        return RemoveCallback (serviceString, characteristicString, functionName);
                    }

                case "WriteCallbackDescriptor":
                    {
                        if (!BCObjectUtilities.CheckArgs(3, 3, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        //0=service (string)
                        //1=characteristic (string)
                        //2=value 0=None 1=Notify 2=Indicate
                        if (!await BCObjectUtilities.CheckArgValue(2, "value", 0, 2, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var serviceString = (await ArgList[0].EvalAsync(context)).AsString;
                        var characteristicString = (await ArgList[1].EvalAsync(context)).AsString;
                        var value = (int)(await ArgList[2].EvalAsync(context)).AsDouble;
                        return await WriteCallbackDescriptor(serviceString, characteristicString, value, Retval);
                    }

#if NEVER_EVER_DEFINED
                case "WriteClientCharacteristicConfigurationDescriptorAsync":
                    if (!BCObjectUtilities.CheckArgs(4, 4, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    //0=service (string)
                    //1=characteristic (string)
                    //3=value
                    if (! await BCObjectUtilities.CheckArgValue(3, "value", 0, 2, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;

                    break;
#endif

                default:
                    await Task.Delay(0);
                    BCValue.MakeNoSuchMethod(Retval, this, name);
                    return RunResult.RunStatus.ErrorContinue;
            }
        }

        public void Dispose()
        {
            if (ble != null)
            {
                ble.Dispose();
            }
        }
    }
}
