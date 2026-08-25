namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>
/// The overworld patrol update. The exact-equality waypoint test is the fragile part — everything
/// else follows from it.
/// </summary>
public class RoamingMovementTests {
    [Fact]
    public void ArrivingAtAWaypointIsExactEqualityNotProximity() {
        // The actor turns only when it lands precisely on the waypoint. One unit out and it walks
        // straight past, which is what a port using floats or a different step size would do
        // every single time.
        Assert.True(RoamingMovement.IsAtWaypoint(4000, -2400, 4000, -2400));
        Assert.False(RoamingMovement.IsAtWaypoint(4001, -2400, 4000, -2400));
        Assert.False(RoamingMovement.IsAtWaypoint(4000, -2399, 4000, -2400));
    }

    [Fact]
    public void BothAxesMustMatch() {
        Assert.False(RoamingMovement.IsAtWaypoint(4000, 0, 4000, -2400));
        Assert.False(RoamingMovement.IsAtWaypoint(0, -2400, 4000, -2400));
    }

    [Fact]
    public void OnlyTheStationaryPatternStandsStill() {
        Assert.False(RoamingMovement.Moves(RoamingMovement.Pattern.Stationary));
        Assert.True(RoamingMovement.Moves(RoamingMovement.Pattern.BackAndForth));
        Assert.True(RoamingMovement.Moves(RoamingMovement.Pattern.CircuitTurningNegative));
        Assert.True(RoamingMovement.Moves(RoamingMovement.Pattern.CircuitTurningPositive));
        Assert.True(RoamingMovement.Moves(RoamingMovement.Pattern.RoadFollowing));
    }

    [Fact]
    public void TheTwoEndedPatternsReadOnlyTwoWaypoints() {
        // A slot may carry four and have two of them never looked at.
        Assert.Equal(2, RoamingMovement.WaypointCount(RoamingMovement.Pattern.BackAndForth));
        Assert.Equal(2, RoamingMovement.WaypointCount(RoamingMovement.Pattern.RoadFollowing));
        Assert.Equal(4, RoamingMovement.WaypointCount(RoamingMovement.Pattern.CircuitTurningNegative));
        Assert.Equal(4, RoamingMovement.WaypointCount(RoamingMovement.Pattern.CircuitTurningPositive));
        Assert.Equal(0, RoamingMovement.WaypointCount(RoamingMovement.Pattern.Stationary));
    }

    [Fact]
    public void TheTwoEndedPatternsAboutFaceAndTheCircuitsCorner() {
        Assert.Equal(RoamingMovement.HalfTurn,
            RoamingMovement.TurnOnReach(RoamingMovement.Pattern.BackAndForth));
        Assert.Equal(RoamingMovement.HalfTurn,
            RoamingMovement.TurnOnReach(RoamingMovement.Pattern.RoadFollowing));
        Assert.Equal(-RoamingMovement.QuarterTurn,
            RoamingMovement.TurnOnReach(RoamingMovement.Pattern.CircuitTurningNegative));
        Assert.Equal(RoamingMovement.QuarterTurn,
            RoamingMovement.TurnOnReach(RoamingMovement.Pattern.CircuitTurningPositive));
    }

    [Fact]
    public void TheTwoCircuitsDifferOnlyInWhichWayTheyCorner() {
        Assert.Equal(
            RoamingMovement.WaypointCount(RoamingMovement.Pattern.CircuitTurningNegative),
            RoamingMovement.WaypointCount(RoamingMovement.Pattern.CircuitTurningPositive));
        Assert.Equal(
            -RoamingMovement.TurnOnReach(RoamingMovement.Pattern.CircuitTurningNegative),
            RoamingMovement.TurnOnReach(RoamingMovement.Pattern.CircuitTurningPositive));
    }

    [Fact]
    public void FourCornersOfAQuarterTurnComeBackToTheStartingHeading() {
        int turn = RoamingMovement.TurnOnReach(RoamingMovement.Pattern.CircuitTurningPositive);

        Assert.Equal(0, unchecked((ushort)(turn * 4)));
    }

    [Fact]
    public void AnAboutFaceTwiceIsNoTurnAtAll() {
        Assert.Equal(0, unchecked((ushort)(RoamingMovement.HalfTurn * 2)));
    }

    [Fact]
    public void OnlyPatternsOneToFourAreActedOn() {
        // Zero falls through the updater's dispatch entirely, which is what makes it stationary
        // rather than a pattern with no behaviour.
        Assert.False(RoamingMovement.IsKnown(0));
        Assert.True(RoamingMovement.IsKnown(1));
        Assert.True(RoamingMovement.IsKnown(4));
        Assert.False(RoamingMovement.IsKnown(5));
        Assert.False(RoamingMovement.IsKnown(-1));
    }

    [Fact]
    public void OnlyTheWalkingKindRoams() {
        // Standing actors are drawn and never updated, so a pattern set on one does nothing.
        Assert.Equal(EncounterActorPose.WalkingKind, RoamingMovement.RoamingKind);
        Assert.NotEqual(EncounterActorPose.StandingKind, RoamingMovement.RoamingKind);
    }

    [Fact]
    public void ABlockedRoadTurnsTheActorRoundRatherThanStoppingIt() {
        Assert.Equal(RoamingMovement.HalfTurn, RoamingMovement.BlockedTurn);
    }

