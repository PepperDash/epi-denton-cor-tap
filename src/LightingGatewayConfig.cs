using System.Collections.Generic;

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
}