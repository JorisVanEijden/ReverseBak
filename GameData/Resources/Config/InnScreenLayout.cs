namespace GameData.Resources.Config;

/// <summary>
/// The inn's rest screen — <c>UI_RestUntilTime</c> @0x4ff5c (ovr150).
/// </summary>
/// <remarks>
/// <b>Not the camp screen's layout.</b> Camping is an overlay that leaves the travel HUD standing
/// around it; the inn clears the whole screen and draws its own frame, because it is entered from a
/// location scene rather than from the world. The dial inside is the same one
/// (<see cref="EncampDial"/>), and the panel it sits on is the same sub-rect of ENCAMP.SCX
/// (<c>encamp_blitDialPanel</c> @0x70a2b) — only the surround differs.
///
/// <para>Positions are canonical 1600x1200 (VGA x5 across, x6 down), matching
/// <see cref="CampPartyStats"/>.</para>
/// </remarks>
public static class InnScreenLayout {
    // ---- the dial panel -------------------------------------------------------------------------

    /// <summary>Left edge of the ENCAMP.SCX sub-rect the panel is cut from — VGA x=13.</summary>
    public const int PanelX = 13 * 5;

    /// <summary>Top edge — VGA y=11.</summary>
    public const int PanelY = 11 * 6;

    /// <summary>Width — VGA 293.</summary>
    public const int PanelWidth = 293 * 5;

    /// <summary>
    /// Height — VGA 101, so the panel's last row is y=111.
    /// </summary>
    /// <remarks>
    /// <b>The frame is drawn ON this artwork, not around it.</b> The panel is blitted first
    /// (0x50025) and the four bevel lines after (0x5002a onward), and the inner bottom line at
    /// VGA y=111 lands exactly on the panel's final row. So the frame overlays the artwork's
    /// outermost pixels by design; treating it as a surround that must clear the panel is what
    /// makes the two rectangles look inconsistent.
    ///
    /// <para>That is also why the per-frame restore is SHORTER — 99 rather than 101 (0x50272
    /// against 0x70a34). It stops before the frame's rows so that redrawing the dial each hour
    /// does not erase the bevel. A port that redraws whole elements in z-order does not need the
    /// distinction, but reading the shorter number as the panel's real height would crop it.</para>
    /// </remarks>
    public const int PanelHeight = 101 * 6;

    /// <summary>Height of the region the original repaints each hour — VGA 99. See
    /// <see cref="PanelHeight"/> for why it is two rows shorter.</summary>
    public const int PanelRefreshHeight = 99 * 6;

    // ---- the frame around it --------------------------------------------------------------------

    /// <summary>
    /// The bevelled frame, drawn as four lines plus four corner pixels — VGA (11,9) to (308,112).
    /// </summary>
    /// <remarks>
    /// <b>Four different pens, and which edge gets which is the whole effect.</b> The original draws
    /// the inner top-left and bottom edges in one pair and the outer ones in another (0x5002a
    /// onward), so the frame reads as carved rather than as a box outline. Drawing all four in one
    /// colour loses the bevel entirely.
    /// </remarks>
    public const int FrameOuterX = 11 * 5;

    /// <inheritdoc cref="FrameOuterX"/>
    public const int FrameOuterY = 9 * 6;

    /// <inheritdoc cref="FrameOuterX"/>
    public const int FrameOuterRight = 308 * 5;

    /// <inheritdoc cref="FrameOuterX"/>
    public const int FrameOuterBottom = 112 * 6;

    /// <summary>The inner rectangle's edges — VGA (12,10) to (307,111).</summary>
    public const int FrameInnerX = 12 * 5;

    /// <inheritdoc cref="FrameInnerX"/>
    public const int FrameInnerY = 10 * 6;

    /// <inheritdoc cref="FrameInnerX"/>
    public const int FrameInnerRight = 307 * 5;

    /// <inheritdoc cref="FrameInnerX"/>
    public const int FrameInnerBottom = 111 * 6;

    /// <summary>Pen for the inner LEFT and BOTTOM edges — the shaded side.</summary>
    public const int FrameInnerShadowPen = 0x91;

    /// <summary>Pen for the outer LEFT and BOTTOM edges.</summary>
    public const int FrameOuterShadowPen = 0x1c;

    /// <summary>Pen for the inner TOP and RIGHT edges — the lit side.</summary>
    public const int FrameInnerLightPen = 0x15;

    /// <summary>Pen for the outer TOP and RIGHT edges.</summary>
    public const int FrameOuterLightPen = 0x10;

    /// <summary>Line thickness, in canonical px — one VGA pixel.</summary>
    public const int FrameLineWidth = 5;

    // ---- the party purse ------------------------------------------------------------------------

    /// <summary>
    /// Baseline of the gold readout — VGA y=89, and the <b>only</b> screen that shows it.
    /// </summary>
    /// <remarks>
    /// <c>UI_DisplayPartyGold</c> @0x4ff0a has exactly one caller, this screen, which it draws twice
    /// (before the offer and once per hour of the stay). It is not part of the camp screen, where it
    /// would collide with the third member's row.
    /// </remarks>
    public const int PurseY = 89 * 6;

    /// <summary>Left edge of the "Party Gold:" caption — VGA x=139.</summary>
    public const int PurseLabelX = 139 * 5;

    /// <summary>Left edge of the amount — VGA x=199, i.e. sixty past the caption.</summary>
    public const int PurseAmountX = 199 * 5;

    /// <summary>Pen the purse is written in.</summary>
    public const int PurseTextPen = 0;

    /// <summary>The caption's catalog key.</summary>
    public const string PurseLabelKey = "base:uistring:money.party_gold_label";
}
