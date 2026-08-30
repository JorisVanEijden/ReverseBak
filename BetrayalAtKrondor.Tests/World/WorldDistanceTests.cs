namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.Location;
using GameData.Resources.World;
using Xunit;

/// <summary>
/// <c>distdir_octagonal_distance_dxdy</c> (SRC/R3D/CORE/DISTDIR.ASM) — the engine's one distance
/// approximation, shared by the combat grid, actor spawning, the proximity scan, projectile hit
/// testing and world sprite rendering.
/// </summary>
public class WorldDistanceTests {
    [Fact]
    public void AStraightLineIsItsOwnLength() {
        // min is 0, so the 3/8 term contributes nothing.
        Assert.Equal(100, WorldDistance.Octagonal(100, 0));
        Assert.Equal(100, WorldDistance.Octagonal(0, 100));
    }

    [Fact]
    public void ADiagonalCostsAboutOnePointThreeSevenFive_NotTheTrueHypotenuse() {
        // *** THE POINT OF THE APPROXIMATION. *** 1.375x against the true 1.414x, so diagonals come
        // out slightly short. Substituting a hypotenuse moves every threshold in the game that
        // compares against this number at once.
        Assert.Equal(137, WorldDistance.Octagonal(100, 100));
        Assert.NotEqual(141, WorldDistance.Octagonal(100, 100));
    }

    [Fact]
    public void SignDoesNotMatter_BothDeltasAreTakenAbsoluteFirst() {
        int expected = WorldDistance.Octagonal(30, 40);
        Assert.Equal(expected, WorldDistance.Octagonal(-30, 40));
        Assert.Equal(expected, WorldDistance.Octagonal(30, -40));
        Assert.Equal(expected, WorldDistance.Octagonal(-30, -40));
    }

    [Fact]
    public void TheLARGERDeltaIsTheBaseAndTheSmallerIsScaled() {
        // Swapping the arguments must not change the answer — the routine sorts them into max/min
        // before it does anything. Taking dx as the base unconditionally would make the result
        // depend on argument order.
        Assert.Equal(WorldDistance.Octagonal(40, 30), WorldDistance.Octagonal(30, 40));
        Assert.Equal(40 + (30 * 3 / 8), WorldDistance.Octagonal(30, 40));
    }

    [Fact]
    public void ZeroDistanceIsZero() {
        Assert.Equal(0, WorldDistance.Octagonal(0, 0));
    }

    [Fact]
    public void TheTwoPointFormIsTheSameMeasurement() {
        Assert.Equal(WorldDistance.Octagonal(10 - 40, 20 - 60),
            WorldDistance.Between(10, 20, 40, 60));
    }

    [Fact]
    public void TheTeleportFareStillMeasuresTheSameWay() {
        // TeleportCost was the only copy for a while, which made a general R3D core routine look
        // like a pricing detail. It delegates now; this pins that the fare did not move.
        Assert.Equal(WorldDistance.Octagonal(30, 40), TeleportCost.OctagonalDistance(30, 40));
        Assert.Equal(WorldDistance.Octagonal(-7, 3), TeleportCost.OctagonalDistance(-7, 3));
    }
}
