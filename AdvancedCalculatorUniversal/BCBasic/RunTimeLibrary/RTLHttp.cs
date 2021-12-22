using BCBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace AdvancedCalculator.BCBasic.RunTimeLibrary
{
    public class RTLHttp: IObjectValue
    {
        public string PreferredName
        {
            get { return "HTTP"; }
        }

        public BCValue GetValue(string name)
        {
            switch (name)
            {
                case "Methods":
                    return new BCValue("Get,Post,Put,ToString");
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

        HttpClient hc;
        private HttpClient GetStockHttpClient()
        {
            if (hc == null) hc = new HttpClient();
            return hc;
        }

        private async Task<RunResult.RunStatus> DoHttpAsync(HttpMethod method, BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            var hasContent = method == HttpMethod.Get ? false : true;
            var uriIndex = 0;
            var contentIndex = hasContent ? 1 : -1;
            var headerIndex = hasContent ? 2 : 1;

            var uri = (await ArgList[uriIndex].EvalAsync(context)).AsString;
            Uri sendUri;
            var isUrl = Uri.TryCreate(uri, UriKind.Absolute, out sendUri);
            if (!isUrl)
            {
                Retval.SetError(998, $"The value provided is not a valid URL: {uri}");
                return RunResult.RunStatus.ErrorStop;
            }

            var request = new HttpRequestMessage(method, sendUri);

            if (contentIndex > 0 && ArgList.Count > headerIndex) 
            {
                var contentString = (await ArgList[contentIndex].EvalAsync(context)).AsString;
                var content = new HttpStringContent(contentString);
                request.Content = content;
            }

            // Now set the extra headers.  This is surprisingly difficult.
            if (headerIndex >= 0 && ArgList.Count > headerIndex)
            {
                string extraHeader;
                var headerObj = await ArgList[headerIndex].EvalAsync(context);
                if (headerObj.AsObject == null)
                {
                    Retval.SetError(991, $"The header provided must be an array");
                    return RunResult.RunStatus.ErrorStop;
                }
                var list = headerObj.AsObject as BCValueList;
                if (list == null)
                {
                    Retval.SetError(992, $"The header provided must be a DIM'd array");
                    return RunResult.RunStatus.ErrorStop;
                }
                foreach (var item in list.data)
                {
                    extraHeader = item.AsString;
                    // Split based on the first colon
                    // Example: Content-type: application/json ==> "Content-type" and "application/json"
                    var fields = extraHeader.Split(new char[] { ':' }, 2);
                    if (fields.Length != 2)
                    {
                        Retval.SetError(993, $"The header provided must include a colon: {extraHeader}");
                        return RunResult.RunStatus.ErrorStop;
                    }
                    var headerName = fields[0].TrimEnd();
                    var headerValue = fields[1].TrimStart();

                    // Seems like an awful lot of work to figure out where to place the headers
                    try
                    {
                        if (request.Content != null && request.Content.Headers.ContainsKey(headerName))
                        {
                            request.Content.Headers[headerName] = headerValue;
                        }
                        else
                        {
                            var ok = request.Content != null ? request.Content.Headers.TryAppendWithoutValidation(headerName, headerValue) : false;
                            if (!ok)
                            {
                                // Must be a request header
                                if (request.Headers.ContainsKey(headerName))
                                {
                                    request.Headers[headerName] = headerValue;
                                }
                                else
                                {
                                    ok = request.Headers.TryAppendWithoutValidation(headerName, headerValue);
                                    if (!ok)
                                    {
                                        Retval.SetError(994, $"The header {headerName} could not be added with value {headerValue}");
                                        return RunResult.RunStatus.ErrorStop;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Retval.SetError(994, $"Exception: Unknown header {headerName} {headerValue} {ex.Message}");
                    }
                }
            }

            // Now actually sent the HTTP request
            try
            {
                hc = GetStockHttpClient();
                var result = await hc.SendRequestAsync(request);
                if (result.IsSuccessStatusCode)
                {
                    var stringResult = await result.Content.ReadAsStringAsync();
                    var tuple = new BCValueList();
                    tuple.SetProperty("Content", new BCValue(stringResult));
                    tuple.SetProperty("StatusCode", new BCValue((int)result.StatusCode));
                    tuple.SetProperty("ReasonPhrase", new BCValue(result.ReasonPhrase));
                    Retval.AsObject = tuple;
                }
                else
                {
                    Retval.SetError((double)result.StatusCode, result.ReasonPhrase);
                }
            }
            catch (Exception ex)
            {
                Retval.SetError(999, $"Exception: {ex.Message}");
            }
            return RunResult.RunStatus.OK;
        }
        

    
        public async Task<RunResult.RunStatus> RunAsync(BCRunContext context, string name, IList<IExpression> ArgList, BCValue Retval)
        {
            switch (name)
            {
                case "Get":
                    if (!BCObjectUtilities.CheckArgs(1, 2, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await DoHttpAsync(HttpMethod.Get, context, name, ArgList, Retval);
                case "Post":
                    if (!BCObjectUtilities.CheckArgs(2, 3, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await DoHttpAsync(HttpMethod.Post, context, name, ArgList, Retval);
                case "Put":
                    if (!BCObjectUtilities.CheckArgs(2, 3, name, ArgList, Retval)) return RunResult.RunStatus.ErrorStop;
                    return await DoHttpAsync(HttpMethod.Put, context, name, ArgList, Retval);
                case "ToString":
                    Retval.AsString = $"Object {PreferredName}";
                    return RunResult.RunStatus.OK;
                default:
                    await Task.Delay(0);
                    BCValue.MakeNoSuchMethod(Retval, this, name);
                    return RunResult.RunStatus.ErrorContinue;
            }
        }


        public void Dispose()
        {
        }
    }
}
