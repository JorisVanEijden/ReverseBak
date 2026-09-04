namespace GameData.Resources.Combat;

using System;

/// <summary>State flags a combatant carries. Values are the original's <c>CAF_*</c> bits.</summary>
[Flags]
public enum CombatantFlags {
    /// <summary>Nothing set.</summary>
    None = 0,

    /// <summary>Has not yet acted this round. Cleared when the actor takes its turn and set again
    /// by the round reset.</summary>
    /// <remarks>
    /// <b>0x01, verified from the encodings.</b> The round reset ORs 0x01 into every combatant
    /// (<c>80 4f 08 01</c>), the turn advance ANDs it out of whoever just acted, and Defend clears
    /// it with <c>80 67 08 fe</c>. This was previously modelled as 0x04 — the bit the round reset
    /// <i>clears</i> — which was harmless only because nothing yet reads a raw status byte into this
    /// enum.
    /// </remarks>
    Ready = 0x01,

    /// <summary>Down. A dead combatant is skipped by turn order and by targeting.</summary>
    Dead = 0x02,

    /// <summary>
    /// The Defend command was ordered this round — <c>CAF_DEFEND_CMD</c>. <b>This is the bit the
    /// round reset clears</b> (<c>flags &amp;= ~CAF_DEFEND_CMD</c>), which is what makes Defend last
    /// exactly one round.
    /// </summary>
    /// <remarks>
    /// Was <c>ClearedEachRound</c>, with "what it means is not established". It is established now:
    /// every value here comes from <c>INCLUDE/defines.h</c>.
    /// </remarks>
    DefendCommand = 0x04,

    /// <summary>Parrying — <c>CAF_PARRY</c>. Raises an attacker's roll by 20, and is cleared the
    /// moment this combatant is picked to act again, so it lasts exactly one round.</summary>
    /// <remarks>0x08, verified: <c>combat_defend</c> @0x64201 sets exactly this bit.</remarks>
    Parry = 0x08,

    /// <summary>Routed: heading for the edge of the field to leave the battle — <c>CAF_FLEE</c>.</summary>
    /// <remarks>
    /// <b>0x10, and this was modelled as 0x20 until 2026-08-22.</b> 0x10 held a speculative
    /// <c>Defending</c> and 0x20 is really <c>CAF_POISON</c>. Not cosmetic:
    /// <see cref="CombatEncounter.BeginRound"/> cleared <c>Defending</c> — i.e. this bit — so a
    /// monster that had decided to rout would have had the decision wiped at the next round
    /// boundary and gone back to fighting. The round reset clears
    /// <see cref="DefendCommand"/> and nothing else.
    /// </remarks>
    Fleeing = 0x10,

    /// <summary>Poisoned — <c>CAF_POISON</c>. Read by <see cref="PoisonTick"/> at the end of the
    /// bearer's own turn.</summary>
    Poisoned = 0x20,

    /// <summary>
    /// Knocked back — <c>CAF_KNOCKBACK</c>, which is also the <b>hit-reaction</b> state.
    /// </summary>
    /// <remarks>
    /// No longer "here for the bit": <c>markActorHit</c> @0x6157d sets it together with a 2-tick
    /// timer and a direction, and <c>tickHitReactionTimers</c> @0x61598 clears it when the timer
    /// runs out. See <see cref="HitReaction"/> for the rule and for why IDA's comments naming this
    /// bit <c>0x64</c> are wrong.
    /// </remarks>
    Knockback = 0x40,

    /// <summary>Summoned by the AI — <c>CAF_AI_SUMMON</c>. Here for the bit; nothing reads it yet.</summary>
    AiSummon = 0x80,
}

/// <summary>
/// One participant in a tactical encounter, as the arena sees it.
///
/// <para>Deliberately the arena's own small view rather than a party member's full record: the turn
/// loop needs a position, a speed, and enough flags to know whether this one may act. A party
/// combatant carries <see cref="PartySlot"/> so the loop can tell the two sides apart, which the
/// original does through <c>cParty_slot</c>.</para>
/// </summary>
public sealed class Combatant {
    /// <summary>Creature class id. 0x36 is immune to damage and always gets a turn.</summary>
    public int ClassId { get; set; }

