namespace GameData.Resources.Combat;

/// <summary>
/// What holds a trap puzzle's crystals together, and what breaks them — <c>CMBTGRID.C</c>'s
/// <c>combatgrid_push_back_actor</c> and the probes it leans on.
///
/// <para><b>A crystal survives by having a neighbour.</b> After a push, any crystal with no
/// continuing run beside it is destroyed. That is the puzzle: you are not pushing crystals to a
/// place, you are pushing them until the chain comes apart.</para>
/// </summary>
public static class CrystalChain {
    /// <summary>The two element ids a crystal is recorded as.</summary>
    public const int FirstCrystalId = 7;

    /// <summary>The two element ids a crystal is recorded as.</summary>
    public const int LastCrystalId = 8;

    /// <summary>What a destroyed element's id becomes.</summary>
    public const int WreckElementId = 0x28;

    /// <summary>
    /// Terrain a destroyed tile is left as.
    /// </summary>
    /// <remarks>
    /// A kind of its own, <b>not</b> the pushable kind — but the tile handler answers for the two
    /// together in one branch, so a wreck behaves like a pushable without being one. Worth keeping
    /// distinct: they share behaviour at one call site, not identity.
    /// </remarks>
    public const int WreckTerrain = 14;

    /// <summary>
    /// Whether an <b>element id</b> is a crystal.
    /// </summary>
    /// <remarks>
    /// Deliberately not called <c>IsCrystal</c>: <see cref="CombatGrid.IsCrystal"/> already means
    /// "is this tile crystal <i>ground</i>", and element ids and terrain kinds are different spaces
    /// — a crystal is element 7 or 8 but writes terrain 3. Two same-named predicates over the two
    /// spaces is exactly the confusion the builder's own documentation warns about.
    /// </remarks>
    public static bool IsCrystalElement(int elementId) =>
        elementId >= FirstCrystalId && elementId <= LastCrystalId;

    /// <summary>
    /// Whether a tile can carry the run.
    /// </summary>
    /// <param name="kind">
    /// The element id being traced, or <c>-1</c> for "anything that is not a crystal".
    /// </param>
    /// <remarks>
    /// <b>The tile must be crystal GROUND, and either empty or holding the kind being traced.</b>
    /// Both halves matter: crystal ground with a foreign element on it breaks the run just as surely
    /// as ordinary ground does.
    /// </remarks>
    public static bool TileCarriesRun(TrapPuzzle puzzle, int x, int y, int kind) {
        if (puzzle?.Grid == null || puzzle.Grid.TerrainAt(x, y) != CombatTerrain.Crystal) {
            return false;
        }

        TrapGridElement element = puzzle.ElementAt(x, y);
        if (element == null) {
            return true;
        }

        return kind == -1 ? !IsCrystalElement(element.ElementId) : element.ElementId == kind;
    }

    /// <summary>
    /// Whether the run carries on from this tile in any direction.
    /// </summary>
    /// <remarks>
    /// <b>All eight neighbours count, diagonals included.</b> A crystal held only by a corner is
    /// still held — treating the run as orthogonal would destroy chains the original keeps, and
    /// diagonal links are how the shipped puzzles snake across the grid.
    /// </remarks>
    public static bool RunContinues(TrapPuzzle puzzle, int x, int y, int kind) {
        for (var dx = -1; dx <= 1; dx++) {
            for (var dy = -1; dy <= 1; dy++) {
                if (dx == 0 && dy == 0) {
                    continue;
                }
                if (TileCarriesRun(puzzle, x + dx, y + dy, kind)) {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Whether a crystal left at this tile is destroyed for want of a neighbour.
    /// </summary>
    /// <remarks>
    /// Checked at <b>both</b> ends of a push independently — where the crystal came from and where
    /// it ended up — so one shove can destroy neither, one, or both.
    /// </remarks>
    public static bool IsolationDestroys(TrapPuzzle puzzle, int x, int y, int kind) =>
        !RunContinues(puzzle, x, y, kind);

    /// <summary>
    /// <b>A crystal with nowhere at all to go destroys itself and takes one neighbour with it.</b>
    /// </summary>
    /// <remarks>
    /// One, not all of them: the original scans the surrounding cells and stops at the first element
    /// of the same kind it finds. So a shove into a dead end costs two crystals, not a cascade —
    /// modelling it as "destroy every adjacent crystal" would make dead ends far more powerful than
    /// they are.
    ///
    /// <para>The scan includes the centre cell, but the crystal there has already become a wreck by
    /// then and no longer matches the kind, so it cannot pick itself twice.</para>
    /// </remarks>
    public static int NeighboursTakenWhenBoxedIn => 1;
}
