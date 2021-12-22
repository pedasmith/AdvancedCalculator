using BCBasic;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AdvancedCalculator.BCBasic
{
    public class MathValues : IObjectValue
    {
        public string PreferredName
        {
            get { return "Math"; }
        }

        public BCValue GetValue(string name)
        {
            switch (name)
            {
                case "E": return new BCValue(Math.E);
                case "Infinity": return new BCValue(Double.PositiveInfinity);
                case "NaN": return new BCValue(Double.NaN);
                case "NegativeInfinity": return new BCValue(Double.NegativeInfinity);
                case "PositiveInfinity": return new BCValue(Double.NegativeInfinity);
                case "PI": return new BCValue(Math.PI);
            }
            return BCValue.MakeNoSuchProperty(this, name, double.NaN);
        }

        public void SetValue(string name, BCValue value)
        {
            ; // Can't ever set a constant.
        }

        public IList<string> GetNames()
        {
            return new List<string>() { "E", "NaN", "PI" };
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
