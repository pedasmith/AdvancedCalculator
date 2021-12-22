using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Text;
using Windows.UI.Xaml.Documents;

namespace Edit
{
    // (ParserResult, ParsedLet, ROS)
    public class ParserResultTuple
    {
        public ParserResultTuple(ParserStatus status, ParsedResult result, ROS span)
        {
            Status = status;
            Result = result;
            RemainingSpan = span;
        }
        public ParserResultTuple(ParserStatus status)
        {
            Status = status;
            Result = null;
            RemainingSpan = null;
        }

        public static ParserResultTuple Expected(string item, ROS span)
        {
            var Retval = new ParserResultTuple(
                new ParserStatus(ParserStatus.ResultType.Error, $"Expected {item}"),
                null,
                span
                );
            return Retval;
        }

        public static ParserResultTuple ExpectedConstant(ROS span)
        {
            var Retval = new ParserResultTuple(
                new ParserStatus(ParserStatus.ResultType.Error, $"Expected constant"),
                null,
                span
                );
            return Retval;
        }
        public static ParserResultTuple ExpectedExpression(ROS span)
        {
            var Retval = new ParserResultTuple(
                new ParserStatus(ParserStatus.ResultType.Error, $"Expected expression"),
                null,
                span
                );
            return Retval;
        }
        public static ParserResultTuple ExpectedIdentifier(ROS span)
        {
            var Retval = new ParserResultTuple(
                new ParserStatus(ParserStatus.ResultType.Error, $"Expected identifier"),
                null,
                span
                );
            return Retval;
        }
        public static ParserResultTuple ExpectedPrintExpression(ROS span)
        {
            var Retval = new ParserResultTuple(
                new ParserStatus(ParserStatus.ResultType.Error, $"Expected print expression"),
                null,
                span
                );
            return Retval;
        }
        public static ParserResultTuple ExpectedReservedWord(string word, ROS span)
        {
            var Retval = new ParserResultTuple(
                new ParserStatus(ParserStatus.ResultType.Error, $"Expected reserved word {word}"),
                null,
                span
                );
            return Retval;
        }
        public static ParserResultTuple ExpectedStatement(ROS span)
        {
            var Retval = new ParserResultTuple(
                new ParserStatus(ParserStatus.ResultType.Error, $"Expected statement"),
                null,
                span
                );
            return Retval;
        }
        public static ParserResultTuple UnexpectedAtEOL(ROS span)
        {
            var Retval = new ParserResultTuple(
                new ParserStatus(ParserStatus.ResultType.Error, $"Unexpected at end of line"),
                null,
                span
                );
            return Retval;
        }
        public ParserStatus Status;
        public ParsedResult Result;
        public ROS RemainingSpan;
        public override string ToString()
        {
            if (Status.Result != ParserStatus.ResultType.Success)
            {
                return $"{Status.Result} message {Status.Error}";
            }
            var str = $"{Status.Result} {Result.ToString()} ** {RemainingSpan.ToString()}";
            return str;
        }
    }

    public class ParserStatus
    {
        public ParserStatus(ResultType result)
        {
            Result = result;
            switch (result)
            {
                case ResultType.Error: Error = "ERROR"; break;
                case ResultType.Success: Error = "OK"; break;
            }
        }
        public ParserStatus(ResultType result, string error)
        {
            Result = result;
            Error = error;
        }





        public static ParserStatus NotAKnownStatement(string word)
        {
            return new ParserStatus(ResultType.Error, $"Not a known statement {word}");
        }



        public enum ResultType { Success, Error };
        public ResultType Result { get; set; } = ResultType.Success;
        public string Error { get; set; }
    }

    /// <summary>
    /// Base class for all results. The Cmd value determines the sub-class.
    /// </summary>
    public class ParsedResult
    {
        public Lexer.Cmd Cmd = Lexer.Cmd.NotCommand; // defaults to nothing...
        /// <summary>
        /// The user-supplied line number (e.g., 10, 20, 100)
        /// </summary>
        public string LineNumber = null;
        /// <summary>
        /// The original line, modified to be marked up with an error indicator
        /// </summary>
        public string ResultLine = "";
        /// <summary>
        /// The error message from the original line
        /// </summary>
        public string StatementError = null;
        /// <summary>
        /// Line number in the original source file. 
        /// </summary>
        public int SourceCodeLine = 789;
    }


