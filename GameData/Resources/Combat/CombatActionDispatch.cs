namespace GameData.Resources.Combat;

/// <summary>
/// What the player's click does on the combat field — the action arms of
/// <c>combat_arena_resolve_menu_action</c> (ovr168 @0x626ca).
///
/// <para>Distinct from <see cref="CombatAi"/>, which decides a monster's turn, and from
/// <see cref="CombatFormulas"/>, which resolves whatever is chosen. This is the layer between them:
/// which action a click selects, and the gates it has to pass first.</para>
/// </summary>
public static class CombatActionDispatch {
    /// <summary>The melee attacks, which are on different mouse buttons.</summary>
    public enum MeleeAttack {
        /// <summary>Neither — the click did not select an attack.</summary>
        None,

        /// <summary>Left button.</summary>
        Thrust,

        /// <summary>Right button.</summary>
        Swing,
    }

    /// <summary>Mouse button code for a thrust.</summary>
    public const int LeftButton = 1;

    /// <summary>Mouse button code for a swing.</summary>
    public const int RightButton = 2;

    /// <summary>
    /// Which melee attack a click on an enemy selects.
    /// </summary>
    /// <remarks>
    /// <b>The two melee attacks are on the two mouse buttons</b> — left thrusts, right swings — and
    /// nothing on screen says so. A port offering a single "attack" verb loses half the melee
    /// system, and the two are not interchangeable: they have different reach rules, different
    /// stamina requirements and different weapon wear (see
    /// <see cref="CombatFormulas.WeaponWearOnSwing"/> against
    /// <see cref="CombatFormulas.WeaponWearOnThrust"/>).
    /// </remarks>
    public static MeleeAttack AttackFor(int mouseButton) {
        switch (mouseButton) {
            case LeftButton: return MeleeAttack.Thrust;
            case RightButton: return MeleeAttack.Swing;
            default: return MeleeAttack.None;
        }
    }

    /// <summary>
    /// <b>A thrust closes the distance; a swing does not.</b>
    /// </summary>
    /// <remarks>
    /// The thrust arm calls the melee-approach routine first and only attacks if it succeeds, so a
    /// left click on a distant enemy walks the attacker into contact and then strikes. The swing arm
    /// runs a reach test instead and never moves anybody.
    ///
    /// <para>So the same click on the same enemy either moves you or refuses, depending on which
    /// button you pressed. Implementing both as "attack if adjacent" removes the game's only
    /// click-to-engage.</para>
    /// </remarks>
    public static bool ApproachesTarget(MeleeAttack attack) => attack == MeleeAttack.Thrust;

    /// <summary>
    /// The combined pool a <b>swing</b> requires, above which it may be made.
    /// </summary>
    /// <remarks>
    /// Strictly greater than one. The thrust has no such test, so an exhausted character can still
    /// thrust but not swing — the heavier attack is the one that runs out first.
    /// </remarks>
    public const int SwingMinimumPool = 1;

    /// <summary>Whether the attacker has the reserves for this attack.</summary>
    public static bool HasReservesFor(MeleeAttack attack, int healthStaminaPool) =>
        attack != MeleeAttack.Swing || healthStaminaPool > SwingMinimumPool;

    /// <summary>
    /// <b>An attack is refused beyond the mover's remaining allowance.</b>
    /// </summary>
    /// <remarks>
    /// Both melee arms sit behind a test of the cursor distance against the same movement-allowance
    /// value the move action uses, so reach and movement are the same budget: you cannot strike
    /// something you could not have walked to.
    /// </remarks>
    public static bool WithinReach(int cursorDistance, int movementAllowance) =>
        cursorDistance <= movementAllowance;

    /// <summary>
    /// <b>A thrust is abandoned if the approach leaves the attacker unable to act.</b>
    /// </summary>
    /// <remarks>
    /// After the approach the arm re-tests the attacker's own cannot-act bit and gives up if it is
    /// now set — walking into a hazard on the way in costs the attack. The swing has no equivalent
    /// because it never moves.
    /// </remarks>
    public static bool ThrustSurvivesTheApproach(bool attackerIncapacitatedAfterApproach) =>
        !attackerIncapacitatedAfterApproach;

    /// <summary>What the defend menu action actually does.</summary>
    public enum GuardAction {
        /// <summary>Raise a guard for the round.</summary>
        Defend,

