namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>
/// The movement-cell counter — <c>worldmove_step_tick_advance</c> / <c>_reset</c>.
/// </summary>
public class WorldStepTickTests {
    [Theory]
    [InlineData(400, 4)]   // Small
    [InlineData(800, 2)]   // Medium
    [InlineData(1600, 1)]  // Large: every step is a boundary
    public void StepsPerCell_IsTheCellOverTheStepDistance(int stepDistance, int expected) {
        Assert.Equal(expected, WorldStepTick.StepsPerCell(stepDistance));
    }

    [Fact]
    public void StepsPerCell_RefusesToDivideByZero() {
        // Not reachable from MOVEMENT.DAT, whose smallest preset is 400 — but a half-initialised
        // session can hand this a zero, and a throw at the first footstep is worse than a boundary.
        Assert.Equal(1, WorldStepTick.StepsPerCell(0));
    }

    [Fact]
    public void Advance_RaisesTheFlagOnlyOnTheStepThatCompletesTheCell() {
        var seen = new System.Collections.Generic.List<int>();
        var count = 0;
        for (var step = 0; step < 8; step++) {
            (count, int tick) = WorldStepTick.Advance(count, stepDistance: 400);
            seen.Add(tick);
        }

        // Four presses per cell at the Small preset, so the fourth and the eighth.
        Assert.Equal(new[] { 0, 0, 0, 1, 0, 0, 0, 1 }, seen);
    }

    [Fact]
    public void Advance_AtTheLargestStep_MakesEveryStepABoundary() {
        (int count, int tick) = WorldStepTick.Advance(0, stepDistance: 1600);

        Assert.Equal(0, count);
        Assert.Equal(WorldStepTick.OnBoundary, tick);
    }

    [Fact]
    public void Reset_RaisesTheFlag_SoTheFirstStepOfANewZoneCounts() {
        // *** NOT "clear everything". *** The zone-change arm resets so the first step in the new
        // zone is a boundary; zeroing both would swallow that zone's first encounter roll.
        (int count, int tick) = WorldStepTick.Reset();

        Assert.Equal(0, count);
        Assert.Equal(WorldStepTick.OnBoundary, tick);
        Assert.True(WorldStepTick.IsCellBoundary(tick));
    }

    [Fact]
    public void Advance_PastTheTarget_KeepsCountingRatherThanClampingEarly() {
        // The original compares for EQUALITY, so a step distance that grows mid-cell can carry the
        // count past the target and the flag stays down until the next reset brings it back. A `>=`
        // would raise the boundary a step early after any preferences change.
        (int count, int tick) = WorldStepTick.Advance(count: 6, stepDistance: 400);

        Assert.Equal(7, count);
        Assert.Equal(0, tick);
        Assert.False(WorldStepTick.IsCellBoundary(tick));
    }
}
