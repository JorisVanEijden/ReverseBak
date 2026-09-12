namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;

using Xunit;

/// <summary>
/// The view zoom is a per-ZONE field, and the dungeons do not share the overworld's — TASK-441.
/// </summary>
/// <remarks>
/// <c>zone_load</c> (canassa <c>SRC/R3D/SCENE/ZONE.C:67-89</c>) re-reads it out of
/// <c>Z##DEF.DAT</c> on every zone change, into the first two bytes of <c>g_world_widget</c>. The
/// port read <see cref="WorldProjection.TravelProjectionShift"/> everywhere, so Mac Mordain Cadal
/// rendered at twice the original's scale — measured from the same save on both sides, the corridor
/// wall stood 300 canonical units tall against the original's 150.
/// </remarks>
public class ZoneViewZoomTests {
    /// <summary>The travel view's VGA rectangle is 294x101.</summary>
    private const int TravelViewHeightVga = 101;

    [Fact]
    public void ADungeonsZoomIsOneBitLessThanTheOverworlds() {
        // The shipped values, from generated/DAT: zones 1-9 carry 9, zones 10-12 carry 8.
        var overworld = new ZoneDefinition("Z01DEF.DAT") { ZonePointer = 9 };
        var dungeon = new ZoneDefinition("Z10DEF.DAT") { ZonePointer = 8 };

        Assert.Equal(9, overworld.ViewZoomShift);
        Assert.Equal(8, dungeon.ViewZoomShift);
    }

    [Fact]
    public void OneBitOfShiftIsAFactorOfTWOOnScreen() {
        // Which is the whole point: a dungeon camera at the same height sees twice the ground, so
        // reading the overworld constant there magnifies everything by two.
        double over = WorldProjection.VerticalFovDegrees(TravelViewHeightVga, 9);
        double dung = WorldProjection.VerticalFovDegrees(TravelViewHeightVga, 8);

        double ratio = System.Math.Tan(dung * System.Math.PI / 360.0)
            / System.Math.Tan(over * System.Math.PI / 360.0);

        Assert.Equal(2.0, ratio, 6);
    }

    [Fact]
    public void TheOverworldZoneStillAgreesWithTheTravelConstant() {
        // So the fix cannot quietly move the overworld while correcting the dungeons.
        var overworld = new ZoneDefinition("Z01DEF.DAT") { ZonePointer = 9 };

        Assert.Equal(WorldProjection.TravelProjectionShift, overworld.ViewZoomShift);
    }
}
