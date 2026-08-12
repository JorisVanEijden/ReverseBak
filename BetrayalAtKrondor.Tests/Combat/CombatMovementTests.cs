namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// Combat-grid bounds, blocking and movement (CMBTGRID.C / CMBTAI.C). The cases below pin the things
/// that make this feel like the original: no path search, a single tile of wall-sliding whose
/// fallback differs for diagonal and orthogonal steps, and crystals you may walk onto but not squeeze
/// between.
/// </summary>
public class CombatMovementTests {
    // ---- grid ----------------------------------------------------------------------------

    [Fact]
    public void TheArenaIsEightByThirteenWithTheTwoFarCornersWalledOff() {
        var grid = new CombatGrid();

        Assert.True(CombatGrid.InBounds(0, 0));
        Assert.True(CombatGrid.InBounds(7, 12));
        Assert.False(CombatGrid.InBounds(8, 0));
        Assert.False(CombatGrid.InBounds(0, 13));
        Assert.True(grid.IsBlocked(0, 0));
        Assert.True(grid.IsBlocked(7, 0));
        Assert.False(grid.IsBlocked(3, 0));
    }

    [Fact]
    public void UndergroundWallsOffTheBackSixRowsLeavingAnEightBySevenArena() {
        var grid = new CombatGrid(underground: true);

        Assert.False(grid.IsBlocked(3, 6));
        Assert.True(grid.IsBlocked(3, 7));
        Assert.True(grid.IsBlocked(3, 12));
    }

    [Fact]
    public void AbovegroundKeepsTheRowsUndergroundLosesSoTheTwoArenasDiffer() {
        Assert.False(new CombatGrid().IsBlocked(3, 10));
        Assert.True(new CombatGrid(underground: true).IsBlocked(3, 10));
    }

    [Fact]
    public void ReadingOffTheGridReportsOutOfBoundsRatherThanThrowing() {
        var grid = new CombatGrid();

        Assert.Equal(CombatTerrain.OutOfBounds, grid.TerrainAt(-1, 5));
        Assert.Equal(CombatTerrain.OutOfBounds, grid.TerrainAt(99, 99));
        Assert.True(grid.IsBlocked(-1, 5));
    }

    [Fact]
    public void WallsPushablesAndOccupantsBlockButCrystalsAndTrapsDoNot() {
        var grid = new CombatGrid();
        grid.SetTerrain(1, 1, CombatTerrain.Wall);
        grid.SetTerrain(2, 1, CombatTerrain.Pushable);
        grid.SetTerrain(3, 1, CombatTerrain.Crystal);
        grid.SetTerrain(4, 1, CombatTerrain.Trap);
        grid.SetOccupied(5, 1, true);

        Assert.True(grid.IsBlocked(1, 1));
        Assert.True(grid.IsBlocked(2, 1));
        Assert.True(grid.IsBlocked(5, 1));
        // You walk onto these — that is how they fire.
        Assert.False(grid.IsBlocked(3, 1));
        Assert.False(grid.IsBlocked(4, 1));
    }

    [Fact]
    public void DistanceIsChebyshevSoADiagonalCostsNoMoreThanAStraightStep() {
        Assert.Equal(3, CombatGrid.ChebyshevDistance(0, 0, 3, 3));
        Assert.Equal(3, CombatGrid.ChebyshevDistance(0, 0, 3, 0));
        Assert.Equal(0, CombatGrid.ChebyshevDistance(4, 4, 4, 4));
    }

    // ---- stepping ------------------------------------------------------------------------

    [Fact]
    public void StandingOnTheDestinationIsSuccessWithNothingToDo() {
        StepResult step = CombatMovement.Step(new CombatGrid(), 3, 3, 3, 3);

        Assert.Equal(StepStatus.AlreadyThere, step.Status);
        Assert.True(step.Succeeded);
    }

    [Fact]
    public void ADestinationOffTheGridIsRefusedOutright() {
        StepResult step = CombatMovement.Step(new CombatGrid(), 3, 3, 99, 3);

        Assert.Equal(StepStatus.TargetOffGrid, step.Status);
        Assert.False(step.Succeeded);
    }

