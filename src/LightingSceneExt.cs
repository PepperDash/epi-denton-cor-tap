using Crestron.SimplSharp.Net.Http;
using Newtonsoft.Json;
using PepperDash.Essentials.Core.Lighting;

namespace PoeTexasCorTap
{
    public static class LightingSceneExt
    {
        public static HttpClientRequest GetRequestForScene(this LightingScene scene, string url)
        {
            var request = new HttpClientRequest { RequestType = RequestType.Put, ContentString = scene.GetBodyForSceneRecall()};
            request.Header.SetHeaderValue("Content-Type", "application/json");
            request.Url.Parse("http://" + url + "/v2/scenes");
            return request;
        }

        public static string GetBodyForSceneRecall(this LightingScene scene)
        {
            var body = new {name = scene.Name, action = "invoke"};
            return JsonConvert.SerializeObject(body);
        }
    }
}