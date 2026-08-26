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
        /// <summary>
        /// Sub-tile X offset of the actor's pose. Zeroed by the removal and the reset;
        /// <see cref="MarkPlaced"/> is the one writer that keeps it.
        /// </summary>
        public int WorldXOffset;

        /// <inheritdoc cref="WorldXOffset"/>
        public int WorldYOffset;

        /// <inheritdoc cref="WorldXOffset"/>
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
    /// <b>THIS writer zeroes the pose.</b> The removal and the reset clear the offsets and facing and
    /// keep only the kind — the record says "gone", not "gone from here". A port that stored the
    /// death position here would be inventing state the original does not keep.
    ///
    /// <para>Note that is a property of these two writers and not of the block:
    /// <see cref="MarkPlaced"/> carries a real pose.</para>
    ///
    /// <para>Called on death only for an actor that was actually REMOVED from the field and found
    /// in the enemy array — a corpse-leaving death writes nothing here, which is why some kills
    /// persist and others do not.</para>
    /// </remarks>
    public void MarkRemoved(int refPair, int recordIndex, int slotIndex) =>
        Write(IndexOf(refPair, recordIndex, slotIndex), KindRemoved);

    /// <summary>
    /// The once-ever seed pass for a ref pair — the opening of
    /// <c>rgnenc_load_encounter_actors</c> (RGNENC.C:184-215).
    /// </summary>
    /// <param name="refPair">The chunk's ref pair.</param>
    /// <param name="recordIds">
    /// The chunk's encounter record ids <b>in trigger order</b> — <see cref="EncounterReset.RecordIds"/>.
    /// </param>
    /// <param name="rosterOf">The seven roster entries of a record; <c>-1</c> marks an empty slot.</param>
    /// <param name="actorIsDead">Whether a roster combatant is dead.</param>
    /// <returns>True when the pass ran; false when this ref pair was already seeded.</returns>
    /// <remarks>
    /// <b>Seeded from the LIVING, not from the record.</b> The pass reads each named combatant's own
    /// saved flags and marks a slot pending only when that combatant is not dead — so a group the
    /// party already wiped out never comes back, and it is the combatant table rather than the
    /// encounter record that remembers.
    ///
    /// <para><b>Slot 0 is stamped BEFORE the walk, and the walk may overwrite it.</b> The original
    /// writes <see cref="KindRemoved"/> into the first slot as its "this pair is seeded" marker and
    /// then runs the roster walk over the same entries, so a live slot 0 ends up
    /// <see cref="KindReset"/> instead. The invariant is only that the first slot is never kind 0
    /// afterwards — which is all <see cref="EncounterActorSpawn.NeedsSeeding"/> asks. A port that
    /// wrote the marker AFTER the walk would clobber a live first actor every time.</para>
    ///
    /// <para><b>Deliberate divergence:</b> the original's seed walk has no record cap while its
    /// placement loop stops at five, so a chunk with six encounter hotspots walks past the end of
    /// the ref pair's block and into the next pair's. That is undefined behaviour rather than a
    /// rule, so this stops at <see cref="RecordsPerRefPair"/>. Callers using
    /// <see cref="EncounterReset.RecordIds"/> are already capped there; this is the backstop.</para>
    /// </remarks>
    public bool Seed(int refPair, System.Collections.Generic.IReadOnlyList<long> recordIds,
        System.Func<long, System.Collections.Generic.IReadOnlyList<short>> rosterOf,
        System.Func<int, bool> actorIsDead) {
        if (!EncounterActorSpawn.NeedsSeeding(_entries[IndexOf(refPair, 0, 0)].KindState)) {
            return false;
        }

        Write(IndexOf(refPair, 0, 0), KindRemoved);
        if (recordIds == null || rosterOf == null || actorIsDead == null) {
            return true;
        }

        for (var record = 0; record < recordIds.Count && record < RecordsPerRefPair; record++) {
            System.Collections.Generic.IReadOnlyList<short> roster = rosterOf(recordIds[record]);
            for (var slot = 0; slot < SlotsPerRecord; slot++) {
                int actor = roster != null && slot < roster.Count ? roster[slot] : -1;
                // Short-circuit rather than asking about slot -1: an empty slot names no combatant,
                // and a caller's dead-check has no answer for one.
                if (actor >= 0 && EncounterActorSpawn.SeedsAsPending(actor, actorIsDead(actor))) {
                    Write(IndexOf(refPair, record, slot),
                        EncounterActorSpawn.KindOf(EncounterActorSpawn.Pending));
                }
            }
        }
        return true;
    }

    /// <summary>Records the encounter reset that puts a defeated group back on the field.</summary>
    public void MarkReset(int refPair, int recordIndex, int slotIndex) =>
        Write(IndexOf(refPair, recordIndex, slotIndex), KindReset);

    /// <summary>
    /// Records an actor as placed and standing, keeping a pose — <c>rgnenc_persist_actor_placed</c>
    /// (RGNENC.C:414).
    /// </summary>
    /// <param name="underground">Whether the zone is underground; see the remarks.</param>
    /// <remarks>
    /// <b>THE THIRD WRITER, AND THE ONLY ONE THAT KEEPS A POSE.</b> The other two — the removal and
    /// the reset — zero the offsets and facing, which made "the pose is not preserved" look like a
    /// property of the block. It is a property of THOSE writers. This one carries a real pose, and a
    /// port that assumed otherwise would bring every saved roamer back at its tile's origin.
    ///
    /// <para><b>Above ground the caller's pose is written; underground the stored one is kept.</b>
    /// The original branches on the zone kind: outdoors it assigns <c>record.pose = src->pose</c>,
    /// underground it re-reads the existing entry and leaves the pose alone, writing back only the
    /// kind. So a dungeon actor resumes exactly where the block last had it, and an outdoor one
    /// resumes where it had walked to.</para>
    ///
    /// <para><b>The kind becomes <see cref="KindStanding"/> whatever it was.</b> Nothing ever
    /// promotes a standing actor back to roaming, so a wandering monster that is saved comes back
    /// stopped and stays stopped — the same one-way trip <see cref="StopRoaming"/> describes.</para>
    /// </remarks>
    public void MarkPlaced(int refPair, int recordIndex, int slotIndex,
        int worldXOffset, int worldYOffset, short facing, bool underground) {
        int at = IndexOf(refPair, recordIndex, slotIndex);
        Entry kept = _entries[at];
        _entries[at] = new Entry {
            WorldXOffset = underground ? kept.WorldXOffset : worldXOffset,
            WorldYOffset = underground ? kept.WorldYOffset : worldYOffset,
            Facing = underground ? kept.Facing : facing,
            KindState = (ushort)(KindStanding << 8),
        };
    }

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
