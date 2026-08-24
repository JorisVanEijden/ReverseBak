namespace GameData.Resources.World;

/// <summary>
/// The per-encounter-actor state block in a save body — what makes a killed roster actor stay
/// dead, and a defeated encounter stay defeated, across a save and reload.
///
/// <para>Reversed from <c>rgnenc_persist_actor_removed</c> (RGNENC.C:437) and
/// <c>rgnenc_reset_and_save</c> (RGNENC.C:457), both of which write 12-byte records here through
/// <c>gstate_temp_file_write_at</c>.</para>
///
/// <para><b>Where the block is, and why that is worth stating.</b> The original addresses it with a
/// chain of macros, each based on the end of the previous region
/// (<c>GAM_ENC_OBJ_STATE</c> -> <c>GAM_ENC_VISITED_TIME(700)</c> -> <c>GAM_ENC_FOUGHT_TIME(700)</c>
/// -> <c>GAM_ENC_ROSTER(700)</c> -> <c>GAM_OBJFIXED_LOCATION(20) + 0x28</c> ->
/// <c>GAM_ENCOUNTER_TABLE + sizeof(EncounterTable)</c> -> <c>sizeof(GameState)</c>). Walking that
/// chain puts the block at <see cref="BodyOffset"/> and puts the block AFTER it exactly where our
/// independently-measured <c>StateDataSize + WorldDataSize</c> already said the combatant pool
/// starts — two derivations meeting at 0x90e7, which is what makes this offset trustworthy rather
/// than merely plausible.</para>
/// </summary>
/// <para><b>The same block <see cref="EncounterActorPersistence"/> describes.</b> That class owns
/// the addressing and the state vocabulary (it named them first); this one owns the byte layout and
/// the read/write of the block out of a save body. Every constant here defers to it rather than
/// restating a number — they were duplicated for two sessions, which is exactly the drift a shared
/// definition prevents.</para>
public sealed class EncounterObjectStates {
    /// <summary>Ref-pairs (zone/chapter pairings) the block covers.</summary>
    public const int RefPairs = EncounterActorPersistence.RefPairs;

    /// <summary>Encounter records per ref-pair.</summary>
    public const int RecordsPerRefPair = EncounterActorPersistence.RecordsPerRefPair;

    /// <summary>Actor slots per encounter record — the roster width.</summary>
    public const int SlotsPerRecord = EncounterActorPersistence.SlotsPerRecord;

    /// <summary>Entries per ref-pair: <c>0x23</c>, the stride the original multiplies by.</summary>
    public const int EntriesPerRefPair = EncounterActorPersistence.SlotsPerRefPair;

    /// <summary>Total entries: 40 x 5 x 7.</summary>
    public const int EntryCount = RefPairs * EntriesPerRefPair;

    /// <summary>Bytes per entry: two int32 offsets, an int16 facing and an uint16 kind/state.</summary>
    public const int EntrySize = EncounterActorPersistence.StateSize;

    /// <summary>Size of the whole block.</summary>
    public const int SaveSize = EntryCount * EntrySize;

    /// <summary>Offset of the block <b>within the save body</b>.</summary>
    public const int BodyOffset = 0x4f47;

    /// <summary>
    /// Where the block starts in a SAVE##.GAM <b>file</b>, past the 100-byte save header. Kept
    /// beside <see cref="BodyOffset"/> so the two are not silently interchanged — reading a file
    /// offset as a body offset lands 100 bytes into the wrong field and still parses.
    /// </summary>
    public const int FileOffset = BodyOffset + 0x64;

    /// <summary>Kind byte meaning "this actor was removed" — written on a roster actor's death.</summary>
    public const int KindRemoved = EncounterActorPersistence.Removed >> 8;

    /// <summary>
    /// Kind byte written by the encounter reset: alive again, but not yet placed on the field.
    /// </summary>
    public const int KindReset = EncounterActorPersistence.Unplaced >> 8;

    /// <summary>Roaming — the actor walks a movement pattern.</summary>
    /// <remarks>
    /// <b>Only this kind moves.</b> Both the updater and the renderer gate on it, so a movement
    /// pattern set on a <see cref="KindStanding"/> actor does nothing at all.
    /// </remarks>
    public const int KindRoaming = EncounterActorPersistence.Roaming >> 8;

    /// <summary>Placed and standing still.</summary>
    public const int KindStanding = EncounterActorPersistence.Placed >> 8;

    /// <summary>
    /// Stops every roaming actor in one encounter record, as defeating it does.
    /// </summary>
    /// <returns>How many slots were stopped.</returns>
    /// <remarks>
    /// <c>rgnenc_mark_defended</c> rewrites any slot whose kind is <see cref="KindRoaming"/> to
    /// <see cref="KindStanding"/> — <b>a one-way trip</b>. Nothing in the game ever promotes a
    /// standing actor back to roaming, so once an encounter has been defeated (or merely saved
    /// while placed) its patrol never resumes.
    ///
    /// <para><b>The low byte goes with it.</b> The kind and the walk frame/direction share one word,
    /// so a stopped actor loses its animation phase too — which is correct: it is no longer walking.
    /// Preserving the low bits would leave a standing actor holding a mid-stride frame.</para>
    /// </remarks>
    public int StopRoaming(int refPair, int recordIndex) {
        var stopped = 0;
        for (var slot = 0; slot < SlotsPerRecord; slot++) {
            int at = IndexOf(refPair, recordIndex, slot);
            if (_entries[at].Kind == KindRoaming) {
                Write(at, KindStanding);
                stopped++;
            }
        }
        return stopped;
    }

