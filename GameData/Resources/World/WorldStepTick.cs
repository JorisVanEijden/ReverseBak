namespace GameData.Resources.World;

/// <summary>
/// The step counter inside a movement cell, and the cell-boundary flag it raises —
/// <c>worldmove_step_tick_reset</c> / <c>_advance</c> / <c>_get</c> (canassa
/// <c>SRC/GAME/WORLD/WORLDMOV.C:775-795</c>).
/// </summary>
/// <remarks>
/// <b>A movement CELL is 1600 units and a step is the player's preference</b>, so at the Small
/// preset (400) it takes four presses to cross one, at Medium two, and at Large exactly one. The
/// counter runs 0..n-1 and the flag is raised only on the press that completes the cell:
/// <code>
/// void worldmove_step_tick_advance(void) {
///     nWorldStepTickCount++;
///     if (nWorldStepTickCount == 0x640 / g_nWorldStepSpeed) worldmove_step_tick_reset();
///     else world_step_tick = 0;
/// }
/// </code>
///
/// <para><b>What it gates is not cosmetic.</b> <c>hotspotevt_activate_at_player</c> (HOTSPOT.C:474)
/// reads the flag and <b>returns outright</b> when it is 0, so an encounter eligible for the Stealth
/// avoidance roll is not evaluated <i>at all</i> on a mid-cell step — not merely un-evaded. Without
/// this gate such an encounter gets one chance per STEP instead of one per CELL, which is four times
/// as many at the Small preset and moves with the player's step-size setting.</para>
///
/// <para><b>Both values are saved</b> (body offsets 52 and 53, parsed as
/// <c>SaveGameMovementData.SubTileStepCount</c> and <c>TileBoundaryCrossed</c>), so they survive a
/// save and a port that keeps them only in scratch state re-arms the gate on every load.</para>
/// </remarks>
public static class WorldStepTick {
    /// <summary>One movement cell, in world units — <c>0x640</c>.</summary>
    public const int CellSize = 0x640;

    /// <summary>The flag's value on a cell boundary.</summary>
    public const int OnBoundary = 1;

    /// <summary>
    /// Steps needed to cross one cell at this step distance — <c>worldmove_step_ticks_per_step</c>.
    /// </summary>
    /// <remarks>
    /// Guarded against a zero step distance, which the original cannot produce (MOVEMENT.DAT's
    /// smallest preset is 400) but a half-initialised session can — and a divide by zero at the
    /// first footstep is a worse failure than a boundary on every step.
    /// </remarks>
    public static int StepsPerCell(int stepDistance) =>
        stepDistance > 0 ? CellSize / stepDistance : 1;

    /// <summary>The counter and flag after one step — <c>worldmove_step_tick_advance</c>.</summary>
    /// <param name="count">The current <c>nWorldStepTickCount</c>.</param>
    /// <param name="stepDistance">The RESOLVED step distance, not the preference index.</param>
    /// <returns>The new counter and flag.</returns>
    public static (int Count, int Tick) Advance(int count, int stepDistance) {
        int next = count + 1;
        // *** THE ORIGINAL COMPARES FOR EQUALITY, NOT >=, AND THE RESET ZEROES THE COUNT. *** A
        // step distance that changes mid-cell can therefore carry the count past the target, and
        // the original simply keeps counting until the next reset brings it back. Reproduced rather
        // than "fixed" with >=, because a >= would raise the boundary a step early after any
        // preferences change.
        return next == StepsPerCell(stepDistance) ? (0, OnBoundary) : (next, 0);
    }

    /// <summary>The counter and flag after a reset — <c>worldmove_step_tick_reset</c>.</summary>
    /// <remarks>
    /// <b>A reset RAISES the flag.</b> It is not "clear everything": the zone-change arm resets so
    /// that the first step in a new zone counts as a boundary. Zeroing both would swallow the first
    /// encounter roll of every zone.
    /// </remarks>
    public static (int Count, int Tick) Reset() => (0, OnBoundary);

    /// <summary>Whether an encounter may be evaluated on this step — <c>worldmove_step_tick_get</c>.</summary>
    public static bool IsCellBoundary(int tick) => tick != 0;
}
