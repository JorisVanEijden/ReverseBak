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

        /// <summary>
        /// <b>REST for one round</b> — <c>combatenc_actor_enter_defense</c>, despite that name.
        /// </summary>
        /// <remarks>
        /// <b>This is not Defend.</b> The game's own describe record for id 19 (DDX 263) reads
        /// "causes the current character to rest for one round", and the behaviour agrees: the
        /// routine heals and spends the turn (see <c>RestAction</c>), which is resting, not
        /// guarding. Defending is <see cref="Defend"/>, id 32. canassa's function name is the
        /// misleading one here.
        /// </remarks>
        Rest,

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
        /// <b>Raise a guard</b> — <c>combatenc_set_flag8_clear_flag1</c>, id 32.
        /// </summary>
        /// <remarks>
        /// Identified 2026-08-22 from the describe record (DDX 266): "allows the current character to
        /// defend for one turn". The body agrees and closes a loop: it sets flag <b>0x08</b> —
        /// <see cref="CombatantFlags.Parry"/>, the flag <see cref="CombatFormulas.MeleeHits"/>
        /// already reads for its to-hit penalty — and clears <see cref="CombatantFlags.Ready"/>.
        /// </remarks>
        Defend,

        /// <summary>
        /// <b>Inspect one enemy</b> — id 47, which sets the arena's pending mode to 3.
        /// </summary>
        /// <remarks>
        /// Identified 2026-08-22 from the describe record (DDX 267): "allows the current character to
        /// inspect one enemy". Mode 3 is therefore a targeting state the arena reads on the next
        /// click, not an action resolved on the spot.
        /// </remarks>
        Inspect,
    }

    /// <summary>The action id for each command, as COMBAT.DAT ships them.</summary>
    /// <remarks>
    /// <b>Id 19 is REST, not Defend.</b> See <see cref="Command.Rest"/> — the game's own help text
    /// says so, and the two were transposed here until 2026-08-22.
    /// </remarks>
    public const int RestId = 19;

    /// <inheritdoc cref="RestId"/>
    public const int ShootId = 31;

    /// <inheritdoc cref="RestId"/>
    public const int CastId = 46;

    /// <inheritdoc cref="RestId"/>
    public const int AutoResolveId = 30;

    /// <inheritdoc cref="RestId"/>
    public const int BackOrRetreatId = 33;

    /// <inheritdoc cref="RestId"/>
    public const int CapabilityLabelId = 14;

    /// <summary>The character-screen button — the hidden 250x270 element on the left.</summary>
    public const int CharacterScreenId = 22;

    /// <summary>Raise a guard — sets Parry, clears Ready.</summary>
    public const int DefendId = 32;

    /// <summary>Inspect one enemy — a targeting mode, not an immediate action.</summary>
    public const int InspectId = 47;

    /// <summary>What a combat menu id does.</summary>
    public static Command For(int actionId) {
        switch (actionId) {
            case RestId: return Command.Rest;
            case ShootId: return Command.Shoot;
            case CastId: return Command.Cast;
            case AutoResolveId: return Command.AutoResolve;
            case BackOrRetreatId: return Command.BackOrRetreat;
            case CapabilityLabelId: return Command.CapabilityLabel;
            case CharacterScreenId: return Command.CharacterScreen;
            case DefendId: return Command.Defend;
            case InspectId: return Command.Inspect;
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
