using BCBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace AdvancedCalculator.BCBasic.RunTimeLibrary
{
    public class RTLSensor : IObjectValue
    {
        public string PreferredName
        {
            get { return "Sensor"; }
        }

        public BCValue GetValue(string name)
        {
            switch (name)
            {
                case "Methods":
                    return new BCValue("Camera,Compass,Light,Location,Microphone,ToString");
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

        List<IObjectValue> AllCreatedObjects = new List<IObjectValue>();
        public void RunComplete()
        {
            foreach (var item in AllCreatedObjects)
            {
                item.RunComplete();
            }
        }

        public async Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            string callbackFunctionName = null;
            switch (name)
            {
                case "Camera":
                    if (!BCObjectUtilities.CheckArgs(0, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    var cameraType = "default";
                    if (ArgList.Count >= 1)
                    {
                        cameraType = (await ArgList[0].EvalAsync(context)).AsString;
                    }
                    var camera = new RTLSensorCamera(cameraType);
                    Retval.AsObject = camera;
                    AllCreatedObjects.Add(Retval.AsObject);
                    return RunResult.RunStatus.OK;
                case "Compass":
                    if (!BCObjectUtilities.CheckArgs(0, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (ArgList.Count >= 1) callbackFunctionName = (await ArgList[0].EvalAsync(context)).AsString;
                    var compass = new RTLSensorCompass();
                    if (callbackFunctionName != null) compass.Start(context, callbackFunctionName);
                    Retval.AsObject = compass;
                    AllCreatedObjects.Add(Retval.AsObject);
                    return RunResult.RunStatus.OK;
                case "Inclinometer":
                    if (!BCObjectUtilities.CheckArgs(0, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (ArgList.Count >= 1) callbackFunctionName = (await ArgList[0].EvalAsync(context)).AsString;
                    var inclinometer = new RTLSensorInclinometer();
                    if (callbackFunctionName != null) inclinometer.Start(context, callbackFunctionName);
                    Retval.AsObject = inclinometer;
                    AllCreatedObjects.Add(Retval.AsObject);
                    return RunResult.RunStatus.OK;
                case "Light":
                    if (!BCObjectUtilities.CheckArgs(0, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (ArgList.Count >= 1) callbackFunctionName = (await ArgList[0].EvalAsync(context)).AsString;
                    var light = new RTLSensorLight();
                    if (callbackFunctionName != null) light.Start(context, callbackFunctionName);
                    Retval.AsObject = light;
                    AllCreatedObjects.Add(Retval.AsObject);
                    return RunResult.RunStatus.OK;
                case "Location":
                    if (!BCObjectUtilities.CheckArgs(0, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (ArgList.Count >= 1) callbackFunctionName = (await ArgList[0].EvalAsync(context)).AsString;
                    var location = new RTLSensorLocation();
                    if (callbackFunctionName != null) await location.StartAsync (context, callbackFunctionName, Retval);
                    Retval.AsObject = location;
                    AllCreatedObjects.Add(Retval.AsObject);
                    return RunResult.RunStatus.OK;
                case "Microphone":
                    if (!BCObjectUtilities.CheckArgs(0, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (ArgList.Count >= 1) callbackFunctionName = (await ArgList[0].EvalAsync(context)).AsString;
                    var microphone = new RTLSensorMicrophone();
                    if (callbackFunctionName != null) await microphone.DoStartAsync(context, callbackFunctionName, Retval);
                    Retval.AsObject = microphone;
                    AllCreatedObjects.Add(Retval.AsObject);
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
