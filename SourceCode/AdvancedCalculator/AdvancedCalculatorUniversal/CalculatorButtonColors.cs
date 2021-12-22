using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI;

namespace AdvancedCalculator
{
    public class CalculatorButtonColors
    {
        public CalculatorButtonColors()
        {
        }
        // Saddle Brown
        private static SolidColorBrush blackBrush = new SolidColorBrush(new Color() { A = 255, R = 0, G = 0, B = 0 });

        private static Brush _colorFeedbackColorful = new SolidColorBrush(new Color() { A = 255, R = 139, G = 69, B = 19 });
        private static Brush _colorFeedbackPlain = blackBrush;
        private Brush _colorFeedback = _colorFeedbackColorful;
        public Brush colorFeedback { get { return _colorFeedback; } }


        private static Brush[] _colorCalcListColorful = new Brush[]
        {
	        new SolidColorBrush(new Color() { A = 255, R = 0, G = 100, B = 0 }),
	        new SolidColorBrush(new Color() { A = 255, R = 0, G = 100, B = 0 }),
	        new SolidColorBrush(new Color() { A = 255, R = 6, G = 111, B = 15 }),
	        new SolidColorBrush(new Color() { A = 255, R = 13, G = 121, B = 32 }),
	        new SolidColorBrush(new Color() { A = 255, R = 23, G = 128, B = 51 }),
	        new SolidColorBrush(new Color() { A = 255, R = 33, G = 135, B = 69 }),
            new SolidColorBrush(new Color() { A = 255, R = 46, G = 139, B = 87 }),
            new SolidColorBrush(new Color() { A = 255, R = 56, G = 143, B = 97 }),
            new SolidColorBrush(new Color() { A = 255, R = 66, G = 148, B = 107 }),
        };
        private static Brush[] _colorCalcListPlain = new Brush[] { blackBrush, blackBrush, blackBrush, blackBrush, blackBrush, blackBrush, blackBrush, blackBrush, blackBrush };
        private Brush[] _colorCalcList = _colorCalcListColorful;
        public Brush[] colorCalcList { get { return _colorCalcList; } }

        // DarkRed..FireBrick
        private static Brush[] _colorSolverListColorful = new Brush[]
        {
	        new SolidColorBrush(new Color() { A = 255, R = 139, G = 0, B = 0 }),
	        new SolidColorBrush(new Color() { A = 255, R = 139, G = 0, B = 0 }),
	        new SolidColorBrush(new Color() { A = 255, R = 177, G = 15, B = 15 }),
	        new SolidColorBrush(new Color() { A = 255, R = 206, G = 38, B = 38 }),
	        new SolidColorBrush(new Color() { A = 255, R = 205, G = 92, B = 92 }),
        };
        private static Brush[] _colorSolverListPlain = new Brush[] { blackBrush, blackBrush, blackBrush, blackBrush, blackBrush };
        private Brush[] _colorSolverList = _colorSolverListColorful;
        public Brush[] colorSolverList { get { return _colorSolverList; } }

        private static Brush[] _colorConverterListColorful = new Brush[]
        {
	        new SolidColorBrush(new Color() { A = 255, R = 0, G = 139, B = 139 }),
	        new SolidColorBrush(new Color() { A = 255, R = 0, G = 139, B = 139 }),
	        new SolidColorBrush(new Color() { A = 255, R = 0, G = 157, B = 168 }),
	        new SolidColorBrush(new Color() { A = 255, R = 0, G = 172, B = 197 }),
	        new SolidColorBrush(new Color() { A = 255, R = 0, G = 183, B = 226 }),
	        new SolidColorBrush(new Color() { A = 255, R = 0, G = 191, B = 255 }),
        };
        private static Brush[] _colorConverterListPlain = new Brush[] { blackBrush, blackBrush, blackBrush, blackBrush, blackBrush, blackBrush };
        private Brush[] _colorConverterList = _colorConverterListColorful;
        public Brush[] colorConverterList { get { return _colorConverterList; } }

        // Indigo --> DarkSlateBlue
        private static Brush[] _color3ListColorful = new Brush[]
        {
	        new SolidColorBrush(new Color() { A = 255, R = 75, G = 0, B = 130 }),
	        new SolidColorBrush(new Color() { A = 255, R = 75, G = 30, B = 135 }),
            new SolidColorBrush(new Color() { A = 255, R = 72, G = 61, B = 139 }),
            new SolidColorBrush(new Color() { A = 255, R = 72, G = 81, B = 145 }),
        };
        private static Brush[] _color3ListPlain = new Brush[] { blackBrush, blackBrush, blackBrush, blackBrush };
        private Brush[] _color3List = _color3ListColorful;
        public Brush[] color3List { get { return _color3List; } } // Despite the name, color3list has 4 colors :-)

