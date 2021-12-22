using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BCBasic
{
    public class BCFunctionRecord
    {
        public BCFunctionRecord(string name, NamedFunctionAsync f, int minArgs, int maxArgs)
        {
            Name = name;
            FunctionAsync = f;
            MinArgs = minArgs;
            MaxArgs = maxArgs;
        }
        public string Name;
        public delegate Task<bool> NamedFunctionAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval);
        public NamedFunctionAsync FunctionAsync;
        public int MinArgs;
        public int MaxArgs;
    }
    public class BCFunctionList
    {
        private static SortedDictionary<string, BCFunctionRecord> FunctionList = new SortedDictionary<string, BCFunctionRecord>()
        {
            // ASC is the same as CODE (ASC is the common name; CODE is the Sinclair name)
            { "ASC", new BCFunctionRecord ("CODE", BCBasicStringFunctions.CODEAsync, 1, 1) },
            { "CHR", new BCFunctionRecord ("CHR", BCBasicStringFunctions.CHRAsync, 0, Int32.MaxValue) },
            { "CHR$", new BCFunctionRecord ("CHR$", BCBasicStringFunctions.CHRAsync, 1, 1) },
            { "CODE", new BCFunctionRecord ("CODE", BCBasicStringFunctions.CODEAsync, 1, 1) },
            { "LEFT", new BCFunctionRecord ("LEFT", BCBasicStringFunctions.LeftAsync, 2, 2) },
            { "MID", new BCFunctionRecord ("MID", BCBasicStringFunctions.MidAsync, 2, 3) },
            { "RIGHT", new BCFunctionRecord ("RIGHT", BCBasicStringFunctions.RightAsync, 2, 2) },
            { "RND", new BCFunctionRecord ("RND", BCBasicMathFunctions.RndAsync, 0, 1) },
            { "SPC", new BCFunctionRecord ("SPC", BCBasicStringFunctions.SPCAsync, 1, 1) },

            // I'm really tired of having to type ACS (etc) instead of ACOS
            { "ACOS", new BCFunctionRecord ("Math.Acos", BCBasic.BCBasicMathFunctions.AcosAsync, 1, 1) },
            { "ASIN", new BCFunctionRecord ("Math.Asin", BCBasic.BCBasicMathFunctions.AsinAsync, 1, 1) },
            { "ATAN", new BCFunctionRecord ("Math.Atan", BCBasic.BCBasicMathFunctions.AtanAsync, 1, 1) },
            { "ATAN2", new BCFunctionRecord ("Math.Atan2", BCBasic.BCBasicMathFunctions.Atan2Async, 2, 2) }, 

            // Some nice bitwise operators
            { "Math.BitAnd", new BCFunctionRecord ("Math.BitAnd", BCBasic.BCBasicMathFunctions.BitAndAsync, 2, 2) },
            { "Math.BitNot", new BCFunctionRecord ("Math.BitNot", BCBasic.BCBasicMathFunctions.BitNotAsync, 1, 1) },
            { "Math.BitOr", new BCFunctionRecord ("Math.BitOr", BCBasic.BCBasicMathFunctions.BitOrAsync, 2, 2) },
            { "Math.BitXor", new BCFunctionRecord ("Math.BitXor", BCBasic.BCBasicMathFunctions.BitXorAsync, 2, 2) },

            { "Math.DtoR", new BCFunctionRecord ("Math.DtoR", BCBasic.BCBasicMathFunctions.DtoRAsync, 1, 1) },
            { "Math.DMStoR", new BCFunctionRecord ("Math.DMStoR", BCBasic.BCBasicMathFunctions.DMStoRAsync, 1, 3) },
            { "Math.Fft", new BCFunctionRecord ("Math.Fft", BCBasic.BCBasicMathFunctions.FftAsync, 1, 1) },
            { "Math.InverseFft", new BCFunctionRecord ("Math.InverseFft", BCBasic.BCBasicMathFunctions.InverseFftAsync, 1, 1) },
            { "Math.IsNaN", new BCFunctionRecord ("Math.IsNan", BCBasic.BCBasicMathFunctions.IsNaNAsync, 1, 1) },
            { "Math.Log2", new BCFunctionRecord ("Math.Log2", BCBasic.BCBasicMathFunctions.Log2Async, 1, 1) }, 
            { "Math.RtoD", new BCFunctionRecord ("Math.RtoD", BCBasic.BCBasicMathFunctions.RtoDAsync, 1, 1) }, 

            { "Math.Abs", new BCFunctionRecord ("Math.Abs", BCBasic.BCBasicMathFunctions.AbsAsync, 1, 1) }, 
            { "Math.Acos", new BCFunctionRecord ("Math.Acos", BCBasic.BCBasicMathFunctions.AcosAsync, 1, 1) }, 
            { "Math.Asin", new BCFunctionRecord ("Math.Asin", BCBasic.BCBasicMathFunctions.AsinAsync, 1, 1) }, 
            { "Math.Atan", new BCFunctionRecord ("Math.Atan", BCBasic.BCBasicMathFunctions.AtanAsync, 1, 1) }, 
            { "Math.Atan2", new BCFunctionRecord ("Math.Atan2", BCBasic.BCBasicMathFunctions.Atan2Async, 2, 2) }, // Takes two args
            { "Math.Ceiling", new BCFunctionRecord ("Math.Ceiling", BCBasic.BCBasicMathFunctions.CeilingAsync, 1, 1) }, 
            { "Math.Cos", new BCFunctionRecord ("Math.Cos", BCBasic.BCBasicMathFunctions.CosAsync, 1, 1) }, 
            { "Math.Cosh", new BCFunctionRecord ("Math.Cosh", BCBasic.BCBasicMathFunctions.CoshAsync, 1, 1) }, 
            { "Math.Exp", new BCFunctionRecord ("Math.Exp", BCBasic.BCBasicMathFunctions.ExpAsync, 1, 1) },
            { "Math.Factorial", new BCFunctionRecord ("Math.Factorial", BCBasic.BCBasicMathFunctions.FactorialAsync, 1, 1) },
            { "Math.Floor", new BCFunctionRecord ("Math.Floor", BCBasic.BCBasicMathFunctions.FloorAsync, 1, 1) },
            { "Math.Frac", new BCFunctionRecord ("Math.Frac", BCBasic.BCBasicMathFunctions.FracAsync, 1, 1) },
            { "Math.Log", new BCFunctionRecord ("Math.Log", BCBasic.BCBasicMathFunctions.LogAsync, 1, 2) }, 
            { "Math.Log10", new BCFunctionRecord ("Math.Log10", BCBasic.BCBasicMathFunctions.Log10Async, 1, 1) }, 
            { "Math.Max", new BCFunctionRecord ("Math.Max", BCBasic.BCBasicMathFunctions.MaxAsync, 1, Int32.MaxValue) }, // actual function can handle any number of paramters
            { "Math.Min", new BCFunctionRecord ("Math.Min", BCBasic.BCBasicMathFunctions.MinAsync, 1, Int32.MaxValue) }, // actual function can handle any number of paramters
            { "Math.Mod", new BCFunctionRecord ("Math.Mod", BCBasic.BCBasicMathFunctions.ModAsync, 2, 2) },
            { "Math.Pow", new BCFunctionRecord ("Math.Pow", BCBasic.BCBasicMathFunctions.PowAsync, 1, 2) },  // Takes two args
            { "Math.Round", new BCFunctionRecord ("Math.Round", BCBasic.BCBasicMathFunctions.RoundAsync, 1, 2) },
            { "Math.Sigma", new BCFunctionRecord ("Math.Sigma", BCBasic.BCBasicMathFunctions.SigmaAsync, 1, 1) },
            { "Math.Sign", new BCFunctionRecord ("Math.Sign", BCBasic.BCBasicMathFunctions.SignAsync, 1, 1) },
            { "Math.Sin", new BCFunctionRecord ("Math.Sin", BCBasic.BCBasicMathFunctions.SinAsync, 1, 1) },
            { "Math.Sinh", new BCFunctionRecord ("Math.Sinh", BCBasic.BCBasicMathFunctions.SinhAsync, 1, 1) },
            { "Math.Sqrt", new BCFunctionRecord ("Math.Sqrt", BCBasic.BCBasicMathFunctions.SqrtAsync, 1, 1) },
            { "Math.Tan", new BCFunctionRecord ("Math.Tan", BCBasic.BCBasicMathFunctions.TanAsync, 1, 1) }, 
            { "Math.Tanh", new BCFunctionRecord ("Math.Tanh", BCBasic.BCBasicMathFunctions.TanhAsync, 1, 1) }, 
            { "Math.Truncate", new BCFunctionRecord ("Math.Truncate", BCBasic.BCBasicMathFunctions.TruncateAsync, 1, 1) }, 
        };

        public static void AddFunction (BCFunctionRecord record)
        {
            FunctionList.Add(record.Name, record);
        }

        public enum RunStatus {  NotFound, Success, WrongArgCount }
        public static async Task<RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            if (FunctionList.ContainsKey(name))
            {
                var record = FunctionList[name];
                var narg = ArgList.Count;
                if (narg < record.MinArgs || narg > record.MaxArgs)
                {
                    // Future: nicer output? -- print out the args, distinguish too few and too many args, and special case min==max.
                    context.ExternalConnections.WriteLine(string.Format ("Error: {0}() must be called with {1} to {2} arguments", name, record.MinArgs, record.MaxArgs));
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
