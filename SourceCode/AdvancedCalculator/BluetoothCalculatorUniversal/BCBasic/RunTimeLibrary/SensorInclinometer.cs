using BCBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Geolocation;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Pickers;


namespace AdvancedCalculator.BCBasic.RunTimeLibrary
{
    public class RTLSensorInclinometer : IObjectValue
    {
        public string PreferredName { get { return "Inclinometer"; } }

        public BCValue GetValue(string name)
        {
            switch (name)
            {
                case "Methods":
                    return new BCValue("Start,Stop,ToString");
                case "Name":
                    return new BCValue(PreferredName);
            }
            return BCValue.MakeNoSuchProperty(this, name);
        }

        public void SetValue(string name, BCValue value)
        {
            ; // Can't ever set a constant.
        }

        public IList<string> GetNames()
        {
            return new List<string>() { "Methods" };
        }

        public void InitializeForRun()
        {
        }

        public void RunComplete()
        {
            Stop();
        }

        Inclinometer sensor = null;
        BCRunContext Context;
        string FunctionName = "";

        private void Sensor_ReadingChanged(Inclinometer sender, InclinometerReadingChangedEventArgs args)
        {
            var arglist = new List<IExpression>() {
                new NumericConstant(args.Reading.PitchDegrees),
                new NumericConstant((int)args.Reading.RollDegrees),
                new NumericConstant(args.Reading.YawDegrees),
                new NumericConstant((int)args.Reading.YawAccuracy),
            };
            Context.ProgramRunContext.AddCallback(new CallbackData(Context, FunctionName, arglist));
        }

        public void Start(BCRunContext context, string fname)
        {
            FunctionName = fname;
            Context = context;

            Stop();
            if (sensor == null)
            {
                sensor = Inclinometer.GetDefault();
            }
            if (sensor == null)
            {
                RTLSystemX.AddError($"Inclinometer: this system has no inclinometer");
            }
            else
            {
                var reportInterval = Math.Max(16, sensor.MinimumReportInterval);
                sensor.ReportInterval = reportInterval;
                sensor.ReadingChanged += Sensor_ReadingChanged;
            }
        }

        private void Stop()
        {
            if (sensor != null)
            {
                sensor.ReadingChanged -= Sensor_ReadingChanged;
            }
            sensor = null;
        }

        public async Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            switch (name)
            {
                case "Start":
                    if (!BCObjectUtilities.CheckArgs(1, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    var fname = (await ArgList[0].EvalAsync(context)).AsString;
                    Start(context, fname);
                    return RunResult.RunStatus.OK;
                case "Stop":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    Stop();
                    return RunResult.RunStatus.OK;
                case "ToString":
                    Retval.AsString = $"Object {PreferredName}";
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
