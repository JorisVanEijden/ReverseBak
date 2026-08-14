namespace BetrayalAtKrondor.Tests.Dialog;

using GameData.Resources.Dialog;
using Xunit;

/// <summary>
/// The speaker-portrait cache. Six slots, two faces per actor but one palette, and a full cache the
/// original never checks for.
/// </summary>
public class ActorFaceCacheTests {
    [Fact]
    public void AKnownActorIsFoundInItsSlot() {
        Assert.Equal(1, ActorFaceCache.SlotFor(12, new[] { 7, 12, 0, 0, 0, 0 }));
    }

    [Fact]
    public void AMissTakesTheLastEmptySlotNotTheFirst() {
        // The scan runs all six and keeps overwriting its candidate, so a cache with holes fills
        // from the back. A port that takes the first free slot will not match a trace.
        Assert.Equal(5, ActorFaceCache.SlotFor(9, new[] { 7, 0, 8, 0, 3, 0 }));
    }

    [Fact]
    public void AFullCacheIsRefusedRatherThanWritingBeforeTheTable() {
        // The original's candidate starts at -1 and is used unguarded — a seventh distinct speaker
        // writes out of bounds. It never bites because a scene never has seven, but we do not
        // reproduce it.
        Assert.Equal(-1, ActorFaceCache.SlotFor(9, new[] { 1, 2, 3, 4, 5, 6 }));
    }

    [Fact]
    public void AHitWinsOverAFreeSlot() {
        Assert.Equal(0, ActorFaceCache.SlotFor(7, new[] { 7, 0, 0, 0, 0, 0 }));
    }

    [Fact]
    public void HighNumberedActorsSimplyHaveNoFace() {
        // Said by nulling the bitmap and palette, not by failing — a caller treating null as an
        // error rejects an ordinary speaker.
        Assert.True(ActorFaceCache.HasFace(48));
        Assert.False(ActorFaceCache.HasFace(49));
        Assert.False(ActorFaceCache.HasFace(200));
    }

    [Fact]
    public void AndAreNotCachedEither() {
        // The no-face path never records the actor number, so the lookup repeats every request.
        Assert.False(ActorFaceCache.IsRemembered(49));
        Assert.True(ActorFaceCache.IsRemembered(12));
    }

    [Fact]
    public void AnActorCanHaveTwoFaces() {
        Assert.Equal("ACT001.BMP", ActorFaceCache.BitmapNameFor(1, alternate: false));
        Assert.Equal("ACT001A.BMP", ActorFaceCache.BitmapNameFor(1, alternate: true));
    }

    [Fact]
    public void ButOnlyOnePalette() {
        // Both portraits are drawn through the same colours; there is no A-variant palette file.
        Assert.Equal("ACT001.PAL", ActorFaceCache.PaletteNameFor(1));
        Assert.DoesNotContain("A.PAL", ActorFaceCache.PaletteNameFor(1));
    }

    [Fact]
    public void NamesArePaddedToThreeDigits() {
        Assert.Equal("ACT007.BMP", ActorFaceCache.BitmapNameFor(7, alternate: false));
        Assert.Equal("ACT048.PAL", ActorFaceCache.PaletteNameFor(48));
    }

    [Fact]
    public void ThePaletteMarkerIsAHandshakeNotAColour() {
        // The loader stamps it; ShowDialogWithFace tests for exactly it before using the palette.
        Assert.True(ActorFaceCache.PaletteIsPrepared(0x3f));
        Assert.False(ActorFaceCache.PaletteIsPrepared(0));
    }

    [Fact]
    public void NothingIsEvictedIndividually() {
        // A scene transition is the only thing that reclaims a portrait.
        Assert.False(ActorFaceCache.EvictsIndividually);
    }

    [Fact]
    public void AnAbsentCacheIsNotAnError() {
        Assert.Equal(-1, ActorFaceCache.SlotFor(1, null));
    }
}
