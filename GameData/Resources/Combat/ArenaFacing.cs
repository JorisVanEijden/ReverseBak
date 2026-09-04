namespace GameData.Resources.Combat;

using System;

/// <summary>
/// Turning the party to face an encounter before an underground fight —
/// <c>hotspotevt_type1_encounter_run</c>'s <c>g_game_mode == 2</c> arm (canassa HOTSPOT.C:493-506)
/// and the rect centre it aims at (HOTSPOT.C:1109).
///
/// <para>Underground only. Above ground the party fights facing wherever they were walking; below
/// it they are turned toward the middle of the hotspot's box first, so the arena is laid out along
/// the corridor rather than across it.</para>
/// </summary>
/// <remarks>
/// <b>It is applied BEFORE the ground check and reverted if that fails</b> — see
/// <see cref="CombatGroundCheck"/>. The two are one operation in the original: turn, ask whether the
/// arena fits from there, and put the heading back if it does not. A port that turned the party and
/// kept the heading regardless would leave them facing a wall after an encounter declined to fire.
/// </remarks>
public static class ArenaFacing {
    /// <summary>
    /// The facing octant that points from a combatant toward another cell, or -1 for no direction.
    /// </summary>
    /// <param name="deltaColumn">Target column minus the actor's, in grid cells.</param>
    /// <param name="deltaRow">Target row minus the actor's.</param>
    /// <remarks>
    /// <b>Octant 0 is AWAY from the camera</b> — deeper into the arena, the direction the party
    /// faces — matching <see cref="Combatant.FacingOctant"/>. The arena's rows run away from the
    /// viewer, so a target one row further in and no columns across is octant 0, and each further
    /// octant is an eighth of a turn toward increasing columns.
    ///
    /// <para><b>The zero delta answers -1 rather than 0.</b> A cursor resting on the actor's own
    /// cell names no direction, and rounding <c>atan2(0, 0)</c> would silently answer "straight
    /// ahead" — turning the actor on a cursor move that carried no information.</para>
    /// </remarks>
    public static int OctantToward(int deltaColumn, int deltaRow) {
        if (deltaColumn == 0 && deltaRow == 0) {
            return -1;
        }
        double radians = Math.Atan2(deltaColumn, deltaRow);
        var octant = (int)Math.Round(radians / (Math.PI / 4.0));

        return ((octant % 8) + 8) % 8;
    }

    /// <summary>A quarter turn — the only headings this produces.</summary>
    public const int Quadrant = 0x4000;

    /// <summary>
    /// Half a quadrant, added before the mask so the snap goes to the NEAREST quarter turn.
    /// </summary>
    /// <remarks>
    /// The original writes <c>(facing + R3D_DEG(45)) &amp; 0xc000</c>. Without the bias the mask
    /// truncates instead of rounding and the party can end up turned as much as a full quadrant away
    /// from the encounter.
    /// </remarks>
    public const int SnapBias = Quadrant / 2;

    /// <summary>
    /// The heading that points from the party toward an offset, in BaK angle units.
    /// </summary>
    /// <param name="dx">Offset east-west, in world units.</param>
    /// <param name="dy">Offset north-south.</param>
    /// <remarks>
    /// <b>The same convention the party's own step uses:</b> heading 0 moves along +Y, and a target
    /// at +Y answers 0; a target at +X answers 0xC000, which is the heading that steps toward +X.
    /// Derived from the original's fixed-point <c>atan2</c> at its exact cases rather than assumed.
    ///
    /// <para><b>A double is safe here where it would not be elsewhere.</b> The original uses a
    /// 512-entry table and this does not reproduce its quantisation — but every caller feeds the
    /// answer straight into <see cref="SnapToQuadrant"/>, which discards everything below 45
    /// degrees. Nothing can observe the difference.</para>
    /// </remarks>
    public static ushort HeadingTo(long dx, long dy) {
        if (dx == 0 && dy == 0) {
            return 0;
        }
        double radians = Math.Atan2(-dx, dy);
        var raw = (int)Math.Round(radians / (2.0 * Math.PI) * 65536.0);
        return unchecked((ushort)(((raw % 65536) + 65536) % 65536));
    }

    /// <summary>Rounds a heading to the nearest quarter turn.</summary>
    public static ushort SnapToQuadrant(ushort heading) =>
        unchecked((ushort)((heading + SnapBias) & 0xc000));

