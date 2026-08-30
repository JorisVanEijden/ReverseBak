namespace GameData.Resources.Combat;

using System;

/// <summary>Terrain kinds a combat-grid tile can carry. Values are the original's.</summary>
public enum CombatTerrain : byte {
    /// <summary>Ordinary walkable floor.</summary>
    Open = 0,

    /// <summary>Outside the playable area — a wall. <c>Load_grid</c> marks the two far corners with
    /// this, and the whole of rows 7..12 underground.</summary>
    OutOfBounds = 2,

    /// <summary>
    /// Trap-crystal ground. Stepping onto it triggers a propagating line effect and 100 damage, and
    /// a pair of such tiles cannot be squeezed between diagonally.
    /// <para>Note this is the <b>ground</b>, not the crystal: while the crystal element still stands
    /// on the tile the tile is blocked (any element blocks). The terrain kind outlives the element,
    /// so it is once the crystal is gone that you can walk on — and set the effect off.</para>
    /// </summary>
    Crystal = 3,

    /// <summary>A pushable element (the trap diamond): walking into it shoves it one tile further,
    /// and the step is refused if it cannot move.</summary>
    Pushable = 5,

    /// <summary>The puzzle's exit cell.</summary>
    Exit = 6,

    /// <summary>Impassable, like <see cref="OutOfBounds"/>.</summary>
    Wall = 7,

    /// <summary>A tile trap; stepping on it fires the trap rather than blocking.</summary>
    Trap = 8,

    /// <summary>Cannon aimed west.</summary>
    CannonWest = 10,

    /// <summary>Cannon aimed east.</summary>
    CannonEast = 11,

    /// <summary>Cannon aimed north.</summary>
    CannonNorth = 12,

    /// <summary>Cannon aimed south.</summary>
    CannonSouth = 13,

    /// <summary>Where party slot 0 starts the puzzle.</summary>
    PartySlot0 = 15,

    /// <summary>Where party slot 1 starts.</summary>
    PartySlot1 = 16,

    /// <summary>Where party slot 2 starts.</summary>
    PartySlot2 = 17,
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
    private readonly int[,] _effectTimer = new int[Width, Height];

    /// <summary>
    /// Builds an empty arena. <c>Load_grid</c> always walls off the two far corners, and underground
    /// additionally walls off rows 7..12, leaving an 8×7 playable area instead of 8×13.
    /// </summary>
    public CombatGrid(bool underground = false) {
        Underground = underground;
        // *** -1, NOT 0. *** A timer of -1 is what marks a cell as carrying no spell effect, and it
        // is the whole reason the decay sweep can run over every cell without eating the authored
        // trap-puzzle terrain: combatgrid_tick_tile_effect touches a cell only when its timer is
        // >= 0. Leaving these at the default zero would expire every crystal, cannon and wall on
        // the arena one tick into the first fight.
        for (var y = 0; y < Height; y++) {
            for (var x = 0; x < Width; x++) {
                _effectTimer[x, y] = NoEffect;
            }
        }
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

    // ---------------------------------------------------------------- spell fields on the floor

    /// <summary>
    /// The timer value that means "this cell carries no spell effect".
    /// </summary>
    /// <remarks>
    /// Both the initial state and what expiry writes back, so an expired cell is indistinguishable
    /// from one that never had an effect — which is what stops a cell being expired twice.
    /// </remarks>
    public const int NoEffect = -1;

    /// <summary>
    /// The one effect kind that reverts to <see cref="CombatTerrain.Crystal"/> instead of
    /// <see cref="CombatTerrain.Open"/> when it lapses.
    /// </summary>
    /// <remarks>
    /// <b>Kind 9 is what a rising Black Slayer paints</b> (<see cref="SlayerRevival.RisenTileEffect"/>),
    /// and the tick's expiry branch singles it out: everything else becomes Open, 9 becomes Crystal.
    /// The revival then resets the cell itself after waiting for the kind to clear, so there the
    /// crystal is transient — but the rule belongs to the tick, not to the revival, and anything
    /// else painting a 9 gets crystal ground back.
    /// </remarks>
    public const int RevertsToCrystalKind = 9;

    /// <summary>
    /// Paints a spell field on a cell — <c>combatgrid_set_tile_effect</c> (CMBTGRID.C:174).
    /// </summary>
    /// <remarks>
    /// <b>THE EFFECT IS THE TERRAIN.</b> The original writes the effect kind straight into the
    /// cell's terrain word with a countdown beside it — there is no overlay and no second field to
    /// consult. So while a field burns, every reader of <see cref="TerrainAt"/> sees the field
    /// rather than the floor, which is exactly why standing in one denies shooting and casting
    /// (<see cref="CombatCapability.DenyingTerrain"/>).
    /// </remarks>
    public void SetTileEffect(int x, int y, CombatTerrain kind, int timer) {
        if (!InBounds(x, y)) {
            return;
        }
        _terrain[x, y] = kind;
        _effectTimer[x, y] = timer;
    }

    /// <summary>Ticks remaining on a cell's effect, or <see cref="NoEffect"/>.</summary>
    public int EffectTimerAt(int x, int y) => InBounds(x, y) ? _effectTimer[x, y] : NoEffect;

    /// <summary>
    /// Ages one cell's effect — <c>combatgrid_tick_tile_effect</c> (CMBTGRID.C:159).
    /// </summary>
    /// <returns>Whether the effect lapsed on this tick.</returns>
    /// <remarks>
    /// <b>A cell at <see cref="NoEffect"/> is not touched at all</b>, which is what makes it safe to
    /// sweep the whole grid: authored terrain never carries a timer, so the crystals, cannons and
    /// walls of a trap puzzle are invisible to this.
    ///
    /// <para><b>Zero is the last tick, not an expired one.</b> The original tests <c>== 0</c> and
    /// expires there, so a field painted with a timer of N burns for N+1 ticks. Decrementing first
    /// and testing after would cost every field one tick.</para>
    /// </remarks>
    public bool TickTileEffect(int x, int y) {
        if (!InBounds(x, y) || _effectTimer[x, y] < 0) {
            return false;
        }

        if (_effectTimer[x, y] > 0) {
            _effectTimer[x, y]--;
            return false;
        }

        _effectTimer[x, y] = NoEffect;
        _terrain[x, y] = (int)_terrain[x, y] == RevertsToCrystalKind
            ? CombatTerrain.Crystal
            : CombatTerrain.Open;
        return true;
    }

    /// <summary>
    /// Ages every cell that carries an effect — the sweep half of
    /// <c>cspell_tick_damage_terrain</c> (CSPELL.C:2427).
    /// </summary>
    /// <returns>How many effects lapsed.</returns>
    /// <remarks>
    /// <b>The original skips kinds 0 and 2 before ticking</b>, which is a shortcut rather than a
    /// rule: an Open or OutOfBounds cell cannot be carrying a live effect, because painting one
    /// overwrites the kind. The timer check below is the same guard stated once, and it is the
    /// honest one — a cell's kind is not what decides whether it is burning, its timer is.
    /// </remarks>
    public int TickTileEffects() {
        var lapsed = 0;
        for (var y = 0; y < Height; y++) {
            for (var x = 0; x < Width; x++) {
                if (TickTileEffect(x, y)) {
                    lapsed++;
                }
            }
        }
        return lapsed;
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
