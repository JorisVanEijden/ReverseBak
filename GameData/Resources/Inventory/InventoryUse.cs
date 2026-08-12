namespace GameData.Resources.Inventory;

using GameData.Resources.Character;
using GameData.Resources.Object;
using System;

/// <summary>
/// What a use needs to know about the character doing it, for the branches that touch the character
/// rather than the item.
///
/// <para>Optional: <see cref="InventoryUse.Use"/> works without it and simply reports those
/// categories as unported, so callers that have no character to hand — tests, tools, a container
/// view — behave exactly as before.</para>
/// </summary>
public sealed class ItemUseContext {
    public ItemUseContext(ActorStat[] stats, int partySlot,
        Func<int, int> readFlag, Action<int, int> writeFlag, Func<int, int> random,
        ActorConditions conditions = null) {
        Stats = stats;
        PartySlot = partySlot;
        ReadFlag = readFlag;
        WriteFlag = writeFlag;
        Random = random;
        Conditions = conditions;
    }

    /// <summary>The character's live attributes, indexed by <see cref="ActorAttribute"/>.</summary>
    public ActorStat[] Stats { get; }

    /// <summary>The character's live afflictions, for the categories that set one.</summary>
    public ActorConditions Conditions { get; }

    /// <summary>1-based character slot — the original's <c>charSlot</c>, which is the 0-based
    /// position in the party record set plus one.</summary>
    public int PartySlot { get; }

    /// <summary>Reads a save-state flag.</summary>
    public Func<int, int> ReadFlag { get; }

    /// <summary>Writes a save-state flag.</summary>
    public Action<int, int> WriteFlag { get; }

    /// <summary>Returns a value in <c>[0, n)</c>.</summary>
    public Func<int, int> Random { get; }

    /// <summary>Whether this context can actually be used.</summary>
    public bool IsUsable =>
        Stats != null && PartySlot >= 1 && ReadFlag != null && WriteFlag != null && Random != null;
}

/// <summary>
/// The <c>outcome</c> local of <c>itemuse_dispatch_on_target</c> (ITEMUSE.C:91), which decides
/// what the common tail says and whether the item is spent. <see cref="NotPorted"/> is ours: it
/// marks a category the original dispatches but the remake cannot yet, and is deliberately
/// distinct from <see cref="NoEffect"/> — telling the player "nothing happens" when the original
/// would have healed them is a visible lie, so an unported branch stays silent instead.
/// </summary>
public enum ItemUseOutcome {
    NotPorted = -3,
    Silent = -2,
    Handled = -1,
    NoEffect = 0,
    Applied = 1,
}

/// <summary>What one item-use did: the original's outcome, the record it wants played, and
/// whether the used item left the container (so a caller re-renders rather than re-indexes).</summary>
public readonly struct ItemUseResult {
    public ItemUseResult(ItemUseOutcome outcome, int dialogId, int dialogVar0, bool sourceRemoved) {
        Outcome = outcome;
        DialogId = dialogId;
        DialogVar0 = dialogVar0;
        SourceRemoved = sourceRemoved;
    }

    public ItemUseOutcome Outcome { get; }

    /// <summary>DDX record to play, or 0 for none. Seed <see cref="DialogVar0"/> into Var 0 first —
    /// every one of these records is a text-less root that branches on it.</summary>
    public int DialogId { get; }

    public int DialogVar0 { get; }

    public bool SourceRemoved { get; }
}

/// <summary>
/// Using an item — <c>Use_Item</c> @0x58cbd / <c>itemuse_dispatch_on_target</c> (ITEMUSE.C:83-511),
/// minus the equip branch, which is <see cref="InventoryEquip.Equip"/>. Spec:
/// docs/specs/inventory-item-handling.md §17.
///
/// <para>This is the <b>item-on-item</b> half of the dispatch: the categories whose effect is to
/// rewrite another item in the same inventory — poisons and coatings, repair kits, bowstrings, and
/// two of the scripted specials. Everything else the original dispatches (potions, food, torches,
/// maps, combat items) needs a stat, timer or combat runtime the remake does not have; those
/// return <see cref="ItemUseOutcome.NotPorted"/> and are listed in spec §17.2.</para>
///
/// <para><b>On <c>wEffect_arg_a</c> / <c>wEffect_arg_b</c></b> (<see cref="ObjectInfo.EffectArgA"/>
/// and <see cref="ObjectInfo.EffectArgB"/>): both are polymorphic, read differently per category —
/// this class is the dispatch that decides which reading applies, and the full table is on
/// <see cref="ObjectInfo.EffectArgA"/>. For the coating categories (9, 10, 11) they are an
/// <see cref="ItemFlags"/> pair — <c>target.flags = (target.flags &amp; arg_b) | arg_a</c> — which
/// is why Althafain's Icer carries 0x400 (<see cref="ItemFlags.Frosted"/>) and 0xE07F (keep
/// everything but the other coatings). For a repair kit (category 8) <c>arg_a</c> is instead a bare
/// target <b>category number</b>: Whetstone 1 = Sword, Aventurine 2 = Crossbow, Armorer's Hammer
/// 4 = Armor.</para>
/// </summary>
public static class InventoryUse {
    /// <summary>Pass as <c>targetIndex</c> for a use with no second item (the Use button).</summary>
    public const int NoTarget = -1;

