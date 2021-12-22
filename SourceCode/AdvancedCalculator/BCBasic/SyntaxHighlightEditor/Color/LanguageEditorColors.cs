using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace Edit
{
    // Inspired by Obivion http://eclipsecolorthemes.org/?view=theme&id=1
    static class LanguageEditorColors
    {
        // String is e.g., #1E1E1E
        public static Color Parse(string str)
        {
            Color Retval = new Color();
            int intval = 0;
            Retval.A = 255;
            Int32.TryParse(str.Substring(1, 2), System.Globalization.NumberStyles.AllowHexSpecifier, null, out intval); Retval.R = (byte)intval;
            Int32.TryParse(str.Substring(3, 2), System.Globalization.NumberStyles.AllowHexSpecifier, null, out intval); Retval.G = (byte)intval;
            Int32.TryParse(str.Substring(5, 2), System.Globalization.NumberStyles.AllowHexSpecifier, null, out intval); Retval.B = (byte)intval;
            return Retval;
        }
        public static Color Background = Parse("#1E1E1E"); // black
        public static Color Selected = Parse("#2E2E2E"); // black (lighter)
        public static Color SelectedBorder = Parse("#3E3E3E"); // black (lighter more)

        public static Color REM = Parse("#C7DD0C"); // yellow
        public static Color Color = Parse("#D197D9"); // violet from interface
        public static Color Numbers = Parse("#7FB347"); // green
        public static Color Strings = Parse("#FFC600"); // dark yellow
        public static Color Variable = Parse("#79ABFF"); // darkish light blue
        public static Color External = Parse("#FFC600"); // Sort of peach
        public static Color Keyword = Parse("#FFFFFF"); // white
        public static Color Math = Parse("#D197D9"); // violet from interface
        public static Color Error = Parse("#B22222"); // x-windows firebrick
        public static Color MalformedStrings = Parse("#FF9644"); // dark yellow
    };


    public class LanguageColors
    {
        private Dictionary<Lexer.SymbolType, Brush> SymbolBrushes = new Dictionary<Lexer.SymbolType, Brush>()
        {
            // 2018-06-21: public enum SymbolType { CommentBegin, CommentText, External, Flag, Identifier, MalformedString, NewLine, Number, Operator, Punctuation, Reserved, String, WS, Unknown }

            {  Lexer.SymbolType.CommentBegin, new SolidColorBrush(LanguageEditorColors.REM) },
            {  Lexer.SymbolType.CommentText, new SolidColorBrush(LanguageEditorColors.REM) },
            {  Lexer.SymbolType.External, new SolidColorBrush(LanguageEditorColors.External) },
            {  Lexer.SymbolType.Flag, new SolidColorBrush(LanguageEditorColors.REM) },
            {  Lexer.SymbolType.Identifier, new SolidColorBrush(LanguageEditorColors.Variable) },
            {  Lexer.SymbolType.MalformedString, new SolidColorBrush(LanguageEditorColors.MalformedStrings) },
            {  Lexer.SymbolType.Number, new SolidColorBrush(LanguageEditorColors.Numbers) },
            {  Lexer.SymbolType.Operator, new SolidColorBrush(LanguageEditorColors.Math) },
            {  Lexer.SymbolType.Punctuation, new SolidColorBrush(LanguageEditorColors.Math) },
            {  Lexer.SymbolType.Reserved, new SolidColorBrush(LanguageEditorColors.Keyword) },
            {  Lexer.SymbolType.String, new SolidColorBrush(LanguageEditorColors.Strings) },

            {  Lexer.SymbolType.Unknown, new SolidColorBrush(Colors.Red) },
        };
        public Brush SymbolTypeToBrush(Lexer.SymbolType type)
        {
            if (SymbolBrushes.ContainsKey(type)) return SymbolBrushes[type];
            return SymbolBrushes[Lexer.SymbolType.Identifier];
        }
        public Brush BackgroundBrush { get; } = new SolidColorBrush(LanguageEditorColors.Background);
        public static Brush SelectedBrush { get; } = new SolidColorBrush(LanguageEditorColors.Selected);
        public static Brush SelectedBorderBrush { get; } = new SolidColorBrush(LanguageEditorColors.SelectedBorder);
    }
}
