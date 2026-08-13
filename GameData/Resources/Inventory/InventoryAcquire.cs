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
    public static bool TryGive(RuntimeContainer container, RuntimeItem item, ObjectInfoSet objects) {
        if (container == null || item == null) {
            return false;
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