    private const int UsedRecord = 1800002;      // 0x1B7742, the tail's outcome-1 record
    private const int NoEffectRecord = 1800003;  // 0x1B7743, the tail's outcome-0 record
    private const int NoRepairRecord = 1800030;  // 0x1B775E, "this needs no repair", Var 0 = target

    // Object ids the dispatch keys on directly (ITEMUSE.C reads them as characters).
    private const byte CrystalStaffId = 1;         // the Raw Manna target
    private const byte RawMannaId = 14;            // 0x0e
    private const byte ShellId = 16;               // 0x10
    private const byte GuardaRevancheId = 22;      // 0x16
    private const byte ExoticSwordId = 23;         // 0x17
    private const byte FirstQuarrelId = 36;        // 0x24 Quarrels / Elven / Tsurani
    private const byte LastQuarrelId = 38;         // 0x26
    private const byte PoisonedQuarrelOffset = 3;  // 0x24..0x26 -> 0x27..0x29
    private const byte PoisonedRationsId = 73;     // 'I'
    private const byte RationPoisonId = 105;       // 'i', Coltari Poison
    private const byte LightBowstringId = 77;      // 'M'
    private const byte BessyMaulerId = 32;         // ' ', a heavy crossbow
    private const byte TsuraniHeavyCrossbowId = 34; // '"', the other heavy crossbow
    private const byte FullCondition = 100;        // 'd'

    // ObjectInfo.Flags bits the common tail reads (ITEMUSE.C:490-503).
    private const ushort ConsumedOnUse = (ushort)ObjectFlags.ConsumedOnUse;
    private const ushort DiscardWhenEmpty = (ushort)ObjectFlags.DiscardWhenEmpty;
    private const ushort ChargeBearing = (ushort)(ObjectFlags.LimitedUses | ObjectFlags.B8000);

    // ItemSlot.flags bits (spec §1).
    private const ushort Broken = (ushort)ItemFlags.Broken;
    private const ushort Repairable = (ushort)ItemFlags.Repairable;
    private const ushort Poisoned = (ushort)ItemFlags.Poisoned;
    private const ushort WearBits = Broken | Repairable; // ~0x30, cleared by a new bowstring

    /// <summary>
    /// The drag gesture's source filter (INVENTOR.C:797-806): dragging an item onto <i>another
    /// item</i> in the same inventory dispatches a use only for categories 8-12 and 25. Any other
    /// category snaps back — dragging a sword onto a potion is not a move and not a use.
    /// </summary>
    public static bool CanUseOnAnotherItem(ObjectType type) =>
        type == ObjectType.Repair
        || type == ObjectType.Poison
        || type == ObjectType.Enhancer
        || type == ObjectType.ClericalEnhancer
        || type == ObjectType.BowString
        || type == ObjectType.Usable;