    /// <summary>One entry.</summary>
    public struct Entry {
        /// <summary>Sub-tile X offset of the actor's pose. Zeroed by both writers.</summary>
        public int WorldXOffset;

        /// <summary>Sub-tile Y offset. Zeroed by both writers.</summary>
        public int WorldYOffset;

        /// <summary>Facing. Zeroed by both writers.</summary>
        public short Facing;

        /// <summary>
        /// Kind in the high byte, state flags in the low byte.
        /// </summary>
        /// <remarks>
        /// The original tests it as <c>(wKind_state &amp; 0xff08) >> 8 == 3</c>, which after the
        /// shift is just the high byte — the <c>0x08</c> in the mask cannot survive it. So the kind
        /// is <see cref="Kind"/> and the low byte is carried untouched.
        /// </remarks>
        public ushort KindState;

        /// <summary>The high byte — see <see cref="KindRemoved"/> and <see cref="KindReset"/>.</summary>
        public int Kind => KindState >> 8;

        /// <summary>True when nothing has ever been written here.</summary>
        public bool IsEmpty =>
            WorldXOffset == 0 && WorldYOffset == 0 && Facing == 0 && KindState == 0;
    }

    private readonly Entry[] _entries = new Entry[EntryCount];

    /// <summary>
    /// Flat index of one actor slot, matching the original's
    /// <c>refPair * 0x23 + recordIndex * 7 + slotIndex</c>.
    /// </summary>
    public static int IndexOf(int refPair, int recordIndex, int slotIndex) =>
        EncounterActorPersistence.StateIndex(refPair, recordIndex, slotIndex);

    public Entry this[int index] => _entries[index];

    /// <summary>Read the block out of a save body.</summary>
    public void Load(byte[] body, int offset = BodyOffset) {
        if (body == null || offset < 0 || offset + SaveSize > body.Length) {
            System.Array.Clear(_entries, 0, EntryCount);
            return;
        }

        for (var i = 0; i < EntryCount; i++) {
            int at = offset + (i * EntrySize);
            _entries[i] = new Entry {
                WorldXOffset = System.BitConverter.ToInt32(body, at),
                WorldYOffset = System.BitConverter.ToInt32(body, at + 4),
                Facing = System.BitConverter.ToInt16(body, at + 8),
                KindState = System.BitConverter.ToUInt16(body, at + 10),
            };
        }
    }

    /// <summary>Write the block back into a save body, inverse of <see cref="Load"/>.</summary>
    public bool Save(byte[] body, int offset = BodyOffset) {
        if (body == null || offset < 0 || offset + SaveSize > body.Length) {
            return false;
        }

        for (var i = 0; i < EntryCount; i++) {
            int at = offset + (i * EntrySize);
            Entry e = _entries[i];
            System.BitConverter.GetBytes(e.WorldXOffset).CopyTo(body, at);
            System.BitConverter.GetBytes(e.WorldYOffset).CopyTo(body, at + 4);
            System.BitConverter.GetBytes(e.Facing).CopyTo(body, at + 8);
            System.BitConverter.GetBytes(e.KindState).CopyTo(body, at + 10);
        }
        return true;
    }

    /// <summary>
    /// Records that one roster actor was removed, so it does not come back on revisit.
    /// </summary>
    /// <remarks>
    /// <b>The pose is zeroed, not preserved.</b> Both writers clear the offsets and facing and keep
    /// only the kind — the record says "gone", not "gone from here". A port that stored the death
    /// position would be inventing state the original does not keep.
    ///
    /// <para>Called on death only for an actor that was actually REMOVED from the field and found
    /// in the enemy array — a corpse-leaving death writes nothing here, which is why some kills
    /// persist and others do not.</para>
    /// </remarks>
    public void MarkRemoved(int refPair, int recordIndex, int slotIndex) =>
        Write(IndexOf(refPair, recordIndex, slotIndex), KindRemoved);

    /// <summary>Records the encounter reset that puts a defeated group back on the field.</summary>
    public void MarkReset(int refPair, int recordIndex, int slotIndex) =>
        Write(IndexOf(refPair, recordIndex, slotIndex), KindReset);

    /// <summary>
    /// Sets a slot's kind directly. For tests and for a loader replaying a state the game wrote —
    /// the named Mark* methods cover the transitions the game itself performs.
    /// </summary>
    public void SetKindForTest(int refPair, int recordIndex, int slotIndex, int kind) =>
        Write(IndexOf(refPair, recordIndex, slotIndex), kind);

    private void Write(int index, int kind) {
        _entries[index] = new Entry { KindState = (ushort)(kind << 8) };
    }

    /// <summary>Entries carrying the given kind — for tests and diagnostics.</summary>
    public int CountOfKind(int kind) {
        var n = 0;
        for (var i = 0; i < EntryCount; i++) {
            if (!_entries[i].IsEmpty && _entries[i].Kind == kind) {
                n++;
            }
        }
        return n;
    }
}