    /// <summary>
    /// The world centre of a hotspot's box — <c>hotspotevt_tile_rect_world_ctr</c>.
    /// </summary>
    /// <param name="partyTileX">
    /// The party's tile. <b>The box's cells are offsets inside it</b>, not world coordinates, so the
    /// same box means a different place depending on where the party is standing.
    /// </param>
    /// <param name="partyTileY"><inheritdoc cref="BoxCentre" path="/param[@name='partyTileX']"/></param>
    /// <param name="boxStartX">The box's four bytes <b>in their on-disk order</b>: minX, maxY, maxX, minY.</param>
    /// <param name="boxEndY"><inheritdoc cref="BoxCentre" path="/param[@name='boxStartX']"/></param>
    /// <param name="boxEndX"><inheritdoc cref="BoxCentre" path="/param[@name='boxStartX']"/></param>
    /// <param name="boxStartY"><inheritdoc cref="BoxCentre" path="/param[@name='boxStartX']"/></param>
    /// <remarks>
    /// <b>This consumer is immune to the min/max swap, and that is worth knowing.</b> The centre is
    /// the midpoint of each pair and a midpoint is commutative, so reading the box as
    /// (min, min, max, max) gives the same answer here — unlike
    /// <see cref="World.EncounterAftermath.ApproachDirection"/>, which inverts. What DOES matter is
    /// the axis pairing: X comes from bytes 0 and 2, Y from bytes 3 and 1.
    ///
    /// <para><b>The far edge is the far side of the max cell, not its near side.</b> The original averages
    /// <c>min * SubCell</c> with <c>(max + 1) * SubCell</c>, so a one-cell box centres on that
    /// cell's middle rather than on its corner — which is what makes a single-cell hotspot point at
    /// itself instead of at its own edge.</para>
    /// </remarks>
    public static (long X, long Y) BoxCentre(int partyTileX, int partyTileY,
        int boxStartX, int boxEndY, int boxEndX, int boxStartY) => (
        ((long)partyTileX * World.WorldPlacement.TileSize)
            + ((boxStartX + boxEndX + 1) * (long)World.WorldPlacement.SubCellSize / 2),
        ((long)partyTileY * World.WorldPlacement.TileSize)
            + ((boxStartY + boxEndY + 1) * (long)World.WorldPlacement.SubCellSize / 2));

    /// <summary>
    /// The heading the party is turned to before an underground fight: toward the box's centre,
    /// snapped to the nearest quarter turn.
    /// </summary>
    /// <inheritdoc cref="BoxCentre"/>
    /// <param name="partyWorldX">The party's absolute world position.</param>
    /// <param name="partyWorldY"><inheritdoc cref="FacingFor" path="/param[@name='partyWorldX']"/></param>
    public static ushort FacingFor(int partyTileX, int partyTileY,
        long partyWorldX, long partyWorldY,
        int boxStartX, int boxEndY, int boxEndX, int boxStartY) {
        (long x, long y) = BoxCentre(partyTileX, partyTileY, boxStartX, boxEndY, boxEndX, boxStartY);
        return SnapToQuadrant(HeadingTo(x - partyWorldX, y - partyWorldY));
    }

    /// <inheritdoc cref="FacingFor(int, int, long, long, int, int, int, int)"/>
    /// <param name="trigger">The hotspot whose box the party is turned toward.</param>
    /// <param name="partyTileX"><inheritdoc cref="FacingFor(int, int, long, long, int, int, int, int)" path="/param[@name='partyTileX']"/></param>
    /// <param name="partyTileY"><inheritdoc cref="FacingFor(int, int, long, long, int, int, int, int)" path="/param[@name='partyTileX']"/></param>
    /// <param name="partyWorldX"><inheritdoc cref="FacingFor(int, int, long, long, int, int, int, int)" path="/param[@name='partyWorldX']"/></param>
    /// <param name="partyWorldY"><inheritdoc cref="FacingFor(int, int, long, long, int, int, int, int)" path="/param[@name='partyWorldX']"/></param>
    /// <remarks>Prefer this: the box's field order is the trap, and here it cannot be got wrong.</remarks>
    public static ushort FacingFor(World.TileEventTrigger trigger,
        int partyTileX, int partyTileY, long partyWorldX, long partyWorldY) =>
        trigger == null
            ? (ushort)0
            : FacingFor(partyTileX, partyTileY, partyWorldX, partyWorldY,
                trigger.StartX, trigger.EndY, trigger.EndX, trigger.StartY);
}
