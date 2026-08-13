namespace GameData.Resources.Data;

using GameData.Resources.Character;

/// <summary>
/// One party member's live attributes and afflictions, to be written back over their saved record
/// by the save writer. Either half may be null to leave that part of the record as the backing body
/// had it.
///
/// <para>Lives here rather than beside the writer for the same reason
/// <see cref="DirtyContainerEdit"/> does: the game side builds these and must not have to reference
/// the extraction assembly to do it.</para>
/// </summary>
public readonly struct DirtyActorEdit {
    public DirtyActorEdit(int characterIndex, ActorStat[] stats, ActorConditions conditions,
        ushort[] knownSpells = null) {
        CharacterIndex = characterIndex;
        Stats = stats;
        Conditions = conditions;
        KnownSpells = knownSpells;
    }

    /// <summary>Character id, 0..5 — which of the six saved party records to write over.</summary>
    public int CharacterIndex { get; }

    /// <summary>Live attribute slots in ActorAttribute order, or null to leave them untouched.</summary>
    public ActorStat[] Stats { get; }

    /// <summary>Live affliction ranks, or null to leave them untouched.</summary>
    public ActorConditions Conditions { get; }

    /// <summary>The three known-spell words, or null to leave them untouched. Without these a
    /// spell learned from a scroll would apply and then vanish on save.</summary>
    public ushort[] KnownSpells { get; }
}
