namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>
/// The palette lerp. Its parameter reads as "how much effect" and means the opposite, which is the
/// one way to get every lighting effect in the game backwards at once.
/// </summary>
public class PaletteBlendTests {
    [Fact]
    public void TheParameterIsALightLevelAndTheEffectIsItsComplement() {
        // Higher means LESS movement toward the target, not more.
        Assert.Equal(0, PaletteBlend.EffectOf(64));
        Assert.Equal(49, PaletteBlend.EffectOf(15));
        Assert.True(PaletteBlend.EffectOf(15) > PaletteBlend.EffectOf(50));
    }

    [Fact]
    public void FullLightLeavesThePaletteAlone() {
        Assert.True(PaletteBlend.IsPassThrough(64));
        Assert.Equal(40, PaletteBlend.Channel(40, 0, 64));
    }

    [Fact]
    public void ZeroLightAlsoLeavesItAloneRatherThanGoingBlack() {
        // The effect is clamped out at both ends, so "no light" means unchanged — the darkness you
        // see at night comes from levels in between, never from the bottom of the range.
        Assert.True(PaletteBlend.IsPassThrough(0));
        Assert.Equal(40, PaletteBlend.Channel(40, 0, 0));
    }

    [Fact]
    public void DarkeningMovesAChannelTowardTheTarget() {
        // light 32 -> effect 32 -> half way to black.
        Assert.Equal(20, PaletteBlend.Channel(40, 0, 32));
    }

    [Fact]
    public void ItWorksUpwardsAsWellAsDown() {
        Assert.Equal(30, PaletteBlend.Channel(20, 40, 32));
    }

    [Fact]
    public void TheDivisionTruncatesTowardZeroInBothDirections() {
        // The original computes the magnitude and negates it, so a channel moving down rounds the
        // same way as one moving up.
        Assert.Equal(PaletteBlend.Channel(0, 10, 63) - 0, 0 - (PaletteBlend.Channel(10, 0, 63) - 10));
    }

    [Fact]
    public void ANightLevelDarkensButNeverToBlack() {
        int lit = PaletteBlend.Channel(63, 0, DaylightLevel.Night);

        Assert.True(lit > 0);
        Assert.True(lit < 63);
    }

    [Fact]
    public void AChannelAlreadyAtTheTargetDoesNotMove() {
        Assert.Equal(0, PaletteBlend.Channel(0, 0, 32));
        Assert.Equal(63, PaletteBlend.Channel(63, 63, 32));
    }

    [Fact]
    public void TheResultStaysInsideTheChannelRange() {
        for (var light = 1; light < PaletteBlend.Scale; light++) {
            for (var source = 0; source <= PaletteBlend.MaxChannel; source += 7) {
                Assert.InRange(PaletteBlend.Channel(source, 0, light), 0, PaletteBlend.MaxChannel);
                Assert.InRange(PaletteBlend.Channel(source, PaletteBlend.MaxChannel, light),
                    0, PaletteBlend.MaxChannel);
            }
        }
    }

    [Fact]
    public void TheTwoLookupTablesAreOneContiguousRun() {
        // The overrun off the end of one into the other is deliberate and is what lets a single
        // index serve both signs.
        Assert.True(PaletteBlend.TablesAreContiguous);
    }

    [Fact]
    public void TheTableIsRebuiltOnlyWhenTheLevelChanges() {
        Assert.True(PaletteBlend.TableIsCachedByLevel);
    }

    [Fact]
    public void TheCandleTintIsGreenAndTheItemLightIsTheWarmOne() {
        // Almost the opposite of what the two names imply; read from the binary, not guessed.
        (int r, int g, int b) candle = DynamicLighting.Colors.CandleLight;
        (int r, int g, int b) item = DynamicLighting.Colors.ItemLight;

        Assert.True(candle.g > candle.r && candle.g > candle.b);
        Assert.True(item.r > item.b && item.g > item.b);
    }

    [Fact]
    public void EachTintKnowsItsColour() {
        Assert.Equal(DynamicLighting.Colors.Stardusk,
            DynamicLighting.ColorOf(DynamicLighting.Tint.Stardusk));
        Assert.Equal(DynamicLighting.Colors.Black,
            DynamicLighting.ColorOf(DynamicLighting.Tint.None));
    }

    [Fact]
    public void EveryColourFitsInSixBits() {
        foreach ((int r, int g, int b) colour in new[] {
            DynamicLighting.Colors.DragonsBreath, DynamicLighting.Colors.Black,
            DynamicLighting.Colors.ItemLight, DynamicLighting.Colors.CandleLight,
            DynamicLighting.Colors.Stardusk,
        }) {
            Assert.InRange(colour.r, 0, PaletteBlend.MaxChannel);
            Assert.InRange(colour.g, 0, PaletteBlend.MaxChannel);
            Assert.InRange(colour.b, 0, PaletteBlend.MaxChannel);
        }
    }
}
