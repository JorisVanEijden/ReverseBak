namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using System.Collections.Generic;
using System.Linq;
using Xunit;

/// <summary>
/// The thrown rock's arc, and the skips that make cue 72 fire more than once.
/// </summary>
public class ThrownRockFlightTests {
    [Fact]
    public void ASHORTHitNeverReachesTheGround_soItNeverSkips() {
        // Four cells at the combat grid's 300 units: 13 steps, delta 400/17 = 23. The rock is still
        // 231 units up when it arrives. A model that skips here would sound a cue the player never
        // hears at close range, which is most crossbow fire.
        int steps = ThrownRockFlight.StepCountForHit(4 * 300);
        IReadOnlyList<ThrownRockFlight.Step> arc =
            ThrownRockFlight.Arc(steps, ThrownRockFlight.ArcDeltaForHit(steps));

        Assert.Equal(13, steps);
        Assert.Equal(23, ThrownRockFlight.ArcDeltaForHit(steps));
        Assert.DoesNotContain(arc, s => s.Skipped);
        Assert.All(arc, s => Assert.True(s.Height > 0));
    }

    [Fact]
    public void ALONGHitDoesReachTheGround_soTheCueIsNotDeadCode() {
        // The positive control for the test above: same rule, longer shot, and now it skips. Without
        // this pair "never skips" would be indistinguishable from a broken integrator.
        int steps = ThrownRockFlight.StepCountForHit(12 * 300);
        IReadOnlyList<ThrownRockFlight.Step> arc =
            ThrownRockFlight.Arc(steps, ThrownRockFlight.ArcDeltaForHit(steps));

        Assert.Contains(arc, s => s.Skipped);
    }

    [Fact]
    public void ADOWNWARDMissSkipsREPEATEDLY() {
        // A miss's delta is RND(0x14) - 5, so it can start negative — the rock drops almost at once
        // and then skips along the ground. This is the common case, because most shots miss, and it
        // is why the cue belongs on the ground contact rather than on arrival.
        IReadOnlyList<ThrownRockFlight.Step> arc = ThrownRockFlight.Arc(120, -5);

        Assert.True(arc.Count(s => s.Skipped) > 1,
            $"expected repeated skips, got {arc.Count(s => s.Skipped)}");
    }

    [Fact]
    public void TheGroundClampIsAPPLIEDBeforeTheHeightIsRecorded() {
        // The original clamps to 1 and inverts in that order, so no step ever reports a height at or
        // below zero. A port that recorded the raw sum would sink the sprite through the floor for
        // one frame on every skip.
        IReadOnlyList<ThrownRockFlight.Step> arc = ThrownRockFlight.Arc(200, -5);

        Assert.All(arc, s => Assert.True(s.Height > 0, $"height {s.Height} is at or below the ground"));
        Assert.All(arc.Where(s => s.Skipped),
            s => Assert.Equal(ThrownRockFlight.GroundClamp, s.Height));
    }

    [Fact]
    public void GravityIsAppliedAFTERTheStep_notBefore() {
        // Order matters: the first step moves by the full initial delta. Subtracting gravity first
        // would lose 6 units of launch on every shot and shift every skip one step earlier.
        IReadOnlyList<ThrownRockFlight.Step> arc = ThrownRockFlight.Arc(1, 23);

        Assert.Equal(ThrownRockFlight.LaunchHeight + 23, arc[0].Height);
    }

    [Fact]
    public void TheSkipCueIsNOTTheImpactCue() {
        // 72 is the skip and 73 the landing; RangedShotSound owns the second. Collapsing them was
        // the reading TASK-144 recorded, and it turns a repeated sound into a single one.
        Assert.NotEqual(RangedShotSound.RockImpactCue, ThrownRockFlight.SkipCue);
        Assert.Equal(72, ThrownRockFlight.SkipCue);
        Assert.Equal(73, RangedShotSound.RockImpactCue);
    }
}
