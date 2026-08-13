namespace GameData.Resources.World;

/// <summary>
/// Remembering which roaming-encounter actors are still standing — <c>rgnenc_persist_actor_placed</c>,
/// <c>rgnenc_persist_actor_removed</c> and <c>rgnenc_savefile_init_35slot_tbl</c>
/// (<c>SRC/GAME/ENC/RGNENC.C</c>).
///
/// <para>This is the state that makes a killed roaming group <b>stay</b> killed: the combat death
/// handler calls the removal path, and the entry is written straight into <c>TEMP.GAM</c> so a
/// revisit finds the slot empty.</para>
///
/// <para>Distinct from <see cref="EncounterVisitTable"/>, which only remembers that the party came
/// <i>near</i> something. This one remembers what happened to it.</para>
/// </summary>
public static class EncounterActorPersistence {
    /// <summary>Actor slots in one encounter record — the same seven-entry roster the encounter tables use.</summary>
    public const int SlotsPerRecord = 7;

    /// <summary>
    /// Slots stored per zone-ref pair: 35, which is <see cref="SlotsPerRecord"/> × 5. So a ref pair
    /// can hold <b>five</b> encounter records, each with its seven actors.
    /// </summary>
    public const int SlotsPerRefPair = 0x23;

    /// <summary>Encounter records a ref pair can hold, implied by the block size.</summary>
    public const int RecordsPerRefPair = SlotsPerRefPair / SlotsPerRecord;

    /// <summary>Zone-ref pairs the save block covers.</summary>
    public const int RefPairs = 0x28;

    /// <summary>Bytes per stored state — the read/write width the original uses.</summary>
    public const int StateSize = 0xc;

    /// <summary>
    /// The value <see cref="InitialState"/> gives the first slot of each block. Every other slot
    /// starts at <see cref="Removed"/>, so slot 0 is deliberately distinguishable from a slot that
    /// has been emptied.
    /// </summary>
    public const int Untouched = 0;

    /// <summary>Not present: what a removal writes, and what slots 1..34 initialise to.</summary>
    public const int Removed = 0x100;

    /// <summary>Standing in the world.</summary>
    public const int Placed = 0x400;

    /// <summary>
    /// Where one actor's state lives, as an index into the save's encounter-state array.
    /// </summary>
    /// <param name="refPair">The zone-ref pair index.</param>
    /// <param name="recordIndex">Which encounter record within that pair, 0..4.</param>
    /// <param name="slotIndex">Which of the record's seven actors.</param>
    public static int StateIndex(int refPair, int recordIndex, int slotIndex) =>
        (refPair * SlotsPerRefPair) + (recordIndex * SlotsPerRecord) + slotIndex;

    /// <summary>
    /// What a freshly initialised block holds at a given slot.
    ///
    /// <para>Slot 0 gets <see cref="Untouched"/> and the rest get <see cref="Removed"/>. The
    /// original writes exactly that and it is preserved rather than tidied into a uniform fill —
    /// anything reading state 0 as "removed" would be conflating two values the game keeps
    /// separate.</para>
    /// </summary>
    public static int InitialState(int slotWithinBlock) =>
        slotWithinBlock == 0 ? Untouched : Removed;

    /// <summary>
    /// Whether a placement keeps the pose already stored rather than the one supplied.
    ///
    /// <para><b>Underground it re-reads the saved pose and keeps it</b>; anywhere else it takes the
    /// caller's. So a roaming actor in a dungeon resumes where it was left, while one outdoors is
    /// repositioned by whatever placed it. Same enclosed zone kind that gates doors, pits and the
    /// proximity encounter check.</para>
    /// </summary>
    public static bool KeepsStoredPose(int zoneKind) => zoneKind == ProximityScan.EncounterZoneKind;
}
