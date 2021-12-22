using BCBasic;
using BCBasic.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedCalculator.BCBasic.RunTimeLibrary
{
    public enum AnalysisType { R, G, B,   }
    public class CameraAnalysis : IObjectValue
    {
        public string PreferredName { get { return "Analysis"; } }
        public BCValue GetValue(string name)
        {
            switch (name)
            {
                case "Function":
                    return new BCValue(FunctionName); 
                case "Methods":
                    return new BCValue("AddPoint,ToString"); 
                case "Name":
                    return new BCValue(PreferredName);
            }
            return BCValue.MakeNoSuchProperty(this, name);
        }
        public void SetValue(string name, BCValue value)
        {
            switch (name)
            {
                case "Radius":
                    AnalysisRadius = value.AsDouble;
                    break;
                case "AnalysisH": // e.g. 64 pixels
                    AnalysisH = (int)value.AsDouble;
                    break;
                case "AnalysisW":
                    AnalysisW = (int)value.AsDouble;
                    break;
                case "CX":
                    AnalysisCX= (int)value.AsDouble;
                    break;
                case "CY":
                    AnalysisCY = (int)value.AsDouble;
                    break;
                case "Function":
                    FunctionName = value.AsString;
                    break;
                case "Image":
                case "View":
                    var graphics = value.AsObject;
                    if (graphics is GraphicsPrimitiveImage)
                    {
                        var image = graphics as GraphicsPrimitiveImage;
                        Image = image;
                    }
                    break;

            }
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
        }
        public async Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            switch (name)
            {
                case "AddPoint":
                    if (!BCObjectUtilities.CheckArgs(3, 15, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;

                    var analysisTypeString = (await ArgList[0].EvalAsync(context)).AsString;

                    var analysisType = AnalysisTypeFromString(analysisTypeString); // Sets the analysis type
                    if (!RGBInterpolation.ContainsKey(analysisType)) RGBInterpolation.Add(analysisType, new InterpolationLibrary());
                    for (int i=1; i<ArgList.Count-1; i+=2)
                    {
                        var x = (await ArgList[i].EvalAsync(context)).AsDouble;
                        var y = (await ArgList[i+1].EvalAsync(context)).AsDouble;
                        RGBInterpolation[analysisType].AddPoint(x, y);
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

        Dictionary<AnalysisType, InterpolationLibrary> RGBInterpolation = new Dictionary<AnalysisType, InterpolationLibrary>();
        public InterpolationLibrary GetInterpolationLibrary(AnalysisType analysisType, InterpolationLibrary defaultValue = null)
        {
            if (RGBInterpolation.ContainsKey (analysisType))
            {
                return RGBInterpolation[analysisType];
            }
            return defaultValue;
        }
        public void Dispose()
        {
        }

        private static AnalysisType AnalysisTypeFromString(string userOption)
        {
            AnalysisType Retval = AnalysisType.R;
            switch (userOption)
            {
                case "R": Retval = AnalysisType.R; break;
                //case "R+": Retval = AnalysisType.RPlus; break;
                case "G": Retval = AnalysisType.G; break;
                case "B": Retval = AnalysisType.B; break;
                    //case "RGB": AnalysisType = AnalysisType.RGB; break;
            }
            return Retval;
        }

        public bool FromString(string userOption)
        {
            AnalysisType = AnalysisTypeFromString(userOption);
            return true;
        }
        public AnalysisType AnalysisType;
        public double AnalysisCX { get; set; } = 0.5;
        public double AnalysisCY { get; set; } = 0.5;
        public double AnalysisRadius { get; set; } = 0.16667; // analysis area is 1/3 of the screen each way;

        public int AnalysisW { get; set; } = 64;
        public int AnalysisH { get; set; } = 32;


        public void CallFunction(double r, double g, double b)
        {
            // It's OK to not have a function name.
            if (FunctionName == null || FunctionName == "") return;

            var arglist = new List<IExpression>() { new NumericConstant(r), new NumericConstant(g), new NumericConstant(b) };
            Context.ProgramRunContext.AddCallback(Context, FunctionName, arglist);
        }
        public String FunctionName;
        public BCRunContext Context;

        public GraphicsPrimitiveImage Image;
    }
}