    /// <summary>Party slot, 1-based; 0 for anything not in the party.</summary>
    public int PartySlot { get; set; }

    /// <summary>Whether this combatant is on the party's side.</summary>
    public bool IsPartyMember => PartySlot != 0;

    /// <summary>Grid position.</summary>
    public int X { get; set; }

    /// <inheritdoc cref="X"/>
    public int Y { get; set; }

    /// <summary>Current health; 0 is dead.</summary>
    public int Health { get; set; }

    /// <summary>Current stamina — the buffer damage eats before health.</summary>
    public int Stamina { get; set; }

    /// <summary>Speed (stat 2), read live: turn order is recomputed from it before every turn, so
    /// anything that changes it reorders the fight.</summary>
    public int Speed { get; set; }

    /// <summary>State flags.</summary>
    public CombatantFlags Flags { get; set; } = CombatantFlags.Ready;

    /// <summary>Redraws left in this combatant's hit reaction — <c>hitReactionTimer</c>.</summary>
    /// <remarks>
    /// Set with <see cref="CombatantFlags.Knockback"/> by <see cref="HitReaction.Begin"/> and counted
    /// down by <see cref="HitReaction.Tick"/> once per combat-view redraw, which is what the original
    /// does and is not a unit of time — see that class for why "two frames" and "a fixed duration"
    /// are both the wrong port.
    /// </remarks>
    public int HitReactionTimer { get; set; }

    /// <summary>Which remap table the recoil redraws this combatant through — <c>hitReactionDir</c>.</summary>
    /// <remarks>
    /// <b>Not a direction, despite the original's parameter name.</b> It selects a 256-byte remap
    /// table, <c>(value &lt;&lt; 8) + 0xA66</c> into the zone's RMP — so the flash is the creature
    /// briefly redrawn one rung further along its zone's own fade ramp. An ordinary blow passes
    /// <see cref="HitReaction.BlowRemap"/>; a self-billed cost passes 0 and does not flinch at all.
    /// </remarks>
    public int HitReactionRemap { get; set; }

    /// <summary>Whether this combatant has a swing to play at the next redraw.</summary>
    /// <remarks>
    /// <b>Set when the blow resolves, played when the arena is next drawn.</b> The model resolves an
    /// attack in one call and the renderer catches up on the following redraw, so the flag is what
    /// carries the event across that gap — the same shape <see cref="DeathShown"/> uses for the
    /// collapse.
    /// </remarks>
    public bool SwingPending { get; set; }

    /// <summary>The <see cref="CombatEffectSprite"/> id flying from this combatant to
    /// <see cref="FlightToSlot"/> on the next redraw, or 0 for none.</summary>
    /// <remarks>
    /// Carries a shot or a cast across the resolve-then-redraw gap exactly as
    /// <see cref="SwingPending"/> carries a melee swing: the attack resolves in one call and the
    /// arena catches up when it is next drawn.
    /// </remarks>
    public int FlightEffectId { get; set; }

    /// <summary>Roster slot of the combatant the pending flight is aimed at.</summary>
    public int FlightToSlot { get; set; }

    /// <summary>Whether <see cref="FlightToSlot"/> names a party member — the slot number alone is
    /// ambiguous, since a party slot and an enemy index share the same range.</summary>
    public bool FlightToParty { get; set; }

    /// <summary>Ticks counted toward this combatant's next idle frame — <c>tickCounter</c>.</summary>
    /// <remarks>
    /// Driven by <see cref="CreatureAnimationStep.Advances"/>, which is a MODULO rather than a
    /// countdown and resets to 1 rather than 0 — see that method for why the two differ.
    /// </remarks>
    public int AnimTick { get; set; }

