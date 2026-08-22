namespace GameData.Resources.Combat;

/// <summary>
/// What pressing a combat button actually does to the turn — <c>combat_arena_show_message_by_id</c>
/// and its caller (canassa COMBAT.C ~1938, ~2515).
///
/// <para><b>Combat buttons split into two kinds, and a port that misses the split gets the turn
/// structure wrong.</b> Some commands RESOLVE immediately and spend the turn; others only arm a
/// PENDING MODE that the arena reads on the player's next click. Treating the second kind as
/// immediate would resolve a shot or an inspection the moment the button is pressed, before a target
/// has been chosen.</para>
/// </summary>
public static class CombatCommandOutcome {
    /// <summary>What the arena is waiting for after a press.</summary>
    public enum PendingMode {
        /// <summary>Nothing pending — the press resolved on the spot.</summary>
        None = -1,

        /// <summary>
        /// <b>Cast was CANCELLED</b> — the spell picker returned -1.
        /// </summary>
        /// <remarks>
        /// Identified 2026-08-22, and it is the opposite of what the control flow first suggested:
        /// the successful branch is <see cref="TargetSelection"/>, and this one is the fall-through
        /// after <c>cspell_cast_menu_loop</c> answers -1. The same branch also clears the pending
        /// selection (<c>p_param5 = -1</c>), which is what a cancel should do.
        /// </remarks>
        CastCancelled = 1,

        /// <summary>Pick an enemy to inspect — set by <see cref="CombatCommands.InspectId"/>.</summary>
        InspectTarget = 3,

        /// <summary>
        /// <b>Something has been chosen and now needs a target.</b>
        /// </summary>
        /// <remarks>
        /// Shared by two commands, which is what names it: Shoot arms it (after
        /// <c>combat_arena_shootmenu_rebuild</c> repacks the quarrel cells for this actor — see
        /// <see cref="CombatMenuSlots.PackCells"/>), and Cast arms it once a spell has actually been
        /// picked. Calling it "the shoot menu" would have missed the spell half.
        /// </remarks>
        TargetSelection = 4,
    }

    /// <summary>Whether a command ends the acting character's turn there and then.</summary>
    /// <remarks>
    /// <b>The caller's <c>turnInact</c> flag.</b> When set, the arena runs
    /// <c>combat_arena_turn_actor_inact</c>, which resolves the turn and clears any pending mode back
    /// to none — so a command cannot both spend the turn and leave something armed.
    ///
    /// <para><see cref="CombatCommands.Command.Rest"/> is the subtle one: its case does NOT set the
    /// flag. <c>combatenc_actor_enter_defense</c> clears <see cref="CombatantFlags.Ready"/> itself,
    /// and the caller notices on the next pass — the turn still ends, just by a different route.
    /// Modelled as spending the turn, because that is what it does.</para>
    /// </remarks>
    public static bool SpendsTheTurn(CombatCommands.Command command) {
        switch (command) {
            case CombatCommands.Command.Rest:
            case CombatCommands.Command.Defend:
            case CombatCommands.Command.AutoResolve:
                return true;
            default:
                return false;
        }
    }

    /// <summary>The mode a command leaves armed, or <see cref="PendingMode.None"/>.</summary>
    /// <remarks>
    /// Shoot and Inspect are the two that arm rather than resolve.
    ///
    /// <para><b>Cast is not answerable here</b> — its mode depends on whether the player went
    /// through with the spell, which is only known after the picker closes. Use
    /// <see cref="ModeAfterCast"/>; this method reports <see cref="PendingMode.None"/> for Cast
    /// rather than picking one of its two outcomes arbitrarily.</para>
    /// </remarks>
    public static PendingMode ModeFor(CombatCommands.Command command) {
        switch (command) {
            case CombatCommands.Command.Shoot: return PendingMode.TargetSelection;
            case CombatCommands.Command.Inspect: return PendingMode.InspectTarget;
            default: return PendingMode.None;
        }
    }

    /// <summary>
    /// The mode Cast leaves armed, once the spell picker has closed.
    /// </summary>
    /// <param name="spellChosen">False when the picker returned -1, i.e. the player backed out.</param>
    /// <remarks>
    /// <b>Cast opens a MODAL spell picker inline</b> — <c>cspell_cast_menu_loop</c> runs a whole
    /// selection UI over the arena and returns a result, and only then does targeting begin. So
    /// pressing Cast does not arm targeting; <i>choosing a spell</i> does.
    ///
    /// <para>Cast also re-checks <see cref="CombatCapability.CanCast"/> before opening the picker and
    /// returns outright if it now fails, so the capability is verified twice: once to draw the
    /// button, once to honour the press.</para>
    /// </remarks>
    public static PendingMode ModeAfterCast(bool spellChosen) =>
        spellChosen ? PendingMode.TargetSelection : PendingMode.CastCancelled;

    /// <summary>
    /// Whether a press needs a follow-up click before anything happens.
    /// </summary>
    public static bool ArmsAPendingMode(CombatCommands.Command command) =>
        ModeFor(command) != PendingMode.None;
}
