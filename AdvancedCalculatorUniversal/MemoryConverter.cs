using BCBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace AdvancedCalculator
{
    //NOTE: don't use IHaveFunctions
    public class MemoryConverter: IObjectValue, IHaveFunctions
    {
        public MemoryConverter()
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

        public SimpleCalculator simpleCalculator = null; // This includes a Memory object with calculator memory values (0..9)
        ApplicationDataContainer Values = null;


        enum IndexType { ValidInteger, InvalidNumber, String };
        private IndexType GetIndexType(string name)
        {
            double didx;
            bool isNumber = Double.TryParse(name, out didx);
            if (isNumber)
            {
                int idx;
                bool isInteger = Int32.TryParse (name, out idx); // Just in case there are any numbers which parse as a double but not as an integer.
                if (isInteger && (double)idx == didx && idx >= 0 && idx < 100) // 100 is the documented largest index value.
                {
                    if (idx < simpleCalculator.MemoryMaxIndex)
                    {
                        return IndexType.ValidInteger; // ValidInteger means it's actually in the memory cell range.
                    }
                    else
                    {
                        return IndexType.String; 
                        // Stuff that's too big for calculator memory will be stored in the Values memory.
                    }
                }
                return IndexType.InvalidNumber;
            }
            return IndexType.String;
        }

        public BCValue GetValue(string name)
        {
            IndexType idxtype = GetIndexType(name);
            int idx;
            string str = null;
            switch (idxtype)
            {
                case IndexType.ValidInteger:
                    idx = Int32.Parse(name);
                    str = simpleCalculator.GetMemoryAt(idx, null);
                    break;

                case IndexType.String:
                    // Prefer the named values in the calculators memory. Otherwise use the values
                    // that we save seperately.
                    idx = simpleCalculator.MemoryNameIndex(name);
                    if (idx >= 0)
                    {
                        str = simpleCalculator.GetMemoryAt(idx, null);
                    }
                    else if (Values.Values.ContainsKey(name))
                    {
                        var d = Values.Values[name] as Double?;
                        if (d.HasValue) return new BCValue(d.Value);
                        var strvalue = Values.Values[name].ToString();
                        return new BCValue(strvalue);
                    }
                    break;
            }

            if (str != null)  // Found something!
            {
                double d = Double.NaN;
                bool ok = Double.TryParse(str, out d);
                if (ok)
                {
                    return new BCValue(d);
                }
                return new BCValue(str); 
            }
            return new BCValue(); // Default is a no such value.
        }

        public void SetValue(string name, BCValue value)
        {
            IndexType idxtype = GetIndexType(name);
            int idx;
            //string str = null;
            switch (idxtype)
            {
                case IndexType.ValidInteger:
                    idx = Int32.Parse(name);
                    simpleCalculator.MemorySet(idx, value.AsDouble);
                    break;

                case IndexType.String:
                    // Prefer the named values in the calculators memory. Otherwise use the values
                    // that we save seperately.
                    idx = simpleCalculator.MemoryNameIndex(name);
                    if (idx >= 0)
                    {
                        if (value.CurrentType == BCValue.ValueType.IsDouble)
                        {
                            simpleCalculator.MemorySet(idx, value.AsDouble);
                        }
                        else
                        {
                            simpleCalculator.MemorySet(idx, value.AsString);
                        }
                    }
                    else
                    {
                        if (value.CurrentType == BCValue.ValueType.IsDouble)
                        {
                            Values.Values[name] = value.AsDouble; // can just set.
                        }
                        else
                        {
                            Values.Values[name] = value.AsString;
                        }
                    }
                    break;
            }
        }

        public IList<string> GetNames()
        {
            var Retval = new List<string>();
            foreach (var key in simpleCalculator.MemoryNames)
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

    //
    // Classes to implement the memory external functions
    //
    class GetOrDefault : IFunction
    {
        public GetOrDefault(MemoryConverter parent)
        {
            Parent = parent;
        }
        MemoryConverter Parent;
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
        public IsSet(MemoryConverter parent)
        {
            Parent = parent;
        }
        MemoryConverter Parent = null;
        public async Task<BCValue> EvalAsync(BCRunContext context, IList<IExpression> argList)
        {
            var name = (await argList[0].EvalAsync(context)).AsString;
            var value = Parent.GetValue(name);

            BCValue Retval = new BCValue();
            Retval.AsDouble = value.CurrentType == BCValue.ValueType.IsNoValue ? 0 : 1;

            return Retval;
        }
    }
}
