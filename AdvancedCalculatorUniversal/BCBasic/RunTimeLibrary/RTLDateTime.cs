using BCBasic;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AdvancedCalculator.BCBasic.RunTimeLibrary
{

    public class RTLDateTime : IObjectValue, IRTLDateTime
    {
        bool nowIsSet = false;
        DateTimeOffset now = DateTimeOffset.MinValue;
        private double AsMilliseconds (DateTimeOffset dt)
        {
#if WINDOWS8
            // NOTE: maybe return as Unix time?
            var ft = dt.ToFileTime(); // 100's of nanoseconds
            return (double)ft / 10000.0; 
#else
            return now.ToUnixTimeMilliseconds();
#endif
        }
        public double CurrentTotalSeconds
        {
            get
            {
                if (!nowIsSet) return double.NaN;
                var value = AsMilliseconds(now) / 1000.0;
                return value;
            }
        }
        public double GetCurrentTotalSeconds() { return CurrentTotalSeconds; } // For the IRTLDateTime class
        private void EnsureNowIsSet()
        {
            if (nowIsSet) return;
            now = DateTimeOffset.Now;
            nowIsSet = true;
        }

        public string PreferredName
        {
            get { return "DateTime"; }
        }


        public BCValue GetValue(string name)
        {
            bool needsNow = true;
            switch (name)
            {
                case "Methods":
                    needsNow = false;
                    break;
            }
            if (needsNow && !nowIsSet)
            {
                var Retval = new BCValue();
                Retval.SetError(1, "GetNow() was not called");
                return Retval;
            }
            switch (name)
            {
                case "AsTotalSeconds":
                    return new BCValue(AsMilliseconds(now) / 1000.0); // Why not CurrentTotalSeconds?
                case "Date":
                    // ISO 8601 aka RFC 3339
                    return new BCValue(now.ToString ("yyyy-MM-dd"));
                case "Day":
                    return new BCValue(now.Day);
                case "DayOfWeek":
                    return new BCValue((int)now.DayOfWeek);
                case "DayOfYear":
                    return new BCValue((int)now.DayOfYear);
                case "Hour":
                    return new BCValue(now.Hour);
                case "HourDecimal":
                    {
                        double h = now.Hour;
                        h += now.Minute / 60.0;
                        h += now.Second / (60.0 * 60.0);
                        h += now.Millisecond / (60.0 * 60.0 * 1000.0);
                        return new BCValue(h);
                    }
                case "Iso8601":
                    return new BCValue(now.ToString("o"));
                case "Minute":
                    return new BCValue(now.Minute);
                case "Month":
                    return new BCValue(now.Month);
                case "MonthName":
                    return new BCValue(now.ToString ("MMM"));
                case "Rfc1123":
                    return new BCValue(now.ToString("r"));
                case "Second":
                    return new BCValue(now.Second);
                case "Time":
                    return new BCValue(now.ToString("HH:mm:ss.ff"));
                case "TimeHHmm":
                    return new BCValue(now.ToString("HH:mm"));
                case "Year":
                    return new BCValue(now.Year);

                case "Methods":
                    return new BCValue("Now,Subtract,ToString");
            }
            return BCValue.MakeNoSuchProperty(this, name);
        }

        public void SetValue(string name, BCValue value)
        {
            ; // Can't ever set a constant.
        }

        public IList<string> GetNames()
        {
            return new List<string>() { "AsTotalSeconds", "Date", "Day", "DayOfWeek", "DayOfYear", "Hour", "HourDecimal", "Methods", "Minute", "Second", "Time", "TimeHHmm", "Year" };
        }

        public void InitializeForRun()
        {
        }

        public void RunComplete()
        {
        }

        public async Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            switch (name)
            {
                case "Add":
                    if (!BCObjectUtilities.CheckArgs(1, 7, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    for (int i=0; i<ArgList.Count; i++)
                    {
                        var amount = (await ArgList[i].EvalAsync(context)).AsDouble;
                        switch (i)
                        {
                            case 0: now = now.AddYears((int)amount); break;
                            case 1: now = now.AddMonths((int)amount); break;
                            case 2: now = now.AddDays(amount); break;
                            case 3: now = now.AddHours(amount); break;
                            case 4: now = now.AddMinutes(amount); break;
                            case 5: now = now.AddSeconds(amount); break;
                            case 6: now = now.AddMilliseconds(amount); break;
                        }
                    }
                    Retval.AsDouble = CurrentTotalSeconds;
                    return RunResult.RunStatus.OK;

                case "GetNow":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    Retval.AsObject = new RTLDateTime() { now = DateTimeOffset.Now, nowIsSet = true };
                    return RunResult.RunStatus.OK;

                case "GetNowUtc":
                    if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    Retval.AsObject = new RTLDateTime() { now = DateTimeOffset.UtcNow, nowIsSet = true };
                    return RunResult.RunStatus.OK;

                case "Parse":
                    if (!BCObjectUtilities.CheckArgs(1, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    var str = (await ArgList[0].EvalAsync(context)).AsString;
                    DateTimeOffset dto;
                    IFormatProvider format = System.Globalization.CultureInfo.InvariantCulture;
                    var status = DateTimeOffset.TryParse(str, format, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out dto);
                    if (status)
                        Retval.AsObject = new RTLDateTime() { now = dto, nowIsSet = true };
                    else
                        Retval.SetError(1, $"Unable to parse string {str}");
                    return RunResult.RunStatus.OK;

                case "Set":
                    if (!BCObjectUtilities.CheckArgs(1, 7, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    int[] values = new int[7] { 0, 0, 0, 0, 0, 0, 0};
                    for (int i = 0; i < ArgList.Count; i++)
                    {
                        values[i] = (int)(await ArgList[i].EvalAsync(context)).AsDouble;
                    }
                    now = new DateTimeOffset(values[0], values[1], values[2], values[3], values[4], values[5], values[6], new TimeSpan());
                    nowIsSet = true;
                    //Retval.AsObject = new RTLDateTime() { now = DateTimeOffset.Now, nowIsSet = true };
                    Retval.AsObject = new RTLDateTime() { now = this.now, nowIsSet = true };
                    return RunResult.RunStatus.OK;

                case "Subtract":
                    if (!BCObjectUtilities.CheckArgs(1, 1, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    var dt1 = await ArgList[0].EvalAsync(context);
                    if (dt1.AsObject == null)
                    {
                        Retval.SetError(1, "Argument to Subtract must be a DateTime");
                        return RunResult.RunStatus.ErrorStop;
                    }
                    var obj2 = dt1.AsObject;
                    var ms2 = obj2.GetValue("AsTotalSeconds");
                    if (Double.IsNaN(ms2.AsDouble))
                    {
                        Retval.SetError(2, "Argument to Subtract doens't have a valid AsTotalSeconds value");
                        return RunResult.RunStatus.ErrorContinue;
                    }
                    var ms = AsMilliseconds (now) / 1000.0;
                    var delta = ms - ms2.AsDouble;

                    Retval.AsDouble = delta; // return the number of seconds
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
