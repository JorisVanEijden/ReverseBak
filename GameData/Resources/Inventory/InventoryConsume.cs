namespace GameData.Resources.Inventory;

using GameData.Resources.Object;
using System;

/// <summary>
/// Taking one of something out of an inventory by object id — <c>itemtbl_inv_consume_one_by_kind</c>
/// (canassa <c>SRC/GAME/ACTOR/ITEMTBL.C</c>). This is how the game spends things it was not explicitly
/// told to use: the daily ration, a spell's material component, an item a combat action needs.
///
/// <para>Distinct from <see cref="InventoryUse"/>'s tail, which spends the charge of an item the
/// player pointed at. This one searches for a kind and has its own branch order.</para>
/// </summary>
public static class InventoryConsume {
    /// <summary>
    /// Torches are excluded outright (<c>kind == 0x54</c>), so nothing that consumes by kind can
    /// burn the party's light source out from under them — a torch is spent by its own timer.
    /// </summary>
    public const int TorchObjectId = 0x54;

    private const ushort LimitedUses = (ushort)ObjectFlags.LimitedUses;
    private const ushort DiscardWhenEmpty = (ushort)ObjectFlags.DiscardWhenEmpty;

    /// <summary>
    /// Take one <paramref name="objectId"/> from <paramref name="inventory"/>. Returns false when
    /// there is none to take, which is the caller's cue that the party goes without.
    ///
    /// <para>A stack loses one from its count. A single item is removed outright — unless its
    /// record says it has limited uses but is <i>not</i> discarded when empty, in which case it is
    /// left in place at zero (a container that empties rather than vanishes). An item already at
    /// zero does not count as a successful take, and the search carries on to the next candidate.</para>
    /// </summary>
    /// <param name="lookup">Object id → record; pass <c>objectInfoSet.GetById</c>.</param>
    public static bool TryConsumeOne(RuntimeContainer inventory, int objectId,
        Func<int, ObjectInfo> lookup) {
        if (inventory == null) {
            throw new ArgumentNullException(nameof(inventory));
        }
        if (objectId == TorchObjectId) {
            return false;
        }

        for (int i = 0; i < inventory.Items.Count; i++) {
            RuntimeItem item = inventory.Items[i];
            if (item.ObjectId != objectId) {
                continue;
            }

            inventory.Dirty = true;
            if (item.Variable > 1) {
                item.Variable--;
                return true;
            }

            ushort flags = (ushort)(lookup?.Invoke(objectId)?.Flags ?? 0);
            if ((flags & LimitedUses) != 0 && (flags & DiscardWhenEmpty) == 0) {
                bool hadSomething = item.Variable != 0;
                item.Variable = 0;
                if (hadSomething) {
                    return true;
                }
                continue; // an empty one is not a meal; keep looking
            }

            InventoryTransfer.RemoveAt(inventory, i);
            return true;
        }
        return false;
    }
}
