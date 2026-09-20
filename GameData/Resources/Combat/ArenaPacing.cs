namespace GameData.Resources.Combat;

/// <summary>
/// How long a monster's turn takes to WATCH.
/// </summary>
/// <remarks>
/// <b>The original has no delay in its turn loop.</b> <c>combat_arena_turn_loop</c> (COMBAT.C:1588)
/// runs <c>combatenc_ai_run_turn</c> for each encounter actor in a tight <c>while</c>, one after the
/// other, with nothing between them. The time is spent INSIDE the turn: a move is animated cell by
/// cell by <c>animateCombatActorMove</c>, which draws the creature mid-stride as it slides, and a
/// swing plays its own animation.
///
/// <para>So the cost is per CELL WALKED, not a flat pause per turn — which is why this is a rate
/// rather than a single duration. A port that waited a fixed time per turn would drag a one-step
/// shuffle out as long as a charge across the grid.</para>
///
/// <para><b>Measured in the original, 2026-09-20</b> (dir.G01/SAVE01, chapter-1 fight, passing party
/// turns with Defend and polling the combatant table at 0.25 s): a monster left (6,7), was seen at
/// (5,6) at t+0.9 s still mid-turn, and finished at (5,4) with its turn ended at t+2.1 s. Three
/// cells in about two seconds, visibly stepping through the ones in between. A non-moving turn — a
/// parry, same fight — was done inside the first poll.</para>
///
/// <para><b>The sampling was 0.25 s over one moving turn</b>, so <see cref="SecondsPerCellWalked"/>
/// is good to "about two thirds of a second", not to three decimal places. It is a tuning value with
/// a measurement behind it, not a constant read out of the binary; re-measure a couple of moves
/// before trusting it further.</para>
/// </remarks>
public static class ArenaPacing {
    /// <summary>How long one cell of a monster's walk is given.</summary>
    /// <remarks>Measured at roughly 0.6–0.7 s a cell; see the type's remarks for the reading.</remarks>
    public const float SecondsPerCellWalked = 0.65f;

    /// <summary>
    /// The beat a turn gets when the monster did not walk — a swing, a parry, a spell.
    /// </summary>
    /// <remarks>
    /// <b>Ours, not measured.</b> The original's non-moving turn finished inside a single 0.25 s
    /// poll, so all that is known is "quick". A turn still needs to be separable from the next one
    /// or two monsters acting in a row read as one event, which is the complaint this pacing exists
    /// to answer — so this is a deliberately small beat rather than zero.
    /// </remarks>
    public const float SecondsPerStillTurn = 0.35f;

    /// <summary>A monster's turn, in seconds, given how far it walked.</summary>
    /// <param name="cellsWalked">Chebyshev distance from where it started to where it ended.</param>
    public static float SecondsFor(int cellsWalked) =>
        cellsWalked <= 0
            ? SecondsPerStillTurn
            : cellsWalked * SecondsPerCellWalked;
}
