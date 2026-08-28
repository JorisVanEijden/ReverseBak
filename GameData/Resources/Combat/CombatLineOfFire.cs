namespace GameData.Resources.Combat;

using System;

/// <summary>
/// Whether a shooter has a clear projectile path to a target across the combat grid —
/// <c>combat_actor_trace_proj_path</c> (CACTOR.C:1161).
/// </summary>
/// <remarks>
/// <b>This is the gate every bespoke AI routine asks about and nothing could answer.</b>
/// <see cref="MonsterTurnRoutines"/> and its siblings all take a <c>hasLineOfSight</c> /
/// <c>lineOfFireClear</c> argument rather than computing one, so they had no consumer partly because
/// there was nothing to pass. This supplies it.
///
/// <para><b>A DDA walk, not the original's physics.</b> The original launches a
/// <c>WorldEntity</c> from the shooter's tile centre — the same <c>-0x4b0</c> / <c>+0xc80</c> offsets
/// <see cref="CombatArenaPlacement.CellOffset"/> uses — aims it with
/// <c>world_rndr_actor_angle_actor</c>, and integrates at <c>forwardVelocity = 300</c>, converting
/// back to tile coordinates each step. Reproducing that would mean porting the motion integrator to
/// answer a yes/no question. Stepping the line finely enough to visit the same tiles is the same
/// behaviour by a shorter route — the project's rule is to port the behaviour, not the
/// implementation.</para>
///
/// <para><b>Terrain is NOT modelled here.</b> The original also lets
/// <c>combat_actor_tile_entry_effect</c> stop the projectile by setting <c>shapeId = -1</c>. That is
/// a separate mechanism with its own table, so this answers only the actor question and takes the
/// terrain answer from the caller when there is one.</para>
/// </remarks>
public static class CombatLineOfFire {
    /// <summary>
    /// Whether the path from (<paramref name="fromX"/>, <paramref name="fromY"/>) to
    /// (<paramref name="toX"/>, <paramref name="toY"/>) is clear.
    /// </summary>
    /// <param name="blocks">
    /// Whether a tile holds something that stops a projectile. <b>The caller must already have
    /// excluded the dead</b> — see the remarks.
    /// </param>
    /// <remarks>
    /// Four rules, each of which a straightforward line-of-sight routine gets wrong:
    ///
    /// <list type="number">
    /// <item><b>The DEAD do not block.</b> The original nulls the occupant when
    /// <c>CAF_DEAD</c> is set, so a corpse on the line is shot straight through. A port that asked
    /// "is this tile occupied" would have monsters refusing to fire down a lane full of bodies.</item>
    /// <item><b>The SHOOTER does not block itself</b>, which matters because the ray starts inside
    /// the shooter's own tile.</item>
    /// <item><b>The TARGET does not block.</b> Reaching it ends the walk successfully — it is the
    /// destination, not an obstacle.</item>
    /// <item><b>Missing everything is CLEAR, not blocked.</b> The original's loop exits on leaving
    /// the grid with <c>result</c> still 1. So a trace that reaches neither the target nor an
    /// obstacle reports a clear shot.</item>
    /// </list>
    /// </remarks>
    public static bool IsClear(int fromX, int fromY, int toX, int toY, Func<int, int, bool> blocks) {
        if (blocks == null || (fromX == toX && fromY == toY)) {
            return true;
        }

        int dx = toX - fromX;
        int dy = toY - fromY;
        // One sample per half tile: fine enough that no tile the line crosses is stepped over, which
        // is what the original's small velocity steps achieve.
        int steps = Math.Max(Math.Abs(dx), Math.Abs(dy)) * 2;
        int lastX = fromX;
        int lastY = fromY;

        for (var i = 1; i <= steps; i++) {
            int x = fromX + (int)Math.Round((double)dx * i / steps, MidpointRounding.AwayFromZero);
            int y = fromY + (int)Math.Round((double)dy * i / steps, MidpointRounding.AwayFromZero);
            if (x == lastX && y == lastY) {
                continue;
            }
            lastX = x;
            lastY = y;

            // Rule 3: arriving at the target is success, and it is checked BEFORE the block test so
            // that a target standing on a blocking tile is still shootable.
            if (x == toX && y == toY) {
                return true;
            }
            if (blocks(x, y)) {
                return false;
            }
        }
        // Rule 4.
        return true;
    }

    /// <summary>
    /// The usual predicate: a tile blocks when a LIVING combatant other than the shooter stands on
    /// it.
    /// </summary>
    /// <remarks>
    /// Supplied so callers do not each re-derive rules 1 and 2. <paramref name="occupantAt"/>
    /// returns the combatant on a tile, or <c>null</c>.
    /// </remarks>
    public static Func<int, int, bool> BlockedByLivingActor(
        Func<int, int, Combatant> occupantAt, Combatant shooter) => (x, y) => {
        if (occupantAt == null) {
            return false;
        }
        Combatant occupant = occupantAt(x, y);
        return occupant != null && occupant != shooter && !occupant.IsDead;
    };
}
