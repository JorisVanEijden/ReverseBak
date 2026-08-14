namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// When a trap puzzle ends. Two traps here: the goal is a row rather than a tile, and the test says
/// "solved" on a grid that has no exit at all.
/// </summary>
public class TrapPuzzleGoalTests {
    private static CombatGrid GridWithExitAt(params (int X, int Y)[] exits) {
        var grid = new CombatGrid();
        foreach ((int x, int y) in exits) {
            grid.SetTerrain(x, y, CombatTerrain.Exit);
        }
        return grid;
    }

    [Fact]
    public void ReachingTheExitsROWEndsItNotTheTile() {
        // Nobody has to stand on the marked cell. Testing the tile would leave puzzles unsolvable
        // wherever the intended path arrives beside the exit rather than on it.
        CombatGrid grid = GridWithExitAt((3, 9));

        Assert.True(TrapPuzzleGoal.PartyIsOut(grid, new[] { 9 }));
        Assert.True(TrapPuzzleGoal.PartyIsOut(grid, new[] { 11 }));
        Assert.False(TrapPuzzleGoal.PartyIsOut(grid, new[] { 8 }));
    }

    [Fact]
    public void OneMemberIsEnough() {
        CombatGrid grid = GridWithExitAt((3, 9));

        Assert.True(TrapPuzzleGoal.PartyIsOut(grid, new[] { 0, 2, 9 }));
    }

    [Fact]
    public void NobodyOutMeansNotSolved() {
        CombatGrid grid = GridWithExitAt((3, 9));

        Assert.False(TrapPuzzleGoal.PartyIsOut(grid, new[] { 0, 4, 8 }));
        Assert.False(TrapPuzzleGoal.PartyIsOut(grid, new int[0]));
    }

    [Fact]
    public void AGridWithNoExitReportsRowZeroAndSoAnyoneOnItIsOut() {
        // Which is why HasExit is a separate question and has to be asked first.
        var grid = new CombatGrid();

        Assert.False(TrapPuzzleGoal.HasExit(grid));
        Assert.Equal(0, TrapPuzzleGoal.ExitRow(grid));
        Assert.True(TrapPuzzleGoal.PartyIsOut(grid, new[] { 0 }));
    }

    [Fact]
    public void HasExitFindsOne() {
        Assert.True(TrapPuzzleGoal.HasExit(GridWithExitAt((0, 12))));
    }

    [Fact]
    public void StaggeredExitsResolveToTheRightmostColumnsRow() {
        // The scan is column-major and each column with an exit overwrites the answer, so it is the
        // last column that wins — not the first found and not the nearest.
        CombatGrid grid = GridWithExitAt((1, 4), (6, 10));

        Assert.Equal(10, TrapPuzzleGoal.ExitRow(grid));
    }

    [Fact]
    public void ExitsOnOneRowAreUnaffectedByThatQuirk() {
        // Which is why the shipped puzzles never notice it.
        CombatGrid grid = GridWithExitAt((0, 11), (3, 11), (7, 11));

        Assert.Equal(11, TrapPuzzleGoal.ExitRow(grid));
    }

    [Fact]
    public void ANullGridIsNotAnError() {
        Assert.False(TrapPuzzleGoal.HasExit(null));
        Assert.Equal(0, TrapPuzzleGoal.ExitRow(null));
        Assert.False(TrapPuzzleGoal.PartyIsOut(null, null));
    }

    [Fact]
    public void ACannonIsOneModelTurnedFourWays() {
        Assert.Equal(90, TrapPuzzleGoal.CannonFacingDegrees(CombatTerrain.CannonWest));
        Assert.Equal(270, TrapPuzzleGoal.CannonFacingDegrees(CombatTerrain.CannonEast));
        Assert.Equal(0, TrapPuzzleGoal.CannonFacingDegrees(CombatTerrain.CannonNorth));
        Assert.Equal(180, TrapPuzzleGoal.CannonFacingDegrees(CombatTerrain.CannonSouth));
    }

    [Fact]
    public void TheOpposingPairsAreHalfATurnApart() {
        Assert.Equal(180,
            TrapPuzzleGoal.CannonFacingDegrees(CombatTerrain.CannonEast)
            - TrapPuzzleGoal.CannonFacingDegrees(CombatTerrain.CannonWest));
        Assert.Equal(180,
            TrapPuzzleGoal.CannonFacingDegrees(CombatTerrain.CannonSouth)
            - TrapPuzzleGoal.CannonFacingDegrees(CombatTerrain.CannonNorth));
    }

    [Fact]
    public void AnExitTileIsWhatMarksAGridAsAPuzzleAtAll() {
        // The round transition asks exactly this to decide whether to play the completion burn, so
        // the same predicate answers "is this a puzzle" and "does the goal test mean anything".
        Assert.True(TrapPuzzleGoal.IsTrapPuzzle(GridWithExitAt((2, 8))));
        Assert.False(TrapPuzzleGoal.IsTrapPuzzle(new CombatGrid()));
    }

    [Fact]
    public void OnlyOccupiedTilesTakeLightWhenItEnds() {
        // The fire marks out what was standing on the grid rather than sweeping it.
        Assert.True(TrapPuzzleGoal.BurnsAtCompletion(tileHoldsCombatant: true));
        Assert.False(TrapPuzzleGoal.BurnsAtCompletion(tileHoldsCombatant: false));
    }

    [Fact]
    public void TheSequenceRunsUntilTheLastFireGoesOut() {
        // Durations are rolled per tile, so it lasts as long as its longest fire, not a fixed time.
        Assert.False(TrapPuzzleGoal.BurnComplete(1));
        Assert.True(TrapPuzzleGoal.BurnComplete(0));
        Assert.True(TrapPuzzleGoal.MaximumBurnDuration > TrapPuzzleGoal.MinimumBurnDuration);
    }

    [Fact]
    public void TheBurnIsTheSameHazardTheRisenLeaveBehind() {
        // One effect used by two systems, not two that happen to share a number.
        Assert.Equal(SlayerRevival.RisenTileEffect, TrapPuzzleGoal.BurningTerrain);
    }

    [Fact]
    public void CannonsNeverFire() {
        // Their facing is read in three places — loader, writer, renderer — and nowhere to act.
        // Recorded because the absence is easy to mistake for a gap.
        Assert.False(TrapPuzzleGoal.CannonsFire);
    }

    [Fact]
    public void OnlyTheFourCannonTerrainsAreCannons() {
        Assert.True(TrapPuzzleGoal.IsCannon(CombatTerrain.CannonWest));
        Assert.True(TrapPuzzleGoal.IsCannon(CombatTerrain.CannonSouth));
        Assert.False(TrapPuzzleGoal.IsCannon(CombatTerrain.Crystal));
        Assert.False(TrapPuzzleGoal.IsCannon(CombatTerrain.Exit));
    }
}
