namespace GameData.Resources.Combat;

using GameData.Resources.Spells;

/// <summary>
/// What a click on the field does while <see cref="CombatCommandOutcome.PendingMode.TargetSelection"/>
/// is armed — <c>combat_arena_disp_spell_action</c> (ovr168 @0x62360) and <c>case 4</c> of
/// <c>combat_arena_resolve_menu_action</c> (ovr168 @0x626ca).
///
/// <para><b>Three tasks each deferred this to the other two, so nobody built it.</b> Shoot and Cast
/// both arm the same pending mode and <see cref="CombatCommandOutcome"/> has returned it since
/// 2026-08-22 with nothing reading it. <see cref="SpellTargetingRules"/> models the spell half of the
/// validity check; the shoot half and the shared resolution are here.</para>
/// </summary>
/// <remarks>
/// <b>WHICH HALF RUNS IS DECIDED BY THE ACTOR'S CAPABILITY, NOT BY THE BUTTON THAT WAS PRESSED.</b>
/// The original re-asks <c>combatenc_show_missile_stat_row</c> — our
/// <see cref="CombatCapability.CanShoot"/> — at click time and resolves a SHOT whenever it answers
/// yes, feeding the selected spell record to the cast only when it answers no. It gets away with that
/// because the two commands share one HUD cell and the overlap is empty
/// (<see cref="CombatMenuSlots.CapabilitySlot"/>): a caster carries no crossbow, a non-caster has no
/// Casting skill. A port that instead remembered "the player pressed Cast" would diverge the moment
/// that assumption stopped holding — and, more usefully, would hide the fact that the capability is
/// re-evaluated at all. It is: an enemy who steps adjacent between the button and the click takes the
/// shot away.
/// </remarks>
public static class CombatTargetSelection {
    /// <summary>What the click resolved to.</summary>
    public enum Resolution {
        /// <summary>
        /// Nothing happened and the mode <b>stays armed</b> — the click missed, or was a cancel.
        /// </summary>
        Pending,

        /// <summary>Loose the selected quarrel at the target.</summary>
        Shoot,

        /// <summary>Cast the selected spell at the actor under the cursor.</summary>
        CastAtTarget,

        /// <summary>Cast the selected spell at the cell, with <b>no target actor at all</b>.</summary>
        CastAtGround,

        /// <summary>
        /// The click was legal but nothing could be aimed there, so the arena drops back to plain
        /// movement with the pending action abandoned.
        /// </summary>
        RevertToMove,
    }

    /// <summary>
    /// Whether the click even reaches the aiming rules.
    /// </summary>
    /// <param name="confirmed">A confirm press rather than a cancel or no press at all.</param>
    /// <param name="cursorDistance">
    /// The cursor's grid distance, or <see cref="SpellTargetingRules.OffGridDistance"/> when it is
    /// not over a cell.
    /// </param>
    /// <param name="cursorY">Screen Y of the cursor.</param>
    /// <remarks>
    /// The same two rejections <see cref="SpellTargetingRules.ClickCommitsTheCast"/> records, plus
    /// the confirm itself — shared verbatim by the shoot arm, which is why it is stated once here
    /// rather than twice.
    /// </remarks>
    public static bool ClickReachesTheField(bool confirmed, int cursorDistance, int cursorY) =>
        confirmed && SpellTargetingRules.ClickCommitsTheCast(cursorY, cursorDistance);

    /// <summary>
    /// Whether the cell under the cursor is a legal <b>shot</b>.
    /// </summary>
    /// <param name="targetIsEncounterActor">
    /// Something is standing there and it belongs to the encounter — you cannot shoot your own party.
    /// </param>
    /// <param name="targetIsDead">The occupant has already been put out of the fight.</param>
    /// <param name="targetIsInLineOfFire">
    /// See the remarks: this is <see cref="CombatLineOfFire.IsClear"/>, arriving by a very indirect
    /// route.
    /// </param>
    /// <param name="hasSelectedQuarrel">
    /// The actor still carries one of the chosen kind — <c>combataiturn_sel_consum_qrl(actor, kind, 0)</c>
    /// answering something other than -1, which is <see cref="QuarrelInventory"/>'s count being
    /// non-zero. Asked <b>without</b> consuming: the shot is not taken yet.
    /// </param>
    /// <remarks>
    /// <b>The mystery flag is line of fire.</b> The original tests
    /// <c>combatgrid_tile_has_terr_bit2</c>, which reads bit 1 of a per-tile byte map — and that map
    /// is rebuilt for the acting character every turn by <c>combatgrid_build_move_attack_map</c>
    /// (CMBTGRID.C:1420), which sets bit 0 when the tile can be WALKED to and bit 1 when
    /// <c>combat_actor_trace_proj_path</c> reaches whoever is standing on it. So the shoot check is
    /// a line-of-fire check wearing a terrain-flag disguise, and this is the first consumer
    /// <see cref="CombatLineOfFire"/> has on the player's side of the fight — until now only the
    /// monster AI asked it.
    ///
    /// <para><b>Bit 1 is only ever set on an occupied tile</b>, because the trace is skipped when the
    /// tile holds nobody. Empty ground therefore fails the shoot check on this flag alone, before the
    /// encounter-actor test is reached.</para>
    ///
    /// <para><b>The dead are nulled first, not rejected later.</b> The original clears its local
    /// target pointer when the occupant is dead and then runs the same three tests against null — so
    /// a corpse reads exactly like empty ground rather than like an invalid target. The distinction
    /// matters one line later: <see cref="Resolve"/> asks whether there is a target at all, and a
    /// corpse must answer no.</para>
    /// </remarks>
    public static bool ShotIsValid(bool targetIsEncounterActor, bool targetIsDead,
        bool targetIsInLineOfFire, bool hasSelectedQuarrel) =>
        !targetIsDead && targetIsInLineOfFire && targetIsEncounterActor && hasSelectedQuarrel;

