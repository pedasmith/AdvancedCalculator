using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Edit
{
    public class ROS
    {
        public ROS(String input)
        {
            Text = input;
            CurrIndex = 0;
        }
        public ROS(ROS rosToCopy, int startingIndex=0) // The startingIndex is relative to the CurrIndex, of course
        {
            Text = rosToCopy.Text;
            CurrIndex = rosToCopy.CurrIndex + startingIndex;
        }
        public int Length { get { return Text.Length - CurrIndex; } }
        public char this[int i] => Text[i + CurrIndex]; // This is an indexer; see https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/indexers/ for details
        public ROS Slice (int startingIndex, int Length)
        {
            var slice = Text.Substring(CurrIndex + startingIndex, Length);
            var Retval = new ROS(slice);
            return Retval;
        }
        public ROS Slice(int startingIndex) // Slices that go to the end don't reallocate the string
        {
            return new ROS(this, startingIndex);
        }
        public override string ToString ()
        {
            return Text.Substring(CurrIndex);
        }
        private string Text;
        private int CurrIndex = 0;
    }
    public class LexerResultTuple
    {
        public LexerResultTuple(LexerStatus status, Word word, ROS span)
        {
            Status = status;
            Word = word;
            RemainingSpan = span;
        }
        public LexerResultTuple(LexerStatus status)
        {
            Status = status;
            Word = null;
            RemainingSpan = null;
        }
        public LexerStatus Status;
        public Word Word;
        public ROS RemainingSpan;
    }

    public class LexerStatus
    {
        public LexerStatus(ResultType result)
        {
            Result = result;
        }
        public enum ResultType { Success, Error };
        public ResultType Result { get; set; } = ResultType.Success;
    }

    public class Lexer
    {
        public enum LexerSwitches {  Normal, PreferOperators };
        // Note that both WS and Flag are considered Whitespace
        public enum SymbolType { CommentBegin, CommentText, External, Flag, Identifier, MalformedString, NewLine, Number, Operator, Punctuation, Reserved, String, WS, Unknown }
        public enum OperatorType { NotOperator, Other, Compare, Addition, Subtraction, Multiplication, Root, Power, RaiseToThePower, Sinclair, Logic }

        delegate bool IsCharType(char input);
        public static bool IsHighSurrogate(char input)
        {
            if (input >= '\uD800' && input <= '\uDBFF') return true;
            return false;
        }
        public static bool IsLowSurrogate(char input)
        {
            if (input >= '\uDC00' && input <= '\uDFFF') return true;
            return false;
        }
        // Given a string with potentially mismatched high/low surrogate chars,
        // Fix it by replacing all bad chars (low not followed by high, or high not preceeded by low)
        // with a ''
        public static byte[] ConvertToUtfRobust (string text)
        {
            var textarray = text.ToCharArray();
            char Replace = '*';
            for (int i = 0; i < textarray.Length; i++)
            {
                var curr = textarray[i];
                var prev = i == 0 ? '\0' : textarray[i - 1];
                var next = i == textarray.Length - 1 ? '\0' : textarray[i + 1];
                if (IsLowSurrogate(curr) && !IsHighSurrogate(prev))
                {
                    textarray[i] = Replace;
                }
                else if (IsHighSurrogate(curr) && !IsLowSurrogate(next))
                {
                    textarray[i] = Replace;
                }
            }
            // Have correct char array now!

            byte[] bytes = null;
            try
            {
                bytes = UnicodeEncoding.UTF8.GetBytes(textarray);
            }
            catch (Exception)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: unable to convert program for saving to disk.");
                bytes = null;
            }
            return bytes;
        }

        // COLOR -> @"(BLACK)|(BLUE)|(RED)|(MAGENTA)|(GREEN)|(CYAN)|(YELLOW)|(WHITE)|(NONE)";
        public bool IsColor (string input)
        {
            bool Retval = (input == "BLACK" || input == "BLUE" || input == "RED" || input == "GREEN" || input == "CYAN" || input == "YELLOW" || input == "WHITE" || input == "NONE");
            return Retval;
        }

        // Skipping over the preliminary 0x
        private static bool IsHexInteger(char input)
        {
            if (input >= '0' && input <= '9') return true;
            if (input >= 'a' && input <= 'f') return true;
            if (input >= 'A' && input <= 'F') return true;
            return false;
        }
        private static bool IsHexIntegerPrefix (ROS input)
        {
            if (input.Length < 3) return false;
            if (input[0] == '0' && (input[1] == 'x' || input[1] == 'X')) return true;
            return false;
        }

        private static bool IsGreek(char input)
        {
            if (input >= '\u0370' && input <= '\u03ff') return true; // main greek page
            return false;
        }

        private static bool IsSubscriptLetter(char input)
        {
            switch (input)
            {
                case 'ᵢ': // U+1D62
                case 'ᵣ': // U+1D63
                case 'ᵤ': // U+1D64
                case 'ᵥ': // U+1D65
                case 'ₐ': // U+2090 
                case 'ₑ': // U+2091
                case 'ₒ': // U+2092
                case 'ₓ': // U+2093
                case 'ₕ': // U+2095 (2094 is a schwa (upside down e))
                case 'ₖ': // U+2096
                case 'ₗ': // U+2097
                case 'ₘ': // U+2098
                case 'ₙ': // U+2099
                case 'ₚ': // U+209A
                case 'ₛ': // U+209B
                case 'ₜ': // U+209C
                case 'ⱼ': // U+2C7C
                    return true;
            }
            return false;
        }

        // VARIABLE -> @"[a-zA-Z][a-zA-Z0-9_]*([.][a-zA-Z][a-zA-Z0-9_]*)*\$?";
        private static bool IsIdentifierStart(char input)
        {
            if (input >= 'a' && input <= 'z') return true;
            if (input >= 'A' && input <= 'Z') return true;
            if (IsGreek(input)) return true;
            return false;
        }
        private static bool IsIdentifierRest(char input)
        {
            if (input >= 'a' && input <= 'z') return true;
            if (input >= 'A' && input <= 'Z') return true;
            if (IsGreek(input)) return true;
            if (IsSubscriptLetter(input)) return true;
            if (input >= '0' && input <= '9') return true;
            if (input == '$') return true;
            if (input == '_') return true;
            return false;
        }

        private static bool IsInteger(char input)
        {
            if (input >= '0' && input <= '9') return true;
            return false;
        }

        // Any of the three minus sign symbols.
        private static bool IsMinusSign(char input)
        {
            if (input == '-') return true;
            if (input == '−') return true;
            if (input == '–') return true;

            return false;
        }
        private static bool IsNewLine(char input)
        {
            // All other newlines have been removed from the input!
            // There are no \v or \r chars at all!
            if (input == '\n') return true;
            return false;
        }

        // 0..9, ., - ∞. All of the chars that might possibly start a number.
        private static bool IsNumber(char input)
        {
            if (input >= '0' && input <= '9') return true;
            if (input == '.' || input == '∞') return true;
            if (IsMinusSign(input)) return true;
            return false;
        }

        private static bool IsPunctuation (char input)
        {
            if (input == '(') return true;
            if (input == ')') return true;
            if (input == '[') return true;
            if (input == ']') return true;
            if (input == ';') return true;
            if (input == ',') return true;
            if (input == '.') return true;
            return false;
        }
        private static OperatorType IsOperator(char input)
        {
            // AND/OR/NOT are handled via ReservedWordOrIdentifier

            // Operator level 5
            if (input == '<') return OperatorType.Compare;
            if (input == '=') return OperatorType.Compare;
            if (input == '>') return OperatorType.Compare;
            if (input == '≅') return OperatorType.Compare;
            if (input == '≇') return OperatorType.Compare;

            // Operator level 6
            if (input == '+') return OperatorType.Addition;
            if (IsMinusSign(input)) return OperatorType.Subtraction;

            // Operator level 9
            if (input == '*') return OperatorType.Multiplication;
            if (input == '×') return OperatorType.Multiplication;
            if (input == '⋅') return OperatorType.Multiplication;
            if (input == '·') return OperatorType.Multiplication;
            if (input == '/') return OperatorType.Multiplication;

            // Operator level 10 is **

            // Root operators
            if (input == '√') return OperatorType.Root;
            if (input == '∛') return OperatorType.Root;
            if (input == '∜') return OperatorType.Root;

            // Power operator
            if (input == '²') return OperatorType.Power;
            if (input == '³') return OperatorType.Power;
            if (input == '⁴') return OperatorType.Power;
            return OperatorType.NotOperator;
        }


        // Starting quote (e.g., doesn't match an end smart quote)
        private static bool IsStartQuote(char input)
        {
            if (input == '"') return true;
            if (input == '“') return true;
            return false;
        }

        private static bool IsWS(char input)
        {
            if (input == ' ') return true;
            if (input == '\t') return true;
            if (input == '\n') return true;
            if (input == '\r') return true;
            if (input == '\v') return true;
            if (IsCarriageReturnArrow(input)) return true;

            return false;
        }
        private static bool IsCarriageReturnArrow(char input)
        {
            if (input == '↲') return true;
            if (input == '↵') return true;
            if (input == '⤶') return true;
            return false;
        }
        // An easy-to-use function builder that makes functions like LexInteger easy
        // to make with no errors.
        // EXAMPLE:
        // private static Word LexInteger(ArraySegment<char> input)
        // {
        //    return LexTemplate(IsInteger, SymbolType.Integer, input);
        //}
        delegate Word LexType(ROS input);
        private static Word LexTemplate(IsCharType IsCharTemplateType, SymbolType SymbolTemplateType, ROS input)
        {
            int i = 0;
            for (i = 0; i < input.Length; i++)
            {
                var ch = input[i]; 
                if (!IsCharTemplateType(ch))
                {
                    if (i == 0) return null; // Whatever it is it's really not what we're looking for!
                    return new Word(input.Slice(0, i), SymbolTemplateType);
                }
            }
            return new Word(input.Slice (0), SymbolTemplateType);
        }


        // As of 2021-12-20, you can add new items in here anywhere; the parsed version of programs is 
        // always regenerated and never saved.
        public enum Cmd
        {
            // Other
            ParseError = -1,
            NotCommand = 0,

            // Primary
            ASSERT = 1, BEEP, CALL, CLS, CONSOLE, DATA, DIM, DUMP, ELSE, END, ENDIF, EXPRESSION,
            FOR, FOREVER, FUNCTION, GLOBAL, GOSUB, GOTO,
            IF, IMPORT, IMPORTFUNCTIONSFROM, INPUT, LET, NEXT, PAPER, PAUSE, PLAY, PRINT,
            RAND, READ, RESTORE, RETURN, REM, SPEAK, STOP,

            // Secondary
            AT=500, DEFAULT, FROM, FUNCTIONS, LIST, ONNOTE, PROMPT, SILENT, STEP, THEN, TO, VOICE, VOICES, WAIT,

            // Tertiary
            BLANKLINE = 800,

            // Operator Logical
            AND = 1000, NOT, OR,

            // Operator Sinclair
            LEN = 1200, STR, CODE, CHR,
        };



        private Dictionary<string, Lexer.Cmd> ReservedPrimaryWords = new Dictionary<string, Lexer.Cmd>()
        {
            { "ASSERT", Cmd.ASSERT },
            { "BEEP", Cmd.BEEP },
            { "CALL", Cmd.CALL }, { "CLS", Cmd.CLS }, { "CONSOLE", Cmd.CONSOLE },
            { "DATA", Cmd.DATA }, { "DIM", Cmd.DIM }, { "DUMP", Cmd.DUMP },
            { "ELSE", Cmd.ELSE }, { "END", Cmd.END }, // BUT NOT END IF
            { "FOR", Cmd.FOR }, { "FOREVER", Cmd.FOREVER }, { "FUNCTION", Cmd.FUNCTION },
            { "GLOBAL", Cmd.GLOBAL }, { "GOSUB", Cmd.GOSUB }, { "GOTO", Cmd.GOTO },
            { "IF", Cmd.IF }, { "IMPORT", Cmd.IMPORT }, { "INPUT", Cmd.INPUT },
            { "LET", Cmd.LET },
            { "NEXT", Cmd.NEXT },
            { "PAPER", Cmd.PAPER }, { "PAUSE", Cmd.PAUSE }, { "PLAY", Cmd.PLAY }, { "PRINT", Cmd.PRINT },
            { "RAND", Cmd.RAND }, { "READ", Cmd.READ }, { "RESTORE", Cmd.RESTORE }, { "RETURN", Cmd.RETURN }, { "REM", Cmd.REM },
            { "SPEAK", Cmd.SPEAK }, { "STOP", Cmd.STOP },
        };
        private Dictionary<string, Lexer.Cmd> ReservedSecondaryWords = new Dictionary<string, Lexer.Cmd>()
        {
            { "AT", Cmd.AT },
            { "DEFAULT", Cmd.DEFAULT },
            { "FROM", Cmd.FROM }, { "FUNCTIONS", Cmd.FUNCTIONS },
            { "LIST", Cmd.LIST },
            { "ONNOTE", Cmd.ONNOTE },
            { "PROMPT", Cmd.PROMPT },
            { "SILENT", Cmd.SILENT },
            { "STEP", Cmd.STEP },
            { "THEN", Cmd.THEN }, { "TO", Cmd.TO },
            { "VOICE", Cmd.VOICE }, { "VOICES", Cmd.VOICES },
            { "WAIT", Cmd.WAIT },


        };
        private Dictionary<string, Lexer.Cmd> ReservedOperatorWords = new Dictionary<string, Lexer.Cmd>()
        {
            { "AND", Cmd.AND },
            { "NOT", Cmd.NOT },
            { "OR", Cmd.OR },
        };

        private Dictionary<string, Lexer.Cmd> ReservedSinclairOperatorWords = new Dictionary<string, Lexer.Cmd>()
        {
            { "CHR$", Cmd.CHR }, { "CODE", Cmd.CODE },
            { "LEN", Cmd.LEN },
            { "STR$", Cmd.STR },
        };

        public IList<string> Externals { get; } = new List<string>(); // External is like Bluetooth and Math

        // Identifiers, reserved words and operators all look
        // about the same. This one method figures them all out.
        // An Identifier matches this VARIABLE 
        // VARIABLE -> @"[a-zA-Z][a-zA-Z0-9_]*([.][a-zA-Z][a-zA-Z0-9_]*)*\$?";
        private Word LexIdentiferEtc(ROS input)
        {
            var remainder = input;
            var lexWord = LexTemplate(IsIdentifierRest, SymbolType.Identifier, input);
            var str = lexWord.ToString();
            if (ReservedPrimaryWords.ContainsKey(str))
            {
                lexWord.SymbolType = SymbolType.Reserved;
                lexWord.Cmd = ReservedPrimaryWords[str];
                if (str == "REM")
                {
                    lexWord.SymbolType = SymbolType.CommentBegin;
                }
            }
            else if (ReservedSecondaryWords.ContainsKey(str))
            {
                lexWord.SymbolType = SymbolType.Reserved;
                lexWord.Cmd = ReservedSecondaryWords[str];
            }
            else if (ReservedOperatorWords.ContainsKey(str)) // AND NOT OR
            {
                lexWord.SymbolType = SymbolType.Operator;
                lexWord.OperatorType = OperatorType.Logic;
                lexWord.Cmd = ReservedOperatorWords[str];
            }
            else if (ReservedSinclairOperatorWords.ContainsKey(str)) // CHR$ CODE LEN STR$
            {
                lexWord.SymbolType = SymbolType.Operator;
                lexWord.OperatorType = OperatorType.Sinclair;
                lexWord.Cmd = ReservedSinclairOperatorWords[str];
            }
            else if (Externals.Contains(str))// e.g. Bluetooth or Math
            {
                lexWord.SymbolType = SymbolType.External;
            }
            else
            {
                // OK, it's part of an identifier. The full oroginal spec is 
                // VARIABLE -> @"[a-zA-Z][a-zA-Z0-9_]*([.][a-zA-Z][a-zA-Z0-9_]*)*\$?";
                // This one is a little different because the $ can be anywhere, not just at the end.
                string identifier = lexWord.Text;
                remainder = remainder.Slice(lexWord.Text.Length);
                var keepGoing = true;
                while (keepGoing)
                {
                    keepGoing = false;
                    var lexDot = LexPunctuation(remainder);
                    if (lexDot != null && lexDot.Text == ".")
                    {
                        remainder = remainder.Slice(lexDot.Text.Length);
                        if (remainder.Length >= 1)
                        {
                            var ch = remainder[0];
                            if (IsIdentifierStart(ch))
                            {
                                var lexMore = LexTemplate(IsIdentifierRest, SymbolType.Identifier, remainder.Slice(1));
                                if (lexMore == null) // e.g. reading g.W where the identifier is only one character long.
                                {
                                    identifier += lexDot.Text + ch;
                                    remainder = remainder.Slice(1); // don't forget the 'ch' (dot is already handled).
                                    keepGoing = true;
                                }
                                else
                                {
                                    identifier += lexDot.Text + ch + lexMore.Text;
                                    remainder = remainder.Slice(1 + lexMore.Text.Length); // don't forget the 'ch' (dot is already handled).
                                    keepGoing = true;
                                }
                            }
                        }
                    }
                }
                lexWord.Text = identifier;
            }

            return lexWord;
        }
        private static bool IsNegative (char ch)
        {
            bool isNegative = (ch == '-') || (ch == '−') || (ch == '–');
            return isNegative;
        }


        //
        // All of the Lexing routines.
        //


        private static Word LexHexInteger(ROS input)
        {
            return LexTemplate(IsHexInteger, SymbolType.Number, input);
        }

        public Word LexInteger(ROS input)
        {
            return LexTemplate(IsInteger, SymbolType.Number, input);
        }
        private static Word LexIntegerStatic(ROS input)
        {
            return LexTemplate(IsInteger, SymbolType.Number, input);
        }

        // Two newlines, unlike most lexing, is two results, not one.
        private static Word LexNewLine(ROS input)
        {
            var ch = input[0]; 
            // Literally has to be the case! IsNewLine(ch))
            return new Word(input.Slice(1), SymbolType.NewLine);
        }


        // There are three kinds of numbers:
        //     NUMBER -> @"[-−–]?[0-9]*[.]?[0-9]+(E[-−–]?[0-9]+)*";
        //     INFINITY -> @"∞";
        //     HEX -> @"0x[0-9a-fA-F]*";
        // Note that the original hex number parser was wrong; it allows 0x as a hex number, which is surprising!
        private static Word LexNumber(ROS input)
        {
            int len = 0;
            bool isNegative = false;
            ROS remainder = input;
            if (remainder.Length > 0 && remainder[0] == '∞')
            {
                return new Word(input.Slice(0,1), SymbolType.Number) { Value = Double.PositiveInfinity };
            }
            if (IsHexIntegerPrefix (input))
            {
                remainder = remainder.Slice(2);
                len += 2;
                Word hex = LexHexInteger(remainder);
                if (hex == null)
                {
                    // NOTE: this isn't the best handling
                    Word error = new Word("0x", SymbolType.Unknown);
                    error.Value = 0;
                    error.Text = "0x";
                    error.Cmd = Cmd.NotCommand;
                    error.OperatorType = OperatorType.Other;
                    return error;
                }
                uint value = 0;
                
                var hexparse = UInt32.TryParse(hex.Text, System.Globalization.NumberStyles.HexNumber, null, out value);
                hex.Value = value;
                hex.Text = "0x" + hex.Text;
                hex.ValueNiceFormat = "F0"; // no decimal places when printed
                return hex;
            }
            Word neg1 = LexOperator(input);
            if (neg1 != null && neg1.OperatorType == OperatorType.Subtraction)
            {
                isNegative = true;
                remainder = remainder.Slice(neg1.Text.Length);
                len += neg1.Text.Length;

                // Negative infinity
                if (remainder.Length > 0 && remainder[0] == '∞')
                {
                    return new Word(input.Slice(0, 2), SymbolType.Number) { Value = Double.NegativeInfinity };
                }
            }
            Word int1 = LexIntegerStatic(remainder);
            if (int1 != null)
            {
                remainder = remainder.Slice(int1.Text.Length);
                len += int1.Text.Length;
            }

            bool hasDot = false;
            if (remainder.Length >= 1 && remainder[0] == '.')
            {
                hasDot = true;
                remainder = remainder.Slice(1);
                len += 1;
            }
            if (int1 == null && !hasDot) return null; // not a number; return quickly.

            Word int2 = LexIntegerStatic(remainder);
            if (int2 != null)
            {
                remainder = remainder.Slice(int2.Text.Length);
                len += int2.Text.Length;
            }

            bool hasE = false;
            bool expIsNegative = false;
            Word exp = null;

            if (remainder.Length >= 2 && remainder[0] == 'E')
            {
                hasE = true;
                remainder = remainder.Slice(1);
                len += 1;
                Word neg2 = LexOperator(remainder);
                if (neg2 != null && neg2.OperatorType == OperatorType.Subtraction)
                {
                    expIsNegative = true;
                    remainder = remainder.Slice(neg2.Text.Length);
                    len += neg2.Text.Length;
                }
                else if (neg2 != null && neg2.OperatorType == OperatorType.Addition)
                {
                    // Allow 1.23E+023
                    expIsNegative = false;
                    remainder = remainder.Slice(neg2.Text.Length);
                    len += neg2.Text.Length;
                }
                exp = LexIntegerStatic(remainder);
                if (exp == null) return null; // Can't have a number 0.0E without a number after.
                // Comes up when parsing, e.g., a = String.Escape ("csv"...)
                remainder = remainder.Slice(exp.Text.Length);
                len += exp.Text.Length;

            }
            if (int1 == null && int2 == null) return null;

            string str = "";
            if (isNegative) str += "-";
            str += int1 == null ? "0" : int1.Text;
            if (hasDot) str += ".";
            if (int2 != null) str += int2.Text;
            if (hasE)
            {
                str += "E";
                if (expIsNegative) str += "-";
                str += exp.Text;
            }
            double d = 0.0;
            var nfi = System.Globalization.NumberFormatInfo.InvariantInfo;
            bool parseStatus = double.TryParse(str, System.Globalization.NumberStyles.Float, nfi, out d);
            var exactString = input.Slice(0, len);
            var Retval = new Word(exactString, SymbolType.Number) { Value = d };
            if (hasE) Retval.ValueNiceFormat = "E2";
            else if (!hasDot) Retval.ValueNiceFormat = "F0"; // no so that 1 prints out as 1, not 1.00
            return Retval;
        }


        private static Word LexOperator(ROS input)
        {
            // Operators are either one or two chars
            //     let a = b the = is one char
            //     if (a >= b) the > is two chars
            //     let a =√b the = is one char and the √ is one char
            //     let a = b * c and let a = b ** c are both valid (one is multiply, the other is power)
            var c1 = input[0];
            var type = IsOperator(c1);
            if (type == OperatorType.NotOperator) return null;
            if (type == OperatorType.Compare && input.Length >= 2)
            {
                var c2 = input[1];
                if (c1 == '>' && c2 == '=') return new Word(input.Slice(0, 2), SymbolType.Operator) { OperatorType = type };
                if (c1 == '<' && c2 == '=') return new Word(input.Slice(0, 2), SymbolType.Operator) { OperatorType = type };
                if (c1 == '<' && c2 == '>') return new Word(input.Slice(0, 2), SymbolType.Operator) { OperatorType = type };
            }
            if (type == OperatorType.Multiplication && input.Length >= 2)
            {
                var c2 = input[1];
                if (c1 == '*' && c2 == '*') return new Word(input.Slice(0, 2), SymbolType.Operator) { OperatorType = OperatorType.RaiseToThePower };
            }
            return new Word(input.Slice(0, 1), SymbolType.Operator) { OperatorType = type };
        }

        // Reads in one char (or maybe later, a grapheme...) of ()[];,.
        private static Word LexPunctuation(ROS input)
        {
            if (input.Length < 1) return null;
            if (IsPunctuation(input[0])) return new Word(input[0].ToString(), SymbolType.Punctuation);
            return null;
        }

        public static Word LexRestOfLine(ROS input)
        {
            try
            {
                for (int i = 0; i < input.Length; i++)
                {
                    var ch = input[i]; //[i];
                    if (ch == '\n')
                    {
                        if (i == 0)
                        {
                            // Just the newline!
                            return new Word(input.Slice(0, 1), SymbolType.NewLine);
                        }
                        else
                        {
                            var prevch = input[i - 1];
                            if (!IsCarriageReturnArrow(prevch))
                            {
                                // The comment line without the newline
                                return new Word(input.Slice(0, i), SymbolType.CommentText);
                            }
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: Exception in LexRestOfLine: {exc.Message}");
            }
            // The last line won't end with a newline.
            return new Word(input, SymbolType.CommentText);
        }

        // If first char isn't a quote, return a zero-length Word
        public Word LexString(ROS input, bool failOnMalformedInput = false)
        {
            try
            {
                if (input.Length == 0)
                {
                    if (failOnMalformedInput) return new Word(input, SymbolType.Unknown); //Is an error
                    return new Word(input, SymbolType.String) { StringValue = "(end of line)" }; //Is an error
                }
                if (input.Length == 1)
                {
                    if (failOnMalformedInput) return new Word(input, SymbolType.Unknown); //Is an error
                    return new Word(input, SymbolType.String) { StringValue = input.ToString() }; //Is an error
                }

                char matchingQuote = input[0]; //.ElementAt(0); //[0];
                if (!IsStartQuote (matchingQuote) && failOnMalformedInput) return new Word("", SymbolType.Unknown);

                if (matchingQuote == '“') matchingQuote = '”';

                int i = 0;
                for (i = 1; i < input.Length; i++) // skip over the starting quote!
                {
                    var ch = input[i]; //.ElementAt(i); //[i];
                    if (ch == matchingQuote)
                    {
                        var nextch = (i + 1 < input.Length) ? input[i + 1] : 'X'; //[i + 1] : 'X' ; 

                        // Easy case: the next char isn't a quote
                        if (ch != nextch)
                        {
                            // Remember that the Retval is ALL the characters and the StringValue is the 
                            // escaped version!
                            var w = new Word(input.Slice(0, i + 1), SymbolType.String);
                            w.StringValue = FixupStringEscapes(w.Text.Substring(1, w.Text.Length - 2), matchingQuote);
                            return w;
                        }
                        // Otherwise skip forward by the duplicate char!
                        i++;
                    }
                }

                // Remember that the Retval is ALL the characters and the StringValue is the 
                // escaped version!

                // Didn't get the final quote; whatever we got, it wasn't a string.
                if (failOnMalformedInput) return new Word("", SymbolType.Unknown);
                var newstring = input.Slice(0, i).ToString();
                var escapestring = FixupStringEscapes(newstring.Substring (1), matchingQuote);// Malformed; no final quote 
                var Retval = new Word(newstring, SymbolType.MalformedString);
                Retval.StringValue = escapestring;
                return Retval;
            }
            catch (Exception exc)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: Exception in LexString: {input.ToString()}::{exc.Message}");
                return new Word("", SymbolType.Unknown);
            }
        }

        private static string FixupStringEscapes(string newstring, char matchingQuote)
        {
            // Only allocate new string unless if we need to...
            if (newstring.Contains(matchingQuote))
            {
                var dbl = "" + matchingQuote + matchingQuote;
                newstring = newstring.Replace(dbl, "" + matchingQuote);
            }
            if (newstring.Contains("&QUOT;"))
            {
                newstring = newstring.Replace("&QUOT;", "\"");
            }
            // NOTE: This is where to handle escapes at some point :-)
            return newstring;
        }
        private bool IsFlagChar(char ch)
        {
            switch (ch)
            {
                case '⚐':
                case '⚑':
                case '⛳':
                case '⛿':
                    return true;

            }
            return false;
        }

        private static Word LexSurrogate(ROS input)
        {
            if (input.Length < 2) return new Word(input, SymbolType.Punctuation); 
            var ch1 = input[0];
            var ch2 = input[1];
            if (!IsHighSurrogate(ch1)) return new Word(input.Slice (0, 1), SymbolType.Punctuation);
            if (IsHighSurrogate(ch1) && !IsLowSurrogate(ch2)) return new Word(input.Slice (0, 1), SymbolType.Punctuation);
            var slice = input.Slice(0, 2);
            string str = slice.ToString();
            var symbolType = SymbolType.Punctuation;
            // FLAGS: ⚐⚑⛳⛿🎌🏁🏳🏴📪📫📬📭🚩
            switch (str)
            {
                case "⚐": symbolType = SymbolType.Flag; break; //One char
                case "⚑": symbolType = SymbolType.Flag; break;
                case "⛳": symbolType = SymbolType.Flag; break;
                case "⛿": symbolType = SymbolType.Flag; break;
                case "🎌": symbolType = SymbolType.Flag; break; // Two chars
                case "🏁": symbolType = SymbolType.Flag; break;
                case "🏳": symbolType = SymbolType.Flag; break;
                case "🏴": symbolType = SymbolType.Flag; break;
                case "📪": symbolType = SymbolType.Flag; break;
                case "📫": symbolType = SymbolType.Flag; break;
                case "📬": symbolType = SymbolType.Flag; break;
                case "📭": symbolType = SymbolType.Flag; break;
                case "🚩": symbolType = SymbolType.Flag; break;
            }

            return new Word(slice, symbolType); // Not fully correct because there are symbols that aren't flags in this region...
        }

        public Word LexWhitespace(ROS input)
        {
            return LexTemplate(IsWS, SymbolType.WS, input);
        }

        public LexerResultTuple LexWordSkipWS(ROS input, LexerSwitches switches = LexerSwitches.Normal)
        {
            try
            {
                int textLengthOffset = 0;
                var result = LexWord(input, switches);
                while (result.Status.Result == LexerStatus.ResultType.Success && result.Word.IsWS && result.RemainingSpan.Length > 0)
                {
                    textLengthOffset += result.Word.Text.Length;
                    result = LexWord(result.RemainingSpan, switches);
                }
                result.Word.TextLengthOffset = textLengthOffset;
                return result;
            }
            catch (Exception exc)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: Exception in LexWordSkipWS: {input.ToString()}::{exc.Message}");
                return new LexerResultTuple(new LexerStatus (LexerStatus.ResultType.Error));
            }
        }




        public static int NLexWord = 0;
        public LexerResultTuple LexWord(ROS input, LexerSwitches switches)
        {
            NLexWord++;

            Word word = null;
            // return blank input right away

            if (input.Length < 1) word = new Word(input.Slice(0, 0), SymbolType.WS);
            else
            {
                var c1 = input[0]; //[0];
                if (IsNewLine(c1)) word = LexNewLine(input);
                else if (IsWS(c1)) word = LexWhitespace(input);
                else if (IsFlagChar(c1)) word = new Word(input.Slice(0,1), SymbolType.Flag);
                else
                {
                    if (word == null && switches != LexerSwitches.PreferOperators && IsNumber(c1)) word = LexNumber(input);
                    if (word == null && IsOperator(c1) != OperatorType.NotOperator) word = LexOperator(input);
                    if (word == null && switches == LexerSwitches.PreferOperators && IsNumber(c1)) word = LexNumber(input);
                    if (word == null && IsPunctuation(c1)) word = LexPunctuation(input);
                    if (word == null && IsHighSurrogate(c1)) word = LexSurrogate(input);
                    if (word == null && IsStartQuote(c1)) word = LexString(input);
                    if (word == null && IsIdentifierStart(c1)) word = LexIdentiferEtc(input);
                }
            }

            if (word != null)
            {
                var rest = input.Slice(word.Text.Length);
                return new LexerResultTuple(new LexerStatus(LexerStatus.ResultType.Success), word, rest);
            }
            return new LexerResultTuple(new LexerStatus(LexerStatus.ResultType.Success), new Word(input.Slice(0, 1), SymbolType.Unknown), input.Slice(1));
        }
        public IList<Word> LexInput(ROS input)
        {
            bool gotRem = false;
            IList<Word> Retval = new List<Word>();
            int count = 0;
            while (true)
            {
                count = count + 1;
                if (count > 1000)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: Most likely an error in LexInput");
                }
                Word word;
                // I'm either parsing a line comment or a series of words.
                if (gotRem)
                {
                    word = LexRestOfLine(input);
                    Retval.Add(word);
                }
                else
                {
                    var status = LexWord(input, LexerSwitches.Normal);
                    word = status.Word;
                    if (status.Status.Result == LexerStatus.ResultType.Success)
                    {
                        
                        Retval.Add(word);
                        // Handle REM <....> correctly. REM is a comment to the end of the line.
                        // Since the input is the entire thing, that's what we will return.
                        if (word.SymbolType == SymbolType.CommentBegin)
                        {
                            gotRem = true;
                            // I got the REM; now grab the rest of the line.
                            // The line might include \n which should be NewLine
                        }
                    }
                }

                var newc = input.Length - word.Text.Length;
                if (newc <= 0) break;
                input = input.Slice (word.Text.Length);
            }
            return Retval;
        }

        /// <summary>
        // Does complex line split, taking into account line continuation chars
        // and the different end-of-lines (and their combinations).
        // So \n\n is two line, and \r\r is two lines, but \r\n is one line.
        /// </summary>
        /// <returns>
        /// list of int,string where the int is the number of lines in each line. Is almost always 1, but is more for lines that continue onto the next line.
        /// </returns>
        public IList<Tuple<int, string>> SplitIntoLines (string programText)
        {
            var Retval = new List<Tuple<int,string>>();
            var language = new LanguageColors();
            var previdx = 0;
            var nl = new char[] { '\n', '\r', '\v' }; // \v is thanks to Word's auto-edits.
            //var continuation = new char[] { '↲', '↵', '⤶' }; // three different kinds of arrows for line continuation
            string firstPart = ""; // some lines are continuations.
            int nline = 1;
            while (true)
            {
                var idx = programText.IndexOfAny(nl, previdx);
                if (idx < 0) // handle the last line
                {
                    if (previdx < programText.Length)
                    {
                        var lastline = programText.Substring(previdx);
                        Retval.Add(new Tuple<int, string>(nline,lastline));
                    }
                    break;
                }
                var foundIdx = idx;
                var ch = programText[idx];
                var chbefore = idx>0 ? programText[idx-1] : ' '; // act like its just a space.

                var iscontinue = IsCarriageReturnArrow(chbefore); // continuation.Contains(chbefore);

                // Skip forward, reading in \n \r \v so long as we 
                // don't see two of anything. So \r\n is one EOL
                // but \r\r is two EOL.
                HashSet<char> found = new HashSet<char>();
                while (!found.Contains(ch) && nl.Contains(ch))
                {
                    found.Add(ch);
                    idx = idx + 1;
                    if (idx >= programText.Length) break;
                    ch = programText[idx];
                }
                var oneline = programText.Substring(previdx, foundIdx - previdx); // Does not include \n \r \v
                var fullLine = firstPart == "" ? oneline : firstPart + '\n' + oneline;
                if (iscontinue)
                {
                    firstPart = fullLine;
                    nline++;
                }
                else
                {
                    if (firstPart != "")
                    {
                        ;
                    }
                    Retval.Add(new Tuple<int, string>(nline, fullLine));
                    firstPart = "";
                    nline = 1;
                }
                previdx = idx;
            }
            return Retval;
        }
    }
}
