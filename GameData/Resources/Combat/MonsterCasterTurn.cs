namespace GameData.Resources.Combat;

/// <summary>
/// A casting monster's turn — <c>combat_ai_execute_turn</c> (canassa CBTAI.C:145).
///
/// <para>Runs <b>after</b> <see cref="OpportunisticCasts"/>: if a species-targeted cast fired, the
/// turn is over and none of this happens.</para>
/// </summary>
public static class MonsterCasterTurn {
    /// <summary>
    /// How much room the AI demands between its target and the party — <c>4 - Casting / 25</c>.
    /// </summary>
    /// <remarks>
    /// <b>A better caster accepts a riskier shot.</b> Casting 0 demands a clearance of 4; Casting 100
    /// demands 0, so an expert will target someone standing right among their allies while a novice
    /// will not. Inverting this — treating skill as needing MORE room — would make skilled casters
    /// the timid ones.
    /// </remarks>
    public static int ClearanceFor(int castingSkill) => 4 - castingSkill / 25;

    /// <summary>The divisor in <see cref="ClearanceFor"/>.</summary>
    public const int ClearancePerSkillStep = 25;

    /// <summary>Clearance used by the retry when the first pass finds nobody.</summary>
    /// <remarks>
    /// <b>Targeting runs twice.</b> The first pass applies <see cref="ClearanceFor"/>; if it finds no
    /// target the routine retries with clearance <b>0</b> and takes a different action on what it
    /// finds. A port that gave up after one pass would leave casters idle in crowded fights.
    /// </remarks>
    public const int RetryClearance = 0;

    /// <summary>
    /// The threshold-table index the health gate uses on each pass.
    /// </summary>
    /// <remarks>
    /// <b>This is the same table as the rest recovery, used differently — and the difference
    /// matters.</b> <c>RestAction.RecoveryAllowed</c> asks whether health clears <i>any</i> entry of
    /// <c>g_anStatCheckThreshold</c>, which reduces to "alive" because six entries are zero. Here the
    /// original names a SPECIFIC index — 0 on the first pass, 1 on the retry — and both of those
    /// entries are <b>10</b>. So a caster needs health above 10, not merely above 0.
    /// </remarks>
    public const int FirstPassThresholdIndex = 0;

    /// <inheritdoc cref="FirstPassThresholdIndex"/>
    public const int RetryThresholdIndex = 1;

    /// <summary>Whether the caster's health clears the gate for a pass.</summary>
    /// <param name="health">Current health.</param>
    /// <param name="thresholds">The ladder — pass <c>RestAction.ShippedHealthThresholds</c>.</param>
    /// <param name="thresholdIndex">
    /// <see cref="FirstPassThresholdIndex"/> or <see cref="RetryThresholdIndex"/>.
    /// </param>
    public static bool HealthAllowsCasting(int health,
        System.Collections.Generic.IReadOnlyList<int> thresholds, int thresholdIndex) =>
        thresholds != null
        && thresholdIndex >= 0 && thresholdIndex < thresholds.Count
        && health > thresholds[thresholdIndex];

    /// <summary>
    /// <b>The first pass also requires line of sight; the retry does not.</b>
    /// </summary>
    /// <remarks>
    /// The first arm calls <c>combat_actor_trace_proj_path</c> before casting. The retry arm goes
    /// straight to <c>combat_ai_resolve_attack_attempt</c> with no path test, so the fallback can act
    /// through cover the first pass would have refused.
    /// </remarks>
    public static bool FirstPassNeedsLineOfSight => true;

    /// <inheritdoc cref="FirstPassNeedsLineOfSight"/>
    public static bool RetryNeedsLineOfSight => false;
}
