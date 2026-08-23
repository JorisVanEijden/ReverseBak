namespace GameData.Resources.Combat;

/// <summary>
/// <c>combataiact_actor_melee_attack</c> (canassa CBTAIACT.C:142) — fourth of the nine AI action
/// routines.
///
/// <para><b>The name is wrong: this routine's main branch is a RANGED attack.</b> It reaches for
/// melee only when the target is adjacent or the shot is blocked. Trusting the name would have a
/// port swinging at a target three tiles away.</para>
/// </summary>
public static class MonsterMeleeTurn {
    /// <summary>What the turn does.</summary>
    public enum Outcome {
        /// <summary>The ranged attack — line of sight and a target further than one tile.</summary>
        RangedAttack,

        /// <summary>Adjacent or blocked: try to take a tile or attack, then fall back to pathing.</summary>
        TileOrAttack,

        /// <summary>Nothing to attack at all.</summary>
        NoTarget,
    }

    /// <summary>Damage range — <c>RNDR(0xf, 0x22)</c>.</summary>
    public static readonly (int Min, int Max) Damage = (0xf, 0x22);   // 15..33

    /// <summary>Damage flags.</summary>
    public const int DamageFlags = 0x200;

    /// <summary>Knockback applied by the hit.</summary>
    public const int Knockback = 1;

    /// <summary>
    /// How many frames the knockback animation steps through.
    /// </summary>
    /// <remarks>
    /// The routine loops <c>knockbackFrame</c> from 1 to 4, presenting a frame each time, before
    /// applying the damage. So <b>the shove is animated first and the damage lands after</b> — a port
    /// that applies damage up front and animates afterwards will kill the target before the
    /// animation it is supposed to play.
    /// </remarks>
    public const int KnockbackFrames = 4;

    /// <summary>Knockback timer set on each frame.</summary>
    public const int KnockbackTimer = 0x64;

    /// <summary>
    /// <b>A null target returns immediately — in the 1.02 CD build only.</b>
    /// </summary>
    /// <remarks>
    /// The floppy build has no such guard and would dereference it. Second such difference found in
    /// these routines; we target the CD build.
    /// </remarks>
    public static bool NullTargetIsGuarded => true;

    /// <summary>Decide the turn.</summary>
    /// <param name="hasTarget">Whether a nearest opponent was found at all.</param>
    /// <param name="hasLineOfSight">Whether the projectile path is clear.</param>
    /// <param name="distance">Chebyshev distance to that opponent.</param>
    public static Outcome Choose(bool hasTarget, bool hasLineOfSight, int distance) {
        if (!hasTarget) {
            return Outcome.NoTarget;
        }
        return hasLineOfSight && distance > 1 ? Outcome.RangedAttack : Outcome.TileOrAttack;
    }
}
