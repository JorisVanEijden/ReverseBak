namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// The arena's cell-to-world mapping and its inverse.
/// </summary>
/// <remarks>
/// The inverse is what a click on empty arena ground needs: every pick the arena has today is an
/// object pick (combatant, corpse, entity), so a click that hits none of them does nothing, and both
/// summon placement and player combat movement are blocked on that. See TASK-112.
/// </remarks>
public class CombatArenaPlacementTests {
    private const int Cell = 300;   // StartData.CombatGridCellSize as shipped

    [Fact]
    public void EveryCellsOwnCentreMapsBackToIt() {
        // *** THE ROUND TRIP IS THE SPECIFICATION. *** If these two ever disagree, a summon lands on
        // a different tile from the one the player clicked, and it does so silently.
        for (var row = 0; row < CombatGrid.Height; row++) {
            for (var column = 0; column < CombatGrid.Width; column++) {
                (int across, int away) = CombatArenaPlacement.CellOffset(column, row, Cell);
                Assert.Equal((column, row), CombatArenaPlacement.CellAt(across, away, Cell));
            }
        }
    }

    [Fact]
    public void APointAnywhereInsideACellResolvesToThatCell() {
        // Not just the centre: the corners of the cell's own footprint, one unit inside each edge.
        (int across, int away) = CombatArenaPlacement.CellOffset(3, 5, Cell);
        int half = Cell / 2;

        Assert.Equal((3, 5), CombatArenaPlacement.CellAt(across - half + 1, away - half + 1, Cell));
        Assert.Equal((3, 5), CombatArenaPlacement.CellAt(across + half - 1, away + half - 1, Cell));
    }

    [Fact]
    public void TheBoundaryBELONGSToTheFartherCell_soNoPointFallsInTwo() {
        // Exactly on the seam between two columns. Flooring puts it in the higher one; the point one
        // unit before it must be in the lower. A gap or an overlap here is a pick that lands on the
        // wrong tile near every edge.
        (int across, int away) = CombatArenaPlacement.CellOffset(3, 5, Cell);
        int seam = across + (Cell / 2);

        Assert.Equal((4, 5), CombatArenaPlacement.CellAt(seam, away, Cell));
        Assert.Equal((3, 5), CombatArenaPlacement.CellAt(seam - 1, away, Cell));
    }

    [Fact]
    public void ItFLOORSRatherThanTruncating_whichOnlyShowsOnTheLeftHalf() {
        // *** THE ARENA IS CENTRED ON THE PARTY, SO HALF OF IT IS NEGATIVE ACROSS. *** C# integer
        // division truncates toward zero, which would fold the cell either side of the centre line
        // onto the same column. Columns 3 and 4 straddle it with the shipped width of 8.
        (int leftAcross, int away) = CombatArenaPlacement.CellOffset(3, 0, Cell);
        (int rightAcross, _) = CombatArenaPlacement.CellOffset(4, 0, Cell);

        Assert.True(leftAcross < 0, "column 3 sits left of the party's line");
        Assert.True(rightAcross > 0, "and column 4 right of it");
        Assert.Equal((3, 0), CombatArenaPlacement.CellAt(leftAcross, away, Cell));
        Assert.Equal((4, 0), CombatArenaPlacement.CellAt(rightAcross, away, Cell));
    }

    [Fact]
    public void APointOutsideTheGridIsNULLRatherThanClamped() {
        // A miss must read as a miss. Clamping would make a click past the arena's edge place a
        // summon on the edge tile, which is a different and worse behaviour than doing nothing.
        (int across, int away) = CombatArenaPlacement.CellOffset(0, 0, Cell);

        Assert.Null(CombatArenaPlacement.CellAt(across - Cell, away, Cell));
        Assert.Null(CombatArenaPlacement.CellAt(across, away - Cell, Cell));

        (int farAcross, int farAway) =
            CombatArenaPlacement.CellOffset(CombatGrid.Width - 1, CombatGrid.Height - 1, Cell);
        Assert.Null(CombatArenaPlacement.CellAt(farAcross + Cell, farAway, Cell));
        Assert.Null(CombatArenaPlacement.CellAt(farAcross, farAway + Cell, Cell));
    }

    [Fact]
    public void ACellSizeOfZeroIsRefusedRatherThanDividingByIt() {
        // HotspotService already guards `cellSize <= 0` before placing actors, because StartData
        // may not have loaded. The inverse is reached from input handling, where the same is true.
        Assert.Null(CombatArenaPlacement.CellAt(0, 0, 0));
        Assert.Null(CombatArenaPlacement.CellAt(0, 0, -1));
    }

    [Fact]
    public void BoundsAreTheGRIDsNotTheFIGHTs() {
        // CellAt answers only "is this on the board". An underground fight plays on fewer rows, and
        // that is IsPlayable's question — keeping them apart means a pick does not silently depend
        // on which kind of fight it is.
        (int across, int away) =
            CombatArenaPlacement.CellOffset(0, CombatGrid.UndergroundPlayableRows, Cell);

        Assert.Equal((0, CombatGrid.UndergroundPlayableRows),
            CombatArenaPlacement.CellAt(across, away, Cell));
        Assert.False(CombatArenaPlacement.IsPlayable(0, CombatGrid.UndergroundPlayableRows, underground: true));
        Assert.True(CombatArenaPlacement.IsPlayable(0, CombatGrid.UndergroundPlayableRows, underground: false));
    }
}
