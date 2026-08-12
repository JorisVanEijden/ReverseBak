namespace GameData.Resources.Combat;

using System;

/// <summary>Terrain kinds a combat-grid tile can carry. Values are the original's.</summary>
public enum CombatTerrain : byte {
    /// <summary>Ordinary walkable floor.</summary>
    Open = 0,

    /// <summary>Outside the playable area — a wall. <c>Load_grid</c> marks the two far corners with
    /// this, and the whole of rows 7..12 underground.</summary>
    OutOfBounds = 2,

    /// <summary>Trap crystal. Stepping onto one triggers a propagating line effect and 100 damage,
    /// and a pair of them cannot be squeezed between diagonally.</summary>
    Crystal = 3,

    /// <summary>A pushable element (the trap diamond): walking into it shoves it one tile further,
    /// and the step is refused if it cannot move.</summary>
    Pushable = 5,

    /// <summary>Impassable, like <see cref="OutOfBounds"/>.</summary>
    Wall = 7,

    /// <summary>A tile trap; stepping on it fires the trap rather than blocking.</summary>
    Trap = 8,
}

/// <summary>
/// The combat grid — terrain and occupancy for one encounter.
///
/// <para>Ported from <c>SRC/COMBAT/GRID/CMBTGRID.C</c> (<c>combatgrid_coord_valid</c>,
/// <c>combatgrid_tile_is_blocked</c>, <c>combatgrid_tile_walkable_kind</c>). The buffer is always
/// 8×13; the smaller underground arena is produced by marking rows 7..12 out of bounds rather than
/// by using a different array, exactly as <c>Load_grid</c> does.</para>
/// </summary>
public sealed class CombatGrid {
    /// <summary>Grid width in tiles; x is 0..7.</summary>
    public const int Width = 8;

    /// <summary>Grid buffer height; y is 0..12.</summary>
    public const int Height = 13;

    /// <summary>Rows actually playable underground — 0..6, the rest walled off.</summary>
    public const int UndergroundPlayableRows = 7;

    private readonly CombatTerrain[,] _terrain = new CombatTerrain[Width, Height];
    private readonly bool[,] _occupied = new bool[Width, Height];

    /// <summary>
    /// Builds an empty arena. <c>Load_grid</c> always walls off the two far corners, and underground
    /// additionally walls off rows 7..12, leaving an 8×7 playable area instead of 8×13.
    /// </summary>
    public CombatGrid(bool underground = false) {
        Underground = underground;
        _terrain[0, 0] = CombatTerrain.OutOfBounds;
        _terrain[Width - 1, 0] = CombatTerrain.OutOfBounds;

        if (underground) {
            for (var y = UndergroundPlayableRows; y < Height; y++) {
                for (var x = 0; x < Width; x++) {
                    _terrain[x, y] = CombatTerrain.OutOfBounds;
                }
            }
        }
    }

    /// <summary>True when this arena was built with the reduced underground playable area.</summary>
    public bool Underground { get; }

    /// <summary>Whether a coordinate is inside the 8×13 buffer at all.</summary>
    public static bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

    /// <summary>Terrain at a tile. Off-grid reads report <see cref="CombatTerrain.OutOfBounds"/>
    /// rather than throwing, as the original does.</summary>
    public CombatTerrain TerrainAt(int x, int y) =>
        InBounds(x, y) ? _terrain[x, y] : CombatTerrain.OutOfBounds;

    /// <summary>Sets a tile's terrain. Off-grid writes are ignored, as in the original.</summary>
    public void SetTerrain(int x, int y, CombatTerrain terrain) {
        if (InBounds(x, y)) {
            _terrain[x, y] = terrain;
        }
    }

    /// <summary>Whether a combatant currently stands on the tile.</summary>
    public bool IsOccupied(int x, int y) => InBounds(x, y) && _occupied[x, y];

    /// <summary>Marks or clears a tile's occupant.</summary>
    public void SetOccupied(int x, int y, bool occupied) {
        if (InBounds(x, y)) {
            _occupied[x, y] = occupied;
        }
    }

    /// <summary>
    /// Whether a tile may be stepped onto. Off-grid, walls and occupied tiles are all blocked.
    /// </summary>
    /// <remarks>
    /// <see cref="CombatTerrain.Pushable"/> counts as blocked here — the caller decides whether the
    /// element can be shoved out of the way. <see cref="CombatTerrain.Crystal"/> and
    /// <see cref="CombatTerrain.Trap"/> do <b>not</b> block: you can walk onto them, and doing so is
    /// how they fire.
    /// </remarks>
    public bool IsBlocked(int x, int y) {
        if (!InBounds(x, y)) {
            return true;
        }
        CombatTerrain terrain = _terrain[x, y];
        return terrain == CombatTerrain.OutOfBounds
            || terrain == CombatTerrain.Wall
            || terrain == CombatTerrain.Pushable
            || _occupied[x, y];
    }

    /// <summary>
    /// Whether this tile is a crystal for the purpose of the diagonal-squeeze rule
    /// (<c>combatgrid_tile_walkable_kind(x, y, -1)</c>).
    /// </summary>
    public bool IsCrystal(int x, int y) => TerrainAt(x, y) == CombatTerrain.Crystal;

    /// <summary>
    /// Chebyshev ("chessboard") distance — diagonal moves cost the same as orthogonal ones, which is
    /// what makes the grid 8-directional. Used by ranged to-hit and by target selection.
    /// </summary>
    public static int ChebyshevDistance(int x1, int y1, int x2, int y2) =>
        Math.Max(Math.Abs(x1 - x2), Math.Abs(y1 - y2));
}
