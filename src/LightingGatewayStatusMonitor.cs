using System;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Net.Http;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Queues;
using RequestType = Crestron.SimplSharp.Net.Http.RequestType;

namespace PoeTexasCorTap
{
    public class LightingGatewayStatusMonitor : StatusMonitorBase
    {
        private readonly LightingGatewayDevice _parent;
        private readonly string _url;
        private readonly CTimer _pollTimer;
        private readonly LightingGatewayQueue _queue;

        public LightingGatewayStatusMonitor(LightingGatewayDevice parent, string url, long warningTime, long errorTime, LightingGatewayQueue queue)
            : base(parent, warningTime, errorTime)
        {
            _parent = parent;
            _url = url;
            _queue = queue;
            _pollTimer = new CTimer(o => _queue.Enqueue(Poll), Timeout.Infinite);

            Status = MonitorStatus.InError;

            CrestronEnvironment.ProgramStatusEventHandler += type =>
            {
                if (type != eProgramStatusEventType.Stopping)
                    return;

                _pollTimer.Stop();
                _pollTimer.Dispose();
            };
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
                using (var response = _parent.Client.Dispatch(request))
                {
                    if (response.Code != 200)
                        return;

                    Status = MonitorStatus.IsOk;
                    ResetErrorTimers();
                }
            }
            catch (HttpException ex)
            {
                Debug.Console(1, this, "Caught an Http Exception dispatching a lighting poll: {0}{1}", ex.Message, ex.StackTrace);
            }
            catch (Exception ex)
            {
                Debug.Console(1, this, "Caught an Exception dispatching a lighting poll: {0}{1}", ex.Message, ex.StackTrace);
            }
        }

        public static HttpClientRequest GetPollRequest(string url)
        {
            var request = new HttpClientRequest { RequestType = RequestType.Get };
            request.Header.SetHeaderValue("accept", "application/json");
            request.Url.Parse("http://" + url + "/v2/config/version");
            return request;
        }
    }
}