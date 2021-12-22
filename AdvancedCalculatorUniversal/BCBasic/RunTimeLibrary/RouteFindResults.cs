using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkParsers
{
    public class RouteFindResults
    {
        public RouteFindResults(Route route, Dictionary<string, string> values)
        {
            Route = route;
            Values = values;
        }
        public Route Route;
        public Dictionary<string, string> Values;
    }
}