        /// <summary>Recover instead.</summary>
        Rest,
    }

    /// <summary>The pool percentage at or above which the action guards rather than rests.</summary>
    public const int DefendThresholdPercent = 0x50;

    /// <summary>
    /// <b>One menu action, two behaviours, chosen by how hurt you are.</b>
    /// </summary>
    /// <param name="statPercent">The combatant's pool as a percentage.</param>
    /// <remarks>
    /// At or above four fifths it defends; below that it rests instead. The player presses the same
    /// button either way and is not told which they got — so a port with separate Defend and Rest
    /// commands is offering a choice the original never gave, and one with only Defend silently
    /// removes the recovery a hurt character depends on.
    /// </remarks>
    public static GuardAction GuardFor(int statPercent) =>
        statPercent >= DefendThresholdPercent ? GuardAction.Defend : GuardAction.Rest;

    /// <summary>
    /// <b>Clicking a party member switches who is acting.</b>
    /// </summary>
    /// <remarks>
    /// A separate action kind from anything on the field: it takes the actor under the cursor,
    /// checks it is in the roster, clears the current actor's ready bit and hands the turn over. So
    /// the party's turn order is not fixed once a round starts — the player can pass control around,
    /// and doing so spends the previous actor's readiness.
    /// </remarks>
    public static bool SwitchingActorSpendsTheCurrentTurn => true;

    /// <summary>
    /// A click is only accepted inside the field, below the menu bar.
    /// </summary>
    /// <remarks>
    /// The same screen-Y test the cast action uses, so the rule is shared across every action rather
    /// than being a property of casting.
    /// </remarks>
    public static bool ClickIsOnTheField(int mouseY) => mouseY < FieldBottomY;

    /// <summary>Screen Y at which the combat field gives way to the menu bar.</summary>
    public const int FieldBottomY = 0x8C;

    // ---------------------------------------------------------------- round and turn transitions
    // combatenc_begin_round_reset_flags @0x640ff, combat_arena_advance_turn @0x6141c.

    /// <summary>
    /// What a new round does to every combatant.
    /// </summary>
    /// <remarks>
    /// Three things per actor, and only three: <see cref="CombatantFlags.Ready"/> is set,
    /// <see cref="CombatantFlags.ClearedEachRound"/> is cleared, and a target that has been put out
    /// of the fight is dropped.
    ///
    /// <para><b>Parry is not touched here.</b> It is cleared when a combatant is next picked to act,
    /// which is what makes Defend last exactly one round rather than until the next round boundary —
    /// a distinction that matters for anyone acting late in the round after the defender.</para>
    /// </remarks>
    public static CombatantFlags BeginRound(CombatantFlags flags) =>
        (flags | CombatantFlags.Ready) & ~CombatantFlags.ClearedEachRound;

    /// <summary>
    /// <b>A stale target is dropped at the round boundary, not when it falls.</b>
    /// </summary>
    /// <param name="targetCanStillAct">The current target is not out of the fight.</param>
    /// <remarks>
    /// The reset clears any target whose cannot-act bit is set. Within a round an actor can still be
    /// pointed at somebody who has just gone down — the engine only tidies up between rounds, which
    /// is visible if anything reads the target during the round it happens.
    /// </remarks>
    public static bool KeepsTargetIntoNextRound(bool targetCanStillAct) => targetCanStillAct;

    /// <summary>
    /// <b>Ending a turn faces the actor before spending it.</b>
    /// </summary>
    /// <remarks>
    /// The turn advance turns the outgoing actor toward its target (or the nearest enemy if it has
    /// none) and only then clears its ready flag. So a combatant's final facing is a property of the
    /// turn it just took, not of whatever happens next.
    /// </remarks>
    public static bool TurnEndFacesBeforeSpending => true;

    /// <summary>
    /// <b>The turn loop skips over anyone who cannot act, rather than ending the round.</b>
    /// </summary>
    /// <remarks>
    /// After picking the next actor the advance re-tests its cannot-act bit and, if set, picks again
    /// — a loop, not a single retry. So a round in which several combatants are incapacitated
    /// advances straight past all of them to the first one that can move.
    /// </remarks>
    public static bool AdvanceSkipsIncapacitatedActors => true;
}
