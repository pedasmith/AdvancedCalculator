using System;
using System.Collections.Generic;
using System.Linq;


namespace Edit
{
    public class ProgramParser
    {
        public const string ErrorMarker = "🚫";

        /// <summary>
        /// OK, weird thing. The original BC BASIC Sigma compiler accepted
        /// a statement of the form =n as an expression. The new compiler does not.
        /// This method, given any input text, will remove the '=' from 
        /// </summary>
        /// <param name="rawText"></param>
        /// <returns></returns>
        public static string RemoveFirstEquals(string rawText)
        {
            int firstIndex = 0;
            bool keepGoing = true;
            for (int i=0; i<rawText.Length-1 && keepGoing; i++)
            {
                var ch = rawText[i];
                switch (ch)
                {
                    case ' ':
                    case '\t':
                        break;
                    case '=':
                        firstIndex = i + 1;
                        keepGoing = false;
                        break;
                    default:
                        keepGoing = false;
                        break;
                }
            }
            return rawText.Substring (firstIndex);
        }

        /// <summary>
        /// Primary entry point into the parser
        /// </summary>
        /// <param name="rawtext">Raw input BASIC code</param>
        /// <returns>Parsed BASIC text</returns>
        public IList<ParsedResult> Parse(string rawtext)
        {
            var Retval = new List<ParsedResult>();
            var p = new StatementParser();
            p.lexer = new Lexer();

            var lines = p.lexer.SplitIntoLines(rawtext);
            int sourceLineNumber = 1;
            foreach (var tuple in lines)
            {
                string line = tuple.Item2;
                ParserResultTuple result = p.ParseStatement(new ROS(line));
                // Error location means that I will need the remaining span.
                if (result.Result != null)
                {
                    if (result.Status.Result == ParserStatus.ResultType.Error)
                    {
                        result.Result.StatementError = result.Status.Error;
                    }
                    result.Result.SourceCodeLine = sourceLineNumber;
                    Retval.Add(result.Result);
                }
                else
                {
                    // Generate an parse-error statement.
                    var rlen = result.RemainingSpan.Length;
                    var errorIndex = line.Length - rlen;
                    var error = new ParsedParseError(result.Status.Error, line, errorIndex);
                    error.SourceCodeLine = sourceLineNumber;
                    Retval.Add(error);
                }
                sourceLineNumber += tuple.Item1;
            }
            return Retval;
        }
    }
    public class StatementParser
    {
        public Lexer lexer;
        public enum ParseSwitches {  Normal, AllowExtra }
        public ParserResultTuple ParseStatement (ROS line, ParseSwitches switches = ParseSwitches.Normal)
        {
            Lexer.Cmd Cmd = Lexer.Cmd.NotCommand;
            ParserResultTuple Retval = new ParserResultTuple(ParserStatus.NotAKnownStatement(""));

            var remainingSpan = line;
            var lexStatus = lexer.LexWordSkipWS(remainingSpan);
            if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                Retval = ParserResultTuple.ExpectedReservedWord(lexStatus.Word.Text, remainingSpan);
                return Retval;
            }
            Word lineNumber = null;
            if (lexStatus.Word.SymbolType == Lexer.SymbolType.Number)
            {
                // Number is too broad; it includes floats and hex. Re-lex as an integer.
                // I'm not happy about the whole "LexInteger is static" problem.
                // Skip over white space first. LexWhitespace returns null if there's no white space to read.
                var lexWS = lexer.LexWhitespace(remainingSpan);
                var wsSpan = (lexWS == null) ? remainingSpan : remainingSpan.Slice(lexWS.Text.Length);
                var lexLineNumber = lexer.LexInteger(wsSpan);
                if (lexLineNumber != null)
                {
                    remainingSpan = wsSpan.Slice(lexLineNumber.Text.Length); // lexStatus.RemainingSpan;
                    lineNumber = lexLineNumber; //  lexStatus.Word;
                    lexStatus = lexer.LexWordSkipWS(remainingSpan);
                    if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
                    {
                        Retval = ParserResultTuple.ExpectedReservedWord(lexStatus.Word.Text, remainingSpan);
                        Retval.Result.LineNumber = lineNumber.Text;
                        return Retval;
                    }
                }
            }
            switch (lexStatus.Word.SymbolType)
            {
                case Lexer.SymbolType.CommentBegin:
                    Retval = ParseREM(lexStatus.Word, lexStatus.RemainingSpan);
                    Cmd = Lexer.Cmd.REM;
                    break;

                case Lexer.SymbolType.Identifier:
                    // must be a a = b without the LET OR might be a call like SIN(x) or an array like data(1) = 1.1
                    var peekStatus = lexer.LexWordSkipWS(lexStatus.RemainingSpan);
                    switch (peekStatus.Word.Text)
                    {
                        
                        default:
                            // For example, memory[0] = memory[0]+1
                            Retval = ParseLET(lexStatus.Word, remainingSpan);
                            Cmd = Lexer.Cmd.LET;
                            break;
                        case "=":
                            Retval = ParseLET(lexStatus.Word, remainingSpan);
                            Cmd = Lexer.Cmd.LET;
                            break;
                        case "(":
                            Retval = ParseCALL(lexStatus.Word, remainingSpan);
                            Cmd = Lexer.Cmd.CALL;
                            if (Retval.Status.Result == ParserStatus.ResultType.Success && Retval.RemainingSpan.ToString().Contains ('='))
                            {
                                // It parses like a call statement (like "data(1)") but there's a remaining portion that contains a =/
                                // That means that the call isn't valid, and it's likely to be a LET statement.
                                var letRetval = ParseLET(lexStatus.Word, remainingSpan);
                                if (letRetval.Status.Result == ParserStatus.ResultType.Success)
                                {
                                    // It parses as a LET, so go with it!
                                    Retval = letRetval;
                                }
                                // In either case, assume that it's let-like
                                Cmd = Lexer.Cmd.LET;
                            }
                            break;
                    }
                    break;

                case Lexer.SymbolType.Reserved:
                    Cmd = (Lexer.Cmd)lexStatus.Word.Cmd;
                    switch ((Lexer.Cmd)lexStatus.Word.Cmd)
                    {
                        case Lexer.Cmd.ASSERT: Retval = ParseASSERT(lexStatus.Word, lexStatus.RemainingSpan); break;
                        case Lexer.Cmd.BEEP: Retval = ParseBEEP(lexStatus.Word, lexStatus.RemainingSpan); break;
                        case Lexer.Cmd.CALL: Retval = ParseCALL(lexStatus.Word, lexStatus.RemainingSpan); break;
                        case Lexer.Cmd.CLS: Retval = ParseCLS(lexStatus.Word, lexStatus.RemainingSpan); break;
                        case Lexer.Cmd.CONSOLE: Retval = ParseCONSOLE(lexStatus.Word, lexStatus.RemainingSpan); break;
                        case Lexer.Cmd.DATA: Retval = ParseDATA(lexStatus.Word, lexStatus.RemainingSpan); break;
                        case Lexer.Cmd.DIM: Retval = ParseDIM(lexStatus.Word, lexStatus.RemainingSpan); break;
                        case Lexer.Cmd.DUMP: Retval = ParseDUMP(lexStatus.Word, lexStatus.RemainingSpan); break;
                        case Lexer.Cmd.ELSE: Retval = ParseELSE(lexStatus.Word, lexStatus.RemainingSpan); break;
                        case Lexer.Cmd.END:
                            Retval = ParseSTOP(lexStatus.Word, lexStatus.RemainingSpan);
                            if (Retval.Result != null) Cmd = Retval.Result.Cmd; // Might be ENDIF
                            break; //Yes, END is handled by STOP
                        case Lexer.Cmd.FOR: Retval = ParseFOR(lexStatus.Word, lexStatus.RemainingSpan); break;
                        case Lexer.Cmd.FOREVER: Retval = ParseFOREVER(lexStatus.Word, lexStatus.RemainingSpan); break;
                        case Lexer.Cmd.FUNCTION: Retval = ParseFUNCTION(lexStatus.Word, lexStatus.RemainingSpan); break;
                        case Lexer.Cmd.GLOBAL: Retval = ParseGLOBAL(lexStatus.Word, lexStatus.RemainingSpan); break;
                        case Lexer.Cmd.GOSUB: Retval = ParseGOSUB(lexStatus.Word, lexStatus.RemainingSpan); break;
                        case Lexer.Cmd.GOTO: Retval = ParseGOTO(lexStatus.Word, lexStatus.RemainingSpan); break;
                        case Lexer.Cmd.IF: Retval = ParseIF(lexStatus.Word, lexStatus.RemainingSpan); break;
                        case Lexer.Cmd.IMPORT:
                            Retval = ParseIMPORT(lexStatus.Word, lexStatus.RemainingSpan);
                            if (Retval.Result != null) Cmd = Retval.Result.Cmd; // because it will be IMPORTFUNCTIONS, not IMPORT.
                            break;
                        case Lexer.Cmd.INPUT: Retval = ParseINPUT(lexStatus.Word, lexStatus.RemainingSpan); break;
                        case Lexer.Cmd.LET: Retval = ParseLET(lexStatus.Word, lexStatus.RemainingSpan); break;
                        case Lexer.Cmd.NEXT: Retval = ParseNEXT(lexStatus.Word, lexStatus.RemainingSpan); break;
                        case Lexer.Cmd.PAPER: Retval = ParsePAPER(lexStatus.Word, lexStatus.RemainingSpan); break;
                        case Lexer.Cmd.PAUSE: Retval = ParsePAUSE(lexStatus.Word, lexStatus.RemainingSpan); break;
                        case Lexer.Cmd.PLAY: Retval = ParsePLAY(lexStatus.Word, lexStatus.RemainingSpan); break;
                        case Lexer.Cmd.PRINT: Retval = ParsePRINT(lexStatus.Word, lexStatus.RemainingSpan); break;
                        case Lexer.Cmd.RAND: Retval = ParseRAND(lexStatus.Word, lexStatus.RemainingSpan); break;
                        case Lexer.Cmd.READ: Retval = ParseREAD(lexStatus.Word, lexStatus.RemainingSpan); break;
                        case Lexer.Cmd.RESTORE: Retval = ParseRESTORE(lexStatus.Word, lexStatus.RemainingSpan); break;
                        case Lexer.Cmd.RETURN: Retval = ParseRETURN(lexStatus.Word, lexStatus.RemainingSpan); break;
                        case Lexer.Cmd.SPEAK: Retval = ParseSPEAK(lexStatus.Word, lexStatus.RemainingSpan); break;
                        case Lexer.Cmd.STOP:
                            Retval = ParseSTOP(lexStatus.Word, lexStatus.RemainingSpan);
                            // because it might be STOP or END
                            break;// Also parses END IF statements.
                        // Handled in CommentBegin: case Lexer.Cmd.REM: Retval = ParseREM(lexStatus.Word, lexStatus.RemainingSpan); break;
                        default:
                            // Note that END IF is currently considerd to be an END statement!
                            Retval = new ParserResultTuple(ParserStatus.NotAKnownStatement(lexStatus.Word.Text), null, remainingSpan);
                            break;
                    }
                    break;

                case Lexer.SymbolType.WS: // The line is entirely blank
                    Retval = new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                        new ParsedBlankLine(lexStatus.Word), 
                        lexStatus.RemainingSpan);
                    Cmd = Lexer.Cmd.BLANKLINE;
                    break;