    /// <summary>
    /// Use the item at <paramref name="sourceIndex"/>, optionally on the item at
    /// <paramref name="targetIndex"/> (<see cref="NoTarget"/> for the Use button's no-target form).
    /// Mutates the container in place, exactly as the original mutates the actor's item array, and
    /// reports what the caller should say and redraw.
    ///
    /// <para>The use-gate chain (ITEMUSE.C:104-131) is <b>not</b> run here: it needs the member
    /// (caster or not) and the combat flag, neither of which a container knows. Callers run it
    /// first — see <c>InventoryMenu.RefuseUse</c>.</para>
    /// </summary>
    public static ItemUseResult Use(RuntimeContainer container, int sourceIndex, int targetIndex,
        ObjectInfoSet objects, ItemUseContext context = null) {
        if (container == null || sourceIndex < 0 || sourceIndex >= container.Items.Count) {
            return new ItemUseResult(ItemUseOutcome.NotPorted, 0, 0, false);
        }
        RuntimeItem source = container.Items[sourceIndex];
        ObjectInfo rec = objects?.GetById(source.ObjectId);
        if (rec == null) {
            return new ItemUseResult(ItemUseOutcome.NotPorted, 0, 0, false);
        }
        // An item is never its own target: the gesture refuses it upstream
        // (focused->wAction_id != hovered->wAction_id), so the dispatch only ever sees a real
        // second item or none at all.
        RuntimeItem target = targetIndex >= 0 && targetIndex < container.Items.Count
            && targetIndex != sourceIndex
            ? container.Items[targetIndex]
            : null;
        ObjectInfo trec = target == null ? null : objects.GetById(target.ObjectId);

        var argA = (ushort)rec.EffectArgA;
        var argB = (ushort)rec.EffectArgB;
        ItemUseOutcome outcome;

        switch (rec.ObjectType) {
            case ObjectType.Poison:            // 9 — ITEMUSE.C:169-186
                outcome = UsePoison(source, target, trec, argA, argB);
                break;
            case ObjectType.Enhancer:          // 10 — ITEMUSE.C:188-206, target half only
                // The no-target half is the antidote ('q' on a poisoned member), which needs the
                // status-rank table. Its guard is "rank != 0", so with no target and no ranks
                // modelled we cannot tell "cures you" from "nothing happens".
                if (target == null) {
                    return new ItemUseResult(ItemUseOutcome.NotPorted, 0, 0, false);
                }
                outcome = Coat(target, trec, ObjectType.Armor, argA, argB);
                break;
            case ObjectType.ClericalEnhancer:  // 11 — ITEMUSE.C:208-217
                outcome = Coat(target, trec, ObjectType.Sword, ObjectType.Armor, argA, argB);
                break;
            case ObjectType.Repair:            // 8 — ITEMUSE.C:219-241
                return Repair(container, source, target, trec, rec, argA);
            case ObjectType.BowString:         // 12 — ITEMUSE.C:243-259
                outcome = Restring(source, target, trec);
                break;
            case ObjectType.Usable:            // 25 — ITEMUSE.C:386-459, two target-directed cases
                return UsableSpecial(container, sourceIndex, source, target, rec);
            case ObjectType.Restorative:       // 19 — ITEMUSE.C:330
                if (context == null || !context.IsUsable || context.Conditions == null) {
                    return new ItemUseResult(ItemUseOutcome.NotPorted, 0, 0, false);
                }
                return ApplyRestorative(container, sourceIndex, source, rec, context);
            case ObjectType.MassRestorative:   // 20 — ITEMUSE.C:258, stat_combatant_apply_condition
                if (context == null || !context.IsUsable || context.Conditions == null) {
                    return new ItemUseResult(ItemUseOutcome.NotPorted, 0, 0, false);
                }
                // EffectArgA is the affliction index and EffectArgB the amount, both read straight
                // through. The shipping pair reads exactly as you would hope: the Herbal Pack is
                // Healing +100 and the Ale Cask is Drunk +25 — which are also the two afflictions
                // the original never announces, so neither raises a condition event.
                ConditionEngine.Apply(context.Conditions, (ActorCondition)rec.EffectArgA,
                    rec.EffectArgB, HealthOf(context), StaminaOf(context), inCombat: false);
                outcome = ItemUseOutcome.Applied;
                break;
            case ObjectType.Book:              // 17 — ITEMUSE.C:265, itemuse_apply_stat_effects
                if (context == null || !context.IsUsable) {
                    // No character to apply it to; say nothing rather than claim no effect.
                    return new ItemUseResult(ItemUseOutcome.NotPorted, 0, 0, false);
                }
                ItemStatEffects.Apply(context.Stats, context.PartySlot, source, rec,
                    context.ReadFlag, context.WriteFlag, context.Random);
                // Outcome 1 REGARDLESS of whether the effect applied: the original sets it
                // unconditionally, so the read is spent — charge consumed, "used" record played —
                // even when the repeat-read roll fails. You pay for the reading, not the learning.
                outcome = ItemUseOutcome.Applied;
                break;
            default:
                return new ItemUseResult(ItemUseOutcome.NotPorted, 0, 0, false);
        }

        if (outcome != ItemUseOutcome.NoEffect) {
            container.Dirty = true;
        }
        return Tail(container, sourceIndex, rec, outcome);
    }

