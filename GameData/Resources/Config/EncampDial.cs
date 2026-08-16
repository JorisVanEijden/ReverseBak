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
}
