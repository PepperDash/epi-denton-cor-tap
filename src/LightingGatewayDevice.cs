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
using Timeout = System.Threading.Timeout;

namespace PoeTexasCorTap
{
    public class LightingGatewayDevice : LightingBase, ICommunicationMonitor, IOnline
    {
        public readonly HttpClient Client = new HttpClient { KeepAlive = true };
        public readonly string Url;
        public readonly string FixtureName;

        private int _currentLevel;
        private readonly LightingGatewayQueue _dispatchQueue;
        private readonly CTimer _levelPoll;

        public readonly IntFeedback CurrentLevelFeedback;

        public LightingGatewayDevice(DeviceConfig config) : base(config.Key, config.Name)
        {
            ResetDebugLevels();
            var props = config.Properties.ToObject<LightingGatewayConfig>();
            FixtureName = props.FixtureName;
            Url = props.Url;

            foreach (var scene in props.Scenes.Select(scene => new LightingScene {ID = scene.Id, Name = scene.Name}))
                LightingScenes.Add(scene);

            _dispatchQueue = new LightingGatewayQueue(Key + "-queue");

            CommunicationMonitor = new LightingGatewayStatusMonitor(this, Url, 60000, 120000);

            DeviceManager.AllDevicesActivated +=
                (sender, args) => CrestronInvoke.BeginInvoke(o => CommunicationMonitor.Start());

            IsOnline.OutputChange +=
                (sender, args) => Debug.Console(1, Debug.ErrorLogLevel.Notice, "Online Status:{0}", args.BoolValue);

            CurrentLevelFeedback =
                new IntFeedback(
                    () => CrestronEnvironment.ScaleWithLimits(_currentLevel, 10000, ushort.MinValue, ushort.MaxValue, 0));

            _levelPoll = new CTimer(o => Poll(), null, Timeout.Infinite, 5000);
        }

        public void Poll()
        {
            _dispatchQueue.Enqueue(() =>
            {
                try
                {
                    var request = GetRequestForInfo(Url);
                    var response = Client.Dispatch(request);
                    Debug.Console(
                            1,
                            this,
                            "Dispatched a level poll");

                        Debug.Console(
                            2,
                            this,
                            "Response: {0}",
                            response.ContentString ?? String.Empty);

                    var data = JsonConvert.DeserializeObject<List<LightingDeviceState>>(response.ContentString);
                    data.Where(d => d.Name.Equals(FixtureName, StringComparison.OrdinalIgnoreCase))
                        .ToList()
                        .ForEach(
                            d =>
                            {
                                var result = d.Level ?? default (int);
                                _currentLevel = (int) result;

                                Debug.Console(1, this, "Setting current level:{0}", _currentLevel);
                                CurrentLevelFeedback.FireUpdate();
                            });
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
            });
        }

        public override bool CustomActivate()
        {
            _levelPoll.Reset(0, 5000);
            return base.CustomActivate();
        }

        public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            var joinMap = new GenericLightingJoinMap(joinStart);

            var joinMapSerialized = JoinMapHelper.GetSerializedJoinMapForDevice(joinMapKey);

            if (!string.IsNullOrEmpty(joinMapSerialized))
                joinMap = JsonConvert.DeserializeObject<GenericLightingJoinMap>(joinMapSerialized);

            if (bridge != null)
            {
                bridge.AddJoinMap(Key, joinMap);
            }
            else
            {
                Debug.Console(0, this,
                    "Please update config to use 'eiscapiadvanced' to get all join map features for this device.");
            }

            Debug.Console(1, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
            Debug.Console(0, "Linking to Lighting Type {0}", GetType().Name);

            IsOnline.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);
            // GenericLighitng Actions & FeedBack
            trilist.SetUShortSigAction(joinMap.SelectScene.JoinNumber,
                u =>
                {
                    var scene = LightingScenes.ElementAtOrDefault(u);
                    if (scene != null)
                        SelectScene(scene);
                });

            var sceneIndex = 0;
            foreach (var scene in LightingScenes)
            {
                var index = sceneIndex;
                trilist.SetSigTrueAction((uint) (joinMap.SelectSceneDirect.JoinNumber + sceneIndex),
                    () =>
                    {
                        var s = LightingScenes.ElementAtOrDefault(index);
                        if (s != null)
                            SelectScene(s);
                    });

                scene.IsActiveFeedback.LinkInputSig(
                    trilist.BooleanInput[(uint) (joinMap.SelectSceneDirect.JoinNumber + sceneIndex)]);
                trilist.SetString((uint) (joinMap.SelectSceneDirect.JoinNumber + sceneIndex), scene.Name);
                trilist.BooleanInput[(uint) (joinMap.ButtonVisibility.JoinNumber + sceneIndex)].BoolValue = true;
                sceneIndex++;
            }

            var customJoinMap = new LightingGatewayJoinMap(joinStart);
            if (bridge != null)
                bridge.AddJoinMap(Key + "-custom", customJoinMap);

            CurrentLevelFeedback.LinkInputSig(trilist.UShortInput[customJoinMap.RampFixture.JoinNumber]);
            trilist.SetUShortSigAction(customJoinMap.RampFixture.JoinNumber, SetLoadLevel);
            trilist.SetString(customJoinMap.FixtureName.JoinNumber, string.IsNullOrEmpty(Name) ? FixtureName : Name);
        }

        public override void SelectScene(LightingScene scene)
        {
            _dispatchQueue.Enqueue(() =>
            {
                try
                {
                    Debug.Console(
                        1,
                        this,
                        "SelectScene: Scene called with ID: {0} and Name: {1}",
                        scene.ID,
                        scene.Name);

                    var request = scene.GetRequestForScene(Url);
                    var response = Client.Dispatch(request);
                    Debug.Console(
                        2,
                        this,
                        "SelectScene: Dispatched a scene request: {0} | Response: {1}",
                        request.Url.PathAndParams,
                        response.Code);

                    if (response.Code != 200)
                        throw new HttpException(response);
                }
                catch (Exception ex)
                {
                    Debug.Console(
                        1,
                        this,
                        Debug.ErrorLogLevel.Notice,
                        "SelectScene: Caught an error dispatching a scene request: {0}{1}",
                        ex.Message,
                        ex.StackTrace);
                }
            });

            _levelPoll.Reset(0, 5000);
        }

        public void SetLoadLevel(ushort level)
        {
            _dispatchQueue.Enqueue(() => DispatchLevelRequest(Url, FixtureName, level, Client));
            _levelPoll.Reset(100, 5000);
        }

        public static HttpClientRequest GetRequestForInfo(string url)
        {
            var request = new HttpClientRequest {RequestType = RequestType.Get};

            request.Header.SetHeaderValue("accept", "application/json");
            request.FinalizeHeader();
            request.Url.Parse("http://" + url + "/v2/devices/");
            return request;
        }

        public static HttpClientRequest GetRequestForLevelSet(string url, string name, int level)
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

        public static void DispatchLevelRequest(string url, string fixtureName, int requestedLevel, HttpClient client)
        {
            var request = GetRequestForLevelSet(url, fixtureName, requestedLevel);
            var response = client.Dispatch(request);
            var responseString = response.ContentString;
            Debug.Console(
                2,
                "Dispatched a level request:{0}: Response: {1}",
                requestedLevel,
                responseString ?? String.Empty);
        }

        public StatusMonitorBase CommunicationMonitor { get; private set; }

        public BoolFeedback IsOnline
        {
            get { return CommunicationMonitor.IsOnlineFeedback; }
        }

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