    public class ParsedExpression : ParsedResult
    {
        // IdentifierEtc includes a, a[1], a.b[2]
        public enum ExpressionType { ColorName, Literal, IdentifierEtc, Parenthesis, Binary, Prefix, Suffix }
        public ExpressionType Type;
    }

    //Example: a [b]
    public class ParsedExpressionArray: ParsedExpression
    {
        public ParsedExpressionArray(Word name, ParsedExpressionList indexes)
        {
            Type = ExpressionType.IdentifierEtc;
            Identifier = name;
            Indexes = indexes;
        }
        public Word Identifier;
        public ParsedExpressionList Indexes;

        public override string ToString()
        {
            return $"{Identifier.Text} {Indexes.ToString()}";
        }
    }

    //Example: a + b
    public class ParsedExpressionBinary : ParsedExpression
    {
        public ParsedExpressionBinary(ParsedExpression left, Word op, ParsedExpression right)
        {
            Type = ExpressionType.Binary;
            Left = left;
            Op = op;
            Right = right;
        }
        public ParsedExpression Left;
        public Word Op;
        public ParsedExpression Right;

        public override string ToString()
        {
            return $"( {Left.ToString()} {Op.Text} {Right.ToString()} )";
        }
    }
    
    
    // Example: BLACK
    public class ParsedExpressionColorName : ParsedExpression
    {
        public ParsedExpressionColorName(Word value)
        {
            Type = ExpressionType.Literal;
            ColorName = value;
        }
        public Word ColorName;

        public override string ToString()
        {
            return $"COLOR {ColorName.ToNiceString()}";
        }
    }

    //Example: a(b) [and a() or a(b,c)]
    public class ParsedExpressionFunction : ParsedExpression
    {
        public ParsedExpressionFunction(Word name, ParsedExpressionList indexes)
        {
            Type = ExpressionType.IdentifierEtc;
            Identifier = name;
            Indexes = indexes;
        }
        public Word Identifier;
        public ParsedExpressionList Indexes;

        public override string ToString()
        {
            return $"{Identifier.Text} {Indexes.ToString()}";
        }
    }


    //Example: a
    public class ParsedExpressionIdentifier : ParsedExpression
    {
        public ParsedExpressionIdentifier(Word value)
        {
            Type = ExpressionType.IdentifierEtc;
            Identifier = value;
        }
        public Word Identifier;

        public override string ToString()
        {
            return Identifier.Text;
        }
    }

    //Example: INPUT DEFAULT "foo" PROMPT "Enter a foo"
    public class ParsedExpressionInput : ParsedExpression
    {
        public ParsedExpressionInput(Word input, ParsedExpression defaultValue, ParsedExpression prompt)
        {
            Type = ExpressionType.IdentifierEtc;
            Input = input;
            Default = defaultValue ?? new ParsedExpressionLiteralString(new Word("", Lexer.SymbolType.WS));
            Prompt = prompt ?? new ParsedExpressionLiteralString(new Word("", Lexer.SymbolType.WS)); ;
        }
        public Word Input;
        public ParsedExpression Default;
        public ParsedExpression Prompt;

        public override string ToString()
        {
            string defstr = Default.ToString();
            string promptstr = Prompt.ToString();
            string defPart = defstr == "" ? "" : $" DEFAULT {defstr}";
            string promptPart = promptstr == "" ? "" : $" PROMPT {promptstr}";
            return $"INPUT{defPart}{promptPart}";
        }
    }

    // Example: 1.234E-12
    public class ParsedExpressionLiteralNumeric : ParsedExpression
    {
        public ParsedExpressionLiteralNumeric(Word value)
        {
            Type = ExpressionType.Literal;
            Literal = value;
        }
        public Word Literal;

        public override string ToString()
        {
            return Literal.ToNiceString();
        }
    }

    // Example: 1.234E-12
    public class ParsedExpressionLiteralString : ParsedExpression
    {
        public ParsedExpressionLiteralString(Word value)
        {
            Type = ExpressionType.Literal;
            Literal = value;
        }
        public Word Literal;

        public override string ToString()
        {
            return Literal.ToNiceString();
        }
    }

    //Example: ( exp )
    public class ParsedExpressionParenthesis : ParsedExpression
    {
        public ParsedExpressionParenthesis(ParsedExpression expression)
        {
            Type = ExpressionType.Parenthesis;
            Expression = expression;
        }
        public ParsedExpression Expression;

