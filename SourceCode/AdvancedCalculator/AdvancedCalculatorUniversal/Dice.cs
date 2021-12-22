using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Windows.UI;


// Boggle is a 6x6 set of dice with the following patterns:
// AAAFRS	AAEEEE	AAFIRS	ADENNN	AEEEEM
// AEEGMU	AEGMNN	AFIRSY	BJKQXZ	CCNSTW
// CEIILT	CEILPT	CEIPST	DHHNOT	DHHLOR
// DHLNOR	DDLNOR	EIIITT	EMOTTT	ENSSSU
// FIPRSY	GORRVW	HIPRRY	NOOTUW	OOOTTU
namespace AdvancedCalculator
{
    public class Dice
    {
        private Random R = new Random();
        static string diceChars = "⚀⚁⚂⚃⚄⚅";
        static SolidColorBrush[] clBrushes = new SolidColorBrush[] {
            new SolidColorBrush (Colors.Orange),
            new SolidColorBrush (Colors.Yellow),
            new SolidColorBrush (Colors.Green),
            new SolidColorBrush (Colors.Blue),
            new SolidColorBrush (Colors.Purple),
            new SolidColorBrush (Colors.Red),
        };
        static string[] clColorNames = new string[] {
            "Orange", "Yellow", "Green", "Blue", "Purple", "Red"
        };
        static string clChars = "▇";

        public void onGamesDiceRoll(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            Panel parent = b.Parent as Panel;
            if (parent is StackPanel) parent = parent.Parent as Panel;
            else if (parent is VariableSizedWrapGrid) parent = parent.Parent as Panel;
            String about = b.Tag as string; // 2|6|dice means 2 dice, each 1..6, and they look like dice
            string[] data = about.Split(new char[] { '|' });
            int ndice = int.Parse(data[0]);
            int nnum = int.Parse(data[1]);
            string type = data[2]; // dice
            Roll(parent.Children, ndice, nnum, type);
        }

        public void Roll(UIElementCollection siblings, int ndice, int nnum, string type)
        {
            // Get the 'name' textblock (if any)
            TextBlock nameBlock = null;
            for (int i = 0; i < siblings.Count; i++)
            {
                var sibling = siblings[i] as TextBlock;
                if (sibling != null)
                {
                    if ((sibling.Tag as string) == "name")
                    {
                        nameBlock = sibling;
                        break;
                    }
                }
            }

            if (type == "coin")
            {
                Flip(siblings, nameBlock, nnum, type);
            }
            else
            {
                int total = 0;
                int nRolls = 0;
                for (int i = 0; i < siblings.Count; i++)
                {
                    var fe = siblings[i];
                    if (fe is TextBlock)
                    {
                        SetItem(fe as TextBlock, nameBlock, ref nRolls, ndice, nnum, type, ref total);
                    }
                    else if (fe is Panel)
                    {
                        foreach (var item in (fe as Panel).Children)
                        {
                            if (item is TextBlock)
                            {
                                SetItem(item as TextBlock, nameBlock, ref nRolls, ndice, nnum, type, ref total);
                            }
                        }
                    }
                }
            }
        }

        private int SetItem(TextBlock sibling, TextBlock nameBlock, ref int nRolls, int ndice, int nnum, string type, ref int total)
        {
            string tag = sibling == null ? null : sibling.Tag as string;
            if (tag != null)
            {
                if (tag == "dice")
                {
                    if (nRolls < ndice)
                    {
                        sibling.Visibility = Visibility.Visible;
                        total += Roll(sibling, nameBlock, nnum, type);
                    }
                    else
                    {
                        sibling.Visibility = Visibility.Collapsed;
                    }
                    nRolls++;
                }
                else if (tag == "total")
                {
                    sibling.Visibility = ndice > 1 ? Visibility.Visible : Visibility.Collapsed;
                    sibling.Text = string.Format("{0}", total);
                }
                else if (tag == "totalTitle")
                {
                    sibling.Visibility = ndice > 1 ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            return total;

        }

        public void Flip(UIElementCollection siblings, TextBlock nameBlock, int nnum, string type)
        {
            int value = R.Next(nnum*32);
            value = (int)Math.Floor((double)value / 32.0); // low bits aren't always very random
            int nRolls = 0;

            for (int i = 0; i < siblings.Count; i++)
            {
                var sibling = siblings[i] as FrameworkElement;
                if (sibling != null)
                {
                    if ((sibling.Tag as string) == "dice")
                    {
                        if (nRolls == value)
                        {
                            sibling.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            sibling.Visibility = Visibility.Collapsed;
                        }
                        nRolls++;
                    }
                }
            }

            if (type == "coin")
            {
                string text = value == 0 ? "Heads" : "Tails";
                nameBlock.Text = text;
            }
        }

        public int Roll (TextBlock ui, TextBlock nameBlock, int nnum, string type)
        {
            //int value = R.Next(nnum) + 1;
            int value = R.Next(nnum * 32);
            value = (int)Math.Floor((double)value / 32.0); // low bits aren't always very random
            value = value + 1;
            
            if (type == "dice" && nnum == diceChars.Length) // Only 6-side dice use this
            {
                ui.Text = string.Format("{0}", diceChars[value-1]); // value is 1..6; dice chars are 0..5
                if (nameBlock != null)
                {
                    nameBlock.Text = string.Format("{0}", value);
                }
            }
            else if (type == "candy" && nnum <= clBrushes.Length)
            {
                int nitems = R.Next(2) + 1; // 1 or 2
                string fmt = nitems == 1 ? "{0}" : "{0}   {0}";
                ui.Text = string.Format (fmt, clChars[0]);
                ui.Foreground = clBrushes[value-1]; // different colors
                if (nameBlock != null)
                {
                    nameBlock.Text = string.Format("{0} {1}", nitems, clColorNames[value - 1]);
                }
            }
            else
            {
                ui.Text = string.Format("{0}", value);
            }
            return value;
        }
    }
}

