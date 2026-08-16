namespace GameData.Resources.Character;

/// <summary>
/// Where the character sheet puts things — the panel, its two columns and the condition list.
/// Faithful port of <c>charscreen_info_draw</c> @0x580fe (ovr160, CHARSCRN.C).
/// </summary>
/// <remarks>
/// <b>The sheet has two sizes and the caller picks.</b> The compact form is portrait, ratings panel
/// and condition list; the full form adds a lower half. The temple healer draws the compact one,
/// which is why that screen is a character sheet with three buttons on it rather than a panel of
/// its own.
///
/// <para>Positions are canonical 1600x1200 (VGA x5 across, x6 down), as everywhere else.</para>
/// </remarks>
public static class CharacterSheetLayout {
    // ---- the ratings panel ----------------------------------------------------------------

    /// <summary>Left edge of the bordered ratings panel — VGA x=84.</summary>
    public const int PanelX = 420;

    /// <summary>Top edge — VGA y=9.</summary>
    public const int PanelY = 54;

    /// <summary>Width — VGA 222.</summary>
    public const int PanelWidth = 1110;

    /// <summary>Height — VGA 71.</summary>
    public const int PanelHeight = 426;

    /// <summary>The pen the panel is filled with.</summary>
    public const int PanelFillPen = 0xA8;

    /// <summary>
    /// The bitmap set the panel's frame is drawn from, and the two rules in it.
    /// </summary>
    /// <remarks>
    /// The frame is not a drawn border but two <b>dotted-rule bitmaps</b> tiled down the sides and
    /// across the top and bottom, taken from the inventory screen's own sheet. Drawing a plain
    /// rectangle instead would lose the dotted texture the whole screen family shares.
    /// </remarks>
    public const string FrameIconSet = "INVSHP2.BMX";

    /// <summary>The vertical dotted rule, drawn down both sides.</summary>
    public const int VerticalRuleIcon = 25;

    /// <summary>The horizontal dotted rule, drawn along the top and bottom.</summary>
    public const int HorizontalRuleIcon = 26;

    // ---- the two column headings ------------------------------------------------------------

    /// <summary>Baseline shared by both headings — VGA y=14.</summary>
    public const int HeadingY = 84;

    /// <summary>Left column heading, "Ratings:" — VGA x=95.</summary>
    public const int RatingsHeadingX = 475;

    /// <summary>Right column heading, "Condition:" — VGA x=210.</summary>
    public const int ConditionHeadingX = 1050;

    // ---- the condition list -------------------------------------------------------------------

    /// <summary>Left edge of every condition line — VGA x=220.</summary>
    public const int ConditionX = 1100;

    /// <summary>Conditions considered, 0 to this bound exclusive.</summary>
    public const int ConditionCount = 7;

    /// <summary>
    /// The baseline of the n-th condition actually listed, counting from 1.
    /// </summary>
    /// <remarks>
    /// <b>Lines are numbered by what was drawn, not by which condition it is.</b> The counter only
    /// advances for a condition the character actually has, so the list closes up instead of leaving
    /// a gap where a condition would have been — three afflictions always occupy the first three
    /// lines, whichever three they are.
    /// </remarks>
    public static int ConditionLineY(int lineNumber) => ((lineNumber * 9) + 16) * 6;

    /// <summary>
    /// Where "Normal" is written when the character has nothing wrong with them.
    /// </summary>
    /// <remarks>
    /// <b>Not the same baseline as the first condition line</b> — VGA y=28 against the first line's
    /// 25. Three original pixels lower, for no reason the code gives. Reproduced rather than aligned
    /// because the two never appear together, so the difference is only ever visible as a wobble
    /// between one character and the next.
    /// </remarks>
    public const int NormalY = 168;

    /// <summary>The word an unafflicted character gets under Condition.</summary>
    public const string NormalText = "Normal";

    /// <summary>How a listed condition reads: its name and its strength as a percentage.</summary>
    public const string ConditionFormat = "{0} ({1}%)";

    /// <summary>Text pen for a condition line.</summary>
    public const int ConditionPen = 0x89;

    /// <summary>Shadow pen behind a condition line.</summary>
    public const int ConditionShadowPen = 0x8F;

    /// <summary>
    /// Whether a condition is listed at all.
    /// </summary>
    /// <remarks>
    /// Strictly positive: a zeroed condition is absent rather than shown at 0%. Signed, so a
    /// negative value is absent too — which is what keeps a cure's <c>-100</c> from ever being
    /// rendered as a condition the character has.
    /// </remarks>
    public static bool IsListed(int conditionValue) => conditionValue > 0;

    // ---- the two sizes --------------------------------------------------------------------------

    /// <summary>Whether the caller asked for the full sheet rather than the compact one.</summary>
    public static bool IsFullSheet(int fullSheetFlag) => fullSheetFlag != 0;

    /// <summary>Left edge of the full sheet's lower half — VGA x=2.</summary>
    public const int LowerHalfX = 10;

    /// <summary>Top of the full sheet's lower half — VGA y=107.</summary>
    public const int LowerHalfY = 642;
}
