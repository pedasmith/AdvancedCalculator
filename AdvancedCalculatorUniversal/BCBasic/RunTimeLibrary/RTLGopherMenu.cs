using BCBasic;
using NetworkParsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedCalculator.BCBasic.RunTimeLibrary
{
    public class RTLGopherMenu : IObjectValue
    {
        public RTLGopherMenu()
        {

        }

        public RTLGopherMenu(string defaultHost, string defaultPort)
        {
            DefaultHost = defaultHost;
            DefaultPort = defaultPort;
        }
        public string DefaultHost = "";
        public string DefaultPort = "";

        public string PreferredName
        {
            get
            {
                return "GopherMenu";
            }
        }

        public BCValue GetValue(string name)
        {
            switch (name)
            {
                case "Methods":
                    return new BCValue("Add,ToString");
            }
            return BCValue.MakeNoSuchProperty(this, name);
        }

        public void SetValue(string name, BCValue value)
        {
        }

        public IList<string> GetNames()
        {
            return new List<string>() { "Methods" };
        }

        public void InitializeForRun()
        {
        }

        public void RunComplete()
        {
            throw new NotImplementedException();
        }


        public void Dispose()
        {
        }

        class GopherMenuEntry
        {
            public GopherMenuEntry (string defaultHost, string defaultPort)
            {
                DefaultHost = defaultHost;
                DefaultPort = defaultPort;
            }
            string _menuType = null;
            public string MenuType
            {
                get
                {
                    if (_menuType == null || _menuType == "") return "1";
                    return _menuType.Substring(0, 1).EscapeGopher();
                }
                set
                {
                    if (_menuType == value) return;
                    _menuType = value;
                }
            }
            public string User = "";
            public string Selector = "";
            public string Host = "";
            public string DefaultHost = "localhost";
            public string HostOrDefault
            {
                get { if (String.IsNullOrEmpty(Host)) return DefaultHost; return Host; }
            }
            public string Port = "";
            public string DefaultPort = "70";
            public string PortOrDefault
            {
                get { if (String.IsNullOrEmpty(Port)) return DefaultPort; return Port; }
            }
            private bool MenuNeedsHost
            {
                get
                {
                    switch (MenuType)
                    {
                        // The 'i' (info) type is the only one that doesn't need a host
                        case "i":
                            return false;
                    }
                    return true;
                }
            }
            public string Options = "";
            private bool CanReturnShortLine
            {
                get
                {
                    if (Selector.Length >= 1) return false; 
                    if (Options.Length >= 1) return false;
                    if (MenuNeedsHost) return false;
                    if (Host.Length >= 1 || Port.Length >= 1) return false;
                    // No options, no host, no port, no default host needed -- OK to be short!
                    return true;
                }
            }

            /// <summary>
            /// Returns a Gopher-formatted menu entry with 3 or 4 tabs. Includes \r\n
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                var retval = MenuType + User.EscapeGopher();
                if (CanReturnShortLine)
                {
                    // Don't need anything else; just return;
                    return retval + "\r\n";
                }
                retval += $"\t{Selector.EscapeGopher()}\t{HostOrDefault.EscapeGopher()}\t{PortOrDefault.EscapeGopher()}";
                if (Options.Length >= 1)
                {
                    retval += $"\t{Options.EscapeGopher()}";
                }
                retval += "\r\n";
                return retval;
            }
        }

        public void AddMenuEntry (string menuType, string user, string selector="", string host="", string port="", string options="")
        {
            var gme = new GopherMenuEntry(DefaultHost, DefaultPort);
            gme.MenuType = menuType;
            gme.User = user;
            gme.Selector = selector;
            if (!String.IsNullOrEmpty (host)) gme.Host = host;
            if (!String.IsNullOrEmpty(port)) gme.Port = port;
            gme.Options = options;
            MenuEntries.Add(gme);
        }
        private List<GopherMenuEntry> MenuEntries = new List<GopherMenuEntry>();
        public async Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            switch (name)
            {
                case "Add":
                    {
                        // type user [selector host port options{+,INLINE,etc}]
                        if (!BCObjectUtilities.CheckArgs(2, 6, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var gme = new GopherMenuEntry(DefaultHost, DefaultPort);
                        gme.MenuType = (await ArgList[0].EvalAsync(context)).AsString;
                        gme.User = (await ArgList[1].EvalAsync(context)).AsString;
                        if (ArgList.Count > 2) gme.Selector = (await ArgList[2].EvalAsync(context)).AsString;
                        if (ArgList.Count > 3) gme.Host = (await ArgList[3].EvalAsync(context)).AsString;
                        if (ArgList.Count > 4) gme.Port = (await ArgList[4].EvalAsync(context)).AsString;
                        if (ArgList.Count > 5) gme.Options = (await ArgList[5].EvalAsync(context)).AsString;
                        MenuEntries.Add(gme);
                        return RunResult.RunStatus.OK;
                    }
                default:
                    await Task.Delay(0);
                    BCValue.MakeNoSuchMethod(Retval, this, name);
                    return RunResult.RunStatus.ErrorContinue;

            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var gme in MenuEntries)
            {
                sb.Append(gme.ToString());
            }

            return sb.ToString();
        }
    }
}
