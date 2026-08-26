namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>
/// One roaming actor's tick.
/// </summary>
public class RoamingMovementTickTests {
    // Heading 0 steps along +Y by a quarter cell, which is what makes the arithmetic below readable.
    private const ushort North = 0;
    private static readonly int Step = RoamingMovement.StepDistance;

    private static RoamingMovement.Pose At(long x, long y, ushort heading = North) =>
        new RoamingMovement.Pose(x, y, heading);

    [Fact]
    public void AStationaryActorDoesNotMoveOrTurn() {
        RoamingMovement.Pose after = RoamingMovement.Tick(At(100, 200, 0x1234),
            RoamingMovement.Pattern.Stationary, new long[] { 100 }, new long[] { 200 });

        Assert.Equal(100, after.X);
        Assert.Equal(200, after.Y);
        Assert.Equal(0x1234, after.Heading);
    }

    [Fact]
    public void ItStepsAQuarterCellAndKeepsItsHeadingAwayFromAWaypoint() {
        RoamingMovement.Pose after = RoamingMovement.Tick(At(0, 0),
            RoamingMovement.Pattern.BackAndForth, new long[] { 0 }, new long[] { 999999 });

        Assert.Equal(0, after.X);
        Assert.Equal(Step, after.Y);
        Assert.Equal(North, after.Heading);
    }

    [Fact]
    public void LandingOnAWaypointABOUTFACESOnTheTwoEndedRoute() {
        RoamingMovement.Pose after = RoamingMovement.Tick(At(0, 0),
            RoamingMovement.Pattern.BackAndForth, new long[] { 0 }, new long[] { Step });

        Assert.Equal(Step, after.Y);
        Assert.Equal(RoamingMovement.HalfTurn, after.Heading);
    }

    [Fact]
    public void TheTurnHappensAFTERTheStep_NotBefore() {
        // Turning first would move the actor away from the waypoint it was about to reach, and the
        // route would unravel from the first tick.
        RoamingMovement.Pose after = RoamingMovement.Tick(At(0, 0),
            RoamingMovement.Pattern.BackAndForth, new long[] { 0 }, new long[] { Step });

        Assert.Equal(Step, after.Y);   // it did arrive
        Assert.NotEqual(North, after.Heading);
    }

    [Fact]
    public void ANYWaypointOfTheRouteCounts_NotJustTheNextOne() {
        // There is no current-waypoint index; the actor compares against every waypoint it has.
        RoamingMovement.Pose after = RoamingMovement.Tick(At(0, 0),
            RoamingMovement.Pattern.CircuitTurningPositive,
            new long[] { 5000, 6000, 0, 7000 }, new long[] { 5000, 6000, Step, 7000 });

        Assert.Equal(RoamingMovement.QuarterTurn, after.Heading);
    }

    [Fact]
    public void TheCircuitsTurnOppositeWays() {
        long[] wx = { 0 };
        long[] wy = { Step };

        Assert.Equal(unchecked((ushort)-RoamingMovement.QuarterTurn),
            RoamingMovement.Tick(At(0, 0), RoamingMovement.Pattern.CircuitTurningNegative, wx, wy).Heading);
        Assert.Equal(RoamingMovement.QuarterTurn,
            RoamingMovement.Tick(At(0, 0), RoamingMovement.Pattern.CircuitTurningPositive, wx, wy).Heading);
    }

    [Fact]
    public void PatternsOneToThreeTakeTheirStepUNCONDITIONALLY() {
        // No walkability test of any kind — a patrolling monster walks through whatever is in its
        // way. Faithful, and pinned so a port does not quietly run patrols through collision.
        var refused = 0;
        RoamingMovement.Pose after = RoamingMovement.Tick(At(0, 0),
            RoamingMovement.Pattern.CircuitTurningPositive, new long[] { 9 }, new long[] { 9 },
            _ => { refused++; return new RoamingMovement.RoadStep(false, 0, 0); });

        Assert.True(refused == 0, "the road step is not consulted for these patterns");
        Assert.Equal(Step, after.Y);
    }

    [Fact]
    public void ABLOCKEDRoadFollowerTurnsAroundInsteadOfStopping() {
        RoamingMovement.Pose after = RoamingMovement.Tick(At(1000, 2000),
            RoamingMovement.Pattern.RoadFollowing, new long[] { 0 }, new long[] { 0 },
            _ => new RoamingMovement.RoadStep(false, 0, 0));

        Assert.Equal(1000, after.X);
        Assert.Equal(2000, after.Y);
        Assert.Equal(RoamingMovement.BlockedTurn, after.Heading);
    }

    [Fact]
    public void ARoadFollowerADOPTSTheSweepsHeadingOnABend() {
        RoamingMovement.Pose after = RoamingMovement.Tick(At(0, 0),
            RoamingMovement.Pattern.RoadFollowing, new long[] { 9 }, new long[] { 9 },
            _ => new RoamingMovement.RoadStep(true, 500, 600,
                (int)RoamingMovement.RoadOutcome.BendsOneWay, 0x2000));

        Assert.Equal(500, after.X);
        Assert.Equal(0x2000, after.Heading);
    }

    [Fact]
    public void ARRIVINGBeatsTheRoadsHeading() {
        // *** THE ORDERING. *** Reaching an end of the route about-faces, and that decision wins
        // over the bend the sweep just reported. Consulting the road first sends the actor onward
        // past the end of its own route.
        RoamingMovement.Pose after = RoamingMovement.Tick(At(0, 0),
            RoamingMovement.Pattern.RoadFollowing, new long[] { 500 }, new long[] { 600 },
            _ => new RoamingMovement.RoadStep(true, 500, 600,
                (int)RoamingMovement.RoadOutcome.BendsOneWay, 0x2000));

        Assert.Equal(RoamingMovement.HalfTurn, after.Heading);
    }

    [Fact]
    public void StraightOnAndAForkBothLeaveTheHeadingAlone() {
        foreach (RoamingMovement.RoadOutcome outcome in
                 new[] { RoamingMovement.RoadOutcome.StraightOn, RoamingMovement.RoadOutcome.NoneOrForked }) {
            RoamingMovement.Pose after = RoamingMovement.Tick(At(0, 0, 0x1111),
                RoamingMovement.Pattern.RoadFollowing, new long[] { 9 }, new long[] { 9 },
                _ => new RoamingMovement.RoadStep(true, 500, 600, (int)outcome, 0x2000));

            Assert.Equal(0x1111, after.Heading);
        }
    }

    [Fact]
    public void NoRoadStepAtAllRefusesRatherThanWalkingThroughTheWorld() {
        // A caller with no world to sweep must not let a road-follower move unchecked — the one
        // pattern whose step is meant to be tested would become the one that ignores everything.
        RoamingMovement.Pose after = RoamingMovement.Tick(At(10, 20),
            RoamingMovement.Pattern.RoadFollowing, new long[] { 0 }, new long[] { 0 });

        Assert.Equal(10, after.X);
        Assert.Equal(RoamingMovement.BlockedTurn, after.Heading);
    }
}
