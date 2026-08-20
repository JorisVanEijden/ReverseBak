namespace GameData.Resources.World;

/// <summary>
/// The dungeon automap — what the overhead map draws underground, and the record it draws from.
/// </summary>
/// <remarks>
/// <b>Underground the map is not a view of the world; it is a view of your own history.</b>
/// <c>drawMap</c> sends an underground zone to <c>renderDungeonAutomap</c> (0x456AB) and never runs
/// the 3D pass, and that renderer draws only the entries the party has already seen. So the map
/// fills in as you explore and shows nothing you have not walked past.
///
/// <para><b>The names around this are a mess and both sources are wrong somewhere.</b> canassa calls
/// the three routines an "encounter table" (<c>worldframe_encounter_table_load</c>,
/// <c>..._enc_tbl_reapply_chap</c>, <c>..._render_chapter_full</c>) — but the code stores visited
/// COORDINATES with a bit per direction, writes them back only underground, and gates the render on
/// them. Nothing about it concerns encounters. IDA's names
/// (<c>loadDungeonAutomapData</c> / <c>saveDungeonAutomapIfUnderground</c> /
/// <c>renderDungeonAutomap</c>) describe what it does, and are the ones to trust here.</para>
/// </remarks>
public static class DungeonAutomap {
    /// <summary>How many places the map can remember: forty.</summary>
    /// <remarks>
    /// A hard cap, not a growing list. Once forty are held there is no eviction — the insert scan
    /// simply finds no free slot and the sighting is dropped, so a big enough dungeon stops
    /// recording rather than forgetting its oldest.
    /// </remarks>
    public const int Capacity = 0x28;

    /// <summary>The byte marking an unused slot in all three coordinates: 0xFF.</summary>
    /// <remarks>
    /// The insert scan looks for a triple of these rather than a count, so a slot is free only when
    /// every coordinate is 0xFF. A port that zero-fills the table instead makes slot 0,0,0 look
    /// occupied and loses the first sighting.
    /// </remarks>
    public const byte EmptySlot = 0xFF;

    /// <summary>
    /// <b>Recording is find-or-insert, then set one bit.</b>
    /// </summary>
    /// <remarks>
    /// A sighting is a coordinate triple plus a DIRECTION. The scan looks for that exact triple
    /// first and only allocates a slot when it is new, so revisiting a place from a second direction
    /// adds a bit to the existing entry rather than a second entry. That is what lets forty slots
    /// cover a dungeon: they are places, not sightings.
    /// </remarks>
    public static bool RecordsPlacesNotSightings => true;

    /// <summary>Whether a direction bit is set, given the entry's flag bytes.</summary>
    /// <remarks>
    /// <c>flags[direction &gt;&gt; 3] &amp; (1 &lt;&lt; (direction &amp; 7))</c> — a plain bitset
    /// addressed by direction, byte then bit.
    /// </remarks>
    public static bool IsSeenFrom(byte[] flags, int direction) {
        int index = direction >> 3;
        if (flags == null || index < 0 || index >= flags.Length) {
            return false;
        }

        return (flags[index] & (1 << (direction & 7))) != 0;
    }

    /// <summary>Sets the direction bit for a sighting.</summary>
    public static void MarkSeenFrom(byte[] flags, int direction) {
        int index = direction >> 3;
        if (flags == null || index < 0 || index >= flags.Length) {
            return;
        }
        flags[index] |= (byte)(1 << (direction & 7));
    }

    /// <summary>
    /// <b>The record is written back to the save ONLY underground.</b>
    /// </summary>
    /// <remarks>
    /// The save path is guarded by the underground mode and by the table being loaded at all, so
    /// walking about above ground never touches it. It is loaded on entering an underground zone and
    /// flushed on leaving, which is why a dungeon remembers what you explored between visits while
    /// the overworld keeps no such record.
    /// </remarks>
    public static bool PersistsOnlyUnderground => true;

    /// <summary>
    /// <b>The automap draws through the zone's MAP shape table, not the world's.</b>
    /// </summary>
    /// <remarks>
    /// The renderer swaps shape table slot 2 in for the duration and back out afterwards. Slot 2 is
    /// <c>Z##M.TBL</c>, which the zone loader adds only for an underground zone — the simplified
    /// geometry (plain corridor shapes, stairs, and none of the world's objects) that makes the
    /// automap read as a plan rather than a scene.
    /// </remarks>
    public const int MapShapeTableSlot = 2;

    /// <summary>The two shape ids that draw as a door mark instead of as an entity.</summary>
    /// <remarks>
    /// Everything visited renders through the ordinary entity path except these, which get their own
    /// mark renderer — so a door reads as a door on the plan rather than as whatever its model
    /// happens to look like from above.
    /// </remarks>
    public static readonly int[] DoorShapeIds = { 0x5C, 0x5D };

    /// <summary>Whether a shape draws as a door mark.</summary>
    public static bool DrawsAsDoorMark(int shapeId) =>
        shapeId == DoorShapeIds[0] || shapeId == DoorShapeIds[1];

    /// <summary>
    /// <b>The CD build does NOT draw the party icon in the automap renderer.</b>
    /// </summary>
    /// <remarks>
    /// The centred <c>mapicons</c> blit at the end of the renderer is inside <c>#ifndef V102CD</c>,
    /// and the 1.02 CD build is our target — so on our build the automap's own marker never runs.
    /// The party marker underground comes from <c>drawMap</c>'s own tail instead, the same one the
    /// overhead map uses above ground (see <see cref="OverheadMapMarker"/>). A port that copies the
    /// floppy branch draws it twice.
    /// </remarks>
    public static bool RendererDrawsItsOwnPartyIcon => false;
}
