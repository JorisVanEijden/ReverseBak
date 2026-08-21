namespace BetrayalAtKrondor.Tests.Animation;

using GameData.Resources.Animation;
using Xunit;

/// <summary>
/// The recovered animation clock. These pin the DERIVATION, not a remembered number — each step is
/// something the sources state, so a wrong constant fails at the step that owns it.
/// </summary>
public class GameTickTests {
    [Fact]
    public void TheDivisorIsTheTRUNCATEDQuotient_AsA16BitDivideProduces() {
        // 0xffff / 13 is 5041.15. The chip gets 5041, and the game's rate follows from THAT rather
        // than from the exact ratio — rounding to 5042 here would be a different clock than the one
        // that shipped, and a test written against a remembered 59.17 would not notice.
        Assert.Equal(5041, GameTick.PitDivisor);
        Assert.NotEqual(0xffff / (double)GameTick.BiosTickMultiple, GameTick.PitDivisor);
    }

    [Fact]
    public void TheArgumentIsAMultipleOfTheBiosTick_NotAFrequency() {
        // The tell: 13 as a frequency would be 13 Hz, which is SLOWER than the BIOS tick it
        // replaces. Read as a multiple it is 13x faster, which is the only reading consistent with
        // the ISR chaining to BIOS every 13th tick to keep the clock right.
        const double biosHz = 18.2065;

        Assert.InRange(GameTick.Irq0Hz, biosHz * 12, biosHz * 14);
        Assert.True(GameTick.Irq0Hz > biosHz, "the installed rate is faster than the BIOS tick");
    }

    [Fact]
    public void TheCounterAdvancesOnceEveryFourthInterrupt() {
        Assert.Equal(4, GameTick.Irq0sPerTick);
        Assert.Equal(GameTick.Irq0Hz / 4, GameTick.TicksPerSecond, 6);
    }

    [Fact]
    public void TheClockRunsAtAboutFiftyNinePointTwoHertz() {
        Assert.Equal(59.1739, GameTick.TicksPerSecond, 3);
        Assert.Equal(0.016899, GameTick.SecondsPerTick, 5);
    }

    [Fact]
    public void ItIsCloseToSixtyHertzButNotSixty() {
        // Why the remake's 60Hz assumption survived so long, and why it still has to go: a whole
        // second of animation lands about 14ms early, which is invisible in a cutscene and wrong in
        // anything that counts ticks against a deadline.
        Assert.InRange(GameTick.TicksPerSecond, 59.0, 59.5);
        Assert.NotEqual(60.0, GameTick.TicksPerSecond, 1);
    }

    [Fact]
    public void TheAttractLoopPauseIsAboutTwoAndAThirdSeconds() {
        // 140 ticks — the one place a tick count had already been recovered but could not be turned
        // into a duration.
        double pause = GameTick.Seconds(AttractLoop.PauseTicksBeforeRepeat);

        Assert.Equal(2.366, pause, 2);
    }

    [Fact]
    public void SecondsIsLinearAndZeroTicksIsNoTime() {
        Assert.Equal(0.0, GameTick.Seconds(0));
        Assert.Equal(GameTick.SecondsPerTick, GameTick.Seconds(1), 9);
        Assert.Equal(2 * GameTick.Seconds(70), GameTick.Seconds(140), 9);
    }
}
