namespace GameData.Resources.World;

/// <summary>
/// Which roaming-encounter spots the party has already come near — <c>worldframe_enc_rec_prox</c>
/// and <c>worldframe_enc_check_visited</c> (<c>SRC/R3D/SCENE/WORLDFRM.C</c>).
///
/// <para>Despite the name of the function that writes it, this <b>spawns nothing</b>. It is a
/// remembered "seen" bitmap: forty world tiles, each with a bit per entity in that tile, set when
/// the party passes within <see cref="ProximityScan.EncounterProximityRange"/> of one. It is what
/// stops the same roaming encounter firing again every frame and again on every revisit.</para>
///
/// <para><b>It is save state.</b> The original reads and writes it to <c>TEMP.GAM</c> at
/// <see cref="SaveOffset"/>, so the marks survive saving and loading. In the shipped
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
    /// Offset of the block in the save body, and its size (<c>0x668</c> = 40×3 + 40×38). The offset
    /// is <c>sizeof(GameState)</c>, which the shipped STARTUP.GAM pins at 0xb3b.
    /// </summary>
    public const int SaveOffset = 0xb3b;

    /// <inheritdoc cref="SaveOffset"/>
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
    /// never noted and its encounters can fire repeatedly. Reproduced rather than "fixed" with an
    /// LRU, which would change which encounters repeat.
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
