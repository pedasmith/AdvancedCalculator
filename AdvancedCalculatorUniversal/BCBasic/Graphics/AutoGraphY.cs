using AdvancedCalculator.BCBasic.RunTimeLibrary;
using BCBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace AdvancedCalculator.BCBasic.Graphics
{
    class AutoGraphY: AutoGraph
    {
        BCValueList Data = null;
        public AutoGraphY(BCValueList data)
        {
            Data = data;
        }
        public void UpdateData (GraphicsControl g, BCValueList data)
        {
            Data = data;
            var floats = Data.AsFloatArray();
            if (floats == null) return;
            g.Push();
            try
            {
                var state = g.CurrState;
                state.Map.SetMap(0, Data.Min, floats.Length - 1, Data.Max);
                state.Map.SetPadding(10);
#if NEVER_EVER_DEFINED
                state.XPadding = 10;
                state.YPadding = 10;

                state.XMin = 0;
                state.XMax = floats.Length - 1;
                state.YMin = Data.Min;
                state.YMax = Data.Max;
#endif
                var oldX = 0;
                var oldY = floats[oldX];
                for (var x = 1; x < floats.Length; x++)
                {
                    var y = floats[x];
                    g.UpdateLine(x-1, oldX, oldY, x, y); // when x=1, it's line index 0
                    oldX = x;
                    oldY = y;
                }
            }
            catch (Exception ex)
            {
                RTLSystemX.AddError($"Exception while AutoGraphY is {ex.Message}"); ;
            }
            g.Pop();
        }
        public void Display(GraphicsControl g)
        {
            // Can't make a graph without at least two points.
            if (Data == null) return;
            var floats = Data.AsFloatArray();
            if (floats == null) return;

            if (floats.Length < 2) return;

            g.Push();
            try
            {
                var state = g.CurrState;

                state.Map.SetMap(0, Data.Min, floats.Length - 1, Data.Max);
                state.Map.SetPadding(10);
#if NEVER_EVER_DEFINED
                state.XPadding = 10;
                state.YPadding = 10;

                state.XMin = 0;
                state.XMax = floats.Length - 1;
                state.YMin = Data.Min;
                state.YMax = Data.Max;
#endif

                var oldX = 0;
                var oldY = floats[oldX];
                for (var x = 1; x < floats.Length; x++)
                {
                    var y = floats[x];
                    g.Line(oldX, oldY, x, y);
                    oldX = x;
                    oldY = y;
                }
            }
            catch (Exception ex)
            {
                RTLSystemX.AddError($"Exception with AutoGraphY is {ex.Message}"); ;
            }

            g.Pop();
        }
    }
}