    [Fact]
    public void OnlyTwoOfTheRoadStepOutcomesHandOverTheHeading() {
        Assert.True(RoamingMovement.AdoptsRoadHeading(2));
        Assert.True(RoamingMovement.AdoptsRoadHeading(3));
        Assert.False(RoamingMovement.AdoptsRoadHeading(0));
        Assert.False(RoamingMovement.AdoptsRoadHeading(1));
        Assert.False(RoamingMovement.AdoptsRoadHeading(4));
    }

    // ---- from the C the earlier note said did not exist (RGNENC.C:677) --------------------------

    [Fact]
    public void TheStepIsExactlyAQuarterCell_whichIsWhyArrivalCanBeExactEquality() {
        // The waypoints sit on the cell lattice, so four steps land precisely on the next one.
        // A per-creature speed or a frame-scaled float breaks the arrival test, not the distance.
        Assert.Equal(RoadTravel.CellSize / 4, RoamingMovement.StepDistance);
        Assert.Equal(0x190, RoamingMovement.StepDistance);

        const int start = RoadTravel.HalfCell;
        int x = start;
        for (var tick = 0; tick < 4; tick++) {
            x += RoamingMovement.Step(0x4000).Dx;   // due west: -delta on x, 0 on y
        }
        Assert.Equal(start - RoadTravel.CellSize, x);
        Assert.True(RoamingMovement.IsAtWaypoint(x, 0, start - RoadTravel.CellSize, 0));
    }

    [Fact]
    public void TheStepUsesTheSameAxisOffsetRoadTravelDoes_diagonalsMoveBothAxesFully() {
        // Not trigonometry: a diagonal covers the full delta on each axis, so it is ~1.41x an
        // orthogonal step. Resolving it with sin/cos drifts off the integer lattice.
        Assert.Equal(RoadTravel.AxisOffset(0x2000, RoamingMovement.StepDistance),
            RoamingMovement.Step(0x2000));

        (int dx, int dy) = RoamingMovement.Step(0x2000);   // 45 degrees
        Assert.Equal(RoamingMovement.StepDistance, System.Math.Abs(dx));
        Assert.Equal(RoamingMovement.StepDistance, System.Math.Abs(dy));
    }

    [Fact]
    public void OnlyRoadFollowingCanHaveItsStepREFUSED() {
        // *** Patterns 1-3 apply the offset outright, with no walkability test at all. *** A
        // patrolling monster walks through whatever is in its way, and running patrols through the
        // party's collision instead would strand them wherever an authored route clips scenery.
        Assert.True(RoamingMovement.StepCanBeBlocked(RoamingMovement.Pattern.RoadFollowing));
        Assert.False(RoamingMovement.StepCanBeBlocked(RoamingMovement.Pattern.BackAndForth));
        Assert.False(RoamingMovement.StepCanBeBlocked(RoamingMovement.Pattern.CircuitTurningNegative));
        Assert.False(RoamingMovement.StepCanBeBlocked(RoamingMovement.Pattern.CircuitTurningPositive));
        Assert.False(RoamingMovement.StepCanBeBlocked(RoamingMovement.Pattern.Stationary));
    }

    [Fact]
    public void TheRoadIsConsultedOnlyAtACellCentre() {
        // The sweep runs only when BOTH coordinates are exactly half a cell in; anywhere else the
        // outcome is reported as nothing without probing. So a follower ignores bends for three
        // ticks out of four and takes them on the fourth.
        Assert.True(RoamingMovement.ConsidersTheRoadAt(RoadTravel.HalfCell, RoadTravel.HalfCell));
        Assert.True(RoamingMovement.ConsidersTheRoadAt(
            (5 * RoadTravel.CellSize) + RoadTravel.HalfCell, RoadTravel.HalfCell));

        Assert.False(RoamingMovement.ConsidersTheRoadAt(RoadTravel.HalfCell, 0));
        Assert.False(RoamingMovement.ConsidersTheRoadAt(0, RoadTravel.HalfCell));
        Assert.False(RoamingMovement.ConsidersTheRoadAt(
            RoadTravel.HalfCell + RoamingMovement.StepDistance, RoadTravel.HalfCell));
    }

    [Fact]
    public void AForkReadsAsNoRoadAtAll() {
        // worldmove_sweep_adjacent_cells returns 0 for "nothing" AND for "a second continuation".
        // For a roaming actor both mean: keep the heading and walk on.
        Assert.Equal(0, (int)RoamingMovement.RoadOutcome.NoneOrForked);
        Assert.False(RoamingMovement.AdoptsRoadHeading((int)RoamingMovement.RoadOutcome.NoneOrForked));
    }

    [Fact]
    public void RunningStraightOnDoesNotCountAsABend() {
        // The original returns its `mode` argument (1 for the actor updater) when the road
        // continues ahead, and the reported target is the CURRENT heading — so excluding it changes
        // nothing. Pinned because that exclusion otherwise looks arbitrary.
        Assert.Equal(1, (int)RoamingMovement.RoadOutcome.StraightOn);
        Assert.False(RoamingMovement.AdoptsRoadHeading((int)RoamingMovement.RoadOutcome.StraightOn));
        Assert.True(RoamingMovement.AdoptsRoadHeading((int)RoamingMovement.RoadOutcome.BendsOneWay));
        Assert.True(RoamingMovement.AdoptsRoadHeading((int)RoamingMovement.RoadOutcome.BendsTheOther));
    }
}
