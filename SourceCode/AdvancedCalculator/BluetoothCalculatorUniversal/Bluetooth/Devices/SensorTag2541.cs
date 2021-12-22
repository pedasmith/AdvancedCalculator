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
using static AdvancedCalculator.Bluetooth.BluetoothDevice;

namespace AdvancedCalculator.Bluetooth.Devices
{
    class SensorTag2541 : IObjectValue
    {
        BluetoothDevice Device = null;
        BluetoothLEDevice Ble = null;
        public SensorTag2541(BluetoothDevice device, BluetoothLEDevice ble)
        {
            Device = device;
            Ble = ble;
            Device?.Bluetooth?.AddDeviceToComplete(this);
        }

        //const string SUFFIX = "-0451-4000-b000-000000000000";
        // Kionix KXTJ9 http://www.kionix.com/accelerometers/kxtj9
        private string AccelerometerService = "f000aa10-0451-4000-b000-000000000000";
        private string AccelerometerData = "f000aa11-0451-4000-b000-000000000000";
        private string AccelerometerConfig = "f000aa12-0451-4000-b000-000000000000";
        private string AccelerometerPeriod = "f000aa13-0451-4000-b000-000000000000";

        //Epcos T5400-C953
        private string BarometerService = "f000aa40-0451-4000-b000-000000000000";
        private string BarometerData = "f000aa41-0451-4000-b000-000000000000";
        private string BarometerConfig = "f000aa42-0451-4000-b000-000000000000";
        private string BarometerCalibration = "f000aa43-0451-4000-b000-000000000000";
        private string BarometerPeriod = "f000aa44-0451-4000-b000-000000000000";

        private string ButtonService = "ffe0";
        private string ButtonData = "ffe1";

        // Invensense IMU-3000 http://invensense.com/mems/gyro/imu3000.html
        private string GyroscopeService = "f000aa50-0451-4000-b000-000000000000";
        private string GyroscopeData = "f000aa51-0451-4000-b000-000000000000";
        private string GyroscopeConfig = "f000aa52-0451-4000-b000-000000000000";
        private string GyroscopePeriod = "f000aa53-0451-4000-b000-000000000000";

        // Sensirion SHT21  http://www.sensirion.com/en/pdf/product_information/Datasheet-humidity-sensor-SHT21.pdf
        private string HumidityService = "f000aa20-0451-4000-b000-000000000000";
        private string HumidityData = "f000aa21-0451-4000-b000-000000000000";
        private string HumidityConfig = "f000aa22-0451-4000-b000-000000000000";
        private string HumidityPeriod = "f000aa23-0451-4000-b000-000000000000";

        // Texas Instruments TMP006 http://www.ti.com/lit/ug/sbou107/sbou107.pdf
        private string IRService = "f000aa00-0451-4000-b000-000000000000";
        private string IRData = "f000aa01-0451-4000-b000-000000000000";
        private string IRConfig = "f000aa02-0451-4000-b000-000000000000";
        private string IRPeriod = "f000aa03-0451-4000-b000-000000000000";

        // Freescale MAG3110 http://cache.freescale.com/files/sensors/doc/fact_sheet/MAG3110FS.pdf
        private string MagnetometerService = "f000aa30-0451-4000-b000-000000000000";
        private string MagnetometerData = "f000aa31-0451-4000-b000-000000000000";
        private string MagnetometerConfig = "f000aa32-0451-4000-b000-000000000000";
        private string MagnetometerPeriod = "f000aa33-0451-4000-b000-000000000000";



        public string PreferredName { get { return "SensorTag2541"; } }

        public IList<string> GetNames() { return new List<string>() { "Methods" }; }


        double AccX = 0;
        double AccY = 0;
        double AccZ = 0;
        int ButtonLeft = 0; // bit 0
        int ButtonRight = 0; // bit 1
        int ButtonSide = 0; // bit 2 Only notifies in test most

