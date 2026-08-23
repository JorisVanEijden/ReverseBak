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
    /// creature never decides to flee. This routine returns early on morale <b>0</b> — such a
    /// creature can be routed but will not move. A port that folded the two into one "never flees"
    /// value would change the behaviour of one group or the other.
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
