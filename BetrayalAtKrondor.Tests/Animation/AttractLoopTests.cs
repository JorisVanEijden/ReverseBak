namespace BetrayalAtKrondor.Tests.Animation;

using GameData.Resources.Animation;
using Xunit;

public class AttractLoopTests {
    [Fact]
    public void ItIsALoopAndNotAnIntro() {
        // Left alone the title screen cycles for ever: animation, credits, pause, animation...
        Assert.Equal(AttractLoop.Stage.Credits, AttractLoop.Next(AttractLoop.Stage.IntroAnimation));
        Assert.Equal(AttractLoop.Stage.PauseBeforeRepeat, AttractLoop.Next(AttractLoop.Stage.Credits));
        Assert.Equal(AttractLoop.Stage.IntroAnimation,
            AttractLoop.Next(AttractLoop.Stage.PauseBeforeRepeat));
    }

    [Fact]
    public void EitherMouseButtonCountsAndSoDoesAnyKey() {
        // Both buttons are polled separately and treated identically — a right-click during the
        // intro is as good as Enter. There is no "correct" key.
        Assert.True(AttractLoop.EndsOnAnyKeyOrEitherMouseButton);
    }

    [Fact]
    public void TheresAPauseBeforeTheLoopRepeats() {
        // deadline = g_timer_ticks + 0x8C, spun on while polling for input. Missing it makes the
        // attract restart immediately and never rest on the fade.
        Assert.Equal(140, AttractLoop.PauseTicksBeforeRepeat);
    }

    [Fact]
    public void EveryPassGoesThroughBlack() {
        Assert.True(AttractLoop.EachPassEndsWithAFadeToBlack);
    }
}
