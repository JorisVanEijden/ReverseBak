namespace GameData.Resources.World;

/// <summary>
/// Which roaming-encounter spots the party has already come near — <c>worldframe_enc_rec_prox</c>
/// and <c>worldframe_enc_check_visited</c> (<c>SRC/R3D/SCENE/WORLDFRM.C</c>).
///
/// <para>Despite the name of the function that writes it, this <b>spawns nothing</b>. It is a
/// remembered "seen" bitmap: forty world tiles, each with a bit per entity in that tile, set when
/// the party passes within <see cref="ProximityScan.AutomapProximityRange"/> of one.</para>
///
/// <para><b>Corrected 2026-08-20: it is the DUNGEON AUTOMAP's record, and nothing to do with
/// encounters firing.</b> This used to say the marks are "what stops the same roaming encounter
/// firing again". They are not: <c>worldframe_enc_check_visited</c> has exactly ONE caller in the
/// whole program — <c>renderDungeonAutomap</c> (0x456AB) — which draws only the entities whose bit
/// is set. Nothing else ever reads a mark. So what this table decides is what the overhead map shows
/// underground, which is why the map fills in as you explore.</para>
///
/// <para>The recorder living inside the proximity scan, and canassa naming the buffer an "encounter
/// table", is where the encounter reading comes from — the same trap that had
/// <see cref="ProximityScan.RecordsOnAutomap"/> called "TriggersEncounter" until the same day. The
/// class keeps its name only because renaming ripples; read it as "the automap's visit record".</para>
///
/// <para><b>How the renderer uses it</b> (0x456AB): it fills the viewport with the sky pen, swaps
/// shape table slot 2 in for the duration — <c>Z##M.TBL</c>, the simplified plan geometry the zone
/// loader adds only underground — draws each marked entity, and swaps back. Doors (shape 0x5C /
/// 0x5D) go through a mark renderer instead of the entity path, so a door reads as a door on the
/// plan. The centred <c>mapicons</c> blit at the end is inside <c>#ifndef V102CD</c>, so on our
/// target build the automap draws NO party marker of its own; that comes from <c>drawMap</c>'s tail,
/// the same one <see cref="OverheadMapMarker"/> covers. A port that copies the floppy branch draws
/// it twice.</para>
///
/// <para><b>It is save state.</b> The original reads and writes it to <c>TEMP.GAM</c> at
/// <see cref="BodyOffset"/>, so the marks survive saving and loading. In the shipped
/// <c>STARTUP.GAM</c> all 120 coordinate bytes are 0xff and the flags are clear — verified in the
/// data — so every slot starts free.</para>
/// </summary>
public sealed class EncounterVisitTable {
    /// <summary>World tiles the table can remember. There is no eviction — see <see cref="MarkSeen"/>.</summary>
    public const int Capacity = 40;

    /// <summary>
    /// Flag bytes per tile: 38, i.e. 304 bits. That covers the 300-entry cap a world tile's object
    /// list has, with four bits to spare.
    /// </summary>
    public const int FlagBytesPerSlot = 38;

    /// <summary>Highest entity index a slot can record.</summary>
    public const int MaxEntityIndex = (FlagBytesPerSlot * 8) - 1;

    /// <summary>All three coordinate bytes at this value mark a free slot.</summary>
    public const byte FreeMarker = 0xff;

    /// <summary>
    /// Offset of the block <b>within the save body</b> — <c>GAM_ENCOUNTER_TABLE</c>, defined as
    /// <c>sizeof(GameState)</c>, so the table sits immediately after the game-state block.
    ///
    /// <para><b>Do not confuse this with where the block appears in a SAVE##.GAM FILE.</b> A save
    /// file is a 100-byte header followed by the body, so the same block is at
    /// <see cref="FileOffset"/> = 0xb3b there while TEMP.GAM — which is the bare body — has it at
    /// 0xad7. Everything in this class works on the body. Confirmed empirically rather than
    /// derived: a free table is 120 bytes of <see cref="FreeMarker"/>, and that run sits at 0xb3b
    /// in STARTUP.GAM/SAVE##.GAM and at 0xad7 in TEMP.GAM.</para>
    /// </summary>
    public const int BodyOffset = 0xad7;

    /// <summary>
    /// Where the block starts in a SAVE##.GAM <b>file</b>, i.e. past the 100-byte save header.
    /// Provided so the two are not silently interchanged; the writer patches the body.
    /// </summary>
    public const int FileOffset = 0xb3b;

    /// <summary>Size of the block: <c>0x668</c> = 40×3 + 40×38.</summary>
    public const int SaveSize = 0x668;

    private readonly byte[] _x = new byte[Capacity];
    private readonly byte[] _y = new byte[Capacity];
    private readonly byte[] _z = new byte[Capacity];
    private readonly byte[][] _flags = new byte[Capacity][];

