namespace GameData.Resources.Combat;

using System.Collections.Generic;

/// <summary>
/// Cannons on a trap-puzzle grid, and when one has line on an actor.
/// </summary>
/// <remarks>
/// <b>Cannons DO fire, and this task previously concluded they do not.</b> The earlier sweep looked
/// for reads of terrain 10-13 and found only three — the loader, the writer and the renderer's yaw
/// pick — and closed the item as "scenery that blocks a tile and points somewhere". The read it
/// could not find is a comparison against a PARAMETER: <c>combatgrid_tile_passable_check</c> tests
/// <c>terrain == required_terrain</c>, and the constants live at its only call site,
/// <c>combatgrid_step_search</c>, which passes them as DIRECTION codes. Nothing in either function
/// mentions a cannon, so a search by terrain id cannot turn it up.
///
/// <para><b>The direction code and the cannon's terrain id are the same number.</b> That is the
/// trick the original is playing: scanning west looks for terrain 11, east for 10, north for 12,
/// south for 13 — each scan hunting the one cannon kind that could be pointing back down it.</para>
///
/// <para>The firing itself is <c>cspell_apply_step_tile_spell(mover, 4, 0x14, -2)</c>: it builds a
/// throwaway caster standing ON THE CANNON'S TILE and resolves spell 4 at intensity 20 against the
/// walker. Placing the stub at the cannon rather than at the victim is what makes any
/// distance-dependent part of the effect measure from the cannon, so this is a shot from there and
/// not damage applied in place.</para>
/// </remarks>
public static class CannonLine {
    /// <summary>A cannon that has line on the tile that was asked about.</summary>
    public readonly struct Shot {
        internal Shot(CombatTerrain cannon, int x, int y) {
            Cannon = cannon;
            X = x;
            Y = y;
        }

        /// <summary>Which of the four cannon terrains it is.</summary>
        public CombatTerrain Cannon { get; }

        /// <summary>The cannon's tile — where the original stands its stub caster.</summary>
        public int X { get; }

        /// <inheritdoc cref="X"/>
        public int Y { get; }
    }

    /// <summary>The spell a cannon casts.</summary>
    public const int SpellId = 4;

    /// <summary>The intensity it casts at.</summary>
    public const int Intensity = 0x14;

    /// <summary>
    /// The element id that does <b>not</b> stop a cannon's line, unlike every other element.
    /// </summary>
    /// <remarks>
    /// The scan stops at any element whose id is not this one. Both 9 and 10 are pushables, so the
    /// two halves of one pair behave differently here — 9 blocks a shot and 10 does not. That
    /// asymmetry is the original's; it is not a tidy "pushables are transparent" rule, and it is why
    /// this is an element-id test rather than a terrain test.
    /// </remarks>
    public const int TransparentElementId = 10;

    /// <summary>Which cannon terrain a scan in each direction is hunting, keyed by that terrain's id.</summary>
    private static readonly (int Dx, int Dy, CombatTerrain Wanted)[] Scans = {
        (-1, 0, CombatTerrain.CannonEast),   // 11
        (1, 0, CombatTerrain.CannonWest),    // 10
        (0, -1, CombatTerrain.CannonNorth),  // 12
        (0, 1, CombatTerrain.CannonSouth),   // 13
    };

    /// <summary>
    /// Every cannon that can see <paramref name="x"/>,<paramref name="y"/> — checked after each step
    /// of a walk, so all four directions fire in the same instant if all four have line.
    /// </summary>
    /// <param name="occupiedByLiveCombatant">
    /// Whether a tile holds a LIVING combatant. Dead ones do not block: the original's blocking test
    /// returns false for a corpse, so a body on the floor is no cover.
    /// </param>
    /// <remarks>
    /// <b>The scan starts on the NEXT tile, so an actor standing on a cannon is not shot by it.</b>
    ///
    /// <para>What stops a scan, in the original's order: the cannon it wants (a shot), a living
    /// combatant, or an element that is not <see cref="TransparentElementId"/>. Everything else is
    /// walked through — including empty crystal ground, which does not shield.</para>
    /// </remarks>
    public static IReadOnlyList<Shot> ShotsOn(TrapPuzzle puzzle, int x, int y,
        System.Func<int, int, bool> occupiedByLiveCombatant = null) {
        var shots = new List<Shot>();
        if (puzzle == null) {
            return shots;
        }

        foreach ((int dx, int dy, CombatTerrain wanted) in Scans) {
            int tx = x;
            int ty = y;
            while (true) {
                tx += dx;
                ty += dy;
                if (!CombatGrid.InBounds(tx, ty)) {
                    break;
                }
                if (puzzle.Grid.TerrainAt(tx, ty) == wanted) {
                    shots.Add(new Shot(wanted, tx, ty));
                    break;
                }
                if (occupiedByLiveCombatant != null && occupiedByLiveCombatant(tx, ty)) {
                    break;
                }
                TrapGridElement element = puzzle.ElementAt(tx, ty);
                if (element != null && element.ElementId != TransparentElementId) {
                    break;
                }
            }
        }

        return shots;
    }

    /// <summary>
    /// <b>The compass names on <see cref="CombatTerrain"/> do not follow one convention and must not
    /// be read as aim directions.</b>
    /// </summary>
    /// <remarks>
    /// Taking the scans at face value: the cannon found by scanning WEST is named CannonEast, so
    /// that pair is named for where it AIMS; the one found by scanning NORTH is named CannonNorth,
    /// so that pair is named for where it SITS. Both cannot be right. The data does not settle it
    /// either — all four terrains draw one model at four yaws — so the names remain the
    /// interpretation this task already flagged them as, and code should switch on the terrain
    /// value rather than trust the word.
    /// </remarks>
    public static bool CompassNamesAreReliable => false;
}
