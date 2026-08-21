namespace GameData.Resources.World;

/// <summary>
/// Placing a zone's roaming encounter actors — <c>rgnenc_load_encounter_actors</c>
/// (<c>SRC/GAME/ENC/RGNENC.C</c>), the spawn the rest of RGNENC animates.
/// </summary>
/// <remarks>
/// <b>The actor's KIND and its saved STATE are the same field.</b> Everything reads the high byte of
/// one 16-bit word: the renderer and the movement updater call it a kind, the save code calls it a
/// state, and they are the same number. <see cref="EncounterActorPersistence"/> names 0x400 "Placed"
/// and 0x100 "Removed"; read as kinds those are <see cref="Standing"/> and <see cref="Gone"/>. Once
/// that clicks, the low bits stop looking like a separate encoding — they are the walk frame and the
/// walk direction riding along in the same word.
/// </remarks>
public static class EncounterActorSpawn {
    // ---- the four kinds, as the high byte of the state word --------------------------------------

    /// <summary>Never touched — the block has not been seeded yet.</summary>
    public const int Unseeded = 0x000;

    /// <summary>Dealt with: killed, or consumed. Not placed.</summary>
    public const int Gone = 0x100;

    /// <summary>Alive and owed a placement, but not yet placed.</summary>
    public const int Pending = 0x200;

    /// <summary>Placed and walking. <b>The only kind that roams.</b></summary>
    public const int Roaming = 0x300;

    /// <summary>Placed and stationary.</summary>
    public const int Standing = 0x400;

    /// <summary>The kind carried by a state word.</summary>
    /// <remarks>
    /// The original masks with <c>0xff08</c> before shifting, but bit 3 is shifted away, so the mask
    /// is just the high byte — the same dead-bit note <see cref="EncounterActorPose"/> records.
    /// </remarks>
    public static int KindOf(int stateWord) => (stateWord >> 8) & 0xff;

    // ---- the seed pass ---------------------------------------------------------------------------

    /// <summary>
    /// Whether the zone's encounter block still needs seeding.
    /// </summary>
    /// <remarks>
    /// Read from the FIRST slot only. The pass runs once per zone-ref block, ever: it stamps that
    /// slot <see cref="Gone"/> as its own "seeded" marker before doing anything else, so a second
    /// visit falls straight through. That doubles up the meaning of slot 0 — which is why
    /// <see cref="EncounterActorPersistence.InitialState"/> deliberately writes 0 there and 0x100
    /// everywhere else rather than filling uniformly.
    /// </remarks>
    public static bool NeedsSeeding(int firstSlotStateWord) => KindOf(firstSlotStateWord) == 0;

    /// <summary>
    /// What a roster entry seeds to: <see cref="Pending"/> if that combatant is still alive,
    /// otherwise left as it was.
    /// </summary>
    /// <param name="rosterSlot">The roster's entry, or -1 for an empty slot.</param>
    /// <remarks>
    /// <b>Seeded from the living, not from the record.</b> The pass walks each encounter's seven-entry
    /// roster, reads each named combatant's own saved flags, and marks a slot pending only when that
    /// combatant is not dead — so a group the party already wiped out never comes back, and it is the
    /// combatant table rather than the encounter record that remembers.
    /// </remarks>
    public static bool SeedsAsPending(int rosterSlot, bool combatantIsDead) =>
        rosterSlot >= 0 && !combatantIsDead;

    // ---- placement -------------------------------------------------------------------------------

    /// <summary>
    /// Whether an actor of this kind is placed at all, given the record's restriction flag.
    /// </summary>
    /// <param name="standingOnly">
    /// The record's flag bit 0. When set, <b>only stationary actors appear</b> — a roaming group
    /// authored on such a record simply does not show up.
    /// </param>
    /// <remarks>
    /// <see cref="Gone"/> and <see cref="Unseeded"/> are never placed: they fall to the switch's
    /// default. So the flag narrows an already-narrow set rather than being the only gate.
    /// </remarks>
    public static bool IsPlaced(int stateWord, bool standingOnly) {
        int kind = KindOf(stateWord);
        if (standingOnly && kind != KindOf(Standing)) {
            return false;
        }
        return kind == KindOf(Pending) || kind == KindOf(Roaming) || kind == KindOf(Standing);
    }

