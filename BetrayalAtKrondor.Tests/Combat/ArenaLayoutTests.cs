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
    public void TheBuildWritesEveryCell() {
        // *** EVERY ARENA CELL USED TO BE OPEN REGARDLESS OF WHAT THE PARTY STOOD IN. *** Nothing
        // wrote to the grid outside CombatGrid's constructor, so a fight in a wood had no trees.
        var grid = new CombatGrid();

        int open = ArenaLayout.Build(grid, (x, y) => x != 3);

        Assert.Equal((CombatGrid.Width - 1) * CombatGrid.Height, open);
        Assert.Equal(CombatTerrain.OutOfBounds, grid.TerrainAt(3, 5));
        Assert.Equal(CombatTerrain.Open, grid.TerrainAt(2, 5));
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
        var openCells = 0;
        ArenaLayout.Build(new CombatGrid(), (x, y) => { openCells++; return true; });

        bool Reaches(int fromX, int fromY, int toX, int toY) =>
            (toY * CombatGrid.Width) + toX < (openCells / 2) + 2;

        ArenaLayout.Build(grid, (x, y) => true, Reaches);

        Assert.Equal(CombatTerrain.Open, grid.TerrainAt(4, 5));
    }

    [Fact]
    public void NoProbeMeansNoPruning() {
        // The build half is useful on its own, and omitting the probe must not silently wall
        // everything off.
        var grid = new CombatGrid();

        int open = ArenaLayout.Build(grid, (x, y) => true);

        Assert.Equal(CombatGrid.Width * CombatGrid.Height, open);
        Assert.Equal(CombatTerrain.Open, grid.TerrainAt(0, 0));
    }
}
