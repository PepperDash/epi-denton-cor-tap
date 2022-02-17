using System;
using System.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharpPro.DeviceSupport;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.Lighting;


namespace PoeTexasCorTap
{
    public class LightingGatewayDevice: LightingBase, ICommunicationMonitor, IOnline
    {
        private readonly string _url;
        private int _requestedLevel;
        private readonly CTimer _levelDispatchTimer;
        private readonly  LightingGatewayConfig _config;

        public LightingGatewayDevice(DeviceConfig config) : base(config.Key, config.Name)
        {
            var props = config.Properties.ToObject<LightingGatewayConfig>();
            _config = props;
            _url = props.Url;
            LightingScenes = props.Scenes.ToList();
            Debug.Console(2, this, "LightingGatewayDevice: LightingScenes ToList() Count: {0}", LightingScenes.Count);
            _levelDispatchTimer = new CTimer(o =>
            {
                try
                {
                    var request = GetRequestForLevel(_url, props.FixtureName, _requestedLevel);
                    using (var client = new HttpClient())
                    using (var response = client.Dispatch(request))
                    {
                        Debug.Console(1, this, "Dispatched a lighting command: {0} | Response: {1}", request.ContentString, response.Code);
                    };
                }
                catch (Exception ex)
                {
                    Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "LightingGatewayDevice: Caught an error dispatching a lighting command: {0}{1}", ex.Message, ex.StackTrace);
                }
            }, Timeout.Infinite);

            CommunicationMonitor = new LightingGatewayStatusMonitor(this, _url, 60000, 120000);

            DeviceManager.AllDevicesActivated += (sender, args) => CrestronInvoke.BeginInvoke(o => CommunicationMonitor.Start());
        }

        public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            LinkLightingToApi(this, trilist, joinStart, joinMapKey, bridge);
            var joinMap = new LightingGatewayJoinMap(joinStart);
            if (bridge != null)
                bridge.AddJoinMap(Key + "-custom", joinMap);
            
            trilist.SetUShortSigAction(joinMap.RampFixture.JoinNumber, SetLoadLevel);
            trilist.SetString(joinMap.FixtureName.JoinNumber, _config.FixtureName);
        }

        public override void SelectScene(LightingScene scene)
        {
            try
            {
                Debug.Console(2, this, "SelectScene: Scene called with ID: {0} and Name: {1}", scene.ID, scene.Name);
                var request = scene.GetRequestForScene(_url);
                using (var client = new HttpClient())
                using (var response = client.Dispatch(request))
                {
                    Debug.Console(1, this, "SelectScene: Dispatched a lighting command: {0} | Response: {1}", request.ContentString, response.Code);
                }; 
            }
            catch (Exception ex)
            {
                Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "SelectScene: Caught an error dispatching a lighting command: {0}{1}", ex.Message, ex.StackTrace);
            }
        }

        public void SetLoadLevel(ushort level)
        {
            _requestedLevel = level;
            _levelDispatchTimer.Reset(25);
        }

        public static HttpClientRequest GetRequestForLevel(string url, string name, int level)
        {
            var scaledLevel = CrestronEnvironment.ScaleWithLimits(level, ushort.MaxValue, ushort.MinValue, 10000, 0);
            var body = new { name, level = scaledLevel };

            var request = new HttpClientRequest { RequestType = RequestType.Put, ContentString = JsonConvert.SerializeObject(body) };
            request.Header.SetHeaderValue("Content-Type", "application/json");
            request.Url.Parse("http://" + url + "/v2/fixtures");
            return request;
        }

        public StatusMonitorBase CommunicationMonitor { get; private set; }
        public BoolFeedback IsOnline { get { return CommunicationMonitor.IsOnlineFeedback; } }
    }
}