    /// <summary>
    /// Category 9 (ITEMUSE.C:169-186). Three mutually exclusive leaves, in the original's order —
    /// the first that matches the <i>item</i> wins, so Coltari Poison on a blade does nothing at
    /// all rather than falling through to the coating leaf.
    /// </summary>
    private static ItemUseOutcome UsePoison(RuntimeItem source, RuntimeItem target, ObjectInfo trec,
        ushort argA, ushort argB) {
        if (target == null) {
            return ItemUseOutcome.NoEffect;
        }
        if (source.ObjectId == RationPoisonId) {
            // 'i' on rations: the stack becomes the poisoned rations outright.
            if (trec?.ObjectType != ObjectType.Food) {
                return ItemUseOutcome.NoEffect;
            }
            target.ObjectId = PoisonedRationsId;
            return ItemUseOutcome.Handled;
        }
        if ((argA & Poisoned) != 0
            && target.ObjectId >= FirstQuarrelId && target.ObjectId <= LastQuarrelId) {
            // Each quarrel kind sits three ids below its poisoned twin.
            target.ObjectId = (byte)(target.ObjectId + PoisonedQuarrelOffset);
            return ItemUseOutcome.Handled;
        }
        return Coat(target, trec, ObjectType.Sword, argA, argB);
    }

    private static ItemUseOutcome Coat(RuntimeItem target, ObjectInfo trec, ObjectType accepted,
        ushort argA, ushort argB) => Coat(target, trec, accepted, accepted, argA, argB);

    /// <summary>
    /// The coating primitive shared by categories 9, 10 and 11:
    /// <c>target-&gt;flags &amp;= arg_b; target-&gt;flags |= arg_a</c>. arg_b keeps the bits the
    /// coating tolerates, so applying one coating replaces any other.
    /// </summary>
    private static ItemUseOutcome Coat(RuntimeItem target, ObjectInfo trec, ObjectType acceptedA,
        ObjectType acceptedB, ushort argA, ushort argB) {
        if (target == null || trec == null
            || (trec.ObjectType != acceptedA && trec.ObjectType != acceptedB)) {
            return ItemUseOutcome.NoEffect;
        }
        target.ItemFlags = (ushort)((target.ItemFlags & argB) | argA);
        return ItemUseOutcome.Applied;
    }

    /// <summary>
    /// Category 8, repair kits (ITEMUSE.C:219-241). <c>arg_a</c> is the category the kit works on.
    /// An item that carries no <see cref="ItemFlags.Repairable"/> refuses with its own record and
    /// returns before the tail, so the kit keeps its charge.
    ///
    /// <para>The repair itself — <c>condition += (100 - condition) * skill / 100</c> with skill =
    /// the member's ArmorCraft or WeaponCraft, followed by a skill-up — reads the stat runtime
    /// (base + permanent + timed modifiers) that the remake has no model for, so it reports
    /// <see cref="ItemUseOutcome.NotPorted"/> rather than repairing by a guessed amount.</para>
    /// </summary>
    private static ItemUseResult Repair(RuntimeContainer container, RuntimeItem source,
        RuntimeItem target, ObjectInfo trec, ObjectInfo rec, ushort argA) {
        if (target == null || trec == null || (int)trec.ObjectType != argA
            || (target.ItemFlags & Broken) != 0) {
            return new ItemUseResult(ItemUseOutcome.NoEffect, NoEffectRecord, source.ObjectId, false);
        }
        if ((target.ItemFlags & Repairable) == 0) {
            return new ItemUseResult(ItemUseOutcome.Handled, NoRepairRecord, target.ObjectId, false);
        }
        return new ItemUseResult(ItemUseOutcome.NotPorted, 0, 0, false);
    }

