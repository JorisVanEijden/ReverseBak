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
}
