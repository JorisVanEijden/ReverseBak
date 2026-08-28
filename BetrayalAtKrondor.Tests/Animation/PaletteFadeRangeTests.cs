namespace BetrayalAtKrondor.Tests.Animation;

using GameData.Resources.Animation;
using Xunit;

/// <summary>Which palette entries a cutscene fade touches (TASK-159 slice).</summary>
public class PaletteFadeRangeTests {
    private const int Vga = 256;

    [Theory]
    // Every (Start, Length) pair the shipped TTM scripts actually use. Extracted from
    // generated/TTM/*.json rather than imagined, so the fixture is the real input distribution.
    [InlineData(16, 240)]
    [InlineData(0, 256)]
    [InlineData(1, 255)]
    [InlineData(228, 28)]
    [InlineData(0, 255)]
    [InlineData(0, 112)]
    [InlineData(128, 80)]
    [InlineData(208, 48)]
    [InlineData(196, 59)]
    [InlineData(0, 239)]
    public void EVERYSHIPPEDRangeResolvesWithoutClamping(int start, int length) {
        // *** This is also what disproves "Length is really a LAST COLOUR". *** Under that reading
        // Start=228 Length=28 is an inverted range, and these pairs would clamp constantly instead
        // of fitting exactly.
        PaletteFadeRange.Range range = PaletteFadeRange.Resolve(start, length, Vga);

        Assert.True(range.Valid);
        Assert.False(range.Clamped, "no shipped fade should need clamping");
        Assert.Equal(start, range.Start);
        Assert.True(range.Start + range.Length <= Vga);
    }

    [Fact]
    public void ASTARTPastTheEndIsRefused_NotClamped() =>
        // Asymmetric on purpose: a start past the end names nothing, so there is no sensible range
        // to salvage. An over-long length still names real entries.
        Assert.False(PaletteFadeRange.Resolve(300, 10, Vga).Valid);

    [Fact]
    public void ANEGATIVEStartIsRefused() =>
        Assert.False(PaletteFadeRange.Resolve(-1, 10, Vga).Valid);

    [Fact]
    public void ANOVERLONGLengthIsClampedToTheEnd() {
        PaletteFadeRange.Range range = PaletteFadeRange.Resolve(200, 100, Vga);

        Assert.True(range.Valid);
        Assert.True(range.Clamped);
        Assert.Equal(56, range.Length);
        Assert.Equal(Vga, range.Start + range.Length);
    }

    [Fact]
    public void THEWHOLEPALETTESentinelFollowsThePalette_NotThe256() {
        // The one case where the sentinel is distinguishable from the clamp. On a 256-entry palette
        // both paths agree, which is why this needs a shorter one to test at all -- and why deleting
        // the branch as dead code would be a silent behaviour change.
        PaletteFadeRange.Range shortPalette =
            PaletteFadeRange.Resolve(0, PaletteFadeRange.WholePaletteLength, 64);

        Assert.True(shortPalette.Valid);
        Assert.Equal(64, shortPalette.Length);
        Assert.False(shortPalette.Clamped, "the sentinel resolves the length; it does not clamp it");
    }

    [Fact]
    public void ANEMPTYPaletteNamesNothing() =>
        Assert.False(PaletteFadeRange.Resolve(0, 256, 0).Valid);

    [Fact]
    public void ANEGATIVELengthIsRefusedRatherThanFadedBackwards() =>
        Assert.False(PaletteFadeRange.Resolve(10, -5, Vga).Valid);
}
