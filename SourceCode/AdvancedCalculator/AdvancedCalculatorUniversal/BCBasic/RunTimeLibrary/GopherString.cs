using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkParsers
{
    /// <summary>
    /// String extension methods designed for the Gopher protocol
    /// See RFC 
    /// </summary>
    public static class GopherString
    {
        /// <summary>
        /// Converts an arbitrary string into one that can be placed into a Gopher menu entry
        /// Gopher doesn't include any way to escape strings, so in practice this means that
        /// all invalid characters (NUL, TAB, CR, LF) are converted to ?
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        static char[] InvalidGopherChars = new char[] { '\0', '\r', '\n', '\t' };
        public static string EscapeGopher(this string s, char Replacement = '?')
        {
            var index = s.IndexOfAny(InvalidGopherChars);
            if (index < 0) return s; // All chars are valid!
            foreach (var ch in InvalidGopherChars)
            {
                s = s.Replace(ch, Replacement);
            }
            return s;
        }
    }
}