        public BCValue GetValue(string name)
        {
            switch (name)
            {
                case "AccX": return new BCValue(AccX);
                case "AccY": return new BCValue(AccY);
                case "AccZ": return new BCValue(AccZ);

                case "ButtonLeft": return new BCValue(ButtonLeft);
                case "ButtonRight": return new BCValue(ButtonRight);
                case "ButtonSide": return new BCValue(ButtonSide);

                case "Methods":
                    return new BCValue("GetName,GetPower,AccelerometerSetup,BarometerSetup,ButtonSetup,GyroscopeSetup,HumiditySetup,IRSetup,MagnetometerSetup,ToString");
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
            string characteristicString, string periodString, byte periodByte, string configString, byte configByte, 
            Windows.Foundation.TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> callback, 
            string functionName, BCRunContext context, 
            string name, IList<IExpression> ArgList, BCValue Retval)
        {
            byte[] buffer = new byte[1];
            buffer[0] = configByte;  
            bool SendData = buffer[0] == 0 ? false : true;
            RunResult.RunStatus status;

            if (configString != "")
            {
                status = await BluetoothDevice.DoWriteBytes(Ble, serviceString, configString, buffer, Retval);
                if (status == RunResult.RunStatus.ErrorStop) { Retval.AsString += " (config)"; return status; }
            }

            if (SendData && periodString != "")
            {
                buffer[0] = periodByte;
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

            var configByte = (byte)(await ArgList[0].EvalAsync(context)).AsDouble;
            var periodByte = (byte)(await ArgList[1].EvalAsync(context)).AsDouble;
            string functionName = (await ArgList[2].EvalAsync(context)).AsString;

            var status = await CommonSensorTagSetupAsync(serviceString, characteristicString, periodString, periodByte, configString, configByte, callback,functionName, context, name, ArgList, Retval);
            return status;
        }

        private void Acc_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            if (Device.AllGattCallbackData.ContainsKey(sender))
            {
                var callbackData = Device.AllGattCallbackData[sender];
                BCRunContext context = callbackData.Context;


                if (args.CharacteristicValue.Length == 3)
                {
                    sbyte bX = (sbyte)args.CharacteristicValue.GetByte(0);
                    sbyte bY = (sbyte)args.CharacteristicValue.GetByte(1);
                    sbyte bZ = (sbyte)args.CharacteristicValue.GetByte(2);

                    AccX = (double)bX / 64.0;
                    AccY = (double)bY / 64.0;
                    AccZ = -((double)bZ / 64.0); // The documentation says to invert

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

        byte[] BarometerCalibrationData = null;
        private async Task<RunResult.RunStatus> BarometerSetup(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            var serviceString = BarometerService;
            string characteristicString = BarometerData;
            string configString = BarometerConfig;
            string periodString = BarometerPeriod;
            Windows.Foundation.TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> callback = Barometer_ValueChanged;

            var configByte = (byte)(await ArgList[0].EvalAsync(context)).AsDouble;
            var periodByte = (byte)(await ArgList[1].EvalAsync(context)).AsDouble;
            string functionName = (await ArgList[2].EvalAsync(context)).AsString;

            BarometerCalibrationData = null;
            RunResult.RunStatus status = RunResult.RunStatus.OK;

            // The barometer needs to be calibrated. 
            if (configByte == 1)
            {
                status = await BarometerGetCalibrationAsync(Retval);
                if (status != RunResult.RunStatus.OK) return status;
            }
            status = await CommonSensorTagSetupAsync(serviceString, characteristicString, periodString, periodByte, configString, configByte, callback, functionName, context, name, ArgList, Retval);

            if (status != RunResult.RunStatus.OK) return status;


            if (status != RunResult.RunStatus.OK) Retval.AsString = $"ERROR: unable to read calibration data";
            return status;
        }

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

        private void Barometer_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            if (Device.AllGattCallbackData.ContainsKey(sender))
            {
                var callbackData = Device.AllGattCallbackData[sender];
                BCRunContext context = callbackData.Context;

                byte[] calibration = BarometerCalibrationData;
                if (args.CharacteristicValue.Length == 4 && calibration != null)
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

            var configByte = (byte)(await ArgList[0].EvalAsync(context)).AsDouble;
            string functionName = (await ArgList[1].EvalAsync(context)).AsString;

            // ConfigByte is critical even if we don't set a config register.  That's because it's also used
            // for turning notifications on and off.
            var status = await CommonSensorTagSetupAsync(ButtonService, characteristicString, "", 0, "", configByte, Button_ValueChanged, functionName, context, name, ArgList, Retval);
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

        private async Task<RunResult.RunStatus> GyroscopeSetup(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            var serviceString = GyroscopeService;
            string characteristicString = GyroscopeData;
            string configString = GyroscopeConfig;
            string periodString = GyroscopePeriod;
            Windows.Foundation.TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> callback = Gyroscope_ValueChanged; 

            var configByte = (byte)(await ArgList[0].EvalAsync(context)).AsDouble;
            var periodByte = (byte)(await ArgList[1].EvalAsync(context)).AsDouble;
            string functionName = (await ArgList[2].EvalAsync(context)).AsString;

            var status = await CommonSensorTagSetupAsync(serviceString, characteristicString, periodString, periodByte, configString, configByte, callback, functionName, context, name, ArgList, Retval);
            return status;
        }

        private void Gyroscope_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            if (Device.AllGattCallbackData.ContainsKey(sender))
            {
                var callbackData = Device.AllGattCallbackData[sender];
                BCRunContext context = callbackData.Context;


                if (args.CharacteristicValue.Length == 6)
                {
                    uint yLSB = (uint)(byte)args.CharacteristicValue.GetByte(0);
                    int yMSB = (int)(sbyte)args.CharacteristicValue.GetByte(1);
                    uint xLSB = (uint)(byte)args.CharacteristicValue.GetByte(2);
                    int xMSB = (int)(sbyte)args.CharacteristicValue.GetByte(3);
                    uint zLSB = (uint)(byte)args.CharacteristicValue.GetByte(4);
                    int zMSB = (int)(sbyte)args.CharacteristicValue.GetByte(5);

                    double y = (double)((yMSB << 8) + yLSB) * (500.0 / 65536.0) * -1;
                    double x = (double)((xMSB << 8) + xLSB) * (500.0 / 65536.0);
                    double z = (double)((zMSB << 8) + zLSB) * (500.0 / 65536.0);

                    foreach (var functionName in callbackData.FunctionNameList)
                    {
                        var arglist = new List<IExpression>() { new ObjectConstant(this), new NumericConstant(x), new NumericConstant(y), new NumericConstant(z) };
                        context.ProgramRunContext.AddCallback(context, functionName, arglist);
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

            var configByte = (byte)(await ArgList[0].EvalAsync(context)).AsDouble;
            var periodByte = (byte)(await ArgList[1].EvalAsync(context)).AsDouble;
            string functionName = (await ArgList[2].EvalAsync(context)).AsString;

            var status = await CommonSensorTagSetupAsync(serviceString, characteristicString, periodString, periodByte, configString, configByte, callback, functionName, context, name, ArgList, Retval);
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

            var configByte = (byte)(await ArgList[0].EvalAsync(context)).AsDouble;
            var periodByte = (byte)(await ArgList[1].EvalAsync(context)).AsDouble;
            string functionName = (await ArgList[2].EvalAsync(context)).AsString;

            var status = await CommonSensorTagSetupAsync(serviceString, characteristicString, periodString, periodByte, configString, configByte, callback, functionName, context, name, ArgList, Retval);
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


        private async Task<RunResult.RunStatus> MagnetometerSetup(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            var serviceString = MagnetometerService;
            string characteristicString = MagnetometerData;
            string configString = MagnetometerConfig;
            string periodString = MagnetometerPeriod;
            Windows.Foundation.TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> callback = Magnetometer_ValueChanged; //NOTE: why is this todo here?

            var configByte = (byte)(await ArgList[0].EvalAsync(context)).AsDouble;
            var periodByte = (byte)(await ArgList[1].EvalAsync(context)).AsDouble;
            string functionName = (await ArgList[2].EvalAsync(context)).AsString;

            var status = await CommonSensorTagSetupAsync(serviceString, characteristicString, periodString, periodByte, configString, configByte, callback, functionName, context, name, ArgList, Retval);
            return status;
        }

        private void Magnetometer_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            if (Device.AllGattCallbackData.ContainsKey(sender))
            {
                var callbackData = Device.AllGattCallbackData[sender];
                BCRunContext context = callbackData.Context;


                if (args.CharacteristicValue.Length == 6)
                {
                    uint xLSB = (uint)(byte)args.CharacteristicValue.GetByte(0);
                    int xMSB = (int)(sbyte)args.CharacteristicValue.GetByte(1);
                    uint yLSB = (uint)(byte)args.CharacteristicValue.GetByte(2);
                    int yMSB = (int)(sbyte)args.CharacteristicValue.GetByte(3);
                    uint zLSB = (uint)(byte)args.CharacteristicValue.GetByte(4);
                    int zMSB = (int)(sbyte)args.CharacteristicValue.GetByte(5);

                    double x = ((xMSB << 8) + xLSB);
                    double y = ((yMSB << 8) + yLSB);
                    double z = ((zMSB << 8) + zLSB);

                    x = x * 2000.0 / 65536.0 * -1;
                    y = y * 2000.0 / 65536.0 * -1;
                    z = z * 2000.0 / 65536.0;

                    foreach (var functionName in callbackData.FunctionNameList.ToList()) // Duplicate the list to prevent race conditions
                    {
                        var arglist = new List<IExpression>() { new ObjectConstant(this), new NumericConstant(x), new NumericConstant(y), new NumericConstant(z) };
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
                    if (!await BCObjectUtilities.CheckArgValue(0, "onoff", 0, 1, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
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

                case "GyroSetup":
                case "GyroscopeSetup":
                    // 0 top 7; bits correspond to which axis is desired. 7==all on
                    // period (100 ms to 2.55 seconds)
                    // name of function to call on change
                    if (!BCObjectUtilities.CheckArgs(3, 3, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(0, "onoff", 0, 7, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(1, "period", 10, 255, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await GyroscopeSetup(context, name, ArgList, Retval);

                case "HumSetup":
                case "HumiditySetup":
                    // 0 or 1 (on/off)
                    // period [units of 10 ms; range is 100 ms to 2.55 seconds]
                    // name of function to call on change
                    if (!BCObjectUtilities.CheckArgs(3, 3, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(0, "onoff", 0, 1, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(1, "period", 10, 255, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await HumiditySetup(context, name, ArgList, Retval);

                case "IRSetup":
                    // 0 or 1 (on/off)
                    // period
                    // name of function to call on change
                    if (!BCObjectUtilities.CheckArgs(3, 3, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(0, "onoff", 0, 1, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(1, "period", 0, 255, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await IRSetup(context, name, ArgList, Retval);

                case "MagSetup":
                case "MagnetometerSetup":
                    // 0 or 1 (on/off)
                    // period [units of 10 ms; range is 100 ms to 2.55 seconds]
                    // name of function to call on change
                    if (!BCObjectUtilities.CheckArgs(3, 3, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(0, "onoff", 0, 1, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(1, "period", 10, 255, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await MagnetometerSetup(context, name, ArgList, Retval);

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

        public async void Dispose()
        {
            //NOTE: doesn't this duplicate what's already being done?

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
