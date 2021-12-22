using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using System.Diagnostics;

namespace AdvancedCalculator
{
    public class ColorDefinitions
    {
        class StringUtil
        {
            public static String GetWord(string str, int start)
            {
                if (start >= str.Length) return null;

                for (int end = start + 1; end < str.Length - 1; end++)
                {
                    char ch = str[end];
                    if (Char.IsUpper(ch))
                    {
                        // done!
                        return str.Substring(start, (end - start));
                    }
                }
                return str.Substring(start);
            }

            public static List<string> SplitByWord(string str)
            {
                List<string> Retval = new List<string>();

                int start = 0;
                while (true)
                {
                    string word = GetWord(str, start);
                    if (word == null) break;
                    Retval.Add(word);
                    start += word.Length;
                }


                return Retval;
            }

            static int TestGetWordOne(string str, int start, string expected)
            {
                int NError = 0;
                string actual = GetWord(str, start);
                if (actual != expected)
                {
                    Debug.WriteLine("ERROR: GetWord ({0}, {1}) expected {2} actual result {3}", str, start, expected, actual);
                }
                return NError;
            }
            public static int TestGetWord()
            {
                int NError = 0;
                NError += TestGetWordOne("MyWord", 0, "My");
                NError += TestGetWordOne("MyWord", 2, "Word");
                NError += TestGetWordOne("abcdef", 0, "abcdef");
                return NError;
            }
        }
        public ColorDefinitions()
        {
            StringUtil.TestGetWord();
            Init();
        }
        public class ColorDef
        {
            public ColorDef() { }
            public ColorDef(Color color, String Name)
            {
                this.FullName = Name;
                List<string> names = StringUtil.SplitByWord(this.FullName);
                int idx = names.Count - 1;
                this.Name = names[idx--];
                this.Adjective1 = idx >= 0 ? names[idx--] : "";
                this.Adjective2 = idx >= 0 ? names[idx--] : "";
                this.Color = color;
                this.Brush = new SolidColorBrush(this.Color);
                this.R = this.Color.R;
                this.G = this.Color.G;
                this.B = this.Color.B;
            }

            public string FullName { get; set; }
            public string Name { get; set; }
            public string Adjective1 { get; set; }
            public string Adjective2 { get; set; }
            public int R { get; set; }
            public int G { get; set; }
            public int B { get; set; }

            public double Rd { get { return (double)R / 255.0; } }
            public double Gd { get { return (double)G / 255.0; } }
            public double Bd { get { return (double)B / 255.0; } }

            public double Maxd { get { return Math.Max(Rd, Math.Max(Gd, Bd)); } }
            public double Mind { get { return Math.Min(Rd, Math.Min(Gd, Bd)); } }
            public double Chromad { get { return Maxd - Mind; } }

            public double Lightnesd { get { return (Maxd + Mind) / 2.0; } }
            public double Hued {
                get
                {
                    double Retval = 2.0;
                    if (Chromad == 0.0) Retval = 0.0;
                    else if (Maxd == Rd) Retval = 0+ ((Gd - Bd) / Chromad);
                    else if (Maxd == Gd) Retval = 2 + ((Bd - Rd) / Chromad);
                    else if (Maxd == Bd) Retval = 4 + ((Rd - Gd) / Chromad);

                    double alpha = .5 * (2 * Rd - Gd - Bd);
                    double beta = Math.Sqrt(3) / 2.0 * (Gd - Bd);
                    double H2 = Math.Atan2(alpha, beta);

                    H2 = H2 * 180 / Math.PI;
                    while (H2 < 0) H2 += 360;
                    while (H2 > 360) H2 -= 360;

                    Retval = Math.Round (H2);
                    return Retval;


                }

            }

            public string Category
            {
                get
                {
                    if (Chromad < 0.17) return "Monochrome";
                    double h = Hued;
                    if (h < 60.0) return "Yellow";
                    else if (h < 120.0) return "Red";
                    else if (h < 180.0) return "Magenta";
                    else if (h < 240.0) return "Blue";
                    else if (h < 300.0) return "Cyan";
                    return "Green";
                }
            }

            public string DML
            {
                get
                {
                    if (Lightnesd <= 0.30) return "Dark";
                    else if (Lightnesd <= 0.64) return "Medium";
                    else return "Light";
                }
            }

            public Color Color { get; set; }
            public Brush Brush { get; set; }
        }
        public ObservableCollection<ColorDef> Colors { get; set;  }

