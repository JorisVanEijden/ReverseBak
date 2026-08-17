namespace GameData.Resources.Animation;

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
}
