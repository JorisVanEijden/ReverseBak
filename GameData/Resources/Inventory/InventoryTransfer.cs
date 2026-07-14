namespace GameData.Resources.Inventory;

using GameData.Resources.Data;
using GameData.Resources.Object;

/// <summary>
/// Faithful port of the corpse-looting item move (KRONDOR.EXE sub_ovr157_16B2 @0x558c2 +
/// the currency short-circuit in sub_ovr157_134E @0x5555e). Currency converts to the gold
/// scalar; other items obey CanFit + stacking; source is compacted swap-with-last.
/// Equip-swap / keys / lit-torch special cases are out of scope (no corpse item needs them).
/// </summary>
public static class InventoryTransfer {
    public enum Result { Moved, GoldConverted, DoesNotFit, Blocked }

    public const int GoldSovereignId = 53;
    public const int SilverRoyalId = 54;
    private const int CharSlotBudget = 20;
    private const int OtherSlotBudget = 28;

    public static Result Move(RuntimeContainer source, int itemIndex, RuntimeContainer target,
        ObjectInfoSet objects, ref int partyGold) {
        if (source == null || target == null || itemIndex < 0 || itemIndex >= source.Items.Count) {
            return Result.Blocked;
        }
        RuntimeItem item = source.Items[itemIndex];

        // Currency: convert straight into the gold scalar; no carried item.
        if (item.ObjectId == GoldSovereignId || item.ObjectId == SilverRoyalId) {
            partyGold += item.ObjectId == GoldSovereignId ? item.Variable * 10 : item.Variable;
            RemoveAt(source, itemIndex);
            return Result.GoldConverted;
        }

        int maxAmount = objects?.GetById(item.ObjectId)?.MaxAmount ?? 1;

        // Stacking: merge into an existing identical stack if the object is stackable.
        if (maxAmount > 1) {
            foreach (RuntimeItem t in target.Items) {
                if (t.ObjectId == item.ObjectId && t.Variable < maxAmount) {
                    int room = maxAmount - t.Variable;
                    int give = item.Variable <= room ? item.Variable : room;
                    t.Variable = (byte)(t.Variable + give);
                    target.Dirty = true;
                    if (give >= item.Variable) { RemoveAt(source, itemIndex); }
                    else { item.Variable = (byte)(item.Variable - give); source.Dirty = true; }
                    return Result.Moved;
                }
            }
        }

        if (!CanFit(target, item, objects)) {
            return Result.DoesNotFit;
        }
        target.Items.Add(item.Clone());
        target.Dirty = true;
        RemoveAt(source, itemIndex);
        return Result.Moved;
    }

    // item_equipped (DOS ItemFlags bit 0x40, see GameData.ItemFlags.Equipped): equipped gear is
    // excluded from a character's slot-footprint sums by canItemFitInContainer.
    private const ushort ItemEquippedFlag = (ushort)GameData.ItemFlags.Equipped;

    // canItemFitInContainer @0x551ec: per-container count cap + a two-pass slot-footprint budget.
    // Pass 1 sums only multi-slot (footprint > 1) items, incl. the incoming item, and requires
    // multiSlotSum + 4 <= budget. Pass 2 sums ALL items' footprints, incl. the incoming item, and
    // requires total <= budget (no slack). Both passes must pass. For a character inventory
    // (ContainerType == SaveGameContainerType.Inventory) currently-equipped items are excluded
    // from both sums.
    public static bool CanFit(RuntimeContainer target, RuntimeItem item, ObjectInfoSet objects) {
        if (target.Items.Count >= target.Capacity) { return false; }
        bool isChar = target.ContainerType == SaveGameContainerType.Inventory;
        int budget = isChar ? CharSlotBudget : OtherSlotBudget;
        int multiSlot = 0, total = 0;
        foreach (RuntimeItem t in target.Items) {
            if (isChar && (t.ItemFlags & ItemEquippedFlag) != 0) { continue; } // equipped excluded from footprint
            int s = Slots(t, objects);
            total += s;
            if (s > 1) { multiSlot += s; }
        }
        int inc = Slots(item, objects);
        total += inc;
        if (inc > 1) { multiSlot += inc; }
        if (multiSlot + 4 > budget) { return false; } // pass 1: multi-slot footprints
        return total <= budget;                        // pass 2: all footprints, no slack
    }

    private static int Slots(RuntimeItem it, ObjectInfoSet objects) {
        int s = objects?.GetById(it.ObjectId)?.InventorySlots ?? 1;
        return s <= 0 ? 1 : s; // howManyInventorySlots: default 1 when the record says 0
    }

    // RemoveItemFromContainer @0x554ef: swap the last item into the removed slot.
    private static void RemoveAt(RuntimeContainer c, int index) {
        int last = c.Items.Count - 1;
        c.Items[index] = c.Items[last];
        c.Items.RemoveAt(last);
        c.Dirty = true;
    }
}