    /// <summary>Ticks between this combatant's idle frames — re-rolled after every advance.</summary>
    /// <remarks>
    /// <b>Slot 0 re-rolls to 8..15 every time it advances</b>, so the idle is deliberately
    /// irregular; a fixed delay produces a metronome the original never has. The initial value the
    /// original passes is 7, which is the only time it is not a roll.
    /// </remarks>
    public int AnimDelay { get; set; } = 7;

    /// <summary>Whether this combatant's death collapse has already been shown.</summary>
    /// <remarks>
    /// <b>On the combatant because the sprite does not survive.</b> A corpse is rebuilt on every
    /// combat redraw, so a flag on the GameObject would replay the collapse each time — the body
    /// would keep falling over. Same reasoning as <see cref="GaitFrame"/>.
    /// </remarks>
    public bool DeathShown { get; set; }

    /// <summary>Which frame of the walk cycle this combatant is currently showing, 0..2.</summary>
    /// <remarks>
    /// <b>It lives on the COMBATANT because the sprite does not survive.</b> Every combat redraw
    /// destroys the arena's GameObjects and builds them again, so a gait kept on the renderer
    /// restarts at frame 0 on every step — measured as a visible snap back at the end of each
    /// slide. The original puts it in the same place for the same reason: <c>creatueBitmapAnim</c>
    /// is a field of <c>p7times17bytes[actorIndex]</c>, a per-ACTOR array that outlives any drawing.
    ///
    /// <para>Two fields, not one: the cycle is a ping-pong (0, 1, 2, 1, 0 …), so frame 1 is
    /// ambiguous about where it is going and the direction has to be carried with it. See
    /// <see cref="World.EncounterActorPose.Advance"/>.</para>
    /// </remarks>
    public int GaitFrame { get; set; }

    /// <summary>Whether the walk cycle is currently running up rather than back down.</summary>
    /// <inheritdoc cref="GaitFrame"/>
    public bool GaitAdvancing { get; set; } = true;

    /// <summary>
    /// Which way this combatant is drawn facing, as an octant 0..7 measured <b>relative to the
    /// camera</b>.
    /// </summary>
    /// <remarks>
    /// <b>The original stores this; it does not derive it.</b>
    /// <c>combat_actor_deploy_encounter</c> @0x5C845 builds the sprite's world rotation as
    /// <code>
    /// worldRotation = (facingDirection &lt;&lt; 13)          // octant, 45 degrees a step
    ///               + (camera.rotation3d.z &amp; 0xE000)   // camera yaw snapped to an octant
    ///               + 0x8000;                           // half a turn
    /// </code>
    /// where <c>facingDirection</c> lives per creature in <c>creatueBitmapAnim</c> @+0x4. It is
    /// <b>not</b> on <c>combatData</c>, which carries only <c>hitReactionDir</c> — the recoil visual.
    ///
    /// <para><b>0 means facing the viewer</b>, because of that half turn. That is why a freshly
    /// deployed party stands frontally in the original on an unopposed puzzle: nobody has turned
    /// toward anything yet, so everyone is still on 0. Before this existed the port passed the
    /// party's travel heading for every combatant, and since the arena camera looks along that same
    /// heading they all resolved to one octant — the whole party in an identical profile (TASK-324).
    /// </para>
    ///
    /// <para><b>Turning is a side effect of choosing an animation</b> in the original:
    /// <c>startCreatureBitmapAnimation</c> @0x5EC23 is what writes it, so a combatant faces where
    /// its current pose points rather than being turned by a separate step. Nothing here updates it
    /// on a move or a swing yet — that is the rest of TASK-324, and until it lands every combatant
    /// simply keeps the deployed 0.</para>
    /// </remarks>
    public int FacingOctant { get; set; }

    /// <summary>Whoever this combatant is currently fighting.</summary>
    public Combatant Target { get; set; }

