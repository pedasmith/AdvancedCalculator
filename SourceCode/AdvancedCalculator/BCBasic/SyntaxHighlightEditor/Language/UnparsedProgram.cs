using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Edit
{
    class UnparsedProgram
    {
        public UnparsedProgram (string name, string text)
        {
            Name = name;
            Text = text;
        }
        public string Name { get; set; }
        public string Text { get; set; }
    }
}
