namespace GameData.Resources.Inventory;

using GameData.Resources.Data;
using GameData.Resources.Object;
using System.Collections.Generic;

/// <summary>
/// Faithful port of the inventory item move (KRONDOR.EXE transferItem @0x5555e +
/// pickupItem @0x558c2; spec docs/specs/inventory-item-handling.md §2, §4, §7, §13-§15).
///
/// <para>The transfer is split into <see cref="Plan"/> + <see cref="Apply"/> because the
/// original can stop mid-move to ask an amount (quantityPickerDialog @0x59EA1) and this
/// library cannot show UI: <see cref="Plan"/> resolves — and executes — everything that
/// needs no answer (equip swap/refusals, currency, auto-equip, plain/merging moves) and
/// otherwise describes the picker to run; the screen shows it and hands the answer to
/// <see cref="Apply"/> (or <see cref="Distribute"/> for Share). <see cref="Move"/> is the
/// non-interactive wrapper: it answers every picker with "all".</para>
///
/// <para>The starving-ration auto-consume is still out of scope (no status system yet).</para>
/// </summary>
public static class InventoryTransfer {
    public enum Result {
        Moved,
        GoldConverted,
        DoesNotFit,
        Blocked,
        /// <summary>Equipped cat-1/3 swap: both items stay equipped, one on each member.</summary>
        SwappedEquipped,
        /// <summary>Refused: an equipped melee weapon/staff never unequips (DDX 1800014).
        /// Also the active-ring guard (pickupItem @0x558c2, id 6 + flag 1 — same dialog).</summary>
        MustKeepEquipped,
        /// <summary>Refused: a lit torch cannot be moved (DDX 1800029).</summary>
        LitTorchRefused,
        /// <summary>The quantity picker was cancelled (Esc, or accepting at 0) — nothing moved.</summary>
        Cancelled,
    }

    public const int GoldSovereignId = 53;
    public const int SilverRoyalId = 54;
    private const int TorchId = 0x54;
    private const int RingOfPrandurId = 6;
    private const ushort ActivatedFlag = 0x0001;      // ItemSlot flag 1: lit torch / active ring
    private const int StackableFlag = 0x800;          // record wFlags: merges up to bMax_stack
    private const int CountableFlag = 0x8000;         // record wFlags: shows (N), always asks quantity
    private const int CharSlotBudget = 20;
    private const int OtherSlotBudget = 28;

    /// <summary>
    /// The outcome of <see cref="Plan"/>: either <see cref="Immediate"/> is set (the transfer
    /// already happened — or was refused — with that result), or the quantity picker described
    /// by <see cref="MaxQuantity"/>/<see cref="AllowShare"/>/<see cref="IsTopUp"/> must run and
    /// its answer go to <see cref="Apply"/> (N), <see cref="Distribute"/> (Share) or nowhere
    /// (cancel).
    /// </summary>
    public sealed class TransferPlan {
        public Result? Immediate { get; internal set; }
        /// <summary>Picker maximum: the stack count, or the destination stack's headroom on the
        /// top-up path. The picker starts at this value (spec §14).</summary>
        public int MaxQuantity { get; internal set; }
        /// <summary>Offer "Share with party" (destination is a party member, not in combat).</summary>
        public bool AllowShare { get; internal set; }
        /// <summary>Partial top-up of an existing destination stack (spec §15) — the source
        /// slot survives with at least 1.</summary>
        public bool IsTopUp { get; internal set; }
        internal RuntimeContainer Source;
        internal RuntimeContainer Target;
        internal ObjectInfoSet Objects;
        internal int ItemIndex;
        internal int TopUpIndex = -1;
    }

