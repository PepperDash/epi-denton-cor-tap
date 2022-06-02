using System;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharp.Net.Https;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using RequestType = Crestron.SimplSharp.Net.Http.RequestType;

namespace PoeTexasCorTap
{
    public class LightingGatewayStatusMonitor : StatusMonitorBase
    {
        private readonly string _url;
        private readonly CTimer _pollTimer;
        private readonly HttpClient _client = new HttpClient();

        public LightingGatewayStatusMonitor(IKeyed parent, string url, long warningTime, long errorTime) : base(parent, warningTime, errorTime)
        {
            _url = url;
            _pollTimer = new CTimer(o => Poll(), Timeout.Infinite);
        }

        public override void Start()
        {
            const int pollTime = 15000;
            _pollTimer.Reset(481, pollTime);
            StartErrorTimers();
        }

        public override void Stop()
        {
            StopErrorTimers();
            _pollTimer.Stop();
        }

        public void Poll()
        {
            try
            {
                var request = GetPollRequest(_url);
                _client.Dispatch(request);
                ResetErrorTimers();
            }
            catch (HttpsException ex)
            {
                Debug.Console(1, "Caught an Https Exception dispatching a lighting poll: {0}{1}", ex.Message, ex.StackTrace);
            }
            catch (Exception ex)
            {
                Debug.Console(1, "Caught an Exception dispatching a lighting poll: {0}{1}", ex.Message, ex.StackTrace);
            }
        }

        public static HttpClientRequest GetPollRequest(string url)
        {
            var request = new HttpClientRequest { RequestType = RequestType.Get };
            request.Url.Parse("http://" + url + "/v2/config/version");
            return request;
        }
    }
}