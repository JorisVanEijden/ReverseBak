namespace GameData.Resources.Combat;

using System;

/// <summary>
/// How a fight writes its survivors back onto the world — <c>combat_actor_deploy_encounter</c>
/// (CACTOR.C:343), called from <c>combat_arena_finalize_round</c>.
/// </summary>
/// <remarks>
/// <b>The name in the original source is a misname: it deploys nothing.</b> It runs at the END of a
/// fight, walking the combat actor array and writing each still-visible combatant's pose into the
/// encounter block via <c>rgnenc_persist_actor_placed</c>. Without it a fight the party walks away
/// from leaves the enemies where they were before it started, not where it ended.
///
/// <para><b>Position is not modelled here</b> — it is
/// <see cref="CombatArenaPlacement.CellOffset"/> applied to the combatant's final grid tile, rotated
/// and offset by the caller exactly as the arena's own placement is. What this class adds is the two
/// rules that have no other home: how the facing is composed, and how two combatants are stopped
/// from persisting onto one tile.</para>
/// </remarks>
public static class CombatEndPersistence {
    /// <summary>One eighth of a turn, the unit the persisted facing is built from.</summary>
    public const ushort Octant = 0x2000;

    /// <summary>Half an octant — the rounding bias, and the bit the original tests.</summary>
    public const ushort HalfOctant = 0x1000;

    /// <summary>Half a turn, added to every persisted facing.</summary>
    public const ushort HalfTurn = 0x8000;

    /// <summary>Rounds a heading to the nearest eighth of a turn.</summary>
    /// <remarks>
    /// <b>The original spells this as a truncation plus a correction</b> —
    /// <c>(yaw &amp; 0xE000)</c>, then <c>+= 45°</c> when <c>(yaw &amp; 0x1000)</c> is set — which
    /// reads like two separate rules and is one. Bias-then-mask is the same function and matches
    /// <see cref="ArenaFacing.SnapToQuadrant"/>'s idiom.
    ///
    /// <para><b>The wrap is load-bearing.</b> A yaw in the last sixteenth of the turn rounds up past
    /// 0x10000 and must come back to 0; written with a widening add it lands outside the heading
    /// space entirely.</para>
    /// </remarks>
    public static ushort SnapToOctant(ushort heading) =>
        unchecked((ushort)((heading + HalfOctant) & 0xE000));

    /// <summary>
    /// The facing a combatant is persisted with.
    /// </summary>
    /// <param name="animationFacing">
    /// The actor's animation facing, 0..7 — <b>an eighth-turn index, not a heading.</b>
    /// </param>
    /// <param name="cameraYaw">The world camera's yaw at the moment the fight ends.</param>
    /// <remarks>
    /// <b>It is composed from the CAMERA, not from the actor's own world heading.</b> The arena is
    /// laid out relative to the party's line of sight, so a combatant's facing only means anything
    /// once the camera that defined the arena is folded back in. A port that persists the actor's
    /// own yaw gets a plausible number that is wrong by however far the party had turned.
    ///
    /// <para><b>Half a turn is added</b> because the persisted facing is which way the actor looks in
    /// the WORLD, and the arena's rows run away from the camera — an actor facing the party is
    /// facing back down the camera's own bearing.</para>
    /// </remarks>
    public static ushort FacingFor(int animationFacing, ushort cameraYaw) =>
        unchecked((ushort)((animationFacing * Octant) + SnapToOctant(cameraYaw) + HalfTurn));

    /// <summary>
    /// The next tile to try when the one a combatant finished on is already taken.
    /// </summary>
    /// <remarks>
    /// <b>The walk wraps in both axes and never terminates on its own</b> — the original relies on
    /// the grid having a free tile, which it does because it only ever runs for actors that are on
    /// it. A port that added a bound would need an answer for the bound being hit, and there isn't
    /// one; a port that did not notice the wrap would walk off the end of the last row.
    ///
    /// <para>x advances first and carries into y, and <b>y wraps to the top</b> rather than
    /// stopping — so the search covers the whole grid from wherever it starts.</para>
    /// </remarks>
    public static (int X, int Y) NextTile(int x, int y) {
        int nextX = x >= CombatGrid.Width - 1 ? 0 : x + 1;
        int nextY = nextX == 0
            ? (y >= CombatGrid.Height - 1 ? 0 : y + 1)
            : y;
        return (nextX, nextY);
    }

    /// <summary>
    /// Where a combatant actually persists, given who else is already standing.
    /// </summary>
    /// <param name="occupied">
    /// Whether a tile is taken by <b>another</b> visible combatant. The actor being placed must not
    /// count itself, or it walks away from a tile it is entitled to.
    /// </param>
    /// <remarks>
    /// <b>The original MOVES the actor, it does not merely choose a spot.</b> It assigns the walked
    /// coordinates back onto the combatant (<c>actor->inner->gridX/Y</c>) as it goes, so the actor's
    /// own grid position ends up wherever the search stopped. Anything reading that position
    /// afterwards — the persisted pose included — sees the new tile.
    ///
    /// <para>Returns the starting tile unchanged when it is free, which is the ordinary case.</para>
    /// </remarks>
    public static (int X, int Y) FreeTileFrom(int x, int y, Func<int, int, bool> occupied) {
        if (occupied == null) {
            return (x, y);
        }
        // Bounded by the grid's size rather than by trust: the original loops until it finds a gap,
        // and a caller whose predicate says every tile is taken would spin for ever. Falling back to
        // the starting tile keeps a wrong predicate from hanging the fight's teardown.
        int limit = CombatGrid.Width * CombatGrid.Height;
        int cx = x;
        int cy = y;
        for (var i = 0; i < limit; i++) {
            if (!occupied(cx, cy)) {
                return (cx, cy);
            }
            (cx, cy) = NextTile(cx, cy);
        }
        return (x, y);
    }
}
