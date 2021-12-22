using BCBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

namespace AdvancedCalculator.Bluetooth
{
    // Implements the "Watch" command which looks for 
    // Bluetooth advertisements and calls callbacks.
    class BluetoothWatchAdvertisements
    {
        enum AdvertisementType { None, Bluetooth, Eddystone, EddystoneUrl, RuuviTag };
        static AdvertisementType Convert(string watchFor)
        {
            switch (watchFor)
            {
                case "Bluetooth": return AdvertisementType.Bluetooth;
                case "Eddystone": return AdvertisementType.Eddystone;
                case "Eddystone-URL": return AdvertisementType.EddystoneUrl;
                case "RuuviTag": return AdvertisementType.RuuviTag;
            }
            return BluetoothWatchAdvertisements.AdvertisementType.None;
        }

        class CallbackData
        {
            public CallbackData(AdvertisementType watchFor, string functionName, BCRunContext context)
            {
                WatchFor = watchFor;
                FunctionName = functionName;
                Context = context;
            }
            public AdvertisementType WatchFor;
            public string FunctionName;
            public BCRunContext Context;
        }
        Dictionary<AdvertisementType, List<CallbackData>> AllCallbacks = new Dictionary<AdvertisementType, List<CallbackData>>();
        private bool AmWatchingFor (AdvertisementType watchFor)
        {
            var Retval = AllCallbacks.ContainsKey(watchFor);
            return Retval;
        }
        public void Add (string watchFor, string functionName, BCRunContext context)
        {
            var wf = Convert(watchFor);
            if (wf == AdvertisementType.None) return; 

            var cbd = new CallbackData(wf, functionName, context);
            if (!AllCallbacks.ContainsKey(wf)) AllCallbacks[wf] = new List<CallbackData>();
            AllCallbacks[wf].Add(cbd);
        }

        BluetoothLEAdvertisementWatcher Watcher = null;
        public void Start()
        {
            if (Watcher == null)
            {
                Watcher = new BluetoothLEAdvertisementWatcher();
                Watcher.Received += Watcher_Received;
                Watcher.Start();
            }
        }

        BCValueList DataSections;
        private List<AdvertisementType> ParseArgs(BluetoothLEAdvertisementReceivedEventArgs args)
        {
            List<AdvertisementType> foundValues = new List<AdvertisementType>();
            foundValues.Add(AdvertisementType.Bluetooth);

            DataSections = new BCValueList();
            foreach (var section in args.Advertisement.DataSections)
            {
                if (AmWatchingFor(AdvertisementType.Bluetooth))
                {
                    // Let's not go through the trouble of building this
                    // if there's no callback for it.
                    var row = new BCValueList();
                    DataSections.data.Add(new BCValue(row));

                    row.data.Add(new BCValue((double)section.DataType));
                    foreach (var b in section.Data.ToArray())
                    {
                        row.data.Add(new BCValue((double)b));
                    }
                }
                switch (section.DataType)
                {
                    case 0x16: // 22=service data
                        var dr = DataReader.FromBuffer(section.Data);
                        dr.ByteOrder = ByteOrder.LittleEndian;
                        var Service = dr.ReadUInt16();
                        // https://github.com/google/eddystone
                        if (Service == 0xFEAA) // An Eddystone type
                        {
                            foundValues.Add(AdvertisementType.Eddystone);

                            //EddystoneFrameType = (byte)(0x0F & (dr.ReadByte() >> 4));
                            EddystoneFrameType = dr.ReadByte();
                            switch (EddystoneFrameType)
                            {
                                case 0x10: // An Eddystone-URL
                                    // https://github.com/google/eddystone/tree/master/eddystone-url
                                    ParseEddystoneUrlArgs(section.Data);
                                    foundValues.Add(AdvertisementType.EddystoneUrl);

                                    if (EddystoneUrl.StartsWith("https://ruu.vi/#"))
                                    {
                                        foundValues.Add(AdvertisementType.RuuviTag);
                                        ParseRuuviTag(EddystoneUrl);
                                    }
                                    break;
                            }
                        }
                        break;
                }
            }
            return foundValues;
        }
        byte EddystoneFrameType;
        int EddystonePower;
        string EddystoneUrl;
        public bool ParseEddystoneUrlArgs(IBuffer buffer)
        {
            var dr = DataReader.FromBuffer(buffer);
            dr.ByteOrder = ByteOrder.LittleEndian;
            var service = dr.ReadUInt16();

            var ft = dr.ReadByte(); //  (byte)(0x0F & (dr.ReadByte() >> 4));
            if (ft != 0x10)
            {
                // Only frame type 0x10 is allowed for Eddystone URL
                return false;
            }
            EddystoneFrameType = ft;

            EddystonePower = (int)(sbyte)dr.ReadByte();
            var UrlScheme = dr.ReadByte();
            var urlBuilder = new StringBuilder();
            switch (UrlScheme)
            {
                case 0: urlBuilder.Append("http://www."); break;
                case 1: urlBuilder.Append("https://www."); break;
                case 2: urlBuilder.Append("http://"); break;
                case 3: urlBuilder.Append("https://"); break;
                default:
                    // Invalid url scheme
                    return false;
            }
            while (dr.UnconsumedBufferLength > 0)
            {
                var b = dr.ReadByte();
                if (b >= 0 && b <= 13)
                {
                    switch (b)
                    {
                        case 0: urlBuilder.Append(".com/"); break;
                        case 1: urlBuilder.Append(".org/"); break;
                        case 2: urlBuilder.Append(".edu/"); break;
                        case 3: urlBuilder.Append(".net/"); break;
                        case 4: urlBuilder.Append(".info/"); break;
                        case 5: urlBuilder.Append(".biz/"); break;
                        case 6: urlBuilder.Append(".gov/"); break;
                        case 7: urlBuilder.Append(".com"); break;
                        case 8: urlBuilder.Append(".org"); break;
                        case 9: urlBuilder.Append(".edu"); break;
                        case 10: urlBuilder.Append(".net"); break;
                        case 11: urlBuilder.Append(".info"); break;
                        case 12: urlBuilder.Append(".biz"); break;
                        case 13: urlBuilder.Append(".gov"); break;
                    }
                }
                else if (b >= 14 && b <= 32)
                {
                    return false; // reserved for future use
                }
                else if (b >= 127 && b <= 255)
                {
                    return false; // reserved for future use
                }
                else
                {
                    urlBuilder.Append((char)b);
                }
            }
            EddystoneUrl = urlBuilder.ToString();
            return true; // Everything workewd
        }


