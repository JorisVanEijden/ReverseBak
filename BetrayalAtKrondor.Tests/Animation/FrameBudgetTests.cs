namespace BetrayalAtKrondor.Tests.Animation;

using GameData.Resources.Animation;
using Xunit;

public class FrameBudgetTests {
    [Fact]
    public void TheFramesOwnWorkComesOutOfItsOwnBudget() {
        // expiry = now + interval, written once the commands have run — so a frame that spent 4ms
        // drawing waits 6ms more of a 10ms budget, not another 10.
        Assert.Equal(6d, FrameBudget.RemainingWait(10d, 4d), 6);
        Assert.Equal(10d, FrameBudget.RemainingWait(10d, 0d), 6);
    }

    [Fact]
    public void AnOverrunIsNotCarriedForward() {
        // Late is late: the scheduler moves on rather than clawing time back from the next frame.
        Assert.Equal(0d, FrameBudget.RemainingWait(10d, 25d), 6);
        Assert.True(FrameBudget.Overran(10d, 25d));
        Assert.True(FrameBudget.Overran(10d, 10d));
        Assert.False(FrameBudget.Overran(10d, 9.5d));
    }

    [Fact]
    public void AZeroBudgetFrameIsAlwaysOverrun() {
        Assert.True(FrameBudget.Overran(0d, 0d));
        Assert.Equal(0d, FrameBudget.RemainingWait(0d, 0d), 6);
    }

    [Fact]
    public void FrameTimingAndPaletteCyclingMustUseTheSameRate() {
        // One timer drives both in the original. Two rates make a shimmer drift against the frame it
        // belongs to, and the error grows with the hold. The remake currently violates this — see
        // the remarks on the rule.
        Assert.True(FrameBudget.FramesAndPaletteCyclesShareOneClock);
    }
}
