namespace GameData.Resources.Combat;

using System.Collections.Generic;

/// <summary>
/// Whether there is enough open ground here to hold a fight at all —
/// <c>combatgrid_tiles_over_thresh</c> (canassa CMBTGRID.C:379).
///
/// <para>Checked BEFORE an encounter fires, by both the combat hotspot and the trap one. If it
/// fails, the trigger simply returns: no fight, no dialog, nothing marked. So an ambush laid on a
/// cliff edge or in a stand of trees never goes off, and a port without this fires encounters in
/// places the arena cannot be laid out.</para>
/// </summary>
/// <remarks>
/// <b>The original has two implementations of this and only one of them is a rule.</b> Above ground
/// it sweeps the arena's footprint through the world and counts cells standing on acceptable
/// terrain. Underground it renders the view, samples a reference pixel at the centre of the screen,
/// and counts how many of the grid's cells project onto a pixel of that colour — reading walkability
/// off the framebuffer.
///
/// <para>The second is a 1993 rendering trick, not a game rule, and it is deliberately NOT ported:
/// the QUESTION it answers ("is this cell standing on open floor") is the same one the sweep asks,
/// and a renderer-independent answer to it is what belongs here. What survives from it is the
/// threshold and the footprint, which are shared.</para>
/// </remarks>
public static class CombatGroundCheck {
    /// <summary>
    /// Open cells needed — <c>count >= 0x18</c>.
    /// </summary>
    /// <remarks>
    /// Out of <see cref="CombatGrid.Width"/> x <see cref="CombatGrid.Height"/> = 104, so a little
    /// under a quarter. It is a "not hemmed in" test rather than a "mostly clear" one.
    /// </remarks>
    public const int MinimumOpenCells = 0x18;

    /// <summary>Whether a cell count clears the bar.</summary>
    public static bool Passes(int openCells) => openCells >= MinimumOpenCells;

    /// <summary>
    /// Cells sampled: the FULL 8 x 13 buffer, both above ground and below.
    /// </summary>
    /// <remarks>
    /// <b>Not the playable area.</b> Underground only
    /// <see cref="CombatGrid.UndergroundPlayableRows"/> of the 13 rows are in play, but the check
    /// sweeps all thirteen either way — it is asking whether the arena's footprint fits, not whether
    /// its playable part does.
    /// </remarks>
    public static int SampledCells => CombatGrid.Width * CombatGrid.Height;

    // ---------------------------------------------------------------- what counts as open

    /// <summary>
    /// World-entity kinds a cell may stand on and still count.
    /// </summary>
    /// <remarks>
    /// <b>An empty cell does not count.</b> The scan has to FIND something and that something has to
    /// be one of these — so a cell over nothing at all fails, which is what keeps the arena off a
    /// void or a drop.
    ///
    /// <para><b>Water is conspicuously absent</b> (<see cref="World.WorldEntityType.Water"/> is 3),
    /// while 2 — the bridge — is present. So a fight can happen on a bridge over a river and not in
    /// the river, which is the distinction the set exists to draw.</para>
    ///
    /// <para><b>It is the WALKABLE set minus one kind, and the one is <see cref="WalkableOnlyKind"/>
    /// — the pit.</b> <c>CheckMoveDestination</c> accepts 0, 1, 2, 14, 15 and 23; this accepts all
    /// of those but 15. So the party can walk onto a pit — that is how they fall in — and cannot
    /// stand on one to fight. Reusing the movement check here would let an arena be laid out across
    /// open pits.</para>
    ///
    /// <para>(Not to be confused with the ROAD-travel gate, <c>worldmove_prox_query_at_cell</c>,
    /// which accepts 1 and 2 only. There are three different "can you be here" sets in the world
    /// code and they differ by one or two kinds each.)</para>
    ///
    /// <para><b>Confirmed against the binary 2026-09-01.</b> <c>worldItem_isOpenGroundAt</c>
    /// @0x2d9aa is this predicate: it collides a position against the world-item list and switches
    /// on the hit item's type-descriptor kind byte, accepting exactly these five. Two details of it
    /// are worth having here — <b>an empty position FAILS</b> (the scan must find something, which
    /// is what keeps an arena off a void), and the switch reads the byte from the
    /// <b>type descriptor</b> reached through <c>worldItemIndexToPtr</c>, not from the item
    /// instance.</para>
    /// </remarks>
    public static IReadOnlyList<int> OpenGroundKinds { get; } = new[] {
        (int)World.WorldEntityType.Ground,   // 0
        (int)World.WorldEntityType.Road,     // 1
        2,                                   // the bridge; named bridge* in the data, unnamed here
        14,                                  // terrain, still unnamed
        (int)World.WorldEntityType.Door,     // 23
    };

