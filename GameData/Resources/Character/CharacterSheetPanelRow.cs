namespace GameData.Resources.Character;

/// <summary>
/// One line inside the character sheet's ratings panel — the four rows under "Ratings:". Faithful
/// port of <c>UI_show_attribute_x_of_y</c> @0x5800f (ovr160).
/// </summary>
/// <remarks>
/// <b>These are not the same rows as <see cref="CharacterSheetRow"/>.</b> The sheet has two row
/// drawers and they share neither their arithmetic nor their ink: this one draws attributes 0..3
/// inside the panel, on an 11-row pitch from y=28, in plain numbers; the other draws attributes
/// 4..15 in the full sheet's lower half, on a 16-row pitch from y=87, as percentages with a sword
/// bar. A port that put every attribute through one of them misplaces four rows by up to ten
/// original rows and gives them the wrong ink besides.
///
/// <para><b>The compact sheet is only these four rows.</b> The lower half's twelve are skipped
/// entirely when the caller asks for the compact form — see
/// <see cref="CharacterSheetLayout.DrawsLowerHalf"/> — so this is the whole of what the temple
/// healer's sheet says about a character's ratings.</para>
///
/// <para>Positions are canonical 1600x1200 (VGA x5 across, x6 down).</para>
/// </remarks>
public static class CharacterSheetPanelRow {
    /// <summary>How many attributes the panel lists: 0 through 3.</summary>
    public const int Count = 4;

    /// <summary>
    /// The last attribute whose maximum is shown beside its current value.
    /// </summary>
    /// <remarks>
    /// <b>Only the first two rows read "x of y".</b> The original leaves after drawing the value
    /// for anything above this (<c>cmp si, 1</c> / <c>jg</c> at 0x580b2), so Health and Stamina get
    /// a maximum and Speed and Strength are a bare number. Both kinds are drawn by the same code
    /// with the same maximum to hand, so a port that reads the row's shape off the data rather than
    /// off the attribute number would show two maxima that the original never prints.
    /// </remarks>
    public const int LastAttributeWithMaximum = 1;

    /// <summary>Whether this row prints its maximum after the current value.</summary>
    public static bool ShowsMaximum(int attributeNumber) =>
        attributeNumber <= LastAttributeWithMaximum;

    /// <summary>
    /// The baseline of an attribute's row, in canonical space.
    /// </summary>
    /// <remarks>VGA <c>attributeNumber * 11 + 28</c>: four rows from y=28 to y=61, which is what
    /// fits them inside a panel that ends at y=80.</remarks>
    public static int RowY(int attributeNumber) => ((attributeNumber * 11) + 28) * 6;

    /// <summary>Left edge of the attribute's name — VGA x=105.</summary>
    public const int NameX = 105 * 5;

    /// <summary>Right edge of the current value, which is right-aligned — VGA x=164.</summary>
    public const int ValueRightX = 164 * 5;

    /// <summary>Left edge of the word between the two numbers — VGA x=170.</summary>
    public const int SeparatorX = 170 * 5;

    /// <summary>Right edge of the maximum, also right-aligned — VGA x=194.</summary>
    public const int MaximumRightX = 194 * 5;

    /// <summary>Catalog key for the word between current and maximum ("of").</summary>
    /// <remarks>
    /// Catalogued rather than literal, like the rest of the sheet's words: it is one of the strings
    /// the extractor lifted out of the executable, so a translation reaches it.
    /// </remarks>
    public const string SeparatorKey = "base:uistring:attribute.current_of_max_separator";

    // ---- ink -------------------------------------------------------------------------------

    /// <summary>
    /// The pen an unchanged row is drawn in.
    /// </summary>
    /// <remarks>
    /// <b>The original names no pen here — it passes -1 and lets the text routine choose.</b>
    /// <c>DisplayText</c> @0x5634d substitutes 0x9F for a negative colour and 0 for a negative
    /// shadow, the same defaults the inventory screen's text drawer uses. So these rows are the
    /// screen's ordinary text colour, and NOT <see cref="CharacterSheetRow.Pen"/> — the lower
    /// half's rows are the ones that name 0x0A/0x01 for themselves.
    /// </remarks>
    public const int Pen = 0x9F;

    /// <summary>The shadow behind an unchanged row, one pixel down-right.</summary>
    /// <inheritdoc cref="Pen"/>
    public const int ShadowPen = 0x00;

    /// <summary>The pen a row takes when the rating changed since it was last looked at.</summary>
    public const int ChangedPen = CharacterSheetRow.ChangedPen;

    /// <summary>The shadow behind a changed row.</summary>
    public const int ChangedShadowPen = CharacterSheetRow.ChangedShadowPen;

    /// <summary>
    /// Which pen this row takes.
    /// </summary>
    /// <remarks>
    /// <b>The whole row changes colour, not just the name.</b> The value — and the maximum, when
    /// there is one — are drawn with the same pair the name is, which is where this differs from
    /// the lower half's rows: there only the name is highlighted.
    /// </remarks>
    public static int RowPen(bool changedSinceLastSeen) => changedSinceLastSeen ? ChangedPen : Pen;

    /// <summary>Which shadow this row takes.</summary>
    /// <inheritdoc cref="RowPen"/>
    public static int RowShadowPen(bool changedSinceLastSeen) =>
        changedSinceLastSeen ? ChangedShadowPen : ShadowPen;

    /// <summary>
    /// The "changed since you last looked" flag key for one actor's attribute.
    /// </summary>
    /// <remarks>
    /// <b>The same flags the lower half's rows use</b> — same base, same 17-wide stride, and read
    /// and cleared the same way (fetch, then write zero, at 0x58031 and 0x58060). Looking at the
    /// sheet is what acknowledges an improvement, so a renderer that only reads leaves every one
    /// of them highlighted forever.
    /// </remarks>
    public static int ChangedFlagFor(int actorNumber, int attributeNumber) =>
        CharacterSheetRow.ChangedFlagFor(actorNumber, attributeNumber);
}
