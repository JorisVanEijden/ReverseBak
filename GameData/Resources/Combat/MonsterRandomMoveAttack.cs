namespace GameData.Resources.Combat;

/// <summary>
/// The wander-then-act routine — <c>combataiact_random_move_attack</c> (canassa CBTAIACT.C:39).
///
/// <para>Only the decision is modelled here; the walk itself needs the grid. Note the monster picks
/// a <b>random</b> destination (rerolling until it finds a movable tile) rather than heading for
/// anyone — this discipline wanders and then attacks whatever it ends up near.</para>
/// </summary>
public static class MonsterRandomMoveAttack {
    /// <summary>What the monster does after moving.</summary>
    public enum Action {
        /// <summary>Adjacent — swing.</summary>
        Melee,

        /// <summary>Cast with targeting type 5.</summary>
        CastFive,

        /// <summary>Cast with targeting type 4.</summary>
        CastFour,

        /// <summary>Nothing better to do: rest.</summary>
        Rest,

        /// <summary>Rest AND raise a guard.</summary>
        RestAndDefend,
    }

    /// <summary>Roll below this may cast at all.</summary>
    public const int CastThreshold = 0x50;      // 80

    /// <summary>Roll below this picks targeting type 5 over 4; above it, also defends.</summary>
    public const int MidRoll = 0x32;            // 50

    /// <summary>
    /// <b>A monster that has died mid-walk does not attack.</b>
    /// </summary>
    /// <remarks>
    /// The whole attack half is wrapped in <c>if ((flags &amp; 2) == 0)</c> — CAF_DEAD — <b>in the
    /// 1.02 CD build only</b>. The floppy build has no such guard, so this is one of the places the
    /// two versions genuinely differ. We target the CD build.
    /// </remarks>
    public static bool DeadActorSkipsTheAttack => true;

    /// <summary>
    /// Decide what to do once the move is done.
    /// </summary>
    /// <param name="distance">Chebyshev distance to the nearest living opponent.</param>
    /// <param name="roll">The routine's <c>RND(100)</c>.</param>
    /// <param name="halfHealth">
    /// Current health halved — <c>stat_actor_get(actor, 0, 0) &gt;&gt; 1</c>. <b>This doubles as the
    /// spell's magnitude</b>, so a wounded caster casts weaker spells.
    /// </param>
    /// <param name="hasLineOfSight">
    /// Whether <c>combat_actor_trace_proj_path</c> finds a clear path to the target.
    /// </param>
    /// <remarks>
    /// <b>Casting needs four things at once</b>: not adjacent, a roll under
    /// <see cref="CastThreshold"/>, line of sight, and <paramref name="halfHealth"/> that is not
    /// exactly 1. That last guard is peculiar — a monster on 2 or 3 health cannot cast, while one on
    /// 0 or 1 (half = 0) can — but it is what the code tests, so it is reproduced rather than
    /// tidied into "must be healthy enough".
    ///
    /// <para><b>The fallback is Rest, and half the time Defend as well.</b> When the cast conditions
    /// fail the monster always rests, and additionally raises a guard when the roll is above
    /// <see cref="MidRoll"/> — so it can hold both flags at once. A port that treated rest and defend
    /// as mutually exclusive would drop the guard.</para>
    /// </remarks>
    public static Action Choose(int distance, int roll, int halfHealth, bool hasLineOfSight) {
        if (distance <= 1) {
            return Action.Melee;
        }

        bool canCast = halfHealth != 1 && distance >= 2 && roll < CastThreshold && hasLineOfSight;
        if (canCast) {
            return roll < MidRoll ? Action.CastFive : Action.CastFour;
        }

        return roll > MidRoll ? Action.RestAndDefend : Action.Rest;
    }

    /// <summary>The magnitude a cast from this routine carries.</summary>
    /// <remarks>
    /// The same halved health that gates the decision is passed to <c>cspell_resolve_cast</c> as its
    /// power, so <b>a hurt monster's spells are weaker</b> — the stat is doing double duty.
    /// </remarks>
    public static int SpellMagnitude(int halfHealth) => halfHealth;
}
