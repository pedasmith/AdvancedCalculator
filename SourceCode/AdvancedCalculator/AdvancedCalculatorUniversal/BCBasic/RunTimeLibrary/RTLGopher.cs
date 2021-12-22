using BCBasic;
using NetworkParsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using Windows.Networking.ServiceDiscovery.Dnssd;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace AdvancedCalculator.BCBasic.RunTimeLibrary
{
    public class RTLGopher: IObjectValue
    {
        private RouteTable Routing = new RouteTable();
        public string PreferredName
        {
            get { return "Gopher"; }
        }

        public BCValue GetValue(string name)
        {
            switch (name)
            {
                case "Methods":
                    return new BCValue("AddRoute,NewMenu,Start,ToString");
            }
            return BCValue.MakeNoSuchProperty(this, name);
        }

        public void SetValue(string name, BCValue value)
        {
            ; // Can't ever set a constant.
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
        }

        string GopherPort = "70";
        StreamSocketListener Listener = null;
        BCRunContext Context = null;

        string DefaultHost = "";
        string DefaultPort = "70";
    
        public async Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            switch (name)
            {
                case "AddRoute":
                    {
                        if (!BCObjectUtilities.CheckArgs(2, 2, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var route = (await ArgList[0].EvalAsync(context)).AsString;
                        var function = (await ArgList[1].EvalAsync(context)).AsString;
                        Routing.AddRoute(route, function);
                        Context = context;
                        return RunResult.RunStatus.OK;
                    }
                case "NewMenu":
                    {
                        if (!BCObjectUtilities.CheckArgs(0, 0, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                        var gm = new RTLGopherMenu(DefaultHost, DefaultPort);
                        Retval.AsObject = gm;
                        return RunResult.RunStatus.OK;
                    }

                case "Start":
                    if (!BCObjectUtilities.CheckArgs(1, 2, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    if (Listener == null)
                    {
                        var dnssdServiceName = (await ArgList[0].EvalAsync(context)).AsString;
                        ushort port = 70;
                        if (ArgList.Count > 1)
                        {
                            port = (ushort)(await ArgList[1].EvalAsync(context)).AsDouble;
                        }
                        var dnssdname = $"{dnssdServiceName}._gopher._tcp.local.";
                        try
                        {
                            Listener = new StreamSocketListener();
                            Listener.ConnectionReceived += Listener_ConnectionReceived;
                            await Listener.BindServiceNameAsync(GopherPort);
                            var gopherserv = new DnssdServiceInstance(dnssdname, null, port);
                            var registration = await gopherserv.RegisterStreamSocketListenerAsync(Listener);


                            // What's my local IP address?
                            var names = NetworkInformation.GetHostNames();
                            var namestr = "";
                            foreach (var host in names)
                            {
                                if (host.IPInformation != null)
                                {
                                    namestr += "IP=" + host.RawName + " ";
                                }
                                else 
                                {
                                    namestr += "NAME=" + host.RawName + " ";
                                }
                            }
                            Retval.AsString = $"Started on port {GopherPort} IP {namestr}";
                            return RunResult.RunStatus.OK;
                        }
                        catch (Exception ex)
                        {
                            Listener = null;
                            Retval.SetError(70, $"Unable to start listener {ex.Message}");
                            return RunResult.RunStatus.OK;
                        }
                    }
                    Retval.AsString = "Already Running";
                    return RunResult.RunStatus.OK;
                case "ToString":
                    Retval.AsString = $"Object {PreferredName}";
                    return RunResult.RunStatus.OK;
                default:
                    await Task.Delay(0);
                    BCValue.MakeNoSuchMethod(Retval, this, name);
                    return RunResult.RunStatus.ErrorContinue;
            }
        }

        public static string ConvertByteArrayToString(byte[] data)
        {
            bool isUtf8 = Network.StringUtilities.IsUtf8(data, data.Length);
            if (isUtf8)
            {
                var unicodeString = "";
                try
                {
                    unicodeString = Encoding.UTF8.GetString(data);
                }
                catch (Exception)
                {
                    ; // failed to encode...
                }
            }
            try
            {
                var results = Encoding.GetEncoding(28591).GetString(data);
                return results;
            }
            catch (Exception)
            {
                ; // failed to encode...
            }
            return $"iERROR!BADRESULTS\tENCODING\t70\t44";
            //return ConvertIBufferToString(ConvertByteArrayToIBuffer(data));
        }

        public static string ConvertIBufferToString(IBuffer bytes)
        {
            if (bytes.Length == 0) return "";
            return ConvertByteArrayToString(bytes.ToArray()); //WORKAROUND: if bytes is [] then the ToArray fails.
        }

        private async void Listener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            // Huh? What's all this?
            // It turns out that the LocalAddress when using IPv6 will return a "local" version of
            // the IPv6 address -- that means it has an extra 'scope' (IIRC) connected to it. 
            // For example, the IPv6 address might be 1:2:3%14 where %14 indicates an adapter.
            // This is actually remarkably non-useful because it can't be used by the other connection
            // at all. So split the address by % and take the stuff before the %.
            DefaultHost = args.Socket.Information.LocalAddress.CanonicalName.Split(new char[] { '%' })[0];
            DefaultPort = args.Socket.Information.LocalPort;

            // Read in the selector string <CR><LF>
            var dr = new DataReader(args.Socket.InputStream);
            dr.InputStreamOptions = InputStreamOptions.Partial;
            uint nbytes= await dr.LoadAsync(1000); //NOTE: very long selectors will fail and that's OK.
            if (nbytes == 0)
            {
                ; // Connection closed before we got any data
            }
            else
            {
                byte[] buffer = new byte[nbytes];
                dr.ReadBytes(buffer);
                bool gotEOL = false;
                for (int i = 0; i < nbytes && !gotEOL; i++)
                {
                    if (buffer[i] == (byte)'\r' || buffer[i] == (byte)'\n')
                    {
                        gotEOL = true;
                    }
                }

                var menu = "";

                // Got the EOL
                if (gotEOL)
                {
                    var str = ConvertByteArrayToString(buffer);
                    var lines = Network.StringUtilities.SplitGuessEnding(str);
                    var selectorLine = lines[0];
                    var selectors = selectorLine.Split(new char[] { '\t' }); // might have selector + search string.
                    var selector = selectors[0];
                    var search = selectors.Length > 1 ? selectors[1] : "";


                    var route = Routing.Find(selector); //match routes
                    if (route == null)
                    {
                        var gm = new RTLGopherMenu(DefaultHost, DefaultPort);
                        gm.AddMenuEntry("i", "Hello from Best Calculator");
                        gm.AddMenuEntry("i", $"Your IP address {args.Socket.Information.RemoteAddress.CanonicalName}");
                        gm.AddMenuEntry("i", $"Connecting to  {args.Socket.Information.LocalAddress.CanonicalName}");

                        // Artificially set up a GopherMenu
                        var names = NetworkInformation.GetHostNames();
                        foreach (var name in names)
                        {
                            gm.AddMenuEntry("1", $"Try {name}", "SystemInfo", $"{name}", GopherPort);
                        }
                        gm.AddMenuEntry("1", "Return", "", DefaultHost, DefaultPort);
                        menu = gm.ToString();
                    }
                    else
                    {
                        var ids = new BCValueList();
                        if (route.Values != null)
                        {
                            foreach (var item in route.Values)
                            {
                                ids.AddPropertyAllowDuplicates(item.Key, new BCValue(item.Value));
                            }
                        }
                        var arglist = new List<IExpression>() { new StringConstant(selector), new ObjectConstant(ids), new StringConstant(search) };
                        var functionName = route.Route.Data;
                        var callbackData = Context.ProgramRunContext.AddCallback(Context, functionName, arglist);
                        // Wait for the function to be called
                        const int StartDelayTime = 1;
                        const int MaxDelayTime = 50;
                        int delayTime = StartDelayTime;
                        while (!callbackData.CurrStatusIsComplete)
                        {
                            await Task.Delay(delayTime);
                            delayTime = Math.Min(MaxDelayTime, delayTime * 2);
                        }
                        // All done! Should have a return GopherMenu!
                        var gm = callbackData.ReturnValue?.AsObject as RTLGopherMenu;
                        if (gm != null)
                        {
                            menu = gm.ToString();
                        }
                    }


                    if (menu != "")
                    {
                        var dw = new DataWriter(args.Socket.OutputStream);
                        dw.WriteString(menu); //NOTE: uses UTF8 and not LATIN1. That's contrary to spec, but matches reality.
                        await dw.StoreAsync();
                        await dw.FlushAsync();
                        dw.DetachStream();
                        args.Socket.Dispose(); // And get rid of the stream.
                    }
                    //TODO: What happens without a return value?

                }
            }
            ;
        }

        public void Dispose()
        {
            if (Listener != null)
            {
                // Must stop listening
                Listener.Dispose();
            }
        }
    }
}
