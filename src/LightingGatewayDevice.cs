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
        private readonly CTimer _levelDispatchTimer;
        private readonly LightingGatewayConfig _config;

        private int _requestedLevel;

        public LightingGatewayDevice(DeviceConfig config) : base(config.Key, config.Name)
        {
            var props = config.Properties.ToObject<LightingGatewayConfig>();
            _config = props;
            _url = props.Url;
            LightingScenes = props.Scenes.ToList();
            Debug.Console(VerboseLevel, this, "LightingGatewayDevice: LightingScenes ToList() Count: {0}", LightingScenes.Count);

            _levelDispatchTimer = new CTimer(o =>
            {
                try
                {
                    var request = GetRequestForLevel(_url, props.FixtureName, _requestedLevel);
                    using (var client = new HttpClient())
                    using (var response = client.Dispatch(request))
                    {
                        Debug.Console(DebugLevel, this, "Dispatched a lighting command: {0} | Response: {1}", request.ContentString, response.Code);
                    };
                }
                catch (Exception ex)
                {
                    Debug.Console(DebugLevel, this, Debug.ErrorLogLevel.Notice, "LightingGatewayDevice: Caught an error dispatching a lighting command: {0}{1}", ex.Message, ex.StackTrace);
                }
            }, Timeout.Infinite);

            CommunicationMonitor = new LightingGatewayStatusMonitor(this, _url, 60000, 120000);

            DeviceManager.AllDevicesActivated += (sender, args) => CrestronInvoke.BeginInvoke(o => CommunicationMonitor.Start());
            ResetDebugLevels();
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
                Debug.Console(VerboseLevel, this, "SelectScene: Scene called with ID: {0} and Name: {1}", scene.ID, scene.Name);
                var request = scene.GetRequestForScene(_url);
                using (var client = new HttpClient())
                using (var response = client.Dispatch(request))
                {
                    Debug.Console(DebugLevel, this, "SelectScene: Dispatched a lighting command: {0} | Response: {1}", request.ContentString, response.Code);
                }; 
            }
            catch (Exception ex)
            {
                Debug.Console(DebugLevel, this, Debug.ErrorLogLevel.Notice, "SelectScene: Caught an error dispatching a lighting command: {0}{1}", ex.Message, ex.StackTrace);
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

        /// <summary>
        /// Trace level (0)
        /// </summary>
        public uint TraceLevel { get; set; }

        /// <summary>
        /// Debug level (1)
        /// </summary>
        public uint DebugLevel { get; set; }

        /// <summary>
        /// Verbose Level (2)
        /// </summary>        
        public uint VerboseLevel { get; set; }

        private CTimer _debugTimer;

        /// <summary>
        /// Resets debug levels for this device instancee
        /// </summary>
        /// <example>
        /// devjson:1 {"deviceKey":"{deviceKey}", "methodName":"ResetDebugLevels", "params":[]}
        /// </example>
        public void ResetDebugLevels()
        {
            TraceLevel = 0;
            DebugLevel = 1;
            VerboseLevel = 2;

            if (_debugTimer == null)
                return;

            _debugTimer.Stop();
        }

        /// <summary>
        /// Sets the debug levels for this device instance
        /// </summary>
        /// <example>
        /// devjson:1 {"deviceKey":"{deviceKey}", "methodName":"SetDebugLevels", "params":[{level, 0-2}]}
        /// </example>
        /// <param name="level"></param>
        public void SetDebugLevels(uint level)
        {
            TraceLevel = level;
            DebugLevel = level;
            VerboseLevel = level;

            if (_debugTimer == null)
                _debugTimer = new CTimer(_ => ResetDebugLevels(), 900000); // 900,000 = 15-mins
            else
                _debugTimer.Reset();
        }

    }
}