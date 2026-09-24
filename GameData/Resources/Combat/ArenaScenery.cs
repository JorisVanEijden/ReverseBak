namespace GameData.Resources.Combat;

using System;
using System.Collections.Generic;

/// <summary>
/// Walls the arena cells that scenery stands on — <c>combatgrid_build_combatant_list</c>
/// (CMBTGRID.C:390), the part after the ground sweep.
/// </summary>
/// <remarks>
/// <b>Runs AFTER the prune, so a tree never changes which pockets are walled off.</b> The original's
/// sweep and prune both live inside <c>combatgrid_tile_fx_init_pass</c> (CMBTGRID.C:679-713), which
/// this routine calls first; the scenery walls land on a grid that is already pruned.
///
/// <para><b>Only the party's own map tile.</b> The first list is
/// <c>g_apCombat_zone_actor_lists[0]</c>, the tile the party stands on (WCURSOR.C:287 compares it
/// to <c>pos / 64000</c>). A tree across the tile edge is drawn but does not wall its cell.</para>
///
/// <para>Not ported: the second list, <c>g_pVisible_entry_pool</c>, which holds the time-of-day
/// spawns (ACTSPAWN.C) rather than scenery.</para>
/// </remarks>
public static class ArenaScenery {
    /// <summary>A placed world object: its shape kind and world position.</summary>
    public readonly record struct Placement(int Kind, long X, long Y);

    /// <summary><c>g_combatant_table</c> holds 15 entries; the walk stops when it is full.</summary>
    public const int TableCapacity = 0xf;

    /// <summary>
    /// Wall every cell a blocking object stands on. <paramref name="tileObjects"/> must be in the
    /// tile's file order: the first object to claim a cell keeps it, and the table fills up.
    /// </summary>
    /// <returns>The cells walled.</returns>
    public static List<(int X, int Y)> Wall(CombatGrid grid, IEnumerable<Placement> tileObjects,
        long partyX, long partyY, int heading, int cellSize) {
        var claimed = new List<(int X, int Y)>();
        var walled = new List<(int X, int Y)>();
        foreach (Placement p in tileObjects) {
            if (!Listed(p.Kind)) {
                continue;
            }
            (int x, int y)? cell = WorldToCell(p.X - partyX, p.Y - partyY, heading, cellSize);
            if (cell == null) {
                continue;
            }
            (int x, int y) = cell.Value;
            bool blocks = Blocks(p.Kind);
            // A blocker in the wedge nearest the camera is left out, and takes no table slot —
            // except a StoneSlab (kind 0x1d).
            if (blocks && InNearWedge(x, y) && p.Kind != 0x1d) {
                continue;
            }
            if (claimed.Contains((x, y))) {
                continue;
            }
            claimed.Add((x, y));
            if (blocks) {
                grid.SetTerrain(x, y, CombatTerrain.OutOfBounds);
                walled.Add((x, y));
            }
            if (claimed.Count >= TableCapacity) {
                break;
            }
        }
        return walled;
    }

    /// <summary>
    /// <c>combatgrid_world_to_view_2d</c>: a world offset from the party to an arena cell, or null.
    /// </summary>
    /// <remarks>
    /// <b>The offset is cut to 16 bits before the multiply</b> — <c>r3d_imul_full32</c> takes
    /// <c>int</c>, which is 16-bit in Borland C — so an object about 65,536 units away lands as if it
    /// were near. That is the shipped behaviour and is kept.
    /// </remarks>
    public static (int X, int Y)? WorldToCell(long dx, long dy, int heading, int cellSize) {
        short sx = unchecked((short)dx);
        short sy = unchecked((short)dy);
        double theta = unchecked((short)-heading) / 65536.0 * 2.0 * Math.PI;
        long cos = (long)Math.Round(Math.Cos(theta) * 0x4000);
        long sin = (long)Math.Round(Math.Sin(theta) * 0x4000);
        long vx = ((sx * cos) - (sy * sin)) >> 14;
        long vy = ((sy * cos) + (sx * sin)) >> 14;

        int x = vx > 0x4000 || vx < -0x4000 ? -1 : ((int)vx + 0x4b0) / cellSize;
        int y = vy > 0x4000 || vy < -0x4000 ? -1 : ((int)vy - 0xc80) / cellSize;
        if (x < 0 || x >= CombatGrid.Width || y < 0 || y >= CombatGrid.Height) {
            return null;
        }
        return (x, y);
    }

    /// <summary><c>combatgrid_obj_kind_prox_actv</c>: kinds that take a table slot.</summary>
    public static bool Listed(int kind) => kind switch {
        5 or 6 or 12 or 13 or 16 or 17 or 18 or 19 or 21 or 22 or 24 or 25 or 26 or 27 or 28
            or 29 or 30 or 31 or 34 or 35 or 37 or 41 => true,
        _ => false,
    };

    /// <summary><c>combatgrid_cmbt_block_kind</c>: kinds that wall their cell.</summary>
    public static bool Blocks(int kind) => kind switch {
        5 or 18 or 21 or 22 or 26 or 27 or 28 or 29 or 30 or 31 or 37 => true,
        _ => false,
    };

    /// <summary><c>combatgrid_tile_cam_near_wedge</c>.</summary>
    public static bool InNearWedge(int x, int y) {
        y -= 6;
        return 8 - x > y && y < x;
    }
}