        double RuuviTemperature;
        double RuuviPressure;
        double RuuviHumidity;

        void ParseRuuviTag(string url)
        {
            RuuviTemperature = double.NaN;
            RuuviPressure = double.NaN;
            RuuviHumidity = double.NaN;
            var hi = url.IndexOf('#');
            if (hi < 0) return;
            var str = url.Substring(hi + 1);
            var ruuviData = System.Convert.FromBase64String(str);

            // https://github.com/ruuvi/ruuvi-sensor-protocols
            var dr = DataReader.FromBuffer(ruuviData.AsBuffer());
            dr.ByteOrder = ByteOrder.BigEndian;
            var format = dr.ReadByte();
            switch (format)
            {
                case 2:
                case 4:
                    RuuviHumidity = dr.ReadByte() / 2;
                    var temp = dr.ReadByte();
                    var tempFraction = dr.ReadByte();
                    var tsigned = (temp & 0x80) != 0;
                    if (tsigned)
                    {
                        temp = (byte)(temp & 0x7F);
                        RuuviTemperature = -1.0 * (temp & 0x7F) + (double)tempFraction / 100.0;
                    }
                    else
                    {
                        RuuviTemperature = (double)temp + (double)tempFraction / 100.0;
                    }

                    RuuviPressure = (double)(dr.ReadUInt16() + 50000) / 100.0;  // Convert to hPA

                    byte tag = 0;
                    //bool gotTag = false;
                    if (dr.UnconsumedBufferLength >= 1)
                    {
                        tag = dr.ReadByte();
                        //gotTag = true; // NOTE: actually use the Tag and gotTag values?
                    }
                    break;
            }
        }

        private void Watcher_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            var foundAdvertisementTypes = ParseArgs(args);
            var addr = args.BluetoothAddress;

            foreach (var type in foundAdvertisementTypes)
            {
                if (!AllCallbacks.ContainsKey(type)) continue;

                var callbackList = AllCallbacks[type];
                foreach (var item in callbackList)
                {
                    var context = item.Context;
                    var functionName = item.FunctionName;
                    List<IExpression> arglist = null;
                    switch (type)
                    {
                        case AdvertisementType.Bluetooth:
                            arglist = new List<IExpression>() { new StringConstant (Bluetooth.AddressToString(addr)),
                                new NumericConstant (args.RawSignalStrengthInDBm),
                                new ObjectConstant (DataSections),
                            };
                            break;
                        case AdvertisementType.Eddystone:
                            arglist = new List<IExpression>() { new StringConstant (Bluetooth.AddressToString(addr)),
                                new NumericConstant (args.RawSignalStrengthInDBm),
                                new NumericConstant (EddystonePower),
                                new NumericConstant (EddystoneFrameType),
                            };
                            break;
                        case AdvertisementType.EddystoneUrl:
                            arglist = new List<IExpression>() { new StringConstant (Bluetooth.AddressToString(addr)),
                                new NumericConstant (args.RawSignalStrengthInDBm),
                                new NumericConstant (EddystonePower),
                                new StringConstant (EddystoneUrl),
                            };
                            break;
                        case AdvertisementType.RuuviTag:
                            arglist = new List<IExpression>() { new StringConstant (Bluetooth.AddressToString(addr)),
                                new NumericConstant (args.RawSignalStrengthInDBm),
                                new NumericConstant (EddystonePower),
                                new NumericConstant (RuuviTemperature),
                                new NumericConstant (RuuviPressure),
                                new NumericConstant (RuuviHumidity),
                            };
                            break;
                    }
                    if (arglist != null)
                    {
                        context.ProgramRunContext.AddCallback(context, functionName, arglist);
                    }
                }
            }
        }

        public void Stop()
        {
            if (Watcher == null) return;
            Watcher.Stop();
            Watcher.Received -= Watcher_Received;
            Watcher = null;
            AllCallbacks.Clear();
        }
    }
}
