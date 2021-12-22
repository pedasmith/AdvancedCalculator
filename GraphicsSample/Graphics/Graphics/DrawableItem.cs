using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Graphics
{
    public class DrawableItem
    {
        public DrawableItem(double z, FrameworkElement item)
        {
            Z = z;
            Item = item;
        }
        public double Z { get; set; }
        public FrameworkElement Item { get; set; }
    }
}
