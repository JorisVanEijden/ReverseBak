namespace BetrayalAtKrondor.Tests.Location;

using GameData.Resources.Location;
using Xunit;

/// <summary>
/// The rift-map screen's rules (<c>UI_teleportation</c> @0x4ee7e, <c>drawTeleportMenu</c> @0x4ecff).
/// What carries here: a temple you have not walked into is not on the map, the temple you are
/// standing in shows but cannot be chosen, and Malac's Cross refuses in two different voices.
/// </summary>
public class TeleportMenuTests {
    // ---- pins and rows ---------------------------------------------------------------------

    [Fact]
    public void PinActionIdsCoverTheTwelveTemplesAndNothingElse() {
        Assert.Equal(1, TeleportMenu.TempleForAction(129));
        Assert.Equal(12, TeleportMenu.TempleForAction(140));

        // 128 is not a pin: the original's test is strictly greater than 0x80.
        Assert.Equal(0, TeleportMenu.TempleForAction(128));
        Assert.Equal(0, TeleportMenu.TempleForAction(141));
        Assert.Equal(0, TeleportMenu.TempleForAction(TeleportMenu.CancelActionId));
    }

    [Fact]
    public void EveryTempleRoundTripsThroughItsActionId() {
        for (int temple = 1; temple <= TeleportMenu.TempleCount; temple++) {
            Assert.Equal(temple, TeleportMenu.TempleForAction(TeleportMenu.ActionIdForTemple(temple)));
        }
    }

    [Fact]
    public void TemplesAreOneBasedAndTheirRowsAreZeroBased() {
        Assert.Equal(0, TeleportMenu.DestinationIdForTemple(1));

        // The twelve pins are exactly the temple rows of TELEPORT.DAT; row 12 onwards is dialog-only.
        Assert.Equal(TeleportDestinationSet.LastTempleDestinationId,
            TeleportMenu.DestinationIdForTemple(TeleportMenu.TempleCount));
    }

    // ---- availability ----------------------------------------------------------------------

    [Fact]
    public void AnUnvisitedTempleIsNotOffered() =>
        Assert.False(TeleportMenu.IsOffered(temple: 3, currentTemple: 1, visited: false));

    [Fact]
    public void TheTempleYouAreStandingInIsNotOfferedEvenThoughItIsVisited() =>
        Assert.False(TeleportMenu.IsOffered(temple: 1, currentTemple: 1, visited: true));

    [Fact]
    public void AVisitedTempleElsewhereIsOffered() =>
        Assert.True(TeleportMenu.IsOffered(temple: 3, currentTemple: 1, visited: true));

    [Fact]
    public void VisitedFlagsAreTheGdsSceneEntryFlags() {
        // GdsScene writes 6480 + n on entry; this screen reads the same bit back.
        Assert.Equal(6481, TeleportMenu.VisitedFlagFor(1));
        Assert.Equal(6492, TeleportMenu.VisitedFlagFor(12));
    }

    [Fact]
    public void KnowingOnlyThisTempleOffersNothing() =>
        Assert.False(TeleportMenu.AnyDestinationOffered(currentTemple: 4, isVisited: t => t == 4));

    [Fact]
    public void KnowingOneOtherTempleIsEnoughToOpenTheMap() =>
        Assert.True(TeleportMenu.AnyDestinationOffered(currentTemple: 4, isVisited: t => t == 4 || t == 9));

    // ---- Malac's Cross -----------------------------------------------------------------------

    [Fact]
    public void TheChapelIsShutOnlyInItsOwnChapterAndOnlyUntilReopened() {
        Assert.True(TeleportMenu.ChapelIsClosed(chapter: 6, reopened: false));
        Assert.False(TeleportMenu.ChapelIsClosed(chapter: 6, reopened: true));
        Assert.False(TeleportMenu.ChapelIsClosed(chapter: 5, reopened: false));
        Assert.False(TeleportMenu.ChapelIsClosed(chapter: 7, reopened: false));
    }

    [Fact]
    public void TheTwoChapelRefusalsAreDifferentDialogs() =>
        // Standing in it vs aiming at it: same condition, different message. Collapsing them would
        // lose that the disturbance is local to Malac's Cross.
        Assert.NotEqual(TeleportMenu.ChapelRefusesServiceDialog, TeleportMenu.ChapelUnreachableDialog);

    // ---- markers -----------------------------------------------------------------------------

    [Fact]
    public void TheTempleYouAreInStillShowsItsMarkerDespiteNotBeingOffered() =>
        Assert.Equal(TeleportMenu.SourcePinIcon,
            TeleportMenu.PinIcon(temple: 2, currentTemple: 2, hoveredTemple: 0, offered: false));

