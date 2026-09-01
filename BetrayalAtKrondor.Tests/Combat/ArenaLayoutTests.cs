namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// Laying the world into the arena grid, and pruning what it isolates.
/// </summary>
public class ArenaLayoutTests {
    // Everything reachable from everything — isolates the build half from the prune.
    private static bool AlwaysReaches(int fromX, int fromY, int toX, int toY) => true;

    [Fact]
    public void TheBuildWritesEveryUNBLOCKEDCell() {
        // *** EVERY ARENA CELL USED TO BE OPEN REGARDLESS OF WHAT THE PARTY STOOD IN. *** Nothing
        // wrote to the grid outside CombatGrid's constructor, so a fight in a wood had no trees.
        var grid = new CombatGrid();

        int open = ArenaLayout.Build(grid, (x, y) => x != 3);

        Assert.Equal(CombatTerrain.OutOfBounds, grid.TerrainAt(3, 5));
        Assert.Equal(CombatTerrain.Open, grid.TerrainAt(2, 5));
        // The two far corners were already walled and stay that way — the count excludes them.
        Assert.Equal(((CombatGrid.Width - 1) * CombatGrid.Height) - 2, open);
    }

    [Fact]
    public void ItNeverWIDENSTheArena() {
        // *** ONLY EVER ADDS BLOCKING. *** The corners are laid down by the grid's construction
        // (the original's Load_grid) and painting open ground over them would make the arena bigger
        // than the game's. Found by a play-verify: with the first version every fight came back
        // 104 cells open, corners included.
        var grid = new CombatGrid();

        ArenaLayout.Build(grid, (x, y) => true);

        Assert.Equal(CombatTerrain.OutOfBounds, grid.TerrainAt(0, 0));
        Assert.Equal(CombatTerrain.OutOfBounds, grid.TerrainAt(CombatGrid.Width - 1, 0));
    }

    [Fact]
    public void APocketIsPrunedWhenItCannotReachMostOfTheGrid() {
        // The third pass. Column 0 is open but cut off; everything else is one region. A cell in
        // the pocket reaches almost nothing, so it is walled off.
        var grid = new CombatGrid();
        bool Reaches(int fromX, int fromY, int toX, int toY) => (fromX == 0) == (toX == 0);

        ArenaLayout.Build(grid, (x, y) => x != 1, Reaches);

        Assert.Equal(CombatTerrain.OutOfBounds, grid.TerrainAt(0, 5));
        Assert.Equal(CombatTerrain.Open, grid.TerrainAt(4, 5));
    }

    [Fact]
    public void AMajorityIsEnoughToSurvive() {
        // *** A SIMPLE MAJORITY, NOT FULL CONNECTIVITY. *** Reaching just over half is enough, so a
        // cell that cannot see a corner of the grid still stays. Demanding a fully connected region
        // would wall off cells the game keeps.
        var grid = new CombatGrid();

        // Reaches only the left five of eight columns — about 62% of the grid, a clear majority and
        // nowhere near fully connected.
        bool Reaches(int fromX, int fromY, int toX, int toY) => toX < 5;

        ArenaLayout.Build(grid, (x, y) => true, Reaches);

        Assert.Equal(CombatTerrain.Open, grid.TerrainAt(4, 5));
        Assert.Equal(CombatTerrain.Open, grid.TerrainAt(7, 5));
    }

    [Fact]
    public void NoProbeMeansNoPruning() {
        // The build half is useful on its own, and omitting the probe must not silently wall
        // everything off.
        var grid = new CombatGrid();

        int open = ArenaLayout.Build(grid, (x, y) => true);

        Assert.Equal((CombatGrid.Width * CombatGrid.Height) - 2, open);
        Assert.Equal(CombatTerrain.Open, grid.TerrainAt(4, 5));
    }
}
