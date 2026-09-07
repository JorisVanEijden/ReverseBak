namespace BetrayalAtKrondor.Tests.Combat;

using System.Collections.Generic;
using System.Linq;
using global::GameData.Resources.Combat;
using Xunit;

/// <summary>
/// The dungeon arena is 8x7, not 8x13.
/// </summary>
/// <remarks>
/// <c>combatgrid_load_and_init</c> walls off rows 7..12 across all eight columns whenever
/// <c>g_game_mode == 2</c> (CMBTGRID.C:727), and it does so <b>before</b> loading TRAPS.DAT — so the
/// smaller arena applies to a trap puzzle as well, with the puzzle's own cells overwriting it
/// afterwards.
///
/// <para><b>Why this is worth a test rather than a comment.</b> The flag arrives through three
/// separate call sites and two of them shipped as a literal <c>false</c> — the AI's rout check and
/// then the arena itself. The failure is quiet in both directions: a grid twice as deep as it should
/// be still draws, still places, and still plays; it just gives the party six rows of dungeon that
/// do not exist. Asserting the SHAPE rather than the flag is what makes a future hard-coded
/// <c>false</c> fail here instead of in a screenshot.</para>
/// </remarks>
public class UndergroundArenaHeightTests {
    private static IEnumerable<(int X, int Y)> AllCells() {
        for (var x = 0; x < CombatGrid.Width; x++) {
            for (var y = 0; y < CombatGrid.Height; y++) {
                yield return (x, y);
            }
        }
    }

    [Fact]
    public void UndergroundWallsOffEveryRowPastTheSixth() {
        var grid = new CombatGrid(underground: true);

        Assert.All(AllCells().Where(c => c.Y >= CombatGrid.UndergroundPlayableRows),
            c => Assert.Equal(CombatTerrain.OutOfBounds, grid.TerrainAt(c.X, c.Y)));
    }

    /// <summary>Above ground those same rows are ordinary floor.</summary>
    /// <remarks>
    /// The half that catches the opposite mistake — walling the rows off everywhere would pass the
    /// test above on its own.
    /// </remarks>
    [Fact]
    public void AboveGroundTheBackRowsArePlayable() {
        var grid = new CombatGrid();

        Assert.All(AllCells().Where(c => c.Y >= CombatGrid.UndergroundPlayableRows),
            c => Assert.NotEqual(CombatTerrain.OutOfBounds, grid.TerrainAt(c.X, c.Y)));
    }

    /// <summary>The two far corners are walled off in both, which is not the underground rule.</summary>
    /// <remarks>
    /// <c>combatgrid_set_tile_effect(0,0,...)</c> and <c>(7,0,...)</c> run before the
    /// <c>g_game_mode</c> test, so they are unconditional. Pinned here so the corners are not
    /// mistaken for part of the underground walling and moved with it.
    /// </remarks>
    [Fact]
    public void BothArenasWallOffTheTwoFarCorners() {
        foreach (bool underground in new[] { true, false }) {
            var grid = new CombatGrid(underground);

            Assert.Equal(CombatTerrain.OutOfBounds, grid.TerrainAt(0, 0));
            Assert.Equal(CombatTerrain.OutOfBounds, grid.TerrainAt(CombatGrid.Width - 1, 0));
        }
    }

    /// <summary>The drawn grid is shallower underground, because the outline follows the terrain.</summary>
    /// <remarks>
    /// The overlay takes no height of its own — it walks the buffer and stops at
    /// <see cref="CombatGridOutline.BreaksRun"/>. So the arena being 8x7 is the ONLY thing that makes
    /// the dungeon lattice 8x7, and this is what ties the two together.
    /// </remarks>
    [Fact]
    public void TheDrawnGridStopsAtTheWalledRows() {
        List<CombatGridOutline.Edge> below =
            CombatGridOutline.Edges(new CombatGrid(underground: true));
        List<CombatGridOutline.Edge> above = CombatGridOutline.Edges(new CombatGrid());

        Assert.NotEmpty(below);
        Assert.True(below.Count < above.Count,
            $"underground outline should be smaller: {below.Count} vs {above.Count}");
        Assert.All(below, e => {
            Assert.True(e.Y1 < CombatGrid.UndergroundPlayableRows);
            Assert.True(e.Y2 < CombatGrid.UndergroundPlayableRows);
        });
    }
}