        // Indigo..MediumOrchid
        private static Brush[] _color9ListColorful = new Brush[]
        {
	        new SolidColorBrush(new Color() { A = 255, R = 75, G = 0, B = 130 }),
	        new SolidColorBrush(new Color() { A = 255, R = 75, G = 0, B = 130 }),
	        new SolidColorBrush(new Color() { A = 255, R = 93, G = 5, B = 149 }),
	        new SolidColorBrush(new Color() { A = 255, R = 111, G = 10, B = 167 }),
	        new SolidColorBrush(new Color() { A = 255, R = 129, G = 18, B = 183 }),
	        new SolidColorBrush(new Color() { A = 255, R = 148, G = 26, B = 198 }),
	        new SolidColorBrush(new Color() { A = 255, R = 166, G = 37, B = 212 }),
	        new SolidColorBrush(new Color() { A = 255, R = 178, G = 59, B = 213 }),
	        new SolidColorBrush(new Color() { A = 255, R = 186, G = 85, B = 211 }),
        };
        private static Brush[] _color9ListPlain = new Brush[] { blackBrush, blackBrush, blackBrush, blackBrush, blackBrush, blackBrush, blackBrush, blackBrush };
        private Brush[] _color9List = _color9ListColorful;
        public Brush[] color9List { get { return _color9List; } }

        private Brush _BaseBrush;
        private Brush _ClearBrush;
        private Brush _ConstantMathBrush;
        private Brush _ConstantPhysicsBrush;
        private Brush _ConstantNumBrush;
        private Brush _DefaultBrush;
        private Brush _ExpBrush;
        private Brush _FormatBrush;
        private Brush _FourFunctionBrush;
        private Brush _MathBrush;
        private Brush _MathBitBrush;
        private Brush _MathByteBrush;
        private Brush _MathFloorBrush;
        private Brush _MathLogBrush;
        private Brush _MathPowBrush;
        private Brush _MathRandBrush;
        private Brush _MathRotBrush;
        private Brush _MathTrigBrush;
        private Brush _MemBrush;
        private Brush _NumberKeyActiveBrush;
        private Brush _NumberKeyDisabledBrush;
        private Brush _ParenBrush;
        private Brush _ProgrammableKeyBrush;

        public enum ColorScheme { Colorful, Plain };
        public void SetColorScheme (ColorScheme scheme)
        {
            switch (scheme)
            {
                case ColorScheme.Colorful:
                    _BaseBrush = new SolidColorBrush(Windows.UI.Colors.DarkSlateBlue);
                    _ClearBrush = new SolidColorBrush(Windows.UI.Colors.Navy);
                    _ConstantMathBrush = new SolidColorBrush(Windows.UI.Colors.DeepSkyBlue);
                    _ConstantPhysicsBrush = new SolidColorBrush(Windows.UI.Colors.DarkBlue);
                    _ConstantNumBrush = new SolidColorBrush(Windows.UI.Colors.SlateBlue);
                    _DefaultBrush = new SolidColorBrush(Windows.UI.Colors.Pink);
                    _ExpBrush = new SolidColorBrush(Windows.UI.Colors.DarkSlateGray);
                    _FormatBrush = new SolidColorBrush(Windows.UI.Colors.SlateBlue);
                    _FourFunctionBrush = new SolidColorBrush(Windows.UI.Colors.DarkMagenta);
                    _MathBrush = new SolidColorBrush(Windows.UI.Colors.SteelBlue);
                    _MathBitBrush = new SolidColorBrush(Windows.UI.Colors.DarkGoldenrod);
                    _MathByteBrush = new SolidColorBrush(Windows.UI.Colors.DarkGreen);
                    _MathFloorBrush = new SolidColorBrush(Windows.UI.Colors.DeepPink);
                    _MathLogBrush = new SolidColorBrush(Windows.UI.Colors.DeepSkyBlue);
                    _MathPowBrush = new SolidColorBrush(Windows.UI.Colors.DarkMagenta);
                    _MathRandBrush = new SolidColorBrush(Windows.UI.Colors.DarkGreen);
                    _MathRotBrush = new SolidColorBrush(Windows.UI.Colors.DeepSkyBlue);
                    _MathTrigBrush = new SolidColorBrush(Windows.UI.Colors.DarkOrange);
                    _MemBrush = new SolidColorBrush(Windows.UI.Colors.Brown);
                    _NumberKeyActiveBrush = new SolidColorBrush(Windows.UI.Colors.LightSeaGreen);
                    _NumberKeyDisabledBrush = new SolidColorBrush(Windows.UI.Colors.Gray);
                    _ParenBrush = new SolidColorBrush(Windows.UI.Colors.DarkGreen);
                    _ProgrammableKeyBrush = new SolidColorBrush(Windows.UI.Colors.DarkGoldenrod); // Dup of ConstantNumBrush?
                    _colorFeedback = _colorFeedbackColorful;
                    _colorCalcList = _colorCalcListColorful;
                    _colorSolverList = _colorSolverListColorful;
                    _colorConverterList = _colorConverterListColorful;
                    _color3List = _color3ListColorful;
                    _color9List = _color9ListColorful;
                    break;
                case ColorScheme.Plain:
                    var brush = new SolidColorBrush(Windows.UI.Colors.Black);
                    _BaseBrush = brush;
                    _ClearBrush = brush;
                    _ConstantMathBrush = brush;
                    _ConstantPhysicsBrush = brush;
                    _ConstantNumBrush = brush;
                    _DefaultBrush = brush;
                    _ExpBrush = brush;
                    _FormatBrush = brush;
                    _FourFunctionBrush = brush;
                    _MathBrush = brush;
                    _MathBitBrush = brush;
                    _MathByteBrush = brush;
                    _MathFloorBrush = brush;
                    _MathLogBrush = brush;
                    _MathPowBrush = brush;
                    _MathRandBrush = brush;
                    _MathRotBrush = brush;
                    _MathTrigBrush = brush;
                    _MemBrush = brush;
                    _NumberKeyActiveBrush = brush;
                    _NumberKeyDisabledBrush = brush;
                    _ParenBrush = brush;
                    _ProgrammableKeyBrush = brush;
                    _colorFeedback = _colorFeedbackPlain;
                    _colorCalcList = _colorCalcListPlain;
                    _colorSolverList = _colorSolverListPlain;
                    _colorConverterList = _colorConverterListPlain;
                    _color3List = _color3ListPlain;
                    _color9List = _color9ListPlain;
                    break;
            }
        }