    [Fact]
    public void MovementIsOneTileAtATimeTowardTheDestination() {
        StepResult step = CombatMovement.Step(new CombatGrid(), 3, 5, 3, 11);

        Assert.Equal(StepStatus.Moved, step.Status);
        Assert.Equal(3, step.X);
        Assert.Equal(6, step.Y);
    }

    [Fact]
    public void ADiagonalDestinationIsApproachedDiagonally() {
        StepResult step = CombatMovement.Step(new CombatGrid(), 2, 2, 6, 6);

        Assert.Equal(StepStatus.Moved, step.Status);
        Assert.Equal(3, step.X);
        Assert.Equal(3, step.Y);
    }

    [Fact]
    public void ABlockedDiagonalDropsToTheVerticalStepFirst() {
        var grid = new CombatGrid();
        grid.SetTerrain(3, 3, CombatTerrain.Wall);

        StepResult step = CombatMovement.Step(grid, 2, 2, 6, 6);

        Assert.Equal(StepStatus.Moved, step.Status);
        Assert.Equal(2, step.X);
        Assert.Equal(3, step.Y);
    }

    [Fact]
    public void AndFallsBackToTheHorizontalStepWhenTheVerticalIsAlsoBlocked() {
        var grid = new CombatGrid();
        grid.SetTerrain(3, 3, CombatTerrain.Wall);
        grid.SetTerrain(2, 3, CombatTerrain.Wall);

        StepResult step = CombatMovement.Step(grid, 2, 2, 6, 6);

        Assert.Equal(StepStatus.Moved, step.Status);
        Assert.Equal(3, step.X);
        Assert.Equal(2, step.Y);
    }

    [Fact]
    public void ABlockedStraightStepSlidesDiagonallyPastTheObstacleInstead() {
        // Heading east from (3,5) into a wall at (4,5): the fallbacks are the diagonals beyond it,
        // not the tiles beside the actor.
        var grid = new CombatGrid();
        grid.SetTerrain(4, 5, CombatTerrain.Wall);

        StepResult step = CombatMovement.Step(grid, 3, 5, 7, 5);

        Assert.Equal(StepStatus.Moved, step.Status);
        Assert.Equal(4, step.X);
        Assert.Equal(6, step.Y);
    }

    [Fact]
    public void TheStraightStepTriesTheOtherDiagonalWhenTheFirstIsBlockedToo() {
        var grid = new CombatGrid();
        grid.SetTerrain(4, 5, CombatTerrain.Wall);
        grid.SetTerrain(4, 6, CombatTerrain.Wall);

        StepResult step = CombatMovement.Step(grid, 3, 5, 7, 5);

        Assert.Equal(StepStatus.Moved, step.Status);
        Assert.Equal(4, step.X);
        Assert.Equal(4, step.Y);
    }

    [Fact]
    public void AnActorBoxedInStaysExactlyWhereItWas() {
        var grid = new CombatGrid();
        grid.SetTerrain(4, 5, CombatTerrain.Wall);
        grid.SetTerrain(4, 6, CombatTerrain.Wall);
        grid.SetTerrain(4, 4, CombatTerrain.Wall);

        StepResult step = CombatMovement.Step(grid, 3, 5, 7, 5);

        Assert.Equal(StepStatus.Blocked, step.Status);
        Assert.False(step.Succeeded);
        Assert.Equal(3, step.X);
        Assert.Equal(5, step.Y);
    }

    [Fact]
    public void ThereIsNoPathSearchSoAConcavePocketStallsTheActorCompletely() {
        // A three-sided pocket opening away from the target. Any pathfinder would walk out around it;
        // the original cannot, and monsters getting stuck on scenery is part of the balance.
        var grid = new CombatGrid();
        grid.SetTerrain(4, 4, CombatTerrain.Wall);
        grid.SetTerrain(4, 5, CombatTerrain.Wall);
        grid.SetTerrain(4, 6, CombatTerrain.Wall);

        StepResult step = CombatMovement.Step(grid, 3, 5, 7, 5);

        Assert.Equal(StepStatus.Blocked, step.Status);
        Assert.Equal(3, step.X);
        Assert.Equal(5, step.Y);
    }

