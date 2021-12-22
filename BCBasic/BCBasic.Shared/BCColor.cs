using System.Globalization;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace BCBasic
{
    public class BCColor
    {
        public Color Color { get; set; }
        private Brush _brush = null;
        public Brush Brush
        {
            get
            {
                if (_brush == null)
                {
                    _brush = new SolidColorBrush(Color);
                }
                return _brush;
            }
            internal set { _brush = value; }
        }
        public BCColor(Color color)
        {
            Color = color;
            _brush = null;
        }

        public BCColor(string name) // or #RRGGBB
        {
            if (name == null) return;
            if (name.StartsWith("#")) InitHex(name);
            else InitColorName(name);

        }
        public BCColor(int index)
        {
            InitSinclairInkNumber(index);
        }

        // #RRGGBB
        private void InitHex(string hex)
        {
            if (hex.Length != 7)
            {
                Color = Colors.Violet; // Error!  Bad color --> RED
            }
            else
            {
                var rstr = hex.Substring(1, 2);
                var gstr = hex.Substring(3, 2);
                var bstr = hex.Substring(5, 2);
                byte r, g, b;
                byte.TryParse(rstr, NumberStyles.AllowHexSpecifier, null, out r);
                byte.TryParse(gstr, NumberStyles.AllowHexSpecifier, null, out g);
                byte.TryParse(bstr, NumberStyles.AllowHexSpecifier, null, out b);
                Color = new Windows.UI.Color() { R=r, G=g, B=b, A=255 };
            }
        }

        private void InitColorName(string colorName)
        {
            switch (colorName.ToLower())
            {
                case "black": Color = Colors.Black; break;
                case "blue": Color = Colors.Blue; break;
                case "red": Color = Colors.Red; break;
                case "magenta": Color = Colors.Magenta; break;
                case "green": Color = Colors.Green; break;
                case "cyan": Color = Colors.DarkCyan; break;
                case "yellow": Color = Colors.DarkGoldenrod; break;
                case "white": Color = Colors.White; break;
                case "none": Color = Colors.Transparent; break;
            }
        }

        private void InitSinclairInkNumber(int number)
        {
            number = number % 8;
            switch (number)
            {
                case 0: InitColorName("BLACK"); break;
                case 1: InitColorName("BLUE"); break;
                case 2: InitColorName("RED"); break;
                case 3: InitColorName("MAGENTA"); break;
                case 4: InitColorName("GREEN"); break;
                case 5: InitColorName("CYAN"); break;
                case 6: InitColorName("YELLOW"); break;
                case 7: InitColorName("WHITE"); break;
            }
        }
    }
}
