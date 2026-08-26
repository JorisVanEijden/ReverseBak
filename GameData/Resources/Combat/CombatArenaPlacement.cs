namespace GameData.Resources.Combat;

/// <summary>
/// Where a combatant physically stands for a grid cell — the arena's cell-to-world mapping.
/// </summary>
/// <remarks>
/// <b>This is NOT <see cref="CombatGroundCheck.SampleOffset"/>, and the difference is deliberate.</b>
/// That one answers "is there ground here" for the pre-fight sweep and sits <b>half a cell nearer
/// the party</b> than the combatant who will stand on the same cell: it folds the half-cell into its
/// sideways step but not into its forward one. The original really is inconsistent between the two,
/// and <c>CombatGroundCheck</c> says so in prose. Reusing the sweep's offset to place a combatant
/// puts every actor half a cell too close — a uniform, plausible-looking error that reads as "the
/// arena is a bit small" rather than as a wrong formula.
///
/// <para><b>Offsets are pre-rotation.</b> <c>Across</c> is toward the party's right and
/// <c>Away</c> in front of them, both before the party's heading is applied — the caller rotates
/// them, the same way the ground sweep does. Keeping the rotation out means this stays a pure
/// function of the grid and cannot disagree with the sweep about which way the arena faces.</para>
/// </remarks>
public static class CombatArenaPlacement {
    /// <summary>
    /// Where cell (<paramref name="column"/>, <paramref name="row"/>) puts a combatant, as an
    /// offset from the party before its heading is applied.
    /// </summary>
    /// <param name="column">0 .. <see cref="CombatGrid.Width"/>-1.</param>
    /// <param name="row">0 .. <see cref="CombatGrid.Height"/>-1.</param>
    /// <param name="cellSize">The arena's cell size — <c>StartData.CombatGridCellSize</c>.</param>
    /// <remarks>
    /// Centred in BOTH axes: the half-cell appears in the forward term as well as the sideways one.
    /// The constants are shared with the sweep rather than restated, so a different cell size moves
    /// the footprint and the combatants standing on it together.
    /// </remarks>
    public static (int Across, int Away) CellOffset(int column, int row, int cellSize) => (
        (column * cellSize) + (cellSize / 2) - CombatGroundCheck.HalfWidthOf(cellSize),
        (row * cellSize) + (cellSize / 2) + CombatGroundCheck.ForwardOffset);

    /// <summary>
    /// How far the forward term differs from the ground sweep's, in world units.
    /// </summary>
    /// <remarks>
    /// Stated so the discrepancy is a value a test can pin rather than a sentence someone has to
    /// notice. Exactly half a cell, and only in the forward direction — the sideways terms agree.
    /// </remarks>
    public static int ForwardDifferenceFromGroundSweep(int cellSize) => cellSize / 2;

    /// <summary>
    /// The row the party's own line occupies — the near edge of the arena, closest to the camera.
    /// </summary>
    /// <remarks>
    /// Row 0 is nearest the party and rows increase away from them, which is what
    /// <see cref="CombatGroundCheck.ForwardOffset"/> being added to <c>row * cellSize</c> means. A
    /// port that read row 0 as the far edge would stand the party behind the monsters.
    /// </remarks>
    public const int NearRow = 0;

    /// <summary>Whether a cell is inside the arena for this fight.</summary>
    /// <remarks>
    /// Underground fights use fewer rows than the grid has
    /// (<see cref="CombatGrid.UndergroundPlayableRows"/>) — the grid keeps its full height, so the
    /// bound is a property of the FIGHT, not of the array.
    /// </remarks>
    public static bool IsPlayable(int column, int row, bool underground) =>
        CombatGrid.InBounds(column, row)
        && row < (underground ? CombatGrid.UndergroundPlayableRows : CombatGrid.Height);
}
