namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>
/// Digging a grave — <c>handle_Grave</c> @0x77ca9.
/// </summary>
public class GraveDiggingTests {
    /// <summary>
    /// THE FENCE for a constant that was wrong. <see cref="GraveDigging.ShovelObjectId"/> read
    /// <c>0x1e</c> — 30, the <b>Light Crossbow</b> — so the first handler built on it would have
    /// refused to dig for want of a bow, with nothing in the disassembly to contradict it.
    /// </summary>
    /// <remarks>
    /// The push at <c>handle_Grave</c> +0x14B assembles as <c>6A 53</c>: a <c>push byte</c> of
    /// <c>0x53</c>. IDA renders that through an enum member named <c>Shovel</c>, so the listing reads
    /// correctly whichever number is written down here — which is exactly how the wrong one survived.
    /// Object 83 in the shipped OBJINFO.DAT is named "Shovel"; object 30 is "Light Crossbow".
    /// </remarks>
    [Fact]
    public void TheShovelIsObject83_NotTheLightCrossbowAt30() {
        Assert.Equal(0x53, GraveDigging.ShovelObjectId);
        Assert.Equal(83, GraveDigging.ShovelObjectId);
    }

    /// <summary>Only the three content bits make a grave diggable; anything else is examine-only.
    /// </summary>
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(4, true)]
    [InlineData(8, true)]
    [InlineData(0x20, false)]
    public void OnlyTheContentBitsMakeItDiggable(int flags, bool diggable) =>
        Assert.Equal(diggable, GraveDigging.IsDiggable(flags));

    /// <summary>
    /// The outcome chain is <b>first match wins, and its last arm is an ELSE</b>: a grave flagged
    /// Loot AND Body opens and never mentions the body, and anything diggable that is neither reads
    /// as an empty coffin whether or not bit 3 is set.
    /// </summary>
    [Theory]
    [InlineData(2, GraveDigging.Contents.Loot)]
    [InlineData(2 | 4, GraveDigging.Contents.Loot)]
    [InlineData(4, GraveDigging.Contents.Body)]
    [InlineData(4 | 8, GraveDigging.Contents.Body)]
    [InlineData(8, GraveDigging.Contents.Empty)]
    public void TheOutcomeChainIsFirstMatchWins(int flags, GraveDigging.Contents expected) =>
        Assert.Equal(expected, GraveDigging.OutcomeFor(flags));

    [Fact]
    public void ANonLootOutcomePicksItsOwnDialog() {
        Assert.Equal(GraveDigging.JustABodyDialog,
            GraveDigging.DialogFor(GraveDigging.Contents.Body));
        Assert.Equal(GraveDigging.EmptyCoffinDialog,
            GraveDigging.DialogFor(GraveDigging.Contents.Empty));
    }

    /// <summary>A trapped grave is dug only from its own tile, and the test is on the TILE both
    /// sides — the grave's world position divided down, not compared raw.</summary>
    [Fact]
    public void ATrappedGraveIsDugOnlyFromItsOwnTile() {
        long x = 3 * WorldPlacement.TileSize + 1000;
        long y = 5 * WorldPlacement.TileSize + 2000;

        Assert.True(GraveDigging.PartyIsCloseEnough(x, y, 3, 5));
        Assert.False(GraveDigging.PartyIsCloseEnough(x, y, 4, 5));
        Assert.False(GraveDigging.PartyIsCloseEnough(x, y, 3, 6));
    }
}
