namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// The lines the arena draws over its playable tiles.
/// </summary>
/// <remarks>
/// Measured against the original before being written: encounter 347's frames carry 1888 pixels of
/// palette pen 224 spanning the whole 640x400 viewport, so this is the tactical GRID and not a
/// perimeter — which is the thing the cases below are here to keep true.
/// </remarks>
public class CombatGridOutlineTests {
    private static CombatGrid Underground() => new(underground: true);

    [Fact]
    public void ItDrawsAGridOverThePlayableArea_NotJustItsOutline() {
        var grid = new CombatGrid();
        List<CombatGridOutline.Edge> edges = CombatGridOutline.Edges(grid);

        // Both passes emit BOTH edges of every run, so every playable tile ends up with all four of
        // its edges. A perimeter-only reading would give a handful of lines; the real thing gives
        // two per row plus two per column.
        Assert.True(edges.Count >= CombatGrid.Height * 2,
            $"expected at least two edges per row, got {edges.Count}");

        // Horizontal edges at y = row and y = row-1 for the same run: that pairing is what fills the
        // interior in, and dropping either one turns this into an outline.
        Assert.Contains(edges, e => e.Y1 == 0 && e.Y2 == 0 && e.X1 != e.X2);
        Assert.Contains(edges, e => e.Y1 == -1 && e.Y2 == -1 && e.X1 != e.X2);
    }

    [Fact]
    public void AWalledCellBreaksTheRun_AndAnOccupiedOneDoesNot() {
        var grid = new CombatGrid();
        Assert.False(CombatGridOutline.BreaksRun(grid, 3, 3));

        grid.SetOccupied(3, 3, true);
        // *** An element standing on a cell must not cut the grid. *** The original tests the two
        // impassable KINDS by number rather than asking whether the cell is blocked, and a crystal
        // or a combatant makes a cell blocked without changing its terrain.
        Assert.False(CombatGridOutline.BreaksRun(grid, 3, 3));

        grid.SetTerrain(3, 3, CombatTerrain.Wall);
        Assert.True(CombatGridOutline.BreaksRun(grid, 3, 3));
        grid.SetTerrain(3, 3, CombatTerrain.OutOfBounds);
        Assert.True(CombatGridOutline.BreaksRun(grid, 3, 3));
    }

    [Fact]
    public void OffGridIsTreatedAsAWall_SoTheRunsStopAtTheEdge() {
        var grid = new CombatGrid();
        Assert.True(CombatGridOutline.BreaksRun(grid, -1, 0));
        Assert.True(CombatGridOutline.BreaksRun(grid, CombatGrid.Width, 0));
        Assert.True(CombatGridOutline.BreaksRun(grid, 0, CombatGrid.Height));

        // The original's inner loops have no bound and rely on the out-of-range read to stop them.
        // Ours stop at the edge; this is the case that would hang or run away without that.
        List<CombatGridOutline.Edge> edges = CombatGridOutline.Edges(grid);
        foreach (CombatGridOutline.Edge e in edges) {
            Assert.InRange(e.X1, 0, CombatGrid.Width);
            Assert.InRange(e.X2, 0, CombatGrid.Width);
        }
    }

    [Fact]
    public void TheUndergroundArenaDrawsNoGridOverItsWalledRows() {
        List<CombatGridOutline.Edge> edges = CombatGridOutline.Edges(Underground());

        // Rows 7..12 are walled off underground (Load_grid), so no horizontal run may start there.
        foreach (CombatGridOutline.Edge e in edges) {
            if (e.Y1 == e.Y2 && e.X1 != e.X2) {
                Assert.True(e.Y1 < CombatGrid.UndergroundPlayableRows,
                    $"a horizontal run at y={e.Y1} is inside the walled rows");
            }
        }
    }

    [Fact]
    public void ACornerSitsHalfACellFromTheCentreOfItsOwnTile() {
        const int cell = 300;
        (int cx, int cy) = CombatArenaPlacement.CellOffset(2, 3, cell);
        (int nx, int ny) = CombatGridOutline.CornerOffset(2, 3, cell);

        // *** The grid must land on the tiles the combatants stand on. *** Deriving the corner from
        // CellOffset rather than from the original's own -0x4b0 / +0xc80 literals is what keeps the
        // two from drifting apart; this pins the relationship that derivation asserts.
        Assert.Equal(cx - (cell / 2), nx);
        Assert.Equal(cy + (cell / 2), ny);
    }

    [Fact]
    public void ThePenComesFromTheZoneTableAndIsIndexedFromOne() {
        var data = new GridData("GRID.DAT");
        data.ZoneBorderPens.AddRange(new[] { 224, 234, 187 });

        // Load_grid seeks (zoneNumber - 1) * 2, so zone 1 is entry 0. Off by one here would give
        // every zone its neighbour's colour, which is exactly the kind of wrong that looks right.
        Assert.Equal(224, CombatGridOutline.PenFor(data, 1));
        Assert.Equal(234, CombatGridOutline.PenFor(data, 2));
        Assert.Equal(187, CombatGridOutline.PenFor(data, 3));

        // A zone the table cannot answer for means no grid, not a wrong-coloured one.
        Assert.Null(CombatGridOutline.PenFor(data, 0));
        Assert.Null(CombatGridOutline.PenFor(data, 4));
        Assert.Null(CombatGridOutline.PenFor(null, 1));
    }

    [Fact]
    public void ZoneOnesPenIsTwoTwoFour_WhichIsWhatTheOriginalDraws() {
        // Pinned against the shipped table AND against a measurement of the running original: the
        // lines in encounter 347's frames are RGB (52,130,69), which is 6-bit (13,32,17), which is
        // palette index 224 exactly in every zone palette.
        var shipped = new GridData("GRID.DAT");
        shipped.ZoneBorderPens.AddRange(
            new[] { 224, 224, 224, 224, 224, 234, 224, 225, 187, 152, 224, 173 });

        Assert.Equal(GridData.ZoneCount, shipped.ZoneBorderPens.Count);
        Assert.Equal(224, CombatGridOutline.PenFor(shipped, 1));
        Assert.Equal(173, CombatGridOutline.PenFor(shipped, GridData.ZoneCount));
    }
}
