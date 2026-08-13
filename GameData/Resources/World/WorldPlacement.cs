namespace GameData.Resources.World;

/// <summary>
/// Turning a zone/tile/sub-tile placement into world coordinates —
/// <c>czone_world_pos_from_tile</c> and <c>czone_world_pos_tile_sub_ctr</c>
/// (<c>SRC/R3D/SCENE/CZONE.C</c>).
///
/// <para>The arithmetic was already described in prose on <c>ChapterStartData.PositionX</c> and
/// <c>LandingPosition</c> but implemented nowhere, so every consumer was going to re-derive it.
/// This is the one copy.</para>
/// </summary>
public static class WorldPlacement {
    /// <summary>World units across one map tile (<c>0xFA00</c>).</summary>
    public const int TileSize = 64000;

    /// <summary>World units across one sub-tile cell (<c>0x640</c>).</summary>
    public const int SubCellSize = 0x640;

    /// <summary>Sub-tile cells along a tile edge — 64000 / 1600.</summary>
    public const int SubCellsPerTile = TileSize / SubCellSize;

    /// <summary>
    /// Half a sub-cell, added on both axes when placing the party.
    ///
    /// <para><b>A spawn lands in the middle of its sub-cell, not on its corner.</b> The engine has
    /// two conversions and the one used for placing the player is the centred variant; dropping the
    /// centring would put every arrival 800 units north-west of where it should be — an eighth of a
    /// tile.</para>
    /// </summary>
    public const int SubCellCentre = SubCellSize / 2;

    /// <summary>The corner of a sub-tile cell, in world units along one axis.</summary>
    public static long CornerOf(int tile, int subCell) =>
        ((long)tile * TileSize) + ((long)subCell * SubCellSize);

    /// <summary>
    /// The centre of a sub-tile cell — where a spawn record, chapter start or teleport destination
    /// actually puts the party.
    /// </summary>
    public static long CentreOf(int tile, int subCell) => CornerOf(tile, subCell) + SubCellCentre;

    /// <summary>The tile a world coordinate falls in.</summary>
    public static int TileOf(long worldCoordinate) => (int)(worldCoordinate / TileSize);

    /// <summary>The sub-tile cell a world coordinate falls in, within its tile.</summary>
    public static int SubCellOf(long worldCoordinate) =>
        (int)(worldCoordinate % TileSize / SubCellSize);
}
