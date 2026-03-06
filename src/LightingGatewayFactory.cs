using System.Collections.Generic;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace PoeTexasCorTap
{
    public class LightingGatewayFactory : EssentialsPluginDeviceFactory<LightingGatewayDevice>
    {
        public LightingGatewayFactory()
        {
            MinimumEssentialsFrameworkVersion = "2.27.1";
            TypeNames = new List<string> {"dentoncortap"};
        }
        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            return new LightingGatewayDevice(dc);
        }
    }
}