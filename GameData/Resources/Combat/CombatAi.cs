namespace GameData.Resources.Combat;

using System.Collections.Generic;

/// <summary>Which candidates a monster will consider when picking a target.</summary>
/// <remarks>Values are the original's <c>role_filter</c>. Anything outside this set matches nothing
/// at all, so an unknown filter leaves the monster with no target rather than a random one.</remarks>
public enum TargetRole {
    /// <summary>Anyone.</summary>
    Anyone = 0,

    /// <summary>Spellcasters — pick off the mages first.</summary>
    Spellcaster = 1,

    /// <summary>The wounded: stamina at or below 50%.</summary>
    Wounded = 2,

    /// <summary>Anyone who can take a ranged shot.</summary>
    MissileCapable = 3,

    /// <summary>Someone already engaged with a living target.</summary>
    Engaged = 4,

    /// <summary>Someone targeting the lead actor.</summary>
    TargetingTheLeader = 5,

    /// <summary>Someone whose own target has died, i.e. just came free.</summary>
    Disengaged = 6,
}

/// <summary>One candidate as the targeting filter sees it.</summary>
public sealed class TargetCandidate {
    /// <summary>Grid position.</summary>
    public int X { get; set; }

    /// <inheritdoc cref="X"/>
    public int Y { get; set; }

    /// <summary>Dead candidates are never picked.</summary>
    public bool IsDead { get; set; }

    /// <summary>Whether this candidate can cast.</summary>
    public bool CanCastSpells { get; set; }

    /// <summary>Whether this candidate could take a ranged shot.</summary>
    public bool CanShoot { get; set; }

    /// <summary>Stamina as a percentage of its maximum.</summary>
    public int StaminaPercent { get; set; }

    /// <summary>Whether this candidate currently has a target at all.</summary>
    public bool HasTarget { get; set; }

    /// <summary>Whether that target is dead.</summary>
    public bool TargetIsDead { get; set; }

    /// <summary>Whether that target is the lead actor.</summary>
    public bool TargetsTheLeader { get; set; }

    /// <summary>How many of the monster's own side stand within the required clearance of this
    /// candidate. Non-zero disqualifies it. <b>Ranged path only</b> — the melee selector has no
    /// clearance parameter and uses <see cref="AttackersAlready"/> instead.</summary>
    public int AlliesNearby { get; set; }

    /// <summary>
    /// How many live attackers are ALREADY aimed at this candidate. <b>Melee path only</b> — the
    /// melee selector skips a candidate once this reaches
    /// <see cref="CombatAi.MaxAttackersPerCandidate"/>, which is how that half of the AI spreads
    /// the pack. Counted from the monsters' own side, like <see cref="AlliesNearby"/>.
    /// </summary>
    public int AttackersAlready { get; set; }
}

/// <summary>What a monster does on its turn.</summary>
public enum AiAction {
    /// <summary>Fleeing: walk toward the chosen edge tile and leave the field on arrival.</summary>
    Flee,

    /// <summary>This creature has its own hand-written routine, selected by class id.</summary>
    SpeciesSpecific,

    /// <summary>Cast a spell.</summary>
    Cast,

    /// <summary>Take a ranged shot.</summary>
    Shoot,

    /// <summary>Close and attack, or move.</summary>
    MeleeOrMove,
}

/// <summary>
/// Monster combat AI: what a creature does on its turn, and who it does it to.
///
/// <para>Ported from <c>combatenc_ai_sel_execute_action</c> and
/// <c>combatenc_ai_pick_target_by_role</c> (<c>SRC/COMBAT/ENC/CBENC.C</c>).</para>
/// </summary>
public static class CombatAi {
    /// <summary>
    /// Which bespoke routine a species-specific class runs — the seven branches of
    /// <c>combatenc_ai_sel_execute_action</c>'s switch (CBENC.C:925).
    /// </summary>
    /// <remarks>
    /// <b>These are named for what they DO, not for the original's symbols</b>, one of which is
    /// actively misleading: <c>combataiact_pick_melee_or_missl</c> does not pick between melee and
    /// missile — past arm's length it may <i>cast a spell</i> instead (CBTAIACT.C:32).
    ///
    /// <para><b>Knowing WHICH routine matters even though none is ported in detail.</b> The switch
    /// splits thirteen classes into melee-flavoured and ranged-flavoured groups, and treating them
    /// as one undifferentiated "has its own routine" bucket loses that — which is how every one of
    /// them ended up doing nothing at all. The detail inside each routine is still TASK-97;
    /// the branch it belongs to is not a guess.</para>
    /// </remarks>
    public enum SpeciesRoutine {
        /// <summary>
        /// Adjacent: melee. Otherwise a roll picks a spell or a shot —
        /// <c>combataiact_pick_melee_or_missl</c> (CBTAIACT.C:23). The only distance-conditional one.
        /// </summary>
        MeleeOrRangedByDistance,

