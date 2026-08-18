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

    // ---- the portrait -------------------------------------------------------------------------

    /// <summary>Left edge of the portrait — VGA x=7.</summary>
    /// <remarks>
    /// <b>The sheet is the one caller that places a face at fixed coordinates.</b>
    /// <c>ShowDialogWithFace</c> @0x4ab29 picks between three placements on its mode argument, and
    /// two of them follow the render view; the sheet passes a NEGATIVE mode, which is the corner
    /// (0x4ac22) — so this portrait does not move with the viewport the way a speaker's does.
    /// </remarks>
    public const int PortraitX = 7 * 5;

    /// <summary>Top edge of the portrait — VGA y=9.</summary>
    /// <inheritdoc cref="PortraitX"/>
    public const int PortraitY = 9 * 6;

    /// <summary>
    /// Whether the sheet shows the actor's second face.
    /// </summary>
    /// <remarks>
    /// <b>It does, and by the same argument that chose the corner.</b> <c>LoadActorFace</c> takes
    /// the alternate bitmap on a negative mode, so "corner" and "alternate face" are one decision
    /// in the original and cannot be set apart — the sheet cannot show the portrait the dialog
    /// shows.
    /// </remarks>
    public const bool PortraitIsAlternate = true;

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

    /// <summary>Catalog key for the word an unafflicted character gets under Condition.</summary>
    /// <remarks>
    /// The catalog names this and the two headings with an <c>item.</c> prefix, which is a
    /// misnomer inherited from where they were first found — they are the character sheet's, not an
    /// item's. Left as they are: the keys are the catalog's contract, and renaming them would break
    /// any override already written against them for the sake of a tidier string.
    /// </remarks>
    public const string NormalKey = "base:uistring:item.condition_normal";

    /// <summary>Catalog key for the left column's heading.</summary>
    /// <inheritdoc cref="NormalKey"/>
    public const string RatingsHeadingKey = "base:uistring:item.ratings_header";

    /// <summary>Catalog key for the right column's heading.</summary>
    /// <inheritdoc cref="NormalKey"/>
    public const string ConditionHeadingKey = "base:uistring:item.condition_label";

    /// <summary>How a listed condition reads: its name and its strength as a percentage.</summary>
    public const string ConditionFormat = "{0} ({1}%)";

    /// <summary>The drop shadow's horizontal offset — one original pixel right.</summary>
    /// <remarks>
    /// <b>Not authored anywhere; it is what the text routine does.</b> <c>DisplayText</c> @0x5634d
    /// draws the shadow string at <c>(x+1, y+1)</c> before the text itself, so every line on this
    /// screen carries the same offset and none of them state it.
    /// </remarks>
    public const int TextShadowOffsetX = 5;

    /// <summary>The drop shadow's vertical offset — one original pixel down.</summary>
    /// <inheritdoc cref="TextShadowOffsetX"/>
    public const int TextShadowOffsetY = 6;

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

    /// <summary>
    /// Whether the sheet draws its lower half at all.
    /// </summary>
    /// <remarks>
    /// <b>The compact sheet has no rating rows below the panel — it skips all twelve.</b> The loop
    /// that draws them tests the caller's flag before its first iteration (0x58369), so the compact
    /// form is the panel's four rows and the condition list, and nothing else. A port that drew the
    /// lower half regardless would spill skills across the temple healer's buttons.
    /// </remarks>
    public static bool DrawsLowerHalf(int fullSheetFlag) => IsFullSheet(fullSheetFlag);

    /// <summary>The first attribute of the lower half — the rows before it are the panel's.</summary>
    /// <remarks>
    /// Attributes 0..3 go in the panel through <see cref="CharacterSheetPanelRow"/>; 4 and up go in
    /// the lower half through <see cref="CharacterSheetRow"/>. The two drawers meet exactly here.
    /// </remarks>
    public const int LowerHalfFirstAttribute = 4;

    /// <summary>How many rows the lower half draws — attributes 4 through 15.</summary>
    public const int LowerHalfAttributeCount = 12;

    // ---- the frame and the vines -------------------------------------------------------------

    /// <summary>
    /// An image drawn as it is stored.
    /// </summary>
    /// <remarks>
    /// Spelled out rather than written as a zero because <see cref="Image.ImageFlags"/> has no
    /// zero member on purpose: it is the enum the BMX extractor serialises, and giving it one would
    /// rewrite every <c>"Flags": 0</c> in the committed image JSON as a name.
    /// </remarks>
    public const Image.ImageFlags Unturned = 0;

    /// <summary>One bitmap placed on the sheet: which image, where, and how it is turned.</summary>
    /// <param name="IconIndex">Sub-image index within <see cref="FrameIconSet"/>.</param>
    /// <param name="X">Canonical x. <b>May be negative</b> — one vine hangs off the left edge.</param>
    /// <param name="Y">Canonical y.</param>
    /// <param name="Flags">How the blit turns the image; see <see cref="Image.ImageFlags"/>.</param>
    public readonly record struct Piece(int IconIndex, int X, int Y, Image.ImageFlags Flags);

    /// <summary>
    /// The four rules that frame the ratings panel, in draw order.
    /// </summary>
    /// <remarks>
    /// <b>One blit each, not a tiled fill</b> — the two rules are already the panel's length, and
    /// each opposite edge is the same image turned round: the right-hand rule is mirrored, the
    /// bottom one flipped. Reproducing them as a drawn rectangle would lose the dotted texture this
    /// screen family shares; drawing them unturned would light the dots from the wrong side.
    ///
    /// <para>The horizontal rules start two original pixels inside the panel's left edge (VGA 86
    /// against the panel's 84), so they butt against the vertical ones rather than crossing them.</para>
    /// </remarks>
    public static readonly System.Collections.Generic.IReadOnlyList<Piece> PanelFrame = new[] {
        new Piece(VerticalRuleIcon, 84 * 5, 9 * 6, Unturned),
        new Piece(VerticalRuleIcon, 304 * 5, 9 * 6, Image.ImageFlags.HorizontalFlip),
        new Piece(HorizontalRuleIcon, 86 * 5, 9 * 6, Unturned),
        new Piece(HorizontalRuleIcon, 86 * 5, 78 * 6, Image.ImageFlags.VerticalFlip),
    };

    /// <summary>The vine piece the corners of the full sheet are decorated with.</summary>
    public const int CornerVineIcon = 24;

    /// <summary>
    /// The vine piece the compact sheet gets instead — the same one the full-screen dialog frames
    /// itself with (<see cref="Dialog.DialogVineCorners"/>), at a placement of the sheet's own.
    /// </summary>
    public const int SmallVineIcon = 9;

    /// <summary>
    /// The vines, which differ with the sheet's size.
    /// </summary>
    /// <remarks>
    /// <b>Both sizes are decorated, and not with the same piece in the same place.</b> The full
    /// sheet gets the big corner vines at the top of its lower half; the compact one gets the small
    /// vine at the bottom of the panel, where the lower half would have started. So this is not a
    /// piece of the lower half that the compact form omits — it is a choice between two
    /// decorations, and dropping it from the compact sheet leaves that edge bare.
    /// </remarks>
    public static System.Collections.Generic.IReadOnlyList<Piece> Vines(int fullSheetFlag) =>
        IsFullSheet(fullSheetFlag) ? FullSheetVines : CompactSheetVines;

    private static readonly Piece[] FullSheetVines = {
        new Piece(CornerVineIcon, 2 * 5, 107 * 6, Unturned),
        new Piece(CornerVineIcon, 188 * 5, 107 * 6, Image.ImageFlags.HorizontalFlip),
    };

    private static readonly Piece[] CompactSheetVines = {
        new Piece(SmallVineIcon, 230 * 5, 131 * 6, Image.ImageFlags.HorizontalFlip),
        new Piece(SmallVineIcon, -4 * 5, 131 * 6, Unturned),
    };
}
