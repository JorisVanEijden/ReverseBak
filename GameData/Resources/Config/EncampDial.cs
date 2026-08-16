namespace GameData.Resources.Config;

/// <summary>
/// The rest dial's behaviour — what a stone means, and what counts as pressing one.
/// Faithful port of the clock-entry arm of <c>encamp_run</c> @0x703d0 (ovr182), around 0x706d9.
/// The geometry and hit test live on <see cref="EncampData"/>.
/// </summary>
/// <remarks>
/// <b>A stone is a time of day, not a duration.</b> The game says so itself in the dial's own help
/// text: "the party will wake up when the time reaches the stone you have selected". So resting to
/// a stone that has already passed today means sleeping round to it tomorrow, not sleeping for
/// zero hours.
/// </remarks>
public static class EncampDial {
    /// <summary>Two-second clock units in an hour — the original's <c>0x708</c>.</summary>
    public const int TicksPerHour = 1800;

    /// <summary>Stones on the dial, one per hour of the day.</summary>
    public const int Stones = 24;

    /// <summary>
    /// The hour a stone selects.
    /// </summary>
    /// <remarks>
    /// <b>Stone index is the hour, with no offset.</b> The original multiplies the hit-test's index
    /// straight by an hour, so stone 0 is midnight and stone 23 is 11pm. Worth stating because the
    /// dial's artwork starts its run at the lower right rather than at the top, which invites a
    /// rotation that is not there.
    /// </remarks>
    public static int HourFor(int stone) => stone;

    /// <summary>The time of day a stone selects, in the clock's own two-second units.</summary>
    public static int TargetTicksFor(int stone) => stone * TicksPerHour;

    /// <summary>
    /// Whether pressing on one stone and releasing on another counts as choosing it.
    /// </summary>
    /// <remarks>
    /// It does not. The original latches which stone the press landed on and acts only when the
    /// release finds the same one, so sliding off a stone abandons the choice rather than
    /// committing it — the ordinary behaviour of a button, applied to hotspots that are not
    /// buttons. Releasing over nothing at all clears the latch too.
    /// </remarks>
    public static bool Commits(int pressedStone, int releasedStone) =>
        pressedStone >= 0 && pressedStone == releasedStone;

    /// <summary>Right-clicking a stone explains what the dial is for.</summary>
    public const int StoneHelpDialog = 240;

    // ---- what colour a stone is drawn -----------------------------------------------------------
    // sub_ovr182_67A @0x70a4a picks one of ENCAMP.BMX's icons per stone and blits it at the stone's
    // ClockEntry position. The dial artwork in ENCAMP.SCX already has stones painted on it, in GOLD
    // -- those are just the backdrop, and the game covers every one of them. Leaving them uncovered
    // is why our dial read gold where the original's reads dark blue.

    /// <summary>ENCAMP.BMX — the stone icons the dial is drawn with.</summary>
    public const string StoneIconSet = "ENCAMP.BMX";

    /// <summary>The ordinary stone: dark blue.</summary>
    public const int StoneIconPlain = 1;

    /// <summary>The stone under the cursor: gold.</summary>
    public const int StoneIconHovered = 2;

    /// <summary>A marked stone — the current hour, and every hour of a running rest: red.</summary>
    public const int StoneIconMarked = 3;

    /// <summary>
    /// The icon a stone is drawn with while the dial is idle.
    /// </summary>
    /// <param name="stone">The stone, which is also its hour — see <see cref="HourFor"/>.</param>
    /// <param name="currentHour">The hour the game clock is in.</param>
    /// <param name="hoveredStone">The stone under the cursor, or -1 for none.</param>
    /// <remarks>
    /// The branch ORDER is the original's: hover is tested before "now", so moving the cursor onto
    /// the current hour turns that stone gold rather than leaving it red.
    ///
    /// <para><b>Only the idle case is modelled here.</b> <c>sub_ovr182_67A</c> has three further
    /// branches that paint a whole SPAN of stones red while a rest is running, driven by a start
    /// time, an end time and a wrap-past-midnight flag. Those are deliberately left out: the call
    /// site that supplies them (@0x70526) is guarded on a rest being in progress, and reading the
    /// span rules off the branches alone would mean guessing which of the three times the caller
    /// passes for a rest that has not started. Our rest is a loop that advances an hour at a time
    /// rather than a scheduled span, so nothing needs them yet.</para>
    /// </remarks>
    public static int IconFor(int stone, int currentHour, int hoveredStone = -1) {
        if (hoveredStone >= 0 && stone == hoveredStone) {
            return StoneIconHovered;
        }

        return stone == currentHour ? StoneIconMarked : StoneIconPlain;
    }
}
