using BCBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedCalculator.Bluetooth.Devices
{
    class Ardudroid : AbstractRfcommCommandResponse, IObjectValue
    {
        //
        // Critical overrides for the IObjectValue
        //
        public new string PreferredName { get; } = "Ardudroid";
        public override IList<string> GetNames() { var baselist = base.GetNames(); return new List<string> { "Methods" }.Concat(baselist).ToList();  }

        //
        // All the things that make this an Adrudroid and not
        // some other device.
        //
        public Ardudroid(BluetoothRfcommDevice rfcomm)
            :base(rfcomm)
        {

        }
        public async Task DigitalWrite(int pin, int value)
        {
            await SendCommandAsync($"*10 {pin} {value}\n");
        }

        public async Task AnalogWrite(int pin, int value)
        {
            await SendCommandAsync($"*11 {pin} {value}\n");
        }

        public async Task<string> Read(int pin, int value)
        {
            string Retval = "";
            await SendCommandAsync($"*13 {pin} {value}\n");

            // This loop will keep on reading in as long as there
            // is more to be read.
            while (CurrResponse == "" || CurrResponse != Retval)
            {
                Retval = CurrResponse;
                await Task.Delay(50);
            }
            Retval = CurrResponse;
            return Retval;
        }

        public async Task ServoAttach(int servo, int pin)
        {
            await SendCommandAsync($"*14 {servo} {pin}\n");
        }

        public async Task ServoMove(int servo, int position)
        {
            await SendCommandAsync($"*15 {servo} {position}\n");
        }

        public override async Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            int v1;
            int v2;

            switch (name)
            {
                case "AnalogWrite":
                    if (!BCObjectUtilities.CheckArgs(2, 2, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    v1 = (int)(await ArgList[0].EvalAsync(context)).AsDouble;
                    v2 = (int)(await ArgList[1].EvalAsync(context)).AsDouble;
                    await DigitalWrite(v1, v2);
                    return RunResult.RunStatus.OK;

                case "DigitalWrite":
                    if (!BCObjectUtilities.CheckArgs(2, 2, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    v1 = (int)(await ArgList[0].EvalAsync(context)).AsDouble;
                    v2 = (int)(await ArgList[1].EvalAsync(context)).AsDouble;
                    await DigitalWrite(v1, v2);
                    return RunResult.RunStatus.OK;

                case "Read":
                    if (!BCObjectUtilities.CheckArgs(2, 2, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    v1 = (int)(await ArgList[0].EvalAsync(context)).AsDouble;
                    v2 = (int)(await ArgList[1].EvalAsync(context)).AsDouble;
                    var result = await Read(v1, v2);
                    Retval.AsString = result;
                    return RunResult.RunStatus.OK;

                case "ServoAttach":
                    if (!BCObjectUtilities.CheckArgs(2, 2, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    v1 = (int)(await ArgList[0].EvalAsync(context)).AsDouble;
                    v2 = (int)(await ArgList[1].EvalAsync(context)).AsDouble;
                    await ServoAttach(v1, v2);
                    return RunResult.RunStatus.OK;

                case "ServoMove":
                    if (!BCObjectUtilities.CheckArgs(2, 2, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    v1 = (int)(await ArgList[0].EvalAsync(context)).AsDouble;
                    v2 = (int)(await ArgList[1].EvalAsync(context)).AsDouble;
                    await ServoMove(v1, v2);
                    return RunResult.RunStatus.OK;

                default:
                    return await base.RunAsync(context, name, ArgList, Retval);
            }
        }

    }
}
