namespace GameData.Resources.Dialog;

/// <summary>
/// The "asked about:" topic menu — IDA <c>BuildKeywordMenu</c> (ovr144 @0x4ae91).
///
/// <para>Builds the grid of keywords an NPC will currently talk about, from the dialog entry's
/// branches. Coordinates here are the original's <b>VGA 320×200</b> values, not canonical ones —
/// they describe the layout, and scaling belongs to whatever draws it.</para>
/// </summary>
public static class KeywordMenu {
    /// <summary>Columns the keywords are laid out in.</summary>
    public const int Columns = 4;

    /// <summary>Slots the menu normally has, the last of which is the farewell.</summary>
    public const int NormalSlots = 16;

    /// <summary>Slots when the keyword list is exactly full.</summary>
    public const int FullSlots = 17;

    /// <summary>Keywords that trigger the tightened layout — one short of <see cref="NormalSlots"/>.</summary>
    public const int TightLayoutCount = 16;

    /// <summary>Save-state key base for "this topic has been asked about": <c>7500 + globalKey</c>.</summary>
    public const int AskedFlagBase = 7500;

    /// <summary>Action id the farewell reports. Every keyword reports its own, well above this.</summary>
    public const int FarewellActionId = 1;

    /// <summary>Action ids for keywords start here and count up by branch index.</summary>
    public const int FirstKeywordActionId = 128;

    /// <summary>The farewell's label is a literal, not a keyword-table entry.</summary>
    public const string FarewellLabel = "GoodBye";

    /// <summary>The farewell sits apart from the grid, at its own column.</summary>
    public const int FarewellX = 237;

    /// <summary>Left edge of the first column.</summary>
    public const int GridLeft = 12;

    /// <summary>Horizontal step between columns.</summary>
    public const int ColumnWidth = 75;

    /// <summary>Top edge the rows are measured from.</summary>
    public const int GridTop = 125;

    /// <summary>Each entry's box.</summary>
    public const int EntryWidth = 70;

    /// <summary>Each entry's box.</summary>
    public const int EntryHeight = 13;

    /// <summary>
    /// Whether the menu opens at all.
    /// </summary>
    /// <remarks>
    /// <b>No available topic means no menu</b> — the function returns without building anything, so
    /// there is not even a farewell button to click. A caller that always shows the menu would put an
    /// empty box on screen where the original shows none.
    /// </remarks>
    public static bool Opens(int availableKeywords) => availableKeywords > 0;

    /// <summary>Slots the menu takes for a given number of available keywords.</summary>
    /// <remarks>
    /// <b>Sixteen available is the one special case</b>: the menu grows by a slot rather than
    /// dropping a keyword. Everything else fits in <see cref="NormalSlots"/>, which leaves fifteen
    /// for topics and one for the farewell.
    /// </remarks>
    public static int SlotCount(int availableKeywords) =>
        availableKeywords == TightLayoutCount ? FullSlots : NormalSlots;

    /// <summary>Index of the farewell, which is always the last slot.</summary>
    public static int FarewellSlot(int availableKeywords) => SlotCount(availableKeywords) - 1;

    /// <summary>Vertical step between rows.</summary>
    /// <remarks>
    /// <b>A full list tightens the rows to limit how far the extra one reaches.</b> The step drops by
    /// a pixel and the top inset disappears — which buys nine pixels, not a free row: the menu still
    /// grows downward, just less than it otherwise would. Do not read the tightening as fitting five
    /// rows into four rows' space.
    /// </remarks>
    public static int RowHeight(int availableKeywords) =>
        availableKeywords == TightLayoutCount ? 14 : 15;

    /// <summary>Extra inset above the first row; zero when the list is full.</summary>
    public static int TopInset(int availableKeywords) =>
        availableKeywords == TightLayoutCount ? 0 : 5;

    /// <summary>Where a slot's box goes.</summary>
    public static (int X, int Y) SlotPosition(int slot, int availableKeywords) => (
        ((slot % Columns) * ColumnWidth) + GridLeft,
        ((slot / Columns) * RowHeight(availableKeywords)) + TopInset(availableKeywords) + GridTop);

    /// <summary>The save-state key that records a topic as already asked about.</summary>
    public static int AskedFlag(int globalKey) => AskedFlagBase + globalKey;

    /// <summary>
    /// Whether a topic is shown as already covered.
    /// </summary>
    /// <remarks>
    /// <b>An asked-about topic stays on the menu and changes appearance</b> — it is built as a
    /// different element kind rather than being dropped, so the player can see what they have
    /// already covered and still re-ask it. Filtering them out would quietly rewrite the
    /// conversation.
    /// </remarks>
    public static bool AlreadyAsked(int askedFlagValue) => askedFlagValue != 0;

    /// <summary>The action id a keyword reports when picked.</summary>
    /// <remarks>
    /// Keyed on the <b>branch index</b>, not on the keyword — so the same topic in two entries
    /// reports different ids, and an id only means anything against the entry it came from.
    /// </remarks>
    public static int ActionIdFor(int branchIndex) => FirstKeywordActionId + branchIndex;

    /// <summary>Index into the keyword table for a branch's label.</summary>
    /// <remarks>The table is <b>1-based</b> against the branch's global key.</remarks>
    public static int LabelIndexFor(int globalKey) => globalKey - 1;
}
