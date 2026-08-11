namespace GameData.Resources.Character;

using GameData.Resources.Data;
using GameData.Resources.Object;
using System;
using System.Collections.Generic;

/// <summary>
/// How a caller wants an attribute change scaled before it is applied
/// (<c>stat_combatant_modify</c>'s <c>mode</c>, canassa <c>SRC/CHAR/STAT.C</c>).
/// </summary>
public enum StatChangeMode {
    /// <summary>Apply the delta as given.</summary>
    Absolute = 0,

    /// <summary>Scale by the current value: <c>delta * base / 100</c>.</summary>
    PercentOfCurrent = 1,

    /// <summary>Scale by the headroom left: <c>delta * (100 - base)</c>.</summary>
    PercentOfRemaining = 2,

    /// <summary>
    /// Skill-use advancement. The delta is multiplied by a per-skill rate that slides from
    /// <c>RatioBase</c> at value 0 to <c>RatioMax</c> at value 100 — so a skill gets harder to
    /// raise the better it already is. A delta of 0 means "one use", i.e. the rate itself.
    /// </summary>
    SkillUse = 3,
}

/// <summary>Which value a read wants back (<c>stat_actor_get</c>'s <c>mode</c>).</summary>
public enum StatReadMode {
    /// <summary>The fully modified, health-scaled value the game shows and rolls against.</summary>
    Effective = 0,

    /// <summary>The actor's ceiling for this attribute, unmodified.</summary>
    Maximum = 1,

    /// <summary>The stored value, unmodified and unscaled.</summary>
    Stored = 3,

    /// <summary>Modified but NOT health-scaled — the value before injury drags it down.</summary>
    Unscaled = 4,
}

/// <summary>
/// The attribute engine: the only place an actor's attribute value is computed or changed.
/// Faithful port of <c>stat_actor_get</c> (0x42fca) and <c>stat_combatant_modify</c> (0x431fc),
/// both marked VERIFIED in the function map, read from canassa <c>SRC/CHAR/STAT.C</c>.
///
/// <para><b>Skills only ever rise through here.</b> There is no separate advancement routine in the
/// original: using a skill calls this with <see cref="StatChangeMode.SkillUse"/>, the sub-unit
/// remainder banks in <see cref="ActorStat.Experience"/>, and whole points fall out when enough has
/// accumulated. That is also where the "skill improved" signal comes from, which the character
/// sheet renders as its advancement marks.</para>
///
/// <para><b>Table provenance.</b> The seven tables below were read out of KRONDOR.EXE
/// (0x3a600 StatMin, 0x3a611 StatMax, 0x3a633 ClampMin, 0x3a644 ClampMax, 0x3a655 Ratio,
/// 0x3a666 RatioBase, 0x3a677 RatioMax), not copied from canassa — canassa carries two variants
/// behind <c>#ifdef V102CD</c> and the bytes in our binary match the <b>1.02 CD</b> fork
/// (RatioBase[9..15] = 80 80 80 20 40 80 40). Re-check these if the project ever targets the 1.00
/// floppy build, because the skill-advancement rates differ.</para>
///
/// <para>The pieces that need party-wide state — the eight timed stat modifiers, the seven
/// condition ranks, the "selected skill" study bonus — arrive as explicit arguments rather than
/// being reached for here, so this stays pure and the systems that own that state can fill them in
/// as they land.</para>
/// </summary>
public static class StatEngine {
    /// <summary>Number of rows in the engine's tables: the 16 stored attributes plus the combo.</summary>
    public const int TableSize = 17;

    /// <summary>The pseudo-attribute index that addresses Health and Stamina as one pool.</summary>
    public const int ComboIndex = (int)ActorAttribute.HealthStaminaCombo;

