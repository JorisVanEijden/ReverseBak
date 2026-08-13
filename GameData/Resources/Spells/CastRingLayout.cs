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
}
