namespace GameData.Resources.Spells;

using System.Collections.Generic;

/// <summary>
/// The casting ring's shape and its hit-testing — <c>cspell_hotspot_cursor_in_range</c> and
/// <c>cspell_hotspot_hittest_at_cursor</c> (<c>SRC/COMBAT/SPELL/CSPELL.C</c>).
///
/// <para>Positions come from <c>RING.DAT</c>; this is only the arithmetic over them.</para>
/// </summary>
public static class CastRingLayout {
    /// <summary>Positions on the ring.</summary>
    public const int PositionCount = 30;

    /// <summary>Spell categories the ring is divided into.</summary>
    public const int CategoryCount = 6;

    /// <summary>Positions per category — five, the last of which is the category's anchor.</summary>
    public const int PositionsPerCategory = PositionCount / CategoryCount;

    /// <summary>
    /// The nominal hit box, as the original writes it.
    /// <b>It accepts nine pixels, not ten</b> — see <see cref="Contains"/>.
    /// </summary>
    public const int HitBoxSize = 10;

    /// <summary>
    /// Which category a ring position belongs to.
    /// </summary>
    /// <summary>The bitmap set the ring's icons come from.</summary>
    public const string IconSet = "CASTFACE.BMX";

    /// <summary>
    /// Added to the base icon for a category anchor.
    /// </summary>
    /// <remarks>
    /// Two, not one — the set is not a plain sequence, so an anchor is the base icon's index plus
    /// two rather than the next one along.
    /// </remarks>
    public const int AnchorIconOffset = 2;

    /// <summary>
    /// The icon a ring position draws.
    /// </summary>
    /// <param name="baseIcon">The icon the caller is drawing the ring with.</param>
    /// <param name="position">Ring position, 0-based.</param>
    /// <param name="markAnchors">
    /// Whether the six category anchors are drawn differently. The caller passes this per pass —
    /// the ring is drawn more than once, and not every pass distinguishes them.
    /// </param>
    /// <remarks>
    /// <b>The anchor test is on the position, not on the data.</b> The original computes
    /// <c>(position + 1) % 5 == 0</c> rather than reading a flag, which is the same six slots
    /// <see cref="RingPosition.IsCategoryAnchor"/> carries — verified against the extracted RING.DAT,
    /// where the flagged indices are exactly 4, 9, 14, 19, 24 and 29.
    /// </remarks>
    public static int IconFor(int baseIcon, int position, bool markAnchors) =>
        markAnchors && IsAnchor(position) ? baseIcon + AnchorIconOffset : baseIcon;

    /// <summary>Whether a ring position is one of the six category anchors.</summary>
    public static bool IsAnchor(int position) => (position + 1) % PositionsPerCategory == 0;

    /// <summary>
    /// <b>Each icon is drawn one pixel up and left of its stored position.</b>
    /// </summary>
    /// <remarks>
    /// The original passes <c>x - 1, y - 1</c> for every ring icon. One VGA pixel, so five canonical
    /// units across and six down — small, but it is the difference between the icon sitting on its
    /// ring position and sitting just off it, thirty times over.
    /// </remarks>
    public const int IconDrawOffsetX = -5;

    /// <inheritdoc cref="IconDrawOffsetX"/>
    public const int IconDrawOffsetY = -6;

    public static int CategoryOf(int position) => position / PositionsPerCategory;

    /// <summary>
    /// The anchor position of a category — <b>the last of its five</b>, at <c>5c + 4</c>. Confirmed
    /// against the shipped RING.DAT, whose anchors sit at 4, 9, 14, 19, 24 and 29.
    /// </summary>
    public static int AnchorPositionOf(int category) =>
        (category * PositionsPerCategory) + PositionsPerCategory - 1;

    /// <summary>
    /// Whether the cursor is on a point.
    /// </summary>
    /// <remarks>
    /// <b>The comparisons are strict at both ends, so a nominally 10-wide box accepts nine
    /// pixels</b> — with the low edge at <c>x - 5</c>, the accepted range is <c>x-4 … x+4</c>.
    /// Using an inclusive test would make every ring position and spell symbol one pixel easier to
    /// hit than the original, which matters on a ring whose points sit close together.
    /// </remarks>
    public static bool Contains(int pointX, int pointY, int cursorX, int cursorY) {
        int lowX = pointX - (HitBoxSize / 2);
        int lowY = pointY - (HitBoxSize / 2);
        return cursorX > lowX && cursorX < lowX + HitBoxSize
            && cursorY > lowY && cursorY < lowY + HitBoxSize;
    }

