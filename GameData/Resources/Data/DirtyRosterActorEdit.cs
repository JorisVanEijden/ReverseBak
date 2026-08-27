namespace GameData.Resources.Data;

using GameData.Resources.Character;

/// <summary>
/// One roster actor's live attributes, to be written back over their saved record — how the damage
/// a fight did to an ENEMY survives it.
/// </summary>
/// <remarks>
/// <b>Deliberately not <see cref="DirtyActorEdit"/>, and the difference is the index.</b> That one
/// carries a character id 0..5 and the writer bounds it against the six party records; this one
/// carries a slot in the 1730-entry actor table. The two record layouts are identical and their
/// meanings are not — feeding a roster slot to the party path writes an enemy's health onto
/// whichever party member shares that number, which is the exact conflation
/// <c>GameSession.RosterStatsOf</c> exists to prevent. Separate types make that unspellable rather
/// than merely documented.
///
/// <para>Attributes only, where <see cref="DirtyActorEdit"/> also carries conditions and known
/// spells: a fight changes an enemy's stats and the rest of its record is what the save already
/// held. The original copies the whole 95 bytes back
/// (<c>SaveEncounterNpcsToTempGam</c>, IDA <c>0x63265</c>) rather than filtering, so widen this the
/// day combat is found to change something else.</para>
/// </remarks>
public readonly struct DirtyRosterActorEdit {
    public DirtyRosterActorEdit(int actorSlot, ActorStat[] stats) {
        ActorSlot = actorSlot;
        Stats = stats;
    }

    /// <summary>Index into the save's 1730-entry actor table.</summary>
    public int ActorSlot { get; }

    /// <summary>Live attributes, or null to leave the saved ones alone.</summary>
    public ActorStat[] Stats { get; }
}
