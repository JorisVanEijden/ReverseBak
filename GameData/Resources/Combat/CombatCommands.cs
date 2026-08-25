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
        /// Let the AI play — <c>combat_arena_turn_loop</c>. <b>Not an instant resolution:</b> it
        /// stops at every party turn to offer the menu, and one press takes control back. See
        /// <see cref="AutoResolveLoop"/>.
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

        /// <summary>
        /// Suspend the fight and open a screen — the <b>inventory</b> unless a modifier is held.
        /// See <see cref="SuspendScreenFor"/>; the routine's canassa name is misleading.
        /// </summary>
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

    /// <summary>Percentage chance the escape roll passes, once the party qualifies to try.</summary>
    public const int EscapeChancePercent = 50;

    /// <summary>
    /// The escape roll itself — <c>combat_arena_maybe_random_trap</c> (canassa COMBAT.C:1545).
    /// </summary>
    /// <param name="anyPartyMemberDead">
    /// Whether any actor on the party's side is dead. A combatant counts only if it is a real party
    /// member (the original tests <c>charSlot != 0</c>), so a dead summon does not block the escape.
    /// </param>
    /// <param name="roll">A roll in <c>[0, 100)</c>.</param>
    /// <param name="encounterAllowsEscape">
    /// Whether this encounter permits retreat at all — <see cref="TrapData.AllowsRetreat"/>.
    /// </param>
    /// <remarks>
    /// <b>Three conditions, and only one of them is the coin flip.</b>
    /// <list type="number">
    ///   <item><b>A dead party member blocks the retreat outright</b> — the loop breaks on the first
    ///     one and never reaches the roll. So a party that has already lost someone is committed to
    ///     the fight, which is the opposite of the intuition that losing makes you likelier to run.</item>
    ///   <item>Then the 50% roll.</item>
    ///   <item>Then <b>the encounter must permit escape at all</b>, tested last and ANDed with the
    ///     rest, so a locked encounter refuses however the roll went.</item>
    /// </list>

    /// <para><b>That third condition is a per-encounter lock, not a load check.</b> The original
    /// reads a flag whose name says "traps loaded", and taking the name at face value is wrong twice
    /// over: the flag is raised unconditionally when the encounter's TRAPS.DAT record is opened, and
    /// it is lowered only by one element type that places nothing
    /// (<see cref="TrapElementType.RetreatLock"/>). So it defaults to ALLOW and five encounters out
    /// of 768 opt out — the opposite polarity to "escape needs data that may be missing", which
    /// would forbid retreat in every ordinary fight.</para>
    ///
    /// <para><b>The name is the usual hazard.</b> "maybe_random_trap" describes a side effect; the
    /// return value is what the caller reads as "you got away" (it plays
    /// <see cref="RetreatEscapeDialog"/> and cancels the fight). See
    /// <see cref="RetreatSucceeded"/>, which records the same warning from the other end.</para>
    /// </remarks>
    public static bool EscapeRollPasses(bool anyPartyMemberDead, int roll, bool encounterAllowsEscape) {
        if (anyPartyMemberDead) {
            return false;
        }
        return roll < EscapeChancePercent && encounterAllowsEscape;
    }

    /// <summary>Plays when the retreat gets out.</summary>
    public const int RetreatEscapeDialog = 0x22;

    /// <summary>Refusal shown when someone on the party side is down.</summary>
    public const int RetreatRefusedMismatchDialog = 0x23;

    /// <summary>Refusal shown when the whole party is standing and the attempt simply failed.</summary>
    public const int RetreatRefusedDialog = 0x12f;

    /// <summary>
    /// Which refusal plays when the escape does not get out.
    /// </summary>
    /// <param name="anyPartyMemberDown">Whether any slot on the party's side is not alive.</param>
    /// <remarks>
    /// <b>The two refusals are not interchangeable flavour — they answer different failures.</b> The
    /// original compares <c>combatenc_alive_actor_count()</c> against <c>g_nCombatOtherCount</c>
    /// while the target state is SWAPPED (<c>combat_arena_swap_tgt_state</c> exchanges the active and
    /// other lists around the test), so both sides of that comparison are the PARTY: living members
    /// against total slots. Unequal means someone is down.
    ///
    /// <para>That dovetails with <see cref="EscapeRollPasses"/>, and is what makes the pair
    /// coherent: a dead party member blocks the roll outright, so it lands here with the counts
    /// unequal and plays <see cref="RetreatRefusedMismatchDialog"/> — "you cannot leave, someone is
    /// down". Reaching here with the whole party standing means the roll itself failed, which is
    /// <see cref="RetreatRefusedDialog"/> — "you tried and did not get away".</para>
    ///
    /// <para><b>Reading the swap as cosmetic inverts this.</b> Without it the comparison looks like
    /// living-enemies against enemy-slots, which would make the dialog depend on enemy casualties
    /// and put the wrong line on screen in exactly the case the player notices — having just lost
    /// someone.</para>
    /// </remarks>
    public static int RetreatRefusalDialog(bool anyPartyMemberDown) =>
        anyPartyMemberDown ? RetreatRefusedMismatchDialog : RetreatRefusedDialog;

    /// <summary><b>A failed retreat still costs the turn.</b></summary>
    public static bool FailedRetreatSpendsTheTurn => true;

    /// <summary>Which screen the suspend button opens.</summary>
    public enum SuspendScreen {
        /// <summary>The acting character's pack — what an unmodified press gives.</summary>
        Inventory,

        /// <summary>The character sheet, reached only with a modifier held.</summary>
        CharacterSheet,
    }

    /// <summary>
    /// <b>Id 22 opens the INVENTORY, not the character sheet — the sheet needs Shift.</b>
    /// </summary>
    /// <param name="modifierHeld">
    /// Whether a shift key is down (the original tests both 0x2a and 0x36), or the menu is already
    /// in its state-2 mode.
    /// </param>
    /// <remarks>
    /// <b>canassa calls this routine <c>combat_arena_suspend_char_screen</c>, and the name describes
    /// the branch it does NOT usually take.</b> The body is an if/else: with a modifier held it runs
    /// <c>charscreen_info_loop</c>, and otherwise — the ordinary press — it runs
    /// <c>cmbinv_inventory_screen_run</c>. A port that trusted the name would put the wrong screen on
    /// the most common path in combat, and the right one behind a modifier nobody would think to try.
    ///
    /// <para><b>The party is copied OUT to the save's character records before the screen and back
    /// afterwards</b>, with each combatant's <c>inner</c> pointer preserved across the round trip.
    /// The screens read the save, not the fight, so without that copy a character would show its
    /// pre-battle health while standing there wounded. Any port that opens a real screen mid-combat
    /// has to reconcile the same two representations — see <c>CombatRuntime.ResolveMelee</c>, which
    /// writes damage through to the save for the same reason.</para>
    /// </remarks>
    public static SuspendScreen SuspendScreenFor(bool modifierHeld) =>
        modifierHeld ? SuspendScreen.CharacterSheet : SuspendScreen.Inventory;
}
