namespace GameData.Resources.Animation;

using System;

/// <summary>
/// The shape of the cutscene book-page turn — the pure half of the
/// <see cref="CutsceneDialogCommand.Kind.BookAnimation"/> command, which the script advances one
/// step at a time through <c>Arg2</c>.
/// </summary>
/// <remarks>
/// <b>THE STEP COUNT IS RE-DERIVED; THE CURVE IS NOT.</b> The turn running 0..<see
/// cref="CutsceneDialogCommand.MaxBookStep"/> comes from the command's own data. The narrowing-plus-
/// skew that renders it does not: <c>skew</c> appears nowhere in the reconstructed source, so the
/// arch below and especially <see cref="MaxSkewFraction"/> are <b>our approximation of a page
/// turning</b>, not a port of one. Extracted here so that is visible and calibratable in one place
/// rather than buried in a draw call (see TASK-30, which owns the general calibration problem).
///
/// <para><b>What the tests pin is therefore the SHAPE, not the magnitude</b> — the page is full width
/// at the start and closed at the end, and the skew is zero at both ends and symmetric about the
/// middle. Those survive recalibration; a test asserting 0.1 would just make the guess harder to
/// correct.</para>
/// </remarks>
public static class BookPageTurn {
    /// <summary>Steps in a full turn — <c>MaxBookStep</c> is inclusive, so the divisor is one more.</summary>
    public const int TotalSteps = CutsceneDialogCommand.MaxBookStep + 1;

    /// <summary>
    /// Peak skew as a fraction of the drawn page's height.
    /// </summary>
    /// <remarks>
    /// <b>Not RE-derived — a chosen value.</b> Nothing in the reconstructed source skews this
    /// bitmap, so there is no original number to match. Kept named and public so recalibrating it is
    /// a one-line change with an obvious blast radius.
    /// </remarks>
    public const double MaxSkewFraction = 0.1;

    /// <summary>
    /// How wide the page is drawn at <paramref name="step"/>, as a fraction of its full width:
    /// 1 at the start, 0 when the turn completes.
    /// </summary>
    /// <remarks>
    /// <b>Clamped, because the caller's step comes from script data.</b> A mod-authored
    /// <c>Arg2</c> past the last step would otherwise give a NEGATIVE width and draw the page
    /// inside out. <see cref="CutsceneDialogCommand.KindOf"/> already rejects those before they
    /// reach here, so the clamp is defence at the second layer rather than the only one.
    /// </remarks>
    public static double WidthFactorAt(int step) {
        double clamped = Clamp(step);
        return (TotalSteps - clamped) / TotalSteps;
    }

    /// <summary>
    /// The skew at <paramref name="step"/> as a fraction of <see cref="MaxSkewFraction"/> — a
    /// half-sine arch: 0 at both ends, 1 in the middle.
    /// </summary>
    /// <remarks>
    /// <b>Zero at BOTH ends is the load-bearing property</b>, not the peak. A page that is flat when
    /// it starts turning and flat when it finishes is what makes consecutive turns join up; a curve
    /// that ended mid-skew would visibly snap on the last frame.
    /// </remarks>
    public static double SkewFractionAt(int step) =>
        Math.Sin(Clamp(step) / TotalSteps * Math.PI);

    /// <summary>The skew at <paramref name="step"/> in the same units as <paramref name="pageHeight"/>.</summary>
    public static double SkewAt(int step, double pageHeight) =>
        pageHeight * MaxSkewFraction * SkewFractionAt(step);

    private static double Clamp(int step) =>
        step < 0 ? 0 : (step > TotalSteps ? TotalSteps : step);
}
