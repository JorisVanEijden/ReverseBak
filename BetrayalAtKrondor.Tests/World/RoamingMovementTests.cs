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
}
