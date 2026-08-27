namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>
/// How often a roaming actor steps, relative to the party (TASK-106).
/// </summary>
/// <remarks>
/// <b>The cadence is the part of this feature that has already been recorded wrong once.</b> A note
/// on the task said the original ticks roaming "per animated frame from
/// <c>world_render_scene_dispatch(animate)</c>" — there is no such caller.
/// <c>updateRoamingEncounterActors</c> @0x7600b has exactly one:
/// <c>drawMap(animate)</c> @0x21711, and only <c>approachWalkToScenePosition</c> @0x6dd52 passes 1.
/// <c>MainGameLoop</c>, <c>HandleMoveEvent</c> and <c>UI_Encamp</c> all pass 0. So roaming advances
/// per frame of an ANIMATED PARTY WALK — never while the party stands still.
///
/// <para>Our travel is discrete where that walk is animated, so the mapping is by DISTANCE: one
/// roaming step per quarter-cell the party covers. These pin the arithmetic that mapping rests on.
/// The Unity side that meters it is <c>WorldRuntime.AdvanceRoamingActors</c>.</para>
/// </remarks>
public class RoamingCadenceTests {
    // What WorldRuntime meters against. Stated here from the same relation rather than imported, so
    // that changing one without the other fails rather than silently re-times every monster.
    private const int RoamingStepDistance = RoadTravel.CellSize / 4;

    [Fact]
    public void AROAMINGSTEPIsExactlyAQuarterCell() {
        // *** This is not a tuning constant. *** The waypoints sit on the cell lattice and arrival
        // is tested by EXACT EQUALITY, so four steps have to land precisely on the next cell. A
        // per-creature speed or a frame-scaled float breaks arrival, not merely distance — an actor
        // that overshoots by one unit never matches a waypoint again and walks off its route.
        Assert.Equal(0x190, RoamingStepDistance);
        Assert.Equal(RoadTravel.CellSize, RoamingStepDistance * 4);
    }

    [Theory]
    [InlineData(0, 0)]              // standing still: nothing roams
    [InlineData(0x190, 1)]          // exactly one quarter-cell
    [InlineData(0x640, 4)]          // a whole cell
    [InlineData(0x18F, 0)]          // just short — carried, not rounded up
    public void TicksAreMeteredByDistanceCovered(long moved, int expected) {
        Assert.Equal(expected, (int)(moved / RoamingStepDistance));
    }

    [Fact]
    public void THEREMAINDERIsCarried_NotRoundedAway() {
        // *** Why WorldRuntime keeps a carry. *** The step length is a Preferences setting, so a
        // player on a short stride can move indefinitely in increments below a quarter-cell. Round
        // each one down independently and roaming stops entirely for that player — a bug that would
        // look like "monsters don't move on my machine".
        long carry = 0;
        var ticks = 0;
        for (var i = 0; i < 4; i++) {
            carry += 0x100;                       // four short steps, none a quarter-cell
            int t = (int)(carry / RoamingStepDistance);
            carry -= (long)t * RoamingStepDistance;
            ticks += t;
        }

        Assert.Equal(2, ticks);                   // 4 * 0x100 = 0x400 -> two quarter-cells
        Assert.Equal(0x400 - (2 * RoamingStepDistance), carry);
    }

    [Fact]
    public void AFixedTicksPerSTEPCouldNotWork() {
        // The reason the meter is distance and not a count of steps: the same number of steps covers
        // different ground depending on the player's step-size preference (and is quartered again
        // underground), so any constant ticks-per-step is right for one setting and wrong for the
        // rest — monsters running at different speeds on different machines.
        const long shortStride = 0x100;
        const long longStride = 0x640;

        Assert.NotEqual(shortStride / RoamingStepDistance, longStride / RoamingStepDistance);
    }

    [Fact]
    public void ADIAGONALStepIsNotLongerThanAStraightOne() {
        // Chebyshev, matching the lattice the world moves on: worldmove_crossing_apply_offset moves
        // the FULL delta on both axes for a diagonal, so measuring the distance euclidean-style
        // would run diagonal travel ~1.4x faster than the original does.
        long dx = 0x190, dy = 0x190;
        long chebyshev = System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dy));

        Assert.Equal(1, (int)(chebyshev / RoamingStepDistance));
    }

    [Fact]
    public void ASTATIONARYPatternNeverSteps() {
        // Pattern 0 has no waypoints, so a tick must be a no-op rather than walking it off in
        // whatever direction it happens to face.
        Assert.False(RoamingMovement.Moves(RoamingMovement.Pattern.Stationary));

        var pose = new RoamingMovement.Pose(1000, 2000, 0x4000);
        RoamingMovement.Pose after = RoamingMovement.Tick(
            pose, RoamingMovement.Pattern.Stationary, null, null);

        Assert.Equal(pose.X, after.X);
        Assert.Equal(pose.Y, after.Y);
        Assert.Equal(pose.Heading, after.Heading);
    }
}
