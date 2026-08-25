namespace GameData.Resources.Character;

using System;
using System.Collections.Generic;

/// <summary>
/// The party's eight timed stat modifiers per member — <c>g_gameState.aActorStatModifiers</c>
/// (gstate.inc:41, 672 bytes) and <c>stat_apply_modifier</c> (canassa CHAR/STAT.C:33).
///
/// <para>The hook <see cref="StatEngine.Get"/> has always taken and nothing has ever filled: its
/// doc says "the eight timed modifiers still have no owner". This is them.</para>
/// </summary>
/// <remarks>
/// <b>The size cross-checks exactly.</b> Six characters x eight slots x fourteen bytes is 672, the
/// block's declared length — and fourteen is what the reader's <c>modPtr += 7</c> on a
/// <c>unsigned short*</c> steps. Two derivations meeting is what makes the shape trustworthy rather
/// than plausible.
/// </remarks>
public static class ActorStatModifiers {
    /// <summary>Characters the block covers.</summary>
    public const int Characters = 6;

    /// <summary>Modifier slots per character.</summary>
    public const int SlotsPerCharacter = 8;

    /// <summary>Bytes per slot — seven 16-bit words.</summary>
    public const int SlotSize = 14;

    /// <summary>Size of the whole block, and the figure gstate.inc declares.</summary>
    public const int BlockSize = Characters * SlotsPerCharacter * SlotSize;

    /// <summary>Flags in a slot's first word. The LOW BYTE is not a flag — see <see cref="CostOf"/>.</summary>
    [Flags]
    public enum ModifierFlags {
        /// <summary>An empty slot: the whole word is zero.</summary>
        None = 0,

        /// <summary>
        /// <b>Applies only in combat — and outside combat it is skipped ENTIRELY.</b>
        /// </summary>
        /// <remarks>
        /// The original's guard is <c>if ((*mod &amp; 0x100) == 0 || inCombat)</c>, and the expiry
        /// check lives INSIDE it. So a combat-only modifier does not merely stop applying out of
        /// combat — <b>it never expires there either</b>, and comes back at whatever strength it had
        /// however long the party has been walking around. A port that tested the expiry first
        /// would quietly retire buffs the game keeps.
        /// </remarks>
        CombatOnly = 0x100,

        /// <summary>The slot carries an expiry time in <see cref="Slot.ExpiresAt"/>.</summary>
        /// <remarks>
        /// Without this bit the modifier is permanent until something clears it. With it, the
        /// expiry is checked <b>lazily, on read</b> — nothing sweeps the table on a timer, so a
        /// modifier is freed the next time that character's stat is looked at.
        /// </remarks>
        Expires = 0x200,

        /// <summary>
        /// Present in the original's switch with an <b>empty body</b>.
        /// </summary>
        /// <remarks>
        /// <c>if ((*mod &amp; 0x400) == 0) { }</c> — the branch does nothing in the reconstruction.
        /// Modelled as a named bit with its meaning UNESTABLISHED rather than dropped, so a slot
        /// carrying it is not silently reinterpreted as something else.
        /// </remarks>
        Unestablished400 = 0x400,

        /// <summary>The value is a PERCENTAGE delta, not an absolute one.</summary>
        Percentage = 0x800,
    }

    /// <summary>
    /// The slot's eviction cost — <b>the low byte of the flags word, not a flag</b>.
    /// </summary>
    /// <remarks>
    /// <c>stat_actor_add_mod</c> takes the first EMPTY slot, and if all eight are full overwrites
    /// the one with the <b>lowest</b> cost. So a full table silently drops the weakest modifier
    /// rather than refusing the new one — see <see cref="SlotToFill"/>.
    /// </remarks>
    public static int CostOf(int flagsWord) => flagsWord & 0xff;

    /// <summary>One modifier slot.</summary>
    public readonly struct Slot {
        public Slot(int flags, int statMask, short value, uint appliedAt, uint expiresAt) {
            Flags = flags;
            StatMask = statMask;
            Value = value;
            AppliedAt = appliedAt;
            ExpiresAt = expiresAt;
        }

        /// <summary>Word 0 — <see cref="ModifierFlags"/> in the high byte, cost in the low.</summary>
        public int Flags { get; }

        /// <summary>Word 1 — which attributes this affects, as <c>1 &lt;&lt; attribute</c>.</summary>
        public int StatMask { get; }

        /// <summary>Word 2 — the delta, absolute or percentage per <see cref="ModifierFlags.Percentage"/>.</summary>
        public short Value { get; }

        /// <summary>Words 3-4 — when it was applied.</summary>
        public uint AppliedAt { get; }

        /// <summary>Words 5-6 — when it lapses, if <see cref="ModifierFlags.Expires"/> is set.</summary>
        public uint ExpiresAt { get; }

        /// <summary>Nothing is in this slot.</summary>
        public bool IsEmpty => Flags == 0;

        internal bool Has(ModifierFlags flag) => (Flags & (int)flag) != 0;
    }

