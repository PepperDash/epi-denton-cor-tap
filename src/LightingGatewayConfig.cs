using System.Collections.Generic;
using PepperDash.Essentials.Core.Lighting;

namespace PoeTexasCorTap
{
    public class LightingGatewayConfig
    {
        public string Url { get; set; }
        public IEnumerable<LightingScene> Scenes { get; set; }
        public string FixtureName { get; set; }

        public LightingGatewayConfig()
        {
            Scenes = new List<LightingScene>();
        }
    }
}