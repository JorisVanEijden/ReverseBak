namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>The swing itself — the sag curve, the step and the cue (collision spec 4.6).</summary>
public class PitSwingTests {
    [Fact]
    public void TheRopeIsFlatOutsideTheSagBand() {
        // Six hundred units either side, and nothing beyond it — a sag applied across the whole
        // crossing would dip the party below the far lip.
        Assert.Equal(0, PitRopeCrossing.SagHeightAt(PitRopeCrossing.SagRadius));
        Assert.Equal(0, PitRopeCrossing.SagHeightAt(PitRopeCrossing.SagRadius + 1));
        Assert.Equal(0, PitRopeCrossing.SagHeightAt(-PitRopeCrossing.SagRadius));
    }

    [Fact]
    public void TheDipIsDeepestAtTheCentreAndSymmetric() {
        // *** THE SAG IS NEGATIVE, AND THAT IS THE POINT. *** The anchor sits BELOW the lip, so
        // the party dips into the chasm and rises to the far side; "deepest" means the SMALLEST
        // value, not the largest. Writing this the other way round is the mistake that made these
        // two tests contradict each other when they were first drafted.
        int centre = PitRopeCrossing.SagHeightAt(0);
        Assert.True(centre < 0, "the rope hangs below the lip");
        Assert.True(centre < PitRopeCrossing.SagHeightAt(300), "and deepest in the middle");
        Assert.True(PitRopeCrossing.SagHeightAt(300) < PitRopeCrossing.SagHeightAt(500),
            "rising towards the far side");
        Assert.Equal(PitRopeCrossing.SagHeightAt(300), PitRopeCrossing.SagHeightAt(-300));
    }

    [Fact]
    public void TheCentreDipMatchesTheOriginalsArithmeticExactly() {
        // z = 0x1C2 - isqrt(0x4DEF9 - d^2). At the centre that is 450 - isqrt(319737) = 450 - 565.
        // A NEGATIVE number, and that is not a bug to clamp away: the anchor sits below the lip, so
        // the party dips into the chasm before rising to the far side.
        Assert.Equal(PitRopeCrossing.SagAnchorHeight - 565, PitRopeCrossing.SagHeightAt(0));
    }

    [Fact]
    public void TheSquareRootTRUNCATES() {
        // The original's isqrt never rounds up. A float sqrt would give a subtly different dip at
        // every frame of the crossing, which is a visibly different swing rather than a rounding
        // detail.
        Assert.Equal(PitRopeCrossing.SagAnchorHeight - 565, PitRopeCrossing.SagHeightAt(0));
        // 565^2 = 319225 <= 319737 < 566^2 = 320356, so 565 is the truncated root.
        Assert.True(565 * 565 <= 0x4DEF9);
        Assert.True(566 * 566 > 0x4DEF9);
    }

    [Fact]
    public void TheCueFiresONCE_AtTheExactCentre() {
        // At the centre, not on entering the band — a band test plays it twice, once per side.
        Assert.True(PitRopeCrossing.PlaysSwingSound(0));
        Assert.False(PitRopeCrossing.PlaysSwingSound(PitRopeCrossing.SagRadius));
        Assert.False(PitRopeCrossing.PlaysSwingSound(-PitRopeCrossing.SagRadius));
        Assert.False(PitRopeCrossing.PlaysSwingSound(100));
    }

    [Fact]
    public void TheCrossingIsWalkedInStepsThatDivideTheSpan() {
        // Both the approach and the crossing move in the same increment, so the span has to be a
        // whole number of them or the party stops short of the landing point.
        Assert.Equal(100, PitRopeCrossing.StepUnits);
        Assert.Equal(0, PitRopeCrossing.CrossingSpan % PitRopeCrossing.StepUnits);
        Assert.Equal(0, PitRopeCrossing.SagRadius % PitRopeCrossing.StepUnits);
    }

    [Fact]
    public void TheOutOfRopeMessageIsNOTTheNoRopeRefusal() {
        // The gate says nothing at all when the party has no rope; this dialog belongs to the
        // consumption at the end of a crossing that was already offered.
        Assert.NotEqual(PitRopeCrossing.OfferDialog, PitRopeCrossing.OutOfRopeDialog);
        Assert.False(PitRopeCrossing.CanOffer(ropeCount: 0, PitRopeCrossing.RotationEast, 0, 0));
    }

    [Fact]
    public void OutsideTheBandTheSagAnswerIsASENTINEL_notAHeight() {
        // *** The trap. *** SagHeightAt returns 0 outside the band; the original writes saved_z
        // there — the walking eye. A caller taking the 0 literally drops the party to the floor for
        // the approach and snaps them onto the rope at the boundary.
        Assert.Equal(0, PitRopeCrossing.SagHeightAt(PitRopeCrossing.SagRadius));
        Assert.False(PitRopeCrossing.IsSagging(PitRopeCrossing.SagRadius));
        Assert.True(PitRopeCrossing.IsSagging(PitRopeCrossing.SagRadius - 1));
        Assert.True(PitRopeCrossing.IsSagging(0));
        Assert.True(PitRopeCrossing.IsSagging(-500), "the band is symmetric about the centre");
    }

    [Fact]
    public void TheReturnValueCannotSeparateTheTwoCases_whichIsWhyTheDistanceIsWhatIsTested() {
        // The curve genuinely passes through 0 partway out, so "SagHeightAt() == 0 means no sag" is
        // wrong for a real point on the rope as well as being unclear.
        var zeroInsideTheBand = false;
        for (var d = 0; d < PitRopeCrossing.SagRadius; d++) {
            if (PitRopeCrossing.SagHeightAt(d) == 0) {
                zeroInsideTheBand = true;
                break;
            }
        }
        Assert.True(zeroInsideTheBand);
    }

    [Theory]
    // Crossing along Y: 0 is +Y and 0x8000 is -Y.
    [InlineData(true, -900, 900, 0x0000)]
    [InlineData(true, 900, -900, 0x8000)]
    // Crossing along X: +X is 0xC000 and -X is 0x4000 — the circle runs anticlockwise, so the
    // intuitive 0x4000 for +X is the one that sends the swing the wrong way.
    [InlineData(false, -900, 900, 0xC000)]
    [InlineData(false, 900, -900, 0x4000)]
    public void TheHeadingPointsFromTheStartTowardTheLanding(
        bool crossingAxisIsY, int start, int landing, int expected) =>
        Assert.Equal((ushort)expected, PitRopeCrossing.CrossingHeading(crossingAxisIsY, start, landing));

    [Fact]
    public void TheHeadingAgreesWithTheOFFSETThatWillBeStepped() {
        // The one check that catches a sign flip: stepping along the returned heading has to move
        // the crossing coordinate toward the landing, not away from it.
        ushort north = PitRopeCrossing.CrossingHeading(crossingAxisIsY: true, -900, 900);
        Assert.True(RoadTravel.AxisOffset(north, PitRopeCrossing.StepUnits).Dy > 0);

        ushort east = PitRopeCrossing.CrossingHeading(crossingAxisIsY: false, -900, 900);
        Assert.True(RoadTravel.AxisOffset(east, PitRopeCrossing.StepUnits).Dx > 0);

        ushort west = PitRopeCrossing.CrossingHeading(crossingAxisIsY: false, 900, -900);
        Assert.True(RoadTravel.AxisOffset(west, PitRopeCrossing.StepUnits).Dx < 0);
    }
}
