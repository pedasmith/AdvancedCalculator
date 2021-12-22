using BCBasic;
using System;
using System.Collections.Generic;

namespace Edit
{
    // Converts the Language ParsedResult into BCBasic FullStatement
    // So here's the funny reason this exists: the BC BASIC compiler is intended to be "pure": it's jusdt
    // a language and doesn't have all the bits needed to actually be run (that way it could in theory be
    // converted to be a compiler, for example, and also makes it faster for just doing syntax checks).
   // So the new (2018) recursive descent parser does its thing, and then this adapter converts the results
   // into something that the BC BASIC system can really handle.
    public class BCBasicStatementAdapter
    {
        public IList<FullStatement> Parse (string txt)
        {
            var pp = new ProgramParser();
            var list = pp.Parse(txt);
            var Retval = new List<FullStatement>();
            foreach (var item in list)
            {
                var convert = Convert(item);
                convert.SourceCodeLine = item.SourceCodeLine;
                Retval.Add(convert);
            }
            return Retval;
        }
        public BCBasicExpressionAdapter ExpressionAdapter = new BCBasicExpressionAdapter();

        public FullStatement Convert (ParsedResult result)
        {
            if (result == null) return null;
            var Retval = new FullStatement(result.LineNumber, null);
            string statementError = result.StatementError;
            IStatement statement = null;
            switch (result.Cmd)
            {
                case Lexer.Cmd.ParseError:
                    {
                        var err = result as ParsedParseError;
                        statement = new Rem($"ERROR IN {result}");
                        statementError = err.Message + "\n" + err.MarkedStatement;
                    }
                    break;
                case Lexer.Cmd.ASSERT:
                    {
                        var exp = ExpressionAdapter.Convert((result as ParsedASSERT).RHS);
                        statement = new AssertExpression(exp as InfixExpression);
                    }
                    break;
                case Lexer.Cmd.BEEP:
                    {
                        var beep = result as ParsedBEEP;
                        var duration = ExpressionAdapter.Convert(beep.Duration);
                        var pitch = ExpressionAdapter.Convert(beep.Pitch);
                        statement = new Beep(duration, pitch);
                    }
                    break;
                case Lexer.Cmd.BLANKLINE:
                    {
                        statement = new Rem("") { RemarkType = Rem.RemType.BLANKLINE }; //NOTE: split this off from deliberate REM statements?
                    }
                    break;
                case Lexer.Cmd.CALL:
                    {
                        var expression = ExpressionAdapter.Convert((result as ParsedCALL).RHS);
                        statement = new Call(expression);
                    }
                    break;
                case Lexer.Cmd.CLS:
                    {
                        var cls = result as ParsedCLS;
                        var background = ExpressionAdapter.Convert(cls.Background);
                        var foreground = ExpressionAdapter.Convert(cls.Foreground);
                        statement = new Cls(background, foreground);
                    }
                    break;
                case Lexer.Cmd.CONSOLE:
                    {
                        var list = ExpressionAdapter.ConvertList((result as ParsedCONSOLE).Values.List);
                        statement = new BCBasic.Console(list); // Because there's also a System.Console
                    }
                    break;
                case Lexer.Cmd.DATA:
                    {
                        var list = ExpressionAdapter.ConvertList((result as ParsedDATA).Data);
                        statement = new Data(list);

                    }
                    break;
                case Lexer.Cmd.DIM:
                    {
                        var dim = result as ParsedDIM;
                        var exp1 = ExpressionAdapter.Convert(dim.Size1);
                        var exp2 = ExpressionAdapter.Convert(dim.Size2);
                        statement = new Dim(dim.Identifier.Text, exp1, exp2);
                    }
                    break;
                case Lexer.Cmd.DUMP:
                    {
                        statement = new Dump();
                    }
                    break;
                case Lexer.Cmd.ELSE:
                    {
                        statement = new Else();
                    }
                    break;
                case Lexer.Cmd.END: // like FUNCTION a(b) ... END which unfortunately is documented and common as of 2018-06-10.
                    {
                        var exp = ExpressionAdapter.Convert((result as ParsedSTOP).Value);
                        statement = new Return(exp);
                    }
                    break;
                case Lexer.Cmd.ENDIF:
                    {
                        statement = new EndIf();
                    }
                    break;
                case Lexer.Cmd.EXPRESSION:
                    {
                        var item = result as ParsedEXPRESSION;
                        var expression = ExpressionAdapter.Convert(item.RHS);
                        statement = new ExpressionEval(expression);
                    }
                    break;
                case Lexer.Cmd.FOR:
                    {
                        var item = result as ParsedFOR;
                        var variable = item.Identifier.Text;
                        var from = ExpressionAdapter.Convert(item.From);
                        var to = ExpressionAdapter.Convert(item.To);
                        var step = ExpressionAdapter.Convert(item.Step);
                        statement = new For(variable, from, to, step);
                    }
                    break;
                case Lexer.Cmd.FOREVER:
                    {
                        var forever = result as ParsedFOREVER;
                        var foreverType = Forever.ForeverType.Wait;
                        switch ((Lexer.Cmd)forever.ForeverType.Cmd)
                        {
                            case Lexer.Cmd.STOP: foreverType = Forever.ForeverType.Stop; break;
                            case Lexer.Cmd.WAIT: foreverType = Forever.ForeverType.Wait; break;
                            default:
                                statementError = $"ERROR: unknown FOREVER type {(Lexer.Cmd)forever.ForeverType.Cmd}";
                                System.Diagnostics.Debug.WriteLine(statementError);
                                break;

                        }
                        statement = new Forever(foreverType);
                    }
                    break;
                case Lexer.Cmd.FUNCTION:
                    {
                        var function = result as ParsedFUNCTION;
                        var arglist = new FunctionArglistDefine();
                        foreach (var arg in function.Args)
                        {
                            arglist.AddArg(arg.Text);
                        }
                        statement = new Function(function.FunctionName.Text, arglist);
                    }
                    break;
                case Lexer.Cmd.GLOBAL:
                    {
                        statement = new Global((result as ParsedGLOBAL).AsNames());
                    }
                    break;
                case Lexer.Cmd.GOSUB:
                    {
                        statement = new Gosub((result as ParsedGOSUB).Line.Text);
                    }
                    break;
                case Lexer.Cmd.GOTO:
                    {
                        statement = new Goto((result as ParsedGOTO).Line.Text);
                    }
                    break;
                case Lexer.Cmd.IF:
                    {
                        var ifstatement = result as ParsedIF;
                        var exp = ExpressionAdapter.Convert(ifstatement.Value);
                        var thenstatement = Convert(ifstatement.Statement);
                        var elsestatement = Convert(ifstatement.ElseStatement);
                        statement = new If(exp, thenstatement?.Statement, elsestatement?.Statement);
                    }
                    break;
                // case Lexer.Cmd.IMPORT: isn't a real thing; it will always be IMPORTFUNCTIONSFROM. There is no other IMPORT function.
                case Lexer.Cmd.IMPORTFUNCTIONSFROM:
                    {
                        var import = result as ParsedIMPORTFUNCTIONSFROM;
                        statement = new Import("FUNCTIONS", import.PackageName.Text);
                    }
                    break;
                case Lexer.Cmd.INPUT:
                    {
                        var list = (result as ParsedINPUT).Identifiers;
                        var args = new List<string>();
                        foreach (var item in list)
                        {
                            args.Add(item.Text);
                        }
                        statement = new Input(args);
                    }
                    break;
                case Lexer.Cmd.LET:
                    {
                        var let = result as ParsedLET;
                        var call = result as ParsedCALL;
                        if (call != null) // is a CALL statement via plain MyFunction(a,b,c) which is parsed like a LET
                        {
                            IExpression rhs = ExpressionAdapter.Convert(call.RHS);
                            statement = new Call(rhs);
                        }
                        else if (let != null) // is a LET statement
                        {
                            IExpression rhs = ExpressionAdapter.Convert(let.RHS);

                            if (let.LHS is ParsedExpressionIdentifier) // e.g. LET a = b
                            {
                                var id = let.LHS as ParsedExpressionIdentifier;
                                statement = new Let(id.Identifier.Text, rhs);
                            }
                            else if (let.LHS is ParsedExpressionArray) // e.g. LET a[1] = b
                            {
                                var array = let.LHS as ParsedExpressionArray;
                                var indexList = array.Indexes.List;
                                var index1 = ExpressionAdapter.Convert(indexList[0]);
                                var index2 = indexList.Count > 1 ? ExpressionAdapter.Convert(indexList[1]) : null ; 
                                var vav = new VariableArrayValue(array.Identifier.Text, index1, index2);
                                statement = new Let(vav, rhs);
                            }
                            else if (let.LHS is ParsedExpressionFunction) // e.g. LET a(1) = b
                            {
                                var fnc = let.LHS as ParsedExpressionFunction;
                                var indexList = fnc.Indexes.List;
                                var index1 = ExpressionAdapter.Convert(indexList[0]);
                                var index2 = indexList.Count > 1 ? ExpressionAdapter.Convert(indexList[1]) : null;
                                var vav = new VariableArrayValue(fnc.Identifier.Text, index1, index2);
                                statement = new Let(vav, rhs);
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"ERROR: unknown type of LET LHS {let.LHS.ToString()} in {result}");
                            }
                        }
                        else
                        {
                            statementError = $"ERROR: unknown type of LET {result.ToString()} in {result}";
                            System.Diagnostics.Debug.WriteLine(statementError);
                        }
                    }
                    break;
                case Lexer.Cmd.NEXT:
                    {
                        statement = new Next((result as ParsedNEXT).Identifier.Text);
                    }
                    break;
                case Lexer.Cmd.PAPER:
                    {
                        var exp = ExpressionAdapter.Convert ((result as ParsedPAPER).Value);
                        statement = new Paper(exp);
                    }
                    break;
                case Lexer.Cmd.PAUSE:
                    {
                        var exp = ExpressionAdapter.Convert((result as ParsedPAUSE).Value);
                        statement = new Pause(exp);
                    }
                    break;
                case Lexer.Cmd.PLAY:
                    {
                        var play = result as ParsedPLAY;
                        var ptype = Play.PlayType.Play;
                        switch ((Lexer.Cmd)play.PlayType.Cmd)
                        {
                            case Lexer.Cmd.ONNOTE: ptype = Play.PlayType.OnNote; break;
                            case Lexer.Cmd.PLAY: ptype = Play.PlayType.Play; break;
                            case Lexer.Cmd.STOP: ptype = Play.PlayType.Stop; break;
                            case Lexer.Cmd.WAIT: ptype = Play.PlayType.Wait; break;
                            default:
                                statementError = $"ERROR: PLAY {play.PlayType.Cmd} is unknown";
                                System.Diagnostics.Debug.WriteLine(statementError);
                                break;
                        }
                        var exp = ExpressionAdapter.Convert((result as ParsedPLAY).Value);
                        statement = new Play(ptype, exp);
                    }
                    break;
                case Lexer.Cmd.PRINT:
                    {
                        var print = result as ParsedPRINT;
                        var list = new List<PrintExpression>();
                        foreach (var item in print.Expressions)
                        {
                            list.Add(Convert(item));
                        }
                        statement = new PrintAt(list);
                    }
                    break;

                case Lexer.Cmd.RAND:
                    {
                        var exp = ExpressionAdapter.Convert((result as ParsedRAND).Value);
                        statement = new Rand(exp);
                    }
                    break;
                case Lexer.Cmd.READ:
                    {
                        var list = (result as ParsedREAD).Identifiers;
                        var args = new List<VariableValue>();
                        foreach (var item in list)
                        {
                            args.Add(new VariableValue(item.Text));
                        }
                        statement = new Read(args);
                    }
                    break;
                case Lexer.Cmd.REM:
                    {
                        statement = new Rem((result as ParsedREM).RHS.Text);
                    }
                    break;
                case Lexer.Cmd.RESTORE:
                    {
                        statement = new Restore();
                    }
                    break;
                case Lexer.Cmd.RETURN:
                    {
                        var exp = ExpressionAdapter.Convert((result as ParsedRETURN).Value);
                        statement = new Return(exp);
                    }
                    break;
                case Lexer.Cmd.SPEAK:
                    {
                        // Is it a SPEAK LIST VOICES?
                        if (result is ParsedSPEAKLISTVOICES)
                        {
                            statement = new SpeakListVoices();
                        }
                        else
                        {
                            var speak = result as ParsedSPEAK;
                            var voice = ExpressionAdapter.Convert(speak.Voice);
                            var exp = ExpressionAdapter.Convert(speak.Value);
                            statement = new Speak(voice, exp);
                        }
                    }
                    break;
                case Lexer.Cmd.STOP:
                    {
                        var ps = result as ParsedSTOP;
                        var exp = ExpressionAdapter.Convert(ps.Value);
                        statement = new Stop(exp, ps.IsSilent);
                    }
                    break;



                default:
                    statementError = $"ERROR: unhandled {result.Cmd} in {result}";
                    System.Diagnostics.Debug.WriteLine(statementError);
                    statement = new Rem(result.ToString());
                    break;
            }
            if (Retval.Statement == null) Retval.Statement = statement;
            if (statementError != null) Retval.StatementError = statementError;
            return Retval;
        }

        private PrintExpression Convert (ParsedPRINTExpression exp)
        {
            PrintExpression.PrintSpaceType spaceType = PrintExpression.PrintSpaceType.Default;
            switch (exp.SpaceType.Text)
            {
                case "?": spaceType = PrintExpression.PrintSpaceType.Default; break;
                case "\n": spaceType = PrintExpression.PrintSpaceType.Newline; break;
                case ",": spaceType = PrintExpression.PrintSpaceType.Tab; break;
                case ";": spaceType = PrintExpression.PrintSpaceType.NoSpace; break;
                case "AT": spaceType = PrintExpression.PrintSpaceType.At; break;
                default:
                    System.Diagnostics.Debug.WriteLine($"ERROR: PRINT has unknown space type {exp.SpaceType.Text}");
                    break;
            }
            var row = ExpressionAdapter.Convert(exp.Row);
            var col = ExpressionAdapter.Convert(exp.Col);
            var value = ExpressionAdapter.Convert(exp.Print);
            
            var Retval = new PrintExpression(spaceType, row, col, value);
            return Retval;
        }


    }
}