    /// <summary>
    /// Category 12, bowstrings (ITEMUSE.C:243-259). Weight has to match: the light string fits
    /// every crossbow <i>except</i> the two heavy ones, and the heavy string fits only those two.
    /// A fitted string sets the crossbow back to full condition and clears its wear bits.
    /// </summary>
    private static ItemUseOutcome Restring(RuntimeItem source, RuntimeItem target, ObjectInfo trec) {
        if (target == null || trec?.ObjectType != ObjectType.Crossbow) {
            return ItemUseOutcome.NoEffect;
        }
        bool heavyBow = target.ObjectId == BessyMaulerId || target.ObjectId == TsuraniHeavyCrossbowId;
        if (heavyBow == (source.ObjectId == LightBowstringId)) {
            return ItemUseOutcome.NoEffect;
        }
        target.Variable = FullCondition;
        target.ItemFlags = (ushort)(target.ItemFlags & ~WearBits);
        return ItemUseOutcome.Applied;
    }

    /// <summary>
    /// Category 25 is a switch on the object id, not a category effect (ITEMUSE.C:386-459). Only
    /// its two target-directed cases are portable today; the rest (the chest, the spyglass view,
    /// the lute, Pug's spell sharing) need screens or runtimes the remake lacks.
    /// </summary>
    private static ItemUseResult UsableSpecial(RuntimeContainer container, int sourceIndex,
        RuntimeItem source, RuntimeItem target, ObjectInfo rec) {
        switch (source.ObjectId) {
            case RawMannaId:
                return RechargeStaff(container, sourceIndex, source, target);
            case ShellId:
                return AwakenExoticSwords(container, target, rec, sourceIndex);
            default:
                return new ItemUseResult(ItemUseOutcome.NotPorted, 0, 0, false);
        }
    }

    /// <summary>
    /// Raw Manna poured into the Crystal Staff (ITEMUSE.C:434-452). Tops the staff up to full and
    /// spends only what fitted; a manna that is used up whole is removed. Both arms return before
    /// the common tail, so the charge bookkeeping here is the whole of it.
    /// </summary>
    private static ItemUseResult RechargeStaff(RuntimeContainer container, int sourceIndex,
        RuntimeItem source, RuntimeItem target) {
        if (target == null || target.ObjectId != CrystalStaffId) {
            return new ItemUseResult(ItemUseOutcome.NoEffect, NoEffectRecord, source.ObjectId, false);
        }
        if (target.Variable == FullCondition) {
            return new ItemUseResult(ItemUseOutcome.Handled, 0, 0, false); // full: silent no-op
        }
        bool removed = false;
        if (source.Variable + target.Variable > FullCondition) {
            source.Variable = (byte)(source.Variable - (FullCondition - target.Variable));
            target.Variable = FullCondition;
        } else {
            target.Variable = (byte)(target.Variable + source.Variable);
            InventoryTransfer.RemoveAt(container, sourceIndex);
            removed = true;
        }
        container.Dirty = true;
        return new ItemUseResult(ItemUseOutcome.Handled, UsedRecord, source.ObjectId, removed);
    }

    /// <summary>
    /// The Shell (ITEMUSE.C:446-459): every Exotic sword the member carries becomes a Guarda
    /// Revanche. Unless the Shell is used <i>on</i> an exotic sword, the party must already hold one
    /// for anything to happen.
    ///
    /// <para>The original reads <c>target-&gt;item_id</c> before testing target for NULL, so the
    /// Use button on a Shell dereferences a null far pointer; the remake treats a missing target as
    /// "not the exotic sword", which is what the count check then covers.</para>
    /// </summary>
    private static ItemUseResult AwakenExoticSwords(RuntimeContainer container, RuntimeItem target,
        ObjectInfo rec, int sourceIndex) {
        bool onExoticSword = target != null && target.ObjectId == ExoticSwordId;
        int converted = 0;
        if (!onExoticSword) {
            foreach (RuntimeItem item in container.Items) {
                if (item.ObjectId == ExoticSwordId) { converted++; }
            }
            if (converted == 0) {
                return new ItemUseResult(ItemUseOutcome.NoEffect, NoEffectRecord,
                    container.Items[sourceIndex].ObjectId, false);
            }
        }
        foreach (RuntimeItem item in container.Items) {
            if (item.ObjectId == ExoticSwordId) { item.ObjectId = GuardaRevancheId; }
        }
        container.Dirty = true;
        return Tail(container, sourceIndex, rec, ItemUseOutcome.Applied);
    }

    /// <summary>
    /// The common tail (ITEMUSE.C:485-503): the outcome's record, then the item's own cost.
    /// A single-use item goes entirely; a charge-bearing one loses a charge and, on its last,
    /// is either discarded or left empty depending on its record.
    /// </summary>
    /// <summary>How much every affliction except Healing is eased by, per dose.</summary>
    private const int RestorativeAfflictionRelief = -5;