        public void OnColorSort(object sender, RoutedEventArgs e)
        {
            string orderBy = (sender as Button).Tag as string;
            IOrderedEnumerable<ColorDef> sortedList = null;
            switch (orderBy)
            {
                case "R": sortedList = Colors.OrderBy(color => color.R); break;
                case "G": sortedList = Colors.OrderBy(color => color.G); break;
                case "B": sortedList = Colors.OrderBy(color => color.B); break;
                case "H": sortedList = Colors.OrderBy(color => color.Hued); break;
                case "L": sortedList = Colors.OrderBy(color => color.Lightnesd); break;
                case "C": sortedList = Colors.OrderBy(color => color.Chromad); break;
                case "Cat": sortedList = Colors.OrderBy(color => color.Category); break;
                case "DML": sortedList = Colors.OrderBy(color => color.DML); break;
            }
            if (sortedList != null)
            {
                var newColors = new ObservableCollection<ColorDef>();
                foreach (var item in sortedList)
                {
                    newColors.Add(item);
                }
                Colors.Clear();
                foreach (var item in newColors)
                {
                    Colors.Add(item);
                }
            }
        }
        public void Init()
        {
            Colors = new ObservableCollection<ColorDef>();
            /*

            Colors.Add(new ColorDef(Windows.UI.Colors.AliceBlue, "AliceBlue"));
            Colors.Add(new ColorDef(Windows.UI.Colors.AntiqueWhite, "AntiqueWhite"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Aqua, "Aqua"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Aquamarine, "Aquamarine"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Azure, "Azure"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Beige, "Beige"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Bisque, "Bisque"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Black, "Black"));
            Colors.Add(new ColorDef(Windows.UI.Colors.BlanchedAlmond, "BlanchedAlmond"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Blue, "Blue"));
            Colors.Add(new ColorDef(Windows.UI.Colors.BlueViolet, "BlueViolet"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Brown, "Brown"));
            Colors.Add(new ColorDef(Windows.UI.Colors.BurlyWood, "BurlyWood"));
            Colors.Add(new ColorDef(Windows.UI.Colors.CadetBlue, "CadetBlue"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Chartreuse, "Chartreuse"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Chocolate, "Chocolate"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Coral, "Coral"));
            Colors.Add(new ColorDef(Windows.UI.Colors.CornflowerBlue, "CornflowerBlue"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Cornsilk, "Cornsilk"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Crimson, "Crimson"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Cyan, "Cyan"));
            Colors.Add(new ColorDef(Windows.UI.Colors.DarkBlue, "DarkBlue"));
            Colors.Add(new ColorDef(Windows.UI.Colors.DarkCyan, "DarkCyan"));
            Colors.Add(new ColorDef(Windows.UI.Colors.DarkGoldenrod, "DarkGoldenrod"));
            Colors.Add(new ColorDef(Windows.UI.Colors.DarkGray, "DarkGray"));
            Colors.Add(new ColorDef(Windows.UI.Colors.DarkGreen, "DarkGreen"));
            Colors.Add(new ColorDef(Windows.UI.Colors.DarkKhaki, "DarkKhaki"));
            Colors.Add(new ColorDef(Windows.UI.Colors.DarkMagenta, "DarkMagenta"));
            Colors.Add(new ColorDef(Windows.UI.Colors.DarkOliveGreen, "DarkOliveGreen"));
            Colors.Add(new ColorDef(Windows.UI.Colors.DarkOrange, "DarkOrange"));
            Colors.Add(new ColorDef(Windows.UI.Colors.DarkOrchid, "DarkOrchid"));
            Colors.Add(new ColorDef(Windows.UI.Colors.DarkRed, "DarkRed"));
            Colors.Add(new ColorDef(Windows.UI.Colors.DarkSalmon, "DarkSalmon"));
            Colors.Add(new ColorDef(Windows.UI.Colors.DarkSeaGreen, "DarkSeaGreen"));
            Colors.Add(new ColorDef(Windows.UI.Colors.DarkSlateBlue, "DarkSlateBlue"));
            Colors.Add(new ColorDef(Windows.UI.Colors.DarkSlateGray, "DarkSlateGray"));
            Colors.Add(new ColorDef(Windows.UI.Colors.DarkTurquoise, "DarkTurquoise"));
            Colors.Add(new ColorDef(Windows.UI.Colors.DarkViolet, "DarkViolet"));
            Colors.Add(new ColorDef(Windows.UI.Colors.DeepPink, "DeepPink"));
            Colors.Add(new ColorDef(Windows.UI.Colors.DeepSkyBlue, "DeepSkyBlue"));
            Colors.Add(new ColorDef(Windows.UI.Colors.DimGray, "DimGray"));
            Colors.Add(new ColorDef(Windows.UI.Colors.DodgerBlue, "DodgerBlue"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Firebrick, "Firebrick"));
            Colors.Add(new ColorDef(Windows.UI.Colors.FloralWhite, "FloralWhite"));
            Colors.Add(new ColorDef(Windows.UI.Colors.ForestGreen, "ForestGreen"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Fuchsia, "Fuchsia"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Gainsboro, "Gainsboro"));
            Colors.Add(new ColorDef(Windows.UI.Colors.GhostWhite, "GhostWhite"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Gold, "Gold"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Goldenrod, "Goldenrod"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Gray, "Gray"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Green, "Green"));
            Colors.Add(new ColorDef(Windows.UI.Colors.GreenYellow, "GreenYellow"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Honeydew, "Honeydew"));
            Colors.Add(new ColorDef(Windows.UI.Colors.HotPink, "HotPink"));
            Colors.Add(new ColorDef(Windows.UI.Colors.IndianRed, "IndianRed"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Indigo, "Indigo"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Ivory, "Ivory"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Khaki, "Khaki"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Lavender, "Lavender"));
            Colors.Add(new ColorDef(Windows.UI.Colors.LavenderBlush, "LavenderBlush"));
            Colors.Add(new ColorDef(Windows.UI.Colors.LawnGreen, "LawnGreen"));
            Colors.Add(new ColorDef(Windows.UI.Colors.LemonChiffon, "LemonChiffon"));
            Colors.Add(new ColorDef(Windows.UI.Colors.LightBlue, "LightBlue"));
            Colors.Add(new ColorDef(Windows.UI.Colors.LightCoral, "LightCoral"));
            Colors.Add(new ColorDef(Windows.UI.Colors.LightCyan, "LightCyan"));
            Colors.Add(new ColorDef(Windows.UI.Colors.LightGoldenrodYellow, "LightGoldenrodYellow"));
            Colors.Add(new ColorDef(Windows.UI.Colors.LightGray, "LightGray"));
            Colors.Add(new ColorDef(Windows.UI.Colors.LightGreen, "LightGreen"));
            Colors.Add(new ColorDef(Windows.UI.Colors.LightPink, "LightPink"));
            Colors.Add(new ColorDef(Windows.UI.Colors.LightSalmon, "LightSalmon"));
            Colors.Add(new ColorDef(Windows.UI.Colors.LightSeaGreen, "LightSeaGreen"));
            Colors.Add(new ColorDef(Windows.UI.Colors.LightSkyBlue, "LightSkyBlue"));
            Colors.Add(new ColorDef(Windows.UI.Colors.LightSlateGray, "LightSlateGray"));
            Colors.Add(new ColorDef(Windows.UI.Colors.LightSteelBlue, "LightSteelBlue"));
            Colors.Add(new ColorDef(Windows.UI.Colors.LightYellow, "LightYellow"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Lime, "Lime"));
            Colors.Add(new ColorDef(Windows.UI.Colors.LimeGreen, "LimeGreen"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Linen, "Linen"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Magenta, "Magenta"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Maroon, "Maroon"));
            Colors.Add(new ColorDef(Windows.UI.Colors.MediumAquamarine, "MediumAquamarine"));
            Colors.Add(new ColorDef(Windows.UI.Colors.MediumBlue, "MediumBlue"));
            Colors.Add(new ColorDef(Windows.UI.Colors.MediumOrchid, "MediumOrchid"));
            Colors.Add(new ColorDef(Windows.UI.Colors.MediumPurple, "MediumPurple"));
            Colors.Add(new ColorDef(Windows.UI.Colors.MediumSeaGreen, "MediumSeaGreen"));
            Colors.Add(new ColorDef(Windows.UI.Colors.MediumSlateBlue, "MediumSlateBlue"));
            Colors.Add(new ColorDef(Windows.UI.Colors.MediumSpringGreen, "MediumSpringGreen"));
            Colors.Add(new ColorDef(Windows.UI.Colors.MediumTurquoise, "MediumTurquoise"));
            Colors.Add(new ColorDef(Windows.UI.Colors.MediumVioletRed, "MediumVioletRed"));
            Colors.Add(new ColorDef(Windows.UI.Colors.MidnightBlue, "MidnightBlue"));
            Colors.Add(new ColorDef(Windows.UI.Colors.MintCream, "MintCream"));
            Colors.Add(new ColorDef(Windows.UI.Colors.MistyRose, "MistyRose"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Moccasin, "Moccasin"));
            Colors.Add(new ColorDef(Windows.UI.Colors.NavajoWhite, "NavajoWhite"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Navy, "Navy"));
            Colors.Add(new ColorDef(Windows.UI.Colors.OldLace, "OldLace"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Olive, "Olive"));
            Colors.Add(new ColorDef(Windows.UI.Colors.OliveDrab, "OliveDrab"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Orange, "Orange"));
            Colors.Add(new ColorDef(Windows.UI.Colors.OrangeRed, "OrangeRed"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Orchid, "Orchid"));
            Colors.Add(new ColorDef(Windows.UI.Colors.PaleGoldenrod, "PaleGoldenrod"));
            Colors.Add(new ColorDef(Windows.UI.Colors.PaleGreen, "PaleGreen"));
            Colors.Add(new ColorDef(Windows.UI.Colors.PaleTurquoise, "PaleTurquoise"));
            Colors.Add(new ColorDef(Windows.UI.Colors.PaleVioletRed, "PaleVioletRed"));
            Colors.Add(new ColorDef(Windows.UI.Colors.PapayaWhip, "PapayaWhip"));
            Colors.Add(new ColorDef(Windows.UI.Colors.PeachPuff, "PeachPuff"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Peru, "Peru"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Pink, "Pink"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Plum, "Plum"));
            Colors.Add(new ColorDef(Windows.UI.Colors.PowderBlue, "PowderBlue"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Purple, "Purple"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Red, "Red"));
            Colors.Add(new ColorDef(Windows.UI.Colors.RosyBrown, "RosyBrown"));
            Colors.Add(new ColorDef(Windows.UI.Colors.RoyalBlue, "RoyalBlue"));
            Colors.Add(new ColorDef(Windows.UI.Colors.SaddleBrown, "SaddleBrown"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Salmon, "Salmon"));
            Colors.Add(new ColorDef(Windows.UI.Colors.SandyBrown, "SandyBrown"));
            Colors.Add(new ColorDef(Windows.UI.Colors.SeaGreen, "SeaGreen"));
            Colors.Add(new ColorDef(Windows.UI.Colors.SeaShell, "SeaShell"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Sienna, "Sienna"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Silver, "Silver"));
            Colors.Add(new ColorDef(Windows.UI.Colors.SkyBlue, "SkyBlue"));
            Colors.Add(new ColorDef(Windows.UI.Colors.SlateBlue, "SlateBlue"));
            Colors.Add(new ColorDef(Windows.UI.Colors.SlateGray, "SlateGray"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Snow, "Snow"));
            Colors.Add(new ColorDef(Windows.UI.Colors.SpringGreen, "SpringGreen"));
            Colors.Add(new ColorDef(Windows.UI.Colors.SteelBlue, "SteelBlue"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Tan, "Tan"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Teal, "Teal"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Thistle, "Thistle"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Tomato, "Tomato"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Transparent, "Transparent"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Turquoise, "Turquoise"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Violet, "Violet"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Wheat, "Wheat"));
            Colors.Add(new ColorDef(Windows.UI.Colors.White, "White"));
            Colors.Add(new ColorDef(Windows.UI.Colors.WhiteSmoke, "WhiteSmoke"));
            Colors.Add(new ColorDef(Windows.UI.Colors.Yellow, "Yellow"));
            Colors.Add(new ColorDef(Windows.UI.Colors.YellowGreen, "YellowGreen"));
            */
        }
    }
}


