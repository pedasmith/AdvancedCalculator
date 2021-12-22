using BCBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedCalculator.Bluetooth.Devices
{
    //
    // A handy class for devices (like Ardudroid) where the device
    // is Bluetooth Rfcomm-derived and uses a command/response pattern.
    // This is not appropriate for, e.g., the Infineon DPS310 where the
    // device can return pretty arbitrary data.
    //
    // It's good for, e.g. the Ardudroid where everything is 
    // command/response, command/response.
    //
    // Devices often have an initial string sent which is the Banner
    //
    abstract class AbstractRfcommCommandResponse: IObjectValue
    {
        //
        // All of the methods for IObjectValue
        //
        public virtual string PreferredName { get; } = "AnRfcommDevice";
        public virtual IList<string> GetNames() { return new List<string>() { "Banner,Name" }; }
        public BCValue GetValue(string name)
        {
            switch (name)
            {
                case "Banner": return new BCValue(Banner);
                default:
                    if (Rfcomm != null)
                    {
                        return Rfcomm.GetValue(name);
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
            if (Rfcomm != null)
            {
                Rfcomm.RunComplete();
            }
        }

        public void SetValue(string name, BCValue value)
        {
            // Do nothing; cannot set any value.
        }


        public virtual async Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            switch (name)
            {
                case "Write":
                    if (!BCObjectUtilities.CheckArgs(1, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    var v1 = (await ArgList[0].EvalAsync(context)).AsString;
                    var n  = await SendCommandAsync (v1); 
                    return RunResult.RunStatus.OK;

                default:
                    await Task.Delay(0);
                    BCValue.MakeNoSuchMethod(Retval, this, name);
                    return RunResult.RunStatus.ErrorContinue;
            }
        }

        public void Dispose()
        {
            if (Rfcomm != null)
            {
                Rfcomm.Dispose();
                Rfcomm = null;
            }
        }


        //
        // All the parts that make this a Rfcomm Command/Response device
        //

        public BluetoothRfcommDevice Rfcomm { get; internal set; }
        public AbstractRfcommCommandResponse (BluetoothRfcommDevice rfcomm)
        {
            Rfcomm = rfcomm;
            Rfcomm.ValueChanged = RfcommValueChanged;
        }

        private void RfcommValueChanged (string str)
        {
            if (NCommand == 0) Banner += str;
            CurrResponse += str;
        }

        public string Banner { get; internal set; } = "";
        public string CurrResponse { get; internal set; } = "";
        public List<string> ResponseHistory { get; } = new List<string>();
        int NCommand = 0;
        public async Task<int> SendCommandAsync(string command)
        {
            NCommand++;
            ResponseHistory.Add(CurrResponse);
            CurrResponse = "";
            int status = await Rfcomm.WriteStringAsync(command);
            return status;
        }
    }
}