        /// <summary>Wander, then attack — <c>combataiact_random_move_attack</c> (CBTAIACT.C:39).</summary>
        RandomMoveAttack,

        /// <summary>Ranged — <c>combataiact_ranged_attack_turn</c>.</summary>
        RangedAttackTurn,

        /// <summary>Straight melee — <c>combataiact_actor_melee_attack</c> (CBTAIACT.C:142).</summary>
        MeleeAttack,

        /// <summary>Close on the nearest and attack — <c>combataiact_action_charge_near</c> (CBTAIACT.C:177).</summary>
        ChargeNearest,

        /// <summary>Melee against a randomly chosen target — <c>combataiact_melee_random_attack</c> (CBTAIACT.C:194).</summary>
        MeleeRandomTarget,

        /// <summary>Ranged — <c>combataiact_ranged_attack</c> (CBTAIACT.C:235).</summary>
        RangedAttack,
    }

    /// <summary>
    /// The class-id → routine table, transcribed from <c>combatenc_ai_sel_execute_action</c>'s
    /// switch (CBENC.C:925). <b>Order of the cases is not meaningful; the grouping is.</b>
    /// </summary>
    private static readonly Dictionary<int, SpeciesRoutine> SpeciesRoutines =
        new Dictionary<int, SpeciesRoutine> {
            { 0x13, SpeciesRoutine.MeleeOrRangedByDistance },
            { 0x31, SpeciesRoutine.RandomMoveAttack },
            { 0x29, SpeciesRoutine.RangedAttackTurn },
            { 0x2a, SpeciesRoutine.RangedAttackTurn },
            { 0x2b, SpeciesRoutine.RangedAttackTurn },
            { 0x39, SpeciesRoutine.RangedAttackTurn },
            { 0x38, SpeciesRoutine.MeleeAttack },
            { 0x1d, SpeciesRoutine.ChargeNearest },
            { 0x1f, SpeciesRoutine.ChargeNearest },
            { 0x20, SpeciesRoutine.ChargeNearest },
            { 0x21, SpeciesRoutine.ChargeNearest },
            { 0x1c, SpeciesRoutine.MeleeRandomTarget },
            { 0x36, SpeciesRoutine.RangedAttack },
        };

    /// <summary>Whether this class id has its own routine rather than using the cascade.</summary>
    /// <remarks>
    /// These classes <b>bypass the cascade entirely</b>: a spell-capable creature on this list still
    /// runs its own routine rather than casting.
    /// </remarks>
    public static bool HasSpeciesRoutine(int classId) => SpeciesRoutines.ContainsKey(classId);

    /// <summary>The routine a class runs, or <c>null</c> when it uses the ordinary cascade.</summary>
    public static SpeciesRoutine? SpeciesRoutineOf(int classId) =>
        SpeciesRoutines.TryGetValue(classId, out SpeciesRoutine routine)
            ? routine
            : (SpeciesRoutine?)null;

    /// <summary>
    /// Whether a routine's attack is made at range rather than in contact.
    /// </summary>
    /// <remarks>
    /// <b><see cref="SpeciesRoutine.MeleeOrRangedByDistance"/> is deliberately absent</b>, because it
    /// is neither until you know how far away the target is — the caller has to resolve that one
    /// with a distance, and a default answer here would silently pick a side.
    /// </remarks>
    public static bool IsRangedRoutine(SpeciesRoutine routine) =>
        routine == SpeciesRoutine.RangedAttackTurn || routine == SpeciesRoutine.RangedAttack;

    /// <summary>
    /// Decides what a monster does this turn.
    /// </summary>
    /// <remarks>
    /// Order matters and is not the obvious one: the morale check runs <b>first</b> and fleeing
    /// preempts everything, then a per-class routine may take over, and only what is left falls
    /// through the spellcast → shoot → melee cascade.
    /// </remarks>
    /// <param name="isFleeing">CAF_FLEE, as left by the morale check that runs before this.</param>
    public static AiAction ChooseAction(int classId, bool isFleeing, bool canCastSpells, bool canShoot) {
        if (isFleeing) {
            return AiAction.Flee;
        }
        if (HasSpeciesRoutine(classId)) {
            return AiAction.SpeciesSpecific;
        }
        if (canCastSpells) {
            return AiAction.Cast;
        }
        return canShoot ? AiAction.Shoot : AiAction.MeleeOrMove;
    }