    /// <summary>
    /// The tile a routed monster is running for — the original's
    /// <c>combatData.target_x_on_grid_</c>/<c>target_y_on_grid_</c>, as written by
    /// <c>combatenc_pick_flee_destination</c> (@0x63ea1).
    /// </summary>
    /// <remarks>
    /// <b>Chosen once, when the rout starts, and then walked toward on every later turn.</b>
    /// <c>combatenc_flee_walk_and_exit_field</c> (@0x64175) takes one walk step per turn and
    /// compares the actor's tile against this one; re-rolling it each turn would make a routed
    /// monster wander instead of leave, because the scan is deliberately noisy
    /// (<see cref="MonsterFleeDestination.AcceptsImprovement"/>).
    ///
    /// <para>Null means no destination has been chosen — either the monster is not routing, or the
    /// scan accepted nothing. <b>Here that means it stands still; in the original it does not.</b>
    /// The original writes into a field the other AI routines also use as their walk target, so a
    /// refused scan leaves the PREVIOUS destination in place and the monster walks to that instead.
    /// We have no equivalent stale value — target selection carries a
    /// <see cref="Combatant"/> and walks to its tile directly, never storing a tile — so there is
    /// nothing to fall back to. The divergence is confined to the scan-accepted-nothing case.</para>
    /// </remarks>
    public (int X, int Y)? FleeDestination { get; set; }

    /// <summary>
    /// Blocked by a lingering spell effect — <c>CanActInCombat</c> @0x63fa2.
    /// </summary>
    /// <remarks>
    /// <b>Derived, not set by hand.</b> Exactly three spells incapacitate — Dannon's Delusions,
    /// Despair Thy Eyes and Grief of 1000 Nights — and <c>ActiveSpellEffectPool</c> recomputes this
    /// whenever the actor's chain changes. The original stores no such flag at all; it walks the
    /// chain on every question. Setting it directly is still possible and is what <c>Kill</c> does,
    /// but anything else that writes it is working around the pool rather than with it.
    ///
    /// <para>The two combat-status bits the original also tests belong to the caller's strict flag,
    /// not here — see <see cref="CanAct"/>.</para>
    /// </remarks>
    public bool Incapacitated { get; set; }

    /// <summary>
    /// Head of this combatant's lingering spell-effect chain — the original's
    /// <c>combatData.activeSpellEffectSlot_</c>. An index into the encounter's
    /// <c>ActiveSpellEffectPool</c>, or -1 for none.
    /// </summary>
    public int ActiveEffectSlot { get; set; } = -1;

    /// <summary>
    /// Ticks until this combatant gets back up, or <see cref="SlayerRevival.NoCountdown"/>.
    /// </summary>
    /// <remarks>
    /// <b>A FIELD OF ITS OWN, WHERE THE ORIGINAL OVERLOADS ONE.</b> The engine keeps this in
    /// <c>dmgFloatFrames</c> — the same byte that counts down a floating damage number — which is
    /// free on a corpse because nothing is showing a number over it. That is an implementation
    /// economy, not a rule: porting the overload would tie the revival clock to a presentation
    /// counter and break the first time something floats a number over a dead Black Slayer.
    ///
    /// <para>Only the two eligible species ever carry one — see
    /// <see cref="SlayerRevival.IsCandidate"/>.</para>
    /// </remarks>
    public int RevivalCountdown { get; set; } = SlayerRevival.NoCountdown;

    /// <summary>True when this combatant is dead.</summary>
    public bool IsDead => (Flags & CombatantFlags.Dead) != 0;

    /// <summary>
    /// Whether this combatant may act. <paramref name="strict"/> is the original's second argument:
    /// the turn picker passes 1 and so also requires the ready flag, while a defence lookup passes 0
    /// and only cares about the incapacitating effects.
    /// </summary>
    public bool CanAct(bool strict) {
        if (strict && ((Flags & CombatantFlags.Ready) == 0 || IsDead)) {
            return false;
        }
        return !Incapacitated;
    }
}
