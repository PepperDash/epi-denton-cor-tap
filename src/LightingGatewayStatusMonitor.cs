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
        private readonly HttpClient _client = new HttpClient();

        public LightingGatewayStatusMonitor(IKeyed parent, string url, long warningTime, long errorTime) : base(parent, warningTime, errorTime)
        {
            _url = url;
            _pollTimer = new CTimer(o => Poll(), Timeout.Infinite);
        }

        public override void Start()
        {
            const int pollTime = 30000;
            _pollTimer.Reset(0, pollTime);
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
                _client.DispatchAsync(
                    request, (o, error) =>
                    {
                        switch (error)
                        {
                            case HTTP_CALLBACK_ERROR.COMPLETED:
                                ResetErrorTimers();
                                break;
                            case HTTP_CALLBACK_ERROR.INVALID_PARAM:
                                Debug.Console(
                                    1, Debug.ErrorLogLevel.Notice, "Caught an error dispatching a lighting command: {0}",
                                    HTTP_CALLBACK_ERROR.INVALID_PARAM.ToString());
                                break;
                            case HTTP_CALLBACK_ERROR.UNKNOWN_ERROR:
                                Debug.Console(
                                    1, Debug.ErrorLogLevel.Notice, "Caught an error dispatching a lighting command: {0}",
                                    HTTP_CALLBACK_ERROR.UNKNOWN_ERROR.ToString());
                                break;
                            default:
                                throw new ArgumentOutOfRangeException("error");
                        }
                    });
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
            request.Url.Parse("http://" + url + "/v2/config/version");
            return request;
        }
    }
}