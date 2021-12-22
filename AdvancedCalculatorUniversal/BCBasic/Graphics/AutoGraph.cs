using BCBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedCalculator.BCBasic.Graphics
{
    interface AutoGraph
    {
        void Display(GraphicsControl g);
        void UpdateData(GraphicsControl g, BCValueList data);
    }

    public class GraphOptions
    {
        //TODO: actually implement the XAxisVisible 
        public bool XAxisVisible { get; set; } = true;
        public double YAxisMin { get; set; } = Double.NaN;
        public double YAxisMinValue (double dataValue)
        {
            if (Double.IsNaN(YAxisMin)) return dataValue;
            return YAxisMin;
        }
        public double YAxisMax { get; set; } = Double.NaN;
        public double YAxisMaxValue(double dataValue)
        {
            if (Double.IsNaN(YAxisMax)) return dataValue;
            return YAxisMax;
        }

        public GraphOptions Dup()
        {
            var Retval = new GraphOptions();
            Retval.XAxisVisible = XAxisVisible;
            Retval.YAxisMin = YAxisMin;
            Retval.YAxisMax = YAxisMax;
            return Retval;
        }
    }
}
