namespace GameData.Resources.Inventory;

/// <summary>
/// Read-only questions about what a container holds — the original's
/// <c>itemtbl_inv_count_by_kind</c>, which spell components, ration counts, dialog conditions and
/// the shop stock check all share.
/// </summary>
public static class InventoryQuery {
    /// <summary>
    /// How many of an object a container holds.
    ///
    /// <para><b>This counts charges, not items.</b> An entry with a non-zero
    /// <see cref="RuntimeItem.Variable"/> contributes that value; only an entry whose Variable is
    /// zero counts as one. So a quiver of 20 arrows answers 20, and — the case that matters for
    /// spell components — <b>a stack whose charges have run out answers 0 and reads as absent</b>,
    /// even though the item is still sitting in the pack.</para>
    ///
    /// <para>The original's parameter is named <c>kind</c>, which invites reading it as an item
    /// category; the body matches it against <c>item_id</c>, so it is an object id.</para>
    /// </summary>
    public static int CountByKind(RuntimeContainer container, int objectId) {
        if (container == null) {
            return 0;
        }
        var total = 0;
        foreach (RuntimeItem item in container.Items) {
            if (item.ObjectId != objectId) {
                continue;
            }
            total += item.Variable != 0 ? item.Variable : 1;
        }
        return total;
    }
}
