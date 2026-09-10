namespace GameData.Resources.Inventory;

using GameData.Resources.Object;

/// <summary>
/// Putting an item into a container that did not come from another container — what a dialog does
/// when it hands the party something (<c>cmbinv_actor_acquire_item</c>).
///
/// <para><see cref="InventoryTransfer"/> deliberately only moves between containers, because every
/// other path in the game is a move. This is the one that creates.</para>
/// </summary>
public static class InventoryAcquire {
    /// <summary>
    /// Gives an item to a container if it will fit.
    /// </summary>
    /// <returns>
    /// False when there is no room — and the caller must act on that, because the original charges
    /// for the item only when it was actually accepted.
    /// </returns>
    /// <param name="sharedKeys">
    /// The party's one shared keys inventory. A KEY never reaches the member's pack:
    /// <c>cmbinv_actor_acquire_item</c> (CMBINV.C:1008) opens with
    /// <c>if (rec-&gt;wCategory == 7) { cmbinv_actor_pickup_item(...); return 1; }</c>, before the
    /// space test. Null disables the diversion, which is what a save with no such container gets.
    ///
    /// <para><b>This is not cosmetic.</b> The picklock screen builds its working set from the ring,
    /// so a key that landed in a pack is invisible to every lock. Measured 2026-09-10: Jimmy the
    /// Hand's Royal Key of Krondor — the ONLY copy of object 71 the game hands out, and the only
    /// thing that opens the chapter-1 palace grate — went into Owyn's pack and the grate's working
    /// set showed a Peasant's Key and two picklocks.</para>
    /// </param>
    public static bool TryGive(RuntimeContainer container, RuntimeItem item, ObjectInfoSet objects,
        RuntimeContainer sharedKeys = null) {
        if (container == null || item == null) {
            return false;
        }
        if (sharedKeys != null && objects?.GetById(item.ObjectId)?.ObjectType == ObjectType.Key) {
            InventoryTransfer.AddKeyToRing(item, sharedKeys, objects);
            return true;
        }
        if (!InventoryTransfer.CanFit(container, item, objects)) {
            return false;
        }

        container.Items.Add(item);
        container.Dirty = true;
        // Same tidy-up any acquisition gets: the new item merges into an existing stack and the
        // grid re-sorts, so a gift of arrows lands on the stack already carried.
        InventoryOrder.Consolidate(container, objects,
            container.ContainerType == GameData.Resources.Data.SaveGameContainerType.Inventory);
        return true;
    }
}
