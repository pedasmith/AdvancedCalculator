using BCBasic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedCalculator.Bluetooth
{
    public class ObjectList : IObjectValue
    {
        public ObservableCollection<IObjectValue> data = new ObservableCollection<IObjectValue>();
        public ObjectList()
        {
            PreferredName = "ObjectList";
        }

        public string PreferredName { get; set; }

        public IList<string> GetNames() { return new List<string>() { "Count" }; }

        public BCValue GetValue(string name)
        {
            switch (name)
            {
                case "Count":
                    return new BCValue(data.Count);
            }
            return BCValue.MakeNoSuchProperty(this, name);
        }

        public void InitializeForRun()
        {
        }

        public void RunComplete()
        {
        }

        public void SetValue(string name, BCValue value)
        {
            // Do nothing; cannot set any value.
        }

        public async Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            switch (name)
            {
                case "Get":
                    // An ObjectList can only be a 1-D list of objects (like Bluetooth devices).
                    // It will never be 2-D.
                    // BUT the second might be a blank string, which is OK
                    if (ArgList.Count != 1)
                    {
                        Retval.AsString = $"{PreferredName}.Get() expected 1 argument, got {ArgList.Count}";
                        return RunResult.RunStatus.ErrorStop;
                    }
                    var idx = (int)(await ArgList[0].EvalAsync(context)).AsDouble;
                    if (idx < 1 || idx > data.Count)
                    {
                        Retval.AsString = $"{PreferredName}.Get({idx}) index must be between 1 and {data.Count}";
                        return RunResult.RunStatus.ErrorStop;
                    }
                    Retval.AsObject = data[idx-1]; // idx is 1-based but data is zero-based.
                    return RunResult.RunStatus.OK;

                case "ToString":
                    Retval.AsString = $"{PreferredName} Length={data.Count}";
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
