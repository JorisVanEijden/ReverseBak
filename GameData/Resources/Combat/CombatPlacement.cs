namespace GameData.Resources.Combat;

using System;

/// <summary>
/// Finding a combatant somewhere to stand — <c>combat_actor_place_on_free_tile</c>
/// (ovr167 @0x5c3f4).
///
/// <para>Used when an actor has to be put on the field or shifted off an occupied tile. It is a
/// two-pass search: first outward from where the actor already is, then a fallback sweep — and the
/// fallback does not work.</para>
/// </summary>
public static class CombatPlacement {
    /// <summary>
    /// Whether a downed actor is left exactly where it lies.
    /// </summary>
    /// <param name="gridHasExit">The grid has an exit tile.</param>
    /// <param name="actorIsDown">The actor carries the cannot-act bit.</param>
    /// <remarks>
    /// The routine's first test: on a grid with <b>no</b> exit, a downed actor is not moved at all.
    /// With an exit it is placed like anyone else. So corpses stay put in a closed arena and are
    /// tidied away in an open one, which is a visible difference in where bodies end up.
    /// </remarks>
    public static bool LeavesDownedActorInPlace(bool gridHasExit, bool actorIsDown) =>
        !gridHasExit && actorIsDown;

    /// <summary>
    /// The column the first pass starts scanning on a given row.
    /// </summary>
    /// <param name="row">The row being scanned.</param>
    /// <param name="actorX">The actor's current column.</param>
    /// <param name="actorY">The actor's current row.</param>
    /// <remarks>
    /// <b>The actor's own row resumes from its own column; every other row starts at zero.</b> So the
    /// search begins where the actor already stands and works along, rather than restarting the row
    /// — which is what keeps a displaced combatant near where it was.
    /// </remarks>
    public static int FirstPassStartColumn(int row, int actorX, int actorY) =>
        row == actorY ? actorX : 0;

    /// <summary>
    /// The rows the first pass covers: <b>the actor's own row and everything above it</b>.
    /// </summary>
    /// <remarks>
    /// It walks from the actor's row down to zero. Rows <i>below</i> the actor are never examined by
    /// this pass — that was the fallback's job.
    /// </remarks>
    public static bool FirstPassCoversRow(int row, int actorY) => row >= 0 && row <= actorY;

    /// <summary>Whether a tile will take this actor.</summary>
    /// <remarks>
    /// Free, or already occupied by this same actor — the second clause is what lets the routine
    /// leave a combatant where it is rather than treating its own tile as blocked.
    /// </remarks>
    public static bool TileAccepts(bool blocked, bool occupiedBySelf) => !blocked || occupiedBySelf;

    /// <summary>
    /// <b>The fallback pass counts the wrong way and cannot work.</b>
    /// </summary>
    /// <remarks>
    /// When the first pass finds nothing the routine restarts at (0, 0) and sweeps again — except
    /// that at the end of each row it <i>decrements</i> the row and loops while the row is less than
    /// <see cref="CombatGrid.Height"/>. Starting from zero, the first decrement gives −1, which is
    /// still less than 13, so it continues into negative rows instead of walking down the grid.
    ///
    /// <para>The bound is the evidence: <c>&lt; 13</c> against an
    /// <see cref="CombatGrid.Height"/> of 13 is exactly what an <i>incrementing</i> sweep would use.
    /// Verified from the encodings — both passes assemble the same <c>dec word [bp-4]</c>
    /// (<c>ff 4e fc</c>), the first correctly and the second not.</para>
    ///
    /// <para><b>Reachability is not established.</b> The fallback runs only when every tile from the
    /// actor's row up to row zero is unusable, which needs heavy terrain blocking rather than merely
    /// a crowded field. Our port sweeps the grid properly rather than reproducing a walk off the top
    /// of it, and this records why that is a deviation rather than an oversight.</para>
    /// </remarks>
    public static bool FallbackPassIsBroken => true;

    /// <summary>
    /// The first free tile for an actor, scanning as the original's first pass does and then
    /// sweeping the rest of the grid properly.
    /// </summary>
    /// <param name="actorX">Current column.</param>
    /// <param name="actorY">Current row.</param>
    /// <param name="accepts">Whether the tile at (x, y) will take this actor.</param>
    /// <returns>The tile, or null when the whole grid is unusable.</returns>
    /// <remarks>
    /// The second phase is where this deviates: see <see cref="FallbackPassIsBroken"/>. Everything
    /// before it — the row order, the resume-from-own-column rule and the accept test — matches.
    /// </remarks>
    public static (int X, int Y)? FindTile(int actorX, int actorY, Func<int, int, bool> accepts) {
        if (accepts == null) {
            return null;
        }

        for (int y = actorY; y >= 0; y--) {
            for (int x = FirstPassStartColumn(y, actorX, actorY); x < CombatGrid.Width; x++) {
                if (accepts(x, y)) {
                    return (x, y);
                }
            }
        }

        // The original's fallback walks off the top of the grid; sweep it properly instead.
        for (var y = 0; y < CombatGrid.Height; y++) {
            for (var x = 0; x < CombatGrid.Width; x++) {
                if (accepts(x, y)) {
                    return (x, y);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// A downed actor that is placed is <b>re-registered</b> afterwards.
    /// </summary>
    /// <remarks>
    /// Both success paths test the cannot-act bit again and, if set, run the actor through the
    /// registration routine before returning. A live actor is simply written to its new tile. So
    /// moving a body is not the same operation as moving a fighter, even though one function does
    /// both.
    /// </remarks>
    public static bool DownedActorIsReRegistered(bool actorIsDown) => actorIsDown;
}
