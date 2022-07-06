using System.Collections.Generic;
using PepperDash.Essentials.Core.Lighting;

namespace PoeTexasCorTap
{
    public class LightingGatewayConfig
    {
        public string Url { get; set; }
        public List<LightingSceneConfig> Scenes { get; set; }
        public string FixtureName { get; set; }

        public LightingGatewayConfig()
        {
            Scenes = new List<LightingSceneConfig>();
        }
    }

    public class LightingSceneConfig
    {
        public string Name { get; set; }
        public string Id { get; set; }
    }
}