/* uiColorMetro XAML:
            <Grid Name="uiColorMetro" Grid.Column="2" HorizontalAlignment="Center" VerticalAlignment="Center" Tag="usingWPFElectricalEngineeringVIRSolver" Visibility="Collapsed">
                <Grid.RowDefinitions>
                    <RowDefinition Height="auto" />
                    <RowDefinition Height="*" />
                    <RowDefinition Height="auto" />
                </Grid.RowDefinitions>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="auto" />
                    <ColumnDefinition Width="*" />
                </Grid.ColumnDefinitions>

                <!-- TOP -->
                <Border Grid.Row="0" Grid.Column="0" Grid.ColumnSpan="2"  >
                    <TextBlock Grid.Row="0" Style="{StaticResource titletext}" >Metro Colors</TextBlock>
                </Border>


                <!-- MAIN -->
                <Grid Name="uiGridColorDefinitions" Grid.Row="1" Grid.Column="0" MinWidth="500" MinHeight="500">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="auto" />
                        <ColumnDefinition Width="auto" />
                        <ColumnDefinition Width="auto" />
                        <ColumnDefinition Width="auto" />
                        <ColumnDefinition Width="auto" />
                        <ColumnDefinition Width="auto" />
                        <ColumnDefinition Width="auto" />
                        <ColumnDefinition Width="auto" />
                        <ColumnDefinition Width="auto" />
                    </Grid.ColumnDefinitions>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="auto" />
                        <RowDefinition Height="auto" />
                        <RowDefinition Height="auto" />

                    </Grid.RowDefinitions>
                    
                    <Grid Grid.Row="0" Grid.Column="0">
                        <StackPanel>
                            <TextBlock FontSize="15">Sort By</TextBlock>
                            <StackPanel Orientation="Horizontal">
                                <Button Tag="Cat" Click="colorDefinitions.OnColorSort" Content="Color"/>
                                <Button Tag="DML" Click="colorDefinitions.OnColorSort" Content="Dark/Med/Light"/>
                                <Button Tag="H" Click="colorDefinitions.OnColorSort" Content="Hue"/>
                                <Button Tag="C" Click="colorDefinitions.OnColorSort" Content="Chroma"/>
                                <Button Tag="L" Click="colorDefinitions.OnColorSort" Content="Lightness"/>
                                <Button Tag="R" Click="colorDefinitions.OnColorSort" Content="Red"/>
                                <Button Tag="G" Click="colorDefinitions.OnColorSort" Content="Green"/>
                                <Button Tag="B" Click="colorDefinitions.OnColorSort" Content="Blue"/>
                            </StackPanel>
                        </StackPanel>
                    </Grid>

                    <ListBox Grid.Row="1" Grid.Column="0" ItemsSource="{Binding Path=colorDefinitions.Colors}">
                        <ListBox.ItemTemplate>
                            <DataTemplate>
                                <Border BorderBrush="LightGray" BorderThickness="2">
                                    <Grid MinHeight="50">
                                        <Grid.ColumnDefinitions>
                                            <ColumnDefinition Width="50" />
                                            <ColumnDefinition Width="auto" />
                                        </Grid.ColumnDefinitions>
                                        <Grid.RowDefinitions>
                                            <RowDefinition Height="20" />
                                            <RowDefinition Height="20" />
                                            <RowDefinition Height="12" />
                                        </Grid.RowDefinitions>
                                        <Rectangle Grid.Row="0" Grid.Column="0" Grid.RowSpan="2" Width="44" Height="44" Fill="{Binding Brush}" />
                                        <StackPanel Grid.Row="0" Grid.Column="1" Grid.ColumnSpan="3" Orientation="Horizontal">
                                            <TextBlock Text="{Binding Adjective2}" FontSize="10" VerticalAlignment="Bottom"/>
                                            <TextBlock Text="{Binding Adjective1}" FontSize="10" VerticalAlignment="Bottom"/>
                                            <TextBlock Text="{Binding Name}" FontSize="16" />
                                        </StackPanel>
                                        <StackPanel Grid.Row="1" Grid.Column="1" Orientation="Horizontal">
                                            <TextBlock Text="{Binding Category}"  FontSize="12"/>
                                            <TextBlock Text="  (" FontSize="12"/>
                                            <TextBlock Text="{Binding DML}" FontSize="12"/>
                                            <TextBlock Text=")" FontSize="12"/>
                                        </StackPanel>
                                        <StackPanel Grid.Row="2" Grid.Column="1" Orientation="Horizontal">
                                            <TextBlock Text="R=" FontSize="8"/>
                                            <TextBlock Text="{Binding R}" FontSize="8"/>
                                            <TextBlock Text=" G=" FontSize="8"/>
                                            <TextBlock Text="{Binding G}" FontSize="8"/>
                                            <TextBlock Text=" B=" FontSize="8"/>
                                            <TextBlock Text="{Binding B}" FontSize="8"/>
                                            <TextBlock Text=" chroma=" FontSize="8"/>
                                            <TextBlock Text="{Binding Chromad}" FontSize="8"/>
                                            <TextBlock Text=" hue=" FontSize="8"/>
                                            <TextBlock Text="{Binding Hued}" FontSize="8"/>
                                            <TextBlock Text=" lightness=" FontSize="8"/>
                                            <TextBlock Text="{Binding Lightnesd}" FontSize="8"/>
                                        </StackPanel>
                                    </Grid>
                                </Border>
                            </DataTemplate>
                        </ListBox.ItemTemplate>
                    </ListBox>

                    
                    


                </Grid>

                <!-- RIGHT -->
                <Image Grid.Row="1" Grid.Column="1" Source="/Assets/SolverImages/Blank.png"  MaxHeight="400" MaxWidth="400" HorizontalAlignment="Left"/>

                <!-- BOTTOM -->
                <Grid Grid.Row="2" Grid.ColumnSpan="2"  Style="{StaticResource aboutboxstyle}">
                </Grid>
            </Grid>
            <!-- uiColorMetro -->


*/
