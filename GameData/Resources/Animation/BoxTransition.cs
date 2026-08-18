namespace GameData.Resources.Animation;

/// <summary>
/// The geometry of the two box screen-transitions — <c>anim_screenTransitionEffect</c> @0x53ab5,
/// TTM opcodes 0xA034 (box-in) and 0xA094 (box-out).
/// </summary>
/// <remarks>
/// Both walk the same rectangle: one centred on the transitioning area, growing from nothing
/// (box-out) or shrinking to it (box-in). The two axes are stepped by different amounts so that
/// however oblong the area is, both reach their extreme on the same pass — the original scales each
/// axis by <c>half / max(halfWidth, halfHeight)</c> in thousandths and takes
/// <c>max(halfWidth, halfHeight)</c> passes.
///
/// <para>What each does with that rectangle differs: box-out reveals INSIDE it and box-in reveals
/// OUTSIDE, so the visible box-in is a hole closing rather than a box growing.</para>
/// </remarks>
public static class BoxTransition {
    /// <summary>A rectangle at one step of the walk.</summary>
    public readonly struct Step {
        public Step(int x, int y, int width, int height) {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }
    }

    /// <summary>
    /// How many passes the walk takes: the longer half-axis, so the shorter one moves in
    /// fractional steps rather than finishing early and standing still.
    /// </summary>
    public static int StepCount(int width, int height) {
        int steps = System.Math.Max(width / 2, height / 2);

        return steps < 1 ? 1 : steps;
    }

    /// <summary>
    /// The rectangle on pass <paramref name="step"/> (1-based) of <paramref name="stepCount"/>.
    /// </summary>
    /// <remarks>
    /// Always centred on the area. Growing at step 1 it is nearly a point and at the last pass it
    /// is the whole area; the caller runs the steps in reverse for box-in, which is the only
    /// difference between the two opcodes' walks.
    /// </remarks>
    public static Step RectAt(int x, int y, int width, int height, int step, int stepCount) {
        if (stepCount < 1) {
            stepCount = 1;
        }
        if (step < 0) {
            step = 0;
        }
        if (step > stepCount) {
            step = stepCount;
        }

        int halfWidth = width / 2;
        int halfHeight = height / 2;

        // Thousandths, as the original does it, so an oblong area's two axes stay in proportion.
        int spanX = halfWidth * 1000 / stepCount * step / 1000;
        int spanY = halfHeight * 1000 / stepCount * step / 1000;

        return new Step(x + halfWidth - spanX, y + halfHeight - spanY, spanX * 2, spanY * 2);
    }
}
