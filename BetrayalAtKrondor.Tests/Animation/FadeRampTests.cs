namespace BetrayalAtKrondor.Tests.Animation;

using GameData.Resources.Animation;
using System;
using Xunit;

public class FadeRampTests {
    [Fact]
    public void TheTableIsTheORIGINALSSTEPS_NotFrameCounts() {
        // {0, 0x50, 0x14, 0x0A, 0x05, 0x02, 0x01} — the amount the ramp counter moves per write.
        Assert.Equal(new[] { 0, 80, 20, 10, 5, 2, 1 }, FadeRamp.StepTable);
    }

    [Fact]
    public void AHigherSpeedIsASLOWERFade() {
        // The argument runs the "wrong" way round: bigger speed, smaller step, longer fade. Reading
        // it as a rate rather than an index gets every fade in the game backwards.
        for (var speed = 1; speed < FadeRamp.MaxSpeed; speed++) {
            Assert.True(FadeRamp.StepFor(speed) > FadeRamp.StepFor(speed + 1));
            Assert.True(FadeRamp.PaletteWrites(speed) < FadeRamp.PaletteWrites(speed + 1));
        }
    }

    [Fact]
    public void SpeedZeroIsInstantAndIsADifferentCodePath() {
        // A step of zero would never terminate the loop, so the original branches before it and
        // writes the destination once. "Ramp with step 0" hangs; "ramp very fast" flickers where the
        // original cuts.
        Assert.True(FadeRamp.IsInstant(0));
        Assert.Equal(0, FadeRamp.PaletteWrites(0));
        for (var speed = 1; speed <= FadeRamp.MaxSpeed; speed++) {
            Assert.False(FadeRamp.IsInstant(speed));
        }
    }

    [Fact]
    public void TheWriteCountsAreTheRampDividedByTheStep() {
        Assert.Equal(8, FadeRamp.PaletteWrites(1));
        Assert.Equal(32, FadeRamp.PaletteWrites(2));
        Assert.Equal(64, FadeRamp.PaletteWrites(3));
        Assert.Equal(128, FadeRamp.PaletteWrites(4));
        Assert.Equal(320, FadeRamp.PaletteWrites(5));
        Assert.Equal(640, FadeRamp.PaletteWrites(6));
    }

    [Fact]
    public void TheClampIsLoadBearingAtTheTopOfTheRamp() {
        // 640 / 10 is 64, one past VGA's maximum — the first write of a fade-in and the last of a
        // fade-out would be out of range without it.
        Assert.Equal(FadeRamp.MaxIntensity, FadeRamp.IntensityAt(FadeRamp.RampTop));
        Assert.Equal(63, FadeRamp.IntensityAt(630));
        Assert.Equal(0, FadeRamp.IntensityAt(0));
        Assert.Equal(0, FadeRamp.IntensityAt(9));
        Assert.Equal(1, FadeRamp.IntensityAt(10));
        Assert.Equal(0, FadeRamp.IntensityAt(-5));
    }

    [Fact]
    public void ASpeedOutsideTheTableThrowsRatherThanGuessing() {
        Assert.Throws<ArgumentOutOfRangeException>(() => FadeRamp.StepFor(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => FadeRamp.StepFor(FadeRamp.MaxSpeed + 1));
    }
}
