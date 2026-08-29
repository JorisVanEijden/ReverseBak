namespace GameData.Resources.Combat;

using System;

/// <summary>
/// Where a routed monster runs to — <c>combatenc_actor_flee_tile_east</c>
/// (canassa CBENC.C:617).
///
/// <para><see cref="MonsterMorale"/> decides WHETHER a monster routs; this decides where it goes
/// afterwards.</para>
/// </summary>
public static class MonsterFleeDestination {
    /// <summary>
    /// <b>Morale 0 means the monster never runs, even once it has routed.</b>
    /// </summary>
    /// <remarks>
    /// <b>This is a different guard from <see cref="MonsterMorale.NeverFleesMorale"/> (0xff), and
    /// they are not interchangeable.</b> The rout CHECK returns early on morale 0xff — such a
    /// creature never decides to flee. This routine returns early on morale <b>0</b>, and does so
    /// <i>before</i> setting the flee flag.
    ///
    /// <para><b>CORRECTED 2026-08-29 against IDA — this used to say a morale-0 creature "can be
    /// routed but will not move", and that is wrong.</b> <c>combatenc_pick_flee_destination</c>
    /// (@0x63ea1) sets <c>CAF_FLEE</c> and clears the target only <i>after</i> the morale-0 test,
    /// so a morale-0 creature never carries the flag at all: it does not rout, it fights on
    /// normally. It never reaches "routed but standing still", because it is never routed.</para>
    ///
    /// <para><b>The asymmetry is the other way round from what the names suggest: morale 0 is the
    /// STRONGER guard.</b> Morale 0xff is only tested in <c>combatenc_morale_flee_check</c>
    /// (@0x63f23), not here — so the spell-driven rout paths that call this routine directly
    /// (<c>Cast_Invitiation</c>, <c>Spell_RunAnimationEffect</c>,
    /// <c>combat_arena_resume_dispatch</c>) CAN rout a never-flees creature, while nothing at all
    /// can rout a morale-0 one. That is why this guard still earns its place even though
    /// <see cref="MonsterMorale.Routs"/> already rejects morale 0 on the ordinary path.</para>
    ///
    /// <para>The two also differ in RNG cost on the morale path: 0xff returns before the roll,
    /// morale 0 after it — see <see cref="MonsterMorale.ConsumesARoll"/>.</para>
    /// </remarks>
    public const int WontMoveMorale = 0;

    /// <summary>A roll strictly above this accepts a better tile.</summary>
    public const int AcceptRollAbove = 50;

    /// <summary>Grid bounds the scan walks — the full playable area.</summary>
    public const int Columns = 8;

    /// <inheritdoc cref="Columns"/>
    public const int Rows = 13;

    /// <summary>
    /// <b>The monster does not run to the FURTHEST tile — it runs to a random far-ish one.</b>
    /// </summary>
    /// <remarks>
    /// The scan walks every tile and, whenever it finds an unblocked one on a higher row than the
    /// best so far, takes it <b>only on a coin flip</b> (<c>RND(100) &gt; 50</c>). So improvements
    /// are frequently skipped and the destination ends up biased towards high rows without being the
    /// maximum. <b>A port that simply picks the highest reachable row is deterministic and wrong</b>
    /// — routed monsters would all pile onto the same edge.
    ///
    /// <para>Despite the routine's name it maximises <b>Y (the row)</b>, not X, so "east" describes
    /// nothing about it.</para>
    /// </remarks>
    public static bool AcceptsImprovement(int roll) => roll > AcceptRollAbove;

    /// <summary>
    /// Run the destination scan.
    /// </summary>
    /// <param name="isBlocked">Whether a tile is blocked.</param>
    /// <param name="roll">The routine's <c>RND(100)</c>, called once per improvement considered.</param>
    /// <returns>The chosen tile, or null when nothing was accepted.</returns>
    /// <remarks>
    /// <b>Returning null is an ordinary outcome</b>, not an error: row 0 can never beat the initial
    /// best, and every improvement can be refused by the coin flip, so a monster may end up with no
    /// destination at all and simply stand still.
    /// </remarks>
    public static (int X, int Y)? Choose(Func<int, int, bool> isBlocked, Func<int> roll) {
        if (isBlocked == null || roll == null) {
            return null;
        }

        var bestY = 0;
        (int X, int Y)? chosen = null;
        for (var x = 0; x < Columns; x++) {
            for (var y = 0; y < Rows; y++) {
                if (isBlocked(x, y) || y <= bestY) {
                    continue;
                }
                if (AcceptsImprovement(roll())) {
                    chosen = (x, y);
                    bestY = y;
                }
            }
        }
        return chosen;
    }

    /// <summary>Whether routing this monster makes it move at all.</summary>
    public static bool WillMove(int morale) => morale != WontMoveMorale;

    /// <summary><b>Fleeing clears the monster's stored target.</b></summary>
    public static bool ClearsTarget => true;
}
