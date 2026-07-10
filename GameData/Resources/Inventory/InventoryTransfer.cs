namespace GameData.Resources.Inventory;

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
    private const int ContainerTypeCharacterInventory = 1;

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

    // canItemFitInContainer @0x551ec: per-container count cap + slot-footprint budget.
    public static bool CanFit(RuntimeContainer target, RuntimeItem item, ObjectInfoSet objects) {
        if (target.Items.Count >= target.Capacity) { return false; }
        int budget = target.ContainerType == ContainerTypeCharacterInventory ? CharSlotBudget : OtherSlotBudget;
        int used = 0;
        foreach (RuntimeItem t in target.Items) { used += Slots(t, objects); }
        return used + Slots(item, objects) <= budget + 4; // +4 slack (multi-slot pass allowance)
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
