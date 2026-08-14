namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>
/// What the lighting blend is asked to do. Underground ignoring the clock, and the tint being
/// suppressed at noon, are the two that change how a scene looks rather than by how much.
/// </summary>
public class DynamicLightingTests {
    private static DynamicLighting.Lighting Above(int daylight, int stardusk = 0, int item = 0,
        int tintStrength = 40) =>
        DynamicLighting.Resolve(false, 1, daylight, 0, stardusk, item, tintStrength);

    private static DynamicLighting.Lighting Below(int candle, int item = 0) =>
        DynamicLighting.Resolve(true, 1, DaylightLevel.Day, candle, 0, item, 0);

    [Fact]
    public void UndergroundIgnoresTheClockEntirely() {
        // The daylight term is added only on the above-ground path. A remake that dims a cave at
        // night is inventing a mechanic.
        int atNoon = DynamicLighting.Resolve(true, 1, DaylightLevel.Day, 20, 0, 0, 0).Light;
        int atMidnight = DynamicLighting.Resolve(true, 1, DaylightLevel.Night, 20, 0, 0, 0).Light;

        Assert.Equal(atNoon, atMidnight);
    }

    [Fact]
    public void UndergroundIgnoresStarduskToo() {
        int without = DynamicLighting.Resolve(true, 1, 0, 20, 0, 0, 0).Light;
        int with = DynamicLighting.Resolve(true, 1, 0, 20, 40, 0, 0).Light;

        Assert.Equal(without, with);
    }

    [Fact]
    public void UndergroundLightIsTheCandlePlusTheNightFloorPlusItems() {
        Assert.Equal(20 + DynamicLighting.MinimumLight + 5, Below(20, item: 5).Light);
    }

    [Fact]
    public void ACandleAlwaysTintsBecauseItsStrengthIsAConstant() {
        DynamicLighting.Lighting lit = Below(candle: 20);

        Assert.Equal(DynamicLighting.Tint.Candle, lit.Tint);
        Assert.Equal(DynamicLighting.CandleTintStrength, lit.TintStrength);
        Assert.True(lit.AppliesTint);
    }

    [Fact]
    public void NoCandleUndergroundMeansNoTintButStillTheFloor() {
        DynamicLighting.Lighting lit = Below(candle: 0);

        Assert.Equal(DynamicLighting.Tint.None, lit.Tint);
        Assert.False(lit.AppliesTint);
        Assert.Equal(DynamicLighting.MinimumLight, lit.Light);
    }

    [Fact]
    public void ALightSourceTintsNothingAtNoon() {
        // The guard is strength < 64, and the time-of-day curve is exactly 64 through the middle of
        // the day — so the tint only starts to show as evening comes on.
        DynamicLighting.Lighting noon = Above(DaylightLevel.Day, stardusk: 10, tintStrength: DaylightLevel.Day);
        DynamicLighting.Lighting evening = Above(40, stardusk: 10, tintStrength: 45);

        Assert.Equal(DynamicLighting.Tint.Stardusk, noon.Tint);
        Assert.False(noon.AppliesTint);
        Assert.True(evening.AppliesTint);
    }

    [Fact]
    public void TheTintIsChosenByPriorityNotBySum() {
        // Stardusk wins outright when present; item light only shows when there is none.
        Assert.Equal(DynamicLighting.Tint.Stardusk, Above(30, stardusk: 1, item: 40).Tint);
        Assert.Equal(DynamicLighting.Tint.ItemLight, Above(30, stardusk: 0, item: 1).Tint);
        Assert.Equal(DynamicLighting.Tint.None, Above(30).Tint);
    }

    [Fact]
    public void BothSourcesStillCountTowardTheBrightnessWhicheverColourWins() {
        Assert.True(Above(30, stardusk: 5, item: 5).Light > Above(30, stardusk: 5).Light);
    }

    [Fact]
    public void TheLightIsClampedAtBothEnds() {
        Assert.Equal(DynamicLighting.MinimumLight, Above(0).Light);
        Assert.Equal(DynamicLighting.MaximumLight, Above(DaylightLevel.Day, stardusk: 60, item: 60).Light);
    }

    [Fact]
    public void AModeOtherThanOneOrTwoLightsNothingAtAll() {
        Assert.True(DynamicLighting.ModeLights(1));
        Assert.True(DynamicLighting.ModeLights(2));
        Assert.False(DynamicLighting.ModeLights(0));
        Assert.False(DynamicLighting.ModeLights(3));
    }

    [Fact]
    public void TheModeDecidesHowMuchOfThePaletteIsProtected() {
        // Everything below the first lit entry is copied through untouched.
        Assert.Equal(112, DynamicLighting.FirstLitEntry(1));
        Assert.Equal(16, DynamicLighting.FirstLitEntry(2));
        Assert.True(DynamicLighting.FirstLitEntry(2) < DynamicLighting.FirstLitEntry(1));
    }

    [Fact]
    public void TheModeIsTheOnOffSwitchAndItTracksTheZonePalette() {
        // Loading a zone palette sets mode 1; disposing them sets 0. Lighting is live exactly while
        // a zone's palette is resident.
        Assert.False(DynamicLighting.ModeLights(DynamicLighting.ModeOff));
        Assert.True(DynamicLighting.ModeLights(DynamicLighting.ModeZone));
        Assert.True(DynamicLighting.ModeLights(DynamicLighting.ModeExtended));
    }

    [Fact]
    public void APaletteQueuedWithNoZoneLoadedIsAppliedRaw() {
        Assert.False(DynamicLighting.FrameIsLit(palettePending: true, DynamicLighting.ModeOff));
        Assert.True(DynamicLighting.FrameIsLit(palettePending: true, DynamicLighting.ModeZone));
    }

    [Fact]
    public void NoPendingPaletteMeansNoLightingWorkAtAll() {
        Assert.False(DynamicLighting.FrameIsLit(palettePending: false, DynamicLighting.ModeZone));
    }

    [Fact]
    public void EachTintKnowsWhichNightFloorItsStrengthComesFrom() {
        Assert.Equal(DaylightLevel.StarduskFloor,
            DynamicLighting.TintFloorFor(DynamicLighting.Tint.Stardusk));
        Assert.Equal(DaylightLevel.ItemLightFloor,
            DynamicLighting.TintFloorFor(DynamicLighting.Tint.ItemLight));
    }

    [Fact]
    public void DragonsBreathDoesNotCompeteWithTheTints() {
        Assert.True(DynamicLighting.DragonsBreathIsIndependent);
    }
}
