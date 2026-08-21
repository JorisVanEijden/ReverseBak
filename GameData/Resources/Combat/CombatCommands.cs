namespace GameData.Resources.Combat;

/// <summary>
/// What each button on the combat HUD does — <c>combat_arena_show_message_by_id</c>'s left-click
/// branches (canassa COMBAT.C ~2011).
///
/// <para><b>These ids are per-screen and collide with the travel HUD's.</b> 19 is Defend here and
/// FollowRoad on REQ_MAIN; 46 is Cast here and CastSpell there. Combat needs its own handler —
/// feeding COMBAT.DAT through the travel screen's switch fires the travel action.</para>
/// </summary>
public static class CombatCommands {
    /// <summary>A combat menu button.</summary>
    public enum Command {
        /// <summary>Not a combat menu id.</summary>
        None,

        /// <summary>Raise a guard — <c>combatenc_actor_enter_defense</c>.</summary>
        Defend,

        /// <summary>Open the SHOOT menu. Refused outright if the actor cannot shoot.</summary>
        Shoot,

        /// <summary>Run the cast flow. Refused outright if the actor cannot cast.</summary>
        Cast,

        /// <summary>
        /// Hand the fight to the AI and let it play out — <c>combat_arena_turn_loop</c>.
        /// </summary>
        AutoResolve,

        /// <summary>
        /// Back out of the SHOOT menu, or leave the fight — one id, two meanings, decided by which
        /// menu is up. See <see cref="BacksOutOfShootMenu"/>.
        /// </summary>
        BackOrRetreat,

        /// <summary>
        /// Drawn when the actor can neither shoot nor cast, and never clickable — a label.
        /// </summary>
        CapabilityLabel,

        /// <summary>Open the character screen — <c>combat_arena_suspend_char_screen</c>.</summary>
        CharacterScreen,

        /// <summary>
        /// Sets a pending-action mode the arena reads later. <b>Not identified.</b> Id 32 clears one
        /// actor flag and sets another; id 47 sets the pending mode to 3. Both are real branches, but
        /// naming them from the body alone would be a guess, and this codebase has been burned by
        /// exactly that.
        /// </summary>
        UnidentifiedMode,
    }

    /// <summary>The action id for each command, as COMBAT.DAT ships them.</summary>
    public const int DefendId = 19;

    /// <inheritdoc cref="DefendId"/>
    public const int ShootId = 31;

    /// <inheritdoc cref="DefendId"/>
    public const int CastId = 46;

    /// <inheritdoc cref="DefendId"/>
    public const int AutoResolveId = 30;

    /// <inheritdoc cref="DefendId"/>
    public const int BackOrRetreatId = 33;

    /// <inheritdoc cref="DefendId"/>
    public const int CapabilityLabelId = 14;

    /// <summary>The character-screen button — the hidden 250x270 element on the left.</summary>
    public const int CharacterScreenId = 22;

    /// <summary>Ids 32 and 47 — see <see cref="Command.UnidentifiedMode"/>.</summary>
    public const int ModeIdA = 32;

    /// <inheritdoc cref="ModeIdA"/>
    public const int ModeIdB = 47;

    /// <summary>What a combat menu id does.</summary>
    public static Command For(int actionId) {
        switch (actionId) {
            case DefendId: return Command.Defend;
            case ShootId: return Command.Shoot;
            case CastId: return Command.Cast;
            case AutoResolveId: return Command.AutoResolve;
            case BackOrRetreatId: return Command.BackOrRetreat;
            case CapabilityLabelId: return Command.CapabilityLabel;
            case CharacterScreenId: return Command.CharacterScreen;
            case ModeIdA:
            case ModeIdB: return Command.UnidentifiedMode;
            default: return Command.None;
        }
    }

    /// <summary>
    /// <b>Auto-resolve is refused while the grid still carries a terrain-6 objective.</b>
    /// </summary>
    /// <remarks>
    /// <c>combatgrid_any_terrain_6()</c> is the same test <see cref="CombatEncounter.HasObjective"/>
    /// models: a trap puzzle with an exit still to reach. You cannot hand a puzzle to the AI and
    /// have it walk out for you — the button simply does nothing, with no message.
    /// </remarks>
    public static bool AutoResolveAllowed(bool gridHasObjective) => !gridHasObjective;

    /// <summary>
    /// Whether id 33 means "back out of the SHOOT menu" rather than "leave the fight".
    /// </summary>
    /// <remarks>
    /// <b>One button, two meanings.</b> With the shoot menu up it is a cancel that returns to the
    /// melee menu; with the melee menu up it is the retreat attempt — see
    /// <see cref="RetreatSucceeded"/>. A handler that ignores which menu is showing turns a harmless
    /// cancel into an attempt to flee, which can cost the actor its turn.
    /// </remarks>
    public static bool BacksOutOfShootMenu(bool shootMenuIsUp) => shootMenuIsUp;

    /// <summary>
    /// <b>Retreat is an ATTEMPT, and the roll decides whether you get out at all.</b>
    /// </summary>
    /// <remarks>
    /// On a SUCCESSFUL roll the escape dialog (<see cref="RetreatEscapeDialog"/>) plays and combat is
    /// cancelled. On a FAILURE the actor merely loses its turn — <c>CAF_READY</c> is cleared — and
    /// one of two refusal dialogs plays, chosen by comparing the living actor count against the
    /// other side's count.
    ///
    /// <para><b>Do not read this as "fleeing costs you a trap".</b> My first reading of the branch
    /// did, because canassa calls the roll <c>combat_arena_maybe_random_trap</c> and the success path
    /// plays a dialog that looks like a penalty. The control flow says otherwise: the roll IS the
    /// escape test, and the failure path is the one that keeps you in the fight. The name describes
    /// a side effect and hides the function's actual role, which is the usual hazard with these
    /// names.</para>
    /// </remarks>
    public static bool RetreatSucceeded(bool escapeRollPassed) => escapeRollPassed;

    /// <summary>Plays when the retreat gets out.</summary>
    public const int RetreatEscapeDialog = 0x22;

    /// <summary>Refusal shown when the living count differs from the other side's.</summary>
    public const int RetreatRefusedMismatchDialog = 0x23;

    /// <summary>The other refusal.</summary>
    public const int RetreatRefusedDialog = 0x12f;

    /// <summary><b>A failed retreat still costs the turn.</b></summary>
    public static bool FailedRetreatSpendsTheTurn => true;
}
