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
    class Hexiwear : IObjectValue
    {
        BluetoothLEDevice Device = null;
        public Hexiwear(BluetoothLEDevice device)
        {
            Device = device;
        }

        public string PreferredName { get { return "Hexiwear"; } }

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

#if NEVER_EVER_DEFINED
        private async Task<RunResult.RunStatus> CommonDottiCall(byte cmd1, byte cmd2, BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
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
#endif

        private async Task<RunResult.RunStatus> ReadXYZValue(string serviceString, string characteristicsString, string name, double divisor, BCValue Retval)
        {
            BCValue data = new BCValue();
            var status = await BluetoothDevice.ReadBytesAsync(Device, serviceString, characteristicsString, BluetoothCacheMode.Uncached, data);
            Retval.SetNoValue();
            if (status == RunResult.RunStatus.OK && data.AsObject != null && data.AsObject is BCValueList)
            {
                var rawlist = data.AsObject as BCValueList;
                int nitem = 3; 
                int byteperitem = rawlist.data.Count / nitem;
                var type = byteperitem == 2 ? "int6-le" : "uint8";

                //byteperitem = 1; // TODO: handle the Magnetometer which returns 6 bytes but only the first 3 are data
                // TODO: the above is a guess
                //divisor = 1; // TODO: switch to no dividing....
                if (rawlist.data.Count == 6 || rawlist.data.Count == 3)
                {
                    var list = new BCValueList("XYZ", new List<string>() { "X", "Y", "Z" });
                    list.data.Add(new BCValue(rawlist.GetValue(1 * byteperitem + 1, type) / divisor));
                    list.data.Add(new BCValue(rawlist.GetValue(3 * byteperitem + 1, type) / divisor));
                    list.data.Add(new BCValue(rawlist.GetValue(5 * byteperitem + 1, type) / divisor));
                    Retval.AsObject = list;
                }
            }
            if (Retval.CurrentType == BCValue.ValueType.IsNoValue)
            {
                Retval.AsString = $"ERROR: unable to read {name}";
            }

            return RunResult.RunStatus.OK;
        }

        // Returns TRUE if a heading could be added, FALSE otherwise.
        private bool AddHeading(BCValue Retval)
        {
            // Retval.AsObject must be a BCValueList with XY values.  Heading is added.
            if (Retval.CurrentType != BCValue.ValueType.IsObject) return false;
            var list = Retval.AsObject as BCValueList;
            if (list == null) return false;
            var names = list.GetNames();
            if (!names.Contains("X") && !names.Contains("Y")) return false;
            double x = list.GetValue("X").AsDouble;
            double y = list.GetValue("Y").AsDouble;
            double heading = double.NaN;
            if (y > 0)
            {
                //  -[arcTAN(x/y)]*180/¹
                //var angle = Math.Atan2(x, y) * 180 / Math.PI;
                var angle = Math.Atan(x/y) * 180 / Math.PI;
                heading = 90 - angle;
            }
            else if (y < 0)
            {
                //var angle = Math.Atan2(x, y) * 180 / Math.PI;
                var angle = Math.Atan(x / y) * 180 / Math.PI;
                heading = 270 - angle;
            }
            else if (y == 0 && x < 0)
            {
                heading = 180;
            }
            else if (y == 0 && x > 0)
            {
                heading = 0;
            }
            else // y==0 and x==0 OR they are NaN values
            {
                heading = double.NaN;
            }
            list.SetProperty("Heading", new BCValue(heading));

            return true;
        }

        private async Task<RunResult.RunStatus> ReadValue(string serviceString, string characteristicsString, string name, double divisor, BCValue Retval)
        {
            BCValue data = new BCValue();
            var status = await BluetoothDevice.ReadBytesAsync(Device, serviceString, characteristicsString, BluetoothCacheMode.Uncached, data);
            Retval.SetNoValue();
            if (status == RunResult.RunStatus.OK && data.AsObject != null && data.AsObject is BCValueList)
            {
                var rawlist = data.AsObject as BCValueList;
                double value = 0;
                if (rawlist.data.Count == 1)
                {
                    value = rawlist.data[0].AsDouble / divisor;
                }
                else if (rawlist.data.Count == 2)
                {
                    value = rawlist.GetValue(1, "int16-le") / divisor;
                }
                Retval.AsDouble = value;
            }
            if (Retval.CurrentType == BCValue.ValueType.IsNoValue)
            {
                Retval.AsString = $"ERROR: unable to read {name}";
            }

            return RunResult.RunStatus.OK;
        }


        public async Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            switch (name)
            {
                // Standard calls that are propertly supported by the device.

                // The Get() functions are non-cached
                case "GetName":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await BluetoothDevice.ReadString(Device, "1800", "2a00", BluetoothCacheMode.Uncached, Retval);

                case "GetManufacturerName":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await BluetoothDevice.ReadString(Device, "180a", "2a29", BluetoothCacheMode.Uncached, Retval);

                /*
                case "GetHardwareRevision":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await BluetoothDevice.ReadString(Device, "180a", "2a27", BluetoothCacheMode.Uncached, Retval);
                 */

                case "GetFirmwareRevision":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await BluetoothDevice.ReadString(Device, "180a", "2a26", BluetoothCacheMode.Uncached, Retval);

                case "GetPower":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await BluetoothDevice.ReadByte(Device, "180f", "2a19", BluetoothCacheMode.Uncached, Retval);

                // These are all of the device specific calls
                // Motion server 0x2000
                case "GetAccelerometer":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await ReadXYZValue("2000", "2001", "accelerometer", 100.0, Retval);

                case "GetGyroscope": // return is degrees/second
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await ReadXYZValue("2000", "2002", "gyroscope", 1.0, Retval);

                case "GetMagnetometer":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    var status = await ReadXYZValue("2000", "2003", "magnetometer", 100.0, Retval);
                    AddHeading(Retval);
                    return status;


                // Weather service 0x2010
                case "GetLight": // Returns percent 0..100
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await ReadValue("2010", "2011", "light", 1.0, Retval);

                case "GetTemperature": 
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await ReadValue("2010", "2012", "temperature", 100.0, Retval);

                case "GetHumidity": 
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await ReadValue("2010", "2013", "humidity", 100.0, Retval);

                case "GetPressure": //TODO: says it's a reading in pascal multiplied by 100.  
                    // Use a divisor of 100 until I figure it out.
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await ReadValue("2010", "2014", "pressure", 1.0, Retval);

                // Health service 0x2020
                case "GetHeart": 
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await ReadValue("2020", "2021", "heart", 1.0, Retval);

                case "GetSteps":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await ReadValue("2020", "2022", "steps", 1.0, Retval);

                case "GetCalories":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await ReadValue("2020", "2023", "calorie", 1.0, Retval);


                // Alert/Command Service 0x2030
                //TODO: need an app for these
                case "SetNotificationCount":
                    {
                        // 2=missed call 4=social 6=email
                        if (!BCObjectUtilities.CheckArgs(2, 2, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        byte category = (byte)((await ArgList[0].EvalAsync(context)).AsDouble);
                        if (category != 2 && category != 4 && category != 6)
                        {
                            Retval.AsString = $"Error: SetNotificationCount() first arg must be 2 4 or 6, not {category}";
                            return RunResult.RunStatus.ErrorStop;
                        }
                        byte count = (byte)((await ArgList[0].EvalAsync(context)).AsDouble);
                        byte[] buffer = new byte[3];
                        buffer[0] = 1; // type = notification
                        buffer[1] = category;
                        buffer[2] = count;
                        return await BluetoothDevice.DoWriteBytes(Device, "2030", "2031", buffer, Retval);
                    }

                    // NOTE: the only documented item we're missing is reading 0x2032 to read in notifications from the Hexiwear.

                case "SetTimeNow":
                    {
                        // TODO: verify datetime changes
                        if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;

                        Int32 unixTimestamp = (Int32)(DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                        byte[] buffer = new byte[20];
                        buffer[0] = 3;
                        buffer[1] = 4;
                        // Data is little-endian (which isn't documented)
                        buffer[5] = (byte)((unixTimestamp >> 24) & 0xFF);
                        buffer[4] = (byte)((unixTimestamp >> 16) & 0xFF);
                        buffer[3] = (byte)((unixTimestamp >>  8) & 0xFF);
                        buffer[2] = (byte)((unixTimestamp >>  0) & 0xFF);
                        // NOTE: should adjust the value by time zone
                        return await BluetoothDevice.DoWriteBytes(Device, "2030", "2031", buffer, Retval);
                    }

                // Application mode service 0x2040
                case "GetMode": // 0=idle 2=sensor tag 5=heart 6=pedometer (steps+calorie)
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await ReadValue("2040", "2041", "mode", 1.0, Retval);

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
