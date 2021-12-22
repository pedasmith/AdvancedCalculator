using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace Graphics
{
    public class GraphicLight : Graph3DContainer
    {
        public Color Light { get; } = Colors.White;
    }
}
