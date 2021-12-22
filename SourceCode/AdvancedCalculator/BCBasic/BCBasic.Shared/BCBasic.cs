using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
// TODO: Need a factory for AdvancedCalculator.BCBasic.RunTimeLibrary.MmlNotePlayer

namespace BCBasic
{
    public interface IObjectValue : IDisposable
    {
        string PreferredName { get; }
        BCValue GetValue(string name);
        void SetValue(string name1, BCValue value);
        IList<string> GetNames();
        void InitializeForRun(); // Called before each run 
        void RunComplete(); // The program has finished running and services can be cleaned up

        // IObjectValue can also call a function by name
        Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval);
    }

    public interface IExpression
    {
        Task<BCValue> EvalAsync(BCRunContext context);
    }

    public interface IConsole
    {
        Task ClsAsync(BCColor background, BCColor foreground, BCGlobalConnections.ClearType clearType);
        Task<string> GetInputAsync(CancellationToken ct, string prompt, string defaultValue);
        void Console(string str);
        string GetAt(int row, int col, int nchar);
        void PrintAt(PrintExpression.PrintSpaceType pst, string str, int row, int col);
        string Inkeys();
        BCColor GetBackground();
        BCColor GetForeground();
    }



    public abstract class BCObject : IObjectValue
    {
        public abstract string PreferredName { get; }
        public BCValue GetValue(string name) { return new BCValue();  } // return the null object
        public void SetValue(string name, BCValue value) {  } // Cannot set
        public IList<string> GetNames() { return new List<string>(); } // empty list
        public void InitializeForRun() { } // Called before each run 
        public void RunComplete() { }

        // IObjectValue can also call a function by name
        public async Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            switch (name)
            {
                case "ToString":
                    Retval.AsString = $"Object {PreferredName}";
                    return RunResult.RunStatus.OK;
                default:
                    await Task.Delay(0);
                    return RunResult.RunStatus.ErrorContinue;
            }
        }

        public void Dispose()
        {
        }
    }

    public interface IHaveFunctions
    {
        //Task<bool> IsSetAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval);
        Dictionary<String, IFunction> GetFunctions();
    }

    public class CompileError : IEquatable<CompileError>
    {
        public int charindex;
        public int line;
        public int column;
        public string message;

        public bool Equals(CompileError other)
        {

            //Check whether the compared object is null.
            if (Object.ReferenceEquals(other, null)) return false;

            //Check whether the compared object references the same data.
            if (Object.ReferenceEquals(this, other)) return true;

            //Check whether the products' properties are equal.
            return line.Equals(other.line) && column.Equals(other.column) && message.Equals(other.message);
        }

        // If Equals() returns true for a pair of objects 
        // then GetHashCode() must return the same value for these objects.
        public override int GetHashCode()
        {
            int hm = message == null ? 0 : message.GetHashCode();
            int hc = column.GetHashCode();
            int hl = line.GetHashCode();

            return hm ^ hc ^ hl;
        }
    }

    public class CompileResult
    {
        public BCBasicProgram Program = null;
        public IList<CompileError> Errors = new List<CompileError>();
        public double ParseTime = 0;
        public double EvalTime = 0;
        public double PrintTime = 0;
    }

    public class ErrorLogger
    {
        private static IConsole DoConsole = null;
        private static BCGlobalConnections ExternalConnections = null;
        public static int NError = 0;
        public static int Line = 0;
        public static void SetConsole (IConsole console)
        {
            DoConsole = console;
        }
        public static void SetExternals (BCGlobalConnections connections)
        {
            ExternalConnections = connections;
        }

        public static void WriteCreateError(string str)
        {
            var linestr = (Line == 0) ? "" : $"Line {Line}: ";
            str = "Create Error: " + linestr + str;
            NError++;
            if (DoConsole != null) DoConsole.Console(str);
            if (ExternalConnections != null) ExternalConnections.WriteLine(str);
            else System.Diagnostics.Debug.WriteLine(str);
        }
    }

    public class BCBasicProgram
    {
        public BCRunContext BCContext { get; internal set; }
        public BCBasicProgram(BCGlobalConnections externals = null)
        {
            BCContext = new BCRunContext(externals);
            BCProgram = null; // Will need to be set when it's run
        }
        public BCProgram BCProgram { get; set;  }

        // Common function to compile a program.  Writes error data out to the provided context (which can be null)
        public static async Task<CompileResult> CompileAsync(BCGlobalConnections connections, IConsole console, BCProgram program, string code)
        {
            DateTime startTime = DateTime.UtcNow;
            CompileResult Retval =new CompileResult();
            ErrorLogger.SetConsole(console);
            ErrorLogger.NError = 0;
            try
            {
                Retval.Program = new BCBasicProgram();
                var pp = new Edit.BCBasicStatementAdapter();
                var statementList = pp.Parse(code); // This wraps the real compiler.
                foreach (var item in statementList)
                {
                    Retval.Program.AddStatement(item);
                }
                Retval.Program.BCContext.ExternalConnections = connections;
                Retval.Program.BCProgram = program;
                await Task.Delay(0);
            }
            catch (Exception)
            {
                BCRunContext.AddError("Code compiles but cannot run (A)");
            }
            DateTime endTime = DateTime.UtcNow;
            Retval.ParseTime = endTime.Subtract(startTime).TotalSeconds;
            startTime = endTime;


            endTime = DateTime.UtcNow;
            Retval.EvalTime = endTime.Subtract(startTime).TotalSeconds;
            startTime = endTime;


            // Make sure that each statement was actually compiled
            var statements = Retval.Program?.GetErrorStatements();
            if (statements != null)
            {
                foreach (var item in statements)
                {
                    if (item.Statement == null)
                    {
                        var err = "Line did not compile correctly";
                        if (console != null) console.Console($"ERROR: line {item.SourceCodeLine} error {err}");
                        Retval.Errors.Add(new CompileError() { column = 1, line = item.SourceCodeLine, message = err, charindex = 0 });
                    }
                    else if (item.StatementError != "")
                    {
                        var err = item.StatementError;
                        if (console != null) console.Console($"ERROR: line {item.SourceCodeLine} error {err}");
                        Retval.Errors.Add(new CompileError() { column = 1, line = item.SourceCodeLine, message = err, charindex = 0 });

                    }
                }
            }
            if (console != null)
            {
                var errorCount = Retval.Errors.Count + ErrorLogger.NError;
                if (errorCount == 0)
                {
                    console.Console (string.Format("PARSE: {0} errors {1} statements", errorCount, Retval.Program.Statements.Count));
                }
                else
                {
                    console.Console(string.Format("PARSE: {0} errors", errorCount));
                }
            }
            endTime = DateTime.UtcNow;
            Retval.PrintTime = endTime.Subtract(startTime).TotalSeconds;
            startTime = endTime;

            return Retval;
        }

        private static Tuple<int, int> PosToLineRow(string str, int pos)
        {
            const string cr = "\r"; // used to be \r\n, but then I changed the editor...
            int line = 0;
            int col = pos;
            int startidx = 0;
            int idx = str.IndexOf(cr, startidx);
            while (idx < pos && idx >= 0)
            {
                line++;
                startidx = idx + 1;
                idx = str.IndexOf(cr, startidx);
            }
            col = pos - startidx;
            return new Tuple<int, int>(line, col);
        }

        List<FullStatement> Statements = new List<FullStatement>();
        /// <summary>
        /// Not all statements added with AddStatement are added to Statements. For example,
        /// FUNCTION abc(def) is added to the list of functions. The ErrorStatements exists
        /// so that we can examine the list of statmeents with errors later on.
        /// </summary>
        List<FullStatement> ErrorStatements = new List<FullStatement>();

        int functionDepth = 0;
        Function currFunction = null;
        Dictionary<string, ForData> currForData = new Dictionary<string, ForData>();
        List<If> currIfNesting = new List<If>();

        private void HandleForNext(FullStatement statement, int line)
        {
            var forStatement = statement.Statement as For;
            if (forStatement != null)
            {
                var fd = new ForData() { ForPC = line, ForStatement = forStatement };
                if (currForData.ContainsKey(forStatement.Variable))
                {
                    // ERROR: the for statement at line ... was not terminated 
                }
                currForData[forStatement.Variable] = fd;
            }
            var nextStatement = statement.Statement as Next;
            if (nextStatement != null)
            {
                if (currForData.ContainsKey(nextStatement.Variable))
                {
                    nextStatement.ForData = currForData[nextStatement.Variable];
                    currForData.Remove(nextStatement.Variable);
                }
                else
                {
                    // ERROR: no corresponding FOR statement was found!
                }
            }
        }

        public bool IsInIfStatement()
        {
            var Retval = currIfNesting.Count > 0;
            return Retval;
        }
        public void AddStatement(FullStatement statement)
        {
            // If there's no line number, then it's as handled as it's going to get.
            bool handledLineNumber = statement.LineNumber == null ? true : false;
            if (statement.StatementError != "") ErrorStatements.Add(statement); // This might be hidden otherwise.

            var returnStatement = statement.Statement as Return;
            if (returnStatement != null)
            {
                ; // NOTE: just a spot to hang the debugger.
            }
            If completedIfStatement = null;
            if (currIfNesting.Count > 0)
            {
                var ifstatement = currIfNesting[currIfNesting.Count - 1];
                int line = ifstatement.AddStatement(statement.Statement);
                if (ifstatement.CurrParseState == If.ParseState.Complete)
                {
                    currIfNesting.Remove(ifstatement);
                    completedIfStatement = ifstatement;
                }
                else
                {
                    // Doesn't do anything if the statement isn't a for or next loop.
                    HandleForNext(statement, line);
                    var thisifstatement = statement.Statement as If;
                    if (thisifstatement != null)
                    {
                        switch (thisifstatement.CurrParseState)
                        {
                            case If.ParseState.Complete:
                                //Nope!  Wasn't added, so no need to remove! completedIfStatement = thisifstatement;
                                break;
                            default:
                                currIfNesting.Add(thisifstatement);
                                break;
                        }
                    }

                }
            }
            else
            {
                var f = statement.Statement as Function;
                var r = statement.Statement as Return;
                if (f != null)
                {
                    // It is a new function!
                    currFunction = f;
                    BCContext.Functions[f.Name] = f;
                    functionDepth++;
                }
                else if (functionDepth > 0) // Inside a function
                {
                    var isFunctionReturn = currFunction.AddStatment(statement);
                    if (isFunctionReturn) functionDepth--;
                    //NOTE: This was an old error -- what if the return statement is in an IF statement?
                    //if (r != null) functionDepth--;
                }
                else
                {
                    int line = Statements.Count;
                    Statements.Add(statement);
                    if (statement.LineNumber != null)
                    {
                        handledLineNumber = true;
                        if (BCContext.LineNumbers.ContainsKey(statement.LineNumber))
                        {
                            var origindex = BCContext.LineNumbers[statement.LineNumber];
                            var origstatement = Statements[origindex];
                            statement.StatementError = $"Line number {statement.LineNumber} was already used by {origstatement.Statement}";
                            ErrorStatements.Add(statement);
                        }
                        else
                        {
                            BCContext.LineNumbers.Add(statement.LineNumber, line);
                        }
                    }
                    HandleForNext(statement, line);
                    var thisifstatement = statement.Statement as If;
                    if (thisifstatement != null)
                    {
                        switch (thisifstatement.CurrParseState)
                        {
                            case If.ParseState.Complete:
                                completedIfStatement = thisifstatement;
                                break;
                            default:
                                currIfNesting.Add(thisifstatement);
                                break;
                        }
                    }
                }
            }

            if (completedIfStatement != null)
            {
                var lists = new List<IList<IStatement>>() { completedIfStatement.Statements, completedIfStatement.ElseStatements };
                foreach (var list in lists)
                {
                    foreach (var substatement in list)
                    {
                        var ifnextStatement = substatement as Next;
                        if (ifnextStatement != null)
                        {
                            if (currForData.ContainsKey(ifnextStatement.Variable))
                            {
                                ifnextStatement.ForData = currForData[ifnextStatement.Variable];
                                // Nope, don't remove if it is was an IF statement. currForData.Remove(nextStatement.Variable);
                            }
                            else
                            {
                                // ERROR: no corresponding FOR statement was found!
                            }
                        }
                    }
                }
            }
            if (!handledLineNumber)
            {
                // Error: can't have line numbers in IF statements
                statement.StatementError = $"ERROR: can't put a line number on this statement";
            }
        }

        public void SetupDataStatements()
        {
            BCContext.ProgramRunContext.ClearDataStatements();
            foreach (var statement in Statements)
            {
                var data = statement.Statement as Data;
                if (data == null) continue;
                BCContext.ProgramRunContext.AddDataStatement(data);
            }
        }

        public void ClearStatements()
        {
            Statements.Clear();
            BCContext.LineNumbers.Clear();
        }

        public IList<FullStatement> GetStatements()
        {
            return Statements;
        }
        public IList<FullStatement> GetErrorStatements()
        {
            return ErrorStatements;
        }

        public async Task RunAsync(CancellationToken ct, bool keepVariables = false)
        {
            try
            {
                BCContext.Init(keepVariables);
                BCContext.ct = ct;
                BCContext.ProgramRunContext.Tracing.ClearTraceCounts();
                BCContext.ProgramRunContext.Tracing.Context = BCContext;
                SetupDataStatements();
                await ContinueAsync();
                //Task t = ContinueNoAsync();
                //await t;
            }
            catch (Exception ex)
            {
                BCContext.ProgramRunContext.Tracing.CurrCount.NException++;
                BCContext.ExternalConnections.WriteLine("ERROR: error while running program:" + ex.Message);
                BCRunContext.AddError($"Exception while running program is {ex.Message}");
            }
            BCContext.ProgramRunContext.Tracing.TraceComplete();
            BCContext.Finish();
            Play.RunComplete(); // In case there is music playing.
        }

        static int NContinueAsync = 0;
        public Task ContinueNoAsync()
        {
            Task t = Task.Run(async () => { await ContinueAsync(); });
            return t;
        }

        public async Task ContinueAsync()
        {
            int ContinueIndex = NContinueAsync++; // Used for debugging; keeps track of continues.
            BCContext.PCDelta = 1; // the default common value

            while (BCContext.PC >= 0 && BCContext.PC < Statements.Count)
            {
                if (BCContext.IsCancellationRequested) break;
                var item = Statements[BCContext.PC];
                var oldPC = BCContext.PC;

                try
                {
                    await item.Statement.RunAsync(BCContext);
                }
                catch (Exception ex)
                {
                    // The statement threw an exception.  Log it and stop running.
                    BCRunContext.AddError($"Unable to evaluate {item.ToString()} because {ex.Message}");
                    BCContext.ProgramRunContext.Tracing.CurrCount.NException++;
                    BCContext.PC = -1; // Stop execution!
                    BCRunContext.AddError($"Exception while evaluating {item.ToString()} is {ex.Message}");
                }
                await BCContext.ProgramRunContext.IncrementStatementTracingCount(item);

                if (BCContext.PC != oldPC || BCContext.PCIsChanged)
                {
                    BCContext.PCDelta = BCContext.PC - oldPC;
                    BCContext.PCIsChanged = false;
                    // The command updated the PC on us; believe it!
                }
                else
                {
                    BCContext.PC += 1;
                    BCContext.PCDelta = 1;
                }
            }
        }


#if NEVER_EVER_DEFINED
        public async Task RunInteractiveAsync(IList<FullStatement> list)
        {
            var originalPC = BCContext.PC;
            BCContext.PC = -2; // Make up an interactive mode
            foreach (var item in list)
            {
                var oldPC = BCContext.PC;
                await item.Statement.RunAsync(BCContext);
                if (BCContext.PC != oldPC)
                {
                    // The command updated the PC on us; believe it!
                    await ContinueAsync();
                    BCContext.PC = -2; // Make up an interactive mode
                }
            }
            BCContext.PC = originalPC;
        }
#endif

        public static async Task EvalInteractiveAsync(BCRunContext context, IList<FullStatement> list)
        {
            var originalPC = context.PC;
            context.PC = -2; // Make up an interactive mode
            foreach (var item in list)
            {
                var oldPC = context.PC;
                await item.Statement.RunAsync(context);
                if (context.PC != oldPC || context.PCIsChanged)
                {
                    context.PCIsChanged = false;
                    // The command updated the PC on us; ignore it (because we're being an expression evaluator)
                    //await ContinueAsync();
                    context.PC = -2; // Make up an interactive mode
                }
            }
            context.PC = originalPC;
        }


    }

    public class FullStatement
    {
        public FullStatement(string lineNumber, IStatement statement)
        {
            LineNumber = lineNumber;
            Statement = statement;
        }
        /// <summary>
        /// The user-supplied line number (10, 20, 100, etc), not the line number from the file.
        /// </summary>
        public string LineNumber { get; set; }
        /// <summary>
        /// The line number in the source file, starting at 1 like humans (mostly) expect.
        /// </summary>
        public int SourceCodeLine { get; set; } = 777;
        public IStatement Statement { get; set; }
        public string StatementError { get; set; } = "";
        public override string ToString()
        {
            if (LineNumber == "" || LineNumber == null) return Statement.ToString();
            return string.Format("{0} {1}", LineNumber, Statement.ToString());
        }

        public static string ReplaceMinus(string str)
        {
            // "-−–"
            var retval = str.Replace('−', '-').Replace('–', '-');
            return retval;
        }
        public static string ReplaceMultiply(string str)
        {
            // OPP9 -> @"(\*|×|⋅|·|/)"; 
            var retval = str.Replace('×', '*').Replace('⋅', '*').Replace('·', '*');
            return retval;
        }
    }

    public interface IStatement
    {
        Task RunAsync(BCRunContext context);
    }

    class AssertExpression : IStatement
    {
        InfixExpression Expression = null;
        // The expression is an ExpressionP5 with an equality
        public AssertExpression(InfixExpression expression)
        {
            Expression = expression;
        }
        public async Task RunAsync(BCRunContext context)
        {
            TestRunner tr = context.ProgramRunContext.CurrTestRunner;

            // Equality statements are always 0 or 1
            string result = "";
            var testResult = (await Expression.EvalAsync(context)).AsDouble;
            var location = tr?.FunctionProgramAndPackageName ?? "(unknown context)";
            if (testResult == 1.0)
            {
                // Example:
                // PASS: Distance(100) ≅ 12.2 from TEST_Distance_Common in  EX: Space and Astronomy

                result = $"✔: {Expression.ToString()} in {location}\n";
                if (tr != null) tr.AddTestPass(result);
            }
            else
            {
                // Example:
                // FAIL: Distance(100) ≅ 12.2 expected ___ from TEST_Distance_Common in  EX: Space and Astronomy

                result = $"✘: ERROR with {Expression.ToString()}\n    expected {Expression.CalculatedRight} actual value {Expression.CalculatedLeft}\n    in {location}\n";
                if (tr == null)
                {
                    context.ExternalConnections.WriteLine(result);
                }
                else
                {
                    tr.AddTestError(result);
                }
            }
        }
    }
    class Beep : IStatement
    {
        public static void RunComplete()
        {
            if (cts != null)
            {
                cts.Cancel();
                cts = null;
            }
        }
#if !WINDOWS8
        static IMmlNotePlayer mml = null;
#endif
        static CancellationTokenSource cts = null;
        public IExpression Duration { get; set; }
        public IExpression Pitch { get; set; }
        public Beep() 
        {
            Duration = null;
            Pitch = null;
        }
        public Beep(IExpression duration, IExpression pitch)
        {
            Duration = duration;
            Pitch = pitch;
        }

        public async Task RunAsync(BCRunContext context)
        {
#if !WINDOWS8
            int pitchValue = Pitch == null ? 0 : (int)(await Pitch.EvalAsync(context)).AsDouble; // is a number -x...0...x where 0==middle C
            double durationValue = Duration == null ? 0.25 : (await Duration.EvalAsync(context)).AsDouble;

            bool ctsIsNew = false;
            if (cts == null || cts.IsCancellationRequested)
            {
                cts = new CancellationTokenSource();
                ctsIsNew = true;
            }

            if (mml == null) mml = new AdvancedCalculator.BCBasic.RunTimeLibrary.MmlNotePlayer(cts.Token);
            else if (ctsIsNew) mml.SetCancellationToken(cts.Token);


            var octave = 4 + Math.Floor(pitchValue / 12.0);
            if (octave < 0) octave = 0;
            if (octave > 8) octave = 8;
            var note = pitchValue % 12;
            while (note < 0) note += 12;
            var noteChar = "CCDDEFFGGAAB"[note];
            var length = (int)(1.0 / durationValue);
            if (length < 1) length = 1;
            if (length > 16) length = 16;

            var mmlString = $"T240 I81 O{octave} {noteChar}{length}"; // quarter note
            mml.AddMmlString(mmlString);
            //var notes = MusicMacroLanguage.MmlNote.Parse(mmlString, MusicMacroLanguage.MmlNote.MMLType.Modern);
            //mml.AddNotes(notes);
#endif
            if (context.AlwaysDelayEval) await Task.Delay(10);
        }
        public override string ToString() { return String.Format("BEEP"); }
    }

    class Call : IStatement
    {
        public Call(IExpression expression) { Expression = expression; }
        public IExpression Expression { get; set; }
        public async Task RunAsync(BCRunContext context)
        {
            var status = await Expression.EvalAsync(context);
            if (status.CurrentType == BCValue.ValueType.IsError)
            {
                context.WriteEvalError($"CALL {Expression.ToString()} {status.AsString}");
            }

        }
        public override string ToString() { return String.Format("CALL {0}", Expression); }
    }
    class Cls : IStatement
    {
        public IExpression BackgroundExpression { get; set; }
        public IExpression ForegroundExpression { get; set; }
        public BCColor BackgroundColor { get; set; }
        public BCColor ForegroundColor { get; set; }

        public Cls ()
        {
            BackgroundColor = null;
            ForegroundColor = null;
            BackgroundExpression = null;
            ForegroundExpression = null;
        }
        public Cls (int colorIndex)
        {
            BackgroundColor = new BCColor(colorIndex);
            ForegroundColor = new BCColor(colorIndex);
            BackgroundExpression = null;
            ForegroundExpression = null;
        }
        public Cls(string colorName)
        {
            BackgroundColor = new BCColor(colorName);
            ForegroundColor = null;
            BackgroundExpression = null;
            ForegroundExpression = null;
        }
        public Cls(string backgroundColorName, string foregroundColorName)
        {
            BackgroundColor = new BCColor(backgroundColorName);
            ForegroundColor = foregroundColorName == null ? null : new BCColor(foregroundColorName);
            BackgroundExpression = null;
            ForegroundExpression = null;
        }
        public Cls(IExpression expression)
        {
            BackgroundColor = null;
            ForegroundColor = null;
            BackgroundExpression = expression;
            ForegroundExpression = null;
        }
        public Cls(IExpression background, IExpression foreground)
        {
            BackgroundColor = null;
            ForegroundColor = null;
            BackgroundExpression = background;
            ForegroundExpression = foreground;
        }

        public async Task RunAsync(BCRunContext context)
        {
            //if (context.AlwaysDelayEval) await Task.Delay(10); 
            if (BackgroundExpression != null)
            {
                var value = await BackgroundExpression.EvalAsync(context);
                if (value.CurrentType == BCValue.ValueType.IsString) BackgroundColor = new BCColor(value.AsString);
                else BackgroundColor = new BCColor(value.AsInt);
            }
            if (ForegroundExpression != null)
            {
                var value = await ForegroundExpression.EvalAsync(context);
                if (value.CurrentType == BCValue.ValueType.IsString) ForegroundColor = new BCColor(value.AsString);
                else ForegroundColor = new BCColor(value.AsInt);
            }
            await context.ExternalConnections.ClsAsync(BackgroundColor, ForegroundColor);
        }
        public override string ToString() { return "CLS"; }
    }

    class Console: IStatement
    {
        private IList<IExpression> Expressions { get; set; }
        public Console()
        {
            Expressions = new List<IExpression>();
        }
        public Console(IList<IExpression> list)
        {
            Expressions = new List<IExpression>();
            foreach (var item in list)
            {
                Expressions.Add(item);
            }
        }
        public void AddExpression (IExpression expression)
        {
            Expressions.Add(expression);
        }
        public async Task RunAsync(BCRunContext context)
        {
            //if (context.AlwaysDelayEval) await Task.Delay(10);
            foreach (var item in Expressions)
            {
                var d = await item.EvalAsync(context);
                context.ExternalConnections.WriteLine(d.AsString);
            }
        }
        public override string ToString() { return "CONSOLE"; }
    }

    public class Data : IStatement
    {
        public IList<BCBasic.IExpression> DataList { get; internal set; }
        public Data (IList<IExpression> allData)
        {
            DataList = allData;
        }
        // Running a DATA statement doesn't actually do anything
        public async Task RunAsync(BCRunContext context)
        {
            if (context.AlwaysDelayEval) await Task.Delay(10);
        }
        public override string ToString() { return $"DATA"; }
    }

    class Dim : IStatement
    {
        public Dim(string variable, IExpression size1, IExpression size2)
        {
            Variable = variable;
            Size1 = size1;
            Size2 = size2;
        }
        public string Variable { get; set; }
        public IExpression Size1 { get; set; }
        public IExpression Size2 { get; set; }
        public async Task RunAsync(BCRunContext context)
        {
            BCValue array;
            if (Size1 == null)
            {
                array = new BCBasic.BCValue(new BCValueList());
            }
            else
            {
                var size1 = (await Size1.EvalAsync(context)).AsInt;
                var valuelist = new BCValueList(size1);
                if (Size2 != null)
                {
                    var size2 = (await Size2.EvalAsync(context)).AsInt;
                    for (int i=0; i<size1; i++)
                    {
                        var row = new BCValueList(size2);
                        valuelist.data[i] = new BCValue (row);
                    }
                }
                array = new BCValue(valuelist);
            }
            context.Set(Variable, array);
            context.LastAssignment = array;
        }
        public override string ToString() { return String.Format("DIM {0}({1})", Variable, Size1 == null ? "" : Size1.ToString()); }
    }

    class Dump : IStatement
    {
        public async Task RunAsync(BCRunContext context)
        {
            if (context.AlwaysDelayEval) await Task.Delay(10); 
            foreach (var item in context.ListVariables())
            {
                string fmt = string.Format ("{0}\t{1}", item, context.GetValue(item, 0).AsString);
                context.ExternalConnections.WriteLine(fmt);
            }
            foreach (var pair in context.ExternalConnections.Externals)
            {
                var external = pair.Value;
                foreach (var name in external.GetNames())
                {
                    var value = external.GetValue(name);
                    string fmt = string.Format("{0}.{1}\t{2}", pair.Key, name, value);
                    context.ExternalConnections.WriteLine(fmt);

                }
            }
        }
        public override string ToString() { return "DUMP"; }
    }


    // The parser keeps track of these and adds them to the NEXT <variable> lines.
    class ForData
    {
        public For ForStatement { get; set; }
        public int ForPC { get; set; }
    }
    class For : IStatement
    {
        public For(string variable, IExpression from, IExpression to, IExpression step) 
        {
            Variable = variable;
            From = from;
            To = to;
            Step = step == null ? new NumericConstant(1.0) : step;
        }

        public string Variable { get; set; }
        public IExpression From { get; set; }
        public IExpression To { get; set; }
        public IExpression Step { get; set; }

        private bool Started = false;
        public bool IsStarted { get { return Started; } }
        public void ResetIsStarted() => Started = false;
        BCValue CurrValue = new BCValue(Double.NaN);

        public async Task RunAsync(BCRunContext context)
        {
            if (context.AlwaysDelayEval) await Task.Delay(10);
            // 
            // Example program:
#if EXAMPLE
            jtot = 0
            FOR i = 1 TO 10
                FOR j = 1 TO 10
                    IF (j > 5) THEN GOTO 10
                    jtot = jtot + 1
                NEXT j
                10 REM Cancel out of the J loop
            NEXT i
            ASSERT (jtot = 50)
#endif
            // In the example, the J loop is terminated early without warning.
            // This is only detected when we re-enter from directly above.
            //
            if (context.PCDelta == 1)
            {
                Started = false;
            }
            if (!Started)
            {
                Started = true;
                var value = context.GetValue(Variable, double.NaN);
                CurrValue.AsDouble = (await From.EvalAsync(context)).AsDouble;
                context.Set(Variable, CurrValue.AsDouble);
            }
            else
            {
            }
        }

        public async Task<bool> UpdateVariableAsync(BCRunContext context)
        {
            CurrValue.AsDouble = context.GetValue(Variable, double.NaN).AsDouble;
            var step = (await Step.EvalAsync(context)).AsDouble;
            CurrValue.AsDouble += step;
            context.Set(Variable, CurrValue.AsDouble);
            var toValue = (await To.EvalAsync(context));
            double to = toValue.AsDouble;
            bool Retval = false;
            if (toValue.CurrentType == BCValue.ValueType.IsError)
            {
                // And error has happened; the TO value couldn't be evaluated
                Retval = false;
            }
            else if (step > 0)
            {
                Retval = (CurrValue.AsDouble <= to);
            }
            else if (step < 0)
            {
                Retval = (CurrValue.AsDouble >= to);
            }
            return Retval;
        }
        public override string ToString() { return String.Format("FOR {0}", Variable); }
    }

    class Next : IStatement
    {
        public Next(string variable)
        {
            Variable = variable;
            ForData = null;
        }
        public string Variable { get; set; }
        public ForData ForData { get; set; }
        public async Task RunAsync(BCRunContext context)
        {
            if (ForData == null) return;
            bool goBack = await ForData.ForStatement.UpdateVariableAsync(context);
            if (goBack)
            {
                context.PC = ForData.ForPC;
            }
            else
            {
                // Reset the loop for the next time through
                ForData.ForStatement.ResetIsStarted ();
            }
        }
        public override string ToString() { return string.Format("NEXT {0}", Variable); }
    }

    class Forever : IStatement
    {
        static bool CancelRequested = false;
        public enum ForeverType { Stop, Wait }
        ForeverType CommandType = ForeverType.Wait;
        public Forever(ForeverType type)
        {
            CommandType = type;
        }
        public async Task RunAsync(BCRunContext context)
        {
            switch (CommandType)
            {
                case ForeverType.Stop:
                    CancelRequested = true;
                    break;
                case ForeverType.Wait:
                    int ms = (int)20;
                    context.ProgramRunContext.Tracing.CurrCount.NForever.DoStart();
                    while (!context.ct.IsCancellationRequested && CancelRequested == false)
                    {
                        try
                        {
                            await Task.Delay(ms, context.ct);
                            // Also update all of the AutoGraphs.  
                            await context.ExternalConnections.RunOnAll(context, "Update"); // Call update on everything.
                            if (context.ProgramRunContext.HaveCallbackToCall())
                            {
                                await context.ProgramRunContext.CallAllCallbacksAsync();
                            }
                        }
                        catch (Exception)
                        {
                            // Might get an exception from the task being cancelled.
                        }
                    }
                    context.ProgramRunContext.Tracing.CurrCount.NForever.DoEnd();
                    CancelRequested = false; // Allows multiple stops.
                    break;
            }
        }
        public override string ToString() { return String.Format($"FOREVER {CommandType.ToString()}"); }
    }


    public class FunctionArglistDefine
    {
        public IList<string> ArgNames { get; set; }
        public void AddArg(string name)
        {
            ArgNames.Add(name);
        }
        public FunctionArglistDefine()
        {
            ArgNames = new List<string>();
        }
    }


    public class Function : IStatement, IFunction
    {
        public string Name { get; set; }
        public Function (string name, FunctionArglistDefine arglist)
        {
            Name = name;
            ArgList = arglist;
            Body = new BCBasicProgram();
        }
        private FunctionArglistDefine ArgList;
        private BCBasicProgram Body;
        private List<For> AllForStatements = new List<For>();
        // Returns TRUE if the statement is the function's main RETURN statement
        public bool AddStatment (FullStatement statement)
        {
            var asFor = statement.Statement as For;
            if (asFor != null) AllForStatements.Add(asFor);
            var isAReturn = (statement.Statement is Return);
            var isFunctionReturn = isAReturn && !Body.IsInIfStatement();
            Body.AddStatement(statement);
            return isFunctionReturn;
        }

        public async Task RunAsync(BCRunContext context)
        {
            // We never actually "Run" a function; when the function is encountered,
            // the statements are put into a new object.  But the function statement itself
            // is not preserved.
            if (context.AlwaysDelayEval) await Task.Delay(10);
        }

        public async Task<BCValue> EvalAsync(BCRunContext context, IList<IExpression> argList)
        {
            // Parent context so that globals and function works plus external and ProgramContext
            Body.BCContext.CopyParentData(context);
            Body.BCProgram = null; // A function can't IMPORT FUNCTION FROM "" at all.
            Body.BCContext.Init();
            for (int i = 0; i < ArgList.ArgNames.Count; i++ )
            {
                var name = ArgList.ArgNames[i];
                var value = i < argList.Count ? await argList[i].EvalAsync(context) : new BCValue (Double.NaN);
                Body.BCContext.Set(name, value);
            }
            //Body.BCContext.ExternalConnections.DoConsole = context.ExternalConnections.DoConsole;
            Body.BCContext.CurrContextType = BCRunContext.ContextType.Function;
            foreach (var forstatement in AllForStatements)
            {
                if (forstatement.IsStarted)
                {
                    ; // Hook for debugging.
                }
                forstatement.ResetIsStarted ();
            }
            await Body.ContinueAsync();

            var Retval = Body.BCContext.ReturnValue;
            return Retval;
        }
        public override string ToString() { return "FUNCTION"; }
    }


    class Global : IStatement
    {
        // 2021-12-20: Global can now define multiple names in one go.
        public Global(List<string> variableNames) { foreach (var name in variableNames) VariableNames.Add(name); }
        //public Global(string variableName) { VariableNames.Add(variableName); }
        public List<string> VariableNames { get; } = new List<string>();
        //public string VariableName { get { return VariableNames[0]; } set { VariableNames[0] = value; } }
        public async Task RunAsync(BCRunContext context)
        {
            foreach (var name in VariableNames)
            {
                if (!context.Globals.Contains(name))
                {
                    context.Globals.Add(name);
                }
            }
            if (context.AlwaysDelayEval) await Task.Delay(10);
        }
        public override string ToString() {
            var names = "";
            foreach (var name in VariableNames)
            {
                if (names != "") names += ", ";
                names += name;
            }
            return $"GLOBAL {names}";
        }
    }


    class Gosub : IStatement
    {
        public Gosub(string lineNumber) { LineNumber = lineNumber; }
        public string LineNumber { get; set; }
        public async Task RunAsync(BCRunContext context)
        {
            if (context.AlwaysDelayEval) await Task.Delay(10);
            var nextline = LineNumber;
            if (context.LineNumbers.ContainsKey(nextline))
            {
                // Note: the IF statement will overwrite this value.
                // That's because the If statement resets the context.PC for
                // it's own reasons.
                context.PCStack.Push(context.PC + 1);
                context.PC = context.LineNumbers[nextline];
            }
            else
            {
                context.ExternalConnections.WriteLine(string.Format("ERROR: was asked to GOSUB {0}, but that line isn't known.", nextline));
                context.PC = context.PCError(context.PC);
            }
        }
        public override string ToString() { return String.Format("GOSUB {0}", LineNumber); }
    }


    class Goto : IStatement
    {
        public Goto(string lineNumber) { LineNumber = lineNumber; }
        public string LineNumber { get; set; }
        public async Task RunAsync(BCRunContext context)
        {
            if (context.AlwaysDelayEval) await Task.Delay(10);
            var nextline = LineNumber;
            if (context.LineNumbers.ContainsKey(nextline))
            {
                context.PC = context.LineNumbers[nextline];
                context.PCIsChanged = true;
            }
            else
            {
                context.ExternalConnections.WriteLine(string.Format("ERROR: was asked to GOTO {0}, but that line isn't known.", nextline));
                context.PC = context.PCError(context.PC);
            }
        }
        public override string ToString() { return String.Format("GOTO {0}", LineNumber); }
    }

    // EndIf and Else are pretend statements: they only exist to interface the 
    // parser and the overall statement-adder 
    class Else : IStatement
    {
        public Task RunAsync(BCRunContext context)
        {
            return Task.Delay(0);
        }
        public override string ToString()
        {
            return "ELSE";
        }
    }
    class EndIf : IStatement
    {
        public Task RunAsync(BCRunContext context)
        {
            return Task.Delay(0);
        }
        public override string ToString()
        {
            return "END IF";
        }
    }

    class ExpressionEval : IStatement
    {
        public ExpressionEval(IExpression expression)
        {
            Expression = expression;
        }
        public IExpression Expression { get; set; }
        BCValue Result { get; set; }
        public async Task RunAsync(BCRunContext context)
        {
            Result = new BCValue(await Expression.EvalAsync(context));
            context.LastAssignment = Result;
        }
        public override string ToString()
        {
            return $"EVAL {Expression.ToString()}";
        }
    }



    class If : IStatement
    {
        public string DebugName = "(justme)";
        public enum ParseState {  Complete, AddingIf, AddingElse };
        public ParseState CurrParseState;
        public If(IExpression expression, IStatement ifstatement, IStatement elsestatement)
        {
            Expression = expression;
            if (ifstatement != null)
            {
                Statements.Add(ifstatement);
                if (elsestatement != null) ElseStatements.Add(elsestatement);
                CurrParseState = ParseState.Complete;
            }
            else
            {
                CurrParseState = ParseState.AddingIf;
            }
        }
        public int AddStatement (IStatement statement)
        {
            int Retval = 0;
            if (CurrParseState == ParseState.AddingIf)
            {
                if (statement is Else)
                {
                    CurrParseState = ParseState.AddingElse;
                }
                else if (statement is EndIf)
                {
                    CurrParseState = ParseState.Complete;
                }
                else
                {
                    Statements.Add(statement);
                    Retval = Statements.Count - 1;
                }
            }
            else
            {
                if (statement is EndIf)
                {
                    CurrParseState = ParseState.Complete;
                }
                else
                {
                    ElseStatements.Add(statement);
                    Retval = ElseStatements.Count - 1;
                }
            }
            return Retval;
        }
        public IExpression Expression { get; set; }
        public IList<IStatement> Statements { get; } = new List<IStatement>();
        public IList<IStatement> ElseStatements { get; } = new List<IStatement>();
        public async Task RunAsync(BCRunContext context)
        {
            var yes = await Expression.EvalAsync(context);
            bool shouldDoIf = yes.AsDoubleToBoolean;
            if (shouldDoIf)
            {
                await DoStatementsAsync(context, Statements);
            }
            else 
            {
                await DoStatementsAsync(context, ElseStatements);
            }
        }
        public override string ToString() { return String.Format("IF () THEN "); }

        private async Task DoStatementsAsync(BCRunContext context, IList<IStatement> statements)
        {
            // The starting PC is the PC of the overall program.  
            // While we're in this if statement, reset it to be just the local block.
            // Then at the end, set it back.
            int startingPC = context.PC;
            context.PC = 0;
            bool didGoto = false;
            context.PCDelta = 1;
            while (context.PC >= 0 && context.PC < statements.Count && !context.PCIsChanged)
            {
                if (context.IsCancellationRequested) break;
                var item = statements[context.PC];
                var oldPC = context.PC;
                await item.RunAsync(context);


                await context.ProgramRunContext.IncrementStatementTracingCount(item);

                if (context.PC != oldPC || context.PCIsChanged)
                {
                    // The command updated the PC on us; believe it!
                    if (item is Gosub)
                    {
                        // Nuts: the GOSUB will have captured the wrong context.
                        // Fix it.
                        // Original line in the GOSUB: context.PCStack.Push(context.PC + 1);
                        context.PCStack.Pop();
                        context.PCStack.Push(startingPC + 1);
                        didGoto = true;
                    }
                    else if (item is Goto) // can only GOTO outside of an IF block.
                    {
                        didGoto = true;
                    }
                    context.PCDelta = context.PC - oldPC;
                }
                else
                {
                    context.PC += 1;
                    context.PCDelta = 1;
                }
            }

            // All done; now clean up.
            if (context.PC >= 0) // When context.PC is < 0 it means that a STOP statement was evaluated.
            {
                if (didGoto)
                {
                    context.PCIsChanged = true; // set to true so that the next level up will handle it.
                }
                else
                {
                    context.PC = startingPC;
                }
            }
        }
    }


    class Import : IStatement
    {
        public Import(string whatToImport, string importLocation)
        {
            WhatToImport = whatToImport;
            ImportLocation = importLocation;
        }
        public string WhatToImport { get; set; }
        public string ImportLocation { get; set; }
        public async Task RunAsync(BCRunContext context)
        {
            if (context.AlwaysDelayEval) await Task.Delay(10);
            switch (WhatToImport)
            {
                case "FUNCTIONS":
                    {
                        if (context.ExternalConnections.Package == null) break;

                        var functionCode = context.ExternalConnections.Package.GetProgram(ImportLocation);
                        var compileResults = await BCBasicProgram.CompileAsync(context.ExternalConnections, context.ExternalConnections.DoConsole, functionCode, functionCode.Code); // will get a new context.
                        var functions = compileResults.Program;
                        foreach (var function in functions.BCContext.Functions)
                        {
                            context.Functions[function.Key] = function.Value;
                        }
                    }
                    break;
                default:
                    // Can't actually reach this; the parser checks for FUNCTIONS
                    ErrorLogger.WriteCreateError($"IMPORT can only import FUNCTIONS, not {WhatToImport}");
                    break;
            }
        }
        public override string ToString() { return String.Format("IF () THEN "); }
    }

    class Input : IStatement
    {
        public Input(List<string> variables)
        {
            foreach (var variable in variables)
            {
                Variables.Add (variable);
            }
        }
        public List<string> Variables { get; set; } = new List<string>();
        public async Task RunAsync(BCRunContext context)
        {
            if (context.AlwaysDelayEval) await Task.Delay(10);
            foreach (var variable in Variables)
            {
                context.ProgramRunContext.Tracing.CurrCount.NInput.DoStart();
                string result = context.ExternalConnections.DoConsole != null ? await context.ExternalConnections.DoConsole.GetInputAsync(context.ct, variable, "") : "";
                context.ProgramRunContext.Tracing.CurrCount.NInput.DoEnd();
                BCValue value;
                if (variable.EndsWith("$"))
                {
                    value = new BCValue(result);
                }
                else
                {
                    double dvalue = Double.NaN;
                    Double.TryParse(result, out dvalue);
                    value = new BCValue(dvalue);
                }
                context.Set(variable, value);
            }
        }
        public override string ToString() { return String.Format("INPUT {0}", Variables[0]); }
    }


    class Let : IStatement
    {
        public Let(string variable, IExpression expression)
        {
            Variable = variable;
            Index1 = null;
            Index2 = null;
            Expression = expression;
        }
        public Let(VariableArrayValue variable, IExpression expression)
        {
            Variable = variable.VariableName;
            Index1 = variable.Index1;
            Index2 = variable.Index2;
            Expression = expression;
        }
        public string Variable { get; set; }
        public IExpression Index1 { get; set; }
        public IExpression Index2 { get; set; }
        public IExpression Expression { get; set; }
        public async Task RunAsync(BCRunContext context)
        {
            var right = new BCValue (await Expression.EvalAsync(context)); 
            if (Variable != "") // for expressions like <<= 10 + 20>>
            {
                if (Index1 == null) context.Set(Variable, right);
                else
                {
                    var index1 = await Index1.EvalAsync(context);
                    var index2 = Index2 == null ? null : await Index2.EvalAsync(context);
                    context.Set(Variable, index1.AsString, index2 == null ? null : index2.AsString, right);
                }
            }
            context.LastAssignment = right;
        }
        public override string ToString()
        {
            var variable = $"{Variable}";
            var idx2 = Index2 == null ? "" : $",{Index2.ToString()}";
            if (Index1 != null) variable = $"{Variable}({Index1.ToString()}{idx2})";
            return $"LET {variable}={Expression.ToString()}";
        }
    }

    class Paper : IStatement
    {
        public BCColor Color { get; set; }
        public IExpression Expression { get; set; }

        public Paper()
        {
            Color = null;
        }
        public Paper(IExpression expression)
        {
            //Color = new BCColor(colorIndex);
            Expression = expression;
        }
        public Paper(string colorName)
        {
            Color = new BCColor(colorName);
        }

        public async Task RunAsync(BCRunContext context)
        {
            if (context.AlwaysDelayEval) await Task.Delay(10);
            if (Color != null)
            {
                await context.ExternalConnections.ClsAsync(Color, null, BCGlobalConnections.ClearType.Paper);
            }
            else if (Expression != null)
            {
                var value = await Expression.EvalAsync(context);
                if (value.CurrentType == BCValue.ValueType.IsString)
                {
                    await context.ExternalConnections.ClsAsync(new BCColor(value.AsString), null, BCGlobalConnections.ClearType.Paper);
                }
                else
                {
                    await context.ExternalConnections.ClsAsync(new BCColor((int)value.AsDouble), null, BCGlobalConnections.ClearType.Paper);
                }
            }
        }
        public override string ToString() { return String.Format("PAPER {0}", Color.ToString()); }
    }



    class Pause : IStatement
    {
        public IExpression NFrame {get; set;}
        public Pause(IExpression nframe)
        {
            NFrame = nframe;
        }
        public async Task RunAsync(BCRunContext context)
        {
            var nframe = (await NFrame.EvalAsync(context)).AsDouble;
            // There are 60 frames/second
            var totalms = Math.Round ((nframe * 1000.0) / 60.0);
            var startTime = DateTime.UtcNow;
            try
            {
                context.ProgramRunContext.Tracing.CurrCount.NPause.DoStart();
                var mscurr = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
                var msleft = totalms - mscurr;
                while (msleft > 10)
                {
                    var mstowait = (int)Math.Min(msleft, 20); // Same as the class FOREVER value
                    await Task.Delay(mstowait, context.ct);
                    // Also update all of the AutoGraphs.  
                    await context.ExternalConnections.RunOnAll(context, "Update"); // Call update on everything.
                    if (context.ProgramRunContext.HaveCallbackToCall())
                    {
                        await context.ProgramRunContext.CallAllCallbacksAsync();
                    }
                    mscurr = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
                    msleft = totalms - mscurr;
                }
                context.ProgramRunContext.Tracing.CurrCount.NPause.DoEnd();
            }
            catch (Exception)
            {
                // Might get an exception from the task being cancelled.
            }
        }
        public override string ToString() { return String.Format("PAUSE {0}", NFrame.ToString()); }
    }


    class Play : IStatement
    {
        public enum PlayType {  Play, Stop, Wait, OnNote }

        public static void RunComplete()
        {
            if (cts != null)
            {
                cts.Cancel();
                cts = null;
            }
        }
#if !WINDOWS8
        static IMmlNotePlayer mml = null;
#endif
        static CancellationTokenSource cts = null;
        public IExpression Value { get; set; }
        public PlayType CommandType { get; set; } = PlayType.Play;
        public Play(IExpression value) // is null for the RETURN in a GOSUB type statement
        {
            Value = value;
            CommandType = PlayType.Play;
        }
        public Play (PlayType type)
        {
            Value = null;
            CommandType = type;
        }
        public Play(PlayType type, IExpression value) // e.g. for OnNote
        {
            Value = value;
            CommandType = type;
        }
        private bool EnsureCtsAndMml()
        {
            bool ctsIsNew = false;
            if (cts == null || cts.IsCancellationRequested)
            {
                cts = new CancellationTokenSource();
                ctsIsNew = true;
            }
#if !WINDOWS8
            if (mml == null) mml = new AdvancedCalculator.BCBasic.RunTimeLibrary.MmlNotePlayer(cts.Token);
            else if (ctsIsNew) mml.SetCancellationToken(cts.Token);
#endif
            return ctsIsNew;
        }
        public async Task RunAsync(BCRunContext context)
        {
#if !WINDOWS8
            string value;
            switch (CommandType)
            {
                case PlayType.OnNote:
                    EnsureCtsAndMml();
                    value = (await Value.EvalAsync(context)).AsString;
                    mml.SetFunction(context, value);
                    // // // mml.context = context;
                    // // // mml.functionName = value;
                    break;
                case PlayType.Play:
                    if (context.AlwaysDelayEval) await Task.Delay(10);
                    EnsureCtsAndMml();
                    value = (await Value.EvalAsync(context)).AsString;
                    mml.AddMmlString(value);
                    // // // var notes = MusicMacroLanguage.MmlNote.Parse(value, MusicMacroLanguage.MmlNote.MMLType.Modern);
                    // // // mml.AddNotes(notes);
                    break;
                case PlayType.Stop:
                    if (cts != null) cts.Cancel();
                    break;
                case PlayType.Wait:
                    const int ms = 10;
                    context.ProgramRunContext.Tracing.CurrCount.NPlayWait.DoStart();
                    while (mml != null && mml.IsPlaying() && !context.ct.IsCancellationRequested)
                    {
                        try
                        {
                            await Task.Delay(ms, context.ct);
                            // Also update all of the AutoGraphs.  
                            await context.ExternalConnections.RunOnAll(context, "Update"); // Call update on everything.
                            if (context.ProgramRunContext.HaveCallbackToCall())
                            {
                                await context.ProgramRunContext.CallAllCallbacksAsync();
                            }
                        }
                        catch (Exception)
                        {
                            // Might get an exception from the task being cancelled.
                        }
                    }
                    context.ProgramRunContext.Tracing.CurrCount.NPlayWait.DoEnd();

                    break;

            }
#else
            await Task.Delay(0);
#endif
        }
        public override string ToString() { return String.Format("PLAY {0}", Value?.ToString() ?? ""); }
    }



    public class PrintExpression
    {
        public enum PrintSpaceType { Default, At, Newline, NoSpace, Tab };
        public PrintExpression(PrintSpaceType type, IExpression row, IExpression col, IExpression value)
        {
            CurrSpaceType = type;
            Row = row;
            Col = col;
            Value = value;
        }
        public PrintSpaceType CurrSpaceType { get; set; }
        public IExpression Row { get; set; }
        public IExpression Col { get; set; }
        public IExpression Value { get; set; }
    }

    class PrintAt : IStatement
    {

        public IList<PrintExpression> Expressions { get; set; }
        public PrintAt(IList<PrintExpression> expressions)
        {
            Expressions = expressions;
        }
        public async Task RunAsync(BCRunContext context)
        {
            var potentialNewDefault = PrintExpression.PrintSpaceType.Newline;
            foreach (var item in Expressions)
            {
                bool reallyPrint = true;
                potentialNewDefault = PrintExpression.PrintSpaceType.Newline;
                double row = item.Row == null ? -1 : (await item.Row.EvalAsync(context)).AsDouble;
                double col = item.Col == null ? -1 : (await item.Col.EvalAsync(context)).AsDouble;
                if (row != -1 && col != -1)
                {
                    // Row and col are 1-based in Basic; reset them to be zero-based for the rest of the code.
                    row -= 1;
                    col -= 1;
                }
                string valueToPrint = "";
                if (item.Value == null)
                {
                    reallyPrint = false;
                    potentialNewDefault = item.CurrSpaceType;
                }
                else
                {
                    valueToPrint = (await item.Value.EvalAsync(context)).AsString;
                    valueToPrint = BCValue.ReplaceNewline(valueToPrint);
                }
                // If the last PRINT statement (from whatever function) was ended with a semicolon or comma
                // without anything to print (e.g. PRINT "A" ; ) then this print should be .NoSpace instead
                // of .NewLine.  Only the first expression in a PRINT can be default (the parser enforces that)
                // and if the last expression is nothing, then item.Value will be null. 
                var spaceType = item.CurrSpaceType == PrintExpression.PrintSpaceType.Default ? context.ProgramRunContext.CurrPrintSpaceType: item.CurrSpaceType;
                if (reallyPrint) context.ExternalConnections.PrintAt(spaceType, valueToPrint, (int)row, (int)col);
            }
            // In reality, this can't happen unless maybe the parser is messed up
            if (potentialNewDefault != PrintExpression.PrintSpaceType.Default)
            {
                context.ProgramRunContext.CurrPrintSpaceType = potentialNewDefault;
            }
        }
        public override string ToString() { return String.Format("PRINT"); }
    }


    class Rand : IStatement
    {
        public IExpression Value { get; set; }
        public Rand(IExpression value) 
        {
            Value = value;
        }
        public async Task RunAsync(BCRunContext context)
        {
            if (context.AlwaysDelayEval) await Task.Delay(10);
            var value = Value == null ? 0.0 : (await Value.EvalAsync(context)).AsDouble;
            if (value == 0.0)
            {
                BCRunContext.StaticRnd = new Random();
            }
            else
            {
                BCRunContext.StaticRnd = new Random((int)value);
            }
        }
        public override string ToString() { return String.Format("RAND {0}", Value.ToString()); }
    }

    class Read : IStatement
    {
        List<VariableValue> Variables = new List<VariableValue>();
        public Read(List<VariableValue> variables)
        {
            foreach (var variable in variables)
            {
                Variables.Add(variable);
            }
        }
        public async Task RunAsync(BCRunContext context)
        {
            foreach (var variable in Variables)
            {
                var value = context.ProgramRunContext.ReadData();
                var right = await value.EvalAsync(context); // This is always trivial because the value is always a constant.
                context.Set(variable.VariableName, right);
            }
        }
        public override string ToString() { return $"READ"; }
    }

    class Rem : IStatement
    {
        public Rem(string remark) { Remark = remark; }
        public string Remark { get; set; }
        public enum RemType {  REM, BLANKLINE };
        public RemType RemarkType = RemType.REM;
        public async Task RunAsync(BCRunContext context)
        {
            if (context.AlwaysDelayEval) await Task.Delay(10);
        }
        public override string ToString()
        {
            if (RemarkType == RemType.BLANKLINE) return $"REM BLANK LINE";
            return $"REM {Remark}";
        }
    }
    class Restore : IStatement
    {
        public Restore()
        {
        }
        public async Task RunAsync(BCRunContext context)
        {
            if (context.AlwaysDelayEval) await Task.Delay(10);
            context.ProgramRunContext.ResetReadIndex();
        }
        public override string ToString() { return $"RESTORE"; }
    }
    class Return: IStatement
    {
        public IExpression ReturnValue { get; set; }
        public Return(IExpression returnValue) // is null for the RETURN in a GOSUB type statement
        {
            ReturnValue = returnValue;
        }

        public async Task RunAsync(BCRunContext context)
        {
            if (context.AlwaysDelayEval) await Task.Delay(10);
            if (ReturnValue != null)
            {
                var value = await ReturnValue.EvalAsync(context);
                context.ReturnValue = value;
            }

            if (context.CurrContextType == BCRunContext.ContextType.Function)
            {
                if (context.PCStack.Count > 0)
                {
                    context.PC = context.PCStack.Pop();
                }
                else
                {
                    // We can just return from here!
                    context.PC = -1;
                }
            }
            else
            {
                if (context.PCStack.Count > 0)
                {
                    context.PC = context.PCStack.Pop();
                }
                else
                {
                    context.ExternalConnections.WriteLine(string.Format("ERROR: was asked to RETURN, but the GOSUB return stack is empty"));
                    context.PC = context.PCError(context.PC);
                }
            }
        }
        public override string ToString() { return String.Format("RETURN"); }
    }

    class Speak : IStatement
    {
        public static void RunComplete()
        {
            if (cts != null)
            {
                cts.Cancel();
                cts = null;
            }
        }
        static CancellationTokenSource cts = null;
        public IExpression Voice { get; set; }
        public IExpression Text { get; set; }
        public Speak(IExpression voice, IExpression text)
        {
            Voice = voice;
            Text = text;
        }

        public async Task RunAsync(BCRunContext context)
        {
            var mediaElement = context.ExternalConnections.CurrMediaElement;
            if (Voice != null) SetVoice((await Voice.EvalAsync(context)).AsString);
            var speak = (await Text.EvalAsync(context)).AsString;

            // The object for controlling the speech synthesis engine (voice).
            var synth = new Windows.Media.SpeechSynthesis.SpeechSynthesizer();
            if (preferredVoice != null) synth.Voice = preferredVoice;

            // Generate the audio stream from plain text.
            var stream = await synth.SynthesizeTextToStreamAsync(speak);

            // Send the stream to the media object.
            // uiMedia is a MediaElement
            if (mediaElement != null)
            {
                mediaElement.SetSource(stream, stream.ContentType);
                mediaElement.Play();
            }
        }

        // An exact match ("Microsoft David" is best.
        // Second best is a partial match like "David" for Microsoft David. 
        Windows.Media.SpeechSynthesis.VoiceInformation preferredVoice = null;
        private void SetVoice(string name)
        {
            Windows.Media.SpeechSynthesis.VoiceInformation secondVoice = null;
            foreach (var voice in Windows.Media.SpeechSynthesis.SpeechSynthesizer.AllVoices)
            {
                if (voice.DisplayName == name)
                {
                    preferredVoice = voice;
                }
                else if (voice.DisplayName.Contains (name))
                {
                    secondVoice = voice;
                }
            }
            if (preferredVoice == null) preferredVoice = secondVoice;
        }
        public override string ToString() { return String.Format("SPEAK"); }
    }

    class SpeakListVoices : IStatement
    {
        public SpeakListVoices()
        {
        }

        public async Task RunAsync(BCRunContext context)
        {
            foreach (var voice in Windows.Media.SpeechSynthesis.SpeechSynthesizer.AllVoices)
            {
                context.ExternalConnections.PrintAt(PrintExpression.PrintSpaceType.Newline, voice.DisplayName, -1, -1);
            }
            if (context.AlwaysDelayEval) await Task.Delay(10);
        }

        public override string ToString() { return String.Format("SPEAK LIST VOICES"); }
    }

    class Stop : IStatement
    {
        public bool IsSilent { get; set; } = false;
        public IExpression ReturnValue { get; set; }
        public Stop(IExpression returnValue, bool isSilent) // is null for the RETURN in a GOSUB type statement
        {
            ReturnValue = returnValue;
            IsSilent = isSilent;
        }

        public async Task RunAsync(BCRunContext context)
        {
            context.IsSilent = IsSilent;
            if (ReturnValue != null)
            {
                var value = await ReturnValue.EvalAsync(context);
                context.ReturnValue = value;
            }
            else
            {
                context.ReturnValue = new BCValue(); // is 'IsNoValue' by default
            }
            context.PC = -1; // Stop execution!
        }
        public override string ToString() { return String.Format("STOP"); }
    }

    public enum ExpressionType { Number, String };

    public interface IFunction
    {
        Task<BCValue> EvalAsync(BCRunContext context, IList<IExpression> argList);
    }

    class NumericConstant : IExpression
    {
        public NumericConstant(double value)
        {
            Value = new BCValue (value);
        }
        // Does a double.parse
        // Allows unicode MINUS (−) as well as the regular dash
        public NumericConstant(string value)
        {
            if (value == "∞")
            {
                Value = new BCValue (Double.PositiveInfinity);
            }
            else
            {
                value = FullStatement.ReplaceMinus(value);
                double dval;
                bool ok = Double.TryParse(value, out dval);
                if (!ok) dval = Double.NaN;
                Value = new BCValue(dval);
            }
        }
        public BCValue Value {get; set;}
        public async Task<BCValue> EvalAsync(BCRunContext context)
        {
            if (context.AlwaysDelayEval) await Task.Delay(10);
            return Value;
        }
        public override string ToString() { return Value.AsString;  }
    }

    class ObjectConstant : IExpression
    {
        public ObjectConstant(IObjectValue value)
        {
            if (value is BCValue) Value = value as BCValue;
            else Value = new BCValue(value);
        }
        public virtual ExpressionType GetExpressionType() { return ExpressionType.String; } //NOTE: what about objects???
        public BCValue Value { get; set; }
        public async Task<BCValue> EvalAsync(BCRunContext context)
        {
            if (context.AlwaysDelayEval) await Task.Delay(10);
            return Value;
        }
        public override string ToString() { return Value.AsString; }
    }

    class StringConstant : IExpression
    {
        public StringConstant(string value)
        {
            Value = new BCValue(value);
        }
        public virtual ExpressionType GetExpressionType() { return ExpressionType.String; }
        public BCValue Value { get; set; }
        public async Task<BCValue> EvalAsync(BCRunContext context)
        {
            if (context.AlwaysDelayEval) await Task.Delay(10);
            return Value;
        }
        public override string ToString() { return Value.AsString; }
    }

    class VariableValue : IExpression // also supports PI and RND
    {
        public VariableValue(string variableName)
        {
            VariableName = variableName;
        }
        public string VariableName { get; set; }
        public async Task<BCValue> EvalAsync(BCRunContext context)
        {
            if (context.AlwaysDelayEval) await Task.Delay(10);
            BCValue Retval = null;
            switch (VariableName)
            {
                case "PI": Retval = new BCValue(Math.PI); break;
                case "RND": Retval = new BCValue(BCRunContext.StaticRnd.NextDouble()); break;
                case "INKEY$": Retval = new BCValue(context.ExternalConnections.Inkeys()); break;
                default:
                    var status = context.GetValue(VariableName, 0.0, ref Retval);
                    if (!status && VariableName.Contains("."))
                    {
                        var names = VariableName.Split(new char[] { '.' });
                        BCValue realParent = null;
                        var parent = context.GetObject(names[0], out realParent);

                        if (parent != null)
                        {
                            for (int i = 1; i < names.Length - 2; i++)
                            {
                                var obj = parent?.GetValue(names[i]);
                                var newparent = obj?.AsObject;
                                if (newparent == null) newparent = obj; // each object is also an IObjectValue
                                parent = newparent;
                            }
                            if (parent != null)
                            {
                                Retval = parent.GetValue(names[names.Length - 1]);
                                if (realParent != null && realParent != parent && Retval.CurrentType == BCValue.ValueType.IsError && Retval.AsDouble == BCValue.NO_SUCH_PROPERTY)
                                {
                                    // Try it on the realParent
                                    Retval = realParent.GetValue(names[names.Length - 1]);
                                }
                            }
                        }
                        if (Retval == null)
                        {
                            Retval = BCValue.MakeError(1, $"No such variable {VariableName} exists");
                        }
                    }
                    break;
            }
            return Retval;
        }
        public override string ToString() { return VariableName; }
    }

    class VariableArrayValue : IExpression 
    {
        public VariableArrayValue(string variableName, IExpression index1, IExpression index2)
        {
            VariableName = variableName;
            Index1 = index1;
            Index2 = index2;
        }
        public VariableArrayValue(string variableName, IList<IExpression> expression)
        {
            VariableName = variableName;
            switch (expression.Count)
            {
                case 1:
                    Index1 = expression[0];
                    break;
                case 2:
                    Index1 = expression[0];
                    Index2 = expression[1];
                    break;
                case 0:
                    ErrorLogger.WriteCreateError($"{variableName}() must provide one size value");
                    break;
                default:
                    ErrorLogger.WriteCreateError($"{variableName}(size) must provide 1 or 2 size value, not {expression.Count}");
                    break;
            }
        }
        public string VariableName { get; set; }
        public IExpression Index1 { get; set; }
        public IExpression Index2 { get; set; }
        public async Task<BCValue> EvalAsync(BCRunContext context)
        {
            if (context.AlwaysDelayEval) await Task.Delay(10);
            BCValue Retval;
            var index1 = (await Index1.EvalAsync(context)).AsString;
            var index2 = Index2 == null ? "" : (await Index2.EvalAsync(context)).AsString;
            Retval = context.GetValue(VariableName, index1, index2, 0.0);
            return Retval;
        }
        public override string ToString() { return VariableName; }
    }

    public class InfixExpression : IExpression
    {
        public InfixExpression(IExpression left, string op, IExpression right)
        {
            Left = left;
            op = FullStatement.ReplaceMinus(op);
            op = FullStatement.ReplaceMultiply(op);
            Op = op;
            Right = right;
        }
        public IExpression Left { get; set; }
        public string Op {get; set;}
        public IExpression Right {get; set;}

        public BCValue CalculatedLeft { get; internal set; }
        public BCValue CalculatedRight { get; internal set; }

        public async Task<BCValue> EvalAsync(BCRunContext context)
        {
            if (context.AlwaysDelayEval) await Task.Delay(10);
            CalculatedLeft = await Left.EvalAsync(context);
            BCValue Retval = new BCValue(CalculatedLeft);
            bool mightBeShortCircuited = false;
            switch (Op)
            {
                case "AND": mightBeShortCircuited = true; break;
                case "OR": mightBeShortCircuited = true; break;
            }
            CalculatedRight = mightBeShortCircuited ? null :  await Right.EvalAsync(context);
            switch (Op)
            {
                case "+":
                    if (Retval.CurrentType == BCValue.ValueType.IsString)
                    {
                        Retval.AsString = Retval.AsString + CalculatedRight.AsString; // Concatenate
                    }
                    else
                    {
                        Retval.AsDouble += CalculatedRight.AsDouble;
                    }
                    break;
                case "-": Retval.AsDouble -= CalculatedRight.AsDouble; break;
                case "*": Retval.AsDouble *= CalculatedRight.AsDouble; break;
                case "/": Retval.AsDouble /= CalculatedRight.AsDouble; break;

                case "**": Retval.AsDouble = Math.Pow(Retval.AsDouble, CalculatedRight.AsDouble); break;
                case "√": Retval.AsDouble = Math.Pow(CalculatedRight.AsDouble, 1.0 / Retval.AsDouble); break;

                //case "<": Retval.AsDouble = (Retval.AsDouble < right.AsDouble) ? 1 : 0; break;
                case "<":
                    if (Retval.CurrentType == BCValue.ValueType.IsString)
                    {
                        var cmp = Retval.AsString.CompareTo(CalculatedRight.AsString);
                        Retval.AsDouble = (cmp == -1) ? 1 : 0;
                    }
                    else
                    {
                        Retval.AsDouble = (Retval.AsDouble < CalculatedRight.AsDouble) ? 1 : 0;
                    }
                    break;
                //case "<=": Retval.AsDouble = (Retval.AsDouble <= right.AsDouble) ? 1 : 0; break;
                case "<=":
                    if (Retval.CurrentType == BCValue.ValueType.IsString)
                    {
                        var cmp = Retval.AsString.CompareTo(CalculatedRight.AsString);
                        Retval.AsDouble = (cmp == -1 || cmp == 0) ? 1 : 0;
                    }
                    else
                    {
                        Retval.AsDouble = (Retval.AsDouble <= CalculatedRight.AsDouble) ? 1 : 0;
                    }
                    break;

                case "=":
                    if (Retval.CurrentType == BCValue.ValueType.IsString)
                    {
                        Retval.AsDouble = (Retval.AsString == CalculatedRight.AsString) ? 1 : 0;
                    }
                    else
                    {
                        Retval.AsDouble = (Retval.AsDouble == CalculatedRight.AsDouble) ? 1 : 0;
                    }
                    break;
                case "≇": // NEITHER APPROXIMATELY NOR ACTUALLY EQUAL TO
                case "≅": // Approximately equal to.  Uses about 5 digits of precision.
                    bool isApproximatelyEqualTo = false;
                    if (Retval.CurrentType == BCValue.ValueType.IsString)
                    {
                        isApproximatelyEqualTo = (String.Compare (Retval.AsString, CalculatedRight.AsString, StringComparison.CurrentCultureIgnoreCase) == 0);
                    }
                    else
                    {
                        // Start with making the problem simpler: a is always > b, and both are always positive. 
                        var a = Math.Abs (Retval.AsDouble);
                        var b = Math.Abs (CalculatedRight.AsDouble);
                        isApproximatelyEqualTo = BCBasicMathFunctions.ApproxEqual(a, b, 5.0);
#if NEVER_EVER_DEFINED
                        bool sameSign = (a <= 0 && b <= 0) || (a >= 0 && b >= 0);
                        if (a < b) 
                        {
                            var c = a;
                            a = b;
                            b = c;
                        }
                        var delta = a - b;
                        var ratio = delta / a; // always compare against the larger.  Ratio is small when the numbers are closer together.
                        // Note that ratio can never be < 0 because a is always >= b, so it's positive, and so is a.
                        var log = ratio > 0 ? Math.Log10(ratio) : Double.NegativeInfinity; // So if I want to compare against 5 sig. figs, just compare log against 5
                        isApproximatelyEqualTo = (sameSign && (log < -5.0));
#endif
                    }
                    if (Op == "≇") isApproximatelyEqualTo = !isApproximatelyEqualTo;
                    Retval.AsDouble = isApproximatelyEqualTo ? 1 : 0;
                    break;
                //case ">=": Retval.AsDouble = (Retval.AsDouble >= right.AsDouble) ? 1 : 0; break;
                case ">=":
                    if (Retval.CurrentType == BCValue.ValueType.IsString)
                    {
                        var cmp = Retval.AsString.CompareTo(CalculatedRight.AsString);
                        Retval.AsDouble = (cmp == 0 || cmp == 1) ? 1 : 0;
                    }
                    else
                    {
                        Retval.AsDouble = (Retval.AsDouble >= CalculatedRight.AsDouble) ? 1 : 0;
                    }
                    break;
                //case ">": Retval.AsDouble = (Retval.AsDouble > right.AsDouble) ? 1 : 0; break;
                case ">":
                    if (Retval.CurrentType == BCValue.ValueType.IsString)
                    {
                        var cmp = Retval.AsString.CompareTo(CalculatedRight.AsString);
                        Retval.AsDouble = (cmp == 1) ? 1 : 0;
                    }
                    else
                    {
                        Retval.AsDouble = (Retval.AsDouble > CalculatedRight.AsDouble) ? 1 : 0;
                    }
                    break;
                //case "<>": Retval.AsDouble = (Retval.AsDouble != right.AsDouble) ? 1 : 0; break;
                case "<>":
                    if (Retval.CurrentType == BCValue.ValueType.IsString)
                    {
                        var cmp = Retval.AsString.CompareTo(CalculatedRight.AsString);
                        Retval.AsDouble = (cmp == -1 || cmp == 1) ? 1 : 0;
                    }
                    else
                    {
                        Retval.AsDouble = (Retval.AsDouble != CalculatedRight.AsDouble) ? 1 : 0;
                    }
                    break;

                case "AND":
                    {
                        // AND is a C-style AND operator (not Sinclair style, which was just plain weird).
                        // It won't propagate NaN, but instead treats NaN like TRUE (like Python and C)
                        // AND will short-circuit
                        bool boolval = Retval.AsDoubleToBoolean;
                        if (boolval)
                        {
                            boolval = boolval && (await Right.EvalAsync(context)).AsDoubleToBoolean;
                        }
                        Retval.AsDouble = boolval ? 1.0 : 0.0;
                        //Retval.AsDouble = (right.AsDouble == 0.0) ? 0.0 : Retval.AsDouble; 
                    }
                    break;
                case "OR":
                    {
                        // AND is a C-style AND operator (not Sinclair style, which was just plain weird).
                        // It won't propagate NaN, but instead treats NaN like TRUE (like Python and C)
                        // AND will short-circuit
                        bool boolval = Retval.AsDoubleToBoolean;
                        if (!boolval)
                        {
                            boolval = boolval || (await Right.EvalAsync(context)).AsDoubleToBoolean;
                        }
                        Retval.AsDouble = boolval ? 1.0 : 0.0;
                        //Retval.AsDouble = (right.AsDouble == 0.0) ? 1.0 : Retval.AsDouble;
                    }
                    break;
                default:
                    context.WriteEvalError($"Unknown operator {Op}");
                    break;
            }
            return Retval;
        }
        public override string ToString() { return String.Format ("{0}{1}{2}",Left.ToString(), Op, Right.ToString()); }
    }

    // e.g., NOT A = B
    class NumericSingleExpression : IExpression
    {
        public NumericSingleExpression(string op, IExpression right)
        {
            Op = op;
            Right = right;
        }
        public string Op { get; set; }
        public IExpression Right { get; set; }

        public async Task<BCValue> EvalAsync(BCRunContext context)
        {
            if (context.AlwaysDelayEval) await Task.Delay(10); 
            var right = await Right.EvalAsync(context);
            var Retval = new BCValue (right);
            switch (Op)
            {
                case "√": Retval.AsDouble = Math.Sqrt (right.AsDouble); break;
                case "NOT": Retval.AsDouble = right.AsDoubleToBoolean ? 0 : 1;  break;
                default:
                    context.WriteEvalError($"Unknown operator {Op} for {Right.ToString()}");
                    break;
            }
            return Retval;
        }
        public override string ToString() { return String.Format("{0}{1}", Op, Right.ToString()); }
    }

    // NOTE: it says NumericInput, but is actually any user input.
    class NumericInput : IExpression
    {
        public NumericInput(IExpression prompt, IExpression defaultValue)
        {
            Prompt = prompt;
            DefaultValue = defaultValue;
        }
        public IExpression Prompt { get; set; }
        public IExpression DefaultValue { get; set; }
        public async Task<BCValue> EvalAsync(BCRunContext context)
        {
            var  defaultValue = DefaultValue != null ? await DefaultValue.EvalAsync(context) : new BCValue (Double.NaN);
            var prompt = Prompt != null ? await Prompt.EvalAsync(context) : new BCValue("Enter a value:");

            context.ProgramRunContext.Tracing.CurrCount.NInput.DoStart();
            string result = context.ExternalConnections.DoConsole != null ? await context.ExternalConnections.DoConsole.GetInputAsync(context.ct, prompt.AsString, defaultValue.AsString) : defaultValue.AsString;
            context.ProgramRunContext.Tracing.CurrCount.NInput.DoEnd();
            var mustBeNumber = (defaultValue.CurrentType == BCValue.ValueType.IsDouble);
            if (mustBeNumber)
            {
                double Retval = defaultValue.AsDouble;
                var isdouble = double.TryParse(result, out Retval);
                if (!isdouble)
                {
                    context.WriteEvalError($"{result} is not a number");
                }
                return new BCValue(Retval);
            }
            return new BCValue(result);
        }
        public override string ToString() { return String.Format("INPUT {0}", Prompt); }
    }

    class NumericFunction : IExpression
    {
        public NumericFunction (string function)
        {
            ArgList = new List<IExpression>();
            Function = function;
        }
        public IList<IExpression> ArgList { get; set; }
        public string Function { get; set; }

        private async Task<BCValue> Arg(BCRunContext context, int index)
        {
            if (index < 0 || index >= ArgList.Count) return new BCValue (Double.NaN);
            return await ArgList[index].EvalAsync(context);
        }
        public async Task<BCValue> EvalAsync(BCRunContext context)
        {
            BCValue Retval = new BCValue(Double.NaN);
            BCValue arg; //  await Arg.EvalAsync(context);
            switch (Function)
            {
                    // keep the sinclair functions; they seem to be reasonably standard.
                case "SGN": arg = await Arg(context, 0);  if (arg.AsDouble < 0) Retval.AsDouble = -1; else if (arg.AsDouble == 0) Retval.AsDouble = 0; else Retval.AsDouble = 1; break;
                case "ABS": arg = await Arg(context, 0); Retval.AsDouble = Math.Abs(arg.AsDouble); break;
                case "SIN": arg = await Arg(context, 0); Retval.AsDouble = Math.Sin(arg.AsDouble); break;
                case "COS": arg = await Arg(context, 0); Retval.AsDouble = Math.Cos(arg.AsDouble); break;
                case "TAN": arg = await Arg(context, 0); Retval.AsDouble = Math.Tan(arg.AsDouble); break;
                case "ASN": arg = await Arg(context, 0); Retval.AsDouble = Math.Asin(arg.AsDouble); break;
                case "ACS": arg = await Arg(context, 0); Retval.AsDouble = Math.Acos(arg.AsDouble); break;
                case "ATN": arg = await Arg(context, 0); Retval.AsDouble = Math.Atan(arg.AsDouble); break;
                case "LN": arg = await Arg(context, 0); Retval.AsDouble = Math.Log(arg.AsDouble); break;
                case "EXP": arg = await Arg(context, 0); Retval.AsDouble = Math.Exp(arg.AsDouble); break;
                case "SQR": arg = await Arg(context, 0); Retval.AsDouble = Math.Sqrt(arg.AsDouble); break;
                case "SQRT": arg = await Arg(context, 0); Retval.AsDouble = Math.Sqrt(arg.AsDouble); break; // SQRT is the same as SQR
                case "INT": arg = await Arg(context, 0); Retval.AsDouble = Math.Floor(arg.AsDouble); break; // INT (-3.4) is -4
                case "LEN": arg = await Arg(context, 0); Retval.AsDouble = arg.AsString.Length; break;
                case "STR$": arg = await Arg(context, 0); Retval.AsString = arg.AsString; break;
                // In the string function list now! case "CODE": arg = await Arg(context, 0); Retval.AsDouble = (double) ((arg.AsString + " ")[0]); break;
                // In the string function list now! case "CHR$": arg = await Arg(context, 0); Retval.AsString = new string(new char[] { (char)arg.AsDouble }); break;
                case "VAL": arg = await Arg(context, 0); Retval.AsDouble = (await EvalAsync (context, arg.AsString)).AsDouble; break;
                default:
                    // The functionList is all of the built-in functions like LEN
                    var status = await BCFunctionList.RunAsync(context, Function, ArgList, Retval);
                    if (status == BCFunctionList.RunStatus.NotFound)
                    {
                        var f = context.GetFunction(Function);
                        if (f != null) // context.Functions.ContainsKey(Function))
                        {
                            Retval = await f.EvalAsync(context, ArgList);
                        }
                        else
                        {
                            // Is the object a real object that I can run a function with?
                            bool foundFunction = false;
                            if (Function.Contains ("."))
                            {
                                var names = Function.Split(new char[] { '.' });
                                BCValue realParent;
                                var parent = context.GetObject(names[0], out realParent);
                                if (parent != null)
                                {
                                    for (int i=1; i<names.Length-2; i++)
                                    {
                                        parent = parent?.GetValue(names[i])?.AsObject;
                                    }
                                    if (parent != null)
                                    {
                                        var rstatus = await parent.RunAsync(context, names[names.Length - 1], ArgList, Retval);
                                        switch (rstatus)
                                        {
                                            case RunResult.RunStatus.OK: foundFunction = true; break;
                                            case RunResult.RunStatus.ErrorStop: foundFunction = true; break; // parent is responsible for showing the error.

                                        }
                                    }
                                }
                            }
                            else // Might be a(i) -- that is, an array reference.
                            {
                                BCValue variable = new BCValue();
                                var gotVariable = context.GetValue(Function, 0.0, ref variable);
                                if (gotVariable && variable.IsArray)
                                {
                                    var list = variable.AsArray;
                                    var rstatus = await list.RunAsync(context, "Get", ArgList, Retval);
                                    foundFunction = true;
                                }
                                
                            }
                            if (!foundFunction)
                            {
                                context.WriteEvalError($"Unable to find function {Function}");
                                Retval = new BCValue(Double.NaN);
                            }
                        }
                    }
                    break;
            }
            return Retval;
        }

        public async Task<BCValue> EvalAsync(BCRunContext context, string code)
        {
            var name = "RETVAL_EVAL_FUNCTION___45463____";
            code = name + "=" + code + "\r\n";
            var program = new BCBasicProgram();
            var pp = new Edit.BCBasicStatementAdapter();
            var statementList = pp.Parse(code); // This wraps the real compiler.
            foreach (var item in statementList)
            {
                program.AddStatement(item);
            }
            program.BCContext.ExternalConnections = context.ExternalConnections;

            var errors = program.GetErrorStatements();
            foreach (var error in errors)
            {
                context.WriteEvalError($"VAL error  {error.StatementError}");
                return new BCValue(Double.NaN);
            }
            if (errors.Count == 0)
            {
                try
                {
                    await BCBasicProgram.EvalInteractiveAsync(context, program.GetStatements());
                }
                catch (Exception ex)
                {
                    context.WriteEvalError($"VAL failed with {ex.Message}");
                    BCRunContext.AddError($"VAL failed with {ex.Message}");
                    return new BCValue(Double.NaN);
                }
            }

            var Retval = context.GetValue(name, Double.NaN);
            context.Remove(name);

            return Retval;
        }

        public override string ToString()
        {
            string arglist = "";
            foreach (var arg in ArgList)
            {
                var argstr = arg.ToString();
                if (arglist == "") arglist = argstr;
                else arglist = arglist + ", " + argstr;
            }
            return $"{Function}({arglist})";
        }
    }
}
