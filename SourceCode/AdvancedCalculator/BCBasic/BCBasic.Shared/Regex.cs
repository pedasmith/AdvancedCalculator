using System;
using System.Collections.Generic;
using System.Text;

namespace OverrideRegex
{
    public enum RegexOptions { Normal, Compiled };
    public class Regex : System.Text.RegularExpressions.Regex
    {
        public Regex(string pattern, RegexOptions options)
            : base(pattern)
        {
        }
    }
}


