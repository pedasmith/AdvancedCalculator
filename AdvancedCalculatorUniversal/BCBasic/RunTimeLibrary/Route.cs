using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkParsers
{
    public class Route
    {
        string[] Sections;
        public string Data;

        /// <summary>
        /// Takes a string like user/{name}/about/{detail}
        /// That route matches e.g. user/person/about/verbose
        /// and the match will set a dictionary { 'name':person, 'detail':verbose }
        /// </summary>
        /// <param name="route"></param>
        public Route (string route, string data)
        {
            Sections = route.Split (new char[] { '/'});
            Data = data;
        }

        public Dictionary<string, string> Match (string potentialMatch)
        {
            var potentialSections = potentialMatch.Split (new char[] { '/' });
            if (Sections.Length != potentialSections.Length)
            {
                // e.g. input is user/person and sections is user/{name}/about/{detail}/
                // This won't match because there's not enough in the string.
               // but also won't match input user/person/about/verbose to user/{name}
                return null;
            }
            var retval = new Dictionary<string, string>();
            for (int i = 0; i < Sections.Length; i++)
            {
                var section = Sections[i];
                var potential = potentialSections[i];
                if (SectionIsMatchType(section))
                {
                    retval.Add(SectionMatchName(section), potential);
                }
                else
                {
                    if (section.Equals (potential,  StringComparison.OrdinalIgnoreCase))
                    {
                        // Matches, keep going.
                    }
                    else
                    {
                        // Got a non-match
                        return null;
                    }
                }
            }


            return retval;
        }

        private static bool SectionIsMatchType(string section)
        {
            if (section.StartsWith("{") && section.EndsWith("}")) return true;
            return false;
        }

        /// <summary>
        /// Given {person} returns person.
        /// Given person returns null.
        /// </summary>
        /// <param name="section"></param>
        /// <returns></returns>
        private static string SectionMatchName(string section)
        {
            if (!SectionIsMatchType(section)) return null;
            var middle = section.Substring (1, section.Length-2);
            return middle;
        }
    }
}
