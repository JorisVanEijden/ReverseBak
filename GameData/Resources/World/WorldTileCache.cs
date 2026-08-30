namespace GameData.Resources.World;

/// <summary>
/// The nine-slot world tile cache — <c>InitWorldTileSystem</c> (0x72a00),
/// <c>CheckAndLoadNewTile</c> (0x72b02) and <c>FindLoadedTileByCoords</c> (0x72ec7), ovr185.
/// </summary>
/// <remarks>
/// <b>The function names are misleading and the shape is not what they suggest.</b>
/// <c>CheckAndLoadNewTile</c> loads nothing: it works out which tile the camera is standing in, and
/// if that tile is already in the cache it SWAPS it into slot 0. If it is not cached the function
/// returns and does nothing at all. The loading happens in <c>UpdateWorldItemsForTile</c>, which
/// despite its name is the routine that calls <c>LoadTzzxxyy.WLD</c> and keeps the ring around the
/// current tile populated.
///
/// <para><b>Slot 0 is the tile the party is standing in.</b> Everything else keys off that: the
/// crossing test compares against slot 0 only, and the search deliberately skips it.</para>
/// </remarks>
public static class WorldTileCache {
    /// <summary>Slots in the cache — one current tile and its eight neighbours.</summary>
    public const int Slots = 9;

    /// <summary>The slot holding the tile the party is in.</summary>
    public const int CurrentSlot = 0;

    /// <summary>World units per tile, the divisor that turns a position into a tile coordinate.</summary>
    public const int TileWorldSize = 64000;

    /// <summary>Bytes of world-item storage each slot owns.</summary>
    /// <remarks>
    /// Nine of these are carved out of one allocation at startup, which is why the cache is a fixed
    /// nine and not a dictionary: the storage is pre-partitioned per slot.
    /// </remarks>
    public const int ItemBytesPerSlot = 6600;

    /// <summary>
    /// A slot with this zone number is empty.
    /// </summary>
    /// <remarks>
    /// <b>All nine start empty</b> — the initialiser zeroes every slot and then loads only the tile
    /// the party is in. So an unpopulated slot is the normal early state, not a fault.
    /// </remarks>
    public const int EmptyZone = 0;

    /// <summary>The tile coordinate a world position falls in.</summary>
    /// <remarks>
    /// <b>Truncating integer division, and then narrowed to a BYTE.</b> The original divides the
    /// 32-bit position and keeps <c>al</c>, so a coordinate outside 0..255 wraps rather than
    /// erroring. Modelled as the division only; the narrowing belongs to whatever stores it.
    /// </remarks>
    public static int TileOf(long worldPosition) => (int)(worldPosition / TileWorldSize);

    /// <summary>Whether the party has left the tile in slot 0.</summary>
    public static bool HasCrossed(int currentTileX, int currentTileY, int slotZeroX, int slotZeroY) =>
        currentTileX != slotZeroX || currentTileY != slotZeroY;

    /// <summary>
    /// Whether a slot may be returned by a lookup.
    /// </summary>
    /// <remarks>
    /// <b>Slot 0 is excluded from the search</b>, not merely unlikely to match. The search runs
    /// 1..8, so a lookup for the tile already current answers "not found" — which is safe only
    /// because the caller has already compared against slot 0 and returned. A port that searches
    /// from 0 finds the current tile and swaps it with itself.
    /// </remarks>
    public static bool IsSearchable(int slot) => slot > CurrentSlot && slot < Slots;

    /// <summary>
    /// <b>A crossing into an unloaded tile does nothing.</b>
    /// </summary>
    /// <remarks>
    /// The crossing handler returns when the lookup fails — no load, no swap, and the world items
    /// are not refreshed. It works only because the ring around the current tile is kept populated
    /// in advance, so by the time the party can reach a tile it is already resident. A port that
    /// streams lazily on the crossing instead will behave the same in the common case and diverge
    /// exactly where the original would have shown stale terrain.
    /// </remarks>
    public static bool LoadsOnCrossing => false;

    /// <summary>
    /// Global keys cleared when the party crosses into another tile.
    /// </summary>
    /// <remarks>
    /// <b>Twenty keys, which is BOTH transient hotspot blocks</b> — the scout-tried flags at 5200
    /// and the spotted flags at 5210. Clearing only the first leaves a spot earned on one tile
    /// buying a sneak-past on the next; see <c>HotspotService</c>, which clears both for this reason.
    /// </remarks>
    public const int FirstClearedGlobal = 5200;

    /// <inheritdoc cref="FirstClearedGlobal"/>
    public const int LastClearedGlobal = 5219;

    /// <summary>Slots in each of the two transient blocks.</summary>
    /// <remarks>
    /// Ten, so the twenty cleared keys are two ten-slot blocks and not one twenty-slot one — which
    /// is the whole reason <see cref="ScoutTriedFlagKey"/> and <see cref="ScoutedFlagKey"/> are ten
    /// apart and mean different things.
    /// </remarks>
    public const int SlotsPerTransientBlock = 10;

    /// <summary>The "a scout roll was tried for this hotspot" flag.</summary>
    public static int ScoutTriedFlagKey(int hotspotIndex) => FirstClearedGlobal + hotspotIndex;

    /// <summary>The "this hotspot has been spotted" flag.</summary>
    /// <remarks>
    /// The second block, <see cref="SlotsPerTransientBlock"/> above the first. Both are cleared on a
    /// crossing; clearing only the first lets a spot earned on one tile buy a sneak-past on the next.
    /// </remarks>
    public static int ScoutedFlagKey(int hotspotIndex) =>
        FirstClearedGlobal + SlotsPerTransientBlock + hotspotIndex;

    /// <summary>Whether a global is wiped by a tile crossing.</summary>
    public static bool ClearedOnCrossing(int key) =>
        key >= FirstClearedGlobal && key <= LastClearedGlobal;
}
