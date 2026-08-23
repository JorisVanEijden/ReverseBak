namespace GameData.Resources.Combat;

/// <summary>
/// <c>combataiact_ranged_attack</c> (canassa CBTAIACT.C:235) — seventh of the nine AI action
/// routines, and the hardest-hitting one in the file.
///
/// <para>Distinct from <see cref="MonsterRangedTurn"/>, which is
/// <c>combataiact_ranged_attack_TURN</c>: canassa gives two routines near-identical names for very
/// different attacks.</para>
/// </summary>
public static class MonsterHeavyRangedTurn {
    /// <summary>
    /// <b>The creature restores its Strength to full at the START OF EVERY TURN.</b>
    /// </summary>
    /// <remarks>
    /// The routine opens with <c>actor->stats[3].base = actor->stats[3].max</c> — stat 3 is Strength
    /// — before it even looks for a target. So draining this creature's Strength is pointless: it
    /// undoes the damage itself each turn, unconditionally, whether or not it goes on to attack.
    ///
    /// <para>It is the first statement of the routine, easy to skim past as bookkeeping, and a port
    /// that drops it makes the creature meaningfully weaker over a long fight.</para>
    /// </remarks>
    public static bool RestoresStrengthEachTurn => true;

    /// <summary>The stat restored — Strength.</summary>
    public const ActorAttribute RestoredAttribute = ActorAttribute.Strength;

    /// <summary>Damage range — <c>RNDR(0x2d, 0x4a)</c>.</summary>
    /// <remarks>
    /// <b>45-73, the largest in the file</b> — roughly triple the 15-33 of the melee routine and an
    /// order above the 4-7 weak tier elsewhere.
    /// </remarks>
    public static readonly (int Min, int Max) Damage = (0x2d, 0x4a);

    /// <summary>Knockback applied — the strongest shove in the file.</summary>
    public const int Knockback = 4;

    /// <summary>Damage flags.</summary>
    public const int DamageFlags = 0x200;

    /// <summary>Whether the routine attacks, rather than deferring to pathing.</summary>
    public static bool Attacks(bool hasLineOfSight, int distance) => hasLineOfSight && distance > 1;
}
