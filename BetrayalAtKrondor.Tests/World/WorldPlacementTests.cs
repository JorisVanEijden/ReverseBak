namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>
/// czone_world_pos_from_tile and its centred variant. The centring is the part that is easy to drop
/// and hard to notice.
/// </summary>
public class WorldPlacementTests {
    [Fact]
    public void ATileIsFortySubCellsAcross() {
        Assert.Equal(64000, WorldPlacement.TileSize);
        Assert.Equal(1600, WorldPlacement.SubCellSize);
        Assert.Equal(40, WorldPlacement.SubCellsPerTile);
    }

    [Fact]
    public void TheCornerIsTilesPlusSubCells() {
        Assert.Equal(0, WorldPlacement.CornerOf(0, 0));
        Assert.Equal(64000, WorldPlacement.CornerOf(1, 0));
        Assert.Equal(1600, WorldPlacement.CornerOf(0, 1));
        Assert.Equal(64000 + (11 * 1600), WorldPlacement.CornerOf(1, 11));
    }

    [Fact]
    public void ASpawnLandsInTheMiddleOfItsSubCell() {
        // Dropping the centring would put every arrival 800 units north-west of where it belongs —
        // an eighth of a tile.
        Assert.Equal(800, WorldPlacement.CentreOf(0, 0));
        Assert.Equal(WorldPlacement.CornerOf(3, 7) + 800, WorldPlacement.CentreOf(3, 7));
    }

    [Fact]
    public void TheCentreIsHalfASubCell() {
        Assert.Equal(WorldPlacement.SubCellSize / 2, WorldPlacement.SubCellCentre);
    }

    [Fact]
    public void AWorldCoordinateDecomposesBackToItsTileAndCell() {
        long world = WorldPlacement.CentreOf(5, 23);

        Assert.Equal(5, WorldPlacement.TileOf(world));
        Assert.Equal(23, WorldPlacement.SubCellOf(world));
    }

    [Fact]
    public void TheRoundTripHoldsAcrossAWholeTile() {
        for (var cell = 0; cell < WorldPlacement.SubCellsPerTile; cell++) {
            long world = WorldPlacement.CentreOf(2, cell);
            Assert.Equal(2, WorldPlacement.TileOf(world));
            Assert.Equal(cell, WorldPlacement.SubCellOf(world));
        }
    }

    [Fact]
    public void TheLastSubCellStaysInsideItsOwnTile() {
        // Centring the final cell must not spill into the next tile.
        long world = WorldPlacement.CentreOf(1, WorldPlacement.SubCellsPerTile - 1);

        Assert.Equal(1, WorldPlacement.TileOf(world));
        Assert.True(world < WorldPlacement.CornerOf(2, 0));
    }

    [Fact]
    public void AChapterStartPositionMatchesTheDocumentedFormula() {
        // ChapterStartData describes tile*64000 + offset*1600 + 800; this is that, once.
        const int tile = 11, offset = 33;

        Assert.Equal((tile * 64000L) + (offset * 1600L) + 800L,
            WorldPlacement.CentreOf(tile, offset));
    }
}
