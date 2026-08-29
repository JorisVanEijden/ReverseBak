namespace GameData.Resources.Combat;

/// <summary>
/// What the auto-resolve button actually does — <c>combat_arena_turn_loop</c> (canassa
/// COMBAT.C:1588).
/// </summary>
/// <remarks>
/// <b>IT IS NOT "hand the fight to the AI and let it play out".</b> That is what
/// <see cref="CombatCommands.Command.AutoResolve"/> said, and a port reading it writes a loop that
/// runs to a winner and returns. The original does something quite different: it plays the AI for
/// BOTH sides while <b>stopping at every party turn to offer the menu</b>, and one press takes
/// control back. It is "watch it play, interrupt when you like", not an instant resolution.
///
/// <para>The distinction is the whole feature. A port that resolves the fight in one call gives the
/// player no way out of a battle that is going badly — the exact situation the button exists for.</para>
/// </remarks>
public static class AutoResolveLoop {
    /// <summary>
    /// <b>The party's turns are played by the same AI, with the sides SWAPPED.</b>
    /// </summary>
    /// <remarks>
    /// The loop calls <c>combat_arena_swap_tgt_state()</c>, then <c>combatenc_ai_run_turn()</c>, then
    /// swaps back — so the monster AI runs with the party as its own side and the enemies as its
    /// targets. There is no separate "player AI": the same routine plays both, which is why an
    /// auto-resolved party fights exactly like a monster would.
    /// </remarks>
    public static bool PartyTurnsUseTheMonsterAiWithSidesSwapped => true;

    /// <summary>
    /// <b>Enemy turns run in an inner loop with no interruption; a PARTY turn is where it stops.</b>
    /// </summary>
    /// <remarks>
    /// The loop advances while the current actor is an encounter actor, running each one's turn
    /// back to back. Only when the picker lands on a party member does it draw the menu.
    /// So the interruption granularity is one PARTY turn, not one turn.
    ///
    /// <para><b>IT DRAWS THE MENU AND POLLS — IT DOES NOT WAIT.</b> An earlier wording here said
    /// "draw the menu and wait", which describes a different feature: a turn-by-turn prompt the
    /// player answers. <c>menupage_run</c> returns whatever is pending and the loop runs the party
    /// member's AI turn immediately afterwards either way; the menu result is only read to see
    /// whether the player has asked to <see cref="Bails"/>. So the fight keeps playing at full
    /// speed and a press interrupts it — which is what the type summary says and what the wording
    /// contradicted. Corrected 2026-08-29 against COMBAT.C:1620.</para>
    /// </remarks>
    public static bool StopsOnlyOnAPartyTurn => true;

    /// <summary>Menu results that end auto-resolve and hand control back.</summary>
    /// <remarks>
    /// <c>0x21</c> is the Back button — the same id that means "leave the fight" on the melee menu
    /// and "back out" on the shoot menu (see <see cref="CombatCommands.BacksOutOfShootMenu"/>). Here
    /// it is a third meaning: stop auto-resolving. A handler that routes id 33 by menu alone still
    /// has to know whether auto-resolve is running.
    /// </remarks>
    public const int BackMenuResult = 0x21;

    /// <inheritdoc cref="BackMenuResult"/>
    public const int CancelMenuResult = 1;

    /// <summary>Whether a menu result stops the loop.</summary>
    public static bool Bails(int menuResult) =>
        menuResult == BackMenuResult || menuResult == CancelMenuResult;

    /// <summary>
    /// Whether the loop should stop because a side is gone.
    /// </summary>
    /// <param name="livingOnTheEnemySide">Living actors in the encounter's list.</param>
    /// <param name="livingOnThePartySide">Living actors on the party's side.</param>
    /// <remarks>
    /// <b>Both counts are re-read INSIDE the loop, not computed once.</b> The party count is taken
    /// with the target state swapped, which is the same trick the retreat refusal uses — see
    /// <see cref="CombatCommands.RetreatRefusalDialog"/>. Caching either turns a fight that has
    /// already ended into one more round.
    /// </remarks>
    public static bool Finished(int livingOnTheEnemySide, int livingOnThePartySide) =>
        livingOnTheEnemySide == 0 || livingOnThePartySide == 0;
}
