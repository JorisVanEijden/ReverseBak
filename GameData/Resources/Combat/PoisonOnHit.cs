namespace GameData.Resources.Combat;

using System;

/// <summary>
/// A poisoned blow poisoning its target — <c>cbstat_apply_drain_tick</c> (CBSTAT.C:163), which
/// <c>combat_arena_apply_damage</c> calls whenever a landed blow's damage mask carries bit 1.
/// </summary>
/// <remarks>
/// <b>This is what a poisoned weapon is FOR.</b> Its +10 damage is the small half; the point is the
/// condition it leaves behind, which <see cref="PoisonTick"/> then bills every turn. The port set
/// <see cref="CombatantFlags.Poisoned"/> nowhere, so the tick had nothing to tick and a poisoned
/// blade was just a slightly harder one.
///
/// <para>Three gates, all the original's:</para>
/// <list type="number">
///   <item>the blow's mask carries <see cref="PoisonBit"/> — <c>CombatDamageMask.ForSwing</c> sets
///     it from the sword slot's 0x80;</item>
///   <item>the target's class does not RESIST poison —
///     <c>g_aClassWeaknessMask[creatureType] &amp; 1</c>, which is IDA's
///     <c>creatureResistanceFlags</c> and our <see cref="CreatureAffinity.ResistanceFlags"/>;</item>
///   <item>the target's worn armour does not protect against poison — the original passes a damage
///     of 1 and a type of 0x80 through <c>cbstat_damage_apply_protection</c> and asks whether
///     anything survived, so this reuses the very same function.</item>
/// </list>
/// </remarks>
public static class PoisonOnHit {
    /// <summary>Bit 1 of a damage mask: this blow is poisonous.</summary>
    public const int PoisonBit = 1;

    /// <summary>Lowest rank a fresh poisoning lands at — <c>RNDR(10, 59)</c>.</summary>
    public const int MinRank = 10;

    /// <summary>Highest, inclusive: <c>RNDR(lo, hi)</c> is <c>lo + rand() % (hi - lo + 1)</c>.</summary>
    public const int MaxRank = 59;

    /// <summary>Whether this landed blow poisons its target.</summary>
    /// <param name="damage">Damage actually dealt; the original gates on <c>damage != 0</c>.</param>
    /// <param name="damageMask">The blow's damage-type mask (<see cref="CombatDamageMask"/>).</param>
    /// <param name="targetResistanceFlags">The target class's
    /// <see cref="CreatureAffinity.ResistanceFlags"/>, or 0 when it has no row.</param>
    /// <param name="targetArmorFlags">Flags of the target's equipped armour.</param>
    public static bool Applies(int damage, int damageMask, int targetResistanceFlags,
        ItemFlags targetArmorFlags) =>
        damage != 0
        && (damageMask & PoisonBit) != 0
        && (targetResistanceFlags & PoisonBit) == 0
        && CombatFormulas.EnchantmentAfterProtection(1, (int)ItemFlags.Poisoned, targetArmorFlags) != 0;

    /// <summary>The rank a fresh poisoning adds: 10..59 inclusive.</summary>
    /// <param name="rnd">Returns a value in <c>[0, n)</c>.</param>
    public static int Rank(Func<int, int> rnd) {
        if (rnd == null) {
            throw new ArgumentNullException(nameof(rnd));
        }
        return MinRank + rnd(MaxRank - MinRank + 1);
    }
}
