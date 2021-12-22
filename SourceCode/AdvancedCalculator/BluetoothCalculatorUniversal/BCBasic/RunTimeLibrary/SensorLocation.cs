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
using Windows.Foundation;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Pickers;


namespace AdvancedCalculator.BCBasic.RunTimeLibrary
{
    public class RTLSensorLocation : IObjectValue
    {
        private string Name = "(location)";
        private int ActionCount { get; set; } = 0;

        public string PreferredName
        {
            get { return "Location"; }
        }

        public BCValue GetValue(string name)
        {
            switch (name)
            {
                case "Methods":
                    return new BCValue("Start,Stop,ToString");
                case "Name":
                    return new BCValue(Name);
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

        Geolocator geolocator = null;
        BCRunContext Context;
        string FunctionName = "";

        private async Task<string> Setup()
        {
            var accessStatus = await Geolocator.RequestAccessAsync();
            switch (accessStatus)
            {
                case GeolocationAccessStatus.Allowed:
                    geolocator = new Geolocator { DesiredAccuracyInMeters = 1 };
                    geolocator.PositionChanged += Geolocator_PositionChanged;
                    break;
                case GeolocationAccessStatus.Denied:
                case GeolocationAccessStatus.Unspecified:
                    RTLSystemX.AddError($"Location: access status {accessStatus}");
                    return "Error: location access was denied";
            }
            return null;
        }

        private void Geolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            Name = args.Position.Coordinate.PositionSource.ToString();
            var accuracy = args.Position.Coordinate.Accuracy;
            var latitude = args.Position.Coordinate.Point.Position.Latitude;
            var longitude = args.Position.Coordinate.Point.Position.Longitude;
            var altitude = args.Position.Coordinate.Point.Position.Altitude;

            var arglist = new List<IExpression>() { new NumericConstant (latitude), new NumericConstant(longitude), new NumericConstant (altitude), new NumericConstant (accuracy) };
            Context.ProgramRunContext.AddCallback(new CallbackData(Context, FunctionName, arglist));
        }


        public async Task StartAsync(BCRunContext context, string fname, BCValue Retval)
        {
            FunctionName = fname;
            Context = context;

            Stop();
            var result = await Setup();
            if (result != null) Retval.SetError(1, result);
        }

        private void Stop()
        {
            if (geolocator != null)
            {
                geolocator.PositionChanged -= Geolocator_PositionChanged;
            }
            geolocator = null;
        }

        public async Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            switch (name)
            {
                case "Start":
                    if (!BCObjectUtilities.CheckArgs(1, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    var fname = (await ArgList[0].EvalAsync(context)).AsString;
                    await StartAsync(context, fname, Retval);
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
