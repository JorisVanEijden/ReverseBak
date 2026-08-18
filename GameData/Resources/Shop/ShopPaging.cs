namespace GameData.Resources.Shop;

/// <summary>
/// Turning the pages of a shop's shelf — the action-50 arm of <c>sub_ovr157_4E3</c> @0x54d0a.
/// </summary>
/// <remarks>
/// A shelf holds six items and the rest are reached by paging, so the control is one button that
/// means three different things depending on the shift keys. The page is a PAGE NUMBER, not an item
/// offset: the screen multiplies it by six on the way into the cell builder.
/// </remarks>
public static class ShopPaging {
    /// <summary>Items on one page — the builder's hard <c>slot &lt; 6</c> bound.</summary>
    public const int PageSize = 6;

    /// <summary>What the button does, by which shift key is down.</summary>
    public enum Turn {
        /// <summary>Plain click: the next page, wrapping to the first.</summary>
        Next,

        /// <summary>Left shift: back one, stopping at the first.</summary>
        Previous,

        /// <summary>Right shift: straight back to the first.</summary>
        First,
    }

    /// <summary>
    /// The page a click lands on.
    /// </summary>
    /// <remarks>
    /// <b>Only forward wraps.</b> Going back stops at the first page rather than jumping to the
    /// last, and the wrap is checked after every turn — including the backward ones, though they
    /// cannot trip it. So a shelf whose stock shrank while a later page was showing snaps to the
    /// front rather than displaying nothing.
    /// </remarks>
    public static int Turned(int page, int itemCount, Turn turn) {
        int next = turn switch {
            Turn.Previous => page > 0 ? page - 1 : page,
            Turn.First => 0,
            _ => page + 1,
        };

        return itemCount <= next * PageSize ? 0 : next;
    }

    /// <summary>Whether the shelf needs the control at all — more stock than one page holds.</summary>
    public static bool Pages(int itemCount) => itemCount > PageSize;

    /// <summary>The index of the first item on <paramref name="page"/>.</summary>
    public static int FirstItem(int page) => page * PageSize;
}
