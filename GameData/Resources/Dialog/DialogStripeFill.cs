namespace GameData.Resources.Dialog;

/// <summary>
/// The dialog panel's patterned fill — the FILL phase of <c>dialog_DrawChrome</c> @0x486a1.
/// </summary>
/// <remarks>
/// <b>The fill is not a colour, it is a woven strip of what is already on screen.</b> The original
/// reads its source from the VGA data segment at a fixed base and blits it through a planar mask,
/// which is why a shipped dialog box carries the background's grain rather than a flat tone.
///
/// <para><b>Only the PHASE is modelled here, not the mechanism.</b> The source is video memory read
/// through latched planar writes — hardware, not data, and precisely the kind of thing this port
/// does not reproduce. The observable behaviour is what carries over: a textured fill whose
/// starting point moves from one dialog to the next.</para>
/// </remarks>
public static class DialogStripeFill {
    /// <summary>
    /// How far the fill's starting point can wander — <c>(rand() &amp; 0xFFF) % 100</c>.
    /// </summary>
    public const int PhaseWindow = 100;

    /// <summary>
    /// Whether this entry's fill is re-phased each time it is drawn.
    /// </summary>
    /// <remarks>
    /// <b>Randomised is the DEFAULT; the flag turns it off.</b>
    /// <see cref="DialogEntryFlags.FixedStripePattern"/> (0x8000) is what pins the source offset to
    /// its base, so an entry carrying it draws the same weave every time. Reading the flag the
    /// other way round — as "stripe this one" — would freeze every ordinary dialog and animate only
    /// the ones meant to hold still.
    /// </remarks>
    public static bool IsRandomised(DialogEntryFlags flags) =>
        (flags & DialogEntryFlags.FixedStripePattern) == 0;

    /// <summary>
    /// The fill's starting offset for one drawing.
    /// </summary>
    /// <param name="flags">The entry's flags.</param>
    /// <param name="roll">Any non-negative number; only its remainder matters.</param>
    /// <returns>0 when the pattern is fixed, otherwise a value in [0, <see cref="PhaseWindow"/>).</returns>
    public static int PhaseFor(DialogEntryFlags flags, int roll) =>
        IsRandomised(flags) ? System.Math.Abs(roll) % PhaseWindow : 0;
}
