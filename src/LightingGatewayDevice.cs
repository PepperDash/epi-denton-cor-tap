using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharp.Net.Https;
using Crestron.SimplSharpPro.DeviceSupport;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.Lighting;
using RequestType = Crestron.SimplSharp.Net.Http.RequestType;

namespace PoeTexasCorTap
{
    public class LightingGatewayDevice : LightingBase, ICommunicationMonitor, IOnline
    {
        public readonly string Url;
        public readonly string FixtureName;

        private readonly CTimer _levelDispatchTimer;

        private static readonly Dictionary<string, CTimer> PollTimers =
            new Dictionary<string, CTimer>(StringComparer.OrdinalIgnoreCase);

        private int _requestedLevel;

        public LightingGatewayDevice(DeviceConfig config) : base(config.Key, config.Name)
        {
            ResetDebugLevels();
            var props = config.Properties.ToObject<LightingGatewayConfig>();
            FixtureName = props.FixtureName;
            Url = props.Url;
            LightingScenes = props.Scenes.ToList();

            _levelDispatchTimer = new CTimer(
                o =>
                {
                    try
                    {
                        DispatchLevelRequest(Url, FixtureName, _requestedLevel);
                    }
                    catch (HttpException ex)
                    {
                        Debug.Console(
                            1,
                            this,
                            "Caught an Http Exception dispatching a lighting command: {0}{1}",
                            ex.Message,
                            ex.StackTrace);
                    }
                    catch (Exception ex)
                    {
                        Debug.Console(
                            1,
                            this,
                            "Caught an error dispatching a lighting command: {0}{1}",
                            ex.Message,
                            ex.StackTrace);
                    }
                },
                Timeout.Infinite);

            CommunicationMonitor = new LightingGatewayStatusMonitor(this, Url, 60000, 120000);

            DeviceManager.AllDevicesActivated +=
                (sender, args) => CrestronInvoke.BeginInvoke(o => CommunicationMonitor.Start());
            IsOnline.OutputChange +=
                (sender, args) => Debug.Console(1, Debug.ErrorLogLevel.Notice, "Online Status:{0}", args.BoolValue);
        }

        public override bool CustomActivate()
        {
            if (!PollTimers.ContainsKey(Url))
            {
                var request = GetRequestForInfo(Url);
                PollTimers.Add(
                    Url,
                    new CTimer(
                        o =>
                        {
                            try
                            {
                                using (var client = new HttpClient())
                                using (var response = client.Dispatch(request))
                                    Debug.Console(DebugLevel, this, "Current Fixtures: {0}", response.ContentString);
                            }
                            catch (HttpsException ex)
                            {
                                Debug.Console(
                                    1,
                                    this,
                                    "Caught an Https Exception dispatching a lighting poll: {0}{1}",
                                    ex.Message,
                                    ex.StackTrace);
                            }
                            catch (Exception ex)
                            {
                                Debug.Console(
                                    1,
                                    this,
                                    "Caught an error dispatching a lighting poll: {0}{1}",
                                    ex.Message,
                                    ex.StackTrace);
                            }
                        },
                        null,
                        30000,
                        30000));
            }

            return base.CustomActivate();
        }

        public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            LinkLightingToApi(this, trilist, joinStart, joinMapKey, bridge);
            var joinMap = new LightingGatewayJoinMap(joinStart);
            if (bridge != null)
                bridge.AddJoinMap(Key + "-custom", joinMap);

            trilist.SetUShortSigAction(joinMap.RampFixture.JoinNumber, SetLoadLevel);
            trilist.SetString(joinMap.FixtureName.JoinNumber, string.IsNullOrEmpty(Name) ? FixtureName : Name);
        }

        public override void SelectScene(LightingScene scene)
        {
            try
            {
                Debug.Console(
                    VerboseLevel,
                    this,
                    "SelectScene: Scene called with ID: {0} and Name: {1}",
                    scene.ID,
                    scene.Name);
                var request = scene.GetRequestForScene(Url);
                using (var client = new HttpClient())
                using (var response = client.Dispatch(request))
                {
                    Debug.Console(
                        DebugLevel,
                        this,
                        "SelectScene: Dispatched a lighting command: {0} | Response: {1}",
                        request.ContentString,
                        response.Code);
                }
            }
            catch (Exception ex)
            {
                Debug.Console(
                    DebugLevel,
                    this,
                    Debug.ErrorLogLevel.Notice,
                    "SelectScene: Caught an error dispatching a lighting command: {0}{1}",
                    ex.Message,
                    ex.StackTrace);
            }
        }

        public void SetLoadLevel(ushort level)
        {
            _requestedLevel = level;
            _levelDispatchTimer.Reset(250);
        }

        public static HttpClientRequest GetRequestForInfo(string url)
        {
            var request = new HttpClientRequest { RequestType = RequestType.Get };

            request.Header.SetHeaderValue("accept", "application/json");
            request.FinalizeHeader();
            request.Url.Parse("http://" + url + "/v2/devices/");
            return request;
        }

        public static HttpClientRequest GetRequestForLevel(string url, string name, int level)
        {
            var scaledLevel = CrestronEnvironment.ScaleWithLimits(level, ushort.MaxValue, ushort.MinValue, 10000, 0);
            var body = new {name, level = scaledLevel};

            var request = new HttpClientRequest
            {
                RequestType = RequestType.Put,
                ContentString = JsonConvert.SerializeObject(body),
            };

            request.Url.Parse("http://" + url + "/v2/devices/levels");
            request.Header.RequestType = "PUT";
            request.Header.ContentType = "application/json";
            request.Header.AddHeader(new HttpHeader("accept", "application/json"));
            request.FinalizeHeader();

            return request;
        }

        public static void DispatchLevelRequest(string url, string fixtureName, int requestedLevel)
        {
            var request = GetRequestForLevel(url, fixtureName, requestedLevel);
            using (var client = new HttpClient())
            using (var response = client.Dispatch(request))
            {
                var responseString = response.ContentString;
                Debug.Console(
                    1,
                    "Dispatched a level request:{0}: Response: {1}",
                    requestedLevel,
                    responseString ?? String.Empty);
            }
        }

        public StatusMonitorBase CommunicationMonitor { get; private set; }
        public BoolFeedback IsOnline { get { return CommunicationMonitor.IsOnlineFeedback; } }

        /// <summary>
        ///     Trace level (0)
        /// </summary>
        public uint TraceLevel { get; set; }

        /// <summary>
        ///     Debug level (1)
        /// </summary>
        public uint DebugLevel { get; set; }

        /// <summary>
        ///     Verbose Level (2)
        /// </summary>
        public uint VerboseLevel { get; set; }

        private CTimer _debugTimer;

        /// <summary>
        ///     Resets debug levels for this device instancee
        /// </summary>
        /// <example>
        ///     devjson:1 {"deviceKey":"{deviceKey}", "methodName":"ResetDebugLevels", "params":[]}
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
        ///     Sets the debug levels for this device instance
        /// </summary>
        /// <example>
        ///     devjson:1 {"deviceKey":"{deviceKey}", "methodName":"SetDebugLevels", "params":[{level, 0-2}]}
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