    /// <summary>The heal aims the pool at this percentage of its maximum.</summary>
    private const int RestorativeHealTarget = 100;

    /// <summary>
    /// One dose of a restorative (ITEMUSE.C:330). Heals the pool and eases <b>every</b> affliction
    /// except Healing itself.
    /// </summary>
    /// <remarks>
    /// <para>Note what the two effect words mean here, which is nothing like the neighbouring
    /// category: <c>EffectArgA</c> is a heal amount and <c>EffectArgB</c> a random spread on top of
    /// it, not an affliction index and rank. The shipped "Restoratives" is 6 and 2, so a dose heals
    /// 6 or 7. Reading them as category 20 does would have applied Near-death.</para>
    ///
    /// <para>The branch returns <see cref="ItemUseOutcome.Handled"/> and consumes its own charge, so
    /// the shared tail neither plays a record nor decrements again — the original returns -1 for the
    /// same reason.</para>
    ///
    /// <para><b>The repeat prompt is not ported.</b> The original loops on DDX 1800004 ("use
    /// another?") and keeps dosing until the player declines or the item runs out. That loop is
    /// driven by a modal answer, so it belongs to the screen rather than here; one invocation is one
    /// dose, and the player clicks again. Faithful per dose, one prompt short of faithful overall.</para>
    /// </remarks>
    private static ItemUseResult ApplyRestorative(RuntimeContainer container, int sourceIndex,
        RuntimeItem source, ObjectInfo rec, ItemUseContext context) {
        int heal = rec.EffectArgA;
        if (rec.EffectArgB > 1) {
            heal += context.Random(rec.EffectArgB);
        }

        for (var i = 0; i < ActorConditions.Count; i++) {
            if (i != (int)ActorCondition.Healing) {
                ConditionEngine.Apply(context.Conditions, (ActorCondition)i, RestorativeAfflictionRelief);
            }
        }

        ActorStat health = HealthOf(context);
        ActorStat stamina = StaminaOf(context);
        if (health != null && stamina != null) {
            StatEngine.ModifyHealthPool(health, stamina, (long)heal << 8, RestorativeHealTarget,
                out _, context.Conditions[ActorCondition.NearDeath]);
        }

        bool removed = false;
        if (source.Variable > 1) {
            source.Variable--;
        } else {
            InventoryTransfer.RemoveAt(container, sourceIndex);
            removed = true;
        }
        container.Dirty = true;
        return new ItemUseResult(ItemUseOutcome.Handled, UsedRecord, source.ObjectId, removed);
    }

    // Only the Near-death branch of ConditionEngine.Apply reads these, and no shipping restorative
    // applies Near-death — but an override could, and passing them is what makes the collapse
    // behave rather than silently skipping the health reset.
    private static ActorStat HealthOf(ItemUseContext context) =>
        context.Stats.Length > (int)ActorAttribute.Health ? context.Stats[(int)ActorAttribute.Health] : null;

    private static ActorStat StaminaOf(ItemUseContext context) =>
        context.Stats.Length > (int)ActorAttribute.Stamina ? context.Stats[(int)ActorAttribute.Stamina] : null;

    private static ItemUseResult Tail(RuntimeContainer container, int sourceIndex, ObjectInfo rec,
        ItemUseOutcome outcome) {
        RuntimeItem source = container.Items[sourceIndex];
        if (outcome == ItemUseOutcome.NoEffect) {
            return new ItemUseResult(outcome, NoEffectRecord, source.ObjectId, false);
        }
        int dialogId = outcome == ItemUseOutcome.Applied ? UsedRecord : 0;
        var flags = (ushort)rec.Flags;
        bool removed = false;
        if (outcome == ItemUseOutcome.Applied || outcome == ItemUseOutcome.Handled) {
            if ((flags & ConsumedOnUse) != 0) {
                InventoryTransfer.RemoveAt(container, sourceIndex);
                removed = true;
            } else if ((flags & ChargeBearing) != 0) {
                if (source.Variable > 1) {
                    source.Variable--;
                } else if ((flags & DiscardWhenEmpty) != 0) {
                    InventoryTransfer.RemoveAt(container, sourceIndex);
                    removed = true;
                } else {
                    source.Variable = 0;
                }
            }
        }
        return new ItemUseResult(outcome, dialogId, source.ObjectId, removed);
    }
}