    [Fact]
    public void AnOccupiedTileBlocksJustLikeAWallSoTheActorSlidesAroundIt() {
        var grid = new CombatGrid();
        grid.SetOccupied(3, 6, true);

        StepResult step = CombatMovement.Step(grid, 3, 5, 3, 11);

        Assert.Equal(StepStatus.Moved, step.Status);
        Assert.False(step.X == 3 && step.Y == 6);
        Assert.Equal(4, step.X); // slid diagonally past the occupant, still gaining a row
        Assert.Equal(6, step.Y);
    }

    // ---- pushables and crystals ----------------------------------------------------------

    [Fact]
    public void APushableElementIsReportedSoTheCallerCanTryToShoveIt() {
        var grid = new CombatGrid();
        grid.SetTerrain(3, 6, CombatTerrain.Pushable);

        StepResult step = CombatMovement.Step(grid, 3, 5, 3, 11, adjacentToTarget: true);

        Assert.Equal(StepStatus.BlockedByPushable, step.Status);
        Assert.Equal(3, step.X);
        Assert.Equal(6, step.Y);
    }

    [Fact]
    public void APushableIsOnlyShovedWhileAdjacentToTheTargetOtherwiseItIsJustAnObstacle() {
        var grid = new CombatGrid();
        grid.SetTerrain(3, 6, CombatTerrain.Pushable);

        StepResult step = CombatMovement.Step(grid, 3, 5, 3, 11, adjacentToTarget: false);

        Assert.NotEqual(StepStatus.BlockedByPushable, step.Status);
    }

    [Fact]
    public void AProbeStepsPastAPushableRatherThanShovingIt() {
        var grid = new CombatGrid();
        grid.SetTerrain(3, 6, CombatTerrain.Pushable);

        StepResult step = CombatMovement.Step(grid, 3, 5, 3, 11, adjacentToTarget: true, probe: true);

        Assert.True(step.Succeeded);
        Assert.NotEqual(StepStatus.BlockedByPushable, step.Status);
    }

    [Fact]
    public void YouMayWalkOntoASingleCrystal() {
        var grid = new CombatGrid();
        grid.SetTerrain(3, 6, CombatTerrain.Crystal);

        StepResult step = CombatMovement.Step(grid, 3, 5, 3, 11);

        Assert.Equal(StepStatus.Moved, step.Status);
        Assert.Equal(3, step.X);
        Assert.Equal(6, step.Y);
    }

    [Fact]
    public void ButYouMayNotSqueezeDiagonallyBetweenTwoOfThem() {
        // Both orthogonal neighbours of the diagonal are crystals, so the diagonal is dropped to an
        // orthogonal step rather than slipping through the gap.
        var grid = new CombatGrid();
        grid.SetTerrain(3, 2, CombatTerrain.Crystal);
        grid.SetTerrain(2, 3, CombatTerrain.Crystal);

        StepResult step = CombatMovement.Step(grid, 2, 2, 6, 6);

        Assert.Equal(StepStatus.Moved, step.Status);
        Assert.False(step.X == 3 && step.Y == 3);
        Assert.Equal(3, step.X);
        Assert.Equal(2, step.Y);
    }

    [Fact]
    public void WhichAxisSurvivesTheSqueezeDependsOnWhichNeighbourIsOccupied() {
        var grid = new CombatGrid();
        grid.SetTerrain(3, 2, CombatTerrain.Crystal);
        grid.SetTerrain(2, 3, CombatTerrain.Crystal);
        grid.SetOccupied(3, 2, true);

        StepResult step = CombatMovement.Step(grid, 2, 2, 6, 6);

        Assert.Equal(2, step.X);
        Assert.Equal(3, step.Y);
    }

    [Fact]
    public void OneCrystalAloneDoesNotStopTheDiagonal() {
        var grid = new CombatGrid();
        grid.SetTerrain(3, 2, CombatTerrain.Crystal);

        StepResult step = CombatMovement.Step(grid, 2, 2, 6, 6);

        Assert.Equal(3, step.X);
        Assert.Equal(3, step.Y);
    }
}
