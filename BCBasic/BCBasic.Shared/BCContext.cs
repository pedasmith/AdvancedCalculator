using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
// TODO: need factory for AdvancedCalculator.BCBasic.RunTimeLibrary.RTLSystemX

namespace BCBasic
{
    public interface IDoStopProgram
    {
        void SetStopProgramVisibility(Windows.UI.Xaml.Visibility visibility);
        void SetStopProgramCancellationTokenSource(CancellationTokenSource tokenSource);
    }

    public interface IMmlNotePlayer
    {
        void SetCancellationToken(CancellationToken cts);
        void AddMmlString(string mmlString);
        bool IsPlaying();
        void SetFunction(BCRunContext context, string functionName);
    }
    public interface IGraphics
    {
        void SetAlignment();
        void Update();
    }



    public class BCGlobalConnections
    {
        public BCGlobalConnections()
        {
            Externals = new Dictionary<string, IObjectValue>();
            Package = null;
        }

        public void AddExternal(IObjectValue external)
        {
            // Can only add once.  Is important because some of the initialization can happen multiple times.
            if (!Externals.ContainsKey(external.PreferredName))
            {
                Externals.Add(external.PreferredName, external);
                var haveIsSet = external as IHaveFunctions;
                if (haveIsSet != null)
                {
                    // 
                }
            }
        }
        public Dictionary<string, IObjectValue> Externals; // All my externals; usually only the program scope has these.
        public BCPackage Package;

        public enum SpaceType { Newline, NoSpace, Tab };

        public IDoStopProgram DoStopProgram { get; set; } = null;
        public IConsole DoConsole { get; set; }
        public enum ClearType { Cls, Paper };
        public async Task ClsAsync(BCColor backgroundColor,  BCColor foregroundColor, ClearType clearType = ClearType.Cls)
        {
            if (DoConsole != null) await DoConsole.ClsAsync(backgroundColor, foregroundColor, clearType);
            // Do nothing; can't clear the screen.
        }
        public BCColor GetBackground()
        {
            if (DoConsole == null) return null;
            return DoConsole.GetBackground();
        }

        public BCColor GetForeground()
        {
            if (DoConsole == null) return null;
            return DoConsole.GetForeground();
        }

        public string Inkeys()
        {
            if (DoConsole != null) return DoConsole.Inkeys();
            return "";
        }

        public void PrintAt(PrintExpression.PrintSpaceType pst, string valueToPrint, int row, int col)
        {
            if (DoConsole != null) DoConsole.PrintAt(pst, valueToPrint, row, col);
        }

        public async Task RunOnAll(BCRunContext context, string function)
        {
            var Retval = new BCValue();
            foreach (var item in Externals)
            {
                await item.Value.RunAsync(context, function, new List<IExpression>(), Retval);
            }
        }

        public void WriteLine(string str)
        {
            if (DoConsole != null) DoConsole.Console(str);
            else System.Diagnostics.Debug.WriteLine(str);
        }

        public Windows.UI.Xaml.Controls.MediaElement CurrMediaElement { get; set; }
    }
    // All of the stuff that's per program and not per item.
    public class CallbackData
    {
        public CallbackData(BCRunContext context, string functionName, List<IExpression> arglist, Suppression allowSuppression = Suppression.AllowSuppression)
        {
            Context = context;
            FunctionName = functionName;
            ArgList = arglist;
            AllowSuppression = allowSuppression;
        }
        public async Task Call()
        {
            await CalculatorConnectionControl.BasicFrameworkElement.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                async () =>
                {
                    try
                    {
                        // Might be in a parent context -- e.g., when using SetInterval
                        // from a TEST function called by the test runner.  The parent
                        // context knows the function, but the primary context does not.
                        bool didCall = false;
                        BCRunContext context = Context;
                        while (!didCall && context != null)
                        {
                            if (context.Functions.ContainsKey(FunctionName))
                            {
                                didCall = true;
                                ReturnValue = await context.Functions[FunctionName].EvalAsync(Context, ArgList);
                                CurrStatus = Status.Complete;
                            }
                            else
                            {
                                context = context.Parent;
                            }
                        }
                        if (!didCall)
                        {
                            BCRunContext.AddError($"Callback {FunctionName} could not be found");
                            CurrStatus = Status.Failed;
                        }
                    }
                    catch (Exception ex)
                    {
                        BCRunContext.AddError($"Callback {FunctionName} failed with {ex.Message}");
                        CurrStatus = Status.Exception;
                    }
                }
            );
        }

