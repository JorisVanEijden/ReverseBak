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

    /// <summary>The highlighted stone: gold. The cursor's on the camp screen, the waking hour's
    /// at an inn — see <see cref="IconFor"/>.</summary>
    public const int StoneIconHovered = 2;

    /// <summary>A marked stone — the current hour, and every hour of a running rest: red.</summary>
    public const int StoneIconMarked = 3;

    /// <summary>
    /// The icon a stone is drawn with.
    /// </summary>
    /// <param name="stone">The stone, which is also its hour — see <see cref="HourFor"/>.</param>
    /// <param name="markedHour">
    /// One hour always drawn red whatever else is going on — the game clock's own hour on both
    /// screens (<c>arg_A</c>).
    /// </param>
    /// <param name="highlightedStone">
    /// The one stone drawn GOLD, or -1 for none (<c>arg_4</c>). <b>Not only a cursor.</b> The camp
    /// screen passes the stone under the mouse; the inn passes its WAKING HOUR (0x50186), so the
    /// gold stone there tells you when you will be woken and never moves. Naming it "hovered" is
    /// what made the inn's use look like a different feature.
    /// </param>
    /// <param name="spanStartHour">
    /// First hour of a range drawn red, or -1 for no range (<c>arg_6</c>).
    /// </param>
    /// <param name="spanEndHour">Last hour of that range (<c>arg_8</c>).</param>
    /// <remarks>
    /// The branch ORDER is the original's: gold is tested before red, so the highlighted stone wins
    /// even when it is also the current hour.
    ///
    /// <para><b>The range is how a rest shows its progress.</b> The camp screen passes the hour the
    /// rest began and the hour the clock has reached (0x70526), so the red arc grows around the rim
    /// as the night passes; the inn passes the same hour for both (0x5017a) and gets a single red
    /// stone. It <b>wraps past midnight</b> — when the start is later than the end the test becomes
    /// "at or after the start OR at or before the end", which is the only way an overnight rest can
    /// be drawn at all.</para>
    ///
    /// <para>A range whose ends are equal marks just that one hour. Without that guard the wrapping
    /// test would read as "at or after N or at or before N" and paint the entire ring — which is
    /// why the original carries a separate flag for it (<c>arg_2</c>, passed as 1 by both callers).</para>
    ///
    /// <para><b>Still not modelled:</b> <c>arg_0</c>, which collapses the whole thing to "mark only
    /// the range's end hour". The inn passes 0 and the camp screen passes a variable
    /// (<c>var_14</c>) whose meaning would take reading <c>UI_Encamp</c>'s state machine to pin
    /// down. Nothing here needs it, and guessing it would be worse than saying so.</para>
    /// </remarks>
    public static int IconFor(int stone, int markedHour, int highlightedStone = -1,
        int spanStartHour = -1, int spanEndHour = -1) {
        if (highlightedStone >= 0 && stone == highlightedStone) {
            return StoneIconHovered;
        }
        if (stone == markedHour || InSpan(stone, spanStartHour, spanEndHour)) {
            return StoneIconMarked;
        }

        return StoneIconPlain;
    }

    /// <summary>Whether a stone falls in the marked range, wrapping past midnight.</summary>
    public static bool InSpan(int stone, int spanStartHour, int spanEndHour) {
        if (spanStartHour < 0 || spanEndHour < 0) {
            return false;
        }
        if (spanStartHour == spanEndHour) {
            return stone == spanStartHour;
        }

        return spanStartHour < spanEndHour
            ? stone >= spanStartHour && stone <= spanEndHour
            : stone >= spanStartHour || stone <= spanEndHour;
    }

    // ---- the sundial's shadow -------------------------------------------------------------------
    // encamp_drawDialNeedle? @0x70c9b. Despite the provisional name it is not a needle: it fills a
    // THREE-POINT polygon in pen 0 -- a black wedge from a fixed apex pair down to a point that
    // walks the dial's arc, i.e. the shadow a gnomon casts as the sun crosses the sky.

    /// <summary>The wedge's two fixed vertices are entries 0 and 1 of <c>NeedleEntries</c>; the
    /// third is one of the 24 that follow, and entry 2 is the scratch slot it is copied into.</summary>
    public const int ShadowArcFirstEntry = 3;

    /// <summary>First hour the shadow is drawn — dawn.</summary>
    public const int ShadowFirstHour = 6;

    /// <summary>Last hour the shadow is drawn — dusk.</summary>
    public const int ShadowLastHour = 18;

    /// <summary>
    /// Which <c>NeedleEntries</c> point the shadow reaches to, or -1 when no shadow is drawn.
    /// </summary>
    /// <param name="ticksOfDay">Time within the day, in the clock's two-second units.</param>
    /// <remarks>
    /// <b>Only between 6am and 6pm, and never at noon.</b> The original returns without drawing
    /// outside <c>[10800, 32400]</c> — the daylight window — so the dial simply has no shadow at
    /// night, and it also returns at exactly midday, where the shadow would be the degenerate line
    /// straight down the gnomon. Those two skips are why the arc holds 24 points for what looks like
    /// a 25-step sweep: noon is the missing one, and every half-hour past it shifts down by one.
    /// </remarks>
    public static int ShadowArcPointFor(int ticksOfDay) {
        const int noon = 12;
        if (ticksOfDay < ShadowFirstHour * TicksPerHour
            || ticksOfDay > ShadowLastHour * TicksPerHour) {
            return -1;
        }

        int halfHoursSinceDawn = ticksOfDay * 2 / TicksPerHour - noon;
        if (halfHoursSinceDawn == noon) {
            return -1;
        }

        return ShadowArcFirstEntry
            + (halfHoursSinceDawn > noon ? halfHoursSinceDawn - 1 : halfHoursSinceDawn);
    }
}
