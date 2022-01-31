using System;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Net.Http;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace PoeTexasCorTap
{
    public class LightingGatewayStatusMonitor : StatusMonitorBase
    {
        private readonly string _url;
        private readonly CTimer _pollTimer;

        public LightingGatewayStatusMonitor(IKeyed parent, string url, long warningTime, long errorTime) : base(parent, warningTime, errorTime)
        {
            _url = url;
            _pollTimer = new CTimer(o => Poll(), Timeout.Infinite);
        }

        public override void Start()
        {
            const int pollTime = 30000;
            _pollTimer.Reset(pollTime);
        }

        public override void Stop()
        {
            _pollTimer.Stop();
        }

        public void Poll()
        {
            try
            {
                var request = GetPollRequest(_url);
                using (var client = new HttpClient())
                using (var response = client.Dispatch(request))
                {
                    Debug.Console(1, "Dispatched a lighting command: {0} | Response: {1}", request.ContentString, response.Code);
                    ResetErrorTimers();
                };
            }
            catch (Exception ex)
            {
                Debug.Console(1, Debug.ErrorLogLevel.Notice, "Caught an error dispatching a lighting command: {0}{1}", ex.Message, ex.StackTrace);
            }
        }

        public static HttpClientRequest GetPollRequest(string url)
        {
            var request = new HttpClientRequest { RequestType = RequestType.Get };
            request.Header.SetHeaderValue("Content-Type", "application/json");
            request.Url.Parse(url + "/v2/config/version");
            return request;
        }
    }
}