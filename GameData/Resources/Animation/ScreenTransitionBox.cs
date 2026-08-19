namespace GameData.Resources.Animation;

using System.Collections.Generic;

/// <summary>
/// The step geometry of the two box screen transitions — <c>anim_screenTransitionEffect</c>
/// @0x53ab5, the arm at <c>loc_ovr153_1383</c> that A034 (box in) and A094 (box out) share.
/// </summary>
/// <remarks>
/// <b>One algorithm with a direction, not two effects.</b> Both opcodes reach the same arm and
/// differ only in which end the step counter starts from: box-out counts up from 1, box-in counts
/// down from the step count. Implementing them separately would duplicate the arithmetic and let
/// the two drift.
///
/// <para>This is the geometry only. What each step COPIES differs — box-out copies the growing
/// centred box, box-in copies the region outside the shrinking one — and that, along with which
/// buffer holds the incoming frame, belongs to the renderer.</para>
/// </remarks>
public static class ScreenTransitionBox {
    /// <summary>
    /// How many steps the transition takes.
    /// </summary>
    /// <remarks>
    /// <b>The LARGER half-extent, so the box always reaches the far edge.</b> The original takes
    /// <c>max(width/2, height/2)</c> — using the smaller one would finish while the long axis was
    /// still short of the edge, leaving a band of the old frame on screen.
    ///
    /// <para>Integer halving is deliberate: the original's <c>sar</c> discards the odd pixel, so an
    /// odd width and its even predecessor take the same number of steps.</para>
    /// </remarks>
    public static int StepCount(int width, int height) {
        int halfWidth = width / 2;
        int halfHeight = height / 2;

        return halfWidth > halfHeight ? halfWidth : halfHeight;
    }

    /// <summary>
    /// The centred box at a given step.
    /// </summary>
    /// <param name="x">Left of the transition region.</param>
    /// <param name="y">Top of the transition region.</param>
    /// <param name="width">Width of the transition region.</param>
    /// <param name="height">Height of the transition region.</param>
    /// <param name="step">
    /// The step, 1..<see cref="StepCount"/>. Box-out walks it upward, box-in downward — that is the
    /// only difference between the two effects.
    /// </param>
    /// <remarks>
    /// The half-extents scale linearly with the step, in the original's fixed point: it multiplies
    /// the half-extent by 1000, divides by the step count once up front, then multiplies by the
    /// step and divides by 1000 again. That rounding is reproduced rather than simplified to a
    /// float ratio, because it is what decides which pixel row the box lands on.
    /// </remarks>
    public static (int X, int Y, int Width, int Height) BoxAt(
        int x, int y, int width, int height, int step) {
        int halfWidth = width / 2;
        int halfHeight = height / 2;
        int steps = StepCount(width, height);
        if (steps <= 0) {
            return (x, y, 0, 0);
        }

        int centreX = x + halfWidth;
        int centreY = y + halfHeight;

        // The original's fixed point: (half * 1000 / steps) * step / 1000.
        int perStepX = halfWidth * 1000 / steps;
        int perStepY = halfHeight * 1000 / steps;
        int currentHalfWidth = perStepX * step / 1000;
        int currentHalfHeight = perStepY * step / 1000;

        return (centreX - currentHalfWidth, centreY - currentHalfHeight,
            currentHalfWidth * 2, currentHalfHeight * 2);
    }

    /// <summary>The step a box-out starts from — it grows outward from the centre.</summary>
    public const int BoxOutFirstStep = 1;

    /// <summary>The step a box-in starts from — it shrinks inward from the edge.</summary>
    public static int BoxInFirstStep(int width, int height) => StepCount(width, height);

    // ---------------------------------------------------------------- what each step copies
    // Read 2026-08-19 from the arm's two branches (0x53bd0 and 0x53c39). This file previously said
    // the copy rule "belongs to the renderer"; it is the original's behaviour rather than ours, so
    // it belongs here and the renderer only performs it.

    /// <summary>
    /// The region a box-OUT step copies: <b>the box itself</b>.
    /// </summary>
    /// <remarks>
    /// One copy per step, so the incoming frame grows out of the centre as a rectangle.
    /// </remarks>
    public static (int X, int Y, int Width, int Height) RevealedByBoxOut(
        int x, int y, int width, int height, int step) => BoxAt(x, y, width, height, step);

    /// <summary>
    /// The regions a box-IN step copies: <b>the FOUR strips around the box, not the box</b>.
    /// </summary>
    /// <remarks>
    /// <b>This is the whole difference between the two effects, and it is not the step direction
    /// alone.</b> Box-in issues four copies covering the transition area <i>outside</i> the current
    /// box — left, right, above, below — so the incoming frame arrives from every edge at once and
    /// the hole closes toward the centre. Reproducing it as "box-out with the loop reversed" gives
    /// a centre-out wipe played backwards, which is a different picture entirely.
    ///
    /// <para>Empty strips are returned rather than skipped, so a caller can copy all four without
    /// testing each: a zero-sized copy is a no-op wherever this is drawn.</para>
    /// </remarks>
    public static IEnumerable<(int X, int Y, int Width, int Height)> RevealedByBoxIn(
        int x, int y, int width, int height, int step) {
        (int left, int top, int boxWidth, int boxHeight) = BoxAt(x, y, width, height, step);
        int right = left + boxWidth;
        int bottom = top + boxHeight;

        yield return (x, y, left - x, height);
        yield return (right, y, (x + width) - right, height);
        yield return (left, y, boxWidth, top - y);
        yield return (left, bottom, boxWidth, (y + height) - bottom);
    }
}