        public override string ToString()
        {
            return $"( {Expression.ToString()} )";
        }
    }



    //Example: √ a
    public class ParsedExpressionPrefix : ParsedExpression
    {
        public ParsedExpressionPrefix(Word op, ParsedExpression right)
        {
            Type = ExpressionType.Prefix;
            Op = op;
            Right = right;
        }
        public Word Op;
        public ParsedExpression Right;

        public override string ToString()
        {
            return $"( {Op.Text} {Right.ToString()} )";
        }
    }


    //Example: a ²
    public class ParsedExpressionSuffix : ParsedExpression
    {
        public ParsedExpressionSuffix(ParsedExpression left, Word op)
        {
            Type = ExpressionType.Suffix;
            Left = left;
            Op = op;
        }
        public ParsedExpression Left;
        public Word Op;

        public override string ToString()
        {
            return $"( {Left.ToString()} {Op.Text} )";
        }
    }

    public class ParsedExpressionList : ParsedResult
    {
        public IList<ParsedExpression> List = new List<ParsedExpression>();
        public override string ToString()
        {
            if (List.Count == 0) return "()";
            var str = "";
            foreach (var item in List)
            {
                if (str != "") str += " , ";
                str += item.ToString();
            }
            return $"( {str} )";
        }
    }


    public class ParsedBlankLine : ParsedResult
    {
        public ParsedBlankLine(Word value)
        {
            Blank = value;
        }
        public Word Blank;

        public override string ToString()
        {
            return $"BLANK LINE";
        }
    }

    public class ParsedParseError : ParsedResult
    {
        public ParsedParseError(string message, string originalStatement, int errorIndex)
        {
            Cmd = Lexer.Cmd.ParseError;
            Message = message;
            ErrorIndex = errorIndex;
            if (originalStatement != null && ErrorIndex >= 0 && ErrorIndex < originalStatement.Length)
            {
                // Note: need a nice marker.
                MarkedStatement = originalStatement.Substring(0, ErrorIndex) + " 🚫 " + originalStatement.Substring(ErrorIndex);
            }
            else if (originalStatement == null)
            {
                //this should never ever be hit
                MarkedStatement = "ERROR IN LINE";
            }
            else
            {
                // this should never be hit
                MarkedStatement = "ERROR IN " + originalStatement;
            }
        }
        public string Message;
        public string OriginalStatement;
        public int ErrorIndex;
        public string MarkedStatement;


        public override string ToString()
        {
            return $"ERROR {Message}";
        }
    }

    public class ParsedASSERT : ParsedResult
    {
        public ParsedASSERT(Word statement, ParsedExpression rhs)
        {
            Statement = statement;
            RHS = rhs;
        }
        public Word Statement; // will be either blank or CALL
        public ParsedExpression RHS;


        public override string ToString()
        {
            return $"ASSERT ( {RHS.ToString()} )";
        }
    }

    public class ParsedBEEP : ParsedResult
    {
        public ParsedBEEP(ParsedExpression duration, ParsedExpression pitch)
        {
            Duration = duration;
            Pitch = pitch;
        }
        public ParsedExpression Duration;
        public ParsedExpression Pitch;


        public override string ToString()
        {
            var dp = (Duration == null || Pitch == null) ? "" : $" {Duration} , {Pitch}";
            return $"BEEP{dp}";
        }
    }
    
    // Technically, any expression. Will handle CALL SIN(x) and SIN(x) as well as SIN(x)+COS(x)
    public class ParsedCALL : ParsedResult
    {
        public ParsedCALL(Word statement, ParsedExpression rhs)
        {
            Statement = statement;
            RHS = rhs;
        }
        public Word Statement; // will be either blank or CALL
        public ParsedExpression RHS;


        public override string ToString()
        {
            return $"CALL {RHS.ToString()}";
        }
    }

    // Paper_Statement -> PAPER ( COLOR | Expression )
    // Expression can also be a color name
    public class ParsedCLS : ParsedResult
    {
        public ParsedCLS(ParsedExpression background, ParsedExpression foreground)
        {
            Background = background;
            Foreground = foreground;
        }
        public ParsedExpression Background;
        public ParsedExpression Foreground;


        public override string ToString()
        {
            var bg = Background == null ? "" : " " + Background.ToString();
            var fg = Foreground == null ? "" : " " + Foreground.ToString();
            return $"CLS{bg}{fg}";
        }
    }

