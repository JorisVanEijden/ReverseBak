namespace GameData.Resources.Character;

using GameData.Resources;

/// <summary>
/// One line of the character sheet's rating list — where it sits, how it reads, and whether it
/// carries a bar. Faithful port of <c>charscreen_draw_sheet_stat_row</c> @0x57dec (ovr160).
/// </summary>
/// <remarks>
/// <b>The sheet has two columns and they are not the same kind of thing.</b> The left column is
/// plain numbers; the right one is skills, and only those draw the sword-shaped progress bar. The
/// discriminator in the original is the column's own x — it tests the coordinate it just computed
/// rather than the attribute — so the two facts are one fact and cannot be set independently.
/// </remarks>
public static class CharacterSheetRow {
    /// <summary>The first attribute that goes in the right-hand column.</summary>
    public const int FirstRightColumnAttribute = 10;

    /// <summary>Attributes the sheet lays out without two of them colliding. See <see cref="RowY"/>.</summary>
    public const int DisplayableAttributes = 16;

    /// <summary>Whether an attribute is drawn in the right column — and therefore with a bar.</summary>
    public static bool IsSkill(int attributeNumber) => attributeNumber >= FirstRightColumnAttribute;

    /// <summary>Left edge of the column an attribute lives in, in canonical space.</summary>
    /// <remarks>VGA 30 for the left column, 147 for the right.</remarks>
    public static int ColumnX(int attributeNumber) => (IsSkill(attributeNumber) ? 147 : 30) * 5;

    /// <summary>
    /// The row an attribute sits on, in canonical space.
    /// </summary>
    /// <remarks>
    /// <b>The remainder is C's, not a modulo.</b> The original computes
    /// <c>((n - 4) % 6) * 16 + 87</c> with a truncating <c>idiv</c>, so attributes below 4 give a
    /// NEGATIVE remainder and land <i>above</i> the nominal first row rather than wrapping to the
    /// bottom of the column. That is what produces the left column's clean run from y=23 down to
    /// y=167; a floored modulo would fold the first four rows to the wrong end and interleave them
    /// with the rest.
    ///
    /// <para>It also means the arithmetic repeats every six rows, so attribute 16 would land on
    /// attribute 10's line. Sixteen is the most the sheet can show without a collision — see
    /// <see cref="DisplayableAttributes"/>.</para>
    /// </remarks>
    public static int RowY(int attributeNumber) => ((((attributeNumber - 4) % 6) * 16) + 87) * 6;

    /// <summary>The name's left edge, relative to its column — VGA +20.</summary>
    public const int NameOffsetX = 20 * 5;

    /// <summary>The value's right edge, relative to its column — VGA +113, right-aligned.</summary>
    public const int ValueOffsetX = 113 * 5;

    /// <summary>Text sits one original row above the row's y.</summary>
    public const int TextOffsetY = -6;

    /// <summary>Name entries in the attribute-name table are this wide.</summary>
    public const int NameStride = 15;

    // ---- ink -----------------------------------------------------------------------------------

    /// <summary>The ordinary pen for a rating's name and its value.</summary>
    public const int Pen = 0x0A;

    /// <summary>The ordinary shadow pen.</summary>
    public const int ShadowPen = 0x01;

    /// <summary>The pen a name is drawn in when the rating changed since it was last looked at.</summary>
    public const int ChangedPen = 0x89;

    /// <summary>The shadow behind a changed name.</summary>
    public const int ChangedShadowPen = 0x8F;

    /// <summary>
    /// Base of the per-attribute "changed since you last looked" flags.
    /// </summary>
    /// <remarks>
    /// <b>Reading the sheet clears them.</b> The draw fetches the flag and writes zero back in the
    /// same breath, so a rating is highlighted exactly once and looking is what acknowledges it. A
    /// port that only reads would leave every improvement highlighted forever; one that clears
    /// somewhere else would lose the highlight before it was ever seen.
    /// </remarks>
    public const int ChangedFlagBase = 6350;

    /// <summary>Attributes per actor in the flag array.</summary>
    public const int AttributesPerActor = 17;

    /// <summary>The flag key for one actor's attribute.</summary>
    public static int ChangedFlagFor(int actorNumber, int attributeNumber) =>
        ChangedFlagBase + (actorNumber * AttributesPerActor) + attributeNumber;

    /// <summary>Which pen a name takes.</summary>
    public static int NamePen(bool changedSinceLastSeen) => changedSinceLastSeen ? ChangedPen : Pen;

    /// <summary>Which shadow a name takes.</summary>
    public static int NameShadowPen(bool changedSinceLastSeen) =>
        changedSinceLastSeen ? ChangedShadowPen : ShadowPen;

    // ---- the value ------------------------------------------------------------------------------

    /// <summary>Catalog key for what a rating with no maximum reads as.</summary>
    /// <remarks>
    /// The text is <b>catalogued, not literal</b> — it is one of the strings lifted out of the
    /// executable, so a translation or an override reaches it like any other. Shown when the
    /// MAXIMUM is zero, not the current value: a rating the character has never had reads
    /// differently from one they have merely lost all of.
    /// </remarks>
    public const string NotApplicableKey = "base:uistring:attribute.value_unavailable";

    /// <summary>The name of an attribute, by its number.</summary>
    /// <remarks>
    /// The original indexes a 15-byte-stride table in the executable with the same number; the
    /// extractor lifted that table into the catalog under these keys, so the ordering is shared
    /// rather than restated.
    /// </remarks>
    public static string NameKey(string attributeName) => "base:uistring:attribute." + attributeName;

    /// <summary>How a rating reads: a percentage, padded to three columns.</summary>
    /// <remarks>
    /// The original's <c>"%3d%%"</c>, padding included. The text is also drawn right-aligned, so the
    /// padding changes nothing on screen — kept because it is what the format string says, and
    /// because a caller that measures the string (to size a box, say) would otherwise get a
    /// different width than the original would have.
    /// </remarks>
    public static string ValueText(int maximum, int percentage) =>
        maximum == 0
            ? Text.UiStrings.Get(NotApplicableKey)
            : ClampPercentage(percentage)
                .ToString(System.Globalization.CultureInfo.InvariantCulture)
                .PadLeft(3) + "%";

    /// <summary>The bar's fill, clamped to a sane range before it is drawn.</summary>
    /// <remarks>
    /// Clamped in both directions, so an over-100 rating fills the bar rather than overrunning it
    /// and a negative one empties it rather than drawing backwards.
    /// </remarks>
    public static int ClampPercentage(int percentage) =>
        percentage < 0 ? 0 : percentage > 100 ? 100 : percentage;

    // ---- the skill bar ---------------------------------------------------------------------------

    /// <summary>Whether this row draws a progress bar at all.</summary>
    public static bool ShowsBar(int attributeNumber) => IsSkill(attributeNumber);

    /// <summary>The bitmap set the bar is drawn from.</summary>
    public const string BarIconSet = "INVSHP2.BMX";

    /// <summary>The sword the bar is shaped like.</summary>
    public const int SwordIcon = 21;

    /// <summary>The marker drawn at the bar's far end when the rating's other flag is set.</summary>
    public const int EndMarkerIcon = 23;

    /// <summary>Left edge of the bar's fill, relative to its column — VGA +13.</summary>
    public const int BarFillOffsetX = 13 * 5;

    /// <summary>The sword graphic's left edge, relative to its column — VGA +14.</summary>
    public const int SwordOffsetX = 14 * 5;
}
