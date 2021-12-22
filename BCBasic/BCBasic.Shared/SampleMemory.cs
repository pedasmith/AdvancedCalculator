using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

namespace BCBasic
{
    // Simple persistant roaming memory scheme.
    public class SampleMemory : IObjectValue, IHaveFunctions
    {
        public SampleMemory()
        {
            var roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;

            Values = roamingSettings.CreateContainer("Memory", ApplicationDataCreateDisposition.Always);

            functions.Add("GetOrDefault", new GetOrDefault(this));
            functions.Add("IsSet", new IsSet(this));
        }
        public string PreferredName
        {
            get { return "Memory"; }
        }

        ApplicationDataContainer Values = null;



        public BCValue GetValue(string name)
        {
            if (Values.Values.ContainsKey(name))
            {
                var d = Values.Values[name] as Double?;
                if (d.HasValue) return new BCValue(d.Value);
            }
            return new BCValue(); // Default is a no such value.
        }

        public void SetValue(string name, BCValue value)
        {
            Values.Values[name] = value.AsDouble;
        }

        public IList<string> GetNames()
        {
            var Retval = new List<string>();
            foreach (var key in Values.Values.Keys)
            {
                Retval.Add(key);
            }
            return Retval;
        }

        public void InitializeForRun()
        {
        }

        public void RunComplete()
        {
        }

        class GetOrDefault : IFunction
        {
            public GetOrDefault(SampleMemory parent)
            {
                Parent = parent;
            }
            SampleMemory Parent;
            public async Task<BCValue> EvalAsync(BCRunContext context, IList<IExpression> argList)
            {
                var name = (await argList[0].EvalAsync(context)).AsString;
                var Retval = Parent.GetValue(name);
                if (Retval.CurrentType == BCValue.ValueType.IsNoValue)
                {
                    Retval = await argList[1].EvalAsync(context);
                }
                return Retval;
            }
        }

        class IsSet : IFunction
        {
            public IsSet(SampleMemory parent)
            {
                Parent = parent;
            }
            SampleMemory Parent = null;
            public async Task<BCValue> EvalAsync(BCRunContext context, IList<IExpression> argList)
            {
                var name = (await argList[0].EvalAsync(context)).AsString;
                var value = Parent.GetValue (name);

                BCValue Retval = new BCValue();
                Retval.AsDouble = value.CurrentType == BCValue.ValueType.IsNoValue ? 0 : 1;

                return Retval;
            }
        }

        Dictionary<string, IFunction> functions = new Dictionary<string, IFunction>();

        public Dictionary<string, IFunction> GetFunctions()
        {
            return functions;
        }
        // IObjectValue can also call a function by name
        public async Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            switch (name)
            {
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