    /// <summary>Whether this actor takes its position from the record's template rather than a save.</summary>
    /// <remarks>
    /// <b>Only a <see cref="Pending"/> actor uses the template.</b> One that has been placed before
    /// resumes from its stored pose, which is what makes a dungeon roamer pick up where it was left.
    /// </remarks>
    public static bool PlacesFromTemplate(int stateWord) => KindOf(stateWord) == KindOf(Pending);

    /// <summary>Bit of the state word holding the walk direction.</summary>
    public const int WalkDirectionBit = 4;

    /// <summary>How many walk frames the randomiser picks from.</summary>
    public const int WalkFrameCount = 3;

    /// <summary>
    /// The state a freshly-placed actor becomes: <see cref="Roaming"/> with a random walk frame and
    /// a random direction through the cycle.
    /// </summary>
    /// <param name="frameRoll"><c>RND(3)</c>.</param>
    /// <param name="directionRoll"><c>RND2(2)</c> — the direction bit is set when this is non-zero.</param>
    /// <remarks>
    /// <b>Every fresh actor starts mid-stride, and differently.</b> Placing them all on frame 0
    /// walking the same way makes a group move in lockstep, which is the tell of a ported spawn.
    /// The direction bit is the same one <see cref="EncounterActorPose"/> reads to tell "frame 1
    /// going up" from "frame 1 coming down".
    /// </remarks>
    public static int FreshlyPlacedState(int frameRoll, int directionRoll) {
        int state = Roaming | (frameRoll % WalkFrameCount);
        if (directionRoll != 0) {
            state |= WalkDirectionBit;
        }
        return state;
    }

    /// <summary>
    /// <b>Persisting an actor forces it to <see cref="Standing"/>, whatever it was.</b>
    /// </summary>
    /// <remarks>
    /// <c>rgnenc_persist_actor_placed</c> writes 0x400 unconditionally. Nothing ever promotes a
    /// standing actor back to <see cref="Roaming"/> — the spawn only randomises a
    /// <see cref="Pending"/> one — and the movement updater ignores every kind but roaming. So a
    /// wandering monster that gets saved comes back STOPPED, and stays stopped for the rest of the
    /// game.
    ///
    /// <para>Recorded because it looks like a bug and a port is likely to "fix" it by preserving the
    /// kind. That would put monsters back on patrol in a game that leaves them standing, which is a
    /// visible behaviour change, so it is a decision to take deliberately rather than by accident.</para>
    /// </remarks>
    public static int StateAfterPersisting => Standing;

    // ---- the caps --------------------------------------------------------------------------------

    /// <summary>Encounter records a zone can have live at once.</summary>
    public const int MaxRecords = 5;

    /// <summary>Actors per record.</summary>
    public const int SlotsPerRecord = EncounterActorPersistence.SlotsPerRecord;

    /// <summary>Placed objects across the whole zone — <see cref="MaxRecords"/> x seven.</summary>
    public const int MaxPlacedObjects = EncounterActorPersistence.SlotsPerRefPair;

    /// <summary>
    /// Where an actor's state lives within the zone's block.
    /// </summary>
    public static int StateSlot(int recordIndex, int slotIndex) =>
        recordIndex * SlotsPerRecord + slotIndex;

    /// <summary>
    /// How many actors a record actually carries.
    /// </summary>
    /// <remarks>
    /// <b>Read off the FIRST slot's kind field, which doubles as the count.</b> The loop bound is
    /// <c>pActors[0].kind</c>, not a separate length — the extractor calls the same byte
    /// <c>SlotCount</c>. Capped at seven regardless of what the byte says, so a corrupt record cannot
    /// walk off the end of the roster.
    /// </remarks>
    public static int ActorCount(int firstSlotCountByte) =>
        firstSlotCountByte < 0 ? 0
        : firstSlotCountByte > SlotsPerRecord ? SlotsPerRecord
        : firstSlotCountByte;

    /// <summary>Tile size in world units — what a tile-relative offset is measured against.</summary>
    public const long TileWorldSize = 64000;

    /// <summary>
    /// Makes a tile-relative offset absolute.
    /// </summary>
    /// <remarks>
    /// Spawn points AND all four waypoints are stored relative to the tile and made absolute at spawn
    /// time by adding the party's current tile origin. A port that treats them as world coordinates
    /// drops the whole group near the origin, and one that converts the spawn but forgets the
    /// waypoints gets actors that walk off toward the corner of the map.
    /// </remarks>
    public static long ToWorld(int tileIndex, long tileRelative) =>
        tileIndex * TileWorldSize + tileRelative;
}