    /// <summary>
    /// Resolve a confirmed click on the field.
    /// </summary>
    /// <param name="actorCanShoot">
    /// <see cref="CombatCapability.CanShoot"/> for the acting character, <b>re-asked now</b>. See the
    /// type's remarks: this, not the button pressed, picks the arm.
    /// </param>
    /// <param name="hasTarget">
    /// A valid target was found under the cursor — <see cref="ShotIsValid"/> for the shoot arm, the
    /// <see cref="SpellTargetingRules.AimOf"/> rules for the cast arm.
    /// </param>
    /// <param name="spellTargetingType">
    /// The selected spell's targeting type. <b>Read even on the shoot arm</b> — see the remarks.
    /// </param>
    /// <remarks>
    /// <b>Targeting type 6 is tested WITHOUT the capability guard and its neighbour 5 is not.</b> The
    /// original's condition is <c>(canShoot == 0 &amp;&amp; kind == 5) || kind == 6</c>: the second
    /// arm is missing the <c>canShoot == 0</c> the first one has. Both types aim at ground
    /// (<see cref="SpellTargetingRules.Aim.ClearGround"/>), so there is no rule that separates them
    /// and the asymmetry is almost certainly a slip in the original. It is ported as written, because
    /// it is reachable: a character who can shoot, clicking empty ground while the stale spell record
    /// happens to hold type 6, casts instead of reverting to movement.
    ///
    /// <para><b>Nothing here is a cancel.</b> Every rejected click returns <see cref="Resolution.Pending"/>
    /// and leaves the mode armed, the same way <see cref="InspectAction.Result.Ignored"/> does. The
    /// only way out of target selection without acting is the menu's own Back
    /// (<see cref="CombatCommands.BacksOutOfShootMenu"/>) or <see cref="Resolution.RevertToMove"/>.</para>
    /// </remarks>
    public static Resolution Resolve(bool confirmed, int cursorDistance, int cursorY,
        bool actorCanShoot, bool hasTarget, int spellTargetingType) {
        if (!ClickReachesTheField(confirmed, cursorDistance, cursorY)) {
            return Resolution.Pending;
        }

        // A crystal-aimed spell commits on an EMPTY cell too, passing the null it found — the one
        // targeting type that reaches the cast with no actor by the same branch an actor would.
        if (hasTarget || (!actorCanShoot && spellTargetingType == CrystalTargetingType)) {
            return actorCanShoot ? Resolution.Shoot : Resolution.CastAtTarget;
        }

        if ((!actorCanShoot && spellTargetingType == GroundTargetingType)
            || spellTargetingType == SummonTargetingType) {
            return Resolution.CastAtGround;
        }

        return Resolution.RevertToMove;
    }

    /// <summary>Targeting type 8 — the crystal-aimed spells, which commit on an empty cell.</summary>
    /// <remarks>Named against <see cref="SpellTargetingRules.Aim.Crystal"/>.</remarks>
    public const int CrystalTargetingType = 8;

    /// <summary>Targeting type 5, the guarded half of the ground-aimed pair.</summary>
    public const int GroundTargetingType = 5;

    /// <summary>Targeting type 6, the unguarded half — see <see cref="Resolve"/>.</summary>
    public const int SummonTargetingType = 6;

    /// <summary>
    /// Whether a resolution spends the acting character's turn.
    /// </summary>
    /// <remarks>
    /// All three acting outcomes clear the ready bit, and
    /// <see cref="Resolution.RevertToMove"/> deliberately does not — it hands the character back a
    /// full turn of movement, which is what makes a misaimed click recoverable rather than wasted.
    /// </remarks>
    public static bool SpendsTheTurn(Resolution resolution) =>
        resolution == Resolution.Shoot
        || resolution == Resolution.CastAtTarget
        || resolution == Resolution.CastAtGround;

    /// <summary>
    /// <b>Hovering assigns the actor's remembered target; clicking does not.</b>
    /// </summary>
    /// <remarks>
    /// Case 4 writes <c>currentActor.target = cursorTarget</c> on every pass through the loop, before
    /// any confirm is tested — so simply moving the cursor over an enemy is what sets the field the
    /// facing and the follow-up animations read. And it is written only when the cursor is over
    /// somebody: sliding off an enemy onto empty ground leaves the last one remembered rather than
    /// clearing it. A port that assigned the target on the click instead would face the wrong way
    /// for a frame and would clear a target the original keeps.
    /// </remarks>
    public static bool HoverAssignsTheTarget => true;
}
