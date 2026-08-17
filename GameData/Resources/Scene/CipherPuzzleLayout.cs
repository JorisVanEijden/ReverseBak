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

    /// <summary>
    /// How many column entries REQ_PUZL ships — <b>twenty</b>, not the fifteen the artwork has room
    /// for.
    /// </summary>
    /// <remarks>
    /// Ids 128..142 step across the panel at 85 apart; <b>143..147 are all parked on top of 142</b>
    /// at canonical x=1340. The authoring ran out of width and stacked the surplus rather than
    /// dropping it, so the count and the number of DISTINCT positions differ — see
    /// <see cref="DistinctColumns"/>.
    ///
    /// <para>The count is what matters to a screen switching entries on and off: sizing that loop
    /// to fifteen leaves the last five in whatever state the data shipped them in, and the loader
    /// keeps faceless click areas navigable, so they sit in the keyboard ring as five invisible
    /// duplicates of the last column. The longest shipped target is ten letters, so the stacking
    /// itself is never reached in the base game.</para>
    /// </remarks>
    public const int MaxColumns = 20;

    /// <summary>How many columns have a position of their own — past this they overlap.</summary>
    public const int DistinctColumns = 15;

    /// <summary>Whether a target of this length can be shown without stacking letters.</summary>
    public static bool FitsWithoutOverlap(int width) => width <= DistinctColumns;

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

    // ---- the riddle's text ----------------------------------------------------------------------

    /// <summary>
    /// The three passes the riddle is drawn in, as (y offset, pen).
    /// </summary>
    /// <remarks>
    /// <b>One block of text drawn three times, not three blocks.</b> UI_RunCipherPuzzle renders the
    /// same string at y-1 in pen 65, at y+1 in pen 149, and then on the baseline in pen 16
    /// (0x78e0b-0x78e4e) — a highlight above and a shadow below, with the body last on top. Drawing
    /// it once loses the emboss that makes it readable against the chest lid.
    ///
    /// <para>Order matters: the body is drawn LAST so it covers the middle of the other two.</para>
    /// </remarks>
    public static (int YOffset, int Pen)[] TextPasses() => new[] {
        (-1, 65),
        (1, 149),
        (0, 16),
    };

    /// <summary>The pen the readable body of the riddle is drawn in.</summary>
    public const int TextBodyPen = 16;
}
