namespace BetrayalAtKrondor.Tests.World;

using System.Collections.Generic;
using GameData.Resources.World;
using Xunit;

/// <summary>
/// The road step a <see cref="RoamingMovement.Pattern.RoadFollowing"/> actor takes —
/// <c>worldmove_crossing_check_8dir</c> as RGNENC.C:740 calls it.
/// </summary>
/// <remarks>
/// <b>Nothing supplied this until now, so all 149 shipped pattern-4 actors turned on the spot.</b>
/// <see cref="RoamingMovement.Tick"/> takes the step as a delegate and a null one refuses
/// everything — the safe half of the choice, and a permanent one until something filled it.
/// </remarks>
public class RoamingMovementRoadStepTests {
    private const int Cell = RoadTravel.CellSize;
    private const int Half = RoadTravel.HalfCell;
    private static readonly int Step = RoamingMovement.StepDistance;

    private const ushort North = 0;
    private const ushort West = 0x4000;

    /// <summary>
    /// Which cell a world coordinate falls in. <b>Floor, not truncation.</b>
    /// </summary>
    /// <remarks>
    /// A plain <c>x / Cell</c> rounds toward zero, so cells -1 and 0 both read as 0 and a fixture
    /// road wraps around the origin — which made the sweep in <see cref="ABendHandsOverItsHeading"/>
    /// see a second continuation and report a fork. The probes genuinely reach negative coordinates
    /// (a westward probe from the first column does), so the helper has to handle them.
    /// </remarks>
    private static int CellOf(int v) => (int)System.Math.Floor(v / (double)Cell);

    /// <summary>Road on the whole column of cells at <paramref name="cellX"/>, running north.</summary>
    private static System.Func<int, int, bool> ColumnOfRoad(int cellX) =>
        (x, _) => CellOf(x) == cellX;

    private static RoamingMovement.Pose At(long x, long y, ushort heading = North) =>
        new RoamingMovement.Pose(x, y, heading);

    [Fact]
    public void AStepAlongTheRoadAdvancesAQuarterCell() {
        RoamingMovement.RoadStep step =
            RoamingMovement.RoadStepFor(At(Half, Half), ColumnOfRoad(0));

        Assert.True(step.Moved);
        Assert.Equal(Half, step.X);
        Assert.Equal(Half + Step, step.Y);
    }

    [Fact]
    public void NoRoadAheadRefusesTheStep() {
        // The column ends at cell 0, and the actor at the top of it faces the empty cell beyond.
        RoamingMovement.RoadStep step =
            RoamingMovement.RoadStepFor(At(Half, Half), (x, y) => CellOf(x) == 0 && CellOf(y) == 0);

        Assert.False(step.Moved);
        Assert.Equal(Half, step.Y);
    }

    [Fact]
    public void OffTheLatticeLineTheStepIsRefusedRatherThanRounded() {
        // Heading north preserves x == half a cell in. One unit off and the original bails before
        // it probes anything, which is what keeps travel on an exact integer lattice.
        RoamingMovement.RoadStep step =
            RoamingMovement.RoadStepFor(At(Half + 1, Half), ColumnOfRoad(0));

        Assert.False(step.Moved);
    }

    [Fact]
    public void ANonCompassHeadingIsRefused() {
        RoamingMovement.RoadStep step =
            RoamingMovement.RoadStepFor(At(Half, Half, 0x1000), ColumnOfRoad(0));

        Assert.False(step.Moved);
    }

    [Fact]
    public void ThePROBEComesFromTheCellCENTRE_notFromWhereTheActorStands() {
        // *** THE DISTINGUISHING CASE, and the one an obvious port gets wrong. *** Three ticks out
        // of four the actor is part-way between centres. Probing from its actual position samples a
        // cell short of the next centre; the original steps back half a cell against the heading and
        // snaps to that centre first. This road exists ONLY on the cells the centres fall in, so a
        // probe taken from the real position finds nothing and the actor stops dead on a road it is
        // standing on.
        var probed = new List<(int X, int Y)>();
        System.Func<int, int, bool> road = (x, y) => {
            probed.Add((x, y));
            return CellOf(x) == 0;
        };

        RoamingMovement.RoadStep step = RoamingMovement.RoadStepFor(At(Half, Half + Step), road);

        Assert.True(step.Moved);
        Assert.Contains((Half, Half + Cell), probed);
        Assert.DoesNotContain((Half, Half + Step + Cell), probed);
    }

    [Fact]
    public void TheSweepRunsONLYWhenTheStepLandsOnACellCentre() {
        // Landing off-centre reports NoneOrForked without probing, so a follower ignores bends for
        // three ticks out of four and can only ever turn where roads actually meet.
        RoamingMovement.RoadStep offCentre =
            RoamingMovement.RoadStepFor(At(Half, Half), ColumnOfRoad(0));
        Assert.Equal((int)RoamingMovement.RoadOutcome.NoneOrForked, offCentre.Outcome);

        RoamingMovement.RoadStep onCentre =
            RoamingMovement.RoadStepFor(At(Half, Half + Cell - Step), ColumnOfRoad(0));
        Assert.Equal(Half + Cell, onCentre.Y);
        Assert.Equal((int)RoamingMovement.RoadOutcome.StraightOn, onCentre.Outcome);
    }

    [Fact]
    public void ARoadRunningStraightOnDoesNotChangeTheHeading() {
        RoamingMovement.RoadStep step =
            RoamingMovement.RoadStepFor(At(Half, Half + Cell - Step), ColumnOfRoad(0));

        Assert.Equal((int)RoamingMovement.RoadOutcome.StraightOn, step.Outcome);
        Assert.False(RoamingMovement.AdoptsRoadHeading(step.Outcome),
            "straight on already reports the current heading, so adopting it would be a no-op");
    }

    [Fact]
    public void ABendHandsOverItsHeading() {
        // An L: the column up to cell (0,1), then west along row 1. Arriving at (0,1)'s centre the
        // sweep finds nothing straight ahead and one continuation to the west.
        System.Func<int, int, bool> elbow = (x, y) => {
            int cx = CellOf(x), cy = CellOf(y);
            return (cx == 0 && cy is >= 0 and <= 1) || (cy == 1 && cx is >= -1 and <= 0);
        };

        RoamingMovement.RoadStep step =
            RoamingMovement.RoadStepFor(At(Half, Half + Cell - Step), elbow);

        Assert.True(step.Moved);
        Assert.True(RoamingMovement.AdoptsRoadHeading(step.Outcome));
        Assert.Equal(West, step.Target);
    }

    [Fact]
    public void ARefusedStepTurnsTheActorRatherThanStoppingIt() {
        // Through Tick, which is where the refusal becomes an about-face.
        RoamingMovement.Pose after = RoamingMovement.Tick(
            At(Half, Half), RoamingMovement.Pattern.RoadFollowing, null, null,
            pose => RoamingMovement.RoadStepFor(pose, (_, _) => false));

        Assert.Equal(Half, after.X);
        Assert.Equal(Half, after.Y);
        Assert.Equal(RoamingMovement.BlockedTurn, after.Heading);
    }

    [Fact]
    public void ANullPredicateIsAProgrammingErrorRatherThanASilentRefusal() {
        // Tick's own null roadStep means "refuse"; a null predicate handed to the step itself is a
        // wiring mistake, and the two must not look the same.
        Assert.Throws<System.ArgumentNullException>(
            () => RoamingMovement.RoadStepFor(At(Half, Half), null));
    }
}
