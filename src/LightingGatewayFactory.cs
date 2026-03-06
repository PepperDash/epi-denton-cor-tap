using System.Collections.Generic;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace PoeTexasCorTap
{
    public class LightingGatewayFactory : EssentialsPluginDeviceFactory<LightingGatewayDevice>
    {
        public LightingGatewayFactory()
        {
#if SERIES4
            MinimumEssentialsFrameworkVersion = "2.27.1";
#else
            MinimumEssentialsFrameworkVersion = "1.18.0";
#endif
            TypeNames = new List<string> {"dentoncortap"};
        }
        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            return new LightingGatewayDevice(dc);
        }
    }
}