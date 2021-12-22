using BCBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;

namespace AdvancedCalculator.Bluetooth
{
#if WINDOWS8
    public class BluetoothWatchAdvertisements
    {
        public void Stop() { }
    }
#endif

    public class Bluetooth : IObjectValue
    {
        BluetoothWatchAdvertisements BWA { get; } = new BluetoothWatchAdvertisements();
        public Bluetooth()
        {
        }

        ObjectList devices = new ObjectList() { PreferredName = "List<BluetoothDevice>" };


        public string PreferredName { get { return "Bluetooth"; } }

        public IList<string> GetNames() { return new List<string>() { "Devices", "DevicesName", "PickDevicesName", "ToString", "Watch" }; }

        public BCValue GetValue(string name)
        {
            return BCValue.MakeNoSuchProperty(this, name);
        }

        public void InitializeForRun()
        {
        }

        public void AddDeviceToComplete(IObjectValue device)
        {
            DevicesToComplete.Add(device);
        }
        private IList<IObjectValue> DevicesToComplete = new List<IObjectValue>();
        public void RunComplete()
        {
            BWA.Stop();
            foreach (var item in DevicesToComplete)
            {
                item.RunComplete();
            }
            DevicesToComplete.Clear();
        }

        public void SetValue(string name, BCValue value)
        {
            // Do nothing; cannot set any value.
        }

        // *Dotti matches Dotti and my-Dotti
        // Meta matches only Meta
        // Star can be at the start or end.
        enum StarPos { None, Begin, End, Both };
        private static bool StarMatch(string pattern, string potential)
        {
            var starPos = StarPos.None;
            if (pattern.StartsWith("*"))
            {
                pattern = pattern.Substring(1);
                starPos = StarPos.Begin;
            }
            if (pattern.EndsWith("*"))
            {
                pattern = pattern.Remove(pattern.Length - 1);
                starPos = (starPos == StarPos.None) ? StarPos.End : StarPos.Both;
            }
            switch (starPos)
            {
                case StarPos.None: return potential == pattern;
                case StarPos.Begin: return potential.EndsWith(pattern);
                case StarPos.End: return potential.StartsWith(pattern);
                case StarPos.Both: return potential.Contains(pattern);
            }
            return false;
        }

        private static bool StarListMatch(string pattern, string potential)
        {
            var patterns = pattern.Split(new char[] { ',' });
            foreach (var onePattern in patterns)
            {
                var Retval = StarMatch(onePattern.Trim(), potential);
                if (Retval == true)
                {
                    return Retval;
                }
            }
            return false;
        }

        // Handles everything PickDevices[Rfcomm|Spp][Name]
        enum BtConnectionType { LE, Rfcomm };
        public async Task<RunResult.RunStatus> DevicesAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            devices.data.Clear();
#if !WINDOWS8
            BtConnectionType connType = BtConnectionType.LE;
            if (name.Contains("Rfcomm")) connType = BtConnectionType.Rfcomm;
            if (name.Contains("Spp")) connType = BtConnectionType.Rfcomm; // Spp is the old name (it's not documented as working)

            var match = "";
            if (name.EndsWith("Name")) // needs to match DevicesName and DeviceRfcommName
            {
                match = (await ArgList[0].EvalAsync(context)).AsString;
            }
            var str = BluetoothLEDevice.GetDeviceSelector();
            if (connType == BtConnectionType.Rfcomm) str = RfcommDeviceService.GetDeviceSelector(RfcommServiceId.SerialPort);
            var list = await DeviceInformation.FindAllAsync(str);
            foreach (var item in list)
            {
                if (match == "" || StarListMatch(match, item.Name))
                {
                    IObjectValue obj = connType == BtConnectionType.LE ? (IObjectValue)new BluetoothDevice(this, item) : (IObjectValue)new BluetoothRfcommDevice(this, item);
                    devices.data.Add(obj);
                }
            }
#else
            await Task.Delay(0);
#endif
            Retval.AsObject = devices;
            return RunResult.RunStatus.OK;
        }


        public async Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            switch (name)
            {
                case "Array":
                    // NOTE: now we just do a DIM statement.
                    {
                        if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var list = new BCValueList();
                        list.ReadOnly = false;
                        Retval.AsObject = list;
                        return RunResult.RunStatus.OK;
                    }

                case "Devices":
                case "DevicesRfcomm":
                case "DevicesSpp":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await DevicesAsync(context, name, ArgList, Retval);

                case "DevicesName": // Bluetooth.DevicesName("Dotti") gets just Dotti devices
                case "DevicesRfcommName": // Bluetooth.DevicesName("Dotti") gets just Dotti devices
                case "DevicesSppName": // Bluetooth.DevicesName("Dotti") gets just Dotti devices
                    if (!BCObjectUtilities.CheckArgs(1, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await DevicesAsync(context, name, ArgList, Retval);


                case "PickDevicesName": // Bluetooth.PickDevicesName("Dotti") gets just Dotti devices and the user can pick one
                case "PickDevicesRfcommName": // Bluetooth.PickDevicesName("Dotti") gets just Dotti devices and the user can pick one
                case "PickDevicesSppName": // Bluetooth.PickDevicesName("Dotti") gets just Dotti devices and the user can pick one
                    {
                        if (!BCObjectUtilities.CheckArgs(1, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var list = new BCValue();
                        var status = await DevicesAsync(context, name, ArgList, list);
#if !WINDOWS8
                        var pd = new PickDevice();
                        status = await pd.DoPickDevice(list.AsObject as ObjectList, Retval);
#endif
                        return status; // RunResult.RunStatus.OK;
                    }

                case "ToString":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    Retval.AsString = $"Object {PreferredName}";
                    return RunResult.RunStatus.OK;

                case "Watch":
                    if (!BCObjectUtilities.CheckArgs(2, 2, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    var watchFor = (await ArgList[0].EvalAsync(context)).AsString;
                    switch (watchFor)
                    {
#if !WINDOWS8
                        case "Bluetooth":
                        case "Eddystone":
                        case "Eddystone-URL":
                        case "RuuviTag":
                            var functionName = (await ArgList[1].EvalAsync(context)).AsString;
                            BWA.Start();
                            BWA.Add(watchFor, functionName, context);
                            return RunResult.RunStatus.OK;
#endif
                        default:
                            Retval.SetError(1, $"Incorrect watchFor value");
                            return RunResult.RunStatus.ErrorStop;
                    }

                default:
                    await Task.Delay(0);
                    BCValue.MakeNoSuchMethod(Retval, this, name);
                    return RunResult.RunStatus.ErrorContinue;
            }
        }

        public void Dispose()
        {
        }

        public static string AddressToString(ulong address)
        {
            var bytes = System.BitConverter.GetBytes(address);
            var Retval = $"{bytes[5]:X02}:{bytes[4]:X02}:{bytes[3]:X02}:{bytes[2]:X02}:{bytes[1]:X02}:{bytes[0]:X02}";
            return Retval;
        }
    }
}
