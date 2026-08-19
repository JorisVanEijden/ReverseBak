namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Config;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// The colour remap the camp dial's shaded wedge draws through.
/// </summary>
/// <remarks>
/// Where the wedge goes is <c>EncampDial</c>'s and is tested there; this is only the substitution.
/// </remarks>
public class EncampShadowTests {
    [Fact]
    public void TheHighPaletteEntriesPassUnderTheWedgeUNSHADED() {
        // The table's second half is the identity — the reverse of the world lighting's split,
        // which protects the low entries and lights the high ones.
        List<(int R, int G, int B)> palette = Ramp();

        Assert.Equal(200, EncampShadow.ShadedEntry(palette, 200));
        Assert.Equal(EncampShadow.RemappedEntries,
            EncampShadow.ShadedEntry(palette, EncampShadow.RemappedEntries));
    }

    [Fact]
    public void AShadedEntryIsTheNearestTOTheDarkenedColourAndStaysInTheLowRange() {
        // A ramp where entry n is (n, n, n): darkening by 10 lands exactly on entry n-10.
        List<(int R, int G, int B)> palette = Ramp();

        Assert.Equal(40, EncampShadow.ShadedEntry(palette, 50));
        Assert.Equal(0, EncampShadow.ShadedEntry(palette, 5));
    }

    [Fact]
    public void WhereThePaletteHasNoDarkerNEIGHBOURTheColourDoesNotChange() {
        // Every entry the same colour: the nearest match to "darker" is itself, so the wedge is
        // invisible there. A constant multiply would darken it anyway, and flat art is most of the
        // dial.
        var flat = new List<(int R, int G, int B)>();
        for (var i = 0; i < 256; i++) {
            flat.Add((32, 32, 32));
        }

        Assert.Equal(0, EncampShadow.ShadedEntry(flat, 70));
        Assert.True(EncampShadow.TiesKeepTheEarlierEntry);
    }

    [Fact]
    public void ThePALETTEISEIGHTBITSoTheDarkeningHasToBeWidenedToo() {
        // The extractor widens VGA's six bits to eight. Ten used raw against 0..255 is a quarter of
        // the intended shade — a wedge you can barely see, which reads as "the remap does almost
        // nothing" rather than as a units mistake.
        Assert.Equal(10, EncampShadow.DarkeningFor(EncampShadow.VgaChannelMax));
        Assert.Equal(40, EncampShadow.DarkeningFor(255));

        List<(int R, int G, int B)> palette = Ramp();
        Assert.Equal(50 - 40, EncampShadow.ShadedEntry(palette, 50, EncampShadow.DarkeningFor(255)));
    }

    private static List<(int R, int G, int B)> Ramp() {
        var palette = new List<(int R, int G, int B)>();
        for (var i = 0; i < 256; i++) {
            palette.Add((i, i, i));
        }

        return palette;
    }
}
