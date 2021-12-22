using BCBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedCalculator.Bluetooth.Devices
{
    // http://sunriseprogrammer.blogspot.com/2017/07/infineon-sensor-hubfiguring-it-out.html
    class Infineon_DPS310 : IObjectValue
    {
        // NOTE: override the function callback on the BluetoothRfcommDevice
        // NOTE: initialize the device
        // NOTE: read in data and call user's callback. 
        BluetoothRfcommDevice Device = null;
        BCRunContext Context;
        String AltitudeFunction = "";
        String PressureFunction = "";
        String TemperatureFunction = "";
        Task InitTask = null;
        public Infineon_DPS310(BluetoothRfcommDevice device, BCRunContext context, string altitudeFunction, string pressureFunction, string temperatureFunction)
        {
            Device = device;
            AltitudeFunction = altitudeFunction;
            PressureFunction = pressureFunction;
            TemperatureFunction = temperatureFunction;
            Context = context;
            device.ValueChanged = ValueChanged;
            if (AltitudeFunction != "" || PressureFunction != "" || TemperatureFunction != "")
            {
                InitTask = InitDps310Async();
            }
        }

        public virtual string PreferredName { get { return "DPS310"; } }

        public IList<string> GetNames() { return new List<string>() { "Methods" }; }

        public BCValue GetValue(string name)
        {
            switch (name)
            {
                case "Methods":
                    return new BCValue("Close,Init,ToString");
            }
            return BCValue.MakeNoSuchProperty(this, name);
        }

        public void InitializeForRun()
        {
        }

        public void RunComplete()
        {
            Device?.RemoveCallback("");
        }

        public void SetValue(string name, BCValue value)
        {
            // Do nothing; cannot set any value.
        }

        private async Task InitDps310Async()
        {
            await Device.WriteStringAsync("$hello\n");
            await Device.WriteStringAsync("$set_mode sid=1;md=mode;val=bg\n");
            await Device.WriteStringAsync("$set_mode sid=1;md=prs_osr;val=16\n");
            await Device.WriteStringAsync("$set_mode sid=1;md=prs_mr;val=32\n");
            await Device.WriteStringAsync("$start_sensor id=1\n");
        }

        string valuePrevious = "";
        char[] CR = new char[] { '\n' };
        char[] COMMA = new char[] { ',' };
        private void ValueChanged(string str)
        {
            str = str + valuePrevious;
            valuePrevious = "";
            var lines = str.Split(CR);
            foreach (var line in lines)
            {
                if (line.StartsWith("$1"))
                {
                    // Example: $1,t,35.0619,285286 
                    // BUT the data might be lumped together into one giant string.
                    var fields = line.Split(COMMA);
                    if (fields.Length == 4)
                    {
                        var type = fields[1];
                        var valuestr = fields[2];
                        double value = 0.0;
                        bool ok = double.TryParse(valuestr, out value);
                        if (ok)
                        {
                            if (Context != null)
                            {
                                switch (type)
                                {
                                    case "a":
                                        if (AltitudeFunction != "")
                                        {
                                            var arglist = new List<IExpression>() {
                                                new ObjectConstant(this),
                                                new NumericConstant(value)
                                            };
                                            Context.ProgramRunContext.AddCallback(Context, AltitudeFunction, arglist);
                                        }
                                        break;
                                    case "p":
                                        if (PressureFunction != "")
                                        {
                                            var arglist = new List<IExpression>() {
                                                new ObjectConstant(this),
                                                new NumericConstant(value)
                                            };
                                            Context.ProgramRunContext.AddCallback(Context, PressureFunction, arglist);
                                        }
                                        break;
                                    case "t":
                                        if (TemperatureFunction != "")
                                        {
                                            var arglist = new List<IExpression>() {
                                                new ObjectConstant(this),
                                                new NumericConstant(value)
                                            };
                                            Context.ProgramRunContext.AddCallback(Context, TemperatureFunction, arglist);
                                        }
                                        break;
                                }
                            }
                        }
                    }
                    else if (fields.Length < 4)
                    {
                        valuePrevious = line; // this is the left-over bit that's an incomplete line. 
                        // We will pick up the rest on the next callback.
                    }
                }
            }
        }

        public async Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            switch (name)
            {
                case "Close":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    Dispose();
                    return RunResult.RunStatus.OK;

                // These are all of the device specific calls

                case "Init":
                    if (!BCObjectUtilities.CheckArgs(1, 3, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    AltitudeFunction = (await ArgList[0].EvalAsync(context)).AsString; // Function to call with pressure, temp, alt. data
                    PressureFunction = ArgList.Count > 1 ? (await ArgList[1].EvalAsync(context)).AsString : ""; // Function to call with pressure, temp, alt. data
                    TemperatureFunction = ArgList.Count > 1 ? (await ArgList[2].EvalAsync(context)).AsString : ""; // Function to call with pressure, temp, alt. data
                    Context = context;
                    await InitDps310Async();

                    return RunResult.RunStatus.OK;



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
            Device.AllRfcommCallbackData.Clear();

            if (Device != null)
            {
                Device.Dispose();
                Device = null;
            }
        }


    }
}
