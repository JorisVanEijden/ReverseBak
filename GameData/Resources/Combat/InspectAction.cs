namespace GameData.Resources.Combat;

/// <summary>
/// The Inspect command's follow-up click — <c>combat_arena_resolve_menu_action</c>'s
/// <c>case 3</c> (canassa COMBAT.C ~2296).
///
/// <para>Pressing Inspect only arms
/// <see cref="CombatCommandOutcome.PendingMode.InspectTarget"/>; this is what happens when the player
/// then clicks. See <see cref="CombatCommandOutcome"/> for the arm-versus-resolve split.</para>
/// </summary>
/// <remarks>
/// <b>WHAT INSPECT SHOWS IS <see cref="CombatAssessment"/>, AND THIS REMARK HAD IT WRONG TWICE.</b>
/// It first named the shoot menu's target panel, then — corrected on 2026-08-30 — claimed the arm
/// "switches the active actor, after which the HUD's default branch draws the stats panel for
/// whoever is now current". <b>Neither is what happens.</b>
///
/// <para><c>combat_arena_switch_active_actor</c> (COMBAT.C:1476) <b>never assigns
/// <c>g_current_actor</c></b>. Its whole body is: unload the spell subsystem and its tables to free
/// heap, call <c>combatenc_anim_actor_stat_rolls(target)</c>, reload them, and wait for the button
/// to be released. The name describes nothing the routine does — reading it as a switch is what
/// produced the wrong note, twice.</para>
///
/// <para>What the player actually sees is an <b>assessment</b>: up to eight of the target's stats,
/// each revealed only if the INSPECTOR passes a d100 roll against their own Assessment skill, drawn
/// over the world view in two columns of three, bracketed by dialog records
/// <see cref="CombatAssessment.OpeningDialog"/> and <see cref="CombatAssessment.ClosingDialog"/>.
/// So Inspect is the Assessment skill's one use in combat, and the reason it can show nothing
/// useful is a bad roll rather than a missing panel.</para>
///
/// <para>Read by <c>scripts/audit-unconsumed-models.py</c>: it separates a rule ported ahead
/// of its feature from an orphan nobody owns, so the audit stays a signal instead of a list
/// to re-triage every run.</para>
/// </remarks>
public static class InspectAction {
    /// <summary>What the follow-up click did.</summary>
    public enum Result {
        /// <summary>Nothing happened and the mode stays armed — the click missed.</summary>
        Ignored,

        /// <summary>The player backed out. The mode is cleared and the turn is NOT spent.</summary>
        Cancelled,

        /// <summary>An enemy was inspected: the view switches to them and the turn is spent.</summary>
        Inspected,
    }

    /// <summary>
    /// The move cost that stands for "backed out" rather than a real tile.
    /// </summary>
    /// <remarks>
    /// The original compares the cost against 1000 — a sentinel, not a reachable distance, since the
    /// grid is 8x13.
    /// </remarks>
    public const int CancelCost = 1000;

    /// <summary>
    /// Resolve the click.
    /// </summary>
    /// <param name="moveCost">
    /// <see cref="CancelCost"/> when the player backed out.
    /// </param>
    /// <param name="confirmed">Whether the click was a confirm rather than a cancel.</param>
    /// <param name="targetIsEncounterActor">
    /// Whether something under the cursor is an <b>encounter</b> actor. Party members do not qualify:
    /// the original gates on <c>combatenc_is_encounter_actor</c>, so you cannot inspect your own.
    /// </param>
    /// <remarks>
    /// <b>Inspecting COSTS THE ACTING CHARACTER THEIR TURN.</b> The successful branch clears
    /// <see cref="CombatantFlags.Ready"/> on the current actor before switching the view. That is not
    /// what "allows the current character to inspect one enemy" (the button's own help text, DDX 267)
    /// suggests, and it is the detail a port would drop — leaving inspection a free action the player
    /// could use every round at no cost.
    ///
    /// <para><b>An empty tile or a party member does nothing at all</b>, and crucially does NOT
    /// clear the mode: the original only resets state on a successful inspect or an explicit cancel,
    /// so a misclick leaves the player still choosing rather than silently wasting the command.</para>
    /// </remarks>
    public static Result Resolve(int moveCost, bool confirmed, bool targetIsEncounterActor) {
        if (moveCost == CancelCost) {
            return Result.Cancelled;
        }
        if (!confirmed) {
            return Result.Ignored;
        }
        return targetIsEncounterActor ? Result.Inspected : Result.Ignored;
    }

    /// <summary>Whether the result ends the acting character's turn.</summary>
    public static bool SpendsTheTurn(Result result) => result == Result.Inspected;

    /// <summary>Whether the result clears the armed mode.</summary>
    /// <remarks>A misclick keeps the mode armed; only success or an explicit cancel clears it.</remarks>
    public static bool ClearsTheMode(Result result) => result != Result.Ignored;
}
