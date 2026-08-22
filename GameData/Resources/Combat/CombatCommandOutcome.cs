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
        /// Set by Cast on one of its two exits. <b>Not identified.</b> The other exit sets
        /// <see cref="ShootMenu"/>, so this is the branch that follows a successful spell selection;
        /// naming it from the control flow alone would be a guess.
        /// </summary>
        CastFollowUp = 1,

        /// <summary>Pick an enemy to inspect — set by <see cref="CombatCommands.InspectId"/>.</summary>
        InspectTarget = 3,

        /// <summary>
        /// The SHOOT menu is up. Set by Shoot, which also calls
        /// <c>combat_arena_shootmenu_rebuild</c> to repack the quarrel cells for this actor
        /// (see <see cref="CombatMenuSlots.PackCells"/>), and by Cast's other exit.
        /// </summary>
        ShootMenu = 4,
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
    /// Shoot and Inspect are the two that arm rather than resolve. Cast arms one of two modes
    /// depending on a branch this model does not yet resolve, so it reports
    /// <see cref="PendingMode.CastFollowUp"/> as the documented default rather than pretending the
    /// choice does not exist.
    /// </remarks>
    public static PendingMode ModeFor(CombatCommands.Command command) {
        switch (command) {
            case CombatCommands.Command.Shoot: return PendingMode.ShootMenu;
            case CombatCommands.Command.Inspect: return PendingMode.InspectTarget;
            case CombatCommands.Command.Cast: return PendingMode.CastFollowUp;
            default: return PendingMode.None;
        }
    }

    /// <summary>
    /// Whether a press needs a follow-up click before anything happens.
    /// </summary>
    public static bool ArmsAPendingMode(CombatCommands.Command command) =>
        ModeFor(command) != PendingMode.None;
}
