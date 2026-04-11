using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharp.Net.Https;
using Crestron.SimplSharpPro.CrestronThread;
using Crestron.SimplSharpPro.DeviceSupport;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.Lighting;
#if SERIES4
using LightingBase = PepperDash.Essentials.Devices.Common.Lighting.LightingBase;
#endif
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

            CommunicationMonitor = new LightingGatewayStatusMonitor(this, Url, 60000, 120000, _dispatchQueue);

            DeviceManager.AllDevicesActivated +=
                (sender, args) => CrestronInvoke.BeginInvoke(o => CommunicationMonitor.Start());

            IsOnline.OutputChange +=
                (sender, args) =>
                {
#if SERIES4
                    Debug.LogInformation("Online Status:{status}", args.BoolValue);
#else
                    Debug.Console(1, Debug.ErrorLogLevel.Notice, "Online Status:{0}", args.BoolValue);
#endif
                };

            CurrentLevelFeedback =
#if SERIES4
                new IntFeedback("CurrentLevel",
#else
                new IntFeedback(
#endif
                    () => CrestronEnvironment.ScaleWithLimits(_currentLevel, 10000, ushort.MinValue, ushort.MaxValue, 0));

            _levelPoll = new CTimer(o => Poll(), null, Timeout.Infinite, 5000);
        }

        public void Poll()
        {
            _dispatchQueue.Enqueue(() =>
            {
                var content = string.Empty;
                var path = string.Empty;
                try
                {
                    var request = GetRequestForInfo(Url);
                    path = request.Url.PathAndParams;
                    using (var response = Client.Dispatch(request))
                    {
#if SERIES4
                        Debug.LogVerbose("[{key}] Dispatched a level poll", Key);

                        Debug.LogVerbose("[{key}] Response: {response}", Key, response.ContentString ?? String.Empty);
#else
                        Debug.Console(
                                1,
                                this,
                                "Dispatched a level poll");

                        Debug.Console(
                            2,
                            this,
                            "Response: {0}",
                            response.ContentString ?? String.Empty);
#endif

                        if (string.IsNullOrEmpty(response.ContentString))
                            return;

                        content = response.ContentString;
                        var json = JToken.Parse(content);

                        var nonArrayToken = json.SelectToken("fixtures");
                        if (nonArrayToken != null)
                        {
                            var data = nonArrayToken.ToObject<List<LightingDeviceState>>();
                            data.Where(d => d.Name.Equals(FixtureName, StringComparison.OrdinalIgnoreCase))
                                .ToList()
                                .ForEach(
                                    d =>
                                    {
                                        var result = d.Level ?? default(int);
                                        _currentLevel = (int)result;

#if SERIES4
                                        Debug.LogVerbose("[{key}] Setting current level:{level}", Key, _currentLevel);
#else
                                        Debug.Console(1, this, "Setting current level:{0}", _currentLevel);
#endif
                                        CurrentLevelFeedback.FireUpdate();
                                    });
                        }
                        else
                        {
                            var data = json.ToObject<List<LightingDeviceState>>();
                            data.Where(d => d.Name.Equals(FixtureName, StringComparison.OrdinalIgnoreCase))
                                .ToList()
                                .ForEach(
                                    d =>
                                    {
                                        var result = d.Level ?? default(int);
                                        _currentLevel = (int)result;

#if SERIES4
                                        Debug.LogVerbose("[{key}] Setting current level:{level}", Key, _currentLevel);
#else
                                        Debug.Console(1, this, "Setting current level:{0}", _currentLevel);
#endif
                                        CurrentLevelFeedback.FireUpdate();
                                    }); 
                        }
                    }
                }
                catch (HttpsException ex)
                {
#if SERIES4
                    Debug.LogError("[{key}] Caught an Https Exception dispatching a lighting poll: {message} {path} {content}", Key, ex.Message, path, content);
#else
                    Debug.Console(
                        1,
                        this,
                        "Caught an Https Exception dispatching a lighting poll: {0} {1} {2}",
                        ex.Message, path, content);
#endif
                }
                catch (Exception ex)
                {
#if SERIES4
                    Debug.LogError("[{key}] Caught an error dispatching a lighting poll: {message} {path} {content}", Key, ex.Message, path, content);
#else
                    Debug.Console(
                        1,
                        this,
                        "Caught an error dispatching a lighting poll: {0} {1} {2}",
                        ex.Message, path, content);
#endif
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
#if SERIES4
                Debug.LogWarning("[{key}] Please update config to use 'eiscapiadvanced' to get all join map features for this device.", Key);
#else
                Debug.Console(0, this,
                    "Please update config to use 'eiscapiadvanced' to get all join map features for this device.");
#endif
            }

#if SERIES4
            Debug.LogVerbose("Linking to Trilist '{id}'", trilist.ID.ToString("X"));
            Debug.LogVerbose("Linking to Lighting Type {type}", GetType().Name);
#else
            Debug.Console(1, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
            Debug.Console(0, "Linking to Lighting Type {0}", GetType().Name);
#endif

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
#if SERIES4
                    Debug.LogVerbose("[{key}] SelectScene: Scene called with ID: {id} and Name: {name}", Key, scene.ID, scene.Name);
#else
                    Debug.Console(
                        1,
                        this,
                        "SelectScene: Scene called with ID: {0} and Name: {1}",
                        scene.ID,
                        scene.Name);
#endif

                    var request = scene.GetRequestForScene(Url);
                    using (var response = Client.Dispatch(request))
                    {
#if SERIES4
                        Debug.LogVerbose("[{key}] SelectScene: Dispatched a scene request: {path} | Response: {code}", Key, request.Url.PathAndParams, response.Code);
#else
                        Debug.Console(
                            2,
                            this,
                            "SelectScene: Dispatched a scene request: {0} | Response: {1}",
                            request.Url.PathAndParams,
                            response.Code);
#endif

                        if (response.Code != 200)
                            throw new HttpException(response);
                        
                    }
                }
                catch (Exception ex)
                {
#if SERIES4
                    Debug.LogError("[{key}] SelectScene: Caught an error dispatching a scene request: {message}{stackTrace}", Key, ex.Message, ex.StackTrace);
#else
                    Debug.Console(
                        1,
                        this,
                        Debug.ErrorLogLevel.Notice,
                        "SelectScene: Caught an error dispatching a scene request: {0}{1}",
                        ex.Message,
                        ex.StackTrace);
#endif
                }
            });

            _levelPoll.Reset(0, 5000);
        }

        public void SetLoadLevel(ushort level)
        {
#if SERIES4
            Debug.LogVerbose("[{key}] Thread ID:{threadId}", Key, Thread.CurrentThread.ManagedThreadId);
#else
            Debug.Console(1, this, "Thread ID :{0}", Thread.CurrentThread.ManagedThreadId);
#endif
            _dispatchQueue.Enqueue(() => DispatchLevelRequest(Url, FixtureName, level, Client));
            _levelPoll.Reset(10, 5000);
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
            using (var response = client.Dispatch(request))
            {
                var responseString = response.ContentString;
#if SERIES4
                Debug.LogVerbose("Dispatched a level request:{level}: Response: {response}", requestedLevel, responseString ?? String.Empty);
#else
                Debug.Console(
                    1,
                    "Dispatched a level request:{0}: Response: {1}",
                    requestedLevel,
                    responseString ?? String.Empty);
#endif
            };
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