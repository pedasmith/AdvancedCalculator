using BCBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace AdvancedCalculator.BCBasic.RunTimeLibrary
{
    public class TraceData
    {
        public void Clear()
        {
            Start = 0;
            End = 0;
        }
        public void CopyTo (TraceData dest)
        {
            dest.Start = Start;
            dest.End = End;
        }
        public void DoStart()
        {
            Start++;
        }
        public void DoEnd()
        {
            End++;
        }
        public int Delta {  get { return Start - End; } }
        public int Start { get; internal set; } = 0;
        public int End { get; internal set; }
    }
    public class TraceCounts
    {
        public int NStatement { get; set; } = 0;
        public int NException { get; set; } = 0;

        public TraceData NInput { get; } = new TraceData();
        public TraceData NPause { get; } = new TraceData();
        public TraceData NForever { get; } = new TraceData();
        public TraceData NPlayWait { get; } = new TraceData();
        public TraceData NCallback { get; } = new TraceData();
        public int NCallbackSuppressed { get; set; } = 0;
        public void Clear()
        {
            NStatement = 0;
            NException = 0;
            NCallbackSuppressed = 0;
            NInput.Clear();
            NPause.Clear();
            NForever.Clear();
            NPlayWait.Clear();
            NCallback.Clear();
        }
        public void CopyTo (TraceCounts dest)
        {
            dest.NStatement = NStatement;
            dest.NException = NException;
            dest.NCallbackSuppressed = NCallbackSuppressed;
            NInput.CopyTo(dest.NInput);
            NPause.CopyTo(dest.NPause);
            NForever.CopyTo(dest.NForever);
            NPlayWait.CopyTo(dest.NPlayWait);
            NCallback.CopyTo(dest.NCallback);
        }
    }

    public class RTLSystemX : IObjectValue
    {
        const int MaxErrors = 5;
        static List<string> Errors = new List<string>();
        public static void AddError(string str)
        {
            while (Errors.Count >= MaxErrors)
            {
                Errors.RemoveAt(0);
            }
            Errors.Add(str);
        }


        //
        // All of the tracing stuff
        //

        public enum TraceLevel {  None, Standard };
        public void ClearTraceCounts ()
        {
            CurrCount.Clear();
            CountsAtLastTrace.Clear();
        }
        public TraceCounts CurrCount = new TraceCounts();
        private TraceCounts CountsAtLastTrace = new TraceCounts();

        // TraceLevel persists between runs.
        static public TraceLevel CurrTraceLevel { get; set; } = RTLSystemX.TraceLevel.Standard;

        public DateTime LastPrintedTrace { get; internal set; } = DateTime.MinValue;
        private double SecondsBetweenTraces = 1.0;
        private int NTracesAtSecondBetweenTraces = 0; // e.g., we print ever second for 5 seconds, ever 5 seconds for 2 minutes, etc.

        public void TraceComplete()
        {
            if (CurrTraceLevel > TraceLevel.None)
            {
                PrintTrace();
            }
        }

        // How many times to trace at differnt speeds.
        List<Tuple<double, int>> TracesPerSecondList = new List<Tuple<double, int>>()
        {
            new Tuple<double, int>(1.0, 5), // every second 5 times
            new Tuple<double, int>(5.0, 11), // very 5 seconds 11 times (total is now 1 minute)
            new Tuple<double, int>(60.0, 59), // every minutes up to an hour
            new Tuple<double, int>(30000.0, int.MaxValue), // every 5 minutes thereafter
        };
        private void UpdateSecondsBetweenTraces()
        {
            NTracesAtSecondBetweenTraces++;
            var idx = TracesPerSecondList.FindIndex((item) => { return item.Item1 == SecondsBetweenTraces; });
            if (idx < 0)
            {
                NTracesAtSecondBetweenTraces = 0;
                SecondsBetweenTraces = TracesPerSecondList[0].Item1;
                return;
            }
            var record = TracesPerSecondList[idx];
            if (NTracesAtSecondBetweenTraces >= record.Item2)
            {
                // Go to the next one.
                NTracesAtSecondBetweenTraces = 0;
                SecondsBetweenTraces = TracesPerSecondList[idx + 1].Item1;
            }
        }
        public FullStatement CurrStatement = null;
        public void PossiblyTrace(bool isAnotherStatement = true)
        {
            var now = DateTime.UtcNow;
            if (LastPrintedTrace == DateTime.MinValue) LastPrintedTrace = now;

            var nseconds = now.Subtract(LastPrintedTrace).TotalSeconds;
            if (nseconds >= SecondsBetweenTraces)
            {
                UpdateSecondsBetweenTraces();
                if (CurrTraceLevel > TraceLevel.None)
                {
                    PrintTrace();
                }
            }
        }
        public BCRunContext Context { get; set; } = null; // just for the WriteLine
        private void PrintTrace()
        {
            var now = DateTime.UtcNow;
            var delta = now.Subtract(LastPrintedTrace).TotalSeconds;
            if (delta == 0.0) delta = 0.0001; // avoid divide-by-zero errors....

            var deltaStatements = CurrCount.NStatement - CountsAtLastTrace.NStatement;
            var sps = (deltaStatements)  / delta;
            var str = $"{deltaStatements} statements @ {sps:F2} statements/second ";
            if (CurrCount.NException > 0) str += $"{CurrCount.NException} exceptions ";
            if (CurrStatement != null) str += $"Current statement is {CurrStatement.ToString()} ";
            if (CurrCount.NCallback.Start > 0)
            {
                str += $"N. Callbacks {CurrCount.NCallback.Start} ";
                if (CurrCount.NCallbackSuppressed > 0)
                {
                    str += $"({CurrCount.NCallbackSuppressed} suppressed) ";
                }
            }

            LastPrintedTrace = now;
            CurrCount.CopyTo(CountsAtLastTrace);

            Context.ExternalConnections.WriteLine(str);
        }


        public string PreferredName
        {
            get { return "System"; }
        }

        public BCValue GetValue(string name)
        {
            switch (name)
            {
                case "Errors":
                    var sb = new StringBuilder();
                    foreach (var item in Errors)
                    {
                        sb.Append(item);
                        sb.Append("\n");
                    }
                    var str = sb.ToString();
                    if (str == "") str = "No errors";
                    return new BCValue(str);
                case "FolderBasic":
                    {
                        Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.RoamingFolder;
                        return new BCValue(localFolder.Path);
                    }
                case "FolderBasicQuota":
                    {
                        var quota = Windows.Storage.ApplicationData.Current.RoamingStorageQuota;
                        return new BCValue(quota);
                    }
                case "FolderTemporary":
                    {
                        Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.TemporaryFolder;
                        return new BCValue(localFolder.Path);
                    }
                case "Methods":
                    return new BCValue("SetInterval,Trace,ToString"); 
                case "Version":
                    {
                        var version = Package.Current.DisplayName;
                        var V = Package.Current.Id.Version;
                        version += $" {V.Major}.{V.Minor} ";
                        version += $"({Package.Current.Id.Architecture}) ";
                        return new BCValue(version);
                    }
            }
            return BCValue.MakeNoSuchProperty(this, name);
        }

        public void SetValue(string name, BCValue value)
        {
            ; // Can't ever set a constant.
        }

        public IList<string> GetNames()
        {
            return new List<string>() { "Errors,Methods,Version" };
        }

        public void InitializeForRun()
        {
        }


        // Totally not needed for the litle class...
        List<IObjectValue> AllCreatedObjects = new List<IObjectValue>();
        public void RunComplete()
        {
            foreach (var item in AllCreatedObjects)
            {
                item.RunComplete();
            }
        }

        CancellationTokenSource TimerCts = new CancellationTokenSource();
        List<Task> AllTimerFunctions = new List<Task>();

        public async Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            switch (name)
            {
                case "SetInterval":
                    // SetInterval ("mycallback", 500, "ABC") calls mycallback("ABC")
                    if (!BCObjectUtilities.CheckArgs(3, 3, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(1, "milliseconds", 20, 5000, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;


                    var functionName = (await ArgList[0].EvalAsync(context)).AsString;
                    var milliseconds = (await ArgList[1].EvalAsync(context)).AsDouble;
                    var arg = (await ArgList[2].EvalAsync(context)).Duplicate();
                    var token = TimerCts.Token;
                    Task t = Task.Run(async () =>
                       {
                           while (!token.IsCancellationRequested)
                           {
                               await Task.Delay((int)milliseconds, token);
                               context.ProgramRunContext.AddCallback(new CallbackData(context, functionName, new List<IExpression>() { new ObjectConstant(arg) }));
                           }
                       }
                    );
                    AllTimerFunctions.Add(t);
                    return RunResult.RunStatus.OK;
                case "Trace":
                    if (!BCObjectUtilities.CheckArgs(1, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (!await BCObjectUtilities.CheckArgValue(0, "traceLevel", 0, 1, context, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;

                    var value = (await ArgList[0].EvalAsync(context)).AsInt;
                    CurrTraceLevel = (TraceLevel)value;
                    return RunResult.RunStatus.OK;
                case "ToString":
                    Retval.AsString = $"Object {PreferredName}";
                    return RunResult.RunStatus.OK;
                default:
                    await Task.Delay(0);
                    BCValue.MakeNoSuchMethod(Retval, this, name);
                    return RunResult.RunStatus.ErrorContinue;
            }
        }


        public void Dispose()
        {
        }
    }
}
