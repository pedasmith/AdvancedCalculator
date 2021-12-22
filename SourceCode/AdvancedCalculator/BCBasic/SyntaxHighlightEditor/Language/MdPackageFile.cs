using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Edit
{
    class MdPackageFile
    {
        public static IList<UnparsedProgram> ParseMdFile(string text)
        {

            List<UnparsedProgram> programs = new List<UnparsedProgram>();
            int lastIdx = text.IndexOf("\n### ");
            while (lastIdx > 0)
            {
                var nextIdx = text.IndexOf("\n### ", lastIdx);
                if (nextIdx < 0) break;
                var titleEnd = text.IndexOf("\n", nextIdx + 4);
                var title = text.Substring(nextIdx + 5, titleEnd - nextIdx - 5);
                var progstart = text.IndexOf("```BASIC", titleEnd);
                var progend = text.IndexOf("\n```", progstart);
                var program = text.Substring(progstart + 9, progend - progstart - 8);
                programs.Add(new UnparsedProgram(title, program));

                lastIdx = nextIdx > 0 ? nextIdx + 10 : -1;
            }
            return programs;
        }
    }
}
