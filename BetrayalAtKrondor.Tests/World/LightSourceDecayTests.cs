namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>
/// How light sources burn down. Three of the four fade quadratically; the fourth builds instead,
/// which is the one a port runs backwards.
/// </summary>
public class LightSourceDecayTests {
    private static int Item(int minutes) =>
        LightSourceDecay.LevelFor(LightSourceDecay.Source.Item, minutes);

    private static int Breath(int minutes, int flicker = 0) =>
        LightSourceDecay.LevelFor(LightSourceDecay.Source.DragonsBreath, minutes, flicker);

    [Fact]
    public void ATorchHoldsSteadyWhileItHasPlentyOfTimeLeft() {
        Assert.Equal(LightSourceDecay.SteadyLevel, Item(8));
        Assert.Equal(LightSourceDecay.SteadyLevel, Item(60));
    }

    [Fact]
    public void ThenItFallsOffACliffRatherThanFadingEvenly() {
        // Six minutes left is still about three-quarters bright; the last minute is almost nothing.
        Assert.Equal(36, Item(6));
        Assert.Equal(9, Item(3));
        Assert.Equal(1, Item(1));
        Assert.Equal(0, Item(0));
    }

    [Fact]
    public void MostOfTheBrightnessIsGoneBeforeMostOfTheTimeIs() {
        // The proportional loss is what makes it read as a cliff: half the burn is spent by six
        // minutes yet three-quarters of the light is still there, and the last three minutes are
        // spent below a fifth of it.
        Assert.True(Item(6) * 4 > LightSourceDecay.SteadyLevel * 2);
        Assert.True(Item(3) * 5 < LightSourceDecay.SteadyLevel);
    }

    [Fact]
    public void TheAbsoluteStepIsBiggestAtTheTopOfTheFadeNotTheBottom() {
        // A square's differences shrink as it falls, so the raw drop per minute is LARGEST just
        // after it leaves the steady level — the opposite of what "falls off a cliff" suggests if
        // read as absolute change.
        Assert.True(Item(7) - Item(6) > Item(2) - Item(1));
    }

    [Fact]
    public void TheSteadyLevelIsSevenSquaredNotEight() {
        // So nothing ever reaches the light scale's ceiling from burning alone.
        Assert.Equal(49, LightSourceDecay.SteadyLevel);
        Assert.NotEqual(64, LightSourceDecay.SteadyLevel);
        Assert.True(LightSourceDecay.SteadyLevel < DaylightLevel.Day);
    }

    [Fact]
    public void CandleGlowAndStarduskBurnExactlyLikeAnItem() {
        for (var minutes = 0; minutes <= 12; minutes++) {
            Assert.Equal(Item(minutes),
                LightSourceDecay.LevelFor(LightSourceDecay.Source.CandleGlow, minutes));
            Assert.Equal(Item(minutes),
                LightSourceDecay.LevelFor(LightSourceDecay.Source.Stardusk, minutes));
        }
    }

    [Fact]
    public void DragonsBreathFlickersWhileItHasTimeToRun() {
        Assert.Equal(8, Breath(30, flicker: 0));
        Assert.Equal(9, Breath(30, flicker: 1));
    }

    [Fact]
    public void AndThenBuildsInsteadOfFading() {
        // The opposite of the other three: porting it as another decaying source runs the effect
        // backwards and loses its whole shape.
        Assert.True(Breath(2) > Breath(5));
        Assert.True(Breath(1) > Breath(2));
    }

    [Fact]
    public void ItPeaksExactlyAsItExpires() {
        Assert.Equal(LightSourceDecay.DragonsBreathPeak, Breath(0));
        Assert.True(Breath(0) > Breath(1));
    }

    [Fact]
    public void DragonsBreathIsTheOnlyThingThatReachesTheCeiling() {
        Assert.Equal(DaylightLevel.Day, LightSourceDecay.DragonsBreathPeak);
        Assert.True(Item(0) < LightSourceDecay.DragonsBreathPeak);
    }

    [Fact]
    public void OnlyTheItemTimerSpendsChargesAndOnlyAtExactlyZero() {
        Assert.True(LightSourceDecay.SpendsItemChargesAt(LightSourceDecay.Source.Item, 0));
        Assert.False(LightSourceDecay.SpendsItemChargesAt(LightSourceDecay.Source.Item, 1));
        Assert.False(LightSourceDecay.SpendsItemChargesAt(LightSourceDecay.Source.CandleGlow, 0));
    }

    [Fact]
    public void ANegativeRemainderIsTreatedAsExpiredRatherThanGoingPositiveAgain() {
        // It is a square, so an unguarded negative would come back up.
        Assert.Equal(0, Item(-3));
    }

    [Fact]
    public void EveryUpdateAsksForARelight() {
        Assert.True(LightSourceDecay.AlwaysRequestsRelight);
    }
}
