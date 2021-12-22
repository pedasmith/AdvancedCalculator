using AdvancedCalculator.BCBasic.RunTimeLibrary;
using BCBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace AdvancedCalculator.Bluetooth
{
    class BluetoothRfcommDevice : IBluetoothDevice, IObjectValue
    {
        public Bluetooth Bluetooth { get; internal set; }
        public DeviceInformation di { get; set; } = null;
        public RfcommDeviceService serviceRfcomm { get; internal set; } = null;
        public StreamSocket socket { get; internal set; } = null;
        public DataWriter dw { get; internal set; } = null;
        private Task readAll { get; set; } = null;
        private CancellationTokenSource cts { get; set; } = null;
        public ValueChangedDelegate ValueChanged { get; set; } = null;

        private string SplitOn { get; set; } = "\r\n";

        private async Task CleanupAsync()
        {
            try
            {
                //await socket?.CancelIOAsync();
                cts.Cancel();
                socket.Dispose();
            }
            catch (Exception)
            {
                ;
            }
            socket = null;
            while (readAll != null) await Task.Delay(50);
            dw.Dispose();
            dw = null;
            serviceRfcomm = null;
        }
        public string UserPickedName
        {
            // Note: combine this into one set of functions in BluetoothDevice?
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
        public BluetoothRfcommDevice(Bluetooth bluetooth, DeviceInformation deviceInformation)
        {
            Bluetooth = bluetooth;
            di = deviceInformation;
            Bluetooth.AddDeviceToComplete(this);
        }

        public string PreferredName { get; } = "BluetoothRfcommDevice";

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
                    break;
            }
            return BCValue.MakeNoSuchProperty(this, name);
        }

        public void InitializeForRun()
        {
        }

        public void RunComplete()
        {
            var task = CleanupAsync();
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
            if (!BCObjectUtilities.CheckArgs(1, 5, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
            var deviceName = (await ArgList[0].EvalAsync(context)).AsString;
            var f1 = (ArgList.Count > 1) ? (await ArgList[1].EvalAsync(context)).AsString : "";
            var f2 = (ArgList.Count > 2) ? (await ArgList[2].EvalAsync(context)).AsString : "";
            var f3 = (ArgList.Count > 3) ? (await ArgList[3].EvalAsync(context)).AsString : "";
            var f4 = (ArgList.Count > 4) ? (await ArgList[4].EvalAsync(context)).AsString : "";
            switch (deviceName)
            {
                case "Ardudroid": Retval.AsObject = new Devices.Ardudroid (this); return RunResult.RunStatus.OK;
                // alt, pressure, temp
                case "DPS310": Retval.AsObject = new Devices.Infineon_DPS310(this, context, f1, f2, f3); return RunResult.RunStatus.OK;
            }
            Retval.SetError(1, $"Error: unknown device type {deviceName}");
            return RunResult.RunStatus.ErrorStop;

        }


        private async Task InitAsync ()
        {
            if (di == null) return;
            if (serviceRfcomm == null)
            {
                serviceRfcomm = await RfcommDeviceService.FromIdAsync(di.Id);
            }
            if (serviceRfcomm == null) return;
            if (socket == null)
            {
                socket = new StreamSocket();
                try
                {
                    await socket.ConnectAsync(serviceRfcomm.ConnectionHostName, serviceRfcomm.ConnectionServiceName);
                }
                catch (Exception ex)
                {
                    RTLSystemX.AddError($"Exception connecting to Rfcomm/Spp. Exception {ex.Message}");
                    socket = null;
                }
            }
            if (socket == null) return;
            //
            // DO THE READING AND WRITING
            //
            if (cts == null)
            {
                cts = new CancellationTokenSource();
            }
            if (dw == null)
            {
                dw = new DataWriter(socket.OutputStream);
            }
            if (readAll == null)
            {
                readAll = Task.Run(async () =>
                {
                    var ct = cts.Token;
                    var dr = new DataReader(socket.InputStream);
                    dr.InputStreamOptions = InputStreamOptions.Partial;
                    const int READBUFFER = 2000;
                    bool keepGoing = true;
                    while (!ct.IsCancellationRequested && keepGoing)
                    {
                        uint n = 0;
                        try
                        {
                            n = await dr.LoadAsync(READBUFFER).AsTask(ct);
                        }
                        catch (TaskCanceledException)
                        {
                            n = 0;
                            keepGoing = false;
                        }
                        catch (Exception ex)
                        {
                            RTLSystemX.AddError($"Exception getting data from Rfcomm/Spp. Exception {ex.Message}");
                            n = 0;
                            keepGoing = false;
                        }
                        if (n > 0)
                        {
                            // Got some data from the device
                            var str = dr.ReadString(dr.UnconsumedBufferLength);
                            if (ValueChanged != null) ValueChanged(str);
                            else StandardValueChanged(str);
                    }
                    }
                    readAll = null; // The task is going away; null it out.
            });
            }
        }

        public RunResult.RunStatus RemoveCallback(string functionName)
        {
            foreach (var item in AllRfcommCallbackData)
            {
                // Remove it from the callback!
                if (functionName == "")
                {
                    item.FunctionNameList.Clear();
                }
                else if (item.FunctionNameList.Contains(functionName))
                {
                    item.FunctionNameList.Remove(functionName);
                }
            }

            return RunResult.RunStatus.OK;
        }

        public class RfcommCallbackData
        {
            public RfcommCallbackData(BCRunContext context, string functionName)
            {
                Context = context;
                FunctionNameList.Add(functionName);
            }

            public BCRunContext Context;
            public IList<string> FunctionNameList = new List<String>();
        }

        public List<RfcommCallbackData> AllRfcommCallbackData = new List<RfcommCallbackData>();
        public delegate void ValueChangedDelegate(string str);

        private string currString = "";
        private void StandardValueChanged(string str)
        {
            // Might do callbacks and might not based on the string.
            if (SplitOn == "")
            {
                CallbackOnString(str);
                return;
            }

            var stringArray = new string[1] { SplitOn };
            currString = currString + str;
            var idx = currString.IndexOf(SplitOn);
            while (idx >= 0)
            {
                var list = currString.Split(stringArray, 2, StringSplitOptions.None);
                CallbackOnString(list[0]);
                if (list.Length > 1)
                {
                    idx = -1;
                    currString = "";
                    // // // currString = list[1];
                    // // // idx = currString.IndexOf(SplitOn);
                }
            }
        }

        private void CallbackOnString(string str)
        {

            foreach (var callbackData in AllRfcommCallbackData)
            {
                BCRunContext context = callbackData.Context;
                foreach (var functionName in callbackData.FunctionNameList)
                {
                    var arglist = new List<IExpression>() { new ObjectConstant(this), new StringConstant(str) };
                    context.ProgramRunContext.AddCallback(context, functionName, arglist);
                }
            }
        }


        public async Task<int> WriteStringAsync (string str)
        {
            await InitAsync();
            if (di == null)
            {
                return 2;
            }
            if (serviceRfcomm == null)
            {
                return 3;
            }
            if (socket == null)
            {
                return 4;
            }
            if (dw == null)
            {
                return 5;
            }
            dw.WriteString(str);
            await dw.StoreAsync();
            return 0; // no error
        }

        public void ErrorString (int error, string name, BCValue Retval)
        {
            switch (error)
            {
                case 2:
                    Retval.SetError(2, $"{PreferredName}.{name}: unable to get the bluetooth device");
                    break;
                case 3:
                    Retval.SetError(3, $"{PreferredName}.{name}: unable to get the bluetooth Rfcomm service");
                    break;
                case 4:
                    Retval.SetError(4, $"{PreferredName}.{name}: unable to get the bluetooth Rfcomm socket");
                    break;
                case 5:
                    Retval.SetError(5, $"{PreferredName}.{name}: unable to get the bluetooth Rfcomm writer");
                    break;
            }

        }

        public async Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            switch (name)
            {
                case "As": return await DoAs(context, name, ArgList, Retval);
                case "Send":
                    if (!BCObjectUtilities.CheckArgs(1, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    var str = (await ArgList[0].EvalAsync(context)).AsString;
                    str = str.Replace("\\n", "\n").Replace("\\r", "\r");
                    var status = await WriteStringAsync(str);
                    if (status != 0)
                    {
                        ErrorString(status, name, Retval);
                    }
                    return RunResult.RunStatus.OK;

                case "ReceiveString":
                    if (!BCObjectUtilities.CheckArgs(1, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    await InitAsync();
                    if (di == null)
                    {
                        Retval.SetError(2, $"{PreferredName}.{name}: unable to get the bluetooth device");
                        return RunResult.RunStatus.ErrorStop;
                    }
                    if (serviceRfcomm == null)
                    {
                        Retval.SetError(3, $"{PreferredName}.{name}: unable to get the bluetooth Rfcomm service");
                        return RunResult.RunStatus.ErrorStop;
                    }
                    if (socket == null)
                    {
                        Retval.SetError(4, $"{PreferredName}.{name}: unable to get the bluetooth Rfcomm socket");
                        return RunResult.RunStatus.ErrorStop;
                    }
                    var fname = (await ArgList[0].EvalAsync(context)).AsString;
                    AllRfcommCallbackData.Add(new RfcommCallbackData(context, fname));
                    Retval.AsString = serviceRfcomm.ServiceId.ToString();
                    return RunResult.RunStatus.OK;

                case "ToString":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    Retval.AsString = $"{PreferredName} Name={di.Name}";
                    return RunResult.RunStatus.OK;


                default:
                    await Task.Delay(0);
                    BCValue.MakeNoSuchMethod(Retval, this, name);
                    return RunResult.RunStatus.ErrorContinue;
            }
        }
        public void Dispose()
        {
            if (serviceRfcomm != null)
            {
                serviceRfcomm.Dispose();
            }
        }
    }
}
