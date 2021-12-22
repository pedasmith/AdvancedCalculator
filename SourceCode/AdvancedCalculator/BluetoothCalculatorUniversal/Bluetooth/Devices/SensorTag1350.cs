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
    class SensorTag1350 : IObjectValue
    {
        BluetoothDevice Device = null;
        BluetoothLEDevice Ble = null;
        public SensorTag1350(BluetoothDevice device, BluetoothLEDevice ble)
        {
            Device = device;
            Ble = ble;
            Device?.Bluetooth?.AddDeviceToComplete(this);
        }

        //const string SUFFIX = "-0451-4000-b000-000000000000";
        // InvenSense MPU9250 https://www.invensense.com/products/motion-tracking/9-axis/mpu-9250/
        private string AccelerometerService = "f000aa80-0451-4000-b000-000000000000";
        private string AccelerometerData = "f000aa81-0451-4000-b000-000000000000";
        private string AccelerometerConfig = "f000aa82-0451-4000-b000-000000000000";
        private string AccelerometerPeriod = "f000aa83-0451-4000-b000-000000000000";

        //Epcos T5400-C953
        private string BarometerService = "f000aa40-0451-4000-b000-000000000000";
        private string BarometerData = "f000aa41-0451-4000-b000-000000000000";
        private string BarometerConfig = "f000aa42-0451-4000-b000-000000000000";
        //HISTORY: private string BarometerCalibration = "f000aa43-0451-4000-b000-000000000000";
        private string BarometerPeriod = "f000aa44-0451-4000-b000-000000000000";

        private string ButtonService = "ffe0";
        private string ButtonData = "ffe1";


        // Sensirion SHT21  http://www.sensirion.com/en/pdf/product_information/Datasheet-humidity-sensor-SHT21.pdf
        private string HumidityService = "f000aa20-0451-4000-b000-000000000000";
        private string HumidityData = "f000aa21-0451-4000-b000-000000000000";
        private string HumidityConfig = "f000aa22-0451-4000-b000-000000000000";
        private string HumidityPeriod = "f000aa23-0451-4000-b000-000000000000";

        // Texas Instruments TMP007 http://www.ti.com/lit/ds/symlink/tmp007.pdf
        private string IRService = "f000aa00-0451-4000-b000-000000000000";
        private string IRData = "f000aa01-0451-4000-b000-000000000000";
        private string IRConfig = "f000aa02-0451-4000-b000-000000000000";
        private string IRPeriod = "f000aa03-0451-4000-b000-000000000000";


        // Texas Instruments OPT3001 http://www.ti.com/lit/ds/symlink/opt3001.pdf
        private string OpticalService = "f000aa70-0451-4000-b000-000000000000";
        private string OpticalData = "f000aa71-0451-4000-b000-000000000000";
        private string OpticalConfig = "f000aa72-0451-4000-b000-000000000000";
        private string OpticalPeriod = "f000aa73-0451-4000-b000-000000000000";


        // IO Service
        private string IOService = "f000aa64-0451-4000-b000-000000000000";
        private string IOData = "f000aa65-0451-4000-b000-000000000000";
        private string IOConfig = "f000aa66-0451-4000-b000-000000000000";


        // LCD Display (Watch) Service for SensorTag
        // http://processors.wiki.ti.com/index.php/CC26xx_LCD#LCD_in_the_SensorTag_application
        // http://processors.wiki.ti.com/index.php/Display_DevPack_User_Guide
        // https://git.ti.com/sensortag-20-android/sensortag-20-android/trees/master/sensortag20/BleSensorTag/src/main/java/com/example/ti/ble/sensortag
        //private string WatchService = "f000ad00-0451-4000-b000-000000000000"; // Part of SensorTagWatchDevPack.cs now
        //private string WatchData = "f000ad01-0451-4000-b000-000000000000"; // Part of SensorTagWatchDevPack.cs now
        //private string WatchConfig = "f000ad02-0451-4000-b000-000000000000"; // Part of SensorTagWatchDevPack.cs now




        public virtual string PreferredName { get { return "SensorTag1350"; } }

        public IList<string> GetNames() { return new List<string>() { "Methods" }; }


        double AccRangeConfig = 2.0;
        int ButtonLeft = 0; // bit 0
        int ButtonRight = 0; // bit 1
        int ButtonSide = 0; // bit 2 Only notifies in test most

        public BCValue GetValue(string name)
        {
            switch (name)
            {
                case "ButtonLeft": return new BCValue(ButtonLeft);
                case "ButtonRight": return new BCValue(ButtonRight);
                case "ButtonSide": return new BCValue(ButtonSide);

                case "Methods":
                    return new BCValue("GetName,GetPower,AccelerometerSetup,BarometerSetup,ButtonSetup,HumiditySetup,IO,IRSetup,OpticalSetup,ToString");
            }
            return BCValue.MakeNoSuchProperty(this, name);
        }

        public void InitializeForRun()
        {
        }

        public void RunComplete()
        {
            Device?.RemoveCallback("", "", "");
        }

        public void SetValue(string name, BCValue value)
        {
            // Do nothing; cannot set any value.
        }


        // The SensorTag has a variety of sensor each with their own setup requirements.  This method
        // attempts to have a single pattern for setting up the sensors (which is why it's complex
        // and looks like a leaky abstraction).
        // value (0=off 1=on)
        // period
        // function
        private async Task<RunResult.RunStatus> CommonSensorTagSetupAsync(string serviceString, 
            string characteristicString, string periodString, byte periodByte, string configString, byte[] configBytes, 
            Windows.Foundation.TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> callback, 
            string functionName, BCRunContext context, 
            string name, IList<IExpression> ArgList, BCValue Retval)
        {
            bool SendData = configBytes[0] == 0 ? false : true;
            RunResult.RunStatus status;

            if (configString != "")
            {
                status = await BluetoothDevice.DoWriteBytes(Ble, serviceString, configString, configBytes, Retval);
                if (status == RunResult.RunStatus.ErrorStop) { Retval.AsString += " (config)"; return status; }
            }

            if (SendData && periodString != "")
            {
                byte[] buffer = { periodByte };
                status = await BluetoothDevice.DoWriteBytes(Ble, serviceString, periodString, buffer, Retval);
                // Weirdly, the SensorTag documents a bunch of the sensors as having period settings.
                // In reality, a bunch do not -- e.g., no IR period setting.
                // So failing to set the period is considered "normal" and won't cause this method to fail.
                //if (status == RunResult.RunStatus.ErrorStop) { Retval.AsString += " (period)"; return status; }
            }

            //
            // Now start to get the data!
            //
            Guid serviceGuid = Guid.Parse(BluetoothDevice.StringToGuid(serviceString));
            var service = Ble.GetGattService(serviceGuid);

            Guid characteristicGuid = Guid.Parse(BluetoothDevice.StringToGuid(characteristicString));
            var characteristics = service.GetCharacteristics(characteristicGuid);


            foreach (var characteristic in characteristics)
            {
                if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
                {
                    GattCommunicationStatus btstatus;
                    if (SendData)
                    {
                        btstatus = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                        Device.AddCallback(serviceString, characteristicString, context, callback, functionName, Retval);
                    }
                    else
                    {
                        btstatus = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
                        Device.RemoveCallback(serviceString, characteristicString, functionName);
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

        private async Task<RunResult.RunStatus> AccelerometerSetup(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            var serviceString = AccelerometerService;
            string characteristicString = AccelerometerData;
            string configString = AccelerometerConfig;
            string periodString = AccelerometerPeriod;
            Windows.Foundation.TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> callback = Acc_ValueChanged;

            var config = (ushort)(await ArgList[0].EvalAsync(context)).AsDouble;
            byte[] configBytes = { (byte)(config & 0xFF), (byte)((config >> 8) & 0xFF)};
            switch (configBytes[1] & 0x03)
            {
                case 0: AccRangeConfig = 2.0; break; // 2G
                case 1: AccRangeConfig = 4.0; break; // 4G
                case 2: AccRangeConfig = 8.0; break; // 8G
                case 3: AccRangeConfig = 16.0; break; // 16G
            }
            AccRangeConfig = 8.0; // Docs are wrong; data is always as if set to 8G.
            var periodByte = (byte)(await ArgList[1].EvalAsync(context)).AsDouble;
            string functionName = (await ArgList[2].EvalAsync(context)).AsString;

            var status = await CommonSensorTagSetupAsync(serviceString, characteristicString, periodString, periodByte, configString, configBytes, callback,functionName, context, name, ArgList, Retval);
            return status;
        }

        private void Acc_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            if (Device.AllGattCallbackData.ContainsKey(sender))
            {
                var callbackData = Device.AllGattCallbackData[sender];
                BCRunContext context = callbackData.Context;


                if (args.CharacteristicValue.Length == 18)
                {
                    var dr = DataReader.FromBuffer(args.CharacteristicValue);
                    dr.ByteOrder = ByteOrder.LittleEndian;
                    var gx = dr.ReadInt16();
                    var gy = dr.ReadInt16();
                    var gz = dr.ReadInt16();
                    var ax = dr.ReadInt16();
                    var ay = dr.ReadInt16();
                    var az = dr.ReadInt16();
                    var mx = dr.ReadInt16();
                    var my = dr.ReadInt16();
                    var mz = dr.ReadInt16();

                    // Convert the raw data into processed doubles

                    // Rotation in degrees per second, min -250 max +250
                    double RotX = (double)gx * 500.0 / 65536;
                    double RotY = (double)gy * 500.0 / 65536;
                    double RotZ = (double)gz * 500.0 / 65536;

                    // Units are G force
                    double AccX = (double)ax * AccRangeConfig / 32768.0;
                    double AccY = (double)ay * AccRangeConfig / 32768.0;
                    double AccZ = (double)az * AccRangeConfig / 32768.0;

                    // Units are micro-Tesla
                    double MagX = (double)mx;
                    double MagY = (double)my;
                    double MagZ = (double)mz;

                    foreach (var functionName in callbackData.FunctionNameList)
                    {
                        var arglist = new List<IExpression>() { new ObjectConstant(this),
                            new NumericConstant(AccX), new NumericConstant(AccY), new NumericConstant(AccZ),
                            new NumericConstant(MagX), new NumericConstant(MagY), new NumericConstant(MagZ),
                            new NumericConstant(RotX), new NumericConstant(RotY), new NumericConstant(RotZ),
                        };
                        context.ProgramRunContext.AddCallback(context, functionName, arglist);
                        //await CalculatorConnectionControl.BasicFrameworkElement.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                        //    async () => { await context.Functions[functionName].EvalAsync(context, arglist); });
                    }
                }
                else if (args.CharacteristicValue.Length == 3)
                {
                    // Kept for historical purposes.  The original 2541 SensorTag
                    // returned plain old bytes here.
                    sbyte bX = (sbyte)args.CharacteristicValue.GetByte(0);
                    sbyte bY = (sbyte)args.CharacteristicValue.GetByte(1);
                    sbyte bZ = (sbyte)args.CharacteristicValue.GetByte(2);

                    double AccX = (double)bX / 64.0;
                    double AccY = (double)bY / 64.0;
                    double AccZ = -((double)bZ / 64.0); // The documentation says to invert

                    foreach (var functionName in callbackData.FunctionNameList)
                    {
                        var arglist = new List<IExpression>() { new ObjectConstant(this), new NumericConstant(AccX), new NumericConstant(AccY), new NumericConstant(AccZ) };
                        context.ProgramRunContext.AddCallback(context, functionName, arglist);
                        //await CalculatorConnectionControl.BasicFrameworkElement.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                        //    async () => { await context.Functions[functionName].EvalAsync(context, arglist); });
                    }
                }
                // Else there's a big problem

            }
        }

        // HISTORY: The 2451 SensorTag needed to be calibrarted.  
        // The v2 ones (like 1350) do not.
        // HISTORY: Was needed by 2451: byte[] BarometerCalibrationData = null;
        private async Task<RunResult.RunStatus> BarometerSetup(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            var serviceString = BarometerService;
            string characteristicString = BarometerData;
            string configString = BarometerConfig;
            string periodString = BarometerPeriod;
            Windows.Foundation.TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> callback = Barometer_ValueChanged;

            byte[] configBytes = { (byte)(await ArgList[0].EvalAsync(context)).AsDouble };
            var periodByte = (byte)(await ArgList[1].EvalAsync(context)).AsDouble;
            string functionName = (await ArgList[2].EvalAsync(context)).AsString;

            //HISTORY: BarometerCalibrationData = null;
            RunResult.RunStatus status = RunResult.RunStatus.OK;

            //HISTORY: The 2451 needed to be calibrated.
            // The barometer needs to be calibrated. 
            //if (configBytes[0] == 1)
            //{
            //    status = await BarometerGetCalibrationAsync(Retval);
            //    if (status != RunResult.RunStatus.OK) return status;
            //}
            status = await CommonSensorTagSetupAsync(serviceString, characteristicString, periodString, periodByte, configString, configBytes, callback, functionName, context, name, ArgList, Retval);

            if (status != RunResult.RunStatus.OK) return status;


            if (status != RunResult.RunStatus.OK) Retval.AsString = $"ERROR: unable to read calibration data";
            return status;
        }
#if NOT_NEEDED_ON_V2_SENSORTAG
        private async Task<RunResult.RunStatus> BarometerGetCalibrationAsync(BCValue Retval)
        {
            var serviceString = BarometerService;
            string characteristicString = BarometerCalibration;
            const byte ReadConfig = 2;

            var status = RunResult.RunStatus.ErrorStop;

            // Set the config to 2
            status = await BluetoothDevice.DoWriteBytes(Ble, serviceString, BarometerConfig, new byte[] { ReadConfig }, Retval);
            if (status == RunResult.RunStatus.ErrorStop) { Retval.AsString += " (config)"; return status; }


            status = RunResult.RunStatus.ErrorStop; // default value if we can't read the configuration data
            Guid serviceGuid = Guid.Parse(BluetoothDevice.StringToGuid(serviceString));
            var service = Ble.GetGattService(serviceGuid);

            Guid characteristicGuid = Guid.Parse(BluetoothDevice.StringToGuid(characteristicString));
            var characteristics = service.GetCharacteristics(characteristicGuid);

            foreach (var characteristic in characteristics)
            {
                if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Read))
                {
                    var readResult = await characteristic.ReadValueAsync(BluetoothCacheMode.Uncached); // read the calibration data fresh.
                    if (readResult.Status == GattCommunicationStatus.Success && readResult.Value.Length == 16)
                    {
                        BarometerCalibrationData = readResult.Value.ToArray();
                        status = RunResult.RunStatus.OK;
                    }
                }
            }

            if (status != RunResult.RunStatus.OK) Retval.AsString = $"ERROR: unable to read calibration data";
            return status;
        }
#endif
        private void Barometer_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            if (Device.AllGattCallbackData.ContainsKey(sender))
            {
                var callbackData = Device.AllGattCallbackData[sender];
                BCRunContext context = callbackData.Context;

                byte[] calibration = null; // HISTORY: BarometerCalibrationData;
                if (args.CharacteristicValue.Length == 6)
                {
                    var dr = DataReader.FromBuffer(args.CharacteristicValue);
                    dr.ByteOrder = ByteOrder.LittleEndian;
                    double temp = dr.ReadUInt16();
                    var msb = dr.ReadByte();
                    temp = temp + (msb << 16); 
                    temp = temp / 100.0;
                    double pressure = dr.ReadUInt16();
                    pressure = pressure + (dr.ReadByte() * 65536.0); // time ^16
                    pressure = pressure / 100.0;

                    // Temp is degrees C
                    // Pressure is hectoPascal
                    foreach (var functionName in callbackData.FunctionNameList.ToList()) // Duplicate the list to prevent race conditions
                    {
                        var arglist = new List<IExpression>() { new ObjectConstant(this), new NumericConstant(temp), new NumericConstant(pressure) };
                        context.ProgramRunContext.AddCallback(context, functionName, arglist);
                        //await CalculatorConnectionControl.BasicFrameworkElement.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                        //    async () => { await context.Functions[functionName].EvalAsync(context, arglist); });
                    }
                }
                // HISTORY: The 2451 returned 2 two-byte values which had to be adjusted
                else if (args.CharacteristicValue.Length == 4 && calibration != null)
                {
                    uint tempLSB = (uint)(byte)args.CharacteristicValue.GetByte(0);
                    int tempMSB = (int)(sbyte)args.CharacteristicValue.GetByte(1);
                    uint pressureLSB = (uint)(byte)args.CharacteristicValue.GetByte(2);
                    uint pressureMSB = (uint)(byte)args.CharacteristicValue.GetByte(3);

                    // C uses units c1..c7 (and that's what the doc say).
                    // Java codes uses values c0..c7
                    // Since my code is like the Java code, follow the Java convention.
                    uint c0 = (uint)((calibration[1] << 8) + calibration[0]);
                    uint c1 = (uint)((calibration[3] << 8) + calibration[2]);
                    uint c2 = (uint)((calibration[5] << 8) + calibration[4]);
                    uint c3 = (uint)((calibration[7] << 8) + calibration[6]);

                    int c4 = (int)(((sbyte)calibration[4 * 2 + 0] << 8) + calibration[4 * 2 + 1]);
                    int c5 = (int)(((sbyte)calibration[5 * 2 + 0] << 8) + calibration[5 * 2 + 1]);
                    int c6 = (int)(((sbyte)calibration[6 * 2 + 0] << 8) + calibration[6 * 2 + 1]);
                    int c7 = (int)(((sbyte)calibration[7 * 2 + 0] << 8) + calibration[7 * 2 + 1]);


                    int t_r;  // Temperature raw value from sensor
                    int p_r;  // Pressure raw value from sensor
                    Double t_a;   // Temperature actual value in unit centi degrees celsius
                    Double S; // Interim value in calculation
                    Double O; // Interim value in calculation
                    Double p_a;   // Pressure actual value in unit Pascal.

                    t_r = (int)((tempMSB<<8) + tempLSB);
                    p_r = (int)((pressureMSB << 8) + pressureLSB);

                    // Java algorithm has the t_a value off by factor of 100
                    t_a = ((c0 * t_r / Math.Pow(2, 8) + c1 * Math.Pow(2, 6))) / Math.Pow(2, 16);
                    S = c2 + c3 * t_r / Math.Pow(2, 17) + ((c4 * t_r / Math.Pow(2, 15)) * t_r) / Math.Pow(2, 19);
                    O = c5 * Math.Pow(2, 14) + c6 * t_r / Math.Pow(2, 3) + ((c7 * t_r / Math.Pow(2, 15)) * t_r) / Math.Pow(2, 4);
                    p_a = (S * p_r + O) / Math.Pow(2, 14);
                    p_a = p_a / 100.0;

                    double temp = t_a;
                    double pressure = p_a;
                    foreach (var functionName in callbackData.FunctionNameList.ToList()) // Duplicate the list to prevent race conditions
                    {
                        var arglist = new List<IExpression>() { new ObjectConstant(this), new NumericConstant(temp), new NumericConstant(pressure) };
                        context.ProgramRunContext.AddCallback(context, functionName, arglist);
                        //await CalculatorConnectionControl.BasicFrameworkElement.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                        //    async () => { await context.Functions[functionName].EvalAsync(context, arglist); });
                    }
                }
                // Else there's a big problem

            }
        }


        // value (0=off 1=on)
        // function
        private async Task<RunResult.RunStatus> ButtonSetup(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            var serviceString = ButtonService;
            string characteristicString = ButtonData;

            byte[] configBytes = { (byte)(await ArgList[0].EvalAsync(context)).AsDouble };
            string functionName = (await ArgList[1].EvalAsync(context)).AsString;

            // ConfigByte is critical even if we don't set a config register.  That's because it's also used
            // for turning notifications on and off.
            var status = await CommonSensorTagSetupAsync(ButtonService, characteristicString, "", 0, "", configBytes, Button_ValueChanged, functionName, context, name, ArgList, Retval);
            return status;
        }

        private void Button_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            if (Device.AllGattCallbackData.ContainsKey(sender))
            {
                var callbackData = Device.AllGattCallbackData[sender];
                BCRunContext context = callbackData.Context;


                if (args.CharacteristicValue.Length == 1)
                {
                    byte b = (byte)args.CharacteristicValue.GetByte(0);
                    ButtonLeft = (b & 1) != 0 ? 1 : 0;
                    ButtonRight = (b & 2) != 0 ? 1 : 0;
                    ButtonSide = (b & 4) != 0 ? 1 : 0;

                    var flist = callbackData.FunctionNameList.ToList();
                    foreach (var functionName in flist)
                    {
                        var arglist = new List<IExpression>() { new ObjectConstant(this), new NumericConstant(ButtonLeft), new NumericConstant(ButtonRight), new NumericConstant(ButtonSide) };
                        context.ProgramRunContext.AddCallback(context, functionName, arglist, CallbackData.Suppression.NoSuppress);
                        //await CalculatorConnectionControl.BasicFrameworkElement.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                        //    async () => { await context.Functions[functionName].EvalAsync(context, arglist); });
                    }
                }
                // Else there's a big problem

            }
        }


        private async Task<RunResult.RunStatus> HumiditySetup(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            var serviceString = HumidityService;
            string characteristicString = HumidityData;
            string configString = HumidityConfig;
            string periodString = HumidityPeriod;
            Windows.Foundation.TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> callback = Humidity_ValueChanged;

            byte[] configBytes = { (byte)(await ArgList[0].EvalAsync(context)).AsDouble };
            var periodByte = (byte)(await ArgList[1].EvalAsync(context)).AsDouble;
            string functionName = (await ArgList[2].EvalAsync(context)).AsString;

            var status = await CommonSensorTagSetupAsync(serviceString, characteristicString, periodString, periodByte, configString, configBytes, callback, functionName, context, name, ArgList, Retval);
            return status;
        }

        private void Humidity_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            if (Device.AllGattCallbackData.ContainsKey(sender))
            {
                var callbackData = Device.AllGattCallbackData[sender];
                BCRunContext context = callbackData.Context;


                if (args.CharacteristicValue.Length == 4)
                {
                    uint tempLSB = (uint)(byte)args.CharacteristicValue.GetByte(0);
                    uint tempMSB = (uint)(byte)args.CharacteristicValue.GetByte(1);
                    uint humLSB = (uint)(byte)args.CharacteristicValue.GetByte(2);
                    uint humMSB = (uint)(byte)args.CharacteristicValue.GetByte(3);

                    double v;

                    uint rawT = (tempMSB << 8) + (tempLSB & 0xFB);
                    v = -46.85 + 175.72 / 65536 * (double)rawT;
                    double temp = rawT == 0 ? 0 : v;

                    uint rawH = (humMSB << 8) + (humLSB & 0xFB);
                    v = -6.0 + 125.0 / 65536 * (double)rawH; // RH= -6 + 125 * SRH/2^16
                    double humidity = rawH == 0 ? 0 : v;

                    foreach (var functionName in callbackData.FunctionNameList.ToList()) // Duplicate the list to prevent race conditions
                    {
                        var arglist = new List<IExpression>() { new ObjectConstant(this), new NumericConstant(temp), new NumericConstant(humidity) };
                        context.ProgramRunContext.AddCallback(context, functionName, arglist);
                        //await CalculatorConnectionControl.BasicFrameworkElement.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                        //    async () => { await context.Functions[functionName].EvalAsync(context, arglist); });
                    }
                }
                // Else there's a big problem

            }
        }


        private async Task<RunResult.RunStatus> IRSetup(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            var serviceString = IRService;
            string characteristicString = IRData;
            string configString = IRConfig;
            string periodString = IRPeriod;
            Windows.Foundation.TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> callback = IR_ValueChanged;

            byte[] configBytes = { (byte)(await ArgList[0].EvalAsync(context)).AsDouble };
            var periodByte = (byte)(await ArgList[1].EvalAsync(context)).AsDouble;
            string functionName = (await ArgList[2].EvalAsync(context)).AsString;

            var status = await CommonSensorTagSetupAsync(serviceString, characteristicString, periodString, periodByte, configString, configBytes, callback, functionName, context, name, ArgList, Retval);
            return status;
        }

        private void IR_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            if (Device.AllGattCallbackData.ContainsKey(sender))
            {
                var callbackData = Device.AllGattCallbackData[sender];
                BCRunContext context = callbackData.Context;


                if (args.CharacteristicValue.Length == 4)
                {
                    var dr = DataReader.FromBuffer(args.CharacteristicValue);
                    dr.ByteOrder = ByteOrder.LittleEndian;

                    const double SCALE_LSB = 0.03125;
                    var rawObjTemp = dr.ReadUInt16();
                    var rawAmbTemp = dr.ReadUInt16();

                    double objTemp = (rawObjTemp >> 2) * SCALE_LSB;
                    double ambTempC = (rawAmbTemp >> 2) * SCALE_LSB;

#if HISTORY
                    // These were the calculations for the TMP006 from the 2451 SensorTag.
                    // The TMP007 has totally different calculations.
                    uint objLSB = (uint)(byte)args.CharacteristicValue.GetByte(0);
                    int objMSB = (int)(sbyte)args.CharacteristicValue.GetByte(1);
                    uint ambLSB = (uint)(byte)args.CharacteristicValue.GetByte(2);
                    uint ambMSB = (uint)(byte)args.CharacteristicValue.GetByte(3);

                    double ambTempC = (double)((ambMSB << 8) + ambLSB) / 128.0; // in degrees C?
                    double Tdie = ambTempC + 273.15;

                    double Vobj2 = (double)((objMSB << 8) + objLSB); // Pretty magical value
                    Vobj2 *= 0.00000015625;

                    double S0 = 5.593E-14;  // Calibration factor
                    double a1 = 1.75E-3;
                    double a2 = -1.678E-5;
                    double b0 = -2.94E-5;
                    double b1 = -5.7E-7;
                    double b2 = 4.63E-9;
                    double c2 = 13.4;
                    double Tref = 298.15;
                    double S = S0 * (1 + a1 * (Tdie - Tref) + a2 * Math.Pow ((Tdie - Tref), 2));
                    double Vos = b0 + b1 * (Tdie - Tref) + b2 * Math.Pow((Tdie - Tref), 2);
                    double fObj = (Vobj2 - Vos) + c2 * Math.Pow((Vobj2 - Vos), 2);
                    double tObj = Math.Pow(Math.Pow(Tdie, 4) + (fObj / S), .25);

                    double objTemp = tObj - 273.15;
#endif
                    foreach (var functionName in callbackData.FunctionNameList.ToList()) // Duplicate the list to prevent race conditions
                    {
                        var arglist = new List<IExpression>() { new ObjectConstant(this), new NumericConstant(objTemp), new NumericConstant(ambTempC) };
                        context.ProgramRunContext.AddCallback(context, functionName, arglist);
                        //await CalculatorConnectionControl.BasicFrameworkElement.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                        //    async () => { await context.Functions[functionName].EvalAsync(context, arglist); });
                    }
                }
                // Else there's a big problem

            }
        }

        private async Task<RunResult.RunStatus> OpticalSetup(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            var serviceString = OpticalService;
            string characteristicString = OpticalData;
            string configString = OpticalConfig;
            string periodString = OpticalPeriod;
            Windows.Foundation.TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> callback = Optical_ValueChanged;

            byte[] configBytes = { (byte)(await ArgList[0].EvalAsync(context)).AsDouble };
            var periodByte = (byte)(await ArgList[1].EvalAsync(context)).AsDouble;
            string functionName = (await ArgList[2].EvalAsync(context)).AsString;

            var status = await CommonSensorTagSetupAsync(serviceString, characteristicString, periodString, periodByte, configString, configBytes, callback, functionName, context, name, ArgList, Retval);
            return status;
        }

        private void Optical_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            if (Device.AllGattCallbackData.ContainsKey(sender))
            {
                var callbackData = Device.AllGattCallbackData[sender];
                BCRunContext context = callbackData.Context;


                if (args.CharacteristicValue.Length == 2)
                {
                    var dr = DataReader.FromBuffer(args.CharacteristicValue);
                    dr.ByteOrder = ByteOrder.LittleEndian;
                    var rawData = dr.ReadUInt16();
                    var m = rawData & 0x0FFF;
                    var e = (rawData & 0xF000) >> 12;
                    var lux = (double)m * (0.01 * Math.Pow(2.0, (double)e));

                    foreach (var functionName in callbackData.FunctionNameList.ToList()) // Duplicate the list to prevent race conditions
                    {
                        var arglist = new List<IExpression>() { new ObjectConstant(this), new NumericConstant(lux) };
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
                // Standard calls that are propertly supported by DOTTI.
                // Manufacturer is "supported", but the result is just "Manufacturer Name"
                // which isn't very interesting.
                case "GetName":
                    // The Get() functions are non-cached
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await BluetoothDevice.ReadString(Ble, "1800", "2a00", BluetoothCacheMode.Uncached, Retval);

                case "GetPower":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await BluetoothDevice.ReadByte(Ble, "180f", "2a19", BluetoothCacheMode.Uncached, Retval);

                case "Close":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    Dispose();
                    return RunResult.RunStatus.OK;

                // These are all of the device specific calls

                case "AccSetup":
                case "AccelerometerSetup":
                    // 0 or 1 (on/off)
                    // period
                    // name of function to call on change
                    if (!BCObjectUtilities.CheckArgs(3, 3, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(0, "onoff", 0, 1024, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(1, "period", 0, 255, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await AccelerometerSetup(context, name, ArgList, Retval);

                case "BarSetup":
                case "BarometerSetup":
                    // 0 or 1 (on/off)
                    // period
                    // name of function to call on change
                    if (!BCObjectUtilities.CheckArgs(3, 3, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(0, "onoff", 0, 1, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(1, "period", 10, 255, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await BarometerSetup(context, name, ArgList, Retval);

                case "ButtonSetup":
                    // 0 or 1 (on/off)
                    // name of function to call on change
                    if (!BCObjectUtilities.CheckArgs(2, 2, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(0, "onoff", 0, 1, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await ButtonSetup(context, name, ArgList, Retval);

                case "HumSetup":
                case "HumiditySetup":
                    // 0 or 1 (on/off)
                    // period [units of 10 ms; range is 100 ms to 2.55 seconds]
                    // name of function to call on change
                    if (!BCObjectUtilities.CheckArgs(3, 3, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(0, "onoff", 0, 1, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(1, "period", 10, 255, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await HumiditySetup(context, name, ArgList, Retval);

                case "IO":
                    // 0 or 1 (on/off)
                    // period
                    // name of function to call on change
                    if (!BCObjectUtilities.CheckArgs(1, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(0, "value", 0, 7, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    var ioValue = (byte)(await ArgList[0].EvalAsync(context)).AsDouble;
                    // Be extra careful.  Setting bit 3 will erase the external flash, which is bad.
                    if (ioValue > 7) return RunResult.RunStatus.ErrorStop;

                    // Set the Config value to 1 (remote mode) and data to the ioValue.
                    // Write the data first; otherwise old value (e.g., 0x7F from the system test) will 
                    // be used resulting in the LED turning on and the buzzer buzzing.
                    var iostatus = await BluetoothDevice.DoWriteBytes(Ble, IOService, IOData, new byte[] { ioValue }, Retval);
                    if (iostatus == RunResult.RunStatus.ErrorStop) { Retval.AsString += " (data)"; return iostatus; }

                    byte[] remoteMode = { 1 };
                    iostatus = await BluetoothDevice.DoWriteBytes(Ble, IOService, IOConfig, remoteMode, Retval);
                    if (iostatus == RunResult.RunStatus.ErrorStop) { Retval.AsString += " (config)"; return iostatus; }

                    return RunResult.RunStatus.OK;


                case "IRSetup":
                    // 0 or 1 (on/off)
                    // period
                    // name of function to call on change
                    if (!BCObjectUtilities.CheckArgs(3, 3, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(0, "onoff", 0, 1, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(1, "period", 0, 255, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await IRSetup(context, name, ArgList, Retval);

                case "OpticalSetup":
                    // 0 or 1 (on/off)
                    // period
                    // name of function to call on change
                    if (!BCObjectUtilities.CheckArgs(3, 3, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(0, "onoff", 0, 1, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(1, "period", 0, 255, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await OpticalSetup(context, name, ArgList, Retval);

                case "ToString":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    Retval.AsString = $"Bluetooth specialization {PreferredName}";
                    return RunResult.RunStatus.OK;

                default:
                    if (name.StartsWith ("Watch"))
                    {
                        if (sensorTagWatchDevPack == null)
                        {
                            sensorTagWatchDevPack = new SensorTagWatchDevPack(this, Device, Ble);
                        }
                        return await sensorTagWatchDevPack.RunAsync(context, name, ArgList, Retval);
                    }
                    await Task.Delay(0);
                    BCValue.MakeNoSuchMethod(Retval, this, name);
                    return RunResult.RunStatus.ErrorContinue;
            }
        }
        SensorTagWatchDevPack sensorTagWatchDevPack = null;

        public async void Dispose()
        {
            //NOTE: really? Does this duplicate what's already being done?
            foreach (var item in Device.AllGattCallbackData)
            {
                var characteristic = item.Key;
                characteristic.ValueChanged -= item.Value.Callback; 
                var btstatus = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
            }
            Device.AllGattCallbackData.Clear();

            if (Ble != null)
            {
                Ble.Dispose();
                Ble = null;
            }
        }
    }
}
