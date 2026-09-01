namespace GameData.Resources.Combat;

using System;

/// <summary>
/// Writing the world under the party into the arena's grid — <c>arena_buildAndPruneGrid</c> @0x2e749.
/// </summary>
/// <remarks>
/// <b>An arena is not "the 8x13 grid minus scenery". It is that, minus every cell you could not
/// reach most of the rest from.</b> The original runs three passes when a fight starts: build the
/// grid from the world, count the free cells, then wall off every free cell that fails a
/// connectivity test. Without the third pass a pocket behind a rock stays walkable and a combatant
/// can strand itself in it.
///
/// <para>The build half is deliberately a <b>callback</b> rather than a world query. The original
/// has two builders that share nothing: above ground it ray-tests the world-item list
/// (<c>arena_buildGridFromWorldItems</c> @0x2e520), and underground it <i>renders the scene and
/// reads pixels</i> (<c>arena_buildGridByRenderProbe</c> @0x2e671) — flooding the buffer with a
/// sentinel colour, drawing the world over it, and asking whether each cell's projected point still
/// shows floor. The second cannot be ported by translating it; there is no framebuffer to sample.
/// Taking "is this cell open" as a function keeps that decision with the caller, where the renderer
/// is.</para>
/// </remarks>
public static class ArenaLayout {
    /// <summary>
    /// Lay the world into <paramref name="grid"/> and prune what it isolates.
    /// </summary>
    /// <param name="grid">The arena grid, already constructed with its walls.</param>
    /// <param name="isOpenAt">Whether a fight can happen on this cell's ground.</param>
    /// <param name="reaches">
    /// Whether a combatant on the first cell can walk to the second. Omitted means no pruning —
    /// the build still runs, which is the useful half on its own.
    /// </param>
    /// <returns>How many cells are open when it is done.</returns>
    public static int Build(CombatGrid grid, Func<int, int, bool> isOpenAt,
        Func<int, int, int, int, bool> reaches = null) {
        if (grid == null) {
            throw new ArgumentNullException(nameof(grid));
        }
        if (isOpenAt == null) {
            throw new ArgumentNullException(nameof(isOpenAt));
        }

        // *** THIS ONLY EVER ADDS BLOCKING. *** A cell the grid already walls off is left exactly as
        // it is — the two far corners, and underground the back rows, are laid down by the grid's
        // own construction (the original's Load_grid) and this must not widen the arena by painting
        // open ground over them. Leaving them also preserves the DISTINCTION between a wall and a
        // pushable, which rewriting every cell to Open-or-OutOfBounds would flatten.
        var free = 0;
        for (var x = 0; x < CombatGrid.Width; x++) {
            for (var y = 0; y < CombatGrid.Height; y++) {
                if (grid.IsBlocked(x, y)) {
                    continue;
                }
                bool open = isOpenAt(x, y);
                grid.SetTerrain(x, y, open ? CombatTerrain.Open : CombatTerrain.OutOfBounds);
                if (open) {
                    free++;
                }
            }
        }

        if (reaches == null) {
            return free;
        }

        // *** THE THRESHOLD MOVES AS PRUNING PROCEEDS, AND THAT IS THE ORIGINAL'S. *** It passes the
        // running count, so a cell judged later faces a lower bar than one judged earlier — which
        // makes the result order-dependent, column-major. Computing the count once would wall off a
        // different set. Not obviously deliberate; it is what ships.
        for (var x = 0; x < CombatGrid.Width; x++) {
            for (var y = 0; y < CombatGrid.Height; y++) {
                if (grid.IsBlocked(x, y) || ReachesMostOfGrid(grid, x, y, free, reaches)) {
                    continue;
                }
                grid.SetTerrain(x, y, CombatTerrain.OutOfBounds);
                free--;
            }
        }

        return free;
    }

    /// <summary>
    /// Whether a combatant here could walk to more than half the other open cells —
    /// <c>arena_cellReachesMostOfGrid</c> @0x2fe55.
    /// </summary>
    /// <remarks>
    /// <b>A simple MAJORITY, not full connectivity, and the difference cuts both ways.</b> Demanding
    /// a fully connected region walls off pockets the game keeps; accepting any reachability at all
    /// keeps pockets it walls off. The original's threshold is literally <c>count &gt;&gt; 1</c> and
    /// it stops the moment it passes, so most cells cost only a handful of probes.
    /// </remarks>
    public static bool ReachesMostOfGrid(CombatGrid grid, int x, int y, int openCells,
        Func<int, int, int, int, bool> reaches) {
        var reached = 0;
        for (var cx = 0; cx < CombatGrid.Width; cx++) {
            for (var cy = 0; cy < CombatGrid.Height; cy++) {
                if ((cx == x && cy == y) || grid.IsBlocked(cx, cy)) {
                    continue;
                }
                if (reaches(x, y, cx, cy) && ++reached > openCells / 2) {
                    return true;
                }
            }
        }
        return false;
    }
}
