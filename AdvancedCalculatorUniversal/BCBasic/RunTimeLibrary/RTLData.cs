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
    public class RTLData : IObjectValue
    {
        //private DataLocation DataLocation = null;
        public string PreferredName
        {
            get { return "Data"; }
        }


        public BCValue GetValue(string name)
        {
            switch (name)
            {
                case "Methods":
                    return new BCValue("GetLocations,PickLocation,ToString");
            }
            return BCValue.MakeNoSuchProperty(this, name);
        }

        public void SetValue(string name, BCValue value)
        {
            ; // Can't ever set a constant.
        }

        public IList<string> GetNames()
        {
            return new List<string>() { };
        }

        public void InitializeForRun()
        {
        }

        public void RunComplete()
        {
        }

        public async Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            switch (name)
            {
                case "GetLocations":
                    if (!BCObjectUtilities.CheckArgs(1, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    var cityname = await ArgList[0].EvalAsync(context);
                    Retval.AsObject = await DataLocation.GetLocationsAsync(cityname.AsString);
                    return RunResult.RunStatus.OK;
                case "PickLocation":
                    {
                        var obj = await DataLocation.PickAsync();
                        if (obj != null) Retval.AsObject = obj;
                        else Retval.SetError(1, "No city was selected");
                    }
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
