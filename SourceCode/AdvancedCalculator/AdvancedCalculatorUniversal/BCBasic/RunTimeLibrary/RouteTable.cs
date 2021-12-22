using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkParsers
{
    public class RouteTable
    {
        Route DefaultRoute = null;
        List<Route> Table = new List<Route>();

        public void AddRoute (string route, string data)
        {
            if (route == "")
            {
                DefaultRoute = new Route(route, data);
            }
            else
            {
                Table.Add(new Route(route, data));
            }
        }

        public RouteFindResults Find (string lookup)
        {
            foreach (var route in Table)
            {
                var result = route.Match (lookup);
                if (result != null)
                {
                    return new RouteFindResults(route, result);
                }
            }
            return new RouteFindResults (DefaultRoute, null);
        }

    }
}
