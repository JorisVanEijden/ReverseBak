namespace GameData.Resources.Combat;

using System;
using System.Collections.Generic;

/// <summary>
/// The arc a thrown rock follows — <c>world_rndr_ranged_attack_anim</c> (WORLDHIT.C:585), the
/// <c>action_id == 0x14</c> branch.
/// </summary>
/// <remarks>
/// <b>A thrown rock is the one projectile that BOUNCES.</b> Every other ranged shot flies flat and
/// is destroyed on arrival; this one carries a vertical delta that gravity pulls down each step, and
/// when it reaches the ground the delta INVERTS and the rock skips onward. That is why
/// <see cref="RangedShotSound.RockImpactCue"/> (73, the landing) and this class's
/// <see cref="SkipCue"/> (72) are different sounds and not two names for one event: 73 plays once,
/// after the flight; 72 plays on every skip, and a long shot skips more than once.
///
/// <para><b>A MISS arcs differently from a hit, and that is deliberate in the original.</b> On a hit
/// the step count is known (distance over speed) and the initial delta is scaled to it, so the rock
/// is still airborne when it arrives. On a miss the step count is the
/// <see cref="MissStepCount"/> sentinel and the delta is a random <c>RND(0x14) - 5</c> — which can be
/// NEGATIVE, dropping the rock almost at once and skipping it along the ground until it leaves the
/// screen. Most shots miss, so the skipping is the common sight, not the rare one.</para>
///
/// <para><b>Heights are the original's integers, not a curve fitted to them.</b> The sequence is
/// exact: add the delta, clamp-and-invert at the ground, then subtract gravity — in that order. A
/// smooth parabola would put the skips in the wrong places, and the skips are what makes sound.</para>
/// </remarks>
public static class ThrownRockFlight {
    /// <summary>The shape/action id a thrown rock flies as.</summary>
    public const int ActionId = 0x14;

    /// <summary>World units advanced per step, from the AI's call site (CBTAITRN.C:131).</summary>
    public const int Speed = 0x5a;

    /// <summary>
    /// Launch height: the 200 every non-class projectile starts at, plus the 200 this branch adds.
    /// </summary>
    public const int LaunchHeight = 400;

    /// <summary>Numerator of the hit arc's initial delta, <c>400 / (steps + 4)</c>.</summary>
    public const int ArcNumerator = 400;

    /// <summary>Bias on the step count in that division — it also keeps a zero-step shot safe.</summary>
    public const int ArcStepBias = 4;

    /// <summary>Subtracted from the delta after every step.</summary>
    public const int Gravity = 6;

    /// <summary>Height the rock is clamped to when it reaches the ground.</summary>
    public const int GroundClamp = 1;

    /// <summary><c>audio_play(0x48)</c> — the skip, played on every ground contact.</summary>
    public const int SkipCue = 0x48;

    /// <summary>The step count a MISS uses: it flies until it leaves the screen.</summary>
    public const int MissStepCount = 10000;

    /// <summary>One step of the flight.</summary>
    public readonly struct Step {
        internal Step(int height, bool skipped) {
            Height = height;
            Skipped = skipped;
        }

        /// <summary>Height above the ground after this step.</summary>
        public int Height { get; }

        /// <summary>Whether the rock touched the ground on this step, which sounds <see cref="SkipCue"/>.</summary>
        public bool Skipped { get; }
    }

    /// <summary>Steps a hit takes: the distance over <see cref="Speed"/>.</summary>
    public static int StepCountForHit(int travelDistance) => travelDistance / Speed;

    /// <summary>
    /// The initial vertical delta for a shot that HITS.
    /// </summary>
    /// <remarks>
    /// Scaled to the step count so the rock is still up when it arrives — a short shot gets a
    /// steeper delta than a long one. A miss does not use this; see <see cref="MissStepCount"/>.
    /// </remarks>
    public static int ArcDeltaForHit(int stepCount) => ArcNumerator / (stepCount + ArcStepBias);

    /// <summary>The exclusive upper bound of a miss's delta roll — <c>RND(0x14)</c>.</summary>
    public const int MissDeltaRange = 0x14;

    /// <summary>Subtracted from that roll, which is what lets a miss start DOWNWARD.</summary>
    public const int MissDeltaBias = 5;

    /// <summary>
    /// The steps to draw and the launch delta for one shot.
    /// </summary>
    /// <param name="cellDx">Column difference between shooter and target.</param>
    /// <param name="cellDy">Row difference.</param>
    /// <param name="cellSize">The combat grid's cell size in world units — START.DAT's.</param>
    /// <param name="hit">Whether the shot hit, which chooses between the two deltas.</param>
    /// <param name="rnd">The combat RNG, called as <c>RND(0x14)</c>; only a miss uses it.</param>
    /// <remarks>
    /// <b>A miss's OVERSHOOT is deliberately not modelled.</b> The original gives a miss the
    /// <see cref="MissStepCount"/> sentinel so the rock flies past the target and skips on until it
    /// leaves the screen; the drawn flight here ends at the target, so the step count returned is
    /// the distance-based one either way and only the DELTA differs. That keeps the character of a
    /// miss — it drops early and skips — over the part of the flight that is drawn.
    /// <c>ponytail: miss overshoot not drawn; needs the flight to outlive its endpoint and exit on
    /// the screen bound, which is a change to the flight's contract rather than to this arithmetic.</c>
    /// </remarks>
    public static (int Steps, int ArcDelta) Launch(
        int cellDx, int cellDy, int cellSize, bool hit, Func<int, int> rnd) {
        int steps = StepCountForHit(
            World.WorldDistance.Octagonal(cellDx * cellSize, cellDy * cellSize));

        return hit
            ? (steps, ArcDeltaForHit(steps))
            : (steps, (rnd?.Invoke(MissDeltaRange) ?? 0) - MissDeltaBias);
    }

    /// <summary>
    /// The height at each step, and where the rock skips.
    /// </summary>
    /// <param name="stepCount">How many steps to fly.</param>
    /// <param name="initialArcDelta">
    /// <see cref="ArcDeltaForHit"/> for a hit, or the miss's <c>RND(0x14) - 5</c>.
    /// </param>
    public static IReadOnlyList<Step> Arc(int stepCount, int initialArcDelta) {
        var steps = new List<Step>(stepCount < 0 ? 0 : stepCount);
        int height = LaunchHeight;
        int delta = initialArcDelta;

        for (int i = 0; i < stepCount; i++) {
            height += delta;
            bool skipped = height <= 0;
            if (skipped) {
                delta = -delta;
                height = GroundClamp;
            }
            steps.Add(new Step(height, skipped));
            delta -= Gravity;
        }

        return steps;
    }
}
