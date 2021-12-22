using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Edit
{
    /// A word is the underlying text plus a symbol type.
    public class Word
    {
        public Word(ROS value, Lexer.SymbolType symbolType)
        {
            Text = value.ToString();
            SymbolType = symbolType;
        }
        public Word(string value, Lexer.SymbolType symbolType)
        {
            Text = value;
            SymbolType = symbolType;
        }
        public int TextLengthOffset = 0;
        public string Text; //NOTE: change to a ROS?
        public bool IsWS {  get { return SymbolType == Lexer.SymbolType.WS || SymbolType == Lexer.SymbolType.Flag; } }
        public Lexer.SymbolType SymbolType; // = Lexer.SymbolType.Identifier;
        public Lexer.OperatorType OperatorType = Lexer.OperatorType.NotOperator;
        public Lexer.Cmd Cmd = Lexer.Cmd.NotCommand;
        public double Value = 0.0;
        public string StringValue = ""; // In a few cases this will be differnt from Text. Can't change text because other stuff depends on Text.Length
        public string ValueNiceFormat = "F2";

        public bool Matches(string value)
        {
            if (value.Length != Text.Length) return false;
            int i = 0;
            foreach (var ch in Text)
            {
                if (ch != value[i++]) return false;
            }
            return true;
        }

        public string ToNiceString()
        {
            var str = Text;
            if (SymbolType == Lexer.SymbolType.Number)
                str = Value.ToString(ValueNiceFormat);
            return str;
        }
        public override string ToString()
        {
            return Text;
        }
    }
}
