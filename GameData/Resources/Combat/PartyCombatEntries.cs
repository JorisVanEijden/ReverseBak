namespace GameData.Resources.Combat;

using System;
using System.Collections.Generic;
using GameData.Resources.Data;

/// <summary>
/// Where each party slot starts a fight — the contents of <c>P1.DAT</c>.
///
/// <para><c>combat_actor_init_pool</c> (canassa CACTOR.C ~62) builds the party side of an encounter
/// and then reads each member's <c>CombatantState</c> straight out of this file, seeking to
/// <c>(charSlot - 1) * sizeof(CombatantState)</c>. So a party member's combat entry tile is shipped
/// data, not something the arena computes.</para>
///
/// <para><b>The entry tiles REPEAT across slots</b> — in the shipped file (1,1), (6,2) and (4,0) each
/// appear twice. That is not a defect: the party collides with itself on entry and
/// <c>combat_actor_place_on_free_tile</c> resolves it, which is why placement has to be a pass
/// rather than a copy. A port that trusts these as final positions stacks party members.</para>
/// </summary>
public sealed class PartyCombatEntries : IResource {
    /// <summary>Slots the file ships.</summary>
    public const int SlotCount = 6;

    public PartyCombatEntries(string id, IReadOnlyList<SaveGameCombatData> slots) {
        Id = id;
        Slots = slots ?? Array.Empty<SaveGameCombatData>();
    }

    public string Id { get; }

    public ResourceType Type => ResourceType.DAT;

    /// <summary>The records, in file order — index 0 is <c>charSlot</c> 1.</summary>
    public IReadOnlyList<SaveGameCombatData> Slots { get; }

    /// <summary>
    /// The entry record for a character slot, or null when the file has none.
    /// </summary>
    /// <remarks>
    /// <b><paramref name="charSlot"/> is 1-based</b>, as the original's seek makes plain
    /// (<c>charSlot - 1</c>). Slot 0 means "not a party member" elsewhere in this codebase, so it has
    /// no entry here either.
    /// </remarks>
    public SaveGameCombatData EntryFor(int charSlot) {
        int index = charSlot - 1;
        return index >= 0 && index < Slots.Count ? Slots[index] : null;
    }
}
