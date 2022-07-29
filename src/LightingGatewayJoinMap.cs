using PepperDash.Essentials.Core;

namespace PoeTexasCorTap
{
    public class LightingGatewayJoinMap : JoinMapBaseAdvanced
    {
        [JoinName("RampFixture")] public JoinDataComplete RampFixture =
            new JoinDataComplete(
                new JoinData {JoinNumber = 1, JoinSpan = 1},
                new JoinMetadata
                {
                    Description = "Lighting Controller Ramp Fixture",
                    JoinCapabilities = eJoinCapabilities.FromSIMPL,
                    JoinType = eJoinType.Analog
                });

        [JoinName("FixtureName")]
        public JoinDataComplete FixtureName =
            new JoinDataComplete(
            new JoinData { JoinNumber = 2, JoinSpan = 1 },
            new JoinMetadata
            {
                Description = "Lighting Fixture Name",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        public LightingGatewayJoinMap(uint joinStart)
            : base(joinStart, typeof (LightingGatewayJoinMap))
        {
        }
    }
}