    public class ParsedCONSOLE : ParsedExpression
    {
        public ParsedCONSOLE(ParsedExpressionList values)
        {
            Values = values;
        }
        public ParsedExpressionList Values;

        public override string ToString()
        {
            return $"CONSOLE {Values.ToString()}";
        }
    }

    public class ParsedDATA : ParsedResult
    {
        public ParsedDATA(IList<ParsedExpression> data)
        {
            Data = data;
        }
        public IList<ParsedExpression> Data;


        public override string ToString()
        {
            var args = "";
            foreach (var item in Data)
            {
                if (args != "") args += " , ";
                args += item.ToString();
            }
            return $"DATA {args}";
        }
    }

    public class ParsedDIM : ParsedResult
    {
        public ParsedDIM(Word identifier, ParsedExpression size1, ParsedExpression size2)
        {
            Identifier = identifier;
            Size1 = size1;
            Size2 = size2;
        }
        public Word Identifier;
        public ParsedExpression Size1 = null;
        public ParsedExpression Size2 = null;


        public override string ToString()
        {
            if (Size1 == null) return $"DIM {Identifier.Text} ()";
            var sizetext = Size1.ToString();
            if (Size2 != null) sizetext = $"{sizetext}, {Size2.ToString()}";
            return $"DIM {Identifier.Text} ( {sizetext} )";
        }
    }


    public class ParsedDUMP : ParsedResult
    {
        public ParsedDUMP()
        {
        }

        public override string ToString()
        {
            return $"DUMP";
        }
    }


    public class ParsedELSE : ParsedResult
    {
        public ParsedELSE()
        {
        }

        public override string ToString()
        {
            return $"ELSE";
        }
    }


    // END IF
    public class ParsedENDIF : ParsedResult
    {
        public ParsedENDIF()
        {
            Cmd = Lexer.Cmd.ENDIF;
        }

        public override string ToString()
        {
            return $"END IF";
        }
    }

    public class ParsedEXPRESSION : ParsedResult
    {
        public ParsedEXPRESSION(ParsedExpression rhs)
        {
            RHS = rhs;
        }
        public ParsedExpression RHS;


        public override string ToString()
        {
            return $"{RHS.ToString()}";
        }
    }

    public class ParsedFOR : ParsedResult
    {
        public ParsedFOR(Word identifier, ParsedExpression from, ParsedExpression to, ParsedExpression step)
        {
            Identifier = identifier;
            From = from;
            To = to;
            Step = step;
        }
        public Word Identifier;
        public ParsedExpression From = null;
        public ParsedExpression To = null;
        public ParsedExpression Step = null;


        public override string ToString()
        {
            var step = Step == null ? "" : $" STEP {Step}";
            return $"FOR {Identifier.Text} = {From} TO {To}{step}";
        }
    }


    // FOREVER STOP|WAIT
    public class ParsedFOREVER : ParsedResult
    {
        public ParsedFOREVER(Word foreverType)
        {
            ForeverType = foreverType;
        }
        public Word ForeverType; // will be either STOP or WAIT (and never blank!)


        public override string ToString()
        {
            return $"FOREVER {ForeverType.Text}";
        }
    }


    public class ParsedFUNCTION : ParsedResult
    {
        public ParsedFUNCTION(Word statement, Word functionName, IList<Word> args)
        {
            FunctionName = functionName;
            Args = args;
        }
        public Word FunctionName; // will be either blank or CALL
        public IList<Word> Args;


        public override string ToString()
        {
            var args = "";
            foreach (var item in Args)
            {
                if (args != "") args += " , ";
                args += item.Text;
            }
            if (args != "") args = $" ( {args} )";
            else args = " ()";
            return $"FUNCTION {FunctionName.Text}{args}";
        }
    }


    public class ParsedGLOBAL : ParsedResult
    {
        public ParsedGLOBAL(List<Word> identifiers)
        {
            Identifiers = identifiers;
        }
        public List<Word> Identifiers;
        public List<string> AsNames()
        {
            var retval = new List<string>();
            foreach (var identifier in Identifiers)
            {
                retval.Add(identifier.Text);
            }
            return retval;
        }


        public override string ToString()
        {
            var names = "";
            foreach (var identifer in Identifiers)
            {
                if (names != "") names = ", ";
                names += identifer.Text;
            }
            return $"GLOBAL {names}";
        }
    }