    public EncounterVisitTable() {
        for (var i = 0; i < Capacity; i++) {
            _flags[i] = new byte[FlagBytesPerSlot];
        }
        Reset();
    }

    /// <summary>Returns every slot to free, as a new game's save block is.</summary>
    public void Reset() {
        for (var i = 0; i < Capacity; i++) {
            _x[i] = FreeMarker;
            _y[i] = FreeMarker;
            _z[i] = FreeMarker;
            System.Array.Clear(_flags[i], 0, FlagBytesPerSlot);
        }
    }

    /// <summary>
    /// Read the block out of a save body. The layout is three parallel coordinate arrays followed
    /// by the per-slot flag bitmaps — <c>xCoord[40]</c>, <c>yCoord[40]</c>, <c>zCoord[40]</c>,
    /// <c>flags[40][38]</c>, which is exactly <see cref="SaveSize"/>.
    /// </summary>
    public void Load(byte[] body, int offset = BodyOffset) {
        if (body == null || offset < 0 || offset + SaveSize > body.Length) {
            Reset();
            return;
        }

        System.Array.Copy(body, offset, _x, 0, Capacity);
        System.Array.Copy(body, offset + Capacity, _y, 0, Capacity);
        System.Array.Copy(body, offset + (Capacity * 2), _z, 0, Capacity);
        int flagBase = offset + (Capacity * 3);
        for (var i = 0; i < Capacity; i++) {
            System.Array.Copy(body, flagBase + (i * FlagBytesPerSlot), _flags[i], 0, FlagBytesPerSlot);
        }
    }

    /// <summary>Write the block back into a save body, inverse of <see cref="Load"/>.</summary>
    public bool Save(byte[] body, int offset = BodyOffset) {
        if (body == null || offset < 0 || offset + SaveSize > body.Length) {
            return false;
        }

        System.Array.Copy(_x, 0, body, offset, Capacity);
        System.Array.Copy(_y, 0, body, offset + Capacity, Capacity);
        System.Array.Copy(_z, 0, body, offset + (Capacity * 2), Capacity);
        int flagBase = offset + (Capacity * 3);
        for (var i = 0; i < Capacity; i++) {
            System.Array.Copy(_flags[i], 0, body, flagBase + (i * FlagBytesPerSlot), FlagBytesPerSlot);
        }
        return true;
    }

    /// <summary>Slots currently holding a tile.</summary>
    public int UsedSlots {
        get {
            var used = 0;
            for (var i = 0; i < Capacity; i++) {
                if (!IsFree(i)) {
                    used++;
                }
            }
            return used;
        }
    }

    /// <summary>
    /// Records that the party has come near entity <paramref name="entityIndex"/> on this tile,
    /// claiming a slot for the tile if it does not have one.
    /// </summary>
    /// <returns>
    /// False when nothing was recorded. <b>A full table silently drops the mark</b> — the original
    /// has no eviction, so once forty tiles are remembered, proximity on a forty-first is simply
    /// never noted and nothing on it is ever drawn on the dungeon automap. Reproduced rather than
    /// "fixed" with an LRU, which would change which parts of a long dungeon the map remembers.
    /// </returns>
    public bool MarkSeen(byte zone, byte tileX, byte tileY, int entityIndex) {
        if (entityIndex < 0 || entityIndex > MaxEntityIndex) {
            return false;
        }
        int slot = FindSlot(zone, tileX, tileY);
        if (slot < 0) {
            slot = ClaimSlot(zone, tileX, tileY);
        }
        if (slot < 0) {
            return false;
        }
        _flags[slot][entityIndex >> 3] |= (byte)(1 << (entityIndex & 7));
        return true;
    }

    /// <summary>Whether that entity on that tile has already been marked.</summary>
    public bool HasSeen(byte zone, byte tileX, byte tileY, int entityIndex) {
        if (entityIndex < 0 || entityIndex > MaxEntityIndex) {
            return false;
        }
        int slot = FindSlot(zone, tileX, tileY);
        return slot >= 0 && (_flags[slot][entityIndex >> 3] & (1 << (entityIndex & 7))) != 0;
    }

    private bool IsFree(int slot) =>
        _x[slot] == FreeMarker && _y[slot] == FreeMarker && _z[slot] == FreeMarker;

    private int FindSlot(byte zone, byte tileX, byte tileY) {
        for (var i = 0; i < Capacity; i++) {
            if (_x[i] == zone && _y[i] == tileX && _z[i] == tileY) {
                return i;
            }
        }
        return -1;
    }

    private int ClaimSlot(byte zone, byte tileX, byte tileY) {
        for (var i = 0; i < Capacity; i++) {
            if (IsFree(i)) {
                _x[i] = zone;
                _y[i] = tileX;
                _z[i] = tileY;
                return i;
            }
        }
        return -1;
    }
}
