using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BCBasic
{
    class BCBasicStringFunctions
    {
        public static async Task<bool> CHRAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            Retval.SetNoValue();
            StringBuilder str = new StringBuilder(); ;
            foreach (var arg in ArgList)
            {
                var val = (await arg.EvalAsync(context)).AsDouble;
                if (val < 0xFFFF)
                {
                    str.Append((char)val);
                }
                else // it likes face/food u+1f60b --> u+d83d u+de0b (surrogate pairs)
                {
                    /*
                        H = Math.floor((S - 0x10000) / 0x400) + 0xD800;
                        L = ((S - 0x10000) % 0x400) + 0xDC00;
                        return String.fromCharCode(H) + String.fromCharCode(L);
                     */
                    var h = Math.Floor((val - 0x10000) / 0x400) + 0xD800;
                    var l = ((val - 0x10000) % 0x400) + 0xDC00;
                    str.Append((char)h);
                    str.Append((char)l);
                }
            }
            Retval.AsString = str.ToString();
            return true;
        }

        public static async Task<bool> CODEAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            Retval.SetNoValue();
            if (ArgList.Count != 1)
            {
                context.ExternalConnections.WriteLine("Error: CODE() must be called with 1 arguments");
                return false;
            } 
            var str = (await ArgList[0].EvalAsync (context)).AsString;
            char value = (str.Length < 1) ? (char)0 : str[0];
            Retval.AsDouble = value;
            return true;
        }
        
        public static async Task<bool> LeftAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            Retval.SetNoValue();
            if (ArgList.Count != 2)
            {
                context.ExternalConnections.WriteLine("Error: LEFT() must be called with 2 arguments");
                return false;
            }
            var str = (await ArgList[0].EvalAsync(context)).AsString;
            var count = (await ArgList[1].EvalAsync(context)).AsInt;
            if (count < 0)
            {
                count = 0;
            }
            if (count > str.Length)
            {
                count = str.Length;
            }
            str = str.Substring(0, count);
            Retval.AsString = str;
            return true;
        }


        public static async Task<bool> MidAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            Retval.SetNoValue();
            if (ArgList.Count < 2 || ArgList.Count > 3)
            {
                context.ExternalConnections.WriteLine("Error: MID() must be called with 2 or 3 arguments");
                return false;
            }
            var str = (await ArgList[0].EvalAsync(context)).AsString;
            var startIdx = (await ArgList[1].EvalAsync(context)).AsInt;
            startIdx -= 1; // input is one-based; we need zero-based.

            if (startIdx < 0)
            {
                Retval.AsString = "";
            }
            else if (startIdx >= str.Length)
            {
                Retval.AsString = "";
            }
            else
            {
                var count = str.Length - startIdx;
                if (ArgList.Count > 2)
                {
                    count = (await ArgList[2].EvalAsync(context)).AsInt;
                    if (count < 0) count = 0;
                    int maxCount = str.Length - startIdx;
                    if (count > maxCount) count = maxCount;
                }
                str = str.Substring(startIdx, count);
                Retval.AsString = str;
            }
            return true;
        }

        // From the Tektronix POS (string, search, startat) function
        // Access is via String.Pos ()
        public static async Task<bool> POSAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            Retval.SetNoValue();
            if (ArgList.Count != 3)
            {
                context.ExternalConnections.WriteLine("Error: POS() must be called with 3 arguments");
                return false;
            }
            var str = (await ArgList[0].EvalAsync(context)).AsString;
            var searchfor = (await ArgList[1].EvalAsync(context)).AsString;
            var startAt = (await ArgList[2].EvalAsync(context)).AsInt;
            if (startAt < 0)
            {
                startAt = 1;
            }
            if (startAt > str.Length)
            {
                startAt = str.Length;
            }

            var rawValue = str.IndexOf(searchfor, startAt - 1);
            var value = rawValue < 0 ? 0 : rawValue + 1;
            
            Retval.AsDouble = value;
            return true;
        }

        // From the Tektronix POS (string, search, startat) function
        // The big differences is that this one isn't insane.
        // TEK: a$ = REP (b$, 4, 2)
        // BC BASIC: a$ = String.Replace (a$, LEFT (b$, 2) , 4)
        // Tektronix BASIC modifies the variable on the left hand side of REP (!)
        public static async Task<bool> ReplaceAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            Retval.SetNoValue();
            if (ArgList.Count != 3)
            {
                context.ExternalConnections.WriteLine("Error: Replace() must be called with 3 arguments");
                return false;
            }
            var str = (await ArgList[0].EvalAsync(context)).AsString;
            var replace = (await ArgList[1].EvalAsync(context)).AsString;
            var startAt = (await ArgList[2].EvalAsync(context)).AsInt;
            if (startAt <= 0)
            {
                startAt = 1;
            }
            if (startAt > str.Length)
            {
                startAt = str.Length+1;
            }
            var end = startAt -1 + replace.Length;
            string value = str.Substring(0, startAt-1) + replace;
            if (end < str.Length) value = value + str.Substring(end);

            Retval.AsString = value;
            return true;
        }

        public static async Task<bool> RightAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            Retval.SetNoValue();
            if (ArgList.Count != 2)
            {
                context.ExternalConnections.WriteLine("Error: RIGHT() must be called with 2 arguments");
                return false;
            }
            var str = (await ArgList[0].EvalAsync(context)).AsString;
            var count = (await ArgList[1].EvalAsync(context)).AsInt;
            if (count < 0)
            {
                count = 0;
            }
            if (count > str.Length)
            {
                count = str.Length;
            }
            var startIdx = str.Length - count;
            if (count == 0) str = "";
            else str = str.Substring(startIdx);
            Retval.AsString = str;
            return true;
        }
        public static async Task<bool> SPCAsync(BCRunContext context, IList<IExpression> ArgList, BCValue Retval)
        {
            Retval.SetNoValue();
            if (ArgList.Count != 1)
            {
                context.ExternalConnections.WriteLine("Error: SPC() must be called with 1 argument");
                return false;
            }
            StringBuilder str = new StringBuilder(); ;
            foreach (var arg in ArgList)
            {
                var val = (await arg.EvalAsync(context)).AsDouble;
                for (int i = 0; i < val; i++)
                    str.Append(" ");
            }
            Retval.AsString = str.ToString();
            return true;
        }
    }
}