    public class ParsedGOSUB : ParsedResult
    {
        public ParsedGOSUB(Word line)
        {
            Line = line;
        }
        public Word Line;


        public override string ToString()
        {
            return $"GOSUB {Line.Text}";
        }
    }


    public class ParsedGOTO : ParsedResult
    {
        public ParsedGOTO(Word line)
        {
            Line = line;
        }
        public Word Line;


        public override string ToString()
        {
            return $"GOTO {Line.Text}";
        }
    }


    public class ParsedIF : ParsedResult
    {
        public ParsedIF(ParsedExpression value, ParsedResult statement, ParsedResult elseStatement)
        {
            Value = value;
            Statement = statement;
            ElseStatement = elseStatement;
        }

        public ParsedExpression Value = null;
        public ParsedResult Statement = null;
        public ParsedResult ElseStatement = null;


        public override string ToString()
        {
            var thenStr = Statement == null ? "" : $" THEN {Statement}";
            var elseStr = ElseStatement == null ? "" : $" ELSE {ElseStatement}";
            return $"IF ( {Value} ){thenStr}{elseStr}";
        }
    }
 

    public class ParsedIMPORTFUNCTIONSFROM : ParsedResult
    {
        public ParsedIMPORTFUNCTIONSFROM(Word packageName)
        {
            Cmd = Lexer.Cmd.IMPORTFUNCTIONSFROM;
            PackageName = packageName;
        }
        public Word PackageName;


        public override string ToString()
        {
            return $"IMPORT FUNCTIONS FROM {PackageName.Text}";
        }
    }


    public class ParsedINPUT : ParsedResult
    {
        public ParsedINPUT(List<Word> identifiers)
        {
            foreach (var identifier in identifiers)
            {
                Identifiers.Add(identifier);
            }
        }
        public List<Word> Identifiers = new List<Word>();


        public override string ToString()
        {
            string names = "";
            foreach (var identifier in Identifiers)
            {
                if (names != "") names += ",";
                names += identifier;
            }
            return $"INPUT {names}";
        }
    }

    public class ParsedLET : ParsedResult
    {
        public ParsedLET(Word statement, ParsedExpression lhs, Word assignment, ParsedExpression rhs)
        {
            Statement = statement;
            LHS = lhs;
            Assignment = assignment;
            RHS = rhs;
        }
        public Word Statement; // will be either blank or LET
        public ParsedExpression LHS; // Must be an identifier type expression e.g. a or a[1] or a(b)
        public Word Assignment; // will be =
        public ParsedExpression RHS;


        public override string ToString()
        {
            return $"LET {LHS.ToString()} {Assignment.Text} {RHS.ToString()}";
        }
    }

    public class ParsedNEXT : ParsedResult
    {
        public ParsedNEXT(Word identifier)
        {
            Identifier = identifier;
        }
        public Word Identifier;


        public override string ToString()
        {
            return $"NEXT {Identifier.Text}";
        }
    }


    // Paper_Statement -> PAPER ( COLOR | Expression )
    // Expression can also be a color name
    public class ParsedPAPER : ParsedResult
    {
        public ParsedPAPER(ParsedExpression value)
        {
            Value = value;
        }
        public ParsedExpression Value;


        public override string ToString()
        {
            return $"PAPER {Value}";
        }
    }


    public class ParsedPAUSE : ParsedResult
    {
        public ParsedPAUSE(ParsedExpression value)
        {
            Value = value;
        }
        public ParsedExpression Value;


        public override string ToString()
        {
            return $"PAUSE {Value}";
        }
    }

    public class ParsedPLAY : ParsedResult
    {
        public ParsedPLAY(Word playType, ParsedExpression value)
        {
            PlayType = playType;
            Value = value;
        }
        public Word PlayType;
        public ParsedExpression Value;


        public override string ToString()
        {
            var v = Value == null ? "" : " " + Value.ToString();
            return $"PLAY {PlayType.Text}{v}";
        }
    }


    public class ParsedPRINTExpression : ParsedResult
    {
        // SpaceType is e.g. the ; or , before an expression. Will be ? \n ; ,
        // ? is for the first expression in a statement (unless there's an AT)
        public ParsedPRINTExpression(Word spaceType, ParsedExpression row, ParsedExpression col, ParsedExpression print)
        {
            SpaceType = spaceType ?? new Word ("?", Lexer.SymbolType.Punctuation);
            Row = row;
            Col = col;
            Print = print;
        }
        public Word SpaceType;
        public ParsedExpression Row;
        public ParsedExpression Col;
        public ParsedExpression Print;

