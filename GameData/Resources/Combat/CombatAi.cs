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
    /// candidate. Non-zero disqualifies it.</summary>
    public int AlliesNearby { get; set; }
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
    /// <summary>Class ids that have a bespoke routine instead of the capability cascade.</summary>
    /// <remarks>
    /// Their individual behaviours are not ported — see TASK-97. What matters structurally is that
    /// these classes <b>bypass the cascade entirely</b>: a spell-capable creature on this list still
    /// runs its own routine rather than casting.
    /// </remarks>
    private static readonly HashSet<int> SpeciesSpecificClasses = new HashSet<int> {
        0x13, 0x31, 0x29, 0x2a, 0x2b, 0x39, 0x38, 0x1d, 0x1f, 0x20, 0x21, 0x1c, 0x36,
    };

    /// <summary>Whether this class id has its own routine rather than using the cascade.</summary>
    public static bool HasSpeciesRoutine(int classId) => SpeciesSpecificClasses.Contains(classId);

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
    public static int SelectTarget(
        int fromX, int fromY, IReadOnlyList<TargetCandidate> candidates,
        int maxDistance, TargetRole role, int minAllyClearance) {
        var chosen = -1;
        if (candidates == null) {
            return chosen;
        }

        for (var i = 0; i < candidates.Count; i++) {
            TargetCandidate candidate = candidates[i];
            int distance = CombatGrid.ChebyshevDistance(fromX, fromY, candidate.X, candidate.Y);

            if (distance > maxDistance || candidate.IsDead) {
                continue;
            }
            // The clearance test uses the caller-supplied count; a 0 clearance disables it, matching
            // the original's early-out inside combatenc_party_within_cheby.
            if (minAllyClearance != 0 && candidate.AlliesNearby != 0) {
                continue;
            }
            if (!MatchesRole(candidate, role)) {
                continue;
            }

            chosen = i;
            maxDistance = distance;
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
