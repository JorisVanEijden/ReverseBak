namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// Where a combatant is put when it needs a tile: a search that starts where it already stands, and
/// a fallback sweep that counts the wrong way.
/// </summary>
public class CombatPlacementTests {
    [Fact]
    public void ACorpseStaysPutInAClosedArena() {
        Assert.True(CombatPlacement.LeavesDownedActorInPlace(gridHasExit: false, actorIsDown: true));
        Assert.False(CombatPlacement.LeavesDownedActorInPlace(gridHasExit: true, actorIsDown: true));
        Assert.False(CombatPlacement.LeavesDownedActorInPlace(gridHasExit: false, actorIsDown: false));
    }

    [Fact]
    public void TheSearchResumesFromTheActorsOwnColumnOnItsOwnRow() {
        Assert.Equal(5, CombatPlacement.FirstPassStartColumn(row: 4, actorX: 5, actorY: 4));
        Assert.Equal(0, CombatPlacement.FirstPassStartColumn(row: 3, actorX: 5, actorY: 4));
    }

    [Fact]
    public void TheFirstPassNeverLooksBelowTheActor() {
        Assert.True(CombatPlacement.FirstPassCoversRow(row: 4, actorY: 4));
        Assert.True(CombatPlacement.FirstPassCoversRow(row: 0, actorY: 4));
        Assert.False(CombatPlacement.FirstPassCoversRow(row: 5, actorY: 4));
    }

    [Fact]
    public void AnActorsOwnTileIsNotTreatedAsBlocked() {
        Assert.True(CombatPlacement.TileAccepts(blocked: true, occupiedBySelf: true));
        Assert.True(CombatPlacement.TileAccepts(blocked: false, occupiedBySelf: false));
        Assert.False(CombatPlacement.TileAccepts(blocked: true, occupiedBySelf: false));
    }

    [Fact]
    public void ADisplacedActorLandsAsCloseToWhereItWasAsPossible() {
        // Its own tile is taken by someone else; the next free one along the row wins.
        (int X, int Y)? tile = CombatPlacement.FindTile(actorX: 3, actorY: 6,
            accepts: (x, y) => y == 6 && x >= 5);

        Assert.Equal((5, 6), tile);
    }

    [Fact]
    public void AndWorksUpwardBeforeFallingBackToASweep() {
        // Nothing on or above row 6; the fallback finds a tile below it.
        (int X, int Y)? tile = CombatPlacement.FindTile(actorX: 0, actorY: 6,
            accepts: (x, y) => y == 9 && x == 2);

        Assert.Equal((2, 9), tile);
    }

    [Fact]
    public void OurSweepIsADeliberateDeviation() {
        // The original decrements where it should increment, so its fallback walks off the top of
        // the grid instead of covering the rows below the actor.
        Assert.True(CombatPlacement.FallbackPassIsBroken);
    }

    [Fact]
    public void AFullGridYieldsNothingRatherThanAnOffGridTile() {
        Assert.Null(CombatPlacement.FindTile(actorX: 0, actorY: 0, accepts: (x, y) => false));
    }

    [Fact]
    public void TheSearchStaysInsideTheGrid() {
        (int X, int Y)? tile = CombatPlacement.FindTile(actorX: 7, actorY: 12,
            accepts: (x, y) => true);

        Assert.NotNull(tile);
        Assert.True(CombatGrid.InBounds(tile.Value.X, tile.Value.Y));
    }

    [Fact]
    public void MovingABodyIsNotTheSameOperationAsMovingAFighter() {
        Assert.True(CombatPlacement.DownedActorIsReRegistered(actorIsDown: true));
        Assert.False(CombatPlacement.DownedActorIsReRegistered(actorIsDown: false));
    }
}