    [Fact]
    public void TheHoveredDestinationGetsItsOwnMarker() =>
        Assert.Equal(TeleportMenu.DestinationPinIcon,
            TeleportMenu.PinIcon(temple: 5, currentTemple: 2, hoveredTemple: 5, offered: true));

    [Fact]
    public void OtherOfferedDestinationsGetThePlainMarker() =>
        Assert.Equal(TeleportMenu.OfferedPinIcon,
            TeleportMenu.PinIcon(temple: 5, currentTemple: 2, hoveredTemple: 7, offered: true));

    [Fact]
    public void AnUnofferedPinDrawsNothingAtAll() =>
        Assert.Equal(-1, TeleportMenu.PinIcon(temple: 5, currentTemple: 2, hoveredTemple: 0, offered: false));

    [Fact]
    public void SourceOutranksHoverWhenTheyCollide() =>
        // Cannot happen through the UI (the source is never offered), but the original's ordering
        // resolves it this way and a port that reversed it would blank the "you are here" marker.
        Assert.Equal(TeleportMenu.SourcePinIcon,
            TeleportMenu.PinIcon(temple: 2, currentTemple: 2, hoveredTemple: 2, offered: true));

    [Fact]
    public void TheSparkCyclesFiveFrames() {
        Assert.Equal(16, TeleportMenu.SparkIcon(0));
        Assert.Equal(20, TeleportMenu.SparkIcon(4));
        Assert.Equal(16, TeleportMenu.SparkIcon(5));
    }

    // ---- the flight --------------------------------------------------------------------------

    [Fact]
    public void TheFlightStartsAndLandsOnTheStraightLine() {
        Assert.Equal(0, TeleportMenu.FlightArcOffset(step: 0, length: 90));
        Assert.Equal(0, TeleportMenu.FlightArcOffset(step: 90, length: 90));
    }

    [Fact]
    public void TheArcPeaksAtASixthOfTheFlightsLength() =>
        Assert.Equal(90 / 6, TeleportMenu.FlightArcOffset(step: 45, length: 90));

    [Fact]
    public void TheArcBowsOnceRatherThanSnaking() {
        // Half a sine period: strictly rising to the midpoint, never negative.
        int previous = -1;
        for (int step = 0; step <= 45; step++) {
            int offset = TeleportMenu.FlightArcOffset(step, length: 90);
            Assert.True(offset >= previous, $"step {step} dipped");
            Assert.True(offset >= 0, $"step {step} went negative");
            previous = offset;
        }
    }

    [Fact]
    public void AZeroLengthFlightDoesNotDivideByZero() =>
        Assert.Equal(0, TeleportMenu.FlightArcOffset(step: 0, length: 0));

    // ---- fares from the real map ---------------------------------------------------------------
    //
    // REQ_TELE ships its pins in CANONICAL 1600x1200, but the fare is measured in the original's
    // 320x200 pixels — and that scale is anisotropic (x5 across, x6 down). Divide back before
    // measuring or every north-south journey is mispriced against every east-west one. These are the
    // reference figures, hand-checked against the disassembly; TeleportScreen.FareTo must reproduce
    // them from the loaded REQ.

    private const int BaseCost = 20;
    private const int PerUnit = 3;

    // Canonical pin positions straight out of generated/REQ/REQ_TELE.json.
    private static long Fare((int X, int Y) from, (int X, int Y) to) =>
        TeleportCost.Price(from.X / 5, from.Y / 6, to.X / 5, to.Y / 6, BaseCost, PerUnit);

    [Fact]
    public void AShortHopFromSungToAstalon() {
        // (930,816) -> (945,894) is VGA (186,136) -> (189,149): d(3,13) -> 13 + 3*3/8 = 14.
        Assert.Equal(62, Fare((930, 816), (945, 894)));
    }

    [Fact]
    public void ALongHaulFromSungToIshap() {
        // (930,816) -> (830,468) is VGA (186,136) -> (166,78): d(20,58) -> 58 + 20*3/8 = 65.
        Assert.Equal(215, Fare((930, 816), (830, 468)));
    }

    [Fact]
    public void MeasuringInCanonicalSpaceWouldMispriceIt() =>
        // The trap this guards: same two pins, canonical coordinates, no unscaling. It does not just
        // come out bigger — the 5-vs-6 stretch changes the shape of the distance too.
        Assert.NotEqual(
            Fare((930, 816), (830, 468)),
            TeleportCost.Price(930, 816, 830, 468, BaseCost, PerUnit));

    // ---- help --------------------------------------------------------------------------------

    [Fact]
    public void CancelAndPinsAskForDifferentHelpTopics() {
        Assert.Equal(0, TeleportMenu.HelpTopicFor(TeleportMenu.CancelActionId));
        Assert.Equal(1, TeleportMenu.HelpTopicFor(TeleportMenu.ActionIdForTemple(3)));
    }
}
