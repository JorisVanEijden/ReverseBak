namespace GameData.Resources.Combat;

using System.Collections.Generic;

/// <summary>
/// The lines the arena draws over its playable tiles — <c>combatgrid_draw_terrain_walls</c>
/// (canassa CMBTGRID.C:741), coloured from <see cref="GridData.ZoneBorderPens"/>.
/// </summary>
/// <remarks>
/// <b>It is a GRID, not a perimeter</b>, despite what "terrain walls" suggests. Each pass emits BOTH
/// edges of every run — top and bottom of each horizontal run, left and right of each vertical one —
/// so every playable tile gets all four of its edges and the result is the tactical grid drawn over
/// the arena floor. Measured on the original (encounter 347, 2026-09-06): 1888 pixels of the zone's
/// border pen spanning x 26..613, y 22..183 of the 640x400 frame, which is the whole viewport rather
/// than one outline.
///
/// <para><b>The runs are the point, not an optimisation to undo.</b> Emitting per-tile edges instead
/// would draw the same picture out of four times as many segments; keeping the runs keeps the line
/// count near what the original submits.</para>
///
/// <para><b>Bounded where the original is not.</b> Its inner loops walk <c>col</c>/<c>row</c> with no
/// limit and lean on the out-of-range read to stop them. That is a property of its bounds-checking
/// accessor, not something to reproduce — these loops stop at the grid edge.</para>
/// </remarks>
public static class CombatGridOutline {
    /// <summary>An edge, in GRID-CORNER coordinates: corner (x, y) is the tile grid's lattice.</summary>
    public readonly struct Edge {
        public Edge(int x1, int y1, int x2, int y2) {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
        }

        public int X1 { get; }
        public int Y1 { get; }
        public int X2 { get; }
        public int Y2 { get; }
    }

    /// <summary>
    /// Whether a cell stops a run. The original tests the two impassable kinds by number rather than
    /// asking whether the cell is blocked — an element standing on a cell does NOT break the grid.
    /// </summary>
    public static bool BreaksRun(CombatGrid grid, int x, int y) {
        if (grid == null || !CombatGrid.InBounds(x, y)) {
            return true;
        }
        CombatTerrain t = grid.TerrainAt(x, y);
        return t == CombatTerrain.OutOfBounds || t == CombatTerrain.Wall;
    }

    /// <summary>Every line the arena draws, in grid-corner coordinates.</summary>
    public static List<Edge> Edges(CombatGrid grid) {
        var edges = new List<Edge>();
        if (grid == null) {
            return edges;
        }

        for (var row = 0; row < CombatGrid.Height; row++) {
            var col = 0;
            while (col < CombatGrid.Width) {
                int runStart = col;
                while (col < CombatGrid.Width && !BreaksRun(grid, col, row)) {
                    col++;
                }
                if (col != runStart) {
                    edges.Add(new Edge(runStart, row, col, row));
                    edges.Add(new Edge(runStart, row - 1, col, row - 1));
                } else {
                    col++;
                }
            }
        }

        for (var col = 0; col < CombatGrid.Width; col++) {
            var row = 0;
            while (row < CombatGrid.Height) {
                int runStart = row;
                while (row < CombatGrid.Height && !BreaksRun(grid, col, row)) {
                    row++;
                }
                if (row != runStart) {
                    edges.Add(new Edge(col, runStart - 1, col, row - 1));
                    edges.Add(new Edge(col + 1, runStart - 1, col + 1, row - 1));
                } else {
                    row++;
                }
            }
        }

        return edges;
    }

    /// <summary>
    /// Where a grid CORNER sits, as an offset from the party before its heading is applied — the
    /// same frame <see cref="CombatArenaPlacement.CellOffset"/> works in.
    /// </summary>
    /// <remarks>
    /// Derived from <see cref="CombatArenaPlacement.CellOffset"/> rather than from the original's
    /// literals: a cell's CENTRE is half a cell in from its near corner across, and half a cell back
    /// from its far corner away. Re-deriving <c>-0x4b0</c> and <c>+0xc80</c> here would be a second
    /// copy of the arena's placement constants, and those two have to agree or the grid floats off
    /// the tiles the combatants stand on.
    /// </remarks>
    public static (int Across, int Away) CornerOffset(int cornerX, int cornerY, int cellSize) {
        (int across, int away) = CombatArenaPlacement.CellOffset(cornerX, cornerY, cellSize);
        return (across - (cellSize / 2), away + (cellSize / 2));
    }

    /// <summary>The zone's border pen, or null when the table cannot answer for that zone.</summary>
    /// <remarks>
    /// Indexed <c>zoneNumber - 1</c>, as <c>Load_grid</c> seeks. A zone outside the table is not an
    /// error worth throwing over — it means no grid rather than a wrong-coloured one.
    /// </remarks>
    public static int? PenFor(GridData grid, int zoneNumber) {
        if (grid?.ZoneBorderPens == null) {
            return null;
        }
        int index = zoneNumber - 1;
        return index >= 0 && index < grid.ZoneBorderPens.Count ? grid.ZoneBorderPens[index] : null;
    }
}