    /// <summary>
    /// Picks a target: the <b>nearest</b> candidate within range that matches the role and stands
    /// clear of its allies. Returns the index into <paramref name="candidates"/>, or -1 for none.
    /// </summary>
    /// <param name="maxDistance">Chebyshev search radius. Each acceptance tightens it, which is what
    /// makes the result the nearest match rather than the last one scanned.</param>
    /// <param name="minAllyClearance">
    /// A candidate is skipped when any of <b>the monsters' own side</b> already stands within this
    /// distance of it — so the pack spreads its attention across targets instead of converging on
    /// one.
    ///
    /// <para><b>It is the MONSTERS' positions that disqualify a target, not the party's.</b>
    /// <c>combatenc_party_within_cheby</c> scans the B array, which is the encounter roster; its
    /// canassa name says "party" and is wrong, in the usual way. An earlier version of this remark
    /// said monsters "prefer stragglers to someone standing in the line", which reads as the party's
    /// own clustering mattering and would have had a caller fill
    /// <see cref="TargetCandidate.AlliesNearby"/> from the wrong side. That field's own doc had it
    /// right.</para>
    /// </param>
    /// <summary>Radius the "anyone" behaviours sweep — <c>monster_*AnyoneWithinSix</c>.</summary>
    public const int AnyoneSearchRadius = 6;

    /// <summary>Radius a melee behaviour sweeps for its specific role: the whole field.</summary>
    public const int MeleeSearchRadius = 100;

    /// <summary>Radius a ranged behaviour sweeps for its specific role.</summary>
    public const int RangedSearchRadius = 10;

    /// <summary>
    /// How far a behaviour looks for its target. <b>The radius belongs to the BEHAVIOUR, not to the
    /// AI.</b>
    /// </summary>
    /// <remarks>
    /// There is no single search radius, which is the thing that is easy to get wrong because one
    /// family really does use a constant. Three families are named in IDA and they pass three
    /// different numbers:
    /// <list type="table">
    ///   <item><term><c>combat_ai_execute_turn</c> wrappers</term><description>6 — see
    ///     <see cref="AiTurnPackets.TargetSearchRadius"/></description></item>
    ///   <item><term>ovr170 melee <c>monster_engage*</c> (6 of 7)</term><description>100</description></item>
    ///   <item><term>ovr172 ranged <c>monster_shoot*</c> (5 of 6)</term><description>10</description></item>
    ///   <item><term><c>monster_engageAnyoneWithinSix</c> / <c>monster_shootAnyoneWithinSix</c></term>
    ///     <description>6</description></item>
    /// </list>
    /// Each family searches wide for its <i>specific</i> role — the whole field for a melee
    /// creature, ten cells for a crossbow shot — and falls back to a short radius-6 sweep for
    /// "anyone". A resolver-wide constant matches none of them.
    /// </remarks>
    public static int SearchRadiusFor(AiAction action, TargetRole role) =>
        // Casting goes through the combat_ai_execute_turn wrappers, which are the family that
        // passes 6 (AiTurnPackets.TargetSearchRadius) — the same number the "anyone" sweeps use.
        // Without this arm a caster picking a SPECIFIC role would fall through to melee's 100.
        action == AiAction.Cast || role == TargetRole.Anyone ? AnyoneSearchRadius
        : action == AiAction.Shoot ? RangedSearchRadius
        : MeleeSearchRadius;

    /// <summary>
    /// How much room a shooter needs around its target, derived from its crossbow accuracy.
    /// </summary>
    /// <remarks>
    /// <b>It is a won't-shoot-into-a-melee rule, not a range or a to-hit modifier.</b>
    /// <c>monster_crossbowShotByTargetMode</c> @0x663d9 computes it and hands it to
    /// <c>combat_selectTargetByMode</c> as the clearance that rejects a candidate with anyone
    /// standing too close:
    /// <code>
    /// 663f0  add ax, 24
    /// 663f3  mov bx, 25
    /// 663f7  idiv bx          ; (accuracy + 24) / 25
    /// 663f9  mov dx, 4
    /// 663fc  sub dx, ax       ; clearance = 4 - that
    /// </code>
    /// A poor shot refuses a target with anyone within four cells; a perfect shot fires regardless.
    /// Leaving it at 0 makes every monster a perfect shot.
    ///
    /// <para><b>The steps do not fall on multiples of 25.</b> <c>idiv</c> truncates toward zero, so
    /// the breaks are at 1, 26, 51 and 76 — accuracy 25 still needs 3 cells and it is 26 that drops
    /// to 2. A table sampled at 0/25/50/75/100 reads as though the boundaries were the round
    /// numbers, and a port built from that sampling is wrong for exactly one accuracy point in
    /// four.</para>
    ///
    /// <para>Clamped at 0 because an accuracy above 100 would otherwise go negative, and "less than
    /// no clearance" is not a thing the rule can mean — a perfect shot already fires regardless.</para>
    /// </remarks>
    public static int AllyClearanceForAccuracy(int crossbowAccuracy) {
        int steps = (crossbowAccuracy + 24) / 25;
        int clearance = 4 - steps;
        return clearance < 0 ? 0 : clearance;
    }