    /// <summary>
    /// The one kind you may walk onto but not fight on — <see cref="World.WorldEntityType.Pit"/>.
    /// </summary>
    /// <remarks>
    /// Named because the whole difference between this check and the movement one is this single
    /// value, and a set-vs-set comparison is the only place that is visible.
    /// </remarks>
    public const int WalkableOnlyKind = (int)World.WorldEntityType.Pit;

    /// <inheritdoc cref="OpenGroundKinds"/>
    public static bool IsOpenGround(int worldEntityKind) {
        for (var i = 0; i < OpenGroundKinds.Count; i++) {
            if (OpenGroundKinds[i] == worldEntityKind) {
                return true;
            }
        }
        return false;
    }

    // ---------------------------------------------------------------- where it samples

    /// <summary>
    /// How far in front of the party the arena's footprint begins, in world units.
    /// </summary>
    /// <remarks>
    /// A bare constant in the original (<c>0xc80</c>) with nothing to derive it from, unlike
    /// <see cref="HalfWidthOf"/>.
    /// </remarks>
    public const int ForwardOffset = 0xc80;

    /// <summary>
    /// Half the footprint's width — what centres it on the party's line of sight.
    /// </summary>
    /// <remarks>
    /// The original writes <c>0x4b0</c> (1200), and with the shipped cell size of 300 that is
    /// exactly half of eight cells. Derived here rather than restated, so a different cell size
    /// stays centred.
    /// </remarks>
    public static int HalfWidthOf(int cellSize) => CombatGrid.Width * cellSize / 2;

    /// <summary>
    /// Where one cell is sampled, as an offset from the party BEFORE the party's heading is applied.
    /// </summary>
    /// <param name="column">0 .. <see cref="CombatGrid.Width"/>-1.</param>
    /// <param name="row">0 .. <see cref="CombatGrid.Height"/>-1.</param>
    /// <param name="cellSize">The arena's cell size — <c>StartData.CombatGridCellSize</c>.</param>
    /// <returns>
    /// <c>Across</c> is positive toward the party's right, <c>Away</c> positive in front of them.
    /// </returns>
    /// <remarks>
    /// <b>Centred in X, but the NEAR EDGE in Y</b>, and that is the original's own inconsistency
    /// rather than a rounding choice here. Combatant placement puts cell (x, y) at
    /// <c>x*cell + cell/2 - halfWidth</c> across and <c>y*cell + cell/2 + ForwardOffset</c> away —
    /// both centred. The sweep folds the half-cell into its sideways step (it moves
    /// <c>halfWidth - cell/2</c>) but not into its forward one, so every sample sits half a cell
    /// nearer the party than the combatant that will stand there.
    ///
    /// <para><b>The sweep also walks the columns in reverse</b>, right to left. It samples the same
    /// eight positions, and since the result is a COUNT the order cannot matter — which is why this
    /// gives them left to right instead of reproducing a walk.</para>
    /// </remarks>
    public static (int Across, int Away) SampleOffset(int column, int row, int cellSize) => (
        (column * cellSize) + (cellSize / 2) - HalfWidthOf(cellSize),
        (row * cellSize) + ForwardOffset);
}
