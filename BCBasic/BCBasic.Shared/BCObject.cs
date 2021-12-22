using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static BCBasic.BCFunctionList;

namespace BCBasic
{
    public interface IBCObject
    {
        Task<RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval);
    }

    public class BCObjectUtilities
    {
        public static bool CheckArgs(int min, int max, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            if (ArgList.Count < min)
            {
                if (min == max)
                    Retval.SetError(1, $"{name} requires {min} args, not {ArgList.Count}.");
                else
                    Retval.SetError(1, $"{name} requires {min} to {max} args, not {ArgList.Count}.");
                return false;
            }
            if (ArgList.Count > max)
            {
                if (min == max)
                    Retval.SetError(1, $"{name} requires {min} args, not {ArgList.Count}.");
                else
                    Retval.SetError(1, $"{name} requires {min} to {max} args, not {ArgList.Count}.");
                return false;
            }
            return true;
        }

        public static async Task<bool> CheckArgValue(int index, string argName, int min, int max, BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            if (index >= ArgList.Count)
            {
                Retval.SetError(1, $"{name} is missing the {argName} argument.");
                return false;
            }
            double value = (await ArgList[index].EvalAsync(context)).AsDouble;
            return CheckValueRange(name, value, argName, min, max, Retval);
        }
        // name and argName are used only for error messages
        public static bool CheckValueRange(string name, double value, string argName, double min, double max, BCValue Retval)
        {
            if (!Double.IsNaN(min) && value < min)
            {
                Retval.SetError(1, $"{name} {argName} argument is {value} but must be >= {min}");
                return false;
            }
            if (!Double.IsNaN(max) && value > max)
            {
                Retval.SetError(1, $"{name} {argName} argument is {value} but must be <= {max}");
                return false;
            }
            return true;
        }
    }
    public abstract class BCObjectSimpleBase : IBCObject
    {
        private static SortedDictionary<string, BCFunctionRecord> FunctionList = new SortedDictionary<string, BCFunctionRecord>()
        { };

        public static async Task<bool> ToSTring(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            await Task.Delay(0);
            Retval.AsString = "(invalid value)";
            //Retval.AsDouble = ((await ArgList[0].EvalAsync(context)).AsDouble * 2 * Math.PI) / 360.0;
            return true;
        }

        public static void AddFunction(BCFunctionRecord record)
        {
            FunctionList.Add(record.Name, record);
        }

        public async Task<RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            if (FunctionList.ContainsKey(name))
            {
                var record = FunctionList[name];
                var narg = ArgList.Count;
                if (narg < record.MinArgs || narg > record.MaxArgs)
                {
                    // Future: nicer output? -- print out the args, distinguish too few and too many args, and special case min==max.
                    context.ExternalConnections.WriteLine(string.Format("{0}() must be called with {1} to {2} arguments", name, record.MinArgs, record.MaxArgs));
                    return RunStatus.WrongArgCount;
                }

                var task = record.FunctionAsync(context, ArgList, Retval);
                var status = await task;
                return RunStatus.Success;
            }
            return RunStatus.NotFound;
        }
    }
}
