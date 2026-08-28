namespace GameData.Resources.Animation;

using System;

/// <summary>
/// Where a freely-rotated cutscene image lands: how big its axis-aligned bounding box becomes, and
/// the top-left that keeps it pivoting about the point the script named.
/// </summary>
/// <remarks>
/// <b>Rotation is real, not an embellishment.</b> The shipped scripts carry 30 rotated draws with
/// arbitrary angles (67.32°, 60.38°, 329.06° …) — a 16-bit angle converted to degrees — so this is a
/// behaviour to reproduce rather than one of our own inventions.
///
/// <para><b>THE ROTATED PATH TREATS X/Y AS THE CENTRE; THE ORDINARY DRAW TREATS THEM AS THE
/// TOP-LEFT.</b> That asymmetry is the whole reason <see cref="TopLeftFor"/> exists, and it is
/// load-bearing rather than a quirk: a rotated bitmap's bounding box GROWS with the angle, so
/// anchoring the box's corner would walk the image across the screen as it turned. Anchoring its
/// centre is what makes it spin in place.</para>
/// </remarks>
public static class RotatedDraw {
    /// <summary>The axis-aligned box a <paramref name="width"/> x <paramref name="height"/> bitmap
    /// occupies once rotated by <paramref name="angleDegrees"/>.</summary>
    /// <remarks>
    /// <b>Rounded UP.</b> A box rounded down would clip the corners it exists to contain, and the
    /// error would show only at angles where the true size lands just above an integer — which is
    /// most of them.
    /// </remarks>
    public static (int Width, int Height) Bounds(int width, int height, double angleDegrees) {
        double radians = angleDegrees * Math.PI / 180.0;
        double cos = Math.Abs(Math.Cos(radians));
        double sin = Math.Abs(Math.Sin(radians));
        return (CeilWithoutNoise((width * cos) + (height * sin)),
                CeilWithoutNoise((width * sin) + (height * cos)));
    }

    /// <summary>
    /// Ceiling, ignoring floating-point dust just above the integer.
    /// </summary>
    /// <remarks>
    /// <b>A BUG THE EXTRACTION EXPOSED, and it was in the inline original too.</b>
    /// <c>Math.Cos(PI/2)</c> is 6.1e-17 rather than 0, so a 100x40 bitmap at exactly 90° measured
    /// 40.0000000000000061 and ceilinged to <b>41</b> — a box one pixel too large at precisely the
    /// angles a reader would assume are exact. It does not bite the shipped scripts, whose angles are
    /// all fractional (88.59, 92.11), but 0/90/180/270 are what a mod or a hand-written test uses.
    ///
    /// <para>The tolerance only ever absorbs values within a nanometre of an integer, which is the
    /// noise and nothing real: a genuine geometric size that close to an integer rounds either way
    /// without a visible difference.</para>
    /// </remarks>
    private static int CeilWithoutNoise(double value) =>
        (int)Math.Ceiling(value - 1e-9);

    /// <summary>
    /// The top-left to draw at so the rotated box is centred on
    /// (<paramref name="centreX"/>, <paramref name="centreY"/>).
    /// </summary>
    /// <param name="scaleX">Horizontal draw scale — the box is measured before scaling.</param>
    /// <param name="scaleY">Vertical draw scale.</param>
    /// <remarks>
    /// <b>The scale is applied to the OFFSET, not to the anchor.</b> Scaling the anchor too would
    /// move the pivot itself, so a scaled rotation would orbit the screen origin instead of spinning
    /// where the script put it.
    /// </remarks>
    /// <param name="boundsWidth">
    /// The box width in CANONICAL px, which is fractional — the caller's canonical size is a float,
    /// so taking an int here would truncate before the halving and lose up to a pixel of centring.
    /// </param>
    /// <param name="boundsHeight"><inheritdoc cref="TopLeftFor" path="/param[@name='boundsWidth']"/></param>
    public static (int X, int Y) TopLeftFor(int centreX, int centreY, double boundsWidth,
        double boundsHeight, double scaleX, double scaleY) => (
        centreX - (int)Math.Round(boundsWidth / 2.0 * scaleX, MidpointRounding.AwayFromZero),
        centreY - (int)Math.Round(boundsHeight / 2.0 * scaleY, MidpointRounding.AwayFromZero));
}