    // Floor a READ may return, per attribute (g_abStatMin @ 0x3a600).
    private static readonly byte[] ReadMin =
        { 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

    // Ceiling a READ may return (g_awStatMax @ 0x3a611). Words, not bytes — reads are not capped
    // at 255 even though the stored byte is.
    private static readonly ushort[] ReadMax =
        { 500, 500, 500, 500, 200, 200, 200, 200, 200, 200, 200, 200, 200, 100, 200, 200, 1000 };

    // Floor/ceiling the STORED value is clamped to after a change
    // (g_abStatClampMin @ 0x3a633, g_abStatClampMax @ 0x3a644).
    private static readonly byte[] StoredMin =
        { 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

    private static readonly byte[] StoredMax =
        { 250, 250, 250, 250, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 244 };

    // How strongly a read is dragged down by the actor's health (g_abStatRatio @ 0x3a655).
    // 0 = not at all (Health, Stamina, combo), 1 = fully, 2 = half-weighted.
    private static readonly byte[] HealthRatio =
        { 0, 0, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 0 };

    // Skill-use advancement rate at value 0 and at value 100 (g_abStatRatioBase @ 0x3a666,
    // g_abStatRatioMax @ 0x3a677). 1.02 CD values — see the type remarks.
    private static readonly byte[] SkillRateAtZero =
        { 0x33, 0x33, 0x08, 0x08, 0x08, 0x33, 0x08, 0x33, 0x00, 0x80, 0x80, 0x80, 0x20, 0x40, 0x80, 0x40, 0x00 };

    private static readonly byte[] SkillRateAtHundred =
        { 0x03, 0x03, 0x01, 0x01, 0x02, 0x03, 0x01, 0x03, 0x10, 0x05, 0x05, 0x10, 0x04, 0x04, 0x10, 0x01, 0x00 };

    /// <summary>Outcome of a change, for the caller that owns the party-wide bookkeeping.</summary>
    public readonly struct StatChange {
        public StatChange(int value, bool changed, bool increased, bool signalsImprovement) {
            Value = value;
            Changed = changed;
            Increased = increased;
            SignalsImprovement = signalsImprovement;
        }

        /// <summary>The stored value after the change.</summary>
        public int Value { get; }

        /// <summary>Did the stored value move at all?</summary>
        public bool Changed { get; }

        /// <summary>Did it move up? (The party "something improved" flag keys off this.)</summary>
        public bool Increased { get; }

        /// <summary>
        /// Should the actor's SKILL_IMPROVED event be raised? True for any change to a skill, but
        /// only for an <i>increase</i> to Health or Stamina — losing health is not an improvement
        /// worth telling the player about.
        /// </summary>
        public bool SignalsImprovement { get; }
    }

    /// <summary>
    /// Change an attribute. Mirrors <c>stat_combatant_modify</c> for every non-combo attribute; use
    /// <see cref="ModifyHealthPool"/> for <see cref="ActorAttribute.HealthStaminaCombo"/>.
    ///
    /// <para><paramref name="studyBonusPer52"/> is the "selected skill" bonus: when the player has
    /// marked this skill for study the original adds <c>delta * tickAdvance / 52</c> before
    /// applying it. Pass 0 (the default) when nothing is marked.</para>
    /// </summary>
    public static StatChange Modify(ActorStat stat, ActorAttribute attribute, long delta,
        StatChangeMode mode = StatChangeMode.Absolute, int studyBonusPer52 = 0) {
        if (stat == null) {
            throw new ArgumentNullException(nameof(stat));
        }
        int index = (int)attribute;
        if (index < 0 || index >= TableSize) {
            throw new ArgumentOutOfRangeException(nameof(attribute));
        }
        int originalBase = stat.Base;

        // A stat the actor does not have is inert — no change, no experience banked.
        if (stat.Max == 0) {
            return new StatChange(0, false, false, false);
        }

        delta = ScaleDelta(delta, index, stat.Base, mode);

        if (studyBonusPer52 != 0) {
            delta += delta * studyBonusPer52 / 0x34;
        }

        // Bank the sub-unit remainder. This is the whole reason repeated small skill uses add up:
        // whatever does not make a full point stays in Experience for next time.
        delta += stat.Experience;
        stat.Experience = unchecked((byte)(delta % 0x100));
        long whole = delta / 0x100;

        if (whole < 0 && stat.Base < Math.Abs(whole)) {
            // The decrement is bigger than what is there: pin to zero rather than wrapping.
            stat.Base = 0;
        } else {
            // Faithful truncation: the original adds the quotient as a SIGNED BYTE, so a change of
            // more than 127 whole points wraps exactly as it did in 1993.
            stat.Base = unchecked((byte)(stat.Base + unchecked((sbyte)whole)));
        }

        if (stat.Base < StoredMin[index]) {
            stat.Base = StoredMin[index];
        }
        if (stat.Base > StoredMax[index]) {
            stat.Base = StoredMax[index];
        }
        if (stat.Max < stat.Base) {
            stat.Max = stat.Base;
        }

        bool changed = stat.Base != originalBase;
        bool increased = stat.Base > originalBase;
        bool signals = changed && (index > 1 || increased);
        return new StatChange(stat.Base, changed, increased, signals);
    }

    private static long ScaleDelta(long delta, int index, byte current, StatChangeMode mode) {
        switch (mode) {
            case StatChangeMode.PercentOfCurrent:
                return current * delta / 100;
            case StatChangeMode.PercentOfRemaining:
                return (100 - current) * delta;
            case StatChangeMode.SkillUse: {
                long rate = SkillRateAtZero[index]
                    + (SkillRateAtHundred[index] - (long)SkillRateAtZero[index]) * current / 100;
                if (rate <= 0) {
                    return 0;
                }
                return delta == 0 ? rate : delta * rate;
            }
            default:
                return delta;
        }
    }

    /// <summary>
    /// Change Health and Stamina as a single pool — <c>stat_combatant_modify</c>'s
    /// <c>stat_idx == 0x10</c> branch, which is how damage and healing actually land.
    ///
    /// <para>The two are summed, moved by <c>delta / 256</c>, and split back with <b>Health filled
    /// first</b>: anything above Health's maximum spills into Stamina. Healing (a positive delta)
    /// stops at <paramref name="healTargetPercent"/> of the combined maximum and does nothing at all
    /// if the pool is already at or above it. Draining to zero or below sets
    /// <paramref name="collapsed"/> so the caller can apply Near-death.</para>
    ///
    /// <para><paramref name="nearDeathRank"/>, when non-zero, overrides the heal target entirely
    /// with <c>((100 - rank) * 30) / 100 + 1</c> — a near-dead actor cannot be healed past a sliver,
    /// and the worse the rank the smaller that sliver.</para>
    /// </summary>
    public static int ModifyHealthPool(ActorStat health, ActorStat stamina, long delta,
        int healTargetPercent, out bool collapsed, int nearDeathRank = 0) {
        if (health == null) {
            throw new ArgumentNullException(nameof(health));
        }
        if (stamina == null) {
            throw new ArgumentNullException(nameof(stamina));
        }
        collapsed = false;

        int sum = stamina.Base + health.Base;
        int maxSum = stamina.Max + health.Max;
        int target = healTargetPercent * maxSum / 100;
        if (nearDeathRank != 0) {
            target = (100 - nearDeathRank) * 0x1e / 100 + 1;
        }

        if (delta > 0) {
            if (sum < target) {
                sum += (int)(delta / 0x100);
                if (sum > target) {
                    sum = target;
                }
            }
        } else {
            sum += (int)(delta / 0x100);
            if (sum <= 0) {
                sum = 0;
                collapsed = true;
            }
        }

        if (health.Max < sum) {
            stamina.Base = unchecked((byte)(sum - health.Max));
            health.Base = health.Max;
        } else {
            stamina.Base = 0;
            health.Base = unchecked((byte)sum);
        }
        return sum;
    }

    /// <summary>
    /// Read an attribute the way the game reads it — <c>stat_actor_get</c>.
    ///
    /// <para>Order matters and is the original's: stored value, plus the equipment modifier, then
    /// (for a party member) the timed modifiers and condition multipliers supplied by
    /// <paramref name="applyPartyEffects"/>, then the health scaling, then the read clamps.</para>
    ///
    /// <para><b>Health scaling is the interesting part.</b> Every skill is dragged down by how hurt
    /// the actor is: the value is scaled by current-health/max-health, at full weight for the
    /// combat attributes and half weight for the craft skills. So a badly wounded character is
    /// worse at everything, without anything having written to their skills. Pass
    /// <see cref="StatReadMode.Unscaled"/> to see the value before that drag.</para>
    /// </summary>
    /// <param name="healthOfActor">The actor's Health slot, needed for the scaling above. May be
    /// the same instance as <paramref name="stat"/> when reading Health itself.</param>
    /// <param name="applyPartyEffects">Hook for the timed stat modifiers and condition penalties.
    /// Null (the default) reads as "no active effects". Afflictions plug in here as
    /// <c>v => ConditionEngine.ApplyAttributePenalties(v, attribute, conditions)</c>; the eight
    /// timed modifiers still have no owner.</param>
    public static int Get(ActorStat stat, ActorAttribute attribute, ActorStat healthOfActor,
        StatReadMode mode = StatReadMode.Effective, Func<int, int> applyPartyEffects = null) {
        if (stat == null) {
            throw new ArgumentNullException(nameof(stat));
        }
        int index = (int)attribute;
        if (index < 0 || index >= TableSize) {
            throw new ArgumentOutOfRangeException(nameof(attribute));
        }
        if (mode == StatReadMode.Stored) {
            return stat.Base;
        }
        if (mode == StatReadMode.Maximum) {
            return stat.Max;
        }

        int value = stat.Base;
        stat.Effective = stat.Base;

        if (stat.Modifier != 0) {
            value += stat.Modifier;
            if (value < 0) {
                value = 0;
            }
            stat.Effective = unchecked((byte)value);
        }

        if (applyPartyEffects != null) {
            value = applyPartyEffects(value);
        }

        if (mode != StatReadMode.Unscaled) {
            value = ApplyHealthScaling(value, index, healthOfActor);
        }

        if (stat.Max == 0) {
            value = 0;
        }
        if (value < ReadMin[index]) {
            value = ReadMin[index];
        }
        if (value > ReadMax[index]) {
            value = ReadMax[index];
        }
        stat.Effective = (byte)(value > 0xfa ? 0xfa : value);
        return value;
    }

    private static int ApplyHealthScaling(int value, int index, ActorStat health) {
        int ratio = HealthRatio[index];
        if (ratio == 0 || health == null) {
            return value;
        }
        int current = health.Base;
        int max = health.Max;
        if (ratio > 1) {
            // Half-weighted: average current health with full health, so the craft skills lose
            // only half as much as the combat ones.
            current = (current + max * (ratio - 1)) / ratio;
        }
        if (max == 0) {
            return 0;
        }
        return (max + value * current - 1) / max;
    }

    /// <summary>
    /// Recompute every attribute's equipment modifier from what the actor is carrying —
    /// <c>stat_actor_recalc_equip_bonuses</c> (<c>ApplyAllModifiersFromItemsInInventory</c>,
    /// 0x42f02, VERIFIED). Call it whenever the actor's inventory changes; it clears all sixteen
    /// modifiers and rebuilds them from scratch, so it is never double-counted.
    ///
    /// <para><b>Carried, not worn.</b> Despite the "equip" in every name for this, the original
    /// walks the actor's whole inventory — an item with a modifier bonuses the actor for being in
    /// the pack at all. Only six objects in the game have one: Staff of Macros, Amulet of the
    /// Upright Man, Idol of Lassur (which is a -20 penalty across twelve attributes), Practice
    /// Lute, Ring of the Golden Way and Weedwalkers.</para>
    ///
    /// <para><b>The Weedwalkers rule.</b> The 1.02 CD build we target counts object 0x5a
    /// (Weedwalkers, +30 Stealth) at most once, however many pairs are in the pack — a stacking
    /// exploit patched in that release. The 1.00 floppy has no such check. No other object is
    /// special-cased.</para>
    /// </summary>
    /// <param name="stats">The actor's attribute slots, as built by <see cref="FromSaved"/>.</param>
    /// <param name="carried">Every object in the actor's inventory, by id.</param>
    /// <param name="lookup">Object id → its record; pass <c>objectInfoSet.GetById</c>.</param>
    public static void RecalculateItemModifiers(ActorStat[] stats, IEnumerable<int> carried,
        Func<int, ObjectInfo> lookup) {
        if (stats == null) {
            throw new ArgumentNullException(nameof(stats));
        }
        if (lookup == null) {
            throw new ArgumentNullException(nameof(lookup));
        }
        for (int i = 0; i < stats.Length; i++) {
            if (stats[i] != null) {
                stats[i].Modifier = 0;
            }
        }
        if (carried == null) {
            return;
        }

        bool weedwalkersCounted = false;
        foreach (int objectId in carried) {
            if (objectId == WeedwalkersObjectId) {
                if (weedwalkersCounted) {
                    continue;
                }
                weedwalkersCounted = true;
            }

            ObjectInfo record = lookup(objectId);
            if (record == null || record.EquipAttributeMask == 0) {
                continue;
            }

            // The original truncates the record's amount to a signed byte before adding it.
            sbyte amount = unchecked((sbyte)record.EquipModifierAmount);
            int mask = (int)record.EquipAttributeMask;
            for (int i = 0; i < stats.Length; i++) {
                if ((mask & (1 << i)) != 0 && stats[i] != null) {
                    stats[i].Modifier = unchecked((sbyte)(stats[i].Modifier + amount));
                }
            }
        }
    }

    /// <summary>Weedwalkers — the one object the CD build refuses to count twice.</summary>
    private const int WeedwalkersObjectId = 0x5a;

    /// <summary>
    /// Hydrate the 16 stored attributes of a saved actor into mutable slots, in the executable's
    /// index order (reusing <see cref="ActorAttributeValues.At"/>, which owns that order). Index 16
    /// has no slot — the combo is derived, never stored.
    /// </summary>
    public static ActorStat[] FromSaved(SaveGameActorData actor) {
        var stats = new ActorStat[ActorAttributeValues.Count];
        for (int i = 0; i < stats.Length; i++) {
            SaveGameAttributeValuesData saved = ActorAttributeValues.At(actor, i);
            stats[i] = saved == null ? new ActorStat() : new ActorStat(saved);
        }
        return stats;
    }
}
