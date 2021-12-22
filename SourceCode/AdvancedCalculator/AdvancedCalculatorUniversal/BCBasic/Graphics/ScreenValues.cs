using BCBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedCalculator.BCBasic.Graphics
{
    public class ScreenValues : IObjectValue
    {
        CalculatorConnectionControl CalculatorControl;
        public ScreenValues(CalculatorConnectionControl calculatorControl)
        {
            CalculatorControl = calculatorControl;
        }
        public string PreferredName
        {
            get { return "Screen"; }
        }

        public BCValue GetValue(string name)
        {
            switch (name)
            {
                case "GH": return new BCValue(CalculatorControl.Screen.GH);
                case "GW": return new BCValue(CalculatorControl.Screen.GW);
                case "H": return new BCValue(CalculatorControl.Screen.NRows);
                case "W": return new BCValue(CalculatorControl.Screen.NCols);
            }
            return BCValue.MakeNoSuchProperty(this, name);
        }

        public void SetValue(string name, BCValue value)
        {
            ; // Can't set the screen height
        }
        public IList<string> GetNames()
        {
            return new List<string>() { "H", "W", "GH", "GW" };
        }

        public void InitializeForRun()
        {
        }

        public void RunComplete()
        {
            CalculatorControl.RemoveFullScreenWindow();
            while (NRequestActive > 0)
            {
                NRequestActive--;
                if (CurrDisplayRequest == null) CurrDisplayRequest = new Windows.System.Display.DisplayRequest();
                CurrDisplayRequest.RequestRelease();
            }
        }

        public void UpdateGraphics()
        {
            CalculatorControl.Screen.Update();
        }

        int NRequestActive = 0;
        Windows.System.Display.DisplayRequest CurrDisplayRequest = null;

        public async Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            switch (name)
            {
                case "ClearLine":
                    if (!BCObjectUtilities.CheckArgs(1, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(0, "row", 1, 25, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    //var str = "";
                    //for (int i = 0; i < CalculatorControl.Screen.NCols; i++) str += " ";
                    var row = (await ArgList[0].EvalAsync(context)).AsInt;
                    CalculatorControl.Screen.ClearLine(row - 1); // BASIC is 1-based but the underlying control is not.
                    //CalculatorControl.Screen.PrintAt(PrintSpaceType.At, str, row-1, 0); 
                    return RunResult.RunStatus.OK;

                case "ClearLines":
                    if (!BCObjectUtilities.CheckArgs(2, 2, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(0, "row", 1, 25, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(1, "row", 1, 25, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    var rowStart = (await ArgList[0].EvalAsync(context)).AsInt;
                    var rowEnd = (await ArgList[1].EvalAsync(context)).AsInt;
                    CalculatorControl.Screen.ClearLines(rowStart - 1, rowEnd - 1); // BASIC is 1-based but the underlying control is not.
                    return RunResult.RunStatus.OK;

                case "RequestActive":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    NRequestActive++;
                    if (CurrDisplayRequest == null) CurrDisplayRequest = new Windows.System.Display.DisplayRequest();
                    CurrDisplayRequest.RequestActive();
                    return RunResult.RunStatus.OK;

                case "RequestRelease":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (NRequestActive < 1)
                    {
                        Retval.SetError(1, $"RequestRelease() is being called more than RequestActive()");
                        return RunResult.RunStatus.ErrorContinue;
                    }
                    if (NRequestActive >= 1)
                    {
                        if (CurrDisplayRequest == null) CurrDisplayRequest = new Windows.System.Display.DisplayRequest();
                        CurrDisplayRequest.RequestActive();
                        NRequestActive--;
                    }
                    return RunResult.RunStatus.OK;
                case "FullScreenGraphics":
                    {
                        if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        if (CalculatorControl.FullScreenWindow == null)
                        {
                            var graphics = new AdvancedCalculator.BCBasic.Graphics.GraphicsControl();
                            CalculatorControl.AddFullScreenWindow(graphics);
                        }
                        Retval.AsObject = CalculatorControl.FullScreenWindow;
                        return RunResult.RunStatus.OK;
                    }
                case "GetAt":
                    {
                        if (!BCObjectUtilities.CheckArgs(2, 3, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var r = (await ArgList[0].EvalAsync(context)).AsInt;
                        var c = (await ArgList[1].EvalAsync(context)).AsInt;
                        var nchar = (ArgList.Count >= 3) ? (await ArgList[2].EvalAsync(context)).AsInt : 99999;
                        var result = CalculatorControl.Screen.GetAt(r - 1, c - 1, nchar);
                        Retval.AsString = result;
                        return RunResult.RunStatus.OK;
                    }

                case "Graphics":
                    {
                        if (!BCObjectUtilities.CheckArgs(0, 4, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var bg = context.ExternalConnections.GetBackground();
                        var fg = context.ExternalConnections.GetForeground();
                        var graphics = new AdvancedCalculator.BCBasic.Graphics.GraphicsControl(GraphicsControl.GraphicsType.Window, bg, fg);
                        CalculatorControl.Screen.AddGraphics(graphics);
                        Retval.AsObject = graphics;

                        if (ArgList.Count >= 2)
                        {
                            var x = (await ArgList[0].EvalAsync(context)).AsDouble;
                            var y = (await ArgList[1].EvalAsync(context)).AsDouble;
                            graphics.SetPosition(x, y);
                        }
                        if (ArgList.Count >= 4)
                        {
                            var h = (await ArgList[2].EvalAsync(context)).AsDouble;
                            var w = (await ArgList[3].EvalAsync(context)).AsDouble;
                            graphics.SetSize(h, w);
                        }
                        return RunResult.RunStatus.OK;
                    }


                case "Update":
                    CalculatorControl.Screen.Update();
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
