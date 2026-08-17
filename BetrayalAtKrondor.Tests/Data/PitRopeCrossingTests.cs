namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.World;
using Xunit;

/// <summary>Swinging across a pit on a rope (<c>handle_Pit</c> @0x79c63).</summary>
public class PitRopeCrossingTests {
    [Theory]
    [InlineData(0x0000, PitRopeCrossing.PitAxis.AlongX)]
    [InlineData(0x8000, PitRopeCrossing.PitAxis.AlongX)]
    [InlineData(0x4000, PitRopeCrossing.PitAxis.AlongY)]
    [InlineData(0xC000, PitRopeCrossing.PitAxis.AlongY)]
    public void TheFourAxisAlignedRotationsAreCrossable(int rotation, PitRopeCrossing.PitAxis axis) =>
        Assert.Equal(axis, PitRopeCrossing.AxisOf(rotation));

    [Theory]
    [InlineData(0x2000)]
    [InlineData(0x6000)]
    [InlineData(0x1234)]
    public void APitAtAnyOtherAngleIsUncrossable(int rotation) =>
        // And says nothing about it — the handler falls through to the no-offer path.
        Assert.Equal(PitRopeCrossing.PitAxis.None, PitRopeCrossing.AxisOf(rotation));

    [Fact]
    public void OppositeFacingsAreTheSameAxis() =>
        // 0 and 0x8000 point opposite ways along one axis, which is why the original tests four
        // equalities rather than two.
        Assert.Equal(PitRopeCrossing.AxisOf(0x0000), PitRopeCrossing.AxisOf(0x8000));

    // ---- the lateral band ----------------------------------------------------------------

    [Fact]
    public void StandingBesideTheHookIsLinedUp() =>
        Assert.True(PitRopeCrossing.IsLinedUp(alongPit: 5000, pitAlongPit: 5000));

    [Fact]
    public void JustInsideTheBandIsLinedUp() {
        Assert.True(PitRopeCrossing.IsLinedUp(5000 + 299, 5000));
        Assert.True(PitRopeCrossing.IsLinedUp(5000 - 299, 5000));
    }

    [Fact]
    public void ExactlyOnTheBandIsRefused() {
        // jg / jl — strict. A party exactly 300 off centre does not get the offer.
        Assert.False(PitRopeCrossing.IsLinedUp(5000 + 300, 5000));
        Assert.False(PitRopeCrossing.IsLinedUp(5000 - 300, 5000));
    }

    [Fact]
    public void StandingAtTheEndOfThePitIsRefused() =>
        // The band is what stops a swing from the far end, where there is no hook.
        Assert.False(PitRopeCrossing.IsLinedUp(5000 + 4000, 5000));

    // ---- the landing ---------------------------------------------------------------------

    [Fact]
    public void TheSwingCrossesToTheFarSide() {
        // Approaching from the high side lands low, and vice versa — never back where you started.
        Assert.Equal(5000 - 900, PitRopeCrossing.LandingPosition(acrossPit: 5400, pitAcrossPit: 5000));
        Assert.Equal(5000 + 900, PitRopeCrossing.LandingPosition(acrossPit: 4600, pitAcrossPit: 5000));
    }

    [Fact]
    public void TheLandingIsAlwaysAcrossTheCentre() {
        foreach (int approach in new[] { 4000, 4999, 5001, 6000 }) {
            int landing = PitRopeCrossing.LandingPosition(approach, 5000);
            Assert.True((approach > 5000) != (landing > 5000),
                "landing " + landing + " is on the same side as approach " + approach);
        }
    }

    // ---- the offer gate ------------------------------------------------------------------

    [Fact]
    public void WithARopeAndAnAlignedPitTheOfferIsMade() =>
        Assert.True(PitRopeCrossing.CanOffer(ropeCount: 1, rotationZ: 0, alongPit: 5000, pitAlongPit: 5000));

    [Fact]
    public void NoRopeMeansNoOffer() =>
        // Checked before the pit is looked at, and the refusal says nothing about ropes.
        Assert.False(PitRopeCrossing.CanOffer(0, 0, 5000, 5000));

    [Fact]
    public void AnUnalignedPitIsNotOfferedEvenWithARope() =>
        Assert.False(PitRopeCrossing.CanOffer(3, 0x2000, 5000, 5000));

    [Fact]
    public void BeingOutOfLineIsNotOfferedEvenWithARope() =>
        Assert.False(PitRopeCrossing.CanOffer(3, 0, 9000, 5000));

    [Fact]
    public void TheRopeIsObject82() =>
        Assert.Equal(82, PitRopeCrossing.RopeObjectId);
}
