namespace GameData.Resources.Scene;

/// <summary>
/// Where a cipher column's letter is drawn, and what a click on it does —
/// <c>sub_ovr191_6EA</c> @0x7934a (canassa <c>cipher_puzzle_layout_letters</c>).
/// </summary>
/// <remarks>
/// <b>The columns come from REQ_PUZL, not from the font.</b> REQ_PUZL.DAT ships fifteen click
/// areas (action ids 128..142) at canonical y=192, 75x90 each, stepping 85 across — authored
/// <c>Visible=false</c> so that a puzzle enables only as many as its word is long. The original
/// takes each entry's own box and centres a single glyph inside it; the font is measured to place
/// the letter, not to lay out the columns.
///
/// <para>Worth stating because the opposite is a natural guess and was recorded here once: that the
/// cells are built from glyph metrics and centred as a group at a fixed y. They are not, and a
/// screen built that way would ignore the REQ and drift from the artwork.</para>
/// </remarks>
public static class CipherPuzzleLayout {
    /// <summary>The first column's action id; the rest run consecutively.</summary>
    public const int FirstColumnActionId = 128;

    /// <summary>How many columns REQ_PUZL ships — the longest word the screen can pose.</summary>
    public const int MaxColumns = 15;

    /// <summary>The action id of a column, or -1 when it is past what the screen can show.</summary>
    public static int ActionIdFor(int column) =>
        column >= 0 && column < MaxColumns ? FirstColumnActionId + column : -1;

    /// <summary>
    /// Where the glyph's left edge goes inside its column box.
    /// </summary>
    /// <remarks>
    /// Centred, then nudged one pixel right (<c>inc dx</c> at 0x79412). The nudge is only on x —
    /// <see cref="GlyphY"/> has no counterpart — so it is a deliberate lean rather than rounding
    /// applied to both axes.
    /// </remarks>
    public static int GlyphX(int columnX, int columnWidth, int glyphWidth) =>
        columnX + (columnWidth / 2) - (glyphWidth / 2) + 1;

    /// <summary>Where the glyph's top edge goes — centred, with no nudge.</summary>
    public static int GlyphY(int columnY, int columnHeight, int glyphHeight) =>
        columnY + (columnHeight / 2) - (glyphHeight / 2);

    /// <summary>
    /// The inset rectangle the column's bevel is drawn on, as (x, y, right, bottom).
    /// </summary>
    /// <remarks>
    /// One pixel in at the top-left and two at the bottom-right (0x7942f-0x79452), which is what
    /// makes the box read as pressed rather than merely outlined. The asymmetry is the original's.
    /// </remarks>
    public static (int X, int Y, int Right, int Bottom) BevelRect(
        int columnX, int columnY, int columnWidth, int columnHeight) =>
        (columnX + 1, columnY + 1,
            columnX + columnWidth - 2, columnY + columnHeight - 2);

    /// <summary>
    /// The row a column shows after it is clicked.
    /// </summary>
    /// <remarks>
    /// <b>A wheel, not a text field.</b> Each click advances the column by one row and wraps at the
    /// end (0x79381 increments, 0x7938f-0x7939f resets on reaching the count) — so the player spells
    /// the answer by rotating columns until the selected rows read as the target word, and can
    /// never type a letter the dial rows do not offer.
    /// </remarks>
    public static int NextRow(int currentRow, int rowCount) =>
        rowCount <= 0 ? 0 : (currentRow + 1) % rowCount;
}