    /// <summary>Whether a slot affects the given attribute.</summary>
    /// <remarks>
    /// <b>The reader tests the mask BEFORE applying</b>, so a live slot for another attribute costs
    /// nothing and — importantly — <b>does not get its expiry checked either</b>. Expiry is a side
    /// effect of being read for a matching stat.
    /// </remarks>
    public static bool Affects(in Slot slot, ActorAttribute attribute) =>
        !slot.IsEmpty && (slot.StatMask & (1 << (int)attribute)) != 0;

    /// <summary>
    /// Applies one slot to a running value, and says whether the slot should now be freed.
    /// </summary>
    /// <param name="slot">The slot.</param>
    /// <param name="value">The value so far.</param>
    /// <param name="inCombat">Whether a fight is running.</param>
    /// <param name="gameTime">The current game time, for the expiry test.</param>
    /// <param name="expired">True when the caller should zero this slot.</param>
    /// <remarks>
    /// <b>The order is the original's and it matters twice.</b> The combat gate comes first and the
    /// expiry sits inside it (see <see cref="ModifierFlags.CombatOnly"/>); and the expiry check runs
    /// BEFORE the value is applied, so a modifier that lapsed on this very read contributes nothing
    /// rather than getting one last application.
    ///
    /// <para>Percentage is <c>value * (delta + 100) / 100</c> in integer arithmetic — truncating,
    /// like everything else in this engine.</para>
    /// </remarks>
    public static int Apply(in Slot slot, int value, bool inCombat, uint gameTime, out bool expired) {
        expired = false;
        if (slot.IsEmpty) {
            return value;
        }
        if (slot.Has(ModifierFlags.CombatOnly) && !inCombat) {
            // Skipped entirely — not applied, and NOT expired.
            return value;
        }
        if (slot.Has(ModifierFlags.Expires) && slot.ExpiresAt < gameTime) {
            expired = true;
            return value;
        }
        return slot.Has(ModifierFlags.Percentage)
            ? value * (slot.Value + 100) / 100
            : value + slot.Value;
    }

    /// <summary>
    /// Which slot a new modifier goes in — <c>stat_actor_add_mod</c> (STAT.C:408).
    /// </summary>
    /// <returns>The index to write.</returns>
    /// <remarks>
    /// <b>The first EMPTY slot wins outright</b>, scanning forward; only when all eight are full
    /// does cost decide, and then the CHEAPEST is overwritten. <b>A full table therefore never
    /// refuses a modifier — it drops one</b>, which is what makes stacking buffs on one character
    /// quietly lossy rather than an error.
    ///
    /// <para>Ties go to the earliest slot, because the scan uses a strict <c>&lt;</c>.</para>
    /// </remarks>
    public static int SlotToFill(IReadOnlyList<Slot> slots) {
        if (slots == null || slots.Count == 0) {
            return -1;
        }
        var best = 0;
        int bestCost = int.MaxValue;
        for (var i = 0; i < slots.Count && i < SlotsPerCharacter; i++) {
            if (slots[i].IsEmpty) {
                return i;
            }
            int cost = CostOf(slots[i].Flags);
            if (cost < bestCost) {
                bestCost = cost;
                best = i;
            }
        }
        return best;
    }

    /// <summary>
    /// Whether <c>stat_actor_clear_mods_mask</c> clears this slot.
    /// </summary>
    /// <remarks>
    /// <b>The test is on the FLAGS word, not the stat mask.</b> The routine ANDs the caller's mask
    /// against <c>wMaskFlags</c> — the word carrying the flags and the cost — so what it selects on
    /// is the modifier's KIND, not which attribute it touches. Reading it as a stat mask clears the
    /// wrong modifiers, and plausibly: both words are masks and they sit next to each other.
    /// </remarks>
    public static bool ClearedBy(in Slot slot, int mask) => (slot.Flags & mask) != 0;
}