        private Brush GetBaseBrush()
        {
            return _BaseBrush;
        }

        private Brush GetClearBrush() // C, CE, delete
        {
            return _ClearBrush;
        }

        private Brush GetDefaultBrush()
        {
            return _DefaultBrush;
        }

        private Brush GetFormatBrush()
        {
            return _FormatBrush;
        }

        private Brush GetConstantMathBrush()
        {
            return _ConstantMathBrush;
        }

        private Brush GetConstantPhysicsBrush()
        {
            return _ConstantPhysicsBrush;
        }

        private Brush GetConstantNumBrush()
        {
            return _ConstantNumBrush;
        }

        private Brush GetProgrammableKeyBrush()
        {
            return _ProgrammableKeyBrush;
        }

        private Brush GetExpBrush()
        {
            return _ExpBrush;
        }

        private Brush GetMathBrush()
        {
            return _MathBrush;
        }

        private Brush GetMathBitBrush()
        {
            return _MathBitBrush;
        }

        private Brush GetMathByteBrush()
        {
            return _MathByteBrush;
        }

        private Brush GetMathFloorBrush()
        {
            return _MathFloorBrush;
        }

        private Brush GetMathLogBrush()
        {
            return _MathLogBrush;
        }

        private Brush GetMathPowBrush()
        {
            return _MathPowBrush;
        }

        private Brush GetMathTrigBrush()
        {
            return _MathTrigBrush;
        }

        private Brush GetMathRandBrush()
        {
            return _MathRandBrush;
        }

        private Brush GetMathRotBrush()
        {
            return _MathRotBrush;
        }

        private Brush GetMemBrush()
        {
            return _MemBrush;
        }

        private Brush GetFourFunctionBrush()
        {
            return _FourFunctionBrush;
        }

        private Brush GetNumberKeyBrush(bool Active)
        {
            if (Active) return _NumberKeyActiveBrush;
            return _NumberKeyDisabledBrush;
        }
        private Brush GetParenBrush()
        {
            return _ParenBrush;
        }

