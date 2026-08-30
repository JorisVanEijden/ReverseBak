namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>
/// Pit (15) carries TWO independent behaviours, which is what made a one-sentence summary of it
/// wrong rather than merely incomplete.
/// </summary>
/// <remarks>
/// <b>The polygon is walkable and the object is clickable.</b> Falling in is
/// <see cref="PitDescent"/>, delivered by the movement loop and already wired; swinging across is
/// <see cref="PitRopeCrossing"/> (<c>handle_Pit</c> @0x79c63), reached as <b>case 15 of the click
/// jump table</b> at <c>HandleEnvironmentInteraction_impl</c> @0x766ad. InteractionProfileTable
/// recorded that Pit "has no click at all", which would have stopped anyone giving it a profile
/// row — corrected 2026-08-30.
/// </remarks>
public class PitIsBothPathsTests {
    [Fact]
    public void TheTwoPathsAreModelledSeparatelyAndShareNoGate() {
        // Different gates entirely: descending needs no item and turns on the zone kind; crossing
        // needs a Rope counted across the whole party. Conflating them gives a pit that either
        // cannot be fallen into or cannot be crossed.
        Assert.Equal(82, PitRopeCrossing.RopeObjectId);
        Assert.NotEqual(PitRopeCrossing.OfferDialog, PitDescent.LandingDialogId);
    }

    [Fact]
    public void TheCrossingIsGatedOnARopeCheckedBEFORETheGeometry() {
        // The original counts the rope across the whole party first and only then looks at whether
        // the pit is axis-aligned — so a party with no rope is never offered the swing at all.
        Assert.Equal(82, PitRopeCrossing.RopeObjectId);
        Assert.True(PitRopeCrossing.OfferDialog > 0);
    }
    [Fact]
    public void ThreeOutcomes_NotTwo_AndOnlyTheGEOMETRYIsSilent() {
        // *** THE MODEL SAID NO ROPE MEANT NO EXPLANATION. IT DOES NOT. *** The rope count is read
        // first (@0x79cb1) and failing it shows dialog 198, "if we only had a rope". What returns
        // without a word is the geometry — an unusable angle or a party outside the band (@0x79e98).
        // Collapsing the two into "silent refusal" is what the old remark did, and a port built on
        // it swallows a line the game speaks.
        Assert.Equal(198, PitRopeCrossing.NoRopeDialog);
        Assert.Equal(177, PitRopeCrossing.ExamineDialog);
        Assert.NotEqual(PitRopeCrossing.NoRopeDialog, PitRopeCrossing.OutOfRopeDialog);

        // No rope: refused however well lined up.
        Assert.False(PitRopeCrossing.CanOffer(0, PitRopeCrossing.RotationEast, 1000, 1000));
        // Rope, but an unusable angle: also refused, and this is the silent one.
        Assert.False(PitRopeCrossing.CanOffer(1, 0x2000, 1000, 1000));
        // Rope and lined up: offered.
        Assert.True(PitRopeCrossing.CanOffer(1, PitRopeCrossing.RotationEast, 1000, 1000));
    }

    [Fact]
    public void ACrossingSPENDSARope_SoTheyAreLimited() {
        // The tail calls useItem(Rope) once the party lands (@0x7a11e) and only re-counts after, so
        // the "that's it for our good rope" line belongs to the LAST one. Two ropes means two
        // crossings, the first of them silent. Omitting the spend gives unlimited crossings.
        Assert.True(PitRopeCrossing.CrossingConsumesARope);
        Assert.Equal(0x114, PitRopeCrossing.OutOfRopeDialog);
    }

}
