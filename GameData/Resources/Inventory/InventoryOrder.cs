namespace GameData.Resources.Inventory;

using GameData.Resources.Object;

/// <summary>
/// Faithful port of the inventory ordering pass the original runs on screen entry and after every
/// item operation: <c>cmbinv_consolidate_stacks</c> (CMBINV.C:570) — a fixpoint loop of
/// <c>cmbinv_combat_sort_initiative</c> (CMBINV.C:545, bubble sort via the
/// <c>cmbinv_item_compare</c> keys — canassa's name is wrong, it sorts inventory slots and has
/// nothing to do with combat initiative; IDA has it as <c>inventory_Sort</c> @0x54fc3) followed by
/// an adjacent-pair stack merge. Mutates the
/// container's item order/stacks exactly like the DOS engine mutates the actor's item array
/// (the new order is save-visible there too).
/// </summary>
public static class InventoryOrder {
    private const ushort EquippedFlag = (ushort)ItemFlags.Equipped;
    private const int StackableFlag = 0x800;        // ObjectFlags.Stackable (wFlags & 0x800)

    /// <summary>Sort + merge stacks until stable. <paramref name="equippedOrder"/> is the original's
    /// <c>bResidence == RES_PARTY_SLOT</c>: party members float equipped items to the front by
    /// descending category. Returns true when anything changed (callers mark the container dirty).</summary>
    public static bool Consolidate(RuntimeContainer container, ObjectInfoSet objects, bool equippedOrder) {
        if (container == null || container.Items.Count < 2) {
            return false;
        }
        bool changed = false;
        bool done;
        do {
            done = true;
            changed |= Sort(container, objects, equippedOrder);
            for (int i = 1; i < container.Items.Count; i++) {
                RuntimeItem a = container.Items[i - 1];
                RuntimeItem b = container.Items[i];
                ObjectInfo rec = objects?.GetById(a.ObjectId);
                if (rec == null || a.ObjectId != b.ObjectId) {
                    continue;
                }
                if (((int)rec.Flags & StackableFlag) == 0) {
                    continue;
                }
                int max = rec.MaxAmount;
                if (b.Variable >= max || a.Variable >= max) {
                    continue;
                }
                int sum = a.Variable + b.Variable;
                done = false;
                changed = true;
                if (sum <= max) {
                    a.Variable = (byte)sum;
                    // swap-with-last removal, like the original's itemCount-- + copy-from-end
                    int last = container.Items.Count - 1;
                    container.Items[i] = container.Items[last];
                    container.Items.RemoveAt(last);
                } else {
                    a.Variable = (byte)(sum - max); // the remainder stack; the full one sorts ahead next pass
                    b.Variable = (byte)max;
                }
            }
        } while (!done);
        return changed;
    }

    /// <summary>One full bubble sort (cmbinv_combat_sort_initiative, sort_mode 0 — the shop-price
    /// mode is out of scope). Returns true if any swap happened.</summary>
    public static bool Sort(RuntimeContainer container, ObjectInfoSet objects, bool equippedOrder) {
        bool changed = false;
        bool swapped;
        do {
            swapped = false;
            for (int i = 0; i < container.Items.Count - 1; i++) {
                if (ShouldSwap(container.Items[i], container.Items[i + 1], objects, equippedOrder)) {
                    (container.Items[i], container.Items[i + 1]) = (container.Items[i + 1], container.Items[i]);
                    swapped = true;
                    changed = true;
                }
            }
        } while (swapped);
        return changed;
    }

    // cmbinv_item_compare (CMBINV.C:484), sort_mode==0 branch. Sort keys, in order: equipped
    // category descending (party only), footprint (InventorySlots) descending, item id ascending,
    // then stack count ascending — except percent-condition items, whose condition is not a key.
    private static bool ShouldSwap(RuntimeItem a, RuntimeItem b, ObjectInfoSet objects, bool equippedOrder) {
        ObjectInfo ra = objects?.GetById(a.ObjectId);
        ObjectInfo rb = objects?.GetById(b.ObjectId);
        if (ra == null || rb == null) {
            return false;
        }
        if (equippedOrder) {
            int catA = (a.ItemFlags & EquippedFlag) != 0 ? (int)ra.ObjectType : 0;
            int catB = (b.ItemFlags & EquippedFlag) != 0 ? (int)rb.ObjectType : 0;
            if (catA < catB) { return true; }
            if (catA > catB) { return false; }
        }
        int qtyA = ra.InventorySlots != 0 ? ra.InventorySlots : 1;
        int qtyB = rb.InventorySlots != 0 ? rb.InventorySlots : 1;
        if (qtyA < qtyB) { return true; }
        if (qtyA > qtyB) { return false; }
        if (a.ObjectId > b.ObjectId) { return true; }
        if (a.ObjectId < b.ObjectId) { return false; }
        if ((ra.Flags & ObjectFlags.Degradable) != 0) {
            return false;
        }
        return a.Variable > b.Variable;
    }
}
