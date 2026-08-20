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

    // ---- where the wheels actually sit -----------------------------------------------------

    /// <summary>
    /// <b>REQ_PUZL's authored column rects are DISCARDED — the screen recomputes them.</b>
    /// </summary>
    /// <remarks>
    /// <c>cipher_puzzle_layout_letters</c> walks the page's entries and overwrites each one's rect
    /// before anything is drawn: a cell sized from the FONT, a row CENTRED on the screen, and a
    /// fixed <see cref="RowTopVga"/>. The positions in the file are only ever a placeholder — the
    /// data ships fifteen columns marching from the left edge, and using them puts a short word in
    /// the top-left corner instead of centred on the chest.
    ///
    /// <para>That is what makes the row grow outwards from the middle as the word gets longer,
    /// which is the whole visual point of the screen.</para>
    /// </remarks>
    public static bool AuthoredColumnRectsAreOverwritten => true;

    /// <summary>Padding added to the font's glyph box to get a cell — 6 either way.</summary>
    public const int CellPaddingVga = 6;

    /// <summary>The gap between one cell and the next — 2.</summary>
    public const int ColumnGapVga = 2;

    /// <summary>The row's top edge: VGA y 0x57.</summary>
    /// <remarks>Fixed, and unrelated to whatever the REQ file's entries claim.</remarks>
    public const int RowTopVga = 0x57;

    /// <summary>How wide the whole row of <paramref name="width"/> cells is.</summary>
    /// <remarks>
    /// <c>width * (cell + gap) - gap</c> — the trailing gap is taken back off, so a one-letter word
    /// spans exactly one cell rather than a cell and a dangling gap.
    /// </remarks>
    public static int RowSpan(int width, int cellWidth, int gap) =>
        (width * (cellWidth + gap)) - gap;

    /// <summary>Where the row starts so that it is centred in <paramref name="containerWidth"/>.</summary>
    /// <remarks>
    /// The original halves both terms independently (<c>(w &gt;&gt; 1) - (span &gt;&gt; 1)</c>)
    /// rather than halving the difference, so where both are odd the result can sit one unit left of
    /// a true centre. Reproduced rather than tidied: it is the difference between matching the
    /// original and being a pixel out on some words.
    /// </remarks>
    public static int RowStartX(int containerWidth, int rowSpan) =>
        (containerWidth / 2) - (rowSpan / 2);

    /// <summary>The left edge of one column's cell.</summary>
    public static int ColumnX(int column, int rowStartX, int cellWidth, int gap) =>
        rowStartX + (column * (cellWidth + gap));

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

    // ---- the wheel's roll -----------------------------------------------------------------------

    /// <summary>
    /// How many frames a column takes to roll from one letter to the next.
    /// </summary>
    /// <remarks>
    /// <b>It is a slot machine, not a swap.</b> The loop at 0x79455-0x7954c draws BOTH letters at
    /// once — the outgoing one and the incoming one stacked above it — clipped to the column's
    /// inset box (<see cref="BevelRect"/>, which is what that rect is really for), and moves the
    /// pair down one pixel per frame. So the old letter falls out of the bottom of the box while
    /// the new one arrives from the top.
    ///
    /// <para>The loop runs <c>fontHeight + 3</c> times and then steps back one (0x7954f), which is
    /// why the travel is <see cref="RollTravel"/> rather than the frame count: the last iteration
    /// overshoots by design and is undone.</para>
    /// </remarks>
    public static int RollFrames(int fontHeight) => fontHeight + 3;

    /// <summary>
    /// How far the pair actually travels — and where the incoming letter starts, negated.
    /// </summary>
    /// <remarks>
    /// <c>fontHeight + 2</c>. The incoming letter is drawn exactly this far ABOVE the outgoing one
    /// (compare the body pass at <c>y - fontHeight - 2</c> against the outgoing's <c>y</c>), so
    /// after this much travel it has landed precisely where the old letter was. Any other figure
    /// leaves the wheel stopped between two letters.
    /// </remarks>
    public static int RollTravel(int fontHeight) => fontHeight + 2;

    /// <summary>
    /// Where the incoming letter sits relative to the outgoing one at the start of a roll.
    /// </summary>
    public static int IncomingLetterOffset(int fontHeight) => -RollTravel(fontHeight);

    // ------------------------------------------------------------------ reading it at all
    // CIPHER.C:112. The screen is built and rendered ONCE in the alien script, and only then does
    // it resolve — for a party that can read it.

    /// <summary>The script the riddle is always drawn in first.</summary>
    public const string AlienFont = "ALIEN.FNT";

    /// <summary>The script it resolves into, for a party that can read it.</summary>
    public const string PuzzleFont = "PUZZLE.FNT";

    /// <summary>
    /// The party member whose presence makes the riddle readable — <b>Gorath</b>.
    /// </summary>
    /// <remarks>
    /// A CHARACTER ID checked against the active party, not a roster slot: the original scans the
    /// active party for this id rather than indexing it. Character 1 is Gorath, and a moredhel
    /// reading moredhel script is the whole of the rule — it is not a skill check, and no amount of
    /// Assessment or Scouting substitutes for him.
    /// </remarks>
    public const int ReaderPartyMember = 1;

    /// <summary>
    /// The running-spell slot that makes it readable without him.
    /// </summary>
    /// <remarks>
    /// Slot 4 of the running-effects mask, which is Union — so the spell that joins minds is the
    /// one that lends his reading to the rest. See <see cref="Spells.SpellPaletteEvents"/> for why
    /// the slot is the DISPATCH order and not the spell number.
    /// </remarks>
    public const int ReaderSpellEvent = 4;

    /// <summary>Whether the party can read the riddle.</summary>
    /// <remarks>
    /// <b>Either route alone is enough</b> — the one who can read it, or the spell that lets anyone.
    /// </remarks>
    public static bool IsLegible(bool readerInParty, bool readerSpellActive) =>
        readerInParty || readerSpellActive;

    /// <summary>
    /// <b>THE ALIEN SCRIPT IS ALWAYS SHOWN FIRST, EVEN TO A PARTY THAT CAN READ IT.</b>
    /// </summary>
    /// <remarks>
    /// The screen is laid out and rendered in <see cref="AlienFont"/> before the legibility test is
    /// made; only afterwards does a party that can read it get a second render in
    /// <see cref="PuzzleFont"/>, brought in by a dissolve. So the riddle visibly TRANSFORMS in
    /// front of the player rather than simply appearing legible — and a party that cannot read it
    /// sees exactly what the other one saw for a moment.
    ///
    /// <para>Drawing straight in the readable font when legible skips that entirely, which is the
    /// obvious implementation and loses the only moment the alien script is ever seen by a party
    /// that could read it anyway.</para>
    /// </remarks>
    public static bool AlienIsAlwaysDrawnFirst => true;

    /// <summary>The font a render pass uses.</summary>
    /// <param name="firstPass">The unconditional pass; false for the resolve.</param>
    public static string FontForPass(bool firstPass, bool legible) =>
        firstPass || !legible ? AlienFont : PuzzleFont;

    /// <summary>The riddle screen's own music track.</summary>
    public const int MusicTrack = 0x3eb;

    /// <summary>Dialog played as the screen opens.</summary>
    public const int OpeningDialog = 0x0b;

    /// <summary>Dialog played once it has been drawn.</summary>
    /// <remarks>Before the legibility test, so both parties hear it.</remarks>
    public const int AfterDrawDialog = 0x0c;
}
