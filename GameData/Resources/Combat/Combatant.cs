namespace GameData.Resources.Combat;

using System;

/// <summary>State flags a combatant carries. Values are the original's <c>CAF_*</c> bits.</summary>
[Flags]
public enum CombatantFlags {
    /// <summary>Nothing set.</summary>
    None = 0,

    /// <summary>Down. A dead combatant is skipped by turn order and by targeting.</summary>
    Dead = 0x02,

    /// <summary>Has not yet acted this round. Cleared when the actor takes its turn and set again
    /// by the round reset.</summary>
    Ready = 0x04,

    /// <summary>Parrying — the Defend command. Raises an attacker's roll by 20, and is cleared the
    /// moment this combatant is picked to act again, so it lasts exactly one round.</summary>
    Parry = 0x08,

    /// <summary>Defend was ordered this round (the regen half of Defend, distinct from
    /// <see cref="Parry"/>).</summary>
    Defending = 0x10,

    /// <summary>Routed: heading for the edge of the field to leave the battle.</summary>
    Fleeing = 0x20,
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
    /// Blocked by a status effect (asleep, stunned, and the rest of the set
    /// <c>combatenc_actor_can_act</c> checks). Held as a flag here because the effect pool is not
    /// modelled yet; the arena only needs the verdict.
    /// </summary>
    public bool Incapacitated { get; set; }

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
