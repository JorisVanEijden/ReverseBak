namespace GameData.Resources.Combat;

using System.Collections.Generic;

/// <summary>
/// The creature-specific ranged/breath turn — <c>combataiact_ranged_attack_turn</c>
/// (canassa CBTAIACT.C:85). Third of the nine AI action routines.
///
/// <para>Unlike <see cref="RangedExchange"/>, which is the ordinary crossbow shot, this is the
/// special attack a handful of creature types have. It is a <b>two-tier</b> attack: a heavy version
/// and a much weaker fallback.</para>
/// </summary>
public static class MonsterRangedTurn {
    /// <summary>What the turn does.</summary>
    public enum Outcome {
        /// <summary>No attack — the routine defers to the pathing/action chooser instead.</summary>
        Path,

        /// <summary>The heavy attack, with the creature's own action id and knockback.</summary>
        Heavy,

        /// <summary>The weak fallback attack.</summary>
        Weak,
    }

    /// <summary>Roll under this defers to pathing even with a clear shot.</summary>
    /// <remarks>
    /// <b>Line of sight is not enough.</b> The routine also needs <c>RND(100) &gt;= 50</c>, so a
    /// creature with a perfectly clear shot still walks away half the time. A port that attacks
    /// whenever it can see the target makes these creatures twice as aggressive as the original.
    /// </remarks>
    public const int PathRollThreshold = 0x32;   // 50

    /// <summary>The creature that always uses the heavy attack.</summary>
    /// <remarks>
    /// Type 0x39 skips the <c>RND2(4)</c> tier roll entirely and also plays a distinct wind-up
    /// animation, so it is the one creature here that never uses the weak fallback.
    /// </remarks>
    public const int AlwaysHeavyCreature = 0x39;

    /// <summary>Heavy-tier damage range — <c>RNDR(0x14, 0x1d)</c>.</summary>
    public static readonly (int Min, int Max) HeavyDamage = (0x14, 0x1d);   // 20..28

    /// <summary>Weak-tier damage range — <c>RNDR(4, 8)</c>.</summary>
    public static readonly (int Min, int Max) WeakDamage = (4, 8);

    /// <summary>Damage flags both tiers carry.</summary>
    public const int DamageFlags = 0x200;

    /// <summary>Knockback the weak tier applies.</summary>
    public const int WeakKnockback = 1;

    /// <summary>
    /// The heavy attack's action id and knockback, by creature type.
    /// </summary>
    /// <remarks>
    /// <b>Only four creature types have an entry, and the original does not guard the rest.</b> The
    /// switch has no default, so a creature that reaches the heavy branch without a case leaves
    /// <c>actionId</c> and <c>knockback</c> UNINITIALISED and attacks with whatever was on the
    /// stack. That is a latent bug rather than a rule; it is unreachable only as long as the
    /// discipline is assigned to these four types.
    /// </remarks>
    public static readonly IReadOnlyDictionary<int, (int ActionId, int Knockback)> HeavyByCreature =
        new Dictionary<int, (int, int)> {
            { 0x29, (2, 1) },
            { 0x2a, (3, 3) },
            { 0x2b, (0x32, 3) },
            { 0x39, (0x32, 3) },
        };

    /// <summary>Whether this creature has a defined heavy attack.</summary>
    public static bool HasHeavyAttack(int creatureType) => HeavyByCreature.ContainsKey(creatureType);

    /// <summary>
    /// Decide the turn.
    /// </summary>
    /// <param name="hasLineOfSight">Whether <c>combat_actor_trace_proj_path</c> finds a clear path.</param>
    /// <param name="pathRoll">The routine's <c>RND(100)</c>.</param>
    /// <param name="tierRoll">The routine's <c>RND2(4)</c> — 0..3.</param>
    /// <param name="creatureType">Used only for <see cref="AlwaysHeavyCreature"/>.</param>
    /// <remarks>
    /// <b>The heavy tier is the COMMON one</b>: <c>RND2(4) &lt;= 2</c> is three outcomes in four, so
    /// the weak fallback fires only a quarter of the time. Reading "&lt;= 2" as a minority case and
    /// swapping the branches would cut these creatures' damage from 20-28 down to 4-7 most turns.
    /// </remarks>
    public static Outcome Choose(bool hasLineOfSight, int pathRoll, int tierRoll, int creatureType) {
        if (!hasLineOfSight || pathRoll < PathRollThreshold) {
            return Outcome.Path;
        }
        return tierRoll <= 2 || creatureType == AlwaysHeavyCreature ? Outcome.Heavy : Outcome.Weak;
    }
}
