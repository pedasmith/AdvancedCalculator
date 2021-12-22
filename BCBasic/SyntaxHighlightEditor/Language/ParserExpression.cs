using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Text;
using Windows.UI.Xaml.Documents;

namespace Edit
{
    public class ExpressionParser
    {
        public ExpressionParser (Lexer lexer)
        {
            this.lexer = lexer;
        }

        public Lexer lexer;

        // Reads in a list a,b,c of expressions ending with ) or ]
        // Does read the ) or ]
        // might just be the ) or ] for an empty list!
        public ParserResultTuple ParseExpressionList (ROS line, string expecting)
        {
            var Retval = new ParsedExpressionList();
            var remainingSpan = line;
            var expressionStatus = ParseExpression(remainingSpan);
            var gotFirstArg = expressionStatus.Status.Result == ParserStatus.ResultType.Success;
            if (gotFirstArg) remainingSpan = expressionStatus.RemainingSpan;
            while (true)
            {
                var lexPunctuation = lexer.LexWordSkipWS(remainingSpan);
                if (lexPunctuation.Status.Result != LexerStatus.ResultType.Success)
                {
                    return ParserResultTuple.Expected($", or {expecting} to continue the argument list", remainingSpan);
                }
                if (lexPunctuation.Word.Text == ",")
                {
                    // keep on parsing.
                    if (expressionStatus.Status.Result != ParserStatus.ResultType.Success)
                    {
                        // Error. Got something like LET a = b(,c). We didn't parse an expression before
                        // the comma.
                        return ParserResultTuple.Expected($"expression before the comma in the argument list", remainingSpan);
                    }
                    Retval.List.Add(expressionStatus.Result as ParsedExpression);
                    remainingSpan = lexPunctuation.RemainingSpan;
                    expressionStatus = ParseExpression(remainingSpan); // read the next expression
                    if (expressionStatus.Status.Result != ParserStatus.ResultType.Success)
                    {
                        return ParserResultTuple.Expected("expression to continue the argument list", remainingSpan);
                    }
                    remainingSpan = expressionStatus.RemainingSpan;
                }
                else if (lexPunctuation.Word.Text == expecting) // expecting is e.g. ) or ]
                {
                    // All done. Be sure to swallow the ) or ]
                    // OK to be e.g. LET a = b(), so it's OK for the first expression to be null.
                    if (expressionStatus.Status.Result == ParserStatus.ResultType.Success) Retval.List.Add(expressionStatus.Result as ParsedExpression);
                    remainingSpan = lexPunctuation.RemainingSpan;
                    return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success), Retval, remainingSpan);
                }
                else
                {
                    return ParserResultTuple.Expected(",", remainingSpan);
                }
            }
            //return new ParserResultTuple(ParserStatus.Expected(", or ) or ] to continue the argument list"));
        }

        // Examples: a a.b a[b] a(b,c) a()
        public ParserResultTuple ParseExpressionFunctionCall(ROS line)
        {
            var lexStatus = lexer.LexWordSkipWS(line);
            if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                return ParserResultTuple.ExpectedExpression(line);
            }

            // VariableOrFunctionCall
            if (lexStatus.Word.SymbolType == Lexer.SymbolType.Identifier)
            {
                var lhs = lexStatus.Word;
                var nextToken = lexer.LexWordSkipWS(lexStatus.RemainingSpan);
                if (nextToken.Status.Result == LexerStatus.ResultType.Success)
                {
                    if (nextToken.Word.SymbolType == Lexer.SymbolType.Punctuation)
                    {
                        // VariableOrFunctionCall -> VARIABLE ( (LPAREN (Expression (COMMA Expression)*)? RPAREN) | (LSQUARE Expression RSQUARE) )?
                        // Handle either a(b,c) or a[b]
                        // But not because this is a function call, not an array reference.
                        if (nextToken.Word.Text == "(")
                        {
                            var list = ParseExpressionList(nextToken.RemainingSpan, ")");
                            if (list.Status.Result != ParserStatus.ResultType.Success)
                            {
                                return list;
                            }
                            return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                                new ParsedExpressionFunction(lhs, list.Result as ParsedExpressionList),
                                list.RemainingSpan);
                        }
                    }
                }
                return ParserResultTuple.Expected("(arguments)", lexStatus.RemainingSpan);
            }
            return ParserResultTuple.ExpectedIdentifier(line);
        }


        // Examples: a a.b a[b] a(b,c) a()
        public ParserResultTuple ParseExpressionVariableOrFunctionCall(ROS line)
        {
            var lexStatus = lexer.LexWordSkipWS(line);
            if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                return ParserResultTuple.ExpectedExpression(line);
            }

            // VariableOrFunctionCall
            if (lexStatus.Word.SymbolType == Lexer.SymbolType.Identifier)
            {
                var lhs = lexStatus.Word;
                var nextToken = lexer.LexWordSkipWS(lexStatus.RemainingSpan);
                if (nextToken.Status.Result == LexerStatus.ResultType.Success)
                {
                    if (nextToken.Word.SymbolType == Lexer.SymbolType.Punctuation)
                    {
                        // VariableOrFunctionCall -> VARIABLE ( (LPAREN (Expression (COMMA Expression)*)? RPAREN) | (LSQUARE Expression RSQUARE) )?
                        // Handle either a(b,c) or a[b]
                        if (nextToken.Word.Text == "(")
                        {
                            var list = ParseExpressionList(nextToken.RemainingSpan, ")");
                            if (list.Status.Result == ParserStatus.ResultType.Success)
                            {
                                return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                                    new ParsedExpressionFunction(lhs, list.Result as ParsedExpressionList),
                                    list.RemainingSpan);
                            }
                        }
                        else if (nextToken.Word.Text == "[")
                        {
                            var list = ParseExpressionList(nextToken.RemainingSpan, "]");
                            if (list.Status.Result == ParserStatus.ResultType.Success)
                            {
                                return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                                    new ParsedExpressionArray(lhs, list.Result as ParsedExpressionList),
                                    list.RemainingSpan);
                            }
                        }
                        // Otherwise, it's not a function call/array but instead is an ordinary identifier.
                    }
                }
                return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                    new ParsedExpressionIdentifier(lhs),
                    lexStatus.RemainingSpan);
            }
            return ParserResultTuple.ExpectedIdentifier(line);
        }

        // ExpressionAtom -> NUMBER | HEX | INFINITY | LPAREN Expression RPAREN | VariableOrFunctionCall | StringValue 
        public ParserResultTuple ParseExpressionAtom(ROS line)
        {
            var lexStatus = lexer.LexWordSkipWS(line);
            if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                return ParserResultTuple.ExpectedExpression(line);
            }

            // LPAREN Expression RPAREN
            if (lexStatus.Word.SymbolType == Lexer.SymbolType.Punctuation && lexStatus.Word.Text == "(")
            {
                var expectedEnd = ")";
                var expr = ParseExpression(lexStatus.RemainingSpan);
                if (expr.Status.Result != ParserStatus.ResultType.Success)
                {
                    return ParserResultTuple.ExpectedExpression(lexStatus.RemainingSpan);
                }

                lexStatus = lexer.LexWordSkipWS(expr.RemainingSpan);
                if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
                {
                    return ParserResultTuple.Expected(")", lexStatus.RemainingSpan);
                }
                if (lexStatus.Word.Text != expectedEnd)
                {
                    return ParserResultTuple.Expected(")", lexStatus.RemainingSpan);
                }
                return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                    new ParsedExpressionParenthesis(expr.Result as ParsedExpression),
                    lexStatus.RemainingSpan);
            }
            // NUMBER | HEX | INFINITY | .. | StringVALUE
            if (lexStatus.Word.SymbolType == Lexer.SymbolType.Number)
            {
                // VARIABLE LITERAL ; start of BINARY SUFFIX FUNCTION ARRAY 
                var lhs = lexStatus.Word;
                return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                    new ParsedExpressionLiteralNumeric(lhs),
                    lexStatus.RemainingSpan);
            }
            if (lexStatus.Word.SymbolType == Lexer.SymbolType.String)
            {
                // VARIABLE LITERAL ; start of BINARY SUFFIX FUNCTION ARRAY 
                var lhs = lexStatus.Word;
                return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                    new ParsedExpressionLiteralString(lhs),
                    lexStatus.RemainingSpan);
            }

            // VariableOrFunctionCall
            if (lexStatus.Word.SymbolType == Lexer.SymbolType.Identifier)
            {
                var expressionStatus = ParseExpressionVariableOrFunctionCall(line);
                return expressionStatus;
            }
            return ParserResultTuple.ExpectedExpression(line);
        }

        // ExpressionP12 -> ExpressionAtom POWER? | (ROOT ExpressionP12)
        // ExpressionP12 renamed to ParseExpressionRootPower
        public ParserResultTuple ParseExpressionRootPower(ROS line)
        {
            var lexStatus = lexer.LexWordSkipWS(line);
            if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                return ParserResultTuple.ExpectedExpression(line);
            }
            var first = lexStatus.Word;

            // ROOT ParseExpressionRootPower
            if (first.SymbolType == Lexer.SymbolType.Operator && first.OperatorType == Lexer.OperatorType.Root)
            {
                var parseStatus = ParseExpressionRootPower(lexStatus.RemainingSpan);
                if (parseStatus.Status.Result!= ParserStatus.ResultType.Success)
                {
                    return ParserResultTuple.ExpectedExpression(lexStatus.RemainingSpan);
                }
                return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                    new ParsedExpressionPrefix(first, parseStatus.Result as ParsedExpression),
                    parseStatus.RemainingSpan);
            }

            // Not a root symbol. Must be a Expression Atom, possibly followed by a POWER.
            var atomStatus = ParseExpressionAtom(line);
            if (atomStatus.Status.Result != ParserStatus.ResultType.Success)
            {
                // Nope, not an atom either. Just fail.
                return ParserResultTuple.ExpectedExpression(line);
            }
            var secondLexStatus = lexer.LexWordSkipWS(atomStatus.RemainingSpan);
            if (secondLexStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                // It's OK that the expression ended (e.g., LET a = 10)
                // Don't need to wrap in anything.
                return atomStatus;
            }

            if (secondLexStatus.Word.OperatorType == Lexer.OperatorType.Power)
            {
                // Got e.g. a = b²
                return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                    new ParsedExpressionSuffix(atomStatus.Result as ParsedExpression, secondLexStatus.Word),
                    secondLexStatus.RemainingSpan);
            }

            // It's OK that this part of the expression ended (e.g., LET a = 10 + 20 we got 10, and the + isn't a POWER)
            // Don't need to wrap in anything.
            return atomStatus;
        }

        // Rename to Unary like CHR$ 123 or LEN a$
        // ExpressionP11 -> ExpressionP12 | (SINCLAIROP ExpressionP11)  | (MINUS ExpressionP11)
        // SINCLAIROP -> @"(LEN)|(STR\$)|(CODE)|(CHR\$)";
        // (ExpressionP12 renamed to ParseExpressionRootPower)
        public ParserResultTuple ParseExpressionUnary(ROS line)
        {
            var lexStatus = lexer.LexWordSkipWS(line);
            if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                return ParserResultTuple.ExpectedExpression(line);
            }
            var first = lexStatus.Word;

            // Handle SINCLAIR operators
            if (lexStatus.Word.OperatorType == Lexer.OperatorType.Sinclair)
            {
                var sinclair = lexStatus.Word;
                var atomStatus = ParseExpressionUnary(lexStatus.RemainingSpan);
                if (atomStatus.Status.Result != ParserStatus.ResultType.Success)
                {
                    return ParserResultTuple.ExpectedExpression(line);
                }
                return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                    new ParsedExpressionPrefix(sinclair, atomStatus.Result as ParsedExpression),
                    atomStatus.RemainingSpan);
            }

            // Handle MINUS operators.
            if (lexStatus.Word.OperatorType == Lexer.OperatorType.Subtraction)
            {
                var subtract = lexStatus.Word;
                var atomStatus = ParseExpressionUnary(lexStatus.RemainingSpan);
                if (atomStatus.Status.Result != ParserStatus.ResultType.Success)
                {
                    return ParserResultTuple.ExpectedExpression(lexStatus.RemainingSpan);
                }
                Word minus = new Word("-", Lexer.SymbolType.Operator) { OperatorType = Lexer.OperatorType.Subtraction };
                var zero = new ParsedExpressionLiteralNumeric(new Word("0", Lexer.SymbolType.Number) { Value = 0.0, ValueNiceFormat = "F0" });
                return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                    new ParsedExpressionBinary(zero, subtract, atomStatus.Result as ParsedExpression),
                    atomStatus.RemainingSpan);
            }
            return ParseExpressionRootPower(line);
        }

        // Rename to ParseExpressionRaiseToThePower like b ** 2
        // ExpressionP10 -> ExpressionP11 (** ExpressionP11)*
        // (ExpressionP11 renamed to ParseExpressionUnary)
        // See https://en.wikipedia.org/wiki/Operator_associativity with example 5**4**3**2 is 5**(4**(3**2)) not ((5**4)**2)**2
        public ParserResultTuple ParseExpressionRaiseToThePower(ROS line)
        {
            var leftStatus = ParseExpressionUnary(line);
            if (leftStatus.Status.Result != ParserStatus.ResultType.Success)
            {
                return ParserResultTuple.ExpectedExpression(line);
            }
            var expressions = new List<ParsedExpression>();
            expressions.Add (leftStatus.Result as ParsedExpression);
            var ops = new List<Word>();
            var remainingSpan = leftStatus.RemainingSpan;

            bool keepGoing = true;
            while (keepGoing)
            {
                var lexStatus = lexer.LexWordSkipWS(remainingSpan);
                if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
                {
                    keepGoing = false;
                }
                else if (lexStatus.Word.OperatorType != Lexer.OperatorType.RaiseToThePower)
                {
                    keepGoing = false;
                }
                else
                {
                    ops.Add (lexStatus.Word);

                    var rhsStatus = ParseExpressionUnary(lexStatus.RemainingSpan);
                    if (rhsStatus.Status.Result != ParserStatus.ResultType.Success)
                    {
                        keepGoing = false;
                        // Failure. got e.g let a = b ** without a trailing expression.
                    }
                    else
                    {
                        expressions.Add(rhsStatus.Result as ParsedExpression);
                        remainingSpan = rhsStatus.RemainingSpan;
                    }
                }
            }
            // At this point, expressions is 0..n long and ops is 0..n-1 long. The ops array
            // must be at least 1 long (and expression 2 long) to have found a match.
            // If expressions is one long AND we terminated early because of a EOL, we're also good.
            // Expressions must be at least one at this point.

            var last = expressions.Count - 1;
            var Retval = expressions[last];
            last -= 1;
            while (last >= 0)
            {
                Retval = new ParsedExpressionBinary(expressions[last], ops[last], Retval);
                last -= 1;
            }

            return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                Retval,
                remainingSpan);
        }

        // Rename to ParseExpressionMultiply like a * b * c
        // ExpressionP9-> ExpressionP10 ((\*|×|⋅|·|/) ExpressionP10)*
        // (ExpressionP10 renamed to ParseExpressionRaiseToThePower)
        public ParserResultTuple ParseExpressionMultiply(ROS line)
        {
            var leftStatus = ParseExpressionRaiseToThePower(line);
            if (leftStatus.Status.Result != ParserStatus.ResultType.Success)
            {
                return ParserResultTuple.ExpectedExpression(line);
            }
            ParsedExpression expression = leftStatus.Result as ParsedExpression;
            var remainingSpan = leftStatus.RemainingSpan;

            while (true)
            {
                var lexStatus = lexer.LexWordSkipWS(remainingSpan);
                if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
                {
                    return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                        expression,
                        remainingSpan);
                }

                if (lexStatus.Word.OperatorType != Lexer.OperatorType.Multiplication)
                {
                    // Nope, it's not what we're looking for. Just return what we've got.
                    return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                        expression,
                        remainingSpan);
                }
                var op = lexStatus.Word;
                remainingSpan = lexStatus.RemainingSpan;

                var rhsStatus = ParseExpressionRaiseToThePower(remainingSpan);
                if (rhsStatus.Status.Result != ParserStatus.ResultType.Success)
                {
                    // Failure. got e.g let a = b / without a trailing expression.
                    return ParserResultTuple.ExpectedExpression(remainingSpan);
                }

                expression = new ParsedExpressionBinary(expression, op, rhsStatus.Result as ParsedExpression);
                remainingSpan = rhsStatus.RemainingSpan;
            }
        }

        // Rename to ParseExpressionAddition like a * b * c
        // ExpressionP6-> ExpressionP9 (\+|-|−|–) ExpressionP9)*
        // (ExpressionP9 renamed to ParseExpressionMultiply)
        public ParserResultTuple ParseExpressionAddition(ROS line)
        {
            var leftStatus = ParseExpressionMultiply(line);
            if (leftStatus.Status.Result != ParserStatus.ResultType.Success)
            {
                return ParserResultTuple.ExpectedExpression(line);
            }
            ParsedExpression expression = leftStatus.Result as ParsedExpression;
            var remainingSpan = leftStatus.RemainingSpan;

            while (true)
            {
                // We're expecting plus or minus. If we get year-10, should lex as year|-|10, not year|-10
                // So request that operators (like plus and minus) will be preferred over a "number"
                var lexStatus = lexer.LexWordSkipWS(remainingSpan, Lexer.LexerSwitches.PreferOperators);
                if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
                {
                    return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                        expression,
                        remainingSpan);
                }

                if (lexStatus.Word.OperatorType != Lexer.OperatorType.Addition && lexStatus.Word.OperatorType != Lexer.OperatorType.Subtraction)
                {
                    // Nope, it's not what we're looking for. Just return what we've got.
                    return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                        expression,
                        remainingSpan);
                }
                var op = lexStatus.Word;
                remainingSpan = lexStatus.RemainingSpan;

                var rhsStatus = ParseExpressionMultiply(remainingSpan);
                if (rhsStatus.Status.Result != ParserStatus.ResultType.Success)
                {
                    // Failure. got e.g let a = b / without a trailing expression.
                    return ParserResultTuple.ExpectedExpression(remainingSpan);
                }

                expression = new ParsedExpressionBinary(expression, op, rhsStatus.Result as ParsedExpression);
                remainingSpan = rhsStatus.RemainingSpan;
            }
        }

        // Rename to ParseExpressionCompare like a > b
        // ExpressionP5-> ExpressionP6 ((<=|>=|<>|<|=|≅|≇|>) ExpressionP6)*
        // (ExpressionP6 renamed to ParseExpressionAddition)
        public ParserResultTuple ParseExpressionCompare(ROS line)
        {
            var leftStatus = ParseExpressionAddition(line);
            if (leftStatus.Status.Result != ParserStatus.ResultType.Success)
            {
                return ParserResultTuple.ExpectedExpression(line);
            }
            ParsedExpression expression = leftStatus.Result as ParsedExpression;
            var remainingSpan = leftStatus.RemainingSpan;

            while (true)
            {
                var lexStatus = lexer.LexWordSkipWS(remainingSpan);
                if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
                {
                    return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                        expression,
                        remainingSpan);
                }

                if (lexStatus.Word.OperatorType != Lexer.OperatorType.Compare)
                {
                    // Nope, it's not what we're looking for. Just return what we've got.
                    return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                        expression,
                        remainingSpan);
                }
                var op = lexStatus.Word;
                remainingSpan = lexStatus.RemainingSpan;

                var rhsStatus = ParseExpressionAddition(remainingSpan);
                if (rhsStatus.Status.Result != ParserStatus.ResultType.Success)
                {
                    // Failure. got e.g let a = b / without a trailing expression.
                    return ParserResultTuple.ExpectedExpression(remainingSpan);
                }

                expression = new ParsedExpressionBinary(expression, op, rhsStatus.Result as ParsedExpression);
                remainingSpan = rhsStatus.RemainingSpan;
            }
        }

        // Rename to Not like let a = NOT b
        // ExpressionP4-> NOT? ExpressionP5
        // (ExpressionP5 renamed to ParseExpressionCompare)
        public ParserResultTuple ParseExpressionNot(ROS line)
        {
            var lexStatus = lexer.LexWordSkipWS(line);
            if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
            {
                return ParserResultTuple.ExpectedExpression(line);
            }
            var first = lexStatus.Word;

            // Handle NOT operators
            if (lexStatus.Word.OperatorType == Lexer.OperatorType.Logic && lexStatus.Word.Text == "NOT")
            {
                var logic = lexStatus.Word;
                var atomStatus = ParseExpressionCompare(lexStatus.RemainingSpan);
                if (atomStatus.Status.Result != ParserStatus.ResultType.Success)
                {
                    return ParserResultTuple.ExpectedExpression(lexStatus.RemainingSpan);
                }
                return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                    new ParsedExpressionPrefix(logic, atomStatus.Result as ParsedExpression),
                    atomStatus.RemainingSpan);
            }
            return ParseExpressionCompare(line);
        }


        // ExpressionP3-> ExpressionP4 (AND ExpressionP4)?
        // Fixed the bug; (AND ..)? should be (AND ..)*
        // (ExpressionP4 renamed to ParseExpressionNot)
        public ParserResultTuple ParseExpressionAnd(ROS line)
        {
            var leftStatus = ParseExpressionNot(line);
            if (leftStatus.Status.Result != ParserStatus.ResultType.Success)
            {
                return ParserResultTuple.ExpectedExpression(line);
            }
            ParsedExpression expression = leftStatus.Result as ParsedExpression;
            var remainingSpan = leftStatus.RemainingSpan;

            while (true)
            {
                var lexStatus = lexer.LexWordSkipWS(remainingSpan);
                if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
                {
                    return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                        expression,
                        remainingSpan);
                }

                if (lexStatus.Word.OperatorType != Lexer.OperatorType.Logic || lexStatus.Word.Text != "AND")
                {
                    // Nope, it's not what we're looking for. Just return what we've got.
                    return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                        expression,
                        remainingSpan);
                }
                var op = lexStatus.Word;
                remainingSpan = lexStatus.RemainingSpan;

                var rhsStatus = ParseExpressionNot(remainingSpan);
                if (rhsStatus.Status.Result != ParserStatus.ResultType.Success)
                {
                    // Failure. got e.g let a = b AND without a trailing expression.
                    return ParserResultTuple.ExpectedExpression(remainingSpan);
                }

                expression = new ParsedExpressionBinary(expression, op, rhsStatus.Result as ParsedExpression);
                remainingSpan = rhsStatus.RemainingSpan;
            }
        }

        // ExpressionP2-> ExpressionP3 (OR ExpressionP3)?
        // Fixed the bug; (OR ..)? should be (OR ..)*
        // (ExpressionP3 renamed to ParseExpressionAnd)
        public ParserResultTuple ParseExpressionOr(ROS line)
        {
            var leftStatus = ParseExpressionAnd(line);
            if (leftStatus.Status.Result != ParserStatus.ResultType.Success)
            {
                return ParserResultTuple.ExpectedExpression(line);
            }
            ParsedExpression expression = leftStatus.Result as ParsedExpression;
            var remainingSpan = leftStatus.RemainingSpan;

            while (true)
            {
                var lexStatus = lexer.LexWordSkipWS(remainingSpan);
                if (lexStatus.Status.Result != LexerStatus.ResultType.Success)
                {
                    return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                        expression,
                        remainingSpan);
                }

                if (lexStatus.Word.OperatorType != Lexer.OperatorType.Logic || lexStatus.Word.Text != "OR")
                {
                    // Nope, it's not what we're looking for. Just return what we've got.
                    return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                        expression,
                        remainingSpan);
                }
                var op = lexStatus.Word;
                remainingSpan = lexStatus.RemainingSpan;

                var rhsStatus = ParseExpressionAnd(remainingSpan);
                if (rhsStatus.Status.Result != ParserStatus.ResultType.Success)
                {
                    // Failure. got e.g let a = b OR without a trailing expression.
                    return ParserResultTuple.ExpectedExpression(remainingSpan);
                }

                expression = new ParsedExpressionBinary(expression, op, rhsStatus.Result as ParsedExpression);
                remainingSpan = rhsStatus.RemainingSpan;
            }
        }

        // InputExpression -> INPUT (DEFAULT Expression)? (PROMPT Expression)?
        // Assumes that the first word (INPUT) is already read.
        public ParserResultTuple ParseExpressionInput(Word input, ROS line)
        {
            ParsedExpression defaultValue = null;
            ParsedExpression prompt = null;
            var remainingSpan = line;

            bool keepGoing = true;
            while (keepGoing)
            {
                var lexStatus = lexer.LexWordSkipWS(remainingSpan);
                if (lexStatus.Status.Result == LexerStatus.ResultType.Success)
                {
                    if (lexStatus.Word.SymbolType == Lexer.SymbolType.Reserved)
                    {
                        switch (lexStatus.Word.Cmd)
                        {
                            case Lexer.Cmd.DEFAULT:
                                {
                                    remainingSpan = lexStatus.RemainingSpan;
                                    var valueStatus = ParseExpression(remainingSpan);
                                    if (valueStatus.Status.Result != ParserStatus.ResultType.Success)
                                    {
                                        return ParserResultTuple.ExpectedExpression(remainingSpan);
                                    }
                                    defaultValue = valueStatus.Result as ParsedExpression;
                                    remainingSpan = valueStatus.RemainingSpan;
                                }
                                break;
                            case Lexer.Cmd.PROMPT:
                                {
                                    remainingSpan = lexStatus.RemainingSpan;
                                    var valueStatus = ParseExpression(remainingSpan);
                                    if (valueStatus.Status.Result != ParserStatus.ResultType.Success)
                                    {
                                        return ParserResultTuple.ExpectedExpression(remainingSpan);
                                    }
                                    prompt = valueStatus.Result as ParsedExpression;
                                    remainingSpan = valueStatus.RemainingSpan;
                                }
                                break;
                            default:
                                // Got something, and it's not right. Return a failure.
                                remainingSpan = lexStatus.RemainingSpan;
                                return ParserResultTuple.ExpectedReservedWord("DEFAULT or PROMPT", remainingSpan);

                        }
                    }
                    else if (lexStatus.Word.IsWS)
                    {
                        remainingSpan = lexStatus.RemainingSpan;
                        keepGoing = false;
                        // got nothing; that's OK
                    }
                    else
                    {
                        remainingSpan = lexStatus.RemainingSpan;
                        return ParserResultTuple.ExpectedReservedWord("DEFAULT or PROMPT", remainingSpan);
                    }
                }
            }

            return new ParserResultTuple(new ParserStatus(ParserStatus.ResultType.Success),
                new ParsedExpressionInput(input, defaultValue, prompt),
                remainingSpan);
        }

        public ParserResultTuple ParseExpression(ROS line)
        {
            var lexStatus = lexer.LexWordSkipWS(line);
            var remainingSpan = lexStatus.RemainingSpan;
            if (lexStatus.Status.Result == LexerStatus.ResultType.Success)
            {
                if (lexStatus.Word.SymbolType == Lexer.SymbolType.Reserved && lexStatus.Word.Text == "INPUT")
                {
                    return ParseExpressionInput(lexStatus.Word, lexStatus.RemainingSpan);
                }
            }
            var result = ParseExpressionOr(line);
            return result;
        }
    }
}