        public override string ToString()
        {
            var space = "";
            switch (SpaceType.Text)
            {
                case "?": space = ""; break;
                case "\n": space = ""; break;
                case ",": space = SpaceType.Text + " "; break;
                case ";": space = SpaceType.Text + " "; break;
                case "AT": space = ""; break;
                default:
                    space = $"ERROR {SpaceType.Text} ?? ";
                    break;
            }
            int nnull = 0;
            if (Row == null) nnull++;
            if (Col == null) nnull++;
            if (nnull == 1) return $"PRINT ERROR should be both a row and a col! {Row}::{Col}::{Print}";
            var at = nnull == 2 ? "" : $"AT {Row} , {Col} ";
            return $"{space}{at}{Print} ";
        }
    }

    public class ParsedPRINT : ParsedResult
    {
        public ParsedPRINT(IList<ParsedPRINTExpression> printExpressions)
        {
            Expressions = printExpressions;
        }
        public IList<ParsedPRINTExpression> Expressions;


        public override string ToString()
        {
            var str = "";
            foreach (var expression in Expressions)
            {
                str += expression.ToString();
            }
            return $"PRINT {str}";
        }
    }

    // Rand_Statement -> RAND Expression
    public class ParsedRAND : ParsedResult
    {
        public ParsedRAND(ParsedExpression value)
        {
            Value = value;
        }
        public ParsedExpression Value;


        public override string ToString()
        {
            return $"RAND {Value}";
        }
    }

    public class ParsedREAD : ParsedResult
    {
        public ParsedREAD(List<Word> identifiers)
        {
            foreach (var identifier in identifiers)
            {
                Identifiers.Add (identifier);
            }
        }
        public List<Word> Identifiers = new List<Word>();


        public override string ToString()
        {
            string names = "";
            foreach (var identifier in Identifiers)
            {
                if (names != "") names += ",";
                names += identifier;
            }
            return $"READ {names}";
        }
    }



    // REM this is a comment
    public class ParsedREM : ParsedResult
    {
        public ParsedREM(Word statement, Word rhs)
        {
            Statement = statement;
            RHS = rhs;
        }
        public Word Statement; // will be either blank or CALL
        public Word RHS;


        public override string ToString()
        {
            return $"REM{RHS.Text}";
        }
    }

    public class ParsedRESTORE : ParsedResult
    {
        public ParsedRESTORE()
        {
        }

        public override string ToString()
        {
            return $"RESTORE";
        }
    }

    // RETURN and RETURN 10
    public class ParsedRETURN : ParsedResult
    {
        public ParsedRETURN(ParsedExpression value)
        {
            Value = value;
        }
        public ParsedExpression Value;


        public override string ToString()
        {
            if (Value == null) return "RETURN";
            return $"RETURN {Value}";
        }
    }


    public class ParsedSPEAKLISTVOICES : ParsedResult
    {
        public ParsedSPEAKLISTVOICES()
        {
        }

        public override string ToString()
        {
            return $"SPEAK LIST VOICES";
        }
    }

    public class ParsedSPEAK : ParsedResult
    {
        public ParsedSPEAK(ParsedExpression voice, ParsedExpression value)
        {
            Voice = voice;
            Value = value;
        }
        public ParsedExpression Voice;
        public ParsedExpression Value;


        public override string ToString()
        {
            var voice = Voice == null ? "" : $" VOICE {Voice}";
            return $"SPEAK{voice} {Value}";
        }
    }

    // (END | STOP) Expression?
    public class ParsedSTOP : ParsedResult
    {
        public ParsedSTOP(Word stop, ParsedExpression value, bool isSilent)
        {
            Cmd = (Lexer.Cmd)stop.Cmd;
            Stop = stop;
            Value = value;
            IsSilent = isSilent;
        }

        public bool IsSilent = false;
        public Word Stop;
        public ParsedExpression Value;

        public override string ToString()
        {
            if (IsSilent) return $"{Stop.Text} SILENT";
            if (Value == null) return $"{Stop.Text}";
            return $"{Stop.Text} {Value}";
        }
    }
}
