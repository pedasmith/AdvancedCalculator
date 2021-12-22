using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BCBasic
{
    public class RunResult
    {
        public RunResult()
        {
        }
        public RunResult(string str)
        {
            Result = new BCValue(str);
        }
        public enum RunStatus { OK, ErrorContinue, ErrorStop }; // ErrorStop means we can't keep trying.  ErrorContinue means we can.
        // ErrorContinue is used for when an object doesn't have the requested method
        // It's also used when a method is being called 'correctly' but the method can't run
        // For example, when calling g.UseScale ("meter") and there's no such scale.

        public RunStatus Status = RunStatus.OK;
        public BCValue Result = new BCValue("");
        public string Error = "";
        public string Message = "";
        public bool IsSilent = false; // set with STOP SILENT and suppresses the STOP dialog box.
        public string GetMessage(string defaultValue)
        {
            if (Message == null || Message == "") return defaultValue;
            return Message;
        }
        public override string ToString() { if (Status == RunStatus.OK) return Result.AsString; return Error; }
    }
}
