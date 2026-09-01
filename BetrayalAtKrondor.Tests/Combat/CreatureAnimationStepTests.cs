namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>The walk cycle — <c>advanceCreatureAnimationFrame</c> @0x5d37a.</summary>
public class CreatureAnimationStepTests {
    [Theory]
    [InlineData(0, 8)]
    [InlineData(7, 15)]
    [InlineData(8, 8)]      // only the low three bits are used
    [InlineData(0x1234, 12)]
    public void TheGaitDelayIsEightPlusTheLowThreeBits(int roll, int expected) {
        Assert.Equal(expected, CreatureAnimationStep.NextGaitDelay(roll));
    }

    [Fact]
    public void EveryDelayTheRollCanProduceIsInRange() {
        for (var roll = 0; roll < 64; roll++) {
            int delay = CreatureAnimationStep.NextGaitDelay(roll);
            Assert.InRange(delay, CreatureAnimationStep.GaitDelayMinimum,
                CreatureAnimationStep.GaitDelayMaximum);
        }
    }

    /// <summary>The gate is a MODULO, which is what makes the counter's reset to 1 matter.</summary>
    [Fact]
    public void ItAdvancesOnEveryMultipleOfTheDelay() {
        Assert.True(CreatureAnimationStep.Advances(tickCounter: 8, frameDelay: 8));
        Assert.True(CreatureAnimationStep.Advances(tickCounter: 16, frameDelay: 8));
        Assert.False(CreatureAnimationStep.Advances(tickCounter: 7, frameDelay: 8));
        // Reset is to 1, not 0 — so the tick after an advance is 1 and does NOT advance again.
        Assert.Equal(1, CreatureAnimationStep.TickCounterAfterAdvance);
        Assert.False(CreatureAnimationStep.Advances(
            CreatureAnimationStep.TickCounterAfterAdvance, frameDelay: 8));
    }

    /// <summary>A zero delay would divide by zero in the original; refuse rather than fault.</summary>
    [Fact]
    public void AZeroDelayDoesNotAdvance() {
        Assert.False(CreatureAnimationStep.Advances(tickCounter: 0, frameDelay: 0));
    }

    [Fact]
    public void OnlySlotZeroPingPongs() {
        Assert.True(CreatureAnimationStep.PingPongs(0));
        Assert.False(CreatureAnimationStep.PingPongs(1));
        Assert.False(CreatureAnimationStep.PingPongs(4));
    }

    /// <summary>Five authored columns; 5, 6 and 7 are the mirror of the others.</summary>
    [Fact]
    public void FacingsAboveFourAreMirrored() {
        for (var facing = 0; facing <= 4; facing++) {
            Assert.False(CreatureAnimationStep.DrawnMirrored(facing), $"facing {facing}");
        }
        for (var facing = 5; facing <= 7; facing++) {
            Assert.True(CreatureAnimationStep.DrawnMirrored(facing), $"facing {facing}");
        }
    }
}