                default:
                    Retval = ParserResultTuple.ExpectedReservedWord(lexStatus.Word.Text, remainingSpan);
                    break;
            }
            if (Retval.Status.Result == ParserStatus.ResultType.Error)
            {
                // Maybe it's just an expression. For example, it might just be an "n"
                // from the SIGMA control. Try parsing as expression; if it works, yay,
                // otherwise, go back to the original error.
                // It might also be a (n) which is a "Punctuation", not an "identifier"
                // which is why this is here and not just an alternative for ParseLET.
                var expression = ParseEXPRESSION(lexStatus.Word, remainingSpan);
                if (expression.Status.Result == ParserStatus.ResultType.Success)
                {
                    // Worked; return the expression.
                    Retval = expression;
                    Cmd = Lexer.Cmd.EXPRESSION;
                }
            }
            if (Retval.Result != null)
            {
                Retval.Result.LineNumber = lineNumber == null ? null : lineNumber.Text;
                Retval.Result.Cmd = Cmd;
                if (Retval.Result.ResultLine == null) Retval.Result.ResultLine = line.ToString();
                if (Retval.Status.Result == ParserStatus.ResultType.Success)
                {
                    // If there's non-WS at the end, it's an error.
                    // Except that some commands like IF () THEN ... ELSE ... allow for extra at the end
                    if (switches != ParseSwitches.AllowExtra)
                    {
                        var eolStatus = lexer.LexWordSkipWS(Retval.RemainingSpan);
                        if (!eolStatus.Word.IsWS)
                        {
                            // Of just add the error to the successful line?
                            Retval.Status.Result = ParserStatus.ResultType.Error;
                            var remainingLen = Retval.RemainingSpan.Length;
                            var lineString = line.ToString();
                            var fullLen = lineString.Length;
                            var errorIndex = fullLen - remainingLen;
                            // Marker: the marker for lines with additional bits.
                            var markup = lineString.Substring(0, errorIndex) + " " + ProgramParser.ErrorMarker + " " + lineString.Substring(errorIndex); 
                            Retval.Status.Error = $"ERROR: Additional items after end of line\n{markup}";
                            return Retval;
                            //return ParserResultTuple.UnexpectedAtEOL(Retval.RemainingSpan);
                        }
                    }
                }
            }
            return Retval;
        }


        // Assert_Statement -> ASSERT LPAREN ExpressionEquality RPAREN
        public ParserResultTuple ParseASSERT(Word firstWord, ROS line)
        {
            var ep = new ExpressionParser(lexer);

            var remainingSpan = line;
            var lexStatus = lexer.LexWordSkipWS(remainingSpan);
            if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                return ParserResultTuple.Expected("( equality-expression )", remainingSpan);
            }
            else if (lexStatus.Word.SymbolType == Lexer.SymbolType.Punctuation && lexStatus.Word.Text != "(")
            {
                return ParserResultTuple.Expected("(", remainingSpan);
            }
            remainingSpan = lexStatus.RemainingSpan;
            // RHS (exp)

            ParsedExpression rhs = null;

            var parseStatus = ep.ParseExpressionCompare(remainingSpan);
            if (parseStatus.Status.Result != ParserStatus.ResultType.Success)
            {
                return ParserResultTuple.ExpectedExpression(remainingSpan);
            }
            var eq = parseStatus.Result as ParsedExpressionBinary;
            if (eq == null || eq.Op.OperatorType != Lexer.OperatorType.Compare)
            {
                return ParserResultTuple.Expected ("equality-expression", remainingSpan);
            }
            rhs = parseStatus.Result as ParsedExpression;
            remainingSpan = parseStatus.RemainingSpan;

            lexStatus = lexer.LexWordSkipWS(remainingSpan);
            if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                return ParserResultTuple.Expected("( equality-expression )", remainingSpan);
            }
            else if (lexStatus.Word.SymbolType != Lexer.SymbolType.Punctuation || lexStatus.Word.Text != ")")
            {
                return ParserResultTuple.Expected(")", remainingSpan);
            }
            remainingSpan = lexStatus.RemainingSpan;

            return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                new ParsedASSERT(firstWord, rhs),
                remainingSpan);
        }

        // Beep_Statement -> BEEP ( Expression COMMA Expression )*
        public ParserResultTuple ParseBEEP(Word firstWord, ROS line)
        {
            var ep = new ExpressionParser(lexer);
            ParsedExpression duration = null;
            ParsedExpression pitch = null;

            ROS remainingSpan = line;
            var parseStatus = ep.ParseExpression(remainingSpan);
            if (parseStatus.Status.Result == ParserStatus.ResultType.Success)
            {
                duration = parseStatus.Result as ParsedExpression;
                remainingSpan = parseStatus.RemainingSpan;


                // read in the comma
                var lexerStatus = lexer.LexWordSkipWS(remainingSpan);
                if (lexerStatus.Status.Result != LexerStatus.ResultType.Success)
                {
                    return ParserResultTuple.Expected(",", remainingSpan);
                }
                else if (lexerStatus.Word.SymbolType != Lexer.SymbolType.Punctuation || lexerStatus.Word.Text != ",")
                {
                    return ParserResultTuple.Expected(",", remainingSpan);
                }
                remainingSpan = lexerStatus.RemainingSpan;


                parseStatus = ep.ParseExpression(remainingSpan);
                if (parseStatus.Status.Result == ParserStatus.ResultType.Success)
                {
                    pitch = parseStatus.Result as ParsedExpression;
                    remainingSpan = parseStatus.RemainingSpan;
                }
                else
                {
                    return ParserResultTuple.ExpectedExpression(remainingSpan);
                }
            }
            else
            {
                // check to see if it's null
                var lexerStatus = lexer.LexWordSkipWS(line);
                if (lexerStatus.Status.Result != LexerStatus.ResultType.Success)
                {
                    return ParserResultTuple.ExpectedExpression(line);
                }
                else if (!lexerStatus.Word.IsWS)
                {
                    return ParserResultTuple.ExpectedExpression(line);
                }
                else
                {
                    ; // all OK; it's just BEEP all by itself.
                }
            }

            return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                new ParsedBEEP(duration, pitch),
                remainingSpan);

        }

        // CALL SIN(x)
        // SIN(x)
        // SIN(x) * 2
        public ParserResultTuple ParseCALL(Word firstWord, ROS line)
        {
            var ep = new ExpressionParser(lexer);

            // RHS (exp)

            ParsedExpression rhs = null;
            ROS remainingSpan = line;

            var parseStatus = ep.ParseExpressionFunctionCall(remainingSpan);
            if (parseStatus.Status.Result != ParserStatus.ResultType.Success)
            {
                return parseStatus; // ParserResultTuple.ExpectedExpression(remainingSpan);
            }
            rhs = parseStatus.Result as ParsedExpression;
            remainingSpan = parseStatus.RemainingSpan;

            return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                new ParsedCALL(firstWord, rhs),
                remainingSpan);
        }

        // Cls_Statement -> CLS (( COLOR | Expression ) ( COLOR | Expression)? )? 
        public ParserResultTuple ParseCLS(Word firstWord, ROS line)
        {
            var ep = new ExpressionParser(lexer);

            // BG (exp)

            ParsedExpression bg = null;
            ParsedExpression fg = null;
            ROS remainingSpan = line;
            var lexStatus = lexer.LexWordSkipWS(remainingSpan);
            if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                return ParserResultTuple.ExpectedExpression(remainingSpan);
            }
            // Allow CLS BLACK but not PAPER BLACK + 10
            if (lexStatus.Word.IsWS)
            {
                // Also allow just CLS
                bg = null;
            }
            else if (lexer.IsColor(lexStatus.Word.Text))
            {
                bg = new ParsedExpressionColorName(lexStatus.Word);
                remainingSpan = lexStatus.RemainingSpan;
            }
            else
            {
                var parseStatus = ep.ParseExpression(remainingSpan);
                if (parseStatus.Status.Result != ParserStatus.ResultType.Success)
                {
                    return ParserResultTuple.ExpectedExpression(remainingSpan);
                }
                bg = parseStatus.Result as ParsedExpression;
                remainingSpan = parseStatus.RemainingSpan;
            }

            if (bg != null)
            {
                lexStatus = lexer.LexWordSkipWS(remainingSpan);
                if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
                {
                    return ParserResultTuple.ExpectedExpression(remainingSpan);
                }
                // Allow CLS BLACK but not PAPER BLACK + 10
                if (lexStatus.Word.IsWS)
                {
                    // Also allow just CLS
                    fg = null;
                }
                else if (lexer.IsColor(lexStatus.Word.Text))
                {
                    fg = new ParsedExpressionColorName(lexStatus.Word);
                    remainingSpan = lexStatus.RemainingSpan;
                }
                else
                {
                    var parseStatus = ep.ParseExpression(remainingSpan);
                    if (parseStatus.Status.Result != ParserStatus.ResultType.Success)
                    {
                        return ParserResultTuple.ExpectedExpression(remainingSpan);
                    }
                    fg = parseStatus.Result as ParsedExpression;
                    remainingSpan = parseStatus.RemainingSpan;
                }
            }

            return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                new ParsedCLS(bg, fg),
                remainingSpan);
        }

        // Console_Statement -> CONSOLE (Expression (COMMA Expression)*)?
        public ParserResultTuple ParseCONSOLE(Word firstWord, ROS line)
        {
            // RHS (exp)
            var ep = new ExpressionParser(lexer);
            ROS remainingSpan = line;
            var parseStatus = ep.ParseExpressionList(remainingSpan, "");
            if (parseStatus.Status.Result != ParserStatus.ResultType.Success)
            {
                return ParserResultTuple.ExpectedExpression(remainingSpan);
            }
            var rhs = parseStatus.Result as ParsedExpressionList;
            remainingSpan = parseStatus.RemainingSpan;

            return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                new ParsedCONSOLE(rhs),
                remainingSpan);

        }



        // Data_Statement -> DATA ConstantList 
        // ConstantList -> Constant (COMMA Constant)*
        // Constant -> NUMBER | HEX | INFINITY | StringValue
        public ParserResultTuple ParseDATA(Word firstWord, ROS line)
        {
            var args = new List<ParsedExpression>();

            LexerResultTuple lexStatus;
            var remainingSpan = line;

            // args (which are all just identifiers)
            bool keepGoing = true;
            while (keepGoing)
            {
                lexStatus = lexer.LexWordSkipWS(remainingSpan);
                if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
                {
                    return ParserResultTuple.ExpectedConstant(remainingSpan);
                }
                else if (lexStatus.Word.SymbolType == Lexer.SymbolType.Number)
                {
                    args.Add(new ParsedExpressionLiteralNumeric(lexStatus.Word));
                }
                else if (lexStatus.Word.SymbolType == Lexer.SymbolType.String)
                {
                    args.Add(new ParsedExpressionLiteralString(lexStatus.Word));
                }
                else if (lexStatus.Word.SymbolType == Lexer.SymbolType.MalformedString)
                {
                    return ParserResultTuple.Expected("missing quote", remainingSpan);
                }
                else
                {
                    return ParserResultTuple.ExpectedConstant(remainingSpan);
                }
                remainingSpan = lexStatus.RemainingSpan;

                // If we're not good, we already returned.
                lexStatus = lexer.LexWordSkipWS(remainingSpan);
                if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
                {
                    return ParserResultTuple.Expected(",", remainingSpan);
                }
                else if (lexStatus.Word.SymbolType == Lexer.SymbolType.Punctuation && lexStatus.Word.Text == ",")
                {
                    // Got the , from e.g. DATA 10,20
                    remainingSpan = lexStatus.RemainingSpan;
                    keepGoing = true;
                }
                else if (lexStatus.Word.IsWS) // Got to end
                {
                    remainingSpan = lexStatus.RemainingSpan;
                    keepGoing = false;
                }
                else
                {
                    return ParserResultTuple.Expected(",", remainingSpan);
                }
            }

            return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                new ParsedDATA(args),
                remainingSpan);
        }

        // Dim_Statement -> DIM VARIABLE LPAREN Expression? RPAREN 
        public ParserResultTuple ParseDIM(Word firstWord, ROS line)
        {
            // Get variable name ( [arg] )

            Word identifier = null;
            var args = new List<Word>();

            // Function name
            var lexStatus = lexer.LexWordSkipWS(line);
            if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                return ParserResultTuple.ExpectedIdentifier(line);
            }
            else if (lexStatus.Word.SymbolType != Lexer.SymbolType.Identifier)
            {
                return ParserResultTuple.ExpectedIdentifier(line);
            }
            identifier = lexStatus.Word;

            // (
            var lookFor = "(";
            lexStatus = lexer.LexWordSkipWS(lexStatus.RemainingSpan);
            if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                return ParserResultTuple.Expected(lookFor, lexStatus.RemainingSpan);
            }
            else if (lexStatus.Word.SymbolType != Lexer.SymbolType.Punctuation && lexStatus.Word.Text != lookFor)
            {
                return ParserResultTuple.Expected(lookFor, lexStatus.RemainingSpan);
            }


            // optional size is an expression
            ParsedExpression size1 = null;
            ParsedExpression size2 = null;
            var ep = new ExpressionParser(lexer);
            var remainingSpan = lexStatus.RemainingSpan;
            var parserStatus = ep.ParseExpression(remainingSpan);
            if (parserStatus.Status.Result == ParserStatus.ResultType.Success)
            {
                size1 = parserStatus.Result as ParsedExpression;
                remainingSpan = parserStatus.RemainingSpan;
            }

            lexStatus = lexer.LexWordSkipWS(remainingSpan);
            if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                return ParserResultTuple.ExpectedIdentifier(remainingSpan);
            }
            if (lexStatus.Word.SymbolType == Lexer.SymbolType.Punctuation && lexStatus.Word.Text == ",")
            {
                // Is a two-D array; get the second size
                remainingSpan = lexStatus.RemainingSpan;
                parserStatus = ep.ParseExpression(remainingSpan);
                if (parserStatus.Status.Result == ParserStatus.ResultType.Success)
                {
                    size2 = parserStatus.Result as ParsedExpression;
                    remainingSpan = parserStatus.RemainingSpan;
                    lexStatus = lexer.LexWordSkipWS(remainingSpan);
                }
            }

            if (lexStatus.Word.SymbolType != Lexer.SymbolType.Punctuation || lexStatus.Word.Text != ")")
            {
                return ParserResultTuple.Expected (")", remainingSpan);
            }
            remainingSpan = lexStatus.RemainingSpan;

            return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                new ParsedDIM(identifier, size1, size2),
                remainingSpan);
        }

        // Dump_Statement -> DUMP 
        public ParserResultTuple ParseDUMP(Word firstWord, ROS line)
        {
            return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                new ParsedDUMP(),
                line);
        }

        // Else_Statement -> ELSE
        public ParserResultTuple ParseELSE(Word firstWord, ROS line)
        {
            return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                new ParsedELSE(),
                line);
        }

        // Just a plain Expression; used when dealing with Sigma control
        // Taken directly from the ParseLET code
        public ParserResultTuple ParseEXPRESSION(Word firstWord, ROS line)
        {
            var ep = new ExpressionParser(lexer);

            // RHS (exp)

            ParsedExpression rhs = null;
            ROS remainingSpan = line;
            var lexStatus = lexer.LexWordSkipWS(remainingSpan);
            if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                return ParserResultTuple.ExpectedExpression(remainingSpan);
            }
            // Allow LET a = BLACK but not LET a = BLACK + 10
            // This matches how ParseLET works
            if (lexer.IsColor(lexStatus.Word.Text))
            {
                rhs = new ParsedExpressionColorName(lexStatus.Word);
                remainingSpan = lexStatus.RemainingSpan;
            }
            else
            {
                var parseStatus = ep.ParseExpression(remainingSpan);
                if (parseStatus.Status.Result != ParserStatus.ResultType.Success)
                {
                    return ParserResultTuple.ExpectedExpression(remainingSpan);
                }
                rhs = parseStatus.Result as ParsedExpression;
                remainingSpan = parseStatus.RemainingSpan;
            }

            return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                new ParsedEXPRESSION(rhs),
                remainingSpan);
        }


        // For_Statement -> FOR VARIABLE EQUALS Expression TO Expression (STEP Expression)
        public ParserResultTuple ParseFOR(Word firstWord, ROS line)
        {
            // Get the i in FOR i = 1 TO 10 STEP 2
            Word identifier = null;
            ParsedExpression from;
            ParsedExpression to;
            ParsedExpression step = null;

            var ep = new ExpressionParser(lexer);


            var remainingSpan = line;
            var lexStatus = lexer.LexWordSkipWS(remainingSpan);
            if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                return ParserResultTuple.ExpectedIdentifier(remainingSpan);
            }
            if (lexStatus.Word.SymbolType != Lexer.SymbolType.Identifier)
            {
                return ParserResultTuple.ExpectedIdentifier(remainingSpan);
            }
            identifier = lexStatus.Word;
            remainingSpan = lexStatus.RemainingSpan;


            // Get the = in FOR i = 1 TO 10 STEP 2
            // OPERATOR (=)
            lexStatus = lexer.LexWordSkipWS(remainingSpan);
            if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                return ParserResultTuple.Expected("=", remainingSpan);
            }
            if (lexStatus.Word.SymbolType != Lexer.SymbolType.Operator) // = is an operator
            {
                return ParserResultTuple.Expected("=", remainingSpan);
            }
            if (lexStatus.Word.Text != "=") // = is an operator
            {
                return ParserResultTuple.Expected("=", remainingSpan);
            }
            remainingSpan = lexStatus.RemainingSpan;


            // Get the 1 in FOR i = 1 TO 10 STEP 2
            var fromStatus = ep.ParseExpression(remainingSpan);
            if (fromStatus.Status.Result != ParserStatus.ResultType.Success)
            {
                return ParserResultTuple.ExpectedExpression(remainingSpan);
            }
            from = fromStatus.Result as ParsedExpression;
            remainingSpan = fromStatus.RemainingSpan;


            // Get the TO in FOR i = 1 TO 10 STEP 2
            lexStatus = lexer.LexWordSkipWS(remainingSpan);
            if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                return ParserResultTuple.Expected("TO", remainingSpan);
            }
            if (lexStatus.Word.SymbolType != Lexer.SymbolType.Reserved || lexStatus.Word.Cmd != Lexer.Cmd.TO) 
            {
                return ParserResultTuple.Expected("TO", remainingSpan);
            }
            remainingSpan = lexStatus.RemainingSpan;


            // Get the 10 in FOR i = 1 TO 10 STEP 2
            var toStatus = ep.ParseExpression(remainingSpan);
            if (toStatus.Status.Result != ParserStatus.ResultType.Success)
            {
                return ParserResultTuple.ExpectedIdentifier(remainingSpan);
            }
            to = toStatus.Result as ParsedExpression;
            remainingSpan = toStatus.RemainingSpan;


            // Get the STEP in FOR i = 1 TO 10 STEP 2
            // (the STEP 2 is optional)
            lexStatus = lexer.LexWordSkipWS(remainingSpan);
            if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                return ParserResultTuple.Expected("STEP", remainingSpan);
            }
            if (lexStatus.Word.IsWS)
            {
                ; // all ok; STEP is optional
            }
            else if (lexStatus.Word.SymbolType == Lexer.SymbolType.Reserved && lexStatus.Word.Cmd == Lexer.Cmd.STEP)
            {
                remainingSpan = lexStatus.RemainingSpan;

                // Get the 2 in FOR i = 1 TO 10 STEP 2
                var stepStatus = ep.ParseExpression(remainingSpan);
                if (stepStatus.Status.Result != ParserStatus.ResultType.Success)
                {
                    return ParserResultTuple.ExpectedExpression(remainingSpan);
                }
                step = stepStatus.Result as ParsedExpression;
                remainingSpan = stepStatus.RemainingSpan;
            }
            else
            {
                return ParserResultTuple.Expected("STEP", remainingSpan);
            }

            return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                new ParsedFOR(identifier, from, to, step),
                remainingSpan);
        }

        
        //Forever_Statement -> FOREVER (STOP | WAIT)?
        public ParserResultTuple ParseFOREVER(Word firstWord, ROS line)
        {
            var ep = new ExpressionParser(lexer);

            var lexerStatus = lexer.LexWordSkipWS(line);
            Word foreverType = null;
            if (lexerStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                return ParserResultTuple.Expected("STOP or WAIT", line);
            }
            else if (lexerStatus.Word.IsWS)
            {
                foreverType = new Word("WAIT", Lexer.SymbolType.Reserved) { Cmd = Lexer.Cmd.WAIT };
            }
            else if (lexerStatus.Word.SymbolType == Lexer.SymbolType.Reserved && 
                (lexerStatus.Word.Cmd == Lexer.Cmd.WAIT || lexerStatus.Word.Cmd == Lexer.Cmd.STOP))
            {
                foreverType = lexerStatus.Word;
            }
            else
            {
                return ParserResultTuple.Expected("STOP or WAIT", line);
            }

            return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                new ParsedFOREVER(foreverType),
                lexerStatus.RemainingSpan);
        }


        // Function_Statement -> FUNCTION VARIABLE LPAREN ( VARIABLE (COMMA VARIABLE)* )? RPAREN
        public ParserResultTuple ParseFUNCTION(Word firstWord, ROS line)
        {
            // Get function name ( arg, arg, arg... )

            Word functionName = null;
            var args = new List<Word>();

            // Function name
            var remainingSpan = line;
            var lexStatus = lexer.LexWordSkipWS(remainingSpan);
            if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                return ParserResultTuple.ExpectedIdentifier(remainingSpan);
            }
            else if (lexStatus.Word.SymbolType != Lexer.SymbolType.Identifier)
            {
                return ParserResultTuple.ExpectedIdentifier(remainingSpan);
            }
            functionName = lexStatus.Word;
            remainingSpan = lexStatus.RemainingSpan;

            var lookFor = "(";
            lexStatus = lexer.LexWordSkipWS(remainingSpan);
            if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                return ParserResultTuple.Expected(lookFor, remainingSpan);
            }
            else if (lexStatus.Word.SymbolType != Lexer.SymbolType.Punctuation || lexStatus.Word.Text != lookFor)
            {
                return ParserResultTuple.Expected(lookFor, remainingSpan);
            }
            remainingSpan = lexStatus.RemainingSpan;


            // args (which are all just identifiers)
            bool keepGoing = true;
            while (keepGoing)
            {
                lexStatus = lexer.LexWordSkipWS(remainingSpan);
                if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
                {
                    return ParserResultTuple.ExpectedIdentifier(remainingSpan);
                }
                else if (lexStatus.Word.SymbolType == Lexer.SymbolType.Punctuation && lexStatus.Word.Text == ")")
                {
                    // Got the ) that ends the argument list for FUNCTION A(). But for FUNCTION A (B, C), 
                    // the ) is handled after reading the identifier.
                    keepGoing = false;
                    remainingSpan = lexStatus.RemainingSpan;
                }
                else if (lexStatus.Word.SymbolType != Lexer.SymbolType.Identifier)
                {
                    return ParserResultTuple.ExpectedIdentifier(remainingSpan);
                }
                else
                {
                    args.Add(lexStatus.Word);
                    remainingSpan = lexStatus.RemainingSpan;

                    // If we're not good, we already returned.
                    lexStatus = lexer.LexWordSkipWS(remainingSpan);
                    if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
                    {
                        return ParserResultTuple.ExpectedIdentifier(remainingSpan);
                    }
                    else if (lexStatus.Word.SymbolType == Lexer.SymbolType.Punctuation && lexStatus.Word.Text == ")")
                    {
                        // Got the ) that ends the argument list for FUNCTION A(). But for FUNCTION A (B, C), 
                        // the ) is handled after reading the identifier.
                        keepGoing = false;
                    }
                    else if (lexStatus.Word.SymbolType == Lexer.SymbolType.Punctuation && lexStatus.Word.Text == ",")
                    {
                        // Got the , from e.g. FUNCTION A(B,C)
                        keepGoing = true;
                        remainingSpan = lexStatus.RemainingSpan;
                    }
                    else
                    {
                        // Expected either , to continue the arg list or ) to end it.
                        return ParserResultTuple.Expected(", or )", remainingSpan);
                    }
                }

            }

            // We have already read in the end ) and don't need to do anything else.

            return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                new ParsedFUNCTION(firstWord, functionName, args),
                lexStatus.RemainingSpan);
        }


        // Global_Statement -> GLOBAL VARIABLE 
        public ParserResultTuple ParseGLOBAL(Word firstWord, ROS line)
        {
            // RHS (exp)

            ROS remainingSpan = line;
            var identifiers = new List<Word>();

            bool moreIdentifiers = true;
            while (moreIdentifiers)
            {
                var lexStatus = lexer.LexWordSkipWS(remainingSpan);
                if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
                {
                    return ParserResultTuple.ExpectedIdentifier(line);
                }
                if (lexStatus.Word.SymbolType != Lexer.SymbolType.Identifier)
                {
                    return ParserResultTuple.ExpectedIdentifier(line);
                }
                identifiers.Add(lexStatus.Word);
                remainingSpan = lexStatus.RemainingSpan;

                // Is the next thing a comma (,)
                lexStatus = lexer.LexWordSkipWS(remainingSpan);
                if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
                {
                    return ParserResultTuple.Expected("<CR> or ,", remainingSpan);
                }
                else if (lexStatus.Word.SymbolType == Lexer.SymbolType.WS)
                {
                    moreIdentifiers = false; // this is how the line ends.
                }
                else if (lexStatus.Word.SymbolType != Lexer.SymbolType.Punctuation || lexStatus.Word.Text != ",")
                {
                    return ParserResultTuple.Expected(",", remainingSpan);
                }
                else
                {
                    // GLOBAL x, y, z
                    remainingSpan = lexStatus.RemainingSpan;
                }
            }


            return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                new ParsedGLOBAL(identifiers),
                remainingSpan);
        }


        // Gosub_Statement -> GOSUB INTEGER 
        public ParserResultTuple ParseGOSUB(Word firstWord, ROS line)
        {
            // RHS (exp)

            int lineNumber = 0;
            ROS remainingSpan = line;
            var lexStatus = lexer.LexWordSkipWS(remainingSpan);
            if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                return ParserResultTuple.Expected("line-number", remainingSpan);
            }
            if (lexStatus.Word.SymbolType == Lexer.SymbolType.Number)
            {
                bool isInt = Int32.TryParse(lexStatus.Word.Text, out lineNumber);
                if (!isInt) return ParserResultTuple.Expected("line-number", remainingSpan);

                remainingSpan = lexStatus.RemainingSpan;
            }
            else
            {
                return ParserResultTuple.Expected("line-number", remainingSpan);
            }

            return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                new ParsedGOSUB(lexStatus.Word),
                remainingSpan);
        }


        // Goto_Statement -> GOTO INTEGER 
        public ParserResultTuple ParseGOTO(Word firstWord, ROS line)
        {
            // RHS (exp)

            int lineNumber = 0;
            ROS remainingSpan = line;
            var lexStatus = lexer.LexWordSkipWS(remainingSpan);
            if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                return ParserResultTuple.Expected("line-number", remainingSpan);
            }
            if (lexStatus.Word.SymbolType == Lexer.SymbolType.Number)
            {
                bool isInt = Int32.TryParse(lexStatus.Word.Text, out lineNumber);
                if (!isInt) return ParserResultTuple.Expected("line-number", remainingSpan);

                remainingSpan = lexStatus.RemainingSpan;
            }
            else
            {
                return ParserResultTuple.Expected("line-number", remainingSpan);
            }

            return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                new ParsedGOTO(lexStatus.Word),
                remainingSpan);
        }


        // If_Statement -> IF Expression (THEN Statement (ELSE Statement)?)? 
        // CHANGE: the THEN is totally optional now
        public ParserResultTuple ParseIF(Word firstWord, ROS line)
        {
            // Get the exp in IF exp THEN statement ELSE elsestate
            ParsedExpression exp;
            ParsedResult thenStatement = null;
            ParsedResult elseStatement = null;

            var ep = new ExpressionParser(lexer);


            var remainingSpan = line;


            // Get the exp in IF exp THEN statement ELSE elsestate
            var ifStatus = ep.ParseExpression(line);
            if (ifStatus.Status.Result != ParserStatus.ResultType.Success)
            {
                return ParserResultTuple.ExpectedExpression(remainingSpan);
            }
            exp = ifStatus.Result as ParsedExpression;
            remainingSpan = ifStatus.RemainingSpan;


            // Get the THEN in IF exp [[THEN] statement [ELSE elsestate]]
            // The THEN is optional
            var lexStatus = lexer.LexWordSkipWS(remainingSpan);
            if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                return ParserResultTuple.Expected("THEN", remainingSpan);
            }
            if (lexStatus.Word.SymbolType == Lexer.SymbolType.Reserved && lexStatus.Word.Cmd == Lexer.Cmd.THEN)
            {
                ; // Now I'm definitely expecting statement
                remainingSpan = lexStatus.RemainingSpan;
            }

            lexStatus = lexer.LexWordSkipWS(remainingSpan);
            if (lexStatus.Word.IsWS)
            {
                // OK to just be IF a=1 (it makes a nesting statement)
                goto GotEverything; 
            }

            // Now I'm expecting a statement and maybe the ELSE stuff.

            // Get the statement in IF exp [[THEN] statement [ELSE elsestate]]
            var statementStatus = ParseStatement(remainingSpan, ParseSwitches.AllowExtra);
            if (statementStatus.Status.Result != ParserStatus.ResultType.Success)
            {
                return ParserResultTuple.ExpectedStatement(remainingSpan);
            }
            thenStatement = statementStatus.Result;
            remainingSpan = statementStatus.RemainingSpan;


            // Get the ELSE in IF exp [[THEN] statement [ELSE elsestate]]
            lexStatus = lexer.LexWordSkipWS(remainingSpan);
            if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                return ParserResultTuple.Expected("ELSE", remainingSpan);
            }
            if (lexStatus.Word.IsWS)
            {
                // all ok; STEP is optional
                goto GotEverything;
            }
            remainingSpan = lexStatus.RemainingSpan;

            // Get the elseStatement in IF exp [[THEN] statement [ELSE elseStatement]]
            statementStatus = ParseStatement(remainingSpan);
            if (statementStatus.Status.Result != ParserStatus.ResultType.Success)
            {
                return ParserResultTuple.ExpectedStatement(remainingSpan);
            }
            elseStatement = statementStatus.Result;
            remainingSpan = statementStatus.RemainingSpan;


            GotEverything:
            return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                new ParsedIF (exp, thenStatement, elseStatement),
                remainingSpan);
        }


        // Import_Statement -> IMPORT FUNCTIONS FROM StringValue
        // StringValue -> STRING | SMARTQUOTESTRING
        public ParserResultTuple ParseIMPORT(Word firstWord, ROS line)
        {
            var ep = new ExpressionParser(lexer);
            var remainingSpan = line;

            // Get FUNCTIONS from IMPORT FUNCTIONS FROM name
            var lexStatus = lexer.LexWordSkipWS(remainingSpan);
            if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                return ParserResultTuple.Expected("IMPORT FUNCTIONS FROM ", remainingSpan);
            }
            if (lexStatus.Word.SymbolType != Lexer.SymbolType.Reserved || lexStatus.Word.Cmd != Lexer.Cmd.FUNCTIONS)
            {
                return ParserResultTuple.Expected("IMPORT FUNCTIONS FROM ", remainingSpan);
            }
            remainingSpan = lexStatus.RemainingSpan;


            // Get FROM from IMPORT FUNCTIONS FROM name
            lexStatus = lexer.LexWordSkipWS(remainingSpan);
            if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                return ParserResultTuple.Expected("IMPORT FUNCTIONS FROM ", remainingSpan);
            }
            if (lexStatus.Word.SymbolType != Lexer.SymbolType.Reserved || lexStatus.Word.Cmd != Lexer.Cmd.FROM)
            {
                return ParserResultTuple.Expected("IMPORT FUNCTIONS FROM ", remainingSpan);
            }
            remainingSpan = lexStatus.RemainingSpan;

            var wsWord = lexer.LexWhitespace(remainingSpan);
            remainingSpan = remainingSpan.Slice(wsWord.Text.Length);

            var word = lexer.LexString(remainingSpan, true); // true=fail on malformed input e.g. not a real string
            if (word.SymbolType != Lexer.SymbolType.String)
            {
                return ParserResultTuple.Expected("quoted name of package to import", remainingSpan);
            }
            // Strip out the quotes
            var name = word.Text.Substring(1, word.Text.Length - 2);
            var nameWord = new Word(name, Lexer.SymbolType.String);
            remainingSpan = remainingSpan.Slice(word.Text.Length);

            return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                new ParsedIMPORTFUNCTIONSFROM (nameWord),
                remainingSpan);
        }


        // Input_Statement -> INPUT VARIABLE 
        // New: INPUT VARIABLE [, VARIABLE]*
        public ParserResultTuple ParseINPUT(Word firstWord, ROS line)
        {
            List<Word> args = new List<Word>();
            // RHS (exp)

            ROS remainingSpan = line;

            while (true)
            {
                var lexStatus = lexer.LexWordSkipWS(remainingSpan);
                if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
                {
                    return ParserResultTuple.ExpectedIdentifier(remainingSpan);
                }
                if (lexStatus.Word.SymbolType != Lexer.SymbolType.Identifier)
                {
                    return ParserResultTuple.ExpectedIdentifier(remainingSpan);
                }
                args.Add(lexStatus.Word);
                remainingSpan = lexStatus.RemainingSpan;

                // Is the next thing a comma? If so, keep going.
                lexStatus = lexer.LexWordSkipWS(remainingSpan);
                if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
                {
                    return ParserResultTuple.ExpectedIdentifier(remainingSpan);
                }
                if (lexStatus.Word.IsWS)
                {
                    break; // All done with the list!
                }
                if (lexStatus.Word.Text == ",")
                {
                    // got a comma, so read in the next argument.
                    remainingSpan = lexStatus.RemainingSpan;
                }
                else
                {
                    return ParserResultTuple.Expected(", ", remainingSpan);
                }
            }

            return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                new ParsedINPUT(args),
                remainingSpan);
        }


        // LET a = exp
        // Let_Statement -> LET? VariableOrFunctionCall ( EQUALS (COLOR | Expression) )?
        // Doubles as both A(1) = 10 and A(1) -- first is a let for an array value, second is function call.
        // Let also handles Color values
        public ParserResultTuple ParseLET(Word firstWord, ROS line)
        {
            var ep = new ExpressionParser(lexer);

            // LHS (a) or a[1] or a.b
            var lhsStatus = ep.ParseExpressionVariableOrFunctionCall(line);
            //var lexStatus = lexer.LexWordSkipWS(line);
            if (lhsStatus.Status.Result != ParserStatus.ResultType.Success)
            {
                return ParserResultTuple.ExpectedIdentifier(line);
            }
            if ((lhsStatus.Result as ParsedExpression).Type != ParsedExpression.ExpressionType.IdentifierEtc)
            {
                return ParserResultTuple.ExpectedIdentifier(line);
            }
            var lhs = lhsStatus.Result;

            // OPERATOR (=)
            var lexStatus = lexer.LexWordSkipWS(lhsStatus.RemainingSpan);
            if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                return ParserResultTuple.Expected("=", lhsStatus.RemainingSpan);
            }
            if (lexStatus.Word.SymbolType != Lexer.SymbolType.Operator) // = is an operator
            {
                return ParserResultTuple.Expected("=", lhsStatus.RemainingSpan);
            }
            if (lexStatus.Word.Text != "=") // = is an operator
            {
                return ParserResultTuple.Expected("=", lhsStatus.RemainingSpan);
            }
            var op = lexStatus.Word;

            // RHS (exp)

            ParsedExpression rhs = null;
            ROS remainingSpan = lexStatus.RemainingSpan;
            lexStatus = lexer.LexWordSkipWS(lexStatus.RemainingSpan);
            if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                return ParserResultTuple.ExpectedExpression(remainingSpan);
            }
            // Allow LET a = BLACK but not LET a = BLACK + 10
            if (lexer.IsColor(lexStatus.Word.Text))
            {
                rhs = new ParsedExpressionColorName(lexStatus.Word);
                remainingSpan = lexStatus.RemainingSpan;
            }
            else
            {
                var parseStatus = ep.ParseExpression(remainingSpan);
                if (parseStatus.Status.Result != ParserStatus.ResultType.Success)
                {
                    return ParserResultTuple.ExpectedExpression(remainingSpan);
                }
                rhs = parseStatus.Result as ParsedExpression;
                remainingSpan = parseStatus.RemainingSpan;
            }

            return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                new ParsedLET(firstWord, lhs as ParsedExpression, op, rhs),
                remainingSpan);
        }

        // Next_Statement -> NEXT VARIABLE {
        public ParserResultTuple ParseNEXT(Word firstWord, ROS line)
        {
            // RHS (exp)

            Word identifier = null;
            ROS remainingSpan = line;
            var lexStatus = lexer.LexWordSkipWS(remainingSpan);
            if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                return ParserResultTuple.ExpectedIdentifier(remainingSpan);
            }
            if (lexStatus.Word.SymbolType != Lexer.SymbolType.Identifier)
            {
                return ParserResultTuple.ExpectedIdentifier(remainingSpan);
            }
            identifier = lexStatus.Word;
            remainingSpan = lexStatus.RemainingSpan;

            return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                new ParsedNEXT(identifier),
                remainingSpan);
        }

        public ParserResultTuple ParsePAPER(Word firstWord, ROS line)
        {
            // RHS (exp)
            ParsedExpression rhs = null;
            ROS remainingSpan = line;
            var lexStatus = lexer.LexWordSkipWS(remainingSpan);
            if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                return ParserResultTuple.ExpectedExpression(remainingSpan);
            }
            // Allow PAPER BLACK but not PAPER BLACK + 10
            if (lexer.IsColor(lexStatus.Word.Text))
            {
                rhs = new ParsedExpressionColorName(lexStatus.Word);
                remainingSpan = lexStatus.RemainingSpan;
            }
            else
            {
                var ep = new ExpressionParser(lexer);
                var parseStatus = ep.ParseExpression(remainingSpan);
                if (parseStatus.Status.Result != ParserStatus.ResultType.Success)
                {
                    return ParserResultTuple.ExpectedExpression(remainingSpan);
                }
                rhs = parseStatus.Result as ParsedExpression;
                remainingSpan = parseStatus.RemainingSpan;
            }

            return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                new ParsedPAPER(rhs),
                remainingSpan);
        }

        // Pause_Statement -> PAUSE Expression 
        public ParserResultTuple ParsePAUSE(Word firstWord, ROS line)
        {
            // RHS (exp)
            var ep = new ExpressionParser(lexer);
            ParsedExpression rhs = null;
            ROS remainingSpan = line;
            var parseStatus = ep.ParseExpression(remainingSpan);
            if (parseStatus.Status.Result != ParserStatus.ResultType.Success)
            {
                return ParserResultTuple.ExpectedExpression(remainingSpan);
            }
            rhs = parseStatus.Result as ParsedExpression;
            remainingSpan = parseStatus.RemainingSpan;

            return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                new ParsedPAUSE(rhs),
                remainingSpan);

        }


        //Play_Statement -> PLAY (STOP | WAIT | (ONNOTE Expression) | Expression)
        public ParserResultTuple ParsePLAY(Word firstWord, ROS line)
        {
            var ep = new ExpressionParser(lexer);

            var remainingSpan = line;
            var lexerStatus = lexer.LexWordSkipWS(remainingSpan);
            Word playType = null;
            ParsedExpression value = null;
            bool needExpression = true;
            if (lexerStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                return ParserResultTuple.Expected("music, STOP WAIT or ONNOTE", remainingSpan);
            }
            else if (lexerStatus.Word.SymbolType == Lexer.SymbolType.Reserved && lexerStatus.Word.Cmd == Lexer.Cmd.ONNOTE)
            {
                playType = lexerStatus.Word;
                needExpression = true;
                remainingSpan = lexerStatus.RemainingSpan;
            }
            else if (lexerStatus.Word.SymbolType == Lexer.SymbolType.Reserved && lexerStatus.Word.Cmd == Lexer.Cmd.STOP)
            {
                playType = lexerStatus.Word;
                needExpression = false;
                remainingSpan = lexerStatus.RemainingSpan;
            }
            else if (lexerStatus.Word.SymbolType == Lexer.SymbolType.Reserved && lexerStatus.Word.Cmd == Lexer.Cmd.WAIT)
            {
                playType = lexerStatus.Word;
                needExpression = false;
                remainingSpan = lexerStatus.RemainingSpan;
            }
            else
            {
                playType = new Word("PLAY", Lexer.SymbolType.Reserved);
                playType.Cmd = Lexer.Cmd.PLAY;
            }

            if (needExpression)
            {
                var parserStatus = ep.ParseExpression(remainingSpan);
                if (parserStatus.Status.Result != ParserStatus.ResultType.Success)
                {
                    return ParserResultTuple.Expected("PLAY music", remainingSpan);
                }
                value = parserStatus.Result as ParsedExpression;
                remainingSpan = parserStatus.RemainingSpan;
            }

            return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                new ParsedPLAY(playType, value),
                remainingSpan);
        }



        // Print_Expression -> (AT Expression COMMA Expression)? Expression
        // Print_Statement -> PRINT Print_Expression ((COMMA | SEMICOLON) Print_Expression?)*
        // Note that we can end with a coma or semicolon
        public ParserResultTuple ParsePRINT(Word firstWord, ROS line)
        {
            var ep = new ExpressionParser(lexer);

            var remainingSpan = line;
            bool keepGoing = true;
            var exp = new ExpressionParser(lexer);
            IList<ParsedPRINTExpression> expressions = new List<ParsedPRINTExpression>();

            // 
            while (keepGoing)
            {
                Word spaceType = null;
                ParsedExpression row = null;
                ParsedExpression col = null;


                // Read in the comma or semicolon. 
                var lexStatus = lexer.LexWordSkipWS(remainingSpan);
                if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
                {
                    return ParserResultTuple.ExpectedPrintExpression(remainingSpan);
                }
                // Might possibly be the ELSE of an IF THEN PRINT ELSE statement
                if (lexStatus.Word.SymbolType == Lexer.SymbolType.Reserved && lexStatus.Word.Cmd == Lexer.Cmd.ELSE)
                {
                    keepGoing = false;
                    break;
                }
                if (lexStatus.Word.IsWS)
                {
                    // all done
                    keepGoing = false;
                    break;
                }

                if (lexStatus.Word.SymbolType == Lexer.SymbolType.Punctuation)
                {
                    // has to be one of , ;
                    switch (lexStatus.Word.Text)
                    {
                        case ",":
                        case ";":
                            spaceType = lexStatus.Word;
                            remainingSpan = lexStatus.RemainingSpan;
                            lexStatus = lexer.LexWordSkipWS(remainingSpan);
                            if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
                            {
                                return ParserResultTuple.ExpectedPrintExpression(remainingSpan);
                            }
                            break;
                        case ".":
                        case "(":
                            // for example PRINT (2/3)
                            // for example PRINT .34
                            // Don't have to do anything; the bit that's been lexed will just be ignored.
                            break;
                        default:
                            if (expressions.Count == 0)
                            {
                                // for example: PRINT ! 10  or PRINT : 10
                                return ParserResultTuple.Expected("AT or a print expression", remainingSpan);
                            }
                            // for example: PRINT 10 : 20  
                            return ParserResultTuple.Expected(", or ; to start a print expression", remainingSpan);
                    }
                }
                // The , and ; are optional for the first element and mandatory for the rest.
                if (spaceType == null && expressions.Count > 0)
                {
                    // Example: PRINT a b
                    return ParserResultTuple.Expected(", or ; to start a print expression", remainingSpan);
                }

                // Just in case the command if IF c=10 THEN PRINT c, ELSE PRINT d
                // I got the "," and now if it's an ELSE, I'm done. ish.
                if (lexStatus.Word.SymbolType == Lexer.SymbolType.Reserved && lexStatus.Word.Cmd == Lexer.Cmd.ELSE)
                {
                    var commaExpression = new ParsedPRINTExpression(spaceType, null, null, null);
                    expressions.Add(commaExpression);
                    keepGoing = false;
                    break;
                }

                // Now grab the (opptional) AT. If there was a , or ; then we've also already lexed the next word.
                if (lexStatus.Word.SymbolType == Lexer.SymbolType.Reserved && lexStatus.Word.Cmd == Lexer.Cmd.AT)
                {
                    // get the row , col . They are each expressions.
                    remainingSpan = lexStatus.RemainingSpan;
                    var rowResult = exp.ParseExpression(remainingSpan);
                    if (rowResult.Status.Result != ParserStatus.ResultType.Success)
                    {
                        return ParserResultTuple.Expected("AT row, col", remainingSpan);
                    }
                    row = rowResult.Result as ParsedExpression;
                    remainingSpan = rowResult.RemainingSpan;

                    var commaStatus = lexer.LexWordSkipWS(remainingSpan);
                    if (commaStatus.Status.Result != LexerStatus.ResultType.Success)
                    {
                        return ParserResultTuple.Expected("AT row, col  (missing the comma)", remainingSpan);
                    }
                    if (commaStatus.Word.SymbolType != Lexer.SymbolType.Punctuation && commaStatus.Word.Text != ",")
                    {
                        return ParserResultTuple.Expected("AT row, col  (missing the comma)", remainingSpan);
                    }
                    remainingSpan = commaStatus.RemainingSpan;

                    // get the row , col . They are each expressions.
                    var colResult = exp.ParseExpression(remainingSpan);
                    if (colResult.Status.Result != ParserStatus.ResultType.Success)
                    {
                        return ParserResultTuple.Expected("AT row, col (missing the col)", remainingSpan);
                    }
                    col = colResult.Result as ParsedExpression;
                    spaceType = new Word("AT", Lexer.SymbolType.Reserved) { Cmd = Lexer.Cmd.AT };
                    remainingSpan = colResult.RemainingSpan;
                }

                // Now we can pick up the expression!
                var expStatus = exp.ParseExpression(remainingSpan);
                if (expStatus.Status.Result != ParserStatus.ResultType.Success)
                {
                    // This is actually OK if there's nothing afterward
                    var isblankStatus = lexer.LexWordSkipWS(remainingSpan);
                    if (isblankStatus.Status.Result == LexerStatus.ResultType.Success
                        && isblankStatus.Word.IsWS)
                    {
                        keepGoing = false;
                    }
                    else
                    {
                        return ParserResultTuple.ExpectedPrintExpression(remainingSpan);
                    }
                }
                var parsed = new ParsedPRINTExpression(spaceType, row, col, expStatus.Result as ParsedExpression);
                remainingSpan = expStatus.RemainingSpan;
                expressions.Add(parsed);
            }

            return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                new ParsedPRINT(expressions),
                remainingSpan);
        }

        // Return_Statement -> RETURN Expression? { return new BCBasic.Return($Expression as BCBasic.IExpression); };
        public ParserResultTuple ParseRETURN(Word firstWord, ROS line)
        {
            // RHS (exp)

            var ep = new ExpressionParser(lexer);
            ParsedExpression rhs = null;
            ROS remainingSpan = line;
            var lexStatus = lexer.LexWordSkipWS(remainingSpan);
            if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                return ParserResultTuple.Expected("return expression (or nothing)", remainingSpan);
            }
            if (lexStatus.Word.IsWS)
            {
                // e.g. just a plain RETURN
                rhs = null;
                remainingSpan = lexStatus.RemainingSpan;
            }
            else
            {
                var parseStatus = ep.ParseExpression(remainingSpan);
                if (parseStatus.Status.Result != ParserStatus.ResultType.Success)
                {

                    return ParserResultTuple.ExpectedExpression(remainingSpan);
                }
                rhs = parseStatus.Result as ParsedExpression;
                remainingSpan = parseStatus.RemainingSpan;
            }

            return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                new ParsedRETURN(rhs),
                remainingSpan);
        }

        // Rand_Statement -> RAND Expression
        public ParserResultTuple ParseRAND(Word firstWord, ROS line)
        {
            // RHS (exp)

            var ep = new ExpressionParser(lexer);
            ParsedExpression rhs = null;
            ROS remainingSpan = line;
            var parseStatus = ep.ParseExpression(remainingSpan);
            if (parseStatus.Status.Result != ParserStatus.ResultType.Success)
            {

                return ParserResultTuple.ExpectedExpression(remainingSpan);
            }
            rhs = parseStatus.Result as ParsedExpression;
            remainingSpan = parseStatus.RemainingSpan;

            return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                new ParsedRAND(rhs),
                remainingSpan);
        }

        // Read_Statement -> READ VARIABLE 
        public ParserResultTuple ParseREAD(Word firstWord, ROS line)
        {
            List<Word> args = new List<Word>();
            // RHS (exp)

            ROS remainingSpan = line;

            while (true)
            {
                var lexStatus = lexer.LexWordSkipWS(remainingSpan);
                if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
                {
                    return ParserResultTuple.ExpectedIdentifier(remainingSpan);
                }
                if (lexStatus.Word.SymbolType != Lexer.SymbolType.Identifier)
                {
                    return ParserResultTuple.ExpectedIdentifier(remainingSpan);
                }
                args.Add(lexStatus.Word);
                remainingSpan = lexStatus.RemainingSpan;

                // Is the next thing a comma? If so, keep going.
                lexStatus = lexer.LexWordSkipWS(remainingSpan);
                if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
                {
                    return ParserResultTuple.ExpectedIdentifier(remainingSpan);
                }
                if (lexStatus.Word.IsWS)
                {
                    break; // All done with the list!
                }
                if (lexStatus.Word.Text == ",")
                {
                    // got a comma, so read in the next argument.
                    remainingSpan = lexStatus.RemainingSpan;
                }
                else
                {
                    return ParserResultTuple.Expected(", ", remainingSpan);
                }
            }

            return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                new ParsedREAD(args),
                remainingSpan);
        }


        // ParseREM                     word = LexRestOfLine(input);
        public ParserResultTuple ParseREM(Word firstWord, ROS line)
        {
            var ep = new ExpressionParser(lexer);

            // RHS (exp)
            var rhs = Lexer.LexRestOfLine(line);
            var remainingSpan = line.Slice(rhs.Text.Length);

            return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                new ParsedREM(firstWord, rhs),
                remainingSpan);
        }

        // RESTORE (which has never been supported)
        // Restore is like ELSE; it just sits by itself
        public ParserResultTuple ParseRESTORE(Word firstWord, ROS line)
        {
            return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                new ParsedRESTORE(),
                line);
        }

        // Speak_Statement -> SPEAK ((LIST VOICES) | ((VOICE Expression)? Expression))
        public ParserResultTuple ParseSPEAK(Word firstWord, ROS line)
        {
            var ep = new ExpressionParser(lexer);
            var remainingSpan = line;

            // LIST VOICE
            var lexStatus = lexer.LexWordSkipWS(remainingSpan);
            if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                return ParserResultTuple.Expected("LIST VOICES or VOICE expression expression", remainingSpan);
            }
            if (lexStatus.Word.SymbolType == Lexer.SymbolType.Reserved && lexStatus.Word.Cmd == Lexer.Cmd.LIST)
            {
                // Handle the SPEAK LIST VOICES
                remainingSpan = lexStatus.RemainingSpan;
                lexStatus = lexer.LexWordSkipWS(remainingSpan);
                if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
                {
                    return ParserResultTuple.Expected("LIST VOICES or VOICE expression expression", remainingSpan);
                }
                if (lexStatus.Word.SymbolType == Lexer.SymbolType.Reserved && lexStatus.Word.Cmd == Lexer.Cmd.VOICES)
                {
                    // Got SPEAK LIST VOICES
                    remainingSpan = lexStatus.RemainingSpan;
                    return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                        new ParsedSPEAKLISTVOICES(),
                        remainingSpan);
                }
                else
                {
                    return ParserResultTuple.Expected("LIST VOICES ", remainingSpan);
                }
            }

            ParsedExpression voice = null;
            ParsedExpression value = null;
            ParserResultTuple parseStatus;

            // Handle SPEAK VOICE "David" 10
            if (lexStatus.Word.SymbolType == Lexer.SymbolType.Reserved && lexStatus.Word.Cmd == Lexer.Cmd.VOICE)
            {
                remainingSpan = lexStatus.RemainingSpan;
                parseStatus = ep.ParseExpression(remainingSpan);
                if (parseStatus.Status.Result != ParserStatus.ResultType.Success)
                {
                    return ParserResultTuple.ExpectedExpression(remainingSpan);
                }
                voice = parseStatus.Result as ParsedExpression;
                remainingSpan = parseStatus.RemainingSpan;
            }

            parseStatus = ep.ParseExpression(remainingSpan);
            if (parseStatus.Status.Result != ParserStatus.ResultType.Success)
            {
                return ParserResultTuple.ExpectedExpression(remainingSpan);
            }
            value = parseStatus.Result as ParsedExpression;
            remainingSpan = parseStatus.RemainingSpan;

            return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                new ParsedSPEAK(voice, value),
                remainingSpan);
        }


        // Stop_Statement -> (END | STOP) SILENT? Expression? 
        // Note that SILENT is only for STOP statements
        // And (sigh) FUNCTION a(b) ... END is also legit (it shouldn't be, but it's in the docs)
        // Also parses END IF statements.
        public ParserResultTuple ParseSTOP(Word firstWord, ROS line)
        {
            // RHS (exp)

            var ep = new ExpressionParser(lexer);
            ParsedExpression rhs = null;
            bool isSilent = false;
            ROS remainingSpan = line;
            var lexStatus = lexer.LexWordSkipWS(remainingSpan);

            bool parseExpression = false;
            bool requireExpression = false;

            if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                return ParserResultTuple.Expected("return expression (or nothing)", remainingSpan);
            }
            if (lexStatus.Word.IsWS)
            {
                // e.g. just a plain END or STOP
                rhs = null;
                remainingSpan = lexStatus.RemainingSpan;
            }
            else if (lexStatus.Word.SymbolType == Lexer.SymbolType.Reserved && firstWord.Cmd == Lexer.Cmd.END && lexStatus.Word.Cmd == Lexer.Cmd.IF)
            {
                remainingSpan = lexStatus.RemainingSpan;

                // AKA, END IF but not STOP IF
                return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                    new ParsedENDIF(),
                    remainingSpan);
            }
            else if (lexStatus.Word.SymbolType == Lexer.SymbolType.Reserved && firstWord.Cmd == Lexer.Cmd.STOP && lexStatus.Word.Cmd == Lexer.Cmd.SILENT)
            {
                remainingSpan = lexStatus.RemainingSpan;

                // AKA, STOP SILENT but not END SILENT
                isSilent = true;
                parseExpression = true;
            }
            else
            {
                parseExpression = true;
                requireExpression = true;
            }

            if (parseExpression)
            { 
                var parseStatus = ep.ParseExpression(remainingSpan);
                if (parseStatus.Status.Result != ParserStatus.ResultType.Success)
                {
                    if (requireExpression)
                    {
                        return ParserResultTuple.ExpectedExpression(remainingSpan);
                    }
                    else
                    {
                        rhs = null; // is already null :-)
                    }
                }
                else
                {
                    rhs = parseStatus.Result as ParsedExpression;
                    remainingSpan = parseStatus.RemainingSpan;
                }
            }

            // Get the potential SILENT for both STOP SILENT and STOP <exp> SILENT

            return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                new ParsedSTOP(firstWord, rhs, isSilent),
                remainingSpan);
        }
    }
}
