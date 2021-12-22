using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphics
{
    public interface GraphicAnimation
    {
    }

    public class GraphicRotateAnimation : GraphicAnimation
    {
        public GraphicRotateAnimation (Graph3DContainer.Axis axis, double angleInRadians)
        {
            Axis = axis;
            AngleInRadians = angleInRadians;
        }
        public Graph3DContainer.Axis Axis { get; set;  }
        public double AngleInRadians { get; set;  }
        public double CurrAngleInRadians { get; set; } = 0;
    }
}
