namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// The arena's reach, which is what a renderer clears the ground with.
/// </summary>
public class CombatArenaPlacementReachTests {
    [Fact]
    public void Reach_CoversEveryCellTheArenaCanPlaceAnActorIn() {
        const int cellSize = 300;

        int reach = CombatArenaPlacement.Reach(cellSize);

        // Every cell of the grid must be inside it, or scenery standing on that cell survives the
        // cull and can still come between the camera and whoever is on it.
        for (var column = 0; column < CombatGrid.Width; column++) {
            for (var row = 0; row < CombatGrid.Height; row++) {
                (int across, int away) = CombatArenaPlacement.CellOffset(column, row, cellSize);
                double distance = Math.Sqrt(((double)across * across) + ((double)away * away));
                Assert.True(distance <= reach,
                    $"cell ({column},{row}) is {distance:F0} away, reach is {reach}");
            }
        }
    }

    [Fact]
    public void Reach_ScalesWithTheCellSize() {
        // It is derived from CellOffset, not stated, so a different START.DAT moves it.
        Assert.True(CombatArenaPlacement.Reach(600) > CombatArenaPlacement.Reach(300));
    }
}
