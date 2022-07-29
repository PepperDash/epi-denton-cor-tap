using System;
using PepperDash.Core;
using Crestron.SimplSharp.Net.Http;
using PepperDash.Essentials.Core.Lighting;

namespace PoeTexasCorTap
{
    public static class LightingSceneExt
    {
        public static HttpClientRequest GetRequestForScene(this LightingScene scene, string hostname)
        {
            var request = new HttpClientRequest { RequestType = RequestType.Put };
            request.Header.SetHeaderValue("Content-Type", "application/json");
            request.Url.Parse("http://" + hostname + "/v2/scenes/invoke?name=" + scene.Name);

            Debug.Console(2, "PoeTexasCorTap {0}", new String('-', 80));
            Debug.Console(2, "PoeTexasCorTap:LightingSceneExt:HttpClientRequest: RequestType = {0}", request.RequestType);
            Debug.Console(2, "PoeTexasCorTap:LightingSceneExt:HttpClientRequest: HeaderValue = {0}", request.Header);
            Debug.Console(2, "PoeTexasCorTap:LightingSceneExt:HttpClientRequest: URL = {0}", request.Url.PathAndParams);
            Debug.Console(2, "PoeTexasCorTap {0}", new String('-', 80));

            return request;
        }
    }
}