    /// <summary>Non-interactive move: every picker question is answered with "all".</summary>
    public static Result Move(RuntimeContainer source, int itemIndex, RuntimeContainer target,
        ObjectInfoSet objects, ref int partyGold, bool targetIsCaster = false,
        RuntimeContainer sharedKeys = null) {
        TransferPlan plan = Plan(source, itemIndex, target, objects, ref partyGold, targetIsCaster,
            allowShare: false, sharedKeys: sharedKeys);
        return plan.Immediate ?? Apply(plan, plan.MaxQuantity);
    }

    /// <param name="targetIsCaster">Caster-ness of the receiving party member (drives the
    /// auto-equip categories); ignored when the target is not a member inventory.</param>
    /// <param name="allowShare">Whether a picker, if one is needed, offers "Share with party"
    /// (pickupItem passes: destination is a party member and not in combat).</param>
    /// <param name="sharedKeys">The party's one shared keys inventory
    /// (<c>g_gameState.shared_inventory</c>). Keys bound for a party member land here instead —
    /// see the cat-7 branch below. Null disables the diversion (keys then take the normal path),
    /// which is what a save with no such container gets.</param>
    public static TransferPlan Plan(RuntimeContainer source, int itemIndex, RuntimeContainer target,
        ObjectInfoSet objects, ref int partyGold, bool targetIsCaster = false,
        bool allowShare = false, RuntimeContainer sharedKeys = null) {
        var plan = new TransferPlan {
            Source = source, Target = target, Objects = objects, ItemIndex = itemIndex,
        };
        if (source == null || target == null || itemIndex < 0 || itemIndex >= source.Items.Count) {
            plan.Immediate = Result.Blocked;
            return plan;
        }
        RuntimeItem item = source.Items[itemIndex];
        ObjectInfo rec = objects?.GetById(item.ObjectId);
        ObjectType category = rec?.ObjectType ?? ObjectType.Misc;

        // transferItem @0x5555e first branch (CMBINV.C:758-771): an equipped melee weapon or
        // staff never unequips. Member->member where the target also has one of the same
        // category equipped swaps the two slots in place — both stay equipped, no space checks.
        // Every other destination refuses (caller plays DDX 1800014). Crossbow and armor are
        // deliberately NOT in this guard: they take the normal path below, which clears the
        // flag and re-runs auto-equip at the destination.
        if ((item.ItemFlags & ItemEquippedFlag) != 0
            && source.ContainerType == SaveGameContainerType.Inventory
            && (category == ObjectType.Sword || category == ObjectType.Staff)) {
            if (target.ContainerType == SaveGameContainerType.Inventory) {
                int other = InventoryEquip.FindEquippedIndex(target, category, objects);
                if (other >= 0) {
                    source.Items[itemIndex] = target.Items[other];
                    target.Items[other] = item;
                    source.Dirty = target.Dirty = true;
                    plan.Immediate = Result.SwappedEquipped;
                    return plan;
                }
            }
            plan.Immediate = Result.MustKeepEquipped;
            return plan;
        }

        // pickupItem @0x558c2 entry guards: a lit torch refuses to move (DDX 1800029), an
        // active Ring of Prandur too (DDX 1800014).
        if ((item.ItemFlags & ActivatedFlag) != 0) {
            if (item.ObjectId == TorchId) {
                plan.Immediate = Result.LitTorchRefused;
                return plan;
            }
            if (item.ObjectId == RingOfPrandurId) {
                plan.Immediate = Result.MustKeepEquipped;
                return plan;
            }
        }

        // pickupItem @0x558c2 (CMBINV.C:881-901), immediately after those guards: a key bound for
        // a PARTY MEMBER never enters that member's pack — it diverts to the party's one shared
        // keys inventory. One slot per key kind: a kind already on the ring just bumps its count,
        // a new kind is appended with count 1. There is no fit test on this path at all (the ring
        // never refuses), and no equip/stack/picker handling — it returns straight away.
        if (sharedKeys != null && category == ObjectType.Key
            && target.ContainerType == SaveGameContainerType.Inventory) {
            plan.Immediate = AddToKeyring(source, itemIndex, item, sharedKeys, objects);
            return plan;
        }

        // Currency: convert straight into the gold scalar; no carried item.
        if (item.ObjectId == GoldSovereignId || item.ObjectId == SilverRoyalId) {
            partyGold += item.ObjectId == GoldSovereignId ? item.Variable * 10 : item.Variable;
            RemoveAt(source, itemIndex);
            plan.Immediate = Result.GoldConverted;
            return plan;
        }

        // classifyItemPickup @0x55482: auto-equip is decided before any fit test, so an
        // incoming weapon/armor lands on the empty paperdoll slot even when the grid budget is
        // full (equipped items are outside it). Only the raw count capacity is still respected.
        // Equippables are never stackable, so this can't shadow a picker.
        if (InventoryEquip.CanAutoEquip(target, item, targetIsCaster, objects)
            && target.Items.Count < target.Capacity) {
            RuntimeItem equipped = item.Clone();
            equipped.ItemFlags = (ushort)(equipped.ItemFlags | ItemEquippedFlag);
            target.Items.Add(equipped);
            target.Dirty = true;
            RemoveAt(source, itemIndex);
            plan.Immediate = Result.Moved;
            return plan;
        }

        int flags = rec != null ? (int)rec.Flags : 0;

        // Room classification (classify 1/2): fits as-is; else consolidate the destination and
        // retry; else acceptable anyway when the WHOLE amount merges into one existing stack
        // (canMergeIntoExistingStack @0x552F9 — never a silent partial merge, spec §13.2).
        bool hasRoom = CanFit(target, item, objects);
        if (!hasRoom) {
            InventoryOrder.Consolidate(target, objects,
                target.ContainerType == SaveGameContainerType.Inventory);
            hasRoom = CanFit(target, item, objects);
        }
        if (!hasRoom && rec != null && (flags & StackableFlag) != 0) {
            foreach (RuntimeItem t in target.Items) {
                if (t.ObjectId == item.ObjectId && t.Variable + item.Variable <= rec.MaxAmount) {
                    hasRoom = true; // classify 2 — the insert below merges via consolidation
                    break;
                }
            }
        }

        if (hasRoom) {
            // The quantity picker (spec §14): countables always ask; member->member moves of
            // any stackable ask too. Stacks below 2 skip the dialog and move whole
            // (quantityPickerDialog @0x59EA1 returns the count unasked).
            bool asks = (flags & CountableFlag) != 0
                || ((flags & StackableFlag) != 0
                    && source.ContainerType == SaveGameContainerType.Inventory
                    && target.ContainerType == SaveGameContainerType.Inventory);
            if (asks && item.Variable >= 2) {
                plan.MaxQuantity = item.Variable;
                plan.AllowShare = allowShare;
                return plan; // the screen runs the picker and calls Apply/Distribute
            }
            plan.Immediate = InsertAndConsolidate(source, itemIndex, target, objects,
                item.Variable, wholeStack: true);
            return plan;
        }

        // classify 0: no room at all. Last resort is the partial top-up of an existing stack
        // (findStackHeadroom @0x55386 — record mask 0x8800, either bit; spec §15).
        if (rec != null && (flags & (StackableFlag | CountableFlag)) != 0) {
            for (int i = 0; i < target.Items.Count; i++) {
                RuntimeItem t = target.Items[i];
                if (t.ObjectId == item.ObjectId && t.Variable < rec.MaxAmount) {
                    int headroom = rec.MaxAmount - t.Variable;
                    plan.TopUpIndex = i;
                    plan.IsTopUp = true;
                    if (headroom >= 2) {
                        plan.MaxQuantity = headroom;
                        return plan; // picker capped at the headroom, no Share
                    }
                    plan.Immediate = TopUp(plan, headroom); // a 1-headroom tops up unasked
                    return plan;
                }
            }
        }
        plan.Immediate = Result.DoesNotFit;
        return plan;
    }

    /// <summary>
    /// Execute a <see cref="Plan"/> that asked for a quantity. <paramref name="quantity"/> 0
    /// cancels (nothing moves); the full amount moves the slot; a partial amount clones it —
    /// destination and (for partials) source consolidate, exactly like pickupItem @0x558c2.
    /// Share (-1 from the picker) goes to <see cref="Distribute"/> instead, never here.
    /// </summary>
    public static Result Apply(TransferPlan plan, int quantity) {
        if (plan == null || plan.Immediate != null) {
            return plan?.Immediate ?? Result.Blocked;
        }
        if (quantity <= 0) {
            return Result.Cancelled;
        }
        if (plan.ItemIndex >= plan.Source.Items.Count) {
            return Result.Blocked; // container changed under the picker — refuse, don't guess
        }
        if (quantity > plan.MaxQuantity) {
            quantity = plan.MaxQuantity;
        }
        if (plan.IsTopUp) {
            return TopUp(plan, quantity);
        }
        RuntimeItem item = plan.Source.Items[plan.ItemIndex];
        return InsertAndConsolidate(plan.Source, plan.ItemIndex, plan.Target, plan.Objects,
            quantity, wholeStack: quantity >= item.Variable);
    }

    /// <summary>
    /// "Share with party" (distributeStackToParty @0x5579E, CMBINV.C:825): the stack splits
    /// across every recipient with room, in order, as progressive integer portions
    /// (remaining × assigned ÷ recipientCount — small stacks can leave early recipients
    /// empty-handed, faithfully). Zero recipients with room refuses; otherwise the source slot
    /// is removed first. The starving-member ration auto-consume is a deliberate gap (no
    /// status system yet).
    /// </summary>
    /// <summary>Share, answered from a picker: distributes the plan's item (the plan itself
    /// must be the pending-quantity kind).</summary>
    public static Result Distribute(TransferPlan plan, IReadOnlyList<RuntimeContainer> recipients) {
        if (plan == null || plan.Immediate != null || plan.IsTopUp) {
            return plan?.Immediate ?? Result.Blocked; // top-up pickers have no Share button
        }
        return Distribute(plan.Source, plan.ItemIndex, recipients, plan.Objects);
    }

    public static Result Distribute(RuntimeContainer source, int itemIndex,
        IReadOnlyList<RuntimeContainer> recipients, ObjectInfoSet objects) {
        if (source == null || recipients == null
            || itemIndex < 0 || itemIndex >= source.Items.Count) {
            return Result.Blocked;
        }
        RuntimeItem item = source.Items[itemIndex];
        int recipientCount = 0;
        foreach (RuntimeContainer r in recipients) {
            if (r != null && CanFit(r, item, objects)) {
                recipientCount++;
            }
        }
        if (recipientCount == 0) {
            return Result.DoesNotFit;
        }
        RuntimeItem copy = item.Clone();
        int remaining = copy.Variable;
        RemoveAt(source, itemIndex);
        int assigned = 0;
        foreach (RuntimeContainer r in recipients) {
            if (r == null || !CanFit(r, copy, objects)) {
                continue;
            }
            assigned++;
            int portion = remaining * assigned / recipientCount;
            if (portion == 0) {
                continue;
            }
            remaining -= portion;
            RuntimeItem part = copy.Clone();
            part.Variable = (byte)portion;
            r.Items.Add(part);
            r.Dirty = true;
            InventoryOrder.Consolidate(r, objects,
                r.ContainerType == SaveGameContainerType.Inventory);
        }
        return Result.Moved;
    }

