using AdvancedCalculator.Bluetooth;
using BCBasic;
using System;
using System.Collections.Generic;
using System.IO;
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
    class MetaMotion : IObjectValue
    {
        static int ObjectId = 0;
        int CurrObjectId = -1;
        BluetoothLEDevice Ble = null;
        Bluetooth Bluetooth { get; set;  }
        public MetaMotion(Bluetooth bluetooth, BluetoothLEDevice ble)
        {
            Bluetooth = bluetooth;
            CurrObjectId = ObjectId++;
            Ble = ble;
        }

        public string PreferredName { get { return "MetaMotion"; } }

        public IList<string> GetNames() { return new List<string>() { "Methods" }; }

        public BCValue GetValue(string name)
        {
            switch (name)
            {
                case "Methods":
                    return new BCValue("AccelerometerSetup,AltimeterSetup,BarometerSetup,ButtonSetup,GetName,GetManufacturerName,GetPower,GyroscopeSetup,LedConfig,LedOff,LedOn,LightSensorSetup,MagnetometerSetup,SetColor,TemperatureRead,TemperatureSetup,ToString"); 
            }
            return BCValue.MakeNoSuchProperty(this, name);
        }

        public void InitializeForRun()
        {
        }

        GattDeviceService dataService = null;
        GattCharacteristic dataCharacteristic = null;
        // Must unsubscribe to the callback (otherwise we get multiple callbacks
        // and it just messes everything up)
        public void RunComplete()
        {
            if (dataCharacteristic != null)
            {
                //NOTE: really?  Doesn't the Device.AddCallback do this for us?
                dataCharacteristic.ValueChanged -= Characteristic_ValueChanged;
                dataCharacteristic = null;
            }
        }

        public void SetValue(string name, BCValue value)
        {
            // Do nothing; cannot set any value.
        }

        // These are what the MetaWear MetaMotion always uses.
        private string ServiceString = "{326a9000-85cb-9195-d9dd-464cfbbae75a}";
        private string CommandString = "{326a9001-85cb-9195-d9dd-464cfbbae75a}";
        private string DataString = "{326a9006-85cb-9195-d9dd-464cfbbae75a}";

        private static byte BUTTON = 1;
        private static byte BUTTON_POWER = 1;
        //private static byte BUTTON_ON = 1;
        //private static byte BUTTON_OFF = 0;



        private static byte LED = 2;

        private static byte LED_PLAY = 1;
        //private static byte LED_PLAY_PAUSE = 0;
        private static byte LED_PLAY_PLAY = 1;
        //private static byte LED_PLAY_AUTOPLAY = 2;

        private static byte LED_STOP = 2;
        private static byte LED_STOP_STOP = 0;
        //private static byte LED_STOP_STOP_AND_CLEAR = 1;

        private static byte LED_CONFIG = 3;
        private static byte LED_GREEN = 0;
        private static byte LED_RED = 1;
        private static byte LED_BLUE = 2;

        private static byte ACCELEROMETER = 3;
        private static byte ACC_POWER = 1;
        private static byte ACC_INTERUPT = 2;
        private static byte ACC_CONFIG = 3;
        private static byte ACC_SUBSCRIBE = 4;

        private static byte BAROMETER = 0x12;
        private static byte BAROMETER_BAROMETER = 1;
        private static byte BAROMETER_ALTITUDE = 2;
        private static byte BAROMETER_CONFIG = 3;
        private static byte BAROMETER_POWER = 4;

        private static byte AMBIENTLIGHTSENSER = 0x14;
        private static byte AMB_POWER = 1;
        private static byte AMB_CONFIG = 2;
        private static byte AMB_STREAM = 3;

        private static byte GYROSCOPE = 0x13;
        private static byte GYR_POWER = 1;
        private static byte GYR_INTERUPT = 2;
        private static byte GYR_CONFIG = 3;
        private static byte GYR_SUBSCRIBE = 5;

        private static byte MAGNETOMETER = 0x15;
        private static byte MAG_POWER = 1;
        private static byte MAG_INTERUPT = 2;
        private static byte MAG_CONFIG_RATE = 3;
        private static byte MAG_CONFIG_POWER = 4;
        private static byte MAG_SUBSCRIBE = 5;

        private static byte TEMPERATURE = 0x04;
        private static byte TEMPERATURE_READ = 0x81; 

        // Metawear has just a single

        private class CallbackData
        {
            public string dataType;
            public string functionName;
            public BCRunContext context;
            public override string ToString() { return $"{dataType}+{functionName}"; }
        }

        // BasicCallbacks is a dictionary of dictionaries.  It manages the callbacks back into Basic.
        // 
        //    "Accelerometer" :: {  "MyBasicFunctionA" :: callbackdata , "MyBasicFunctionB" :: callbackdata }
        //
        // so that when "Accelerometer" data comes in, I can call all of the BC BASIC functions
        // that have been registered.

        private Dictionary<string, Dictionary<string, CallbackData>> BasicCallbacks = new Dictionary<string, Dictionary<string, CallbackData>>();
        private void AddBasicCallback(CallbackData data)
        {
            lock (this)
            {
                if (!BasicCallbacks.ContainsKey(data.dataType))
                {
                    BasicCallbacks.Add(data.dataType, new Dictionary<string, CallbackData>());
                }
                if (!BasicCallbacks[data.dataType].ContainsKey(data.functionName))
                {
                    BasicCallbacks[data.dataType].Add(data.functionName, data);
                }
            }
        }

        private void RemoveBasicCallback (string dataType, string functionName)
        {
            //
            // Remove the callback from the BasicCallbacks list.  Make sure to 
            // clean up correctly.
            //
            lock (this)
            {
                if (BasicCallbacks.ContainsKey(dataType))
                {
                    var list = BasicCallbacks[dataType];
                    if (list.ContainsKey(functionName))
                    {
                        list.Remove(functionName);
                        if (list.Count == 0)
                        {
                            BasicCallbacks.Remove(dataType);
                        }
                    }
                }
            }
        }

        private async Task RemoveCallbackAsync(string dataType, string functionName)
        {
            RemoveBasicCallback(dataType, functionName);

            // If we removed the last of the callback, tell the device to stop notifying
            if (BasicCallbacks.Count == 0)
            {
                if (this.dataService != null) // will always be the case.
                {
                    Guid characteristicGuid = Guid.Parse(BluetoothDevice.StringToGuid(DataString));
                    var characteristics = dataService.GetCharacteristics(characteristicGuid);

                    //NOTE: what?  Why are we looping when we already now the dataCharacteristic?
                    foreach (var characteristic in characteristics) // Only ever one!
                    {
                        // Always the case; it's part of being a MetaWear device
                        if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
                        {
                            var btstatus = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
                            if (dataCharacteristic != null)
                            {
                                dataCharacteristic.ValueChanged -= Characteristic_ValueChanged;
                                dataCharacteristic = null;
                            }
                        }
                    }
                    this.dataService = null;
                }
            }
        }


        private async Task SetupCallbackAsync(string dataType, string functionName, BCRunContext context)
        {
            if (this.dataService == null)
            {
                // Tell the device to notify when the data characteristic changes
                // Tell the Bluetooth stack to call our common callback routine when it gets a callback from the device.
                Guid serviceGuid = Guid.Parse(BluetoothDevice.StringToGuid(ServiceString));
                dataService = Ble.GetGattService(serviceGuid);

                Guid characteristicGuid = Guid.Parse(BluetoothDevice.StringToGuid(DataString));
                var characteristics = dataService.GetCharacteristics(characteristicGuid);


                foreach (var characteristic in characteristics) // Only ever one!
                {
                    if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
                    {
                        var btstatus = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                        if (dataCharacteristic == null)
                        {
                            dataCharacteristic = characteristic;
                            characteristic.ValueChanged += Characteristic_ValueChanged;
                            Bluetooth.AddDeviceToComplete(this); // The ValueChanged must be undone at the end.
                        }
                    }
                }
            }

            // Add to the callback list
            var data = new CallbackData() { context = context, dataType = dataType, functionName = functionName };
            AddBasicCallback(data);
        }

        private void CallCallbacks(string dataType, List<IExpression> arglist)
        {
            Dictionary<string, CallbackData> callbacks = new Dictionary<string, CallbackData>();
            lock (this)
            {
                // Copy out the list
                if (BasicCallbacks.ContainsKey(dataType))
                {
                    foreach (var item in BasicCallbacks[dataType])
                    {
                        callbacks.Add(item.Key, item.Value);
                    }
                }
            }

            foreach (var callback in callbacks)
            {
                var functionName = callback.Value.functionName;
                var context = callback.Value.context;
                context.ProgramRunContext.AddCallback(context, functionName, arglist);
                //await CalculatorConnectionControl.BasicFrameworkElement.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                //    async () => { await context.Functions[functionName].EvalAsync(context, arglist); });
            }
        }

        private async void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var bytes = args.CharacteristicValue.ToArray();
            IBuffer buffer = bytes.AsBuffer();
            var len = buffer.Length;
            var byteStream = buffer.AsStream().AsInputStream();
            var dr = new DataReader(byteStream);
            dr.ByteOrder = ByteOrder.LittleEndian;
            await dr.LoadAsync(len);
            var b0 = dr.ReadByte(); // e.g., ACCELEROMETER
            var b1 = dr.ReadByte(); // e.g., ACC_READ

            if (len > 2)
            {
                if (b0 == ACCELEROMETER && b1 == ACC_SUBSCRIBE)
                {
                    if (CurrAccelerationScale == null) return;
                    Int16 xint = dr.ReadInt16();
                    Int16 yint = dr.ReadInt16();
                    Int16 zint = dr.ReadInt16();

                    double x = (double)xint / CurrAccelerationScale.Scale;
                    double y = (double)yint / CurrAccelerationScale.Scale;
                    double z = (double)zint / CurrAccelerationScale.Scale;

                    var arglist = new List<IExpression>() { new ObjectConstant(this), new NumericConstant(x), new NumericConstant(y), new NumericConstant(z) };
                    CallCallbacks("Accelerometer", arglist);
                }
                else if (b0 == AMBIENTLIGHTSENSER && b1 == AMB_STREAM)
                {
                    // https://github.com/mbientlab/MetaWear-SDK-Android/blob/master/library/src/main/java/com/mbientlab/metawear/module/AmbientLightLtr329.java

                    // The actual value here doesn't really make sense.
                    var lux = dr.ReadUInt32();
                    var arglist = new List<IExpression>() { new ObjectConstant(this), new NumericConstant(lux) };
                    CallCallbacks("LightSensor", arglist);
                }
                else if (b0 == BAROMETER && b1 == BAROMETER_ALTITUDE)
                {
                    var height = (double)dr.ReadInt32() / 256.0;
                    var arglist = new List<IExpression>() { new ObjectConstant(this), new NumericConstant(height) };
                    System.Diagnostics.Debug.WriteLine($"METAWEAR Altimeter: {b0} {b1} {height}");
                    CallCallbacks("Altimeter", arglist);
                }
                else if (b0 == BAROMETER && b1 == BAROMETER_BAROMETER)
                {
                    var pascal = (double)dr.ReadInt32() / 256.0;
                    var arglist = new List<IExpression>() { new ObjectConstant(this), new NumericConstant(pascal) };
                    System.Diagnostics.Debug.WriteLine($"METAWEAR Barometer: {b0} {b1} {pascal}");
                    CallCallbacks("Barometer", arglist);
                }
                else if (b0 == BUTTON && b1 == BUTTON_POWER)
                {
                    // Last bytes is the switch value; either '0' or '1'
                    byte value = dr.ReadByte();
                    var arglist = new List<IExpression>() { new ObjectConstant(this), new NumericConstant(value) };
                    CallCallbacks("Button", arglist);
                }
                else if (b0 == GYROSCOPE && b1 == GYR_SUBSCRIBE)
                {
                    if (CurrGyroscopeScale == null) return;
                    Int16 xint = dr.ReadInt16();
                    Int16 yint = dr.ReadInt16();
                    Int16 zint = dr.ReadInt16();

                    double x = (double)xint / CurrGyroscopeScale.Scale;
                    double y = (double)yint / CurrGyroscopeScale.Scale;
                    double z = (double)zint / CurrGyroscopeScale.Scale;

                    var arglist = new List<IExpression>() { new ObjectConstant(this), new NumericConstant(x), new NumericConstant(y), new NumericConstant(z) };
                    CallCallbacks("Gyroscope", arglist);
                }
                else if (b0 == MAGNETOMETER && b1 == MAG_SUBSCRIBE)
                {
                    // https://github.com/mbientlab/MetaWear-SDK-Android/blob/master/library/src/main/java/com/mbientlab/metawear/impl/MagnetometerBmm150Impl.java
                    // I'm dubious about this actual data.

                    Int16 xint = dr.ReadInt16();
                    Int16 yint = dr.ReadInt16();
                    Int16 zint = dr.ReadInt16();

                    double x = (double)xint / 16.0; 
                    double y = (double)yint / 16.0; 
                    double z = (double)zint / 16.0; 

                    var arglist = new List<IExpression>() { new ObjectConstant(this), new NumericConstant(x), new NumericConstant(y), new NumericConstant(z) };
                    CallCallbacks("Magnetometer", arglist);
                }
                else if (b0 == TEMPERATURE && b1 == TEMPERATURE_READ)
                {
                    var channel = dr.ReadByte();
                    var tempint = dr.ReadInt16();
                    double temperatureInCelcius = (double)tempint / 8.0;
                    var arglist = new List<IExpression>() { new ObjectConstant(this), new NumericConstant(temperatureInCelcius) };
                    CallCallbacks("Temperature", arglist);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"METAWEAR CALLBACK: {b0} {b1}");
                    ;
                }
            }
        }

        class AccelerationData
        {
            public AccelerationData(byte configByte, int gForce, double scale)
            {
                ConfigByte = configByte;
                GForce = gForce;
                Scale = scale;
            }
            public byte ConfigByte;
            public int GForce;
            public double Scale;

            public static AccelerationData GetFromGForce(IList<AccelerationData> list, double gforce)
            {
                AccelerationData Retval = null;
                foreach (var item in list)
                {
                    if (item.GForce >= gforce && (Retval == null || item.GForce < Retval.GForce)) // Better match
                    {
                        Retval = item;
                    }
                }
                if (Retval == null) Retval = list.OrderByDescending(item => item.GForce).First();

                return Retval;
            }
        }

        List<AccelerationData> AccelerationTable = new List<AccelerationData>()
        {
            new AccelerationData (0x03, 2, 16384.0),
            new AccelerationData (0x05, 4, 8192.0),
            new AccelerationData (0x08, 8, 4096.0),
            new AccelerationData (0x0C, 16, 2048.0),
        };
        AccelerationData CurrAccelerationScale = null;


        class GyroscopeData
        {
            public GyroscopeData(byte configByte, int dps, double scale)
            {
                ConfigByte = configByte;
                DPS = dps;
                Scale = scale;
            }
            public byte ConfigByte;
            public int DPS;
            public double Scale;

            public static GyroscopeData GetFromDPS(IList<GyroscopeData> list, double dps)
            {
                GyroscopeData Retval = null;
                foreach (var item in list)
                {
                    if (item.DPS >= dps && (Retval == null || item.DPS < Retval.DPS)) // Better match
                    {
                        Retval = item;
                    }
                }
                if (Retval == null) Retval = list.OrderByDescending(item => item.DPS).First();

                return Retval;
            }
        }
        // {  FSR_2000DPS=0, FSR_1000DPS, FSR_500DPS, FSR_250DPS, FSR_125DPS};

        List<GyroscopeData> GyroscopeTable = new List<GyroscopeData>()
        {
            new GyroscopeData (0x00, 2000,  16.4),
            new GyroscopeData (0x01, 1000,  32.8),
            new GyroscopeData (0x02,  500,  65.6),
            new GyroscopeData (0x03,  250, 131.2),
            new GyroscopeData (0x04,  125, 262.4),
        };
        GyroscopeData CurrGyroscopeScale = null;

        // SensorRateData is for e.g., "config byte 0x07 means 50 times per second
        // SensorSpeedData is for e.g., "config byte 0x06 means every 2 seconds"
        class SensorRateData
        {
            public SensorRateData(byte configByte, double rate)
            {
                ConfigByte = configByte;
                Rate = rate;
            }
            public byte ConfigByte;
            public double Rate;

            public static SensorRateData GetFromRate(IList<SensorRateData> list, double rate)
            {
                SensorRateData Retval = null;
                foreach (var item in list)
                {
                    if (item.Rate >= rate && (Retval == null || item.Rate < Retval.Rate)) // Better match
                    {
                        Retval = item;
                    }
                }
                if (Retval == null) Retval = list.OrderByDescending(item => item.Rate).First();

                return Retval;
            }
        }


        class SensorSpeedData
        {
            public SensorSpeedData(byte configByte, double speed)
            {
                ConfigByte = configByte;
                Speed = speed;
            }
            public byte ConfigByte;
            public double Speed;

            public static SensorSpeedData GetFromSpeed(IList<SensorSpeedData> list, double speed)
            {
                SensorSpeedData Retval = null;
                foreach (var item in list)
                {
                    if (item.Speed <= speed && (Retval == null || item.Speed > Retval.Speed)) // Better match
                    {
                        Retval = item;
                    }
                }
                if (Retval == null) Retval = list.OrderBy(item => item.Speed).First();

                return Retval;
            }
        }

        List<SensorRateData> AccelerometerRateTable = new List<SensorRateData>()
        {
            new SensorRateData (0x01, 0.78125),
            new SensorRateData (0x02, 1.5625),
            new SensorRateData (0x03, 3.125),
            new SensorRateData (0x04, 6.25),
            new SensorRateData (0x05, 12.5),
            new SensorRateData (0x06, 25),
            new SensorRateData (0x07, 50),
            new SensorRateData (0x08, 100),
            new SensorRateData (0x09, 200),
            new SensorRateData (0x0A, 400),
            new SensorRateData (0x0B, 800),
            new SensorRateData (0x0C, 1600),
            new SensorRateData (0x0D, 3200),
        };
        SensorRateData CurrAccelometerRate = null;


        // Altimeter and Barometer share this data
        List<SensorSpeedData> AltimeterSpeedTable = new List<SensorSpeedData>()
        {
            new SensorSpeedData (0x00, 0.005),
            new SensorSpeedData (0x01, 0.0625),
            new SensorSpeedData (0x02, 0.125),
            new SensorSpeedData (0x03, 0.250),
            new SensorSpeedData (0x04, 0.500),
            new SensorSpeedData (0x05, 1.0),
            new SensorSpeedData (0x06, 2.0),
            new SensorSpeedData (0x07, 4.0),
        };
        SensorSpeedData CurrAltimeterSpeed = null;

        List<SensorRateData> GyroscopeRateTable = new List<SensorRateData>()
        {
            new SensorRateData (0x06, 25),
            new SensorRateData (0x07, 50),
            new SensorRateData (0x08, 100),
            new SensorRateData (0x09, 200),
            new SensorRateData (0x0A, 400),
            new SensorRateData (0x0B, 800),
            new SensorRateData (0x0C, 1600),
            new SensorRateData (0x0D, 3200),
        };
        SensorRateData CurrGyroscopeRate = null;


        private async Task<byte[]> SetLedConfig (byte color, byte intensityHigh, byte intensityLow, ushort rise, ushort high, ushort fall, ushort pulseLength, byte repeat)
        {
            var ms = new InMemoryRandomAccessStream();
            var dw = new DataWriter(ms);
            dw.ByteOrder = ByteOrder.LittleEndian; // the MetaWear is a little-endian device.

            dw.WriteByte(LED); //2=Module
            dw.WriteByte(LED_CONFIG); // 3==Command is Configure
            dw.WriteByte(color);
            dw.WriteByte(2); // unknown; it's just a magic '2'
            dw.WriteByte(intensityHigh);
            dw.WriteByte(intensityLow);
            dw.WriteUInt16(rise);
            dw.WriteUInt16(high);
            dw.WriteUInt16(fall);
            dw.WriteUInt16(pulseLength);
            dw.WriteUInt16(0); // Unknown value (is a delay amount)
            dw.WriteByte(repeat);

            // Now pull all the data out
            await dw.StoreAsync();
            ms.Seek(0);
            var bytes = new byte[ms.Size];
            await ms.ReadAsync(bytes.AsBuffer(), (uint)ms.Size, Windows.Storage.Streams.InputStreamOptions.None);
            return bytes;
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

                case "GetManufacturerName":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await BluetoothDevice.ReadString(Ble, "180a", "2a29", BluetoothCacheMode.Uncached, Retval);

                case "GetPower":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await BluetoothDevice.ReadByte(Ble, "180f", "2a19", BluetoothCacheMode.Uncached, Retval);

                // These are all of the DEVICE specific calls


                case "AccelerometerSetup":
                    {
                        // 0 or 1 (on/off)
                        // name of function to call on change
                        if (!BCObjectUtilities.CheckArgs(2, 4, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        if (!await BCObjectUtilities.CheckArgValue(0, "onoff", 0, 1, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var onoff = (byte)((await ArgList[0].EvalAsync(context)).AsDouble);
                        string functionName = (await ArgList[1].EvalAsync(context)).AsString;


                        RunResult.RunStatus result = RunResult.RunStatus.ErrorContinue; // 

                        // Get the callbacks all hooked up.
                        switch (onoff)
                        {
                            case 0:
                                {
                                    byte[] power = new byte[] { ACCELEROMETER, ACC_POWER, 0 }; // 0=off
                                    result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, power, Retval);
                                    await RemoveCallbackAsync("Accelerometer", functionName);
                                    break;
                                }
                            case 1:
                                {
                                    double gforce = ArgList.Count > 2 ? (await ArgList[2].EvalAsync(context)).AsDouble : 2.0; //DEFAULT: 2g
                                    double rate = ArgList.Count > 3 ? (await ArgList[3].EvalAsync(context)).AsDouble : 25.0; //DEFAULT: 25x per second

                                    CurrAccelerationScale = AccelerationData.GetFromGForce(AccelerationTable, gforce);
                                    CurrAccelometerRate = SensorRateData.GetFromRate(AccelerometerRateTable, rate);
                                    byte isSigned = 0;
                                    byte bwp = 2;
                                    //byte rate = 7; // 7==50 Hz
                                    byte configByte = (byte)((isSigned << 7) | (bwp << 4) | CurrAccelometerRate.ConfigByte);
                                    byte range = CurrAccelerationScale.ConfigByte; // rawAccelerometerRange; // 3==2g range
                                    byte[] config = new byte[] { ACCELEROMETER, ACC_CONFIG, configByte, range };
                                    result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, config, Retval);

                                    byte[] interupt = new byte[] { ACCELEROMETER, ACC_INTERUPT, 1, 0 };
                                    result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, interupt, Retval);

                                    byte[] subscribe = new byte[] { ACCELEROMETER, ACC_SUBSCRIBE, 1, 0 };
                                    result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, subscribe, Retval);

                                    byte[] power = new byte[] { ACCELEROMETER, ACC_POWER, 1 };
                                    result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, power, Retval);

                                    await SetupCallbackAsync("Accelerometer", functionName, context);
                                    break;
                                }
                        }

                        return result;
                    }

                case "AltimeterSetup":
                case "BarometerSetup":
                    {
                        // 0 or 1 (on/off)
                        // name of function to call on change
                        if (!BCObjectUtilities.CheckArgs(2, 3, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        if (!await BCObjectUtilities.CheckArgValue(0, "onoff", 0, 1, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var onoff = (byte)((await ArgList[0].EvalAsync(context)).AsDouble);
                        string functionName = (await ArgList[1].EvalAsync(context)).AsString;

                        RunResult.RunStatus result = RunResult.RunStatus.ErrorContinue; // 

                        // Get the callbacks all hooked up.
                        switch (onoff)
                        {
                            case 0:
                                {
                                    byte[] altitude = new byte[] { BAROMETER, BAROMETER_ALTITUDE, 0 };
                                    result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, altitude, Retval);

                                    byte[] barometer = new byte[] { BAROMETER, BAROMETER_BAROMETER, 0 };
                                    result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, barometer, Retval);

                                    byte[] power = new byte[] { BAROMETER, BAROMETER_POWER, 0, 0 }; // 0=off
                                    result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, power, Retval);

                                    await RemoveCallbackAsync(name == "AltimeterSetup" ? "Altimeter" : "Barometer", functionName);
                                    break;
                                }
                            case 1:
                                {
                                    double speed = ArgList.Count > 2 ? (await ArgList[2].EvalAsync(context)).AsDouble : 1.0; //DEFAULT: 1 second

                                    CurrAltimeterSpeed = SensorSpeedData.GetFromSpeed(AltimeterSpeedTable, speed);

                                    byte oversample = 3; // 3=standard 
                                    byte standbytime = CurrAltimeterSpeed.ConfigByte; // 7=4 seconds 5==1 second  2==0.5 mseconds
                                    byte filtermode = 0; // 0==off
                                    byte configByte1 = (byte)((oversample<< 2));
                                    byte configByte2 = (byte)((standbytime<< 5) | (filtermode<< 2));

                                    byte[] power = new byte[] { BAROMETER, BAROMETER_POWER, 1, 1 };
                                    result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, power, Retval);

                                    byte[] config = new byte[] { BAROMETER, BAROMETER_CONFIG, configByte1, configByte2};
                                    result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, config, Retval);

                                    // Either the altimeter OR the barometer can be on. (This is a BC BASIC restriction)
                                    byte[] altitude = new byte[] { BAROMETER, BAROMETER_ALTITUDE, (byte)(name=="AltimeterSetup" ? 1 : 0) };
                                    result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, altitude, Retval);

                                    byte[] barometer = new byte[] { BAROMETER, BAROMETER_BAROMETER, (byte)(name == "BarometerSetup" ? 1 : 0) };
                                    result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, barometer, Retval);

                                    await SetupCallbackAsync(name=="AltimeterSetup" ? "Altimeter" : "Barometer", functionName, context);
                                    break;
                                }
                        }

                        return result;
                    }

                case "ButtonSetup":
                    {
                        // 0 or 1 (on/off)
                        // name of function to call on change
                        if (!BCObjectUtilities.CheckArgs(2, 2, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        if (!await BCObjectUtilities.CheckArgValue(0, "onoff", 0, 1, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var onoff = (byte)((await ArgList[0].EvalAsync(context)).AsDouble);
                        string functionName = (await ArgList[1].EvalAsync(context)).AsString;

                        byte[] cmd;
                        RunResult.RunStatus result = RunResult.RunStatus.ErrorContinue; // 


                        // Get the callbacks all hooked up.

                        switch (onoff)
                        {
                            case 0:
                                cmd = new byte[3] { BUTTON, BUTTON_POWER, onoff };
                                result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, cmd, Retval);

                                await RemoveCallbackAsync("Button", functionName);
                                break;
                            case 1:
                                cmd = new byte[3] { BUTTON, BUTTON_POWER, onoff };
                                result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, cmd, Retval);

                                await SetupCallbackAsync("Button", functionName, context);
                                break;
                        }

                        return result;
                    }

                case "GyroscopeSetup":
                    {
                        // 0 or 1 (on/off)
                        // name of function to call on change
                        if (!BCObjectUtilities.CheckArgs(2, 4, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        if (!await BCObjectUtilities.CheckArgValue(0, "onoff", 0, 1, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var onoff = (byte)((await ArgList[0].EvalAsync(context)).AsDouble);
                        string functionName = (await ArgList[1].EvalAsync(context)).AsString;


                        RunResult.RunStatus result = RunResult.RunStatus.ErrorContinue; // 

                        // Get the callbacks all hooked up.
                        switch (onoff)
                        {
                            case 0:
                                {
                                    byte[] power = new byte[] { GYROSCOPE, GYR_POWER, 0 }; // 0=off
                                    result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, power, Retval);

                                    await RemoveCallbackAsync("Gyroscope", functionName);
                                    break;
                                }
                            case 1:
                                {
                                    double dps = ArgList.Count > 2 ? (await ArgList[2].EvalAsync(context)).AsDouble : 500.0; //DEFAULT: 2g
                                    double rate = ArgList.Count > 3 ? (await ArgList[3].EvalAsync(context)).AsDouble : 25.0; //DEFAULT: 25x per second

                                    CurrGyroscopeScale = GyroscopeData.GetFromDPS(GyroscopeTable, dps);
                                    CurrGyroscopeRate = SensorRateData.GetFromRate(GyroscopeRateTable, rate);

                                    byte bwp = 2;
                                    //byte rate = 7; // 7==50 Hz
                                    byte configByte = (byte)((bwp << 4) | CurrGyroscopeRate.ConfigByte);
                                    byte precision = CurrGyroscopeScale.ConfigByte; // rawAccelerometerRange; // 3==2g range
                                    byte[] config = new byte[] { GYROSCOPE, GYR_CONFIG, configByte, precision };
                                    result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, config, Retval);

                                    byte[] interupt = new byte[] { GYROSCOPE, GYR_INTERUPT, 1, 0 };
                                    result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, interupt, Retval);

                                    byte[] subscribe = new byte[] { GYROSCOPE, GYR_SUBSCRIBE, 1, 0 };
                                    result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, subscribe, Retval);

                                    byte[] power = new byte[] { GYROSCOPE, GYR_POWER, 1 };
                                    result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, power, Retval);

                                    await SetupCallbackAsync("Gyroscope", functionName, context);
                                    break;
                                }
                        }

                        return result;
                    }

                case "LedOff":
                    {
                        if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;

                        var cmd = new byte[3] { LED, LED_STOP, LED_STOP_STOP };
                        var result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, cmd, Retval);
                        return result;
                    }

                case "LedOn":
                    {
                        if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;


                        var cmd = new byte[3] { LED, LED_PLAY, LED_PLAY_PLAY};
                        var result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, cmd, Retval);
                        return result;
                    }

                case "LightSensorSetup":
                    {
                        // 0 or 1 (on/off)
                        // name of function to call on change
                        if (!BCObjectUtilities.CheckArgs(2, 2, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        if (!await BCObjectUtilities.CheckArgValue(0, "onoff", 0, 1, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var onoff = (byte)((await ArgList[0].EvalAsync(context)).AsDouble);
                        string functionName = (await ArgList[1].EvalAsync(context)).AsString;

                        RunResult.RunStatus result = RunResult.RunStatus.ErrorContinue; // 

                        // Get the callbacks all hooked up.
                        switch (onoff)
                        {
                            case 0:
                                {
                                    byte[] power = new byte[] { ACCELEROMETER, ACC_POWER, 0 }; // 0=off
                                    result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, power, Retval);

                                    await RemoveCallbackAsync("LightSensor", functionName);
                                    break;
                                }
                            case 1:
                                {
                                    byte gain = 0; // 0==default gain; results are 0..64K
                                    byte integrationTime = 0; // 0==100 milliseconds (default)
                                    byte rate = 3; // 3==500 milliseconds (default)
                                    byte configByte1 = (byte)((gain << 2)); // ALS_CONTR Register (0x80) (reset is handled by the firmware)
                                    byte configByte2 = (byte)((integrationTime << 3) | rate); // ALS_MEAS_RATE Register 0x85

                                    byte[] power = new byte[] { AMBIENTLIGHTSENSER, AMB_POWER, 1 };
                                    result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, power, Retval);

                                    await Task.Delay(100); // Must delay before configuring.

                                    byte[] config = new byte[] { AMBIENTLIGHTSENSER, AMB_CONFIG, configByte1, configByte2};
                                    result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, config, Retval);

                                    byte[] stream = new byte[] { AMBIENTLIGHTSENSER, AMB_STREAM, 1, 0 };
                                    result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, stream, Retval);

                                    await SetupCallbackAsync("LightSensor", functionName, context);
                                    break;
                                }
                        }

                        return result;
                    }
                case "MagnetometerSetup":
                    {
                        // 0 or 1 (on/off)
                        // name of function to call on change
                        if (!BCObjectUtilities.CheckArgs(2, 4, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        if (!await BCObjectUtilities.CheckArgValue(0, "onoff", 0, 1, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var onoff = (byte)((await ArgList[0].EvalAsync(context)).AsDouble);
                        string functionName = (await ArgList[1].EvalAsync(context)).AsString;


                        RunResult.RunStatus result = RunResult.RunStatus.ErrorContinue; // 

                        // Get the callbacks all hooked up.
                        switch (onoff)
                        {
                            case 0:
                                {
                                    byte[] power = new byte[] { MAGNETOMETER, MAG_POWER, 0 }; // 0=off
                                    result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, power, Retval);

                                    await RemoveCallbackAsync("Magnetometer", functionName);
                                    break;
                                }
                            case 1:
                                {
                                    byte[] config_power = new byte[] { MAGNETOMETER, MAG_CONFIG_POWER, 0x04, 0x14 };
                                    result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, config_power, Retval);

                                    byte[] config_rate = new byte[] { MAGNETOMETER, MAG_CONFIG_RATE, 0x02 };
                                    result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, config_rate, Retval);

                                    byte[] interupt = new byte[] { MAGNETOMETER, MAG_INTERUPT, 1, 0 };
                                    result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, interupt, Retval);

                                    byte[] subscribe = new byte[] { MAGNETOMETER, MAG_SUBSCRIBE, 1, 0 };
                                    result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, subscribe, Retval);

                                    byte[] power = new byte[] { MAGNETOMETER, MAG_POWER, 1 };
                                    result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, power, Retval);

                                    await SetupCallbackAsync("Magnetometer", functionName, context);
                                    break;
                                }
                        }

                        return result;
                    }

                // Unlike the other values, you can't get a stream of temperature readings.
                // You first call TemperatureSetup to get the callbacks all lined up
                // Then call TemperatureRead for each reading.
                case "TemperatureRead":
                    {
                        if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        byte[] cmd;
                        cmd = new byte[3] { TEMPERATURE, TEMPERATURE_READ, 0 }; // 0==internal value
                        var result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, cmd, Retval);
                        return result;
                    }


                case "TemperatureSetup":
                    {
                        if (!BCObjectUtilities.CheckArgs(2, 2, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        if (!await BCObjectUtilities.CheckArgValue(0, "onoff", 0, 1, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var onoff = (byte)((await ArgList[0].EvalAsync(context)).AsDouble);
                        string functionName = (await ArgList[1].EvalAsync(context)).AsString;

                        RunResult.RunStatus result = RunResult.RunStatus.ErrorContinue; // 

                        switch (onoff)
                        {
                            case 0:
                                await RemoveCallbackAsync("Temperature", functionName);
                                break;
                            case 1:
                                byte[] cmd;
                                cmd = new byte[3] { TEMPERATURE, TEMPERATURE_READ, 0 }; // 0==internal value
                                result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, cmd, Retval);

                                await SetupCallbackAsync("Temperature", functionName, context);
                                break;
                        }

                        return result;
                    }




                case "LedConfig":
                    {
                        if (!BCObjectUtilities.CheckArgs(8, 8, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        if (!await BCObjectUtilities.CheckArgValue(0, "led", 0, 2, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        if (!await BCObjectUtilities.CheckArgValue(1, "high", 0, 255, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        if (!await BCObjectUtilities.CheckArgValue(2, "low", 0, 255, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        if (!await BCObjectUtilities.CheckArgValue(3, "riseTime", 0, ushort.MaxValue, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        if (!await BCObjectUtilities.CheckArgValue(4, "highTime", 0, ushort.MaxValue, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        if (!await BCObjectUtilities.CheckArgValue(5, "fallTime", 0, ushort.MaxValue, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        if (!await BCObjectUtilities.CheckArgValue(6, "totalTime", 0, ushort.MaxValue, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        if (!await BCObjectUtilities.CheckArgValue(7, "repeat", 0, 255, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        // Convert the input 0..255 to MetaWear 0..31

                        var led = (byte)((await ArgList[0].EvalAsync(context)).AsDouble);
                        var hi = (byte)((await ArgList[1].EvalAsync(context)).AsDouble / 8.0);
                        var lo = (byte)((await ArgList[2].EvalAsync(context)).AsDouble / 8.0);

                        var riseTime = (ushort)((await ArgList[3].EvalAsync(context)).AsDouble);
                        var highTime = (ushort)((await ArgList[4].EvalAsync(context)).AsDouble);
                        var fallTime = (ushort)((await ArgList[5].EvalAsync(context)).AsDouble);
                        var totalTime = (ushort)((await ArgList[6].EvalAsync(context)).AsDouble);
                        var repeat = (byte)((await ArgList[7].EvalAsync(context)).AsDouble);

                        byte[] cmd;
                        RunResult.RunStatus result;
                        cmd = await SetLedConfig(led, hi, lo, riseTime, highTime, fallTime, totalTime, repeat);
                        result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, cmd, Retval);

                        // Configure only; don't play
                        //cmd = new byte[3] { LED, LED_PLAY, LED_PLAY_PLAY };
                        //result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, cmd, Retval);
                        return result;
                    }


                case "SetColor":
                    {
                        if (!BCObjectUtilities.CheckArgs(3, 3, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        if (!await BCObjectUtilities.CheckArgValue(0, "red", 0, 255, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        if (!await BCObjectUtilities.CheckArgValue(1, "green", 0, 255, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        if (!await BCObjectUtilities.CheckArgValue(2, "blue", 0, 255, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        // Convert the input 0..255 to MetaWear 0..31
                        var r = (byte)((await ArgList[0].EvalAsync(context)).AsDouble / 8.0);
                        var g = (byte)((await ArgList[1].EvalAsync(context)).AsDouble / 8.0);
                        var b = (byte)((await ArgList[2].EvalAsync(context)).AsDouble / 8.0);
                        byte[] cmd;
                        RunResult.RunStatus result;
                        cmd = await SetLedConfig(LED_GREEN, g, g, 0, 500, 0, 1000, 0);
                        result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, cmd, Retval);

                        cmd = await SetLedConfig(LED_RED, r, r, 0, 500, 0, 1000, 0);
                        result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, cmd, Retval);

                        cmd = await SetLedConfig(LED_BLUE, b, b, 0, 500, 0, 1000, 0);
                        result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, cmd, Retval);

                        cmd = new byte[3] { LED, LED_PLAY, LED_PLAY_PLAY };
                        result = await BluetoothDevice.DoWriteBytes(Ble, ServiceString, CommandString, cmd, Retval);
                        return result;
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