        private void SetColor(Button button, string erase)
        {
            if (button != null)
            {
                var tag = button.Tag as string;
                if (tag != null)
                {
                    if (tag.StartsWith("#KEY|n"))
                    {
                        var key = tag.Substring(6);
                        bool isActive = !erase.Contains(key);
                        button.Background = GetNumberKeyBrush(isActive);
                    }
                    else if (tag.StartsWith("#KEYbase"))
                    {
                        button.Background = GetBaseBrush();
                    }
                    else if (tag.StartsWith("#KEYclear"))
                    {
                        button.Background = GetClearBrush();
                    }
                    else if (tag.StartsWith("#KEYconmath"))
                    {
                        button.Background = GetConstantMathBrush();
                    }
                    else if (tag.StartsWith("#KEYconphys"))
                    {
                        button.Background = GetConstantPhysicsBrush();
                    }
                    else if (tag.StartsWith("#KEYconnum"))
                    {
                        button.Background = GetConstantNumBrush();
                    }
                    else if (tag.StartsWith("#KEYexp"))
                    {
                        button.Background = GetExpBrush();
                    }
                    else if (tag.StartsWith("#KEY4func"))
                    {
                        button.Background = GetFourFunctionBrush();
                    }
                    else if (tag.StartsWith("#KEYformat"))
                    {
                        button.Background = GetFormatBrush();
                    }
                    else if (tag.StartsWith("#KEYmathfloor"))
                    {
                        button.Background = GetMathFloorBrush();
                    }
                    else if (tag.StartsWith("#KEYmathbyte"))
                    {
                        button.Background = GetMathByteBrush();
                    }
                    else if (tag.StartsWith("#KEYmathbit"))
                    {
                        button.Background = GetMathBitBrush();
                    }
                    else if (tag.StartsWith("#KEYmathlog"))
                    {
                        button.Background = GetMathLogBrush();
                    }
                    else if (tag.StartsWith("#KEYmathpow"))
                    {
                        button.Background = GetMathPowBrush();
                    }
                    else if (tag.StartsWith("#KEYmathrand"))
                    {
                        button.Background = GetMathRandBrush();
                    }
                    else if (tag.StartsWith("#KEYmathrot"))
                    {
                        button.Background = GetMathRotBrush();
                    }
                    else if (tag.StartsWith("#KEYmathtrig"))
                    {
                        button.Background = GetMathTrigBrush();
                    }
                    else if (tag.StartsWith("#KEYmath"))
                    {
                        button.Background = GetMathBrush();
                    }
                    else if (tag.StartsWith("#KEYmem"))
                    {
                        button.Background = GetMemBrush();
                    }
                    else if (tag.StartsWith("#KEYnum"))
                    {
                        button.Background = GetNumberKeyBrush(true); // always active
                    }
                    else if (tag.StartsWith("#KEYparen"))
                    {
                        button.Background = GetParenBrush();
                    }
                    else if (tag.StartsWith("#KEYprog"))
                    {
                        button.Background = GetProgrammableKeyBrush();
                    }
                    else if (tag.StartsWith("#KEY"))
                    {
                        button.Background = GetDefaultBrush();
                    }
                }
            }
        }

        public void SetColors(UserControl uc, string erase)
        {
            if (uc.Content is FourPanel)
            {
                SetColors(uc.Content as FourPanel, erase);
                return;
            }
            if (uc.Content is FourPanelViewbox)
            {
                SetColors(uc.Content as FourPanelViewbox, erase);
                return;
            }
            if (uc.Content is UserControl)
            {
                SetColors(uc.Content as UserControl, erase);
                return;
            }
            if (uc.Content is Panel)
            {
                SetColors(uc.Content as Panel, erase);
                return;
            }
        }

        public void SetColors(FourPanel fp, string erase)
        {
            if (fp.ItemMain is UserControl)
            {
                SetColors(fp.ItemMain as UserControl, erase);
            }
            else if (fp.ItemMain is Panel)
            {
                Panel p = fp.ItemMain as Panel;
                SetColors(p, erase);
            }
            else if (fp.ItemMain is Viewbox)
            {
                Viewbox v = fp.ItemMain as Viewbox;
                SetColors(v.Child, erase);
            }
            else if (fp.ItemMain is Grid)
            {
                Panel p = fp.ItemMain as Panel;
                SetColors(p, erase);
            }
        }
        public void SetColors(FourPanelViewbox fp, string erase)
        {
            if (fp.ItemMain is UserControl)
            {
                SetColors(fp.ItemMain as UserControl, erase);
            }
            else if (fp.ItemMain is Panel)
            {
                Panel p = fp.ItemMain as Panel;
                SetColors(p, erase);
            }
            else if (fp.ItemMain is Viewbox)
            {
                Viewbox v = fp.ItemMain as Viewbox;
                SetColors(v.Child, erase);
            }
            else if (fp.ItemMain is Grid)
            {
                Panel p = fp.ItemMain as Panel;
                SetColors(p, erase);
            }
        }
        public void SetColors(Panel panel, string erase)
        {
            if (panel == null) return;
            foreach (var child in panel.Children)
            {
                SetColors(child, erase);
                /*
                if (child is Button)
                {
                    SetColor(child as Button, erase);
                }
                else if (child is Panel)
                {
                    SetColors(child as Panel, erase);
                }
                else if (child is UserControl)
                {
                    SetColors(child as UserControl, erase);
                }
                 * */
            }
        }

        public void SetColors(UIElement child, string erase)
        {
            if (child == null) return;
            if (child is Button)
            {
                SetColor(child as Button, erase);
            }
            else if (child is Panel)
            {
                SetColors(child as Panel, erase);
            }
            else if (child is UserControl)
            {
                SetColors(child as UserControl, erase);
            }
        }
    }
}
