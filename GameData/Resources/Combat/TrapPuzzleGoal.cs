namespace GameData.Resources.Combat;

using System.Collections.Generic;

/// <summary>
/// When a trap puzzle is over — <c>combatgrid_actor_past_terr6_row</c> and its two companions
/// (<c>SRC/COMBAT/GRID/CMBTGRID.C</c>).
/// </summary>
public static class TrapPuzzleGoal {
    /// <summary>Grid columns the scan walks.</summary>
    public const int Columns = 8;

    /// <summary>Grid rows the scan walks.</summary>
    public const int Rows = 13;

    /// <summary>
    /// Whether the puzzle has an exit at all.
    /// </summary>
    /// <remarks>
    /// <b>Callers must ask this first.</b> See <see cref="ExitRow"/> for why: on a grid with no exit
    /// the reached-the-exit test answers yes immediately, so using it alone turns every ordinary
    /// encounter into an instantly-solved puzzle. The original keeps this as a separate function for
    /// exactly that reason.
    /// </remarks>
    public static bool HasExit(CombatGrid grid) {
        if (grid == null) {
            return false;
        }
        for (var x = 0; x < Columns; x++) {
            for (var y = 0; y < Rows; y++) {
                if (grid.TerrainAt(x, y) == CombatTerrain.Exit) {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// The row the party has to reach.
    /// </summary>
    /// <remarks>
    /// <b>The scan is column-major and keeps going.</b> Each column that contains an exit tile
    /// overwrites the answer, so what comes back is the exit row of the <i>last</i> column that has
    /// one — not the first found, and not the nearest. On the shipped puzzles the exit tiles sit on
    /// one row so it makes no difference, but a mod that staggers them would find the rule is
    /// "whichever the rightmost exit is on".
    ///
    /// <para><b>With no exit tile anywhere this is row 0</b>, which every actor on the grid is at or
    /// past — hence <see cref="HasExit"/>.</para>
    /// </remarks>
    public static int ExitRow(CombatGrid grid) {
        var row = 0;
        if (grid == null) {
            return row;
        }
        for (var x = 0; x < Columns; x++) {
            for (var y = 0; y < Rows; y++) {
                if (grid.TerrainAt(x, y) == CombatTerrain.Exit) {
                    row = y;

                    break;
                }
            }
        }
        return row;
    }

    /// <summary>
    /// Whether the party has got out.
    /// </summary>
    /// <param name="partyMemberRows">The grid row of each <i>party member</i> on the field.</param>
    /// <remarks>
    /// <b>It is a row threshold, not the exit tile.</b> Reaching or passing the exit's row anywhere
    /// along it ends the puzzle — nobody has to stand on the marked cell. A port that tests the tile
    /// would leave puzzles unsolvable wherever the intended path arrives beside the exit rather than
    /// on it.
    ///
    /// <para><b>One member is enough</b>, and only party members count: the original filters the
    /// combatant list on a non-zero character slot, so monsters wandering past the line do not end
    /// anything.</para>
    /// </remarks>
    public static bool PartyIsOut(CombatGrid grid, IEnumerable<int> partyMemberRows) {
        if (partyMemberRows == null) {
            return false;
        }
        int exitRow = ExitRow(grid);
        foreach (int row in partyMemberRows) {
            if (row >= exitRow) {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// The facing each cannon terrain is drawn at, in degrees.
    /// </summary>
    /// <remarks>
    /// All four render the <b>same model</b> and differ only by this yaw — so a cannon is one asset
    /// turned four ways, not four pieces of art. The degrees are what the renderer is passed
    /// directly; the compass words on <see cref="CombatTerrain"/> are an interpretation of them and
    /// depend on which way the grid's yaw runs.
    /// </remarks>
    public static int CannonFacingDegrees(CombatTerrain terrain) => terrain switch {
        CombatTerrain.CannonWest => 90,
        CombatTerrain.CannonEast => 270,
        CombatTerrain.CannonNorth => 0,
        CombatTerrain.CannonSouth => 180,
        _ => 0,
    };

    /// <summary>Whether a terrain value is one of the four cannon facings.</summary>
    public static bool IsCannon(CombatTerrain terrain) =>
        terrain >= CombatTerrain.CannonWest && terrain <= CombatTerrain.CannonSouth;
}
