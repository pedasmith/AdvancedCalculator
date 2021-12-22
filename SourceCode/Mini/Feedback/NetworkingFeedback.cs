using System;
using System.Threading.Tasks;


// Feedback
using System.Net.Http;
using System.Net.Http.Headers;
using Windows.Data.Json;
using System.Threading;
using Windows.UI.Xaml.Controls;

namespace Shipwreck.Utilities
{
    public class NetworkingFeedback
    {
        public ILog Log;
        HttpClient feedback = null;
        class FeedbackHeaders : MessageProcessingHandler
        {
            public FeedbackHeaders(HttpMessageHandler innerHandler)
                : base(innerHandler)
            {
            }
            protected override HttpRequestMessage ProcessRequest(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json; charset=utf-8");
                return request;
            }

            protected override HttpResponseMessage ProcessResponse(HttpResponseMessage response, CancellationToken cancellationToken)
            {
                return response;
            }
        }

        public async Task SendFeedback(string product, string subject, string body, string appDetails, TextBlock uiFeedbackStatus)
        {
            var host = "shipwreckfeedback.azurewebsites.net";
            var baseUrl = new Uri("http://" + host + "/api/");

            lock (this)
            {
                if (feedback == null)
                {
                    var bottom = new HttpClientHandler();
                    HttpMessageHandler chain = bottom;
                    chain = new FeedbackHeaders(chain);
                    const int timeoutInSeconds = 40;
                    feedback = new HttpClient(chain) { BaseAddress = baseUrl, Timeout = new TimeSpan(0, 0, timeoutInSeconds) };
                }
            }
            JsonObject data = new JsonObject();
            data.Add("AppName", JsonValue.CreateStringValue(product));
            data.Add("Like", JsonValue.CreateStringValue(subject));
            data.Add("Comment", JsonValue.CreateStringValue(body));
            data.Add("AppDetails", JsonValue.CreateStringValue(appDetails));
            var jsonString = data.Stringify();
            StringContent content = new StringContent(jsonString);

            uiFeedbackStatus.Text = "sending...";
            bool showError = false;
            string uiError = "Error in sending feedback"; // Generic error

            if (subject == "test network")
            {
                uiFeedbackStatus.Text = "waiting 20 seconds";
                await Task.Delay(20000);
                uiFeedbackStatus.Text = "wait complete";
            }

            try{
                var response = await feedback.PostAsync("UserComments", content);
                uiError = "Error in recieving confirmation";
                var resultString = await response.Content.ReadAsStringAsync();

                uiError = "Error in sending feedback; didn't understand response";
                JsonObject result = JsonObject.Parse(resultString);
                if (result.ContainsKey("d"))
                {
                    string resultText = result.GetNamedString("d");
                    uiFeedbackStatus.Text = resultText;
                }
                else
                {
                    uiFeedbackStatus.Text = "Thank you for your feedback.";
                }
            } catch (Exception ex)
            {
                if (Log != null) Log.WriteWithTime("Exception while networking");
                if(Log != null) Log.WriteWithTime("Exception: {0} message {1}", ex.ToString(), ex.Message);
                if (ex.InnerException != null)
                {
                    if (Log!=null) Log.WriteWithTime("Inner Exception: {0} message {1}", ex.InnerException.ToString(), ex.InnerException.Message);
                }
                showError = true;
            }

            if (showError)
            {
                uiFeedbackStatus.Text = uiError; 
            }
            await Task.Delay(30000);
            uiFeedbackStatus.Text = "Ready";
        }

    }
}
