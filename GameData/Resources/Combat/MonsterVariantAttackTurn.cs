namespace GameData.Resources.Combat;

/// <summary>
/// <c>combataiact_melee_random_attack</c> (canassa CBTAIACT.C:194) — sixth of the nine AI action
/// routines.
///
/// <para><b>Another "melee" name on a ranged routine</b>, and this one needs even more room than its
/// siblings. What is actually random is not the target but WHICH OF THREE ATTACKS it uses.</para>
/// </summary>
public static class MonsterVariantAttackTurn {
    /// <summary>
    /// <b>Distance must exceed TWO</b>, not one.
    /// </summary>
    /// <remarks>
    /// Every other attacking routine in this file uses <c>distance &gt; 1</c>; this one uses
    /// <c>&gt; 2</c>, so a target two tiles away is pathed to rather than attacked. Copying the
    /// neighbouring routine's bound would make this creature attack from a range it never uses.
    /// </remarks>
    public const int MinimumDistanceExclusive = 2;

    /// <summary>One of the three attacks this routine rolls between.</summary>
    public readonly struct Variant {
        public Variant(int actionId, int damageMin, int damageMax, int knockback) {
            ActionId = actionId;
            DamageMin = damageMin;
            DamageMax = damageMax;
            Knockback = knockback;
        }

        public int ActionId { get; }
        public int DamageMin { get; }
        public int DamageMax { get; }
        public int Knockback { get; }
    }

    /// <summary>
    /// The three attacks, indexed by the routine's <c>RND(3)</c>.
    /// </summary>
    /// <remarks>
    /// <b>Damage and knockback move in opposite directions.</b> Variant 0 hits hardest (15-33) and
    /// shoves least (1); variant 2 hits weakest (5-13) and shoves most (3). So the roll is a
    /// trade-off rather than a difficulty tier, and a port that ranked them as weak/medium/strong
    /// would get the knockback backwards.
    /// </remarks>
    public static readonly Variant[] Variants = {
        new Variant(actionId: 2, damageMin: 0xf, damageMax: 0x22, knockback: 1),
        new Variant(actionId: 3, damageMin: 5, damageMax: 34, knockback: 2),
        new Variant(actionId: 4, damageMin: 5, damageMax: 14, knockback: 3),
    };

    /// <summary>Damage flags all three carry.</summary>
    public const int DamageFlags = 0x200;

    /// <summary>
    /// <b>The rolled damage is scaled by a percentage of the actor's stat</b> —
    /// <c>cbstat_scale_base_stat_pct</c>.
    /// </summary>
    /// <remarks>
    /// Unlike the flat damage in <see cref="MonsterTurnRoutines.ChooseRangedTurn"/>, these attacks scale with the
    /// creature, so the ranges above are the input to that scaling rather than the damage dealt.
    /// The scaling function itself is not ported here.
    /// </remarks>
    public static bool DamageIsStatScaled => true;

    /// <summary>Whether the routine attacks at all.</summary>
    public static bool Attacks(bool hasLineOfSight, int distance) =>
        hasLineOfSight && distance > MinimumDistanceExclusive;

    /// <summary>The variant a roll selects.</summary>
    public static Variant VariantFor(int roll) => Variants[roll];
}