    /// <summary>
    /// How many of its own side the melee selector lets pile onto one candidate.
    /// </summary>
    /// <remarks>
    /// <b>The two selectors spread the pack by different rules.</b> The ranged copy skips a
    /// candidate with anyone standing within <c>exclusionRadius</c> of it. The melee copy
    /// (<c>combat_selectTargetByCriterion</c> @0x64ff6) has no such parameter — it counts how many
    /// live attackers are ALREADY aimed at the candidate and skips it once that reaches this cap,
    /// computed once at entry as <c>ceil(attackers / candidates)</c> and floored at 1. Six
    /// attackers against two targets gives 3.
    ///
    /// <para>Porting the ranged clearance onto the melee path is the easy mistake — it looks like
    /// the same "spread out" behaviour and it is not the rule the melee selector runs. Porting
    /// neither is worse: the pack converges on whoever is nearest.</para>
    /// </remarks>
    public static int MaxAttackersPerCandidate(int liveAttackers, int liveCandidates) {
        if (liveCandidates <= 0) {
            return 1;
        }
        int cap = (liveAttackers + liveCandidates - 1) / liveCandidates;
        return cap < 1 ? 1 : cap;
    }

    /// <param name="maxAttackersPerCandidate">
    /// The melee saturation cap from <see cref="MaxAttackersPerCandidate"/>; 0 disables the rule,
    /// which is what the ranged path wants — it spreads with
    /// <paramref name="minAllyClearance"/> instead.
    /// </param>
    /// <param name="excludeAtMaxDistance">
    /// <b>The two selectors bound the distance differently.</b> The game ships this routine twice —
    /// <c>combat_selectTargetByMode</c> @0x63ce6 (ovr169, ranged) accepts
    /// <c>dist &lt;= maxDistance</c>, while <c>combat_selectTargetByCriterion</c> @0x64ff6 (ovr170,
    /// melee) <i>skips</i> on <c>dist &gt;= maxDistance</c>. Set this for the melee families, where
    /// a radius of 6 means "within 5". It makes no difference at 100 and all the difference at 6.
    /// </param>
    public static int SelectTarget(
        int fromX, int fromY, IReadOnlyList<TargetCandidate> candidates,
        int maxDistance, TargetRole role, int minAllyClearance,
        bool excludeAtMaxDistance = false, int maxAttackersPerCandidate = 0) {
        var chosen = -1;
        if (candidates == null) {
            return chosen;
        }

        // *** The behaviour's radius and the nearest-so-far bound are kept APART. *** They used to
        // be one variable that each acceptance tightened, which is equivalent while the test is
        // `>` — but applying the melee `>=` rule to a tightened bound would also start rejecting a
        // candidate at the SAME distance as the best so far, silently flipping which of two
        // equidistant targets wins. That tie-break is not something the exclusive bound was
        // observed to change, so it is left exactly as it was.
        int nearestSoFar = int.MaxValue;

        for (var i = 0; i < candidates.Count; i++) {
            TargetCandidate candidate = candidates[i];
            int distance = CombatGrid.ChebyshevDistance(fromX, fromY, candidate.X, candidate.Y);

            bool outOfRange = excludeAtMaxDistance ? distance >= maxDistance : distance > maxDistance;
            if (outOfRange || distance > nearestSoFar || candidate.IsDead) {
                continue;
            }
            // The clearance test uses the caller-supplied count; a 0 clearance disables it, matching
            // the original's early-out inside combatenc_party_within_cheby.
            if (minAllyClearance != 0 && candidate.AlliesNearby != 0) {
                continue;
            }
            // The melee half of the same idea: not "who is standing near it" but "how many of us
            // are already swinging at it".
            if (maxAttackersPerCandidate != 0
                && candidate.AttackersAlready >= maxAttackersPerCandidate) {
                continue;
            }
            if (!MatchesRole(candidate, role)) {
                continue;
            }

            chosen = i;
            nearestSoFar = distance;
        }
        return chosen;
    }

    private static bool MatchesRole(TargetCandidate candidate, TargetRole role) => role switch {
        TargetRole.Anyone => true,
        TargetRole.Spellcaster => candidate.CanCastSpells,
        TargetRole.Wounded => candidate.StaminaPercent <= 50,
        TargetRole.MissileCapable => candidate.CanShoot,
        TargetRole.Engaged => candidate.HasTarget && !candidate.TargetIsDead,
        TargetRole.Disengaged => candidate.HasTarget && candidate.TargetIsDead,
        TargetRole.TargetingTheLeader => candidate.TargetsTheLeader,
        // An unrecognised filter matches nothing, leaving the monster targetless.
        _ => false,
    };
}
