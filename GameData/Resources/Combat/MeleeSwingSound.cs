namespace GameData.Resources.Combat;

using System.Collections.Generic;

/// <summary>
/// Which cue a melee swing plays — <c>combat_arena_resolve_melee_swing</c> (canassa COMBAT.C:596).
/// </summary>
/// <remarks>
/// <b>The cues encode the weapon's MATERIAL, not the attack.</b> The branch everything falls through
/// to asks whether the combatant holds an intact <see cref="ObjectType.Staff"/> — category 3 — and
/// picks wood or metal from it. The parry clang then asks it of BOTH sides and has a third cue for
/// the mixed case. Read as "does this creature have a special sound" the table looks arbitrary; read
/// as wood-on-wood, metal-on-metal and one-of-each it is obvious.
///
/// <para><b>Ours is the 1.02 CD build</b>, so the <c>#ifdef V102CD</c> arm is part of the rule and
/// not an alternative: four creature classes that the floppy sends to the default branch have their
/// own cue here.</para>
/// </remarks>
public static class MeleeSwingSound {
    /// <summary>Heavy/creature impact — the larger monsters.</summary>
    public const int CreatureHeavy = 0x4a;

    /// <summary>The other creature impact.</summary>
    public const int CreatureLight = 0x1a;

    /// <summary>A landed blow from something holding no staff — metal.</summary>
    public const int HitWithoutStaff = 0x41;

    /// <summary>A landed blow from something holding a staff — wood.</summary>
    public const int HitWithStaff = 0x42;

    /// <summary>A miss that nothing parried.</summary>
    public const int Miss = 0x13;

    /// <summary>Parried, both sides holding a staff — wood on wood.</summary>
    public const int ParryBothStaves = 0x43;

    /// <summary>Parried, neither side holding a staff — metal on metal.</summary>
    public const int ParryNeitherStaff = 0x07;

    /// <summary>Parried, one of each.</summary>
    public const int ParryMixed = 0x42;

    /// <summary>
    /// Creature class whose parry always clangs as wood, whatever it is holding.
    /// </summary>
    /// <remarks>The original tests it as a special case beside the both-staves test.</remarks>
    public const int AlwaysWoodParryClass = 0x2c;

    private static readonly HashSet<int> HeavyClasses =
        new HashSet<int> { 0x13, 0x1c, 0x29, 0x2a, 0x2b, 0x2e, 0x30 };

    private static readonly HashSet<int> LightClasses =
        new HashSet<int> { 0x27, 0x2c, 0x31, 0x3a };

    /// <summary>Classes the CD build gives the staff cue to regardless of what they hold.</summary>
    private static readonly HashSet<int> CdStaffClasses =
        new HashSet<int> { 0x1d, 0x1f, 0x20, 0x21 };

    /// <summary>The cue a landed blow plays.</summary>
    /// <param name="attackerCreatureType">The attacker's class.</param>
    /// <param name="attackerHasStaff">Whether it holds an intact staff (equipment category 3).</param>
    public static int Hit(int attackerCreatureType, bool attackerHasStaff) {
        if (HeavyClasses.Contains(attackerCreatureType)) {
            return CreatureHeavy;
        }
        if (LightClasses.Contains(attackerCreatureType)) {
            return CreatureLight;
        }
        if (CdStaffClasses.Contains(attackerCreatureType)) {
            return HitWithStaff;
        }
        return attackerHasStaff ? HitWithStaff : HitWithoutStaff;
    }

    /// <summary>
    /// The cue a miss plays.
    /// </summary>
    /// <param name="defenderParried">
    /// Whether the defender both holds a guard AND can act — the original requires both, so a
    /// downed combatant's raised guard makes no sound.
    /// </param>
    /// <param name="defenderCreatureType">The defender's class.</param>
    /// <param name="defenderHasStaff">Whether the defender holds an intact staff.</param>
    /// <param name="attackerHasStaff">Whether the attacker does.</param>
    public static int MissCue(bool defenderParried, int defenderCreatureType,
        bool defenderHasStaff, bool attackerHasStaff) {
        if (!defenderParried) {
            return Miss;
        }
        if (defenderHasStaff && attackerHasStaff) {
            return ParryBothStaves;
        }
        if (defenderCreatureType == AlwaysWoodParryClass) {
            return ParryBothStaves;
        }
        if (!defenderHasStaff && !attackerHasStaff) {
            return ParryNeitherStaff;
        }
        return ParryMixed;
    }
}
