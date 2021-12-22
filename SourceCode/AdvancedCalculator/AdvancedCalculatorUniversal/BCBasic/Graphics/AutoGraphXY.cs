using AdvancedCalculator.BCBasic.RunTimeLibrary;
using BCBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace AdvancedCalculator.BCBasic.Graphics
{
    class AutoGraphXY: AutoGraph
    {
        // In the future these might be settable.
        const int XCOL = 0;
        const int YCOL = 1;

        BCValueList Data = null;
        public AutoGraphXY(BCValueList data)
        {
            Data = data;
        }

        public void Display(GraphicsControl g)
        {
            // Can't make a graph without at least two points.
            if (Data == null || Data.data == null) return;
            if (Data.data.Count < 2) return;

            g.Push();
            try
            {
                var state = g.CurrState;

                var xminmax = Data.GetMinMaxOf(XCOL);
                var yminmax = Data.GetMinMaxOf(YCOL);
                var ymin = state.GraphOptions.YAxisMinValue(yminmax.Item1);
                var ymax = state.GraphOptions.YAxisMaxValue(yminmax.Item2);

                state.Map.SetMap(xminmax.Item1, ymin, xminmax.Item2, ymax);
                state.Map.SetPadding(10);

                g.Line(xminmax.Item1, 0, xminmax.Item2, 0);

                var oldX = double.NaN;
                var oldY = double.NaN;
                for (var index = 0; index < Data.data.Count; index++)
                {
                    try
                    {
                        var x = Data.data[index].AsArray.data[XCOL].AsDouble;
                        var y = Data.data[index].AsArray.data[YCOL].AsDouble;
                        if (!double.IsNaN(oldX) && !double.IsNaN(oldY))
                        {
                            g.Line(oldX, oldY, x, y);
                        }
                        oldX = x;
                        oldY = y;
                    }
                    catch (Exception ex)
                    {
                        RTLSystemX.AddError($"Exception while AutoGraphXY is {ex.Message}"); ;
                    }
                }
            }
            catch (Exception ex)
            {
                RTLSystemX.AddError($"Exception in AutoGraphXY is {ex.Message}"); ;
            }

            g.Pop();
        }

        public void UpdateData(GraphicsControl g, BCValueList data)
        {
            throw new NotImplementedException();
        }
    }
}
