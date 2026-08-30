namespace GameData.Resources.Inventory;

using GameData.Resources.Character;
using GameData.Resources.Object;
using GameData.Resources.Spells;
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
        ActorConditions conditions = null, ushort[] knownSpells = null,
        Character.ActorStatModifiers.Slot[] statModifiers = null, uint gameTime = 0) {
        Stats = stats;
        PartySlot = partySlot;
        ReadFlag = readFlag;
        WriteFlag = writeFlag;
        Random = random;
        Conditions = conditions;
        KnownSpells = knownSpells;
        StatModifiers = statModifiers;
        GameTime = gameTime;
    }

    /// <summary>The character's live attributes, indexed by <see cref="ActorAttribute"/>.</summary>
    public ActorStat[] Stats { get; }

    /// <summary>The character's live afflictions, for the categories that set one.</summary>
    public ActorConditions Conditions { get; }

    /// <summary>The character's live known-spell words, for the scroll that teaches one.</summary>
    public ushort[] KnownSpells { get; }

    /// <summary>
    /// The character's own eight timed modifier slots, for the potion category that fills one.
    /// </summary>
    /// <remarks>
    /// <b>This character's slots, not the whole party's block.</b> The caller does the addressing —
    /// the table is six characters wide and indexed by ROSTER position, which is not the same as a
    /// combatant's place in the active party. Handing over just the eight keeps that mistake out of
    /// here entirely.
    /// </remarks>
    public Character.ActorStatModifiers.Slot[] StatModifiers { get; }

    /// <summary>Game time in two-second ticks, for stamping and expiring those slots.</summary>
    public uint GameTime { get; }

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
    public ItemUseResult(ItemUseOutcome outcome, int dialogId, int dialogVar0, bool sourceRemoved,
        int musicTrack = Audio.MusicPlayback.QueryOnly) {
        Outcome = outcome;
        DialogId = dialogId;
        DialogVar0 = dialogVar0;
        SourceRemoved = sourceRemoved;
        MusicTrack = musicTrack;
    }

    public ItemUseOutcome Outcome { get; }

    /// <summary>DDX record to play, or 0 for none. Seed <see cref="DialogVar0"/> into Var 0 first —
    /// every one of these records is a text-less root that branches on it.</summary>
    public int DialogId { get; }

    public int DialogVar0 { get; }

    public bool SourceRemoved { get; }

    /// <summary>
    /// A track to play <b>while <see cref="DialogId"/> is on screen</b>, putting back whatever was
    /// playing once it closes. <see cref="Audio.MusicPlayback.QueryOnly"/> — the default — means
    /// this use does not touch the music.
    /// </summary>
    /// <remarks>
    /// <b>Interrupts rather than replaces.</b> The one use that sets it (the practice lute) saves
    /// the outgoing track and restores it, so the tune is heard over the top of whatever the party
    /// was listening to and the zone's music comes straight back.
    ///
    /// <para><see cref="Audio.MusicPlayback.QueryOnly"/> rather than
    /// <see cref="Audio.MusicPlayback.NoTrack"/> for "nothing to do", because NoTrack means
    /// <i>silence</i> — every ordinary item use would then stop the music.</para>
    /// </remarks>
    public int MusicTrack { get; }
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
                return UsableSpecial(container, sourceIndex, source, target, rec, context);
            case ObjectType.MagicalScroll:     // 13 — ITEMUSE.C:262, combat_actor_bitmap_set_bit
                if (context == null || !context.IsUsable || context.KnownSpells == null) {
                    return new ItemUseResult(ItemUseOutcome.NotPorted, 0, 0, false);
                }
                // The scroll's spell number lives in its condition byte, the same field the shop
                // prices a scroll by. Learning reports success only when the spell was NOT already
                // known, and the tail spends the item only on success — so re-reading a scroll you
                // have already learned says "nothing happens" AND KEEPS THE SCROLL.
                outcome = SpellBook.Learn(context.KnownSpells, source.Variable)
                    ? ItemUseOutcome.Applied
                    : ItemUseOutcome.NoEffect;
                break;
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
            case ObjectType.Potion:            // 18 — ITEMUSE.C:302-329
                if (context?.StatModifiers == null) {
                    return new ItemUseResult(ItemUseOutcome.NotPorted, 0, 0, false);
                }
                return DrinkPotion(container, sourceIndex, rec, context);
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
            case ObjectType.Note:              // 16 — ITEMUSE.C:267-299
                if (context == null) {
                    return new ItemUseResult(ItemUseOutcome.NotPorted, 0, 0, false);
                }
                return UseNote(source, context);
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
        RuntimeItem source, RuntimeItem target, ObjectInfo rec, ItemUseContext context) {
        switch (source.ObjectId) {
            case RawMannaId:
                return RechargeStaff(container, sourceIndex, source, target);
            case ShellId:
                return AwakenExoticSwords(container, target, rec, sourceIndex);
            case Audio.MusicSelection.PracticeLuteItemId:
                return PractiseLute(container, sourceIndex, source, rec, context);
            default:
                return new ItemUseResult(ItemUseOutcome.NotPorted, 0, 0, false);
        }
    }

    /// <summary>
    /// Playing the practice lute — <c>ITEMUSE.C</c>'s <c>case 0x51</c>, the item-81 arm of the
    /// category-25 switch.
    /// </summary>
    /// <remarks>
    /// Four things happen, in this order, and the order is the interesting part:
    /// <list type="number">
    ///   <item>the player's <b>Barding</b> is read;</item>
    ///   <item>a tune is chosen from it (<see cref="Audio.MusicSelection.ForLutePractice"/>) and
    ///     started, keeping whatever was playing;</item>
    ///   <item>the "you use it" record plays;</item>
    ///   <item><b>only then</b> is Barding raised, and the saved track put back.</item>
    /// </list>
    ///
    /// <para><b>So you always hear the tune for the skill you had BEFORE practising</b>, never the
    /// one the practice just earned you. Raising the skill first would let a player at the top of a
    /// band hear the better tune on the very run that got them there.</para>
    ///
    /// <para><b>The gain is a FRACTION of a point.</b> The roll goes to the stat modifier unshifted
    /// where every other caller shifts by eight, so one practice is worth roughly a sixth to
    /// two-thirds of a point — see
    /// <see cref="Audio.MusicSelection.PracticeGainIsFractional"/>. It banks in the stat's
    /// experience remainder like any other sub-unit change.</para>
    ///
    /// <para><b>The Barding read is the EFFECTIVE value, not the stored one</b> (<c>mode 0</c>), so
    /// it carries the actor's modifiers and the health scaling with it: <b>a wounded musician plays
    /// a worse tune</b>, and recovers the better one by resting rather than by practising.</para>
    ///
    /// <para><b>THE LUTE IS A CHARGED ITEM, so the common tail has to run.</b> Its arm sets
    /// <c>outcome = -1</c> and <c>break</c>s — it does not return — so control reaches
    /// <c>done:</c>, which spends a use and discards the lute when the last one goes. The shipped
    /// record carries <c>LimitedUses | DiscardWhenEmpty</c>, so this is not hypothetical: skipping
    /// the tail (which the first cut of this did, while its own comment claimed otherwise) gives an
    /// infinite lute. The tail is asked for <see cref="ItemUseOutcome.Handled"/>, which adds no
    /// record of its own — the arm has already named one, and the original's tail plays its record
    /// only for <c>outcome == 1</c>, so the message is heard exactly once.</para>
    /// </remarks>
    private static ItemUseResult PractiseLute(RuntimeContainer container, int sourceIndex,
        RuntimeItem source, ObjectInfo rec, ItemUseContext context) {
        if (context == null || !context.IsUsable || context.Random == null) {
            return new ItemUseResult(ItemUseOutcome.NotPorted, 0, 0, false);
        }

        ActorStat barding = StatOf(context, ActorAttribute.Barding);
        ActorStat health = HealthOf(context);
        if (barding == null || health == null) {
            return new ItemUseResult(ItemUseOutcome.NotPorted, 0, 0, false);
        }

        int skill = StatEngine.Get(barding, ActorAttribute.Barding, health);
        int track = Audio.MusicSelection.ForLutePractice(skill);

        // RNDR(low, high) — inclusive of both ends, so the span is high - low + 1.
        int gain = Audio.MusicSelection.PracticeGainLow
            + context.Random(Audio.MusicSelection.PracticeGainHigh
                - Audio.MusicSelection.PracticeGainLow + 1);
        StatEngine.Modify(barding, ActorAttribute.Barding, gain, StatChangeMode.Absolute);

        byte objectId = source.ObjectId;
        container.Dirty = true;
        ItemUseResult tail = Tail(container, sourceIndex, rec, ItemUseOutcome.Handled);
        return new ItemUseResult(tail.Outcome, UsedRecord, objectId, tail.SourceRemoved, track);
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
    /// <summary>Shown when a potion is refused — <c>dialog_play_record(0x1b7760)</c>.</summary>
    /// <remarks>
    /// <b>An item's refusal SPEAKS, and a spell status' does not.</b> The two insertion paths
    /// diverge here as well as on the dedupe test: a potion that does nothing says so, which is why
    /// this returns a dialog rather than a silent <c>NoEffect</c>.
    /// </remarks>
    public const int PotionRefusedRecord = 0x1b7760;

    /// <summary>
    /// Drinking a stat potion — <c>ITEMUSE.C:302-329</c>, effect category 0x12.
    /// </summary>
    /// <remarks>
    /// <b>THE FIELD NAMES MISLEAD AND THREE OF THE FOUR ARE READ FOR SOMETHING ELSE HERE.</b> The
    /// record's words are named for what they hold in other categories:
    /// <see cref="ObjectInfo.EffectArgA"/> is the modifier's FLAGS word,
    /// <see cref="ObjectInfo.EffectArgB"/> is the STAT MASK,
    /// <see cref="ObjectInfo.UseEffectAmount"/> (canassa's <c>wEffect_chance_pct</c>) is the VALUE
    /// and not a percentage, and <see cref="ObjectInfo.EffectDurationHours"/> (its
    /// <c>wEffect_stat_value</c>) is the DURATION and not a stat. Reading them by the names gives a
    /// chance where the value belongs.
    ///
    /// <para><b>The dedupe is STRICTER than the spell path's.</b> ANY non-empty slot on that stat
    /// refuses it — there is no exemption for spell statuses, so where two casts of a debuff stack,
    /// a potion never stacks with anything. And it sweeps every slot for expiry first, the same as
    /// the spell path, so a lapsed modifier does not block a fresh drink.</para>
    ///
    /// <para>All five shipped potions carry flags 0x0200: Expires SET and CombatOnly CLEAR. So an
    /// item's buff really does lapse, and unlike a spell status it applies out of combat too — the
    /// flags come from the item's own record, so that is data rather than a rule.</para>
    /// </remarks>
    private static ItemUseResult DrinkPotion(RuntimeContainer container, int sourceIndex,
        ObjectInfo rec, ItemUseContext context) {
        Character.ActorStatModifiers.Slot[] slots = context.StatModifiers;
        Character.ActorStatModifiers.SweepExpired(slots, inCombat: false, context.GameTime);

        int statMask = rec.EffectArgB;
        if (Character.ActorStatModifiers.ItemModifierIsBlocked(slots, statMask)) {
            return new ItemUseResult(ItemUseOutcome.NoEffect, PotionRefusedRecord, 0, false);
        }

        int slot = Character.ActorStatModifiers.SlotToFill(slots);
        if (slot < 0) {
            return new ItemUseResult(ItemUseOutcome.NoEffect, PotionRefusedRecord, 0, false);
        }

        slots[slot] = new Character.ActorStatModifiers.Slot(rec.EffectArgA, statMask,
            (short)rec.UseEffectAmount, context.GameTime,
            Character.ActorStatModifiers.ItemExpiryAt(context.GameTime, rec.EffectDurationHours));

        RuntimeItem source = container.Items[sourceIndex];
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
    private static ActorStat StatOf(ItemUseContext context, ActorAttribute attribute) =>
        context.Stats != null && context.Stats.Length > (int)attribute
            ? context.Stats[(int)attribute]
            : null;

    private static ActorStat HealthOf(ItemUseContext context) =>
        StatOf(context, ActorAttribute.Health);

    private static ActorStat StaminaOf(ItemUseContext context) =>
        StatOf(context, ActorAttribute.Stamina);

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

    /// <summary>
    /// Reading a note — <c>ITEMUSE.C</c>'s category-16 branch. See <see cref="NoteMapView"/> for why
    /// almost none of this category is about maps.
    /// </summary>
    /// <returns>
    /// Always <see cref="ItemUseOutcome.Silent"/>: the original's -2 neither spends the note nor
    /// prints a result, because the dialog it names has already said everything.
    /// <see cref="ItemUseResult.DialogVar0"/> carries the <b>map id</b>, which is what a caller needs
    /// in order to know whether to put a picture behind that dialog.
    /// </returns>
    /// <remarks>
    /// <b>A caller that wants the first-time preface must read the viewed flag BEFORE calling this</b>
    /// — the flag is written here, so asking afterwards always answers "already seen". That ordering
    /// is the one thing about this branch a caller can get wrong silently.
    /// </remarks>
    private static ItemUseResult UseNote(RuntimeItem source, ItemUseContext context) {
        if (!NoteMapView.ShowsAMap(source.ObjectId)) {
            return new ItemUseResult(ItemUseOutcome.Silent, NoteMapView.WrongNoteDialogId, 0, false);
        }

        int mapId = source.Variable;
        // Written whichever way the branch goes — a note whose map has no image still marks it seen.
        context.WriteFlag?.Invoke(NoteMapView.ViewedFlag(mapId), 1);

        return new ItemUseResult(ItemUseOutcome.Silent,
            NoteMapView.HasImage(mapId) ? NoteMapView.MapShownDialogId : NoteMapView.PrefaceDialogId,
            mapId, false);
    }

}