    // The keyring insert (pickupItem @0x558c2 / CMBINV.C:886-900). The original asks
    // itemtbl_inv_count_by_kind first and then bumps EVERY slot of that kind — a loop that can
    // only ever see one, since the else-branch is the only thing that appends. Reproduced as
    // written rather than "find the one slot", so a save that somehow carries two slots of one
    // key behaves as the original would. The key's own count is discarded: a picked-up key is
    // worth exactly one on the ring.
    private static Result AddToKeyring(RuntimeContainer source, int itemIndex, RuntimeItem item,
        RuntimeContainer keys, ObjectInfoSet objects) {
        bool alreadyHeld = false;
        foreach (RuntimeItem held in keys.Items) {
            if (held.ObjectId == item.ObjectId) {
                held.Variable = (byte)(held.Variable + 1);
                alreadyHeld = true;
            }
        }
        if (!alreadyHeld) {
            RuntimeItem added = item.Clone();
            added.Variable = 1;
            keys.Items.Add(added);
        }
        keys.Dirty = true;
        InventoryOrder.Sort(keys, objects, equippedOrder: false); // cmbinv_combat_sort_initiative
        RemoveAt(source, itemIndex);
        return Result.Moved;
    }

    // The shared insert: destination receives a clone carrying `amount`, arriving unequipped
    // (pickupItem: flags &= ~0x40), and consolidates (which performs any stack merging — the
    // original inserts then consolidates too, so a full-fit merge and a plain landing are the
    // same code). Whole moves remove the source slot; partials leave the remainder and
    // consolidate the source as well.
    private static Result InsertAndConsolidate(RuntimeContainer source, int itemIndex,
        RuntimeContainer target, ObjectInfoSet objects, int amount, bool wholeStack) {
        RuntimeItem item = source.Items[itemIndex];
        RuntimeItem moved = item.Clone();
        moved.ItemFlags = (ushort)(moved.ItemFlags & ~ItemEquippedFlag);
        if (!wholeStack) {
            moved.Variable = (byte)amount;
        }
        target.Items.Add(moved);
        target.Dirty = true;
        if (wholeStack) {
            RemoveAt(source, itemIndex);
        } else {
            item.Variable = (byte)(item.Variable - amount);
            source.Dirty = true;
            InventoryOrder.Consolidate(source, objects,
                source.ContainerType == SaveGameContainerType.Inventory);
        }
        InventoryOrder.Consolidate(target, objects,
            target.ContainerType == SaveGameContainerType.Inventory);
        return Result.Moved;
    }

    // The top-up path (pickupItem @0x558c2, CMBINV.C:980-995): bump the destination stack
    // (clamped to bMax_stack) and reduce the source — which is NEVER removed here; giving
    // everything leaves 1 behind, the original's own quirk.
    private static Result TopUp(TransferPlan plan, int quantity) {
        RuntimeItem item = plan.Source.Items[plan.ItemIndex];
        if (plan.TopUpIndex < 0 || plan.TopUpIndex >= plan.Target.Items.Count) {
            return Result.Blocked;
        }
        RuntimeItem stack = plan.Target.Items[plan.TopUpIndex];
        int max = plan.Objects?.GetById(item.ObjectId)?.MaxAmount ?? 1;
        if (item.Variable < quantity) {
            item.Variable = (byte)quantity; // faithful defensive clamp before the subtract
        }
        int sum = stack.Variable + quantity;
        stack.Variable = (byte)(sum > max ? max : sum);
        item.Variable = (byte)(item.Variable - quantity);
        if (item.Variable == 0) {
            item.Variable = 1; // the keep-1 quirk: this path never empties the source slot
        }
        plan.Source.Dirty = plan.Target.Dirty = true;
        InventoryOrder.Consolidate(plan.Source, plan.Objects,
            plan.Source.ContainerType == SaveGameContainerType.Inventory);
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

    // RemoveItemFromContainer @0x554ef: swap the last item into the removed slot. Internal rather
    // than private because using an item consumes it the same way (InventoryUse's common tail).
    internal static void RemoveAt(RuntimeContainer c, int index) {
        int last = c.Items.Count - 1;
        c.Items[index] = c.Items[last];
        c.Items.RemoveAt(last);
        c.Dirty = true;
    }
}
