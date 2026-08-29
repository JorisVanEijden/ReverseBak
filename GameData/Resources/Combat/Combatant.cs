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

    /// <summary>Knocked back — <c>CAF_KNOCKBACK</c>. Here for the bit; nothing reads it yet.</summary>
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
