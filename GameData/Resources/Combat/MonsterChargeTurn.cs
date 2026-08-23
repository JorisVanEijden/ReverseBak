namespace GameData.Resources.Combat;

/// <summary>
/// <c>combataiact_action_charge_near</c> (canassa CBTAIACT.C:177) — fifth of the nine AI action
/// routines.
///
/// <para><b>The name is wrong here too: it does not charge.</b> Given room and a clear path it
/// SHOOTS, and otherwise defers to the pathing chooser. Nothing in the routine closes distance
/// deliberately.</para>
/// </summary>
public static class MonsterChargeTurn {
    /// <summary>What the turn does.</summary>
    public enum Outcome {
        /// <summary>Shoot the nearest opponent.</summary>
        RangedAttack,

        /// <summary>Defer to the pathing/action chooser.</summary>
        Path,
    }

    /// <summary>Minimum distance for the shot.</summary>
    /// <remarks>
    /// <b>Three tiles, not two.</b> So this discipline will not shoot a target two tiles away — it
    /// paths instead — which is a wider dead zone than the usual "not adjacent" rule elsewhere in the
    /// AI.
    /// </remarks>
    public const int MinimumRange = 3;

    /// <summary>The quarrel type used.</summary>
    public const int QuarrelType = 8;

    /// <summary>
    /// Roll below this defers to pathing — a <b>5% failure</b>, not a 5% chance to act.
    /// </summary>
    /// <remarks>
    /// The test is <c>RND(100) &gt;= 5</c>, so the shot happens 95 times in 100. Reading the constant
    /// as the success rate would invert the routine almost completely.
    /// </remarks>
    public const int FailureRollBelow = 5;

    /// <summary>
    /// <b>The trace uses mode 0, not mode 1.</b>
    /// </summary>
    /// <remarks>
    /// <c>combat_actor_trace_proj_path(actor, target, 0)</c> — every other routine in this file
    /// passes 1. The modes are not modelled here; recorded so the difference is not smoothed away.
    /// </remarks>
    public const int TraceMode = 0;

    /// <summary>
    /// <b>The routine always clears the actor's stored target, on either branch.</b>
    /// </summary>
    /// <remarks>
    /// So this discipline never carries a target between turns — anything that assumed a persistent
    /// target would be wrong for it.
    /// </remarks>
    public static bool ClearsStoredTarget => true;

    /// <summary>Decide the turn.</summary>
    /// <param name="distance">Chebyshev distance to the nearest opponent.</param>
    /// <param name="hasLineOfSight">Result of the mode-0 trace.</param>
    /// <param name="roll">The routine's <c>RND(100)</c>.</param>
    public static Outcome Choose(int distance, bool hasLineOfSight, int roll) =>
        distance >= MinimumRange && hasLineOfSight && roll >= FailureRollBelow
            ? Outcome.RangedAttack
            : Outcome.Path;
}
