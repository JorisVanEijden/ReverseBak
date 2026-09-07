namespace GameData.Resources.Shop;

using GameData.Resources.Object;

/// <summary>
/// The name line of one shelf cell — <c>invui_grid_render</c> (<c>SRC/SCREENS/INVENTOR.C</c>).
/// </summary>
/// <remarks>
/// <b>A shop cell carries the condition or count INSIDE the name, not as a corner label.</b> The
/// ordinary inventory grid draws the figure in the cell's bottom-right; the shop's name line takes
/// its place, because the cell's bottom two lines are the name and the price. Dropping it — which
/// the port did — loses the only indication of a weapon's condition or a stack's size at the point
/// where the player is deciding whether to pay for it.
///
/// <para><b>The flag test is not the grid's.</b> INVENTOR.C:465-468 is
/// <c>if (wFlags &amp; 0xa000) " (%d)"; else if (wFlags &amp; 0x1000) " (%d%%)"</c> — one mask,
/// two arms, and neither is the corner label's rule (which additionally wants <c>0x8</c> for the
/// percentage and treats <c>0x3000</c> as select-only). Reusing the corner rule here gets Quarrels
/// right by luck and a charged item wrong.</para>
///
/// <para>Measured against the original at Fletcher's Post, LaMut (2026-09-07): "Broadsword (100%)",
/// "Light Crossbow (100%)", "Quarrels (25)", "Elven Quarrels (25)".</para>
/// </remarks>
public static class ShopCellLabel {
    /// <summary>Flags whose items show a plain count — <c>0x8000 | 0x2000</c>.</summary>
    private const int CountMask = 0xa000;

    /// <summary>Flags whose items show a percentage instead.</summary>
    private const int PercentMask = 0x1000;

    /// <summary>
    /// The cell's name, split across the one or two lines the original draws it on.
    /// </summary>
    /// <remarks>
    /// <b><see cref="ObjectInfo.WordWrap"/> is the split point, and it is authored per object.</b>
    /// INVENTOR.C:453-463 nul-terminates the name at that index, draws the head one font-height
    /// higher, and starts the second line at <c>split + 1</c> — so "Standard Kingdom Armor"
    /// (WordWrap 16) is "Standard Kingdom" over "Armor", and "Tsurani Light Crossbow" (13) is
    /// "Tsurani Light" over "Crossbow". Zero means the name is short enough for one line.
    ///
    /// <para><b>The suffix always rides on the LAST line</b>, because the original appends it to
    /// <c>buf</c> after the head has already been drawn.</para>
    ///
    /// <para>This is what keeps a long name inside its cell. The original never wraps or fits text
    /// to a box — it centres one line and lets it overhang — so without the authored split a name
    /// like "Standard Kingdom Armor (100%)" runs straight into the next cell's text.</para>
    /// </remarks>
    /// <param name="name">The item's name from <see cref="ObjectInfo"/>.</param>
    /// <param name="wordWrap">That object's <see cref="ObjectInfo.WordWrap"/> split index.</param>
    /// <param name="flags">That object's flags.</param>
    /// <param name="condition">
    /// The slot's <c>condition</c> byte — a percentage for a degradable item, a count for a stack.
    /// </param>
    /// <returns>
    /// <c>First</c> is null when the name fits one line; <c>Last</c> always carries the suffix.
    /// </returns>
    public static (string First, string Last) LinesFor(
        string name, int wordWrap, ObjectFlags flags, int condition) {
        name ??= string.Empty;

        string head = null;
        string tail = name;
        if (wordWrap > 0 && wordWrap < name.Length) {
            head = name.Substring(0, wordWrap);
            tail = name.Substring(wordWrap + 1);
        }

        return (head, Suffixed(tail, flags, condition));
    }

    /// <summary>
    /// Whether a cell's text needs the black outline the original draws behind it.
    /// </summary>
    /// <remarks>
    /// <b>The test is the item's bulk, and the original spells it <c>== 4</c>.</b>
    /// <c>invui_grid_render</c> guards each of the three text draws with
    /// <c>if (wDefault_qty_or_1 == 4)</c> and, when it holds, draws the same string first at
    /// (x-1, y-1) in colour 0. The eleven items carrying a 4 are exactly the bulky ones — four
    /// staves, six armours and the bag of grain — whose sprites are tall enough to reach down into
    /// the name lines. The original does not move the text out of the way; it outlines it.
    ///
    /// <para>Not <c>&gt;= 4</c>: nothing in the shipped table exceeds 4, so the two agree today and
    /// only the equality is evidenced.</para>
    /// </remarks>
    /// <param name="inventorySlots">
    /// <see cref="ObjectInfo.InventorySlots"/> — the <c>wDefault_qty_or_1</c> word, by position in
    /// <c>ItemRecord</c>.
    /// </param>
    public static bool NeedsTextOutline(int inventorySlots) => inventorySlots == BulkySlots;

    /// <summary>The slot count the original tests for.</summary>
    private const int BulkySlots = 4;

    /// <summary>The condition or count the original appends to the last name line.</summary>
    private static string Suffixed(string line, ObjectFlags flags, int condition) {
        var bits = (int)flags;
        if ((bits & CountMask) != 0) {
            return $"{line} ({condition})";
        }

        return (bits & PercentMask) != 0 ? $"{line} ({condition}%)" : line;
    }
}
