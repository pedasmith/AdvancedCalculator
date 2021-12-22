using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Text;
using Windows.UI.Xaml.Documents;

namespace Edit
{
    class ParserTest
    {
        public static int Test()
        {
            int NError = 0;
            var pt = new ParserTest();
            var p = new StatementParser();
            p.lexer = new Lexer();

            NError += pt.TestAdditional(p);
            NError += pt.TestNumberedLines(p);

            NError += pt.TestASSERT(p);
            NError += pt.TestBEEP(p);
            NError += pt.TestCALL(p);
            NError += pt.TestCLS(p);
            NError += pt.TestCONSOLE(p);
            NError += pt.TestDATA(p);
            NError += pt.TestDIM(p);
            NError += pt.TestDUMP(p);
            NError += pt.TestELSE(p);
            NError += pt.TestFOR(p);
            NError += pt.TestFOREVER(p);
            NError += pt.TestFUNCTION(p);
            NError += pt.TestGLOBAL(p);
            NError += pt.TestGOTO_GOSUB(p);
            NError += pt.TestIF(p);
            NError += pt.TestIMPORT(p);
            NError += pt.TestINPUT(p);
            NError += pt.TestLET(p);
            NError += pt.TestNEXT(p);
            NError += pt.TestPAPER(p);
            NError += pt.TestPAUSE(p);
            NError += pt.TestPLAY(p);
            NError += pt.TestPRINT(p);
            NError += pt.TestRAND(p);
            NError += pt.TestREAD(p);
            NError += pt.TestREM(p);
            NError += pt.TestRETURN(p);
            NError += pt.TestSPEAK(p);
            NError += pt.TestSTOP(p);
            //NError += pt.TestCompileSpeed(p, 10000); // Surface Pro 4: 20K lines/second
            NError += pt.TestAllMdFiles(p);

            if (NError > 0)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: ParserTest: {NError} ERRORS");

            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"INFO: ParserTest: PASS");
                System.Diagnostics.Debug.WriteLine($"INFO: ParserTest: NLexWord {Lexer.NLexWord}");
            }
            return NError;
        }


        // Horrible non-async thing because results can't be in an async function.
        private int TestAllMdFiles(StatementParser p)
        {
            int NError = 0;
            NError += TestAllMdFilesIn(p, @"Assets\BCBasic\");
#if BLUETOOTH
            NError += TestAllMdFilesIn(p, @"Assets\BCBasic-BT\");
#endif
            return NError;
        }



        private int TestAllMdFilesIn(StatementParser p, string directory)
        {
            int NError = 0;
            int nstatements = 0;
            int nok = 0;
            int ntbd = 0;
            int nparseErrors = 0;
            var sa = new BCBasicStatementAdapter();
            StorageFolder InstallationFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            var taskFolder = InstallationFolder.GetFolderAsync(directory); taskFolder.AsTask().Wait();
            var basicDir = taskFolder.GetResults();
            var taskFiles = basicDir.GetFilesAsync(); taskFiles.AsTask().Wait();
            var files = taskFiles.GetResults();

            List<Tuple<string, IList<UnparsedProgram>>> AllPrograms = new List<Tuple<string, IList<UnparsedProgram>>>();
            foreach (var file in files)
            {
                if (file.Name.EndsWith(".bcbasic"))
                {
                    var taskText = FileIO.ReadTextAsync(file); taskText.AsTask().Wait();
                    var text = taskText.GetResults();
                    var programs = MdPackageFile.ParseMdFile(text);
                    AllPrograms.Add(new Tuple<string, IList<UnparsedProgram>>(file.Name, programs));
                }
            }

            var counts = new SortedDictionary<string, int>();

            var startTime = DateTime.UtcNow;
            foreach (var list in AllPrograms)
            {
                var filename = list.Item1;
                foreach (var program in list.Item2)
                {
                    System.Diagnostics.Debug.WriteLine($"NOTE: file {filename} program {program.Name.Trim()}");
                    var rawtext = program.Text;
                    var lines = p.lexer.SplitIntoLines(rawtext);
                    foreach (var tuple in lines)
                    {
                        var line = tuple.Item2;
                        var result = p.ParseStatement(new ROS(line)); // old way when I used Read Only Span line.AsSpan());
                        var convert = sa.Convert(result.Result);
                        nstatements++;
                        if (result.Status.Result == ParserStatus.ResultType.Success && convert.Statement != null && !(result.Result is ParsedParseError))
                        {
                            nok++;
                        }
                        else
                        {
                            if (result.Status.Error.StartsWith ("Not a known statement"))
                            {
                                var word = result.Status.Error.Split(new char[] { ' ' })[4];
                                if (!counts.ContainsKey(word)) counts.Add(word, 0);
                                counts[word]++;
                                ntbd++;
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"ERROR: parsing line {line}");
                                nparseErrors++;
                            }
                        }
                    }
                }
            }
            var endTime = DateTime.UtcNow;
            var nseconds = endTime.Subtract(startTime).TotalSeconds;
            var statementsPerSecond = (int)Math.Round (nstatements / nseconds);
            System.Diagnostics.Debug.WriteLine($"Stats: statementsPerSecond={statementsPerSecond} nstatements={nstatements} nok={nok} nparseErrors={nparseErrors} ntbd={ntbd}");
            var countsDesc = counts.OrderByDescending(kp => kp.Value).ToList();
            foreach (var item in countsDesc)
            {
                System.Diagnostics.Debug.WriteLine($"    : {item.Key} = {item.Value}");
            }
            return NError;
        }


        private int TestAdditional(StatementParser p)
        {
            int NError = 0;
            // 	if ($MINUS != null) 
            //    return new BCBasic.InfixExpression(new BCBasic.NumericConstant(0), "-", $ExpressionP11 as BCBasic.IExpression);


            NError += TestOneStatement(p, "a =  - PI", "LET a = ( 0 - PI )");
            NError += TestOneStatement(p, "a = String.Es", "LET a = String.Es");
            return NError;
        }

        private int TestNumberedLines(StatementParser p)
        {
            int NError = 0;
            // The ToString() for statements doesn't include the line numbers (because why not, that's why)
            NError += TestOneStatement(p, "10 REM", "REM");
            NError += TestOneStatement(p, " 10 REM", "REM");
            NError += TestOneStatement(p, "10  REM", "REM");
            NError += TestOneStatement(p, "10 REM  ", "REM  ");
            return NError;
        }

        private int TestASSERT(StatementParser p)
        {
            int NError = 0;
            NError += TestOneStatement(p, "ASSERT (1=2)", "ASSERT ( ( 1 = 2 ) )");
            NError += TestOneStatement(p, "ASSERT (1<=2)", "ASSERT ( ( 1 <= 2 ) )");
            NError += TestOneStatement(p, "ASSERT (1<>2+1)", "ASSERT ( ( 1 <> ( 2 + 1 ) ) )");
            // Really need the parens! NOT 2 is higher precedence than = and shouldn't show up
            NError += TestOneStatement(p, "ASSERT (0=(NOT 2))", "ASSERT ( ( 0 = ( ( NOT 2 ) ) ) )");
            return NError;
        }

        private int TestBEEP(StatementParser p)
        {
            int NError = 0;
            NError += TestOneStatement(p, "BEEP", "BEEP");
            NError += TestOneStatement(p, "BEEP a,b", "BEEP a , b");
            return NError;
        }


        private int TestCALL(StatementParser p)
        {
            int NError = 0;
            NError += TestOneStatement(p, "CALL SIN(x)", "CALL SIN ( x )");
            NError += TestOneStatement(p, "CALL SIN()", "CALL SIN ()");
            NError += TestOneStatement(p, "CALL SIN(x,y)", "CALL SIN ( x , y )");
            NError += TestOneStatement(p, "CALL SIN(x,y,z)", "CALL SIN ( x , y , z )");
            NError += TestOneStatement(p, "SIN(x)", "CALL SIN ( x )");
            return NError;
        }
        private int TestCLS(StatementParser p)
        {
            int NError = 0;
            NError += TestOneStatement(p, "CLS", "CLS");
            NError += TestOneStatement(p, "CLS RED GREEN", "CLS COLOR RED COLOR GREEN");
            NError += TestOneStatement(p, "CLS RED 10", "CLS COLOR RED 10");
            NError += TestOneStatement(p, "CLS 10 GREEN", "CLS 10 COLOR GREEN");
            return NError;
        }


        private int TestCONSOLE(StatementParser p)
        {
            int NError = 0;
            NError += TestOneStatement(p, "CONSOLE ABC", "CONSOLE ( ABC )");
            NError += TestOneStatement(p, "CONSOLE ABC,DEF", "CONSOLE ( ABC , DEF )");
            return NError;
        }

        private int TestDATA(StatementParser p)
        {
            int NError = 0;
            // Nope, not valid: NError += TestOneStatement(p, "DATA", "DATA");
            NError += TestOneStatement(p, "DATA 10", "DATA 10");
            NError += TestOneStatement(p, "DATA 10,20", "DATA 10 , 20");
            NError += TestOneStatement(p, "DATA \"A\"", "DATA \"A\"");
            return NError;
        }


        private int TestDIM(StatementParser p)
        {
            int NError = 0;
            NError += TestOneStatement(p, "DIM A()", "DIM A ()");
            NError += TestOneStatement(p, "DIM A(10)", "DIM A ( 10 )");
            return NError;
        }


        private int TestDUMP(StatementParser p)
        {
            int NError = 0;
            NError += TestOneStatement(p, "DUMP", "DUMP");
            return NError;
        }

        private int TestELSE(StatementParser p)
        {
            int NError = 0;
            NError += TestOneStatement(p, "ELSE", "ELSE");
            return NError;
        }

        private int TestFOR(StatementParser p)
        {
            int NError = 0;
            NError += TestOneStatement(p, "FOR i = 1 TO 10", "FOR i = 1 TO 10");
            NError += TestOneStatement(p, "FOR i = 1 TO 10 STEP 2", "FOR i = 1 TO 10 STEP 2");
            return NError;
        }


        private int TestFOREVER(StatementParser p)
        {
            int NError = 0;
            NError += TestOneStatement(p, "FOREVER", "FOREVER WAIT");
            NError += TestOneStatement(p, "FOREVER STOP", "FOREVER STOP");
            NError += TestOneStatement(p, "FOREVER WAIT", "FOREVER WAIT");
            return NError;
        }


        private int TestFUNCTION(StatementParser p)
        {
            int NError = 0;
            NError += TestOneStatement(p, "FUNCTION A()", "FUNCTION A ()");
            NError += TestOneStatement(p, "FUNCTION A(B)", "FUNCTION A ( B )");
            NError += TestOneStatement(p, "FUNCTION A(B,C)", "FUNCTION A ( B , C )");
            return NError;
        }


        private int TestGLOBAL(StatementParser p)
        {
            int NError = 0;
            NError += TestOneStatement(p, "GLOBAL i", "GLOBAL i");
            return NError;
        }


        private int TestGOTO_GOSUB(StatementParser p)
        {
            int NError = 0;
            NError += TestOneStatement(p, "GOTO 10", "GOTO 10");
            NError += TestOneStatement(p, "GOSUB 10", "GOSUB 10");
            return NError;
        }


        private int TestIF(StatementParser p)
        {
            int NError = 0;
            NError += TestOneStatement(p, "IF (i<10) THEN j=2", "IF ( ( ( i < 10 ) ) ) THEN LET j = 2");
            NError += TestOneStatement(p, "IF i<10 THEN j=2", "IF ( ( i < 10 ) ) THEN LET j = 2");
            NError += TestOneStatement(p, "IF (i<10)", "IF ( ( ( i < 10 ) ) )");
            NError += TestOneStatement(p, "IF (i<10) THEN j=2 ELSE k=3", "IF ( ( ( i < 10 ) ) ) THEN LET j = 2 ELSE LET k = 3");
            return NError;
        }


        private int TestIMPORT(StatementParser p)
        {
            int NError = 0;
            NError += TestOneStatement(p, "IMPORT FUNCTIONS FROM \"abc\"", "IMPORT FUNCTIONS FROM abc");
            return NError;
        }


        private int TestINPUT(StatementParser p)
        {
            int NError = 0;
            NError += TestOneStatement(p, "INPUT i", "INPUT i");
            return NError;
        }


        private int TestLET(StatementParser p)
        {
            int NError = 0;

            NError += TestOneStatement(p, "LET d=(x)²", "LET d = ( ( x ) ² )");

            NError += TestOneStatement(p, "LET a(1)=b", "LET a ( 1 ) = b");
            NError += TestOneStatement(p, "LET a[1]=b", "LET a ( 1 ) = b");
            NError += TestOneStatement(p, "LET memory[0] = 10", "LET memory ( 0 ) = 10");
            NError += TestOneStatement(p, "LET memory[0] = memory[0]+1", "LET memory ( 0 ) = ( memory ( 0 ) + 1 )");
            NError += TestOneStatement(p, "memory[0] = memory[0]+1", "LET memory ( 0 ) = ( memory ( 0 ) + 1 )");
            NError += TestOneStatement(p, "x1 = (g.W - w)/2", "LET x1 = ( ( ( g.W - w ) ) / 2 )");

            NError += TestOneStatement(p, "LET a=INPUT DEFAULT year-10", "LET a = INPUT DEFAULT ( year - 10 )");
            NError += TestOneStatement(p, "LET a=INPUT ", "LET a = INPUT"); //Note the space at the end
            NError += TestOneStatement(p, "LET a=INPUT DEFAULT a DEFAULT b", "LET a = INPUT DEFAULT b");
            NError += TestOneStatement(p, "LET a=INPUT PROMPT a PROMPT b", "LET a = INPUT PROMPT b");
            NError += TestOneStatement(p, "LET a=INPUT DEFAULT a PROMPT b", "LET a = INPUT DEFAULT a PROMPT b");
            NError += TestOneStatement(p, "LET a=INPUT PROMPT a DEFAULT b", "LET a = INPUT DEFAULT b PROMPT a");
            NError += TestOneStatement(p, "LET a=b", "LET a = b");
            NError += TestOneStatement(p, "LET a[1]=b", "LET a ( 1 ) = b");

            NError += TestOneStatement(p, "LET a=b( )", "LET a = b ()");
            NError += TestOneStatement(p, "LET a=b()", "LET a = b ()");
            NError += TestOneStatement(p, "LET a=b(c)", "LET a = b ( c )");
            NError += TestOneStatement(p, "LET a=b[c]", "LET a = b ( c )");
            NError += TestOneStatement(p, "LET a=b(c,d)", "LET a = b ( c , d )");
            NError += TestOneStatement(p, "LET a=b(c,d,e)", "LET a = b ( c , d , e )");


            NError += TestOneStatement(p, "LET a=1.23", "LET a = 1.23");
            NError += TestOneStatement(p, "LET a=-1.23", "LET a = -1.23");
            NError += TestOneStatement(p, "LET a=.23", "LET a = 0.23");
            NError += TestOneStatement(p, "LET a=1.23E+023", "LET a = 1.23E+023");
            NError += TestOneStatement(p, "LET a=1.23E23", "LET a = 1.23E+023");
            NError += TestOneStatement(p, "LET a=∞", "LET a = ∞");
            NError += TestOneStatement(p, "LET a=-∞", "LET a = -∞");
            NError += TestOneStatement(p, "LET a= b**c**d", "LET a = ( b ** ( c ** d ) )");
            NError += TestOneStatement(p, "LET a=BLACK", "LET a = COLOR BLACK");


            NError += TestOneStatement(p, "LET a=0xF", "LET a = 15");
            NError += TestOneStatement(p, "LET a=0x100", "LET a = 256");
            NError += TestOneStatement(p, "LET a=0xFF", "LET a = 255");


            NError += TestOneStatement(p, "LET a=b", "LET a = b");
            NError += TestOneStatement(p, "LET a=-b", "LET a = ( 0 - b )");
            NError += TestOneStatement(p, "LET a=√b", "LET a = ( √ b )");
            NError += TestOneStatement(p, "LET a= LEN b", "LET a = ( LEN b )");
            NError += TestOneStatement(p, "LET a= -b", "LET a = ( 0 - b )");
            NError += TestOneStatement(p, "LET a= b**c", "LET a = ( b ** c )");
            NError += TestOneStatement(p, "LET a= b**c**d", "LET a = ( b ** ( c ** d ) )");
            NError += TestOneStatement(p, "LET a=b+c", "LET a = ( b + c )");
            NError += TestOneStatement(p, "LET a=b+c+d", "LET a = ( ( b + c ) + d )");
            NError += TestOneStatement(p, "LET a=b*c", "LET a = ( b * c )");
            NError += TestOneStatement(p, "LET a=b*c*d", "LET a = ( ( b * c ) * d )");
            NError += TestOneStatement(p, "LET a=b*c+d*e", "LET a = ( ( b * c ) + ( d * e ) )");
            NError += TestOneStatement(p, "LET a=b>c", "LET a = ( b > c )");
            NError += TestOneStatement(p, "LET a=b>=c", "LET a = ( b >= c )");
            NError += TestOneStatement(p, "LET a=b<>c", "LET a = ( b <> c )");
            NError += TestOneStatement(p, "LET a=b>c<>d", "LET a = ( ( b > c ) <> d )");
            NError += TestOneStatement(p, "LET a=NOT b", "LET a = ( NOT b )");
            NError += TestOneStatement(p, "LET a=b AND c", "LET a = ( b AND c )");
            NError += TestOneStatement(p, "LET a=b OR c", "LET a = ( b OR c )");
            NError += TestOneStatement(p, "LET a=b AND c OR d AND e", "LET a = ( ( b AND c ) OR ( d AND e ) )");
            NError += TestOneStatement(p, "LET a=INPUT", "LET a = INPUT");
            NError += TestOneStatement(p, "LET a=INPUT DEFAULT 10", "LET a = INPUT DEFAULT 10");
            NError += TestOneStatement(p, "LET a=INPUT PROMPT 20", "LET a = INPUT PROMPT 20");
            NError += TestOneStatement(p, "LET a=INPUT DEFAULT 10 PROMPT 20", "LET a = INPUT DEFAULT 10 PROMPT 20");

            NError += TestOneStatement(p, "LET a=b$", "LET a = b$");
            NError += TestOneStatement(p, "LET a=b.c", "LET a = b.c");
            NError += TestOneStatement(p, "LET a=b.cd.efg", "LET a = b.cd.efg");
            NError += TestOneStatement(p, "LET a=b.cd.efg.hijk", "LET a = b.cd.efg.hijk");

            // Hard to parse
            NError += TestOneStatement(p, "LET a= 0x", "ERROR");
            NError += TestOneStatement(p, "LET a= .", "ERROR");
            NError += TestOneStatement(p, "LET a= .Escape", "ERROR");


            return NError;
        }


        private int TestNEXT(StatementParser p)
        {
            int NError = 0;
            NError += TestOneStatement(p, "NEXT i", "NEXT i");
            return NError;
        }


        private int TestPAPER(StatementParser p)
        {
            int NError = 0;
            NError += TestOneStatement(p, "PAPER 10", "PAPER 10");
            NError += TestOneStatement(p, "PAPER BLACK", "PAPER COLOR BLACK");
            return NError;
        }


        private int TestPAUSE(StatementParser p)
        {
            int NError = 0;
            NError += TestOneStatement(p, "PAUSE 10", "PAUSE 10");
            return NError;
        }


        private int TestPLAY(StatementParser p)
        {
            int NError = 0;
            NError += TestOneStatement(p, "PLAY ABC", "PLAY PLAY ABC");
            NError += TestOneStatement(p, "PLAY STOP", "PLAY STOP");
            NError += TestOneStatement(p, "PLAY WAIT", "PLAY WAIT");
            NError += TestOneStatement(p, "PLAY ONNOTE ABC", "PLAY ONNOTE ABC");
            return NError;
        }


        private int TestPRINT(StatementParser p)
        {
            int NError = 0;
            NError += TestOneStatement(p, "IF (c>10) THEN PRINT c ELSE PRINT d", "IF ( ( ( c > 10 ) ) ) THEN PRINT c  ELSE PRINT d ");
            NError += TestOneStatement(p, "IF (c>10) THEN PRINT c, ELSE PRINT d", "IF ( ( ( c > 10 ) ) ) THEN PRINT c ,   ELSE PRINT d ");
            NError += TestOneStatement(p, "PRINT \"ABC\"", "PRINT \"ABC\" ");
            NError += TestOneStatement(p, "PRINT 1,2", "PRINT 1 , 2 ");
            NError += TestOneStatement(p, "PRINT 1;2", "PRINT 1 ; 2 ");
            NError += TestOneStatement(p, "PRINT 1;2;", "PRINT 1 ; 2 ;  ");
            NError += TestOneStatement(p, "PRINT AT 1,2 3", "PRINT AT 1 , 2 3 ");
            NError += TestOneStatement(p, "PRINT AT 1,2 3 ; AT 4,5 6", "PRINT AT 1 , 2 3 AT 4 , 5 6 ");

            // Blank print expressions are not allowed (but should be?) NError += TestOneStatement(p, "PRINT 1,,3", "PRINT 1 , , 3");
            return NError;
        }


        private int TestRAND(StatementParser p)
        {
            int NError = 0;
            NError += TestOneStatement(p, "RAND a", "RAND a");
            NError += TestOneStatement(p, "RAND 10+20", "RAND ( 10 + 20 )");
            return NError;
        }


        private int TestREAD(StatementParser p)
        {
            int NError = 0;
            NError += TestOneStatement(p, "READ a", "READ a");
            return NError;
        }


        private int TestREM(StatementParser p)
        {
            int NError = 0;
            NError += TestOneStatement(p, "REM this is a comment", "REM this is a comment");
            NError += TestOneStatement(p, "REM A", "REM A");
            //This isn't how comments work :-) NError += TestOneStatement(p, "REM this\nis a comment\n", "REM this\nis a comment");
            return NError;
        }


        private int TestRETURN(StatementParser p)
        {
            int NError = 0;
            NError += TestOneStatement(p, "RETURN", "RETURN");
            NError += TestOneStatement(p, "RETURN 10", "RETURN 10");
            NError += TestOneStatement(p, "RETURN 10+20", "RETURN ( 10 + 20 )");
            return NError;
        }


        private int TestSPEAK(StatementParser p)
        {
            int NError = 0;
            NError += TestOneStatement(p, "SPEAK LIST VOICES", "SPEAK LIST VOICES");
            NError += TestOneStatement(p, "SPEAK VOICE A 10", "SPEAK VOICE A 10");
            NError += TestOneStatement(p, "SPEAK 10", "SPEAK 10");
            return NError;
        }


        private int TestSTOP(StatementParser p)
        {
            int NError = 0;
            NError += TestOneStatement(p, "END", "END");
            NError += TestOneStatement(p, "STOP", "STOP");
            NError += TestOneStatement(p, "STOP 10+20", "STOP ( 10 + 20 )");
            NError += TestOneStatement(p, "END IF", "END IF");
            return NError;
        }


        private int TestCompileSpeed (StatementParser p, int nlines)
        {
            int NError = 0;
            var sb = new StringBuilder();
            for (int i=0; i<nlines; i++)
            {
                sb.Append ("LET a=b AND c OR d AND e\n");
            }
            var program = sb.ToString();

            var start = DateTime.UtcNow;

            var lines = p.lexer.SplitIntoLines(program);
            var nparseErrors = 0;
            foreach (var tuple in lines)
            {
                var line = tuple.Item2;
                var result = p.ParseStatement(new ROS (line));
                if (result.Status.Result != ParserStatus.ResultType.Success) nparseErrors++;
            }


            var end = DateTime.UtcNow;
            var delta = end.Subtract(start);
            var seconds = delta.TotalSeconds;
            var linesPerSecond = nlines / seconds;
            if (nparseErrors > 0)
            {
                NError += 1;
                System.Diagnostics.Debug.WriteLine($"ERROR: {nparseErrors} while compiling for speed.");
            }
            if (linesPerSecond < 500)
            {
                NError += 1;
                System.Diagnostics.Debug.WriteLine($"ERROR: compile speed is only {linesPerSecond} for {nlines} lines.");
            }
            if (NError == 0)
            {
                System.Diagnostics.Debug.WriteLine($"INFO: compile speed is {linesPerSecond} for {nlines} lines.");
            }

            return NError;
        }


        private int TestOneStatement (StatementParser p, string statement, string expected)
        {
            int NError = 0;
            var shouldBeError = expected.StartsWith("ERROR");

            try
            {
                var status = p.ParseStatement(new ROS (statement));
                if (status.Status.Result != ParserStatus.ResultType.Success)
                {
                    if (!shouldBeError)
                    {
                        System.Diagnostics.Debug.WriteLine($"ERROR: Edit.TestOneStatement: {statement} was not parsed {status.Status.Error}");
                        NError += 1;
                    }
                    return NError;
                }
                var actual = status.Result.ToString();
                if (actual != expected)
                {
                    if (!shouldBeError)
                    {
                        System.Diagnostics.Debug.WriteLine($"ERROR: Edit.TestOneStatement: {statement} expected {expected} got actual {actual} instead");
                        NError += 1;
                    }
                    return NError;
                }
            }
            catch (Exception e)
            {
                // Exceptions are never allowed even for ERROR types
                System.Diagnostics.Debug.WriteLine($"ERROR: Edit.TestParser: exception {e.Message} for {statement}");
                NError += 1;
            }
            return NError;
        }
    }
}
