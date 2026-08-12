namespace GameData.Resources.Inventory;

using GameData.Resources.Character;
using GameData.Resources.Object;
using System;

/// <summary>
/// The permanent stat gain an item confers when a character uses it — what reading a book does.
///
/// <para>Ported from <c>itemuse_apply_stat_effects</c> (<c>SRC/SCREENS/ITEMUSE.C</c>), reached from
/// the item-use dispatch for category 17 (<see cref="ObjectType.Book"/>).</para>
///
/// <para><b>The first read is the real one.</b> The first time a given character uses a given item
/// type the bonus applies in full, permanently, with no roll. Every use after that is gated on a
/// percentage chance and applies a different magnitude in a different mode. So a book is worth
/// reading once per character and only speculatively thereafter — which is the mechanic, not an
/// optimisation to smooth away.</para>
/// </summary>
public static class ItemStatEffects {
    /// <summary>Base of the per-(character, item) "has read this" save flags — the original's
    /// <c>ITEM_USED(idx) = idx + 6476</c>.</summary>
    public const int UsedFlagBase = 6476;

    /// <summary>Flag-key stride between party slots. See the aliasing note on <see cref="UsedFlagKey"/>.</summary>
    public const int SlotStride = 0x14;

    /// <summary>Attributes the effect mask can address.</summary>
    private const int AttributeBits = 0x10;

    /// <summary>
    /// Save-state key of the "this character has already used this item type" flag.
    /// </summary>
    /// <remarks>
    /// <b>This key space aliases, and the original is aliasing too.</b> The index is
    /// <c>(slot-1) * 20 + objectId</c>, but object ids run past 20 — so character 2 reading object 0
    /// lands on the same flag as character 1 reading object 20. With 138 object ids and a stride of
    /// 20 the overlap is wholesale, and the practical effect is that one character's first read can
    /// consume another character's first-read bonus for an unrelated item.
    /// <para>Reproduced rather than fixed: widening the stride would silently change which reads
    /// pay out, and existing saves carry flags in the original's layout. If it is ever "corrected",
    /// it has to be a deliberate, migrated change.</para>
    /// </remarks>
    public static int UsedFlagKey(int partySlot, int objectId) =>
        UsedFlagBase + ((partySlot - 1) * SlotStride) + objectId;

    /// <summary>
    /// Applies an item's stat effect to a character.
    /// </summary>
    /// <param name="stats">The character's live attributes, indexed by <see cref="ActorAttribute"/>.</param>
    /// <param name="partySlot">1-based party slot. Anything below 1 is refused, as in the original.</param>
    /// <param name="item">The item being used; its <c>Variable</c> is the condition/charges, and an
    /// exhausted item does nothing.</param>
    /// <param name="record">
    /// The object record. Three of its fields are read here, and <b>two of them mean something
    /// different in this category than their names suggest</b>:
    /// <see cref="ObjectInfo.EffectArgA"/> is a bitmask of which attributes to raise,
    /// <see cref="ObjectInfo.EffectArgB"/> is the first-read amount, and then
    /// <see cref="ObjectInfo.UseEffectAmount"/> (+0x42) is the <i>chance percentage</i> for later
    /// reads while <see cref="ObjectInfo.EffectDurationHours"/> (+0x44) is their <i>magnitude</i> —
    /// canassa's <c>wEffect_chance_pct</c> and <c>wEffect_stat_value</c>. Both fields are
    /// polymorphic across categories; the duration reading is right for timed effects and wrong
    /// here.
    /// </param>
    /// <param name="readFlag">Reads a save-state flag.</param>
    /// <param name="writeFlag">Writes a save-state flag.</param>
    /// <param name="rnd">Returns a value in <c>[0, n)</c>.</param>
    /// <returns>True when at least one attribute was raised.</returns>
    public static bool Apply(
        ActorStat[] stats, int partySlot, RuntimeItem item, ObjectInfo record,
        Func<int, int> readFlag, Action<int, int> writeFlag, Func<int, int> rnd) {
        if (stats == null) {
            throw new ArgumentNullException(nameof(stats));
        }
        if (item == null) {
            throw new ArgumentNullException(nameof(item));
        }
        if (record == null) {
            throw new ArgumentNullException(nameof(record));
        }
        if (readFlag == null || writeFlag == null || rnd == null) {
            throw new ArgumentNullException(nameof(readFlag));
        }

        // The original computes the flag key before these guards and then returns anyway; the guards
        // are what matter. A slot of 0 means "not a party member".
        if (partySlot < 1 || record.EffectArgA == 0 || item.Variable == 0) {
            return false;
        }

        int key = UsedFlagKey(partySlot, item.ObjectId);

        if (readFlag(key) == 0) {
            // First read: full value, no roll, and the flag is set so it never pays out twice.
            writeFlag(key, 1);
            ApplyToMaskedAttributes(stats, record.EffectArgA,
                (long)record.EffectArgB << 8, StatChangeMode.Absolute);
            return true;
        }

        // Every later read: gated, smaller, and applied as a share of the headroom left rather than
        // a flat amount — so it tapers as the attribute approaches its maximum.
        if (rnd(100) >= record.UseEffectAmount) {
            return false;
        }
        ApplyToMaskedAttributes(stats, record.EffectArgA,
            record.EffectDurationHours, StatChangeMode.PercentOfRemaining);
        return true;
    }

    private static void ApplyToMaskedAttributes(ActorStat[] stats, int mask, long delta, StatChangeMode mode) {
        for (var i = 0; i < AttributeBits; i++) {
            if ((mask & (1 << i)) == 0) {
                continue;
            }
            if (i < stats.Length && stats[i] != null) {
                StatEngine.Modify(stats[i], (ActorAttribute)i, delta, mode);
            }
        }
    }
}
