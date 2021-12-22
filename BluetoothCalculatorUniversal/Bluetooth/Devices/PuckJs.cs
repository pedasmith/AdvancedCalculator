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
    class PuckJs : IObjectValue
    {
        static int NCreate = 0;
        int thisCreateNumber = 0;
        BluetoothDevice Device = null;
        BluetoothLEDevice Ble = null;
        public PuckJs(BluetoothDevice device, BluetoothLEDevice ble)
        {
            Device = device;
            Ble = ble;
            thisCreateNumber = NCreate;
            NCreate++;
        }

        private Guid UartService = new Guid("6e400001-b5a3-f393-e0a9-e50e24dcca9e");
        private Guid UartTx = new Guid ("6e400002-b5a3-f393-e0a9-e50e24dcca9e");
        private Guid UartRx = new Guid ("6e400003-b5a3-f393-e0a9-e50e24dcca9e");

        public string PreferredName { get { return "PuckJs"; } }

        public IList<string> GetNames() { return new List<string>() { "Methods" }; }

        public BCValue GetValue(string name)
        {
            switch (name)
            {
                case "Methods":
                    return new BCValue("GetName,GetPower,Close,RxSetup,RxSetupLine,Tx,ToString");
            }
            return BCValue.MakeNoSuchProperty(this, name);
        }

        public void InitializeForRun()
        {
        }

        List<string> callbackFunctions = new List<string>();
        public void RunComplete()
        {
            if (RxCharacteristic != null)
            {
                // Should also turn off notify, but we can't in a non-Async routine.
                RxCharacteristic.ValueChanged -= Rx_ValueChanged;
            }

            foreach (var item in callbackFunctions)
            {
                Device.RemoveCallback(UartService, UartRx, "");
            }
        }

        public void SetValue(string name, BCValue value)
        {
            // Do nothing; cannot set any value.
        }

        GattCharacteristic RxCharacteristic = null; // Must keep this around so it doesn't get disposed.
        private async Task<RunResult.RunStatus> SetupRxAsync(bool SendData, string functionName, CallbackType callbackType, BCRunContext context, BCValue Retval)
        {
            var name = "Setup UART";

            var service = Ble.GetGattService(UartService);
            var characteristics = service.GetCharacteristics(UartRx);
            
            if (SendData == false)
            {
                return await RemoveRxAsync(functionName, Retval);
            }

            SetCallbackType(functionName, callbackType);
            foreach (var characteristic in characteristics) // Actually only one.
            {
                if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
                {
                    GattCommunicationStatus btstatus = GattCommunicationStatus.Success;

                    if (RxCharacteristic == null)
                    {
                        // Only set up the device callback once.
                        RxCharacteristic = characteristic;
                        btstatus = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                    }

                    callbackFunctions.Add(functionName);
                    Device.AddCallback(UartService, UartRx, context, Rx_ValueChanged, functionName, Retval);

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

        private async Task<RunResult.RunStatus> RemoveRxAsync(string functionName, BCValue Retval)
        {
            var name = "Reset UART";
            if (RxCharacteristic != null)
            {
                // Not quite right?  Beca
                var btstatus = await RxCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
                Device.RemoveCallback(UartService, UartRx, functionName);
                RxCharacteristic = null;

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
            Retval.AsString = $"Error: Unable to {name}";
            return RunResult.RunStatus.ErrorStop;
        }

        enum CallbackType {  EachString, FullLine };
        Dictionary<string, CallbackType> AllCallbackType = new Dictionary<string, CallbackType>();
        private void SetCallbackType (string functionName, CallbackType callbackType)
        {
            AllCallbackType[functionName] = callbackType;
        }
        private CallbackType GetCallbackType(string functionName)
        {
            if (AllCallbackType.ContainsKey(functionName))
            {
                return AllCallbackType[functionName];
            }
            return CallbackType.EachString;
        }
        //NOTE: remove the callback types?


        string strBuffer = "";
        private void Rx_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            if (!Device.AllGattCallbackData.ContainsKey(sender))
            {
                return;
            }

            var callbackData = Device.AllGattCallbackData[sender];
            BCRunContext context = callbackData.Context;

            var dr = DataReader.FromBuffer(args.CharacteristicValue);
            dr.ByteOrder = ByteOrder.LittleEndian;
            var str = dr.ReadString(dr.UnconsumedBufferLength);
            strBuffer = strBuffer + str;
            var strLines = new List<string>();
            var crIndex = strBuffer.IndexOf('\n');
            while (crIndex >= 0)
            {
                var line = strBuffer.Substring(0, crIndex+1);
                strLines.Add(line);

                strBuffer = strBuffer.Substring(crIndex + 1);
                crIndex = strBuffer.IndexOf('\n');
            }

            foreach (var functionName in callbackData.FunctionNameList)
            { 
                var callbackType = GetCallbackType(functionName);
                if (callbackType == CallbackType.EachString)
                {
                    var arglist = new List<IExpression>() { new ObjectConstant(this), new StringConstant(str), };
                    context.ProgramRunContext.AddCallback(context, functionName, arglist);

                    //await CalculatorConnectionControl.BasicFrameworkElement.Dispatcher.RunAsync(
                    //    Windows.UI.Core.CoreDispatcherPriority.Normal, 
                    //    async () =>
                    //    {
                    //        try
                    //        {
                    //            await context.Functions[functionName].EvalAsync(context, arglist);
                    //        }
                    //        catch (Exception)
                    //        {
                    //            ;
                    //        }
                    //    });
                }
                else
                {
                    foreach (var line in strLines)
                    {
                        var arglist = new List<IExpression>() { new ObjectConstant(this), new StringConstant(line), };
                        context.ProgramRunContext.AddCallback(context, functionName, arglist);
                        //await CalculatorConnectionControl.BasicFrameworkElement.Dispatcher.RunAsync(
                        //    Windows.UI.Core.CoreDispatcherPriority.Normal,
                        //    async () => { await context.Functions[functionName].EvalAsync(context, arglist); }
                        //    );

                    }
                }
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

                case "RxSetup":
                case "RxSetupLine":
                    // name of function to call on change
                    if (!BCObjectUtilities.CheckArgs(2, 2, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(0, "setup", 0, 1, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;

                    var setup = (await ArgList[0].EvalAsync(context)).AsDouble;
                    var functionName = (await ArgList[1].EvalAsync(context)).AsString;
                    var storedData = (name.EndsWith("Line")) ? CallbackType.FullLine : CallbackType.EachString;
                    return await SetupRxAsync(setup==0 ? false : true, functionName, storedData, context, Retval);

                case "Tx":

                    if (!BCObjectUtilities.CheckArgs(1, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    var txValue = (await ArgList[0].EvalAsync(context)).AsString;
                    RunResult.RunStatus iostatus = RunResult.RunStatus.OK;
                    var bufferList = Utilities.BluetoothUtilities.TextToByteArray(txValue);
                    foreach (var textBuffer in bufferList)
                    {
                        iostatus = await BluetoothDevice.DoWriteBytes(Ble, UartService, UartTx, textBuffer, Retval);
                    }
                    return iostatus;


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
            if (Ble != null)
            {
                Ble.Dispose();
                Ble = null;
            }
        }
    }
}
