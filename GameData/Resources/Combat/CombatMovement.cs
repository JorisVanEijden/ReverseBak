namespace GameData.Resources.Combat;

/// <summary>How one <see cref="CombatMovement.Step"/> ended.</summary>
public enum StepStatus {
    /// <summary>The actor moved one tile.</summary>
    Moved,

    /// <summary>Already standing on the destination; nothing to do.</summary>
    AlreadyThere,

    /// <summary>Every candidate tile was blocked, so the actor stayed put. The original reverts the
    /// position in this case and reports failure.</summary>
    Blocked,

    /// <summary>The destination holds a pushable element. The caller must try to shove it one tile
    /// further along the same direction: on success the actor advances, on failure the step is
    /// refused and the "path is blocked" animation plays.</summary>
    BlockedByPushable,

    /// <summary>The requested destination is off the grid; the original refuses outright.</summary>
    TargetOffGrid,
}

/// <summary>Result of a single movement step.</summary>
public readonly struct StepResult {
    internal StepResult(StepStatus status, int x, int y) {
        Status = status;
        X = x;
        Y = y;
    }

    /// <summary>What happened.</summary>
    public StepStatus Status { get; }

    /// <summary>Tile the actor ends on — unchanged unless <see cref="StepStatus.Moved"/>.</summary>
    public int X { get; }

    /// <inheritdoc cref="X"/>
    public int Y { get; }

    /// <summary>Whether the actor may keep spending movement. The original treats "already there"
    /// and a completed move as success, and a full block as failure.</summary>
    public bool Succeeded => Status == StepStatus.Moved || Status == StepStatus.AlreadyThere;
}

/// <summary>
/// Combat-grid movement: one tile at a time, toward a destination.
///
/// <para>Ported from <c>combataipath_step_to_target</c> and <c>combataipath_resolv_blockd_diag</c>
/// (<c>SRC/COMBAT/AI/CMBTAI.C</c>).</para>
///
/// <para><b>There is no path search.</b> Movement is greedy sign-stepping with a single tile of
/// wall-sliding, so a concave obstacle stalls an actor outright — which is why the original has a
/// "path is blocked" message at all. Do not substitute A*: it would change which fights are
/// winnable, since monsters that get stuck on scenery are part of the balance.</para>
/// </summary>
public static class CombatMovement {
    /// <summary>
    /// Moves one tile from <paramref name="x"/>,<paramref name="y"/> toward the destination.
    /// </summary>
    /// <param name="adjacentToTarget">The original's <c>g_bActorAdjacentToTarget</c>; a pushable
    /// element is only shoved when this is set.</param>
    /// <param name="probe">Test reachability without committing. Only changes behaviour at a
    /// pushable element, which a probe steps past rather than shoving.</param>
    /// <remarks>
    /// The caller repeats this up to the actor's Speed allowance, and is responsible for updating
    /// occupancy and for firing crystal/trap terrain effects on the tile stepped onto.
    /// </remarks>
    public static StepResult Step(
        CombatGrid grid, int x, int y, int targetX, int targetY,
        bool adjacentToTarget = false, bool probe = false) {
        if (x == targetX && y == targetY) {
            return new StepResult(StepStatus.AlreadyThere, x, y);
        }
        if (!CombatGrid.InBounds(targetX, targetY)) {
            return new StepResult(StepStatus.TargetOffGrid, x, y);
        }

        int dx = System.Math.Sign(targetX - x);
        int dy = System.Math.Sign(targetY - y);

        ResolveBlockedDiagonal(grid, x, y, ref dx, ref dy);

        int nx = x + dx;
        int ny = y + dy;

        if (!grid.IsBlocked(nx, ny)) {
            return new StepResult(StepStatus.Moved, nx, ny);
        }

        // A pushable element on the destination is shoved rather than walked around, but only while
        // adjacent to the target. A probe passes straight through instead.
        if (grid.TerrainAt(nx, ny) == CombatTerrain.Pushable && adjacentToTarget) {
            return probe
                ? new StepResult(StepStatus.AlreadyThere, x, y)
                : new StepResult(StepStatus.BlockedByPushable, nx, ny);
        }

        // Wall-slide. The fallback pair depends on whether the blocked step was diagonal, which the
        // original selects with an inverted-De-Morgan test on dx/dy.
        int slideX, slideY;
        if (dx != 0 && dy != 0) {
            // Diagonal blocked: drop to one axis — vertical first, then horizontal.
            if (!grid.IsBlocked(x, ny)) {
                slideX = x;
                slideY = ny;
            } else if (!grid.IsBlocked(nx, y)) {
                slideX = nx;
                slideY = y;
            } else {
                return new StepResult(StepStatus.Blocked, x, y);
            }
        } else {
            // Orthogonal blocked: try the two diagonals on the far side of the obstacle. The offsets
            // read oddly because the original swaps dx and dy into the opposite axes.
            int perpX = nx + dy;
            int perpY = ny + dx;
            if (!grid.IsBlocked(perpX, perpY)) {
                slideX = perpX;
                slideY = perpY;
            } else if (!grid.IsBlocked(nx - dy, ny - dx)) {
                slideX = nx - dy;
                slideY = ny - dx;
            } else {
                return new StepResult(StepStatus.Blocked, x, y);
            }
        }

        return new StepResult(StepStatus.Moved, slideX, slideY);
    }

    /// <summary>
    /// Forbids squeezing diagonally between two crystals by zeroing one delta, forcing an orthogonal
    /// step instead (<c>combataipath_resolv_blockd_diag</c>).
    /// </summary>
    /// <remarks>
    /// Only applies to a diagonal move whose <i>both</i> orthogonal neighbours are crystals. Which
    /// delta is dropped depends on occupancy: if the horizontal neighbour is occupied the actor
    /// keeps its vertical component, otherwise the horizontal one.
    /// </remarks>
    private static void ResolveBlockedDiagonal(CombatGrid grid, int x, int y, ref int dx, ref int dy) {
        if (dx == 0 || dy == 0) {
            return;
        }
        if (!grid.IsCrystal(x + dx, y) || !grid.IsCrystal(x, y + dy)) {
            return;
        }

        if (grid.IsOccupied(x + dx, y)) {
            dx = 0;
        } else {
            dy = 0;
        }
    }
}