    /// <summary>
    /// The ring position under the cursor, restricted to a band of indices.
    ///
    /// <para>The band is how the power slider limits selection to the spell's affordable range; the
    /// scan still walks all thirty positions and rejects those outside it, so a point outside the
    /// band is simply not clickable rather than being clamped to the nearest one that is.</para>
    /// </summary>
    /// <returns>The position index, or -1 for none. <b>First match by index wins</b>, not nearest.</returns>
    public static int PositionAt(IReadOnlyList<RingPosition> positions, int cursorX, int cursorY,
        int minIndex = 0, int maxIndex = PositionCount - 1) {
        if (positions == null) {
            return -1;
        }
        for (var i = 0; i < positions.Count; i++) {
            RingPosition p = positions[i];
            if (p != null && Contains(p.X, p.Y, cursorX, cursorY) && i >= minIndex && i <= maxIndex) {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// The spell symbol under the cursor.
    /// </summary>
    /// <param name="isCastable">
    /// Asked for each candidate symbol's spell. <b>An uncastable spell's symbol is not clickable at
    /// all</b> — the original folds the castability check into the hit test rather than merely
    /// greying the glyph, so the cursor falls through it to whatever is behind.
    /// </param>
    /// <returns>The index into the symbol list, or -1.</returns>
    public static int SymbolAt(IReadOnlyList<(int X, int Y, int SpellId)> symbols,
        int cursorX, int cursorY, System.Func<int, bool> isCastable) {
        if (symbols == null) {
            return -1;
        }
        for (var i = 0; i < symbols.Count; i++) {
            (int x, int y, int spellId) = symbols[i];
            if (Contains(x, y, cursorX, cursorY) && (isCastable == null || isCastable(spellId))) {
                return i;
            }
        }
        return -1;
    }
    // ---------------------------------------------------------------- the power slider
    // UI_SelectSpellCost @0x69718.

    /// <summary>
    /// The power a ring position stands for: <b>the position plus one</b>.
    /// </summary>
    /// <remarks>
    /// Positions are zero-based and powers are one-based, and the routine converts at every single
    /// use — the hit test returns a position, the info preview is shown the position plus one, and
    /// the committed cost is the position plus one. Carrying the position through as if it were the
    /// power makes every cast one point weak.
    /// </remarks>
    public static int PowerAtPosition(int ringPosition) => ringPosition + 1;

    /// <summary>The position that offers a given power.</summary>
    public static int PositionForPower(int power) => power - 1;

    /// <summary>
    /// <b>A click commits the hovered power outright — there is no confirm step.</b>
    /// </summary>
    /// <remarks>
    /// The click is tested <i>inside</i> the branch that runs only when the cursor is over a
    /// selectable position, so clicking anywhere off the affordable band does nothing at all: not a
    /// cancel, not a clamp, just no effect. Together with the hit test refusing positions outside
    /// the band, that means the slider has no way to select an unaffordable power even momentarily.
    /// </remarks>
    public static bool ClickCommitsImmediately => true;

    /// <summary>
    /// <b>The info panel previews the power under the cursor, not the one selected.</b>
    /// </summary>
    /// <remarks>
    /// Every frame the panel is redrawn for the hovered position, so the damage and cost readout
    /// tracks the mouse before anything is committed. When the cursor is over no valid position it
    /// is redrawn with a cost of zero rather than left showing the last value — so the readout
    /// resets as you leave the band instead of going stale.
    /// </remarks>
    public static int PreviewPower(int hoveredPosition) =>
        hoveredPosition < 0 ? 0 : PowerAtPosition(hoveredPosition);

    /// <summary>The value returned when the slider is cancelled.</summary>
    public const int Cancelled = -1;

    /// <summary>
    /// Escape cancels, and the routine <b>waits for the key to come back up</b> before returning.
    /// </summary>
    /// <remarks>
    /// Without that wait the same keypress would be seen again by the screen underneath and cancel
    /// that too. It is the sort of thing a port on event-driven input gets for free and a port
    /// polling a key table does not.
    /// </remarks>
    public static bool CancelWaitsForKeyRelease => true;
}
