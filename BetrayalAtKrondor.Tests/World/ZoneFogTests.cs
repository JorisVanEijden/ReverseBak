namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;

using Xunit;

public class ZoneFogTests {
    [Fact]
    public void TheHazeSpansTheSpriteRemapsOwnRange() {
        var outdoors = new ZoneDefinition("Z01DEF.DAT") {
            ZoneLocation = 0, RmpResourceCount = 8, SpriteFogDivisor = 1500, SpriteFogNearDistance = 13000,
        };
        var underground = new ZoneDefinition("Z10DEF.DAT") {
            ZoneLocation = ZoneDefinition.UndergroundZoneLocation, RmpResourceCount = 8,
            SpriteFogDivisor = -1, SpriteFogNearDistance = 3000,
        };

        Assert.Equal((13000u, 25000u), ZoneFog.Range(outdoors));
        Assert.Equal((3000u, 6400u), ZoneFog.Range(underground));
    }
}