        public string FunctionName { get; set; }
        public List<IExpression> ArgList { get; set; }
        public BCRunContext Context { get; set; }
        public enum Suppression {  AllowSuppression, NoSuppress };
        public Suppression AllowSuppression { get; set; }
        public BCValue ReturnValue { get; internal set; } = null;
        public enum Status {  NotRun, Suppressed, Failed, Exception, Complete }
        public Status CurrStatus { get; internal set; } = Status.NotRun;
        // As of 2019-03-25, only one status isn't complete; the rest are all final states.
        public bool CurrStatusIsComplete
        {
            get { return CurrStatus != Status.NotRun; }
        }
    }
    public class BCProgramRunContext
    {
        public TestRunner CurrTestRunner { get; set; } = null;
        public AdvancedCalculator.BCBasic.RunTimeLibrary.RTLSystemX Tracing = new AdvancedCalculator.BCBasic.RunTimeLibrary.RTLSystemX();
        private IList<CallbackData> PendingCallbacks = new List<CallbackData>();
        public PrintExpression.PrintSpaceType CurrPrintSpaceType { get; set; } = PrintExpression.PrintSpaceType.Newline;
        public int NPendingCallbacks {  get { return PendingCallbacks.Count; } }

        //
        // ALL CALLBACK STUFF
        //

        // ArgList is, e.g., var arglist = new List<IExpression>() { new NumericConstant(value) };
        // context.ProgramRunContext.AddCallback (contextm, functionName, arglist);
        public CallbackData AddCallback(BCRunContext context, String functionName, List<IExpression> arglist, CallbackData.Suppression suppression = CallbackData.Suppression.AllowSuppression)
        {
            var callbackData = new CallbackData(context, functionName, arglist, suppression);
            AddCallback(callbackData);
            return callbackData;
        }

        public void AddCallback(CallbackData data)
        {
            lock (this)
            {
                PendingCallbacks.Add(data);
            }
        }
        public bool HaveCallbackToCall()
        {
            return PendingCallbacks.Count > 0;
        }
        public async Task<int> CallAllCallbacksAsync()
        {
            int Retval = 0;
            if (PendingCallbacks.Count == 0) return 0;
            List<CallbackData> toBeCalled = new List<CallbackData>();
            lock (this)
            {
                HashSet<string> FoundFunctions = new HashSet<string>();
                foreach (var item in PendingCallbacks)
                {
                    // Each function can only be called once (unless it does not allow suppression)
                    if (item.AllowSuppression == CallbackData.Suppression.NoSuppress)
                    {
                        ;
                    }
                    else if (FoundFunctions.Contains (item.FunctionName))
                    {
                        this.Tracing.CurrCount.NCallbackSuppressed++;
                        foreach (var potentiallySuppressed in toBeCalled)
                        {
                            if (potentiallySuppressed.FunctionName == item.FunctionName)
                            {
                                // Tell the originator that it's been suppressed
                                potentiallySuppressed.CurrStatus = CallbackData.Status.Suppressed;
                            }
                        }
                        toBeCalled.RemoveAll(value => value.FunctionName == item.FunctionName); // will just be one :-)
                    }
                    else
                    {
                        FoundFunctions.Add(item.FunctionName);
                    }
                    toBeCalled.Add(item);
                }
                PendingCallbacks.Clear();
            }
            foreach (var item in toBeCalled)
            {
                this.Tracing.CurrCount.NCallback.DoStart();
                await item.Call();
                this.Tracing.CurrCount.NCallback.DoEnd();
                Retval++;
            }
            return Retval;
        }

        //
        // DATA and READ Statements
        //

        int currDataStatement = 0;
        int currDataItem = -1; // Which data item in the list to look at?

        List<Data> AllDataStatements = new List<Data>();
        public void ClearDataStatements()
        {
            AllDataStatements.Clear();
            currDataStatement = 0;
            currDataItem = -1;
        }
        public void ResetReadIndex()
        {
            currDataStatement = 0;
            currDataItem = -1;
        }
        public void AddDataStatement (Data statement)
        {
            AllDataStatements.Add(statement);
        }

        public IExpression ReadData()
        {
            // Note: fails if there isn't anything at all.
            if (DataIsEmpty()) return new NumericConstant(Double.NaN); // Not a number or anything else!
            var Retval = ReadNextData();
            while (Retval == null) Retval = ReadNextData(); // Bound to be at least one!
            return Retval;
        }
        private bool DataIsEmpty()
        {
            if (AllDataStatements == null) return true; // nothing
            if (AllDataStatements.Count == 0) return true; // still nothing
            foreach (var data in AllDataStatements)
            {
                foreach (var item in data.DataList)
                {
                    if (item != null) return false; // At least one statement has at least one value
                }
            }
            return true; // nope, nothing.
        }
        private IExpression ReadNextData()
        {
            var list = AllDataStatements[currDataStatement].DataList;
            currDataItem += 1;
            if (currDataItem >= list.Count)
            {
                currDataStatement += 1;
                currDataItem = -1;
                if (currDataStatement >= AllDataStatements.Count)
                {
                    currDataStatement = 0;
                }
                return null;
            }
            var Retval = list[currDataItem];
            return Retval;
        }


        //
        //
        //
        public Task IncrementStatementTracingCount(IStatement currStatement)
        {
            return IncrementStatementTracingCount(new FullStatement("000", currStatement));
        }

        public async Task IncrementStatementTracingCount(FullStatement currStatement)
        {
            Tracing.CurrStatement = currStatement;
            Tracing.PossiblyTrace(true); // is another statement
            Tracing.CurrCount.NStatement++;

            var nstatement = Tracing.CurrCount.NStatement;
            // Surface Pr 4: 
            // MOD value of 100K results in very bumpy output @ 596100 statements/second
            // MOD value of 10K results in very bumpy output @ 344000 statements/second
            if (nstatement % 10000 == 1)
            {
                await Task.Delay(1); // Every so often, stop and let the console catch up.
            }
        }

    }
    public class BCRunContext
    {
        public BCRunContext(BCGlobalConnections externals)
        {
            ProgramRunContext = new BCProgramRunContext();
            ExternalConnections = externals;
            CurrContextType = ContextType.Program;
            Values = new Dictionary<string, BCValue>();
            LineNumbers = new Dictionary<string, int>();
            NextToFor = new Dictionary<int, int>();
            PCStack = new Stack<int>();
        }

        public void ClearValues()
        {
            Values.Clear();
            foreach (var ext in ExternalConnections.Externals)
            {
                ext.Value.InitializeForRun();
            }
        }
        public void Init(bool keepVariables = false)
        {
            ReturnValue = new BCValue(); // set to not-set
            if (keepVariables == false)
            {
                ClearValues();
            }
            PCStack.Clear();
            PC = 0;
            PCIsChanged = false;
        }

        public void Finish()
        {
            try
            {
                foreach (var ext in ExternalConnections.Externals)
                {
                    ext.Value.RunComplete();
                }
            }
            catch (Exception ex)
            {
                //; Don't care what happened; keep going
                BCRunContext.AddError($"Exception while finishing is {ex.Message}");
            }
        }
        //
        // These make up the context in which a function (or program) runs.
        //
        private Dictionary<string, BCValue> Values; // All my variables (e.g., A = 10)
        public List<String> Globals = new List<string>(); // All of the variables that are really globals, not local variables
        public Dictionary<string, Function> Functions = new Dictionary<string, Function>(); // All the functions defined in this scope; usually only a Program has these.

        public Stack<int> PCStack; // Current executing line
        public Dictionary<string, int> LineNumbers; // All of the "line numbers" (e.g., 10 A$="My line"); are treated internally as opaque strings
        public Dictionary<int, int> NextToFor;


        public void CopyParentData(BCRunContext context)
        {
            this.ProgramRunContext = context.ProgramRunContext;
            this.ExternalConnections = context.ExternalConnections;
            this.Parent = context;
        }
        public BCProgramRunContext ProgramRunContext; // contains info that's per program run, not per context.
        public BCRunContext Parent; // My parent context; used to look up functions and externals
        public BCGlobalConnections ExternalConnections = new BCGlobalConnections();


        public static Random StaticRnd = new Random();
        public enum ContextType { Program, Function };


        public ContextType CurrContextType { get; set; }
        public BCValue ReturnValue { get; set; }
        public BCValue LastAssignment { get; set; }
        public bool IsSilent { get; set; } = false;
        public int PC;
        public int PCDelta; // How much did PC change? Is almost always 1
        public bool PCIsChanged;
        private CancellationToken? _ct =null;
        public CancellationToken ct
        {
            get
            {
                if (_ct.HasValue) return _ct.Value;
                if (Parent != null) return Parent.ct;
                return _ct.GetValueOrDefault();
            }
            set
            {
                _ct = value;
            }
        }
        public bool IsCancellationRequested
        {
            get 
            {
                if (ct != null) return ct.IsCancellationRequested;
                return false;
            }
        }


        public int PCError(int PC) // Says what to do on error; we just keep going.
        {
            return PC + 1;
        }

        public IEnumerable<string> ListVariables() // Return all our variables
        {
            return Values.Keys;
        }


        // The old call; is still used.
        public BCValue GetValue(string name, double defaultValue)
        {
            BCValue Retval = null;
            var status = GetValue(name, defaultValue, ref Retval);
            return Retval;
        }

        public bool GetValue(string name, double defaultValue, ref BCValue Retval)
        {
            bool isPotentialGlobal = Globals.Contains(name);
            return GetValue(name, isPotentialGlobal, defaultValue, ref Retval);
        }

        char[] dot = new char[] { '.' };
        private bool GetValue(string name, bool isPotentialGlobal, double defaultValue, ref BCValue Retval)
        {
            if (name.Contains(".")) // Is an external
            {
                var list = name.Split(dot, 2);
                var external = list[0];
                var field = list[1];

                // device.Name or list.Count where device and list
                // are variables of type IsObject are not handled here; they
                // are handled by the caller.
                if (ExternalConnections.Externals.ContainsKey(external))
                {
                    Retval = ExternalConnections.Externals[external].GetValue(field);
                    return true;
                }
                else if (Parent != null)
                {
                    var status = Parent.GetValue(name, defaultValue, ref Retval); // Common case for, e.g., CONSTANTS.E
                    return status;
                }
            }
            else if (isPotentialGlobal && Parent != null)
            {
                // e.g.
                // FUNCTION PrintMessage()
                //     GLOBAL message
                //     PRINT message
                // END
                // 
                // The function will use the variable from the Parent context, not the local one.
                var status = Parent.GetValue(name, isPotentialGlobal, defaultValue, ref Retval); 
                return status;
            }
            else if (Values.ContainsKey(name))
            {
                Retval = Values[name];
                return true;
            }

            Retval = new BCValue(defaultValue);
            return false;
        }




        public IObjectValue GetObject(string name, out BCValue realParent)
        {
            bool potentialGlobal = Globals.Contains(name);
            return GetObject(name, potentialGlobal, out realParent);
        }

        //
        // Note that the globals table used for lookup must always come from the original context.
        // e.g., 
        // value = 10
        // FUNCTION F1()
        //     F2()
        // END
        // FUNCTION F2()
        //    GLOBAL value
        //    value = 10
        // END
        // 
        // We decide up front in F2 that value is a global.  When we look for value 
        // in F1, we don't check to see if F1 has declared value to be a global; we
        // don't check in that function's context for the list of globals.
        private IObjectValue GetObject (string name, bool potentialGlobal, out BCValue realParent)
        {
            if (Values.ContainsKey(name))
            {
                var obj = Values[name];
                if (obj.AsObject != null)
                {
                    realParent = obj;
                    return obj.AsObject;
                }
                else
                {
                    realParent = null;
                    return obj;
                }
            }
            else if (potentialGlobal && Parent != null)
            {
                // e.g.
                // FUNCTION PrintMessage()
                //     GLOBAL message
                //     PRINT message
                // END
                // 
                // The function will use the variable from the Parent context, not the local one.
                realParent = null;
                var obj = Parent.GetObject(name, potentialGlobal, out realParent);
                return obj;
            }
            if (ExternalConnections.Externals.ContainsKey(name))
            {
                realParent = null;
                return ExternalConnections.Externals[name];
            }
            realParent = null;
            return null;
        }

        // Gets an array value.  In the future, we will support lists of indexes and BCValues that store arrays.
        public BCValue GetValue(string name, string index1, string index2, double defaultValue)
        {
            bool potentialGlobal = Globals.Contains(name);
            return GetValue(name, index1, index2, potentialGlobal, defaultValue);
        }

        private BCValue GetValue(string name, string index1, string index2, bool potentialGlobal, double defaultValue)
        {
            // TODAY: no BCValues are arrays
            // a = apple[45] --> NaN
            if (Values.ContainsKey(name))
            {
                //NOTE: add the e.g. "SET"
                var thisvalue = Values[name];
                if (thisvalue.CurrentType == BCValue.ValueType.IsObject)
                {
                    var obj = thisvalue.AsObject;
                    var Retval = new BCValue();
                    var arglist = String.IsNullOrEmpty(index2)
                        ? new List<IExpression>() { new NumericConstant(index1) }
                        : new List<IExpression>() { new NumericConstant(index1), new NumericConstant(index2) }
                        ;
                    obj.RunAsync(this, "Get", arglist, Retval);
                    return Retval;
                }
                else if (thisvalue.CurrentType == BCValue.ValueType.IsString) // JULY 2018: a="abc" : b=a[1] works now!
                {
                    var str = thisvalue.AsString;
                    double indexBase0 = 0.0;
                    var isNumber = double.TryParse(index1, out indexBase0); // is no index2 for strings
                    if (!isNumber) return new BCValue(defaultValue);
                    indexBase0 -= 1; // convert from 1-based array to 0-based
                    int index0int = (int)indexBase0;
                    if (index0int < 0 || index0int >= str.Length) return new BCValue(defaultValue);
                    char ch = str[index0int];
                    return new BCValue(new string(ch, 1));
                }
                return new BCValue(defaultValue);
                //return Values[name];
            }
            if (potentialGlobal && Parent != null)
            {
                // e.g.
                // FUNCTION PrintMessage()
                //     GLOBAL floor
                //     item = floor(1)
                //     PRINT item
                // END
                // 
                // The function will use the variable from the Parent context, not the local one.
                BCValue thisvalue = null;
                var found = Parent.GetValue (name, potentialGlobal, defaultValue, ref thisvalue);
                if (found && thisvalue.CurrentType == BCValue.ValueType.IsObject)
                {
                    var obj = thisvalue.AsObject;
                    var Retval = new BCValue();
                    obj.RunAsync(this, "Get", new List<IExpression>() { new NumericConstant(index1), new NumericConstant(index2) }, Retval);
                    return Retval;
                }
            }
            if (ExternalConnections.Externals.ContainsKey(name))
            {
                return ExternalConnections.Externals[name].GetValue(index1); // Never uses index2
            }
            else if (Parent != null)
            {
                var thisvalue = Parent.GetValue(name, defaultValue); // Common case for, e.g., CONSTANTS.E
                if (thisvalue.CurrentType == BCValue.ValueType.IsObject)
                {
                    var obj = thisvalue.AsObject;
                    var Retval = new BCValue();
                    obj.RunAsync(this, "Get", new List<IExpression>() { new NumericConstant(index1), new NumericConstant(index2) }, Retval);
                    return Retval;
                }
                return new BCValue(defaultValue);
            }

            return new BCValue(defaultValue);
        }

        public IFunction GetFunction(string name)
        {
            return GetFunction(this, name);
        }

        public static IFunction GetFunction(BCRunContext context, string name)
        {
            while (context != null)
            {
                IFunction f = context.Functions.ContainsKey(name) ? context.Functions[name] : null;
                if (f != null) return f;

                // See if any of the Externals are also IHaveIsSet
                foreach (var item in context.ExternalConnections.Externals)
                {
                    var externalName = item.Key;
                    var haveIsSet = item.Value as IHaveFunctions;
                    if (haveIsSet != null)
                    {
                        var list = haveIsSet.GetFunctions();
                        foreach (var functionRecord in list)
                        {
                            var fullname = externalName + "." + functionRecord.Key;
                            if (name == fullname)
                            {
                                f = functionRecord.Value;
                                if (f != null) return f;
                            }
                        }
                    }
                }
                context = context.Parent;
            }
            return null;
        }

        public void Set(string name, double value)
        {
            var isPotentialGlobal = Globals.Contains(name);
            Set(name, isPotentialGlobal, value);
        }

        private void Set(string name, bool isPotentialGlobal, double value)
        {
            if (name.Contains(".")) // Is an external
            {
                var list = name.Split(dot, 2);
                var external = list[0];
                var field = list[1];
                if (ExternalConnections.Externals.ContainsKey(external))
                {
                    ExternalConnections.Externals[external].SetValue(field, new BCValue(value));
                }
            }
            else if (!Values.ContainsKey(name))
            {
                if (isPotentialGlobal && Parent != null)
                {
                    // e.g.
                    // FUNCTION PrintMessage()
                    //     GLOBAL message
                    //     PRINT message
                    // END
                    // 
                    // The function will use the variable from the Parent context, not the local one.
                    Parent.Set(name, isPotentialGlobal, value);
                }
                else
                {
                    Values[name] = new BCValue(value);
                }
            }
            else
            {
                Values[name].AsDouble = value;
            }
        }

        public void Set(string name, BCValue value)
        {

            bool potentialGlobal = Globals.Contains(name) && Parent != null;
            if (!potentialGlobal && name.Contains ("."))
            {
                var fields = name.Split(dot, 2);
                var external = fields[0];
                potentialGlobal = Globals.Contains(external) && Parent != null;
            }
            Set(name, potentialGlobal, value);
        }

        private void Set(string name, bool isPotentialGlobal, BCValue value)
        {
            if (name.Contains(".")) // Is an external or global like g.Background = "#88EE33"
            {
                var list = name.Split(dot, 2);
                var external = list[0];
                var field = list[1];
                if (ExternalConnections.Externals.ContainsKey(external))
                {
                    ExternalConnections.Externals[external].SetValue(field, value);
                }
                else
                {
                    // DIM data()
                    // data.MaxCount = 100
                    BCValue realParent;
                    var obj = GetObject(external, out realParent);

                    if (isPotentialGlobal && !Values.ContainsKey(name) && Parent != null)
                    {
                        // e.g.
                        // FUNCTION SetColor(color)
                        //     GLOBAL g
                        //     g.Background = color
                        // END
                        // 
                        // The function will use the variable from the Parent context, not the local one.
                        Parent.Set(name, isPotentialGlobal, value);
                    }
                    else if (obj != null)
                    {
                        obj.SetValue(field, value);
                    }                    // is External a variable?
                }
            }
            else
            {
                if (isPotentialGlobal && !Values.ContainsKey(name) && Parent != null)
                {
                    // e.g.
                    // FUNCTION PrintMessage()
                    //     GLOBAL message
                    //     PRINT message
                    // END
                    // 
                    // The function will use the variable from the Parent context, not the local one.
                    Parent.Set(name, isPotentialGlobal, value);
                }
                else
                {
                    Values[name] = value;
                }
            }
        }

        // HISTORY: doesn't handle GLOBAL data where data is a DIM'd array! --> Later comment: Huh? Isn't this fixed now?
        public void Set(string name, string index1, string index2, BCValue value)
        {
            bool potentialGlobal = Globals.Contains(name) && Parent != null;
            Set(name, index1, index2, potentialGlobal, value);
        }

        private void Set(string name, string index1, string index2, bool isPotentialGlobal, BCValue value)
        {
            if (ExternalConnections.Externals.ContainsKey(name))
            {
                ExternalConnections.Externals[name].SetValue(index1, value);
            }
            else
            {
                if (isPotentialGlobal && !Values.ContainsKey(name) && Parent != null)
                {
                    Parent.Set(name, index1, index2, isPotentialGlobal, value);
                }
                else if (Values.ContainsKey(name))
                {
                    var thisvalue = Values[name];
                    if (thisvalue.CurrentType == BCValue.ValueType.IsObject)
                    {
                        var obj = thisvalue.AsObject;
                        var DummyRetval = new BCValue();
                        obj.RunAsync(this, "Set", new List<IExpression>() { new NumericConstant(index1), index2==null ? null : new NumericConstant(index2), new ObjectConstant(value) }, DummyRetval). Wait();
                    }
                    else
                    {
                        // NOTE: is this the right error handling?
                        var idx2 = index2 == null ? "" : $",{index2}";
                        var err = $"ERROR: Can't set {name}[{index1}{idx2}] because it's not an object or array";
                        this.ExternalConnections.WriteLine(err);
                        BCRunContext.AddError(err);
                    }
                }
                else
                {
                    var idx2 = index2==null ? "" : $",{index2}";
                    var err = $"Can't set {name}[{index1}{idx2}] because it's not a variable";
                    this.ExternalConnections.WriteLine(err);
                    BCRunContext.AddError(err);
                }
            }
        }


        public void Remove(string name)
        {
            if (Values.ContainsKey(name))
            {
                Values.Remove(name);
            }
        }

        // Call this when an object cannot be correctly created
        public void WriteCreateError(string str)
        {
            str = "Create Error: " + str;
            if (ExternalConnections != null) ExternalConnections.WriteLine(str);
            else System.Diagnostics.Debug.WriteLine(str);
        }

        // Call this when an evaluation fails
        public void WriteEvalError(string str)
        {
            str = "Warning: " + str;
            if (ExternalConnections != null) ExternalConnections.WriteLine(str);
            else System.Diagnostics.Debug.WriteLine(str);
        }

        // Called in a somewhat random way that depends on the RunTimeLibrary :-(
        public static void AddError(string str)
        {
            AdvancedCalculator.BCBasic.RunTimeLibrary.RTLSystemX.AddError(str);
        }

        //
        // The I/O part of the Context
        //
        public bool AlwaysDelayEval { get { return false; } } // must always be false!
    }
}
