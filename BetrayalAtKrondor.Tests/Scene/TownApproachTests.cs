namespace BetrayalAtKrondor.Tests.Scene;

using GameData.Resources.Scene;
using GameData.Resources.World;
using Xunit;

public class TownApproachTests {
    [Fact]
    public void TheOffsetIsTwoBytesInOneField() {
        // 5136 = 0x1410 -> X 0x10, Y 0x14. Reading it as one number lands nowhere.
        Assert.Equal(0x10, TownApproach.SubTileX(5136));
        Assert.Equal(0x14, TownApproach.SubTileY(5136));
    }

    [Fact]
    public void TheDestinationIsRelativeToTheTileThePartyIsStandingOn() {
        (long ax, long ay) = TownApproach.DestinationOf(10, 20, 5136);
        (long bx, long by) = TownApproach.DestinationOf(11, 20, 5136);

        Assert.Equal(WorldPlacement.CentreOf(10, 0x10), ax);
        Assert.Equal(WorldPlacement.CentreOf(20, 0x14), ay);
        // Same record, next tile east: one tile further, same sub-cell.
        Assert.Equal(ax + WorldPlacement.TileSize, bx);
        Assert.Equal(ay, by);
    }

    [Fact]
    public void TheDestinationIsCentredInItsSubCellNotOnTheCorner() {
        (long x, long _) = TownApproach.DestinationOf(0, 0, 0x0101);

        Assert.Equal(WorldPlacement.SubCellSize + WorldPlacement.SubCellCentre, x);
    }

    [Fact]
    public void UndergroundHalvesTheStepButNotTheDestination() {
        Assert.Equal(400, TownApproach.StepFor(400, underground: false));
        Assert.Equal(200, TownApproach.StepFor(400, underground: true));
    }
}
