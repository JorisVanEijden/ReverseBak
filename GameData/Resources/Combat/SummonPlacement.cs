namespace GameData.Resources.Combat;

/// <summary>
/// Picking the tile a summoned creature lands on — the placement loop at ovr173 @0x6758f.
/// </summary>
/// <remarks>
/// <b>The picker runs on the combat menu's own input poll</b> (<c>menu_pollInput</c> against
/// COMBAT.DAT), so the HUD stays live while the player chooses. It is not a modal of its own.
///
/// <para><b>There is no way out.</b> The loop ends only on a valid click — no cancel, no escape.
/// Once the picker opens the player must place the creature somewhere legal, which is worth knowing
/// before wiring an Esc that the original does not have.</para>
/// </remarks>
public static class SummonPlacement {
    /// <summary>Whether a tile is on the grid at all.</summary>
    /// <remarks>The combat grid's own bounds — the loop tests them itself rather than trusting the cursor.</remarks>
    public static bool InBounds(int x, int y) => CombatGrid.InBounds(x, y);

    /// <summary>
    /// Whether the cell is drawn as a legal target.
    /// </summary>
    /// <remarks>
    /// <b>The HIGHLIGHT and the ACCEPTANCE tests are not the same test</b> — see
    /// <see cref="Accepts"/>. This one asks only whether the cell is in bounds and unblocked.
    /// </remarks>
    public static bool Highlights(CombatGrid grid, int x, int y) =>
        grid != null && InBounds(x, y) && !grid.IsBlocked(x, y);

    /// <summary>
    /// Whether a click on the cell actually places the creature.
    /// </summary>
    /// <remarks>
    /// <b>Crystal ground highlights but refuses.</b> The acceptance test adds one condition the
    /// highlight does not: the cell's element must not be the trap crystal. Crystal ground is
    /// deliberately NOT blocking (you can walk onto it — that is how it goes off), so it passes the
    /// highlight test and then swallows the click.
    ///
    /// <para>That asymmetry is in the original and reads as an unresponsive cursor rather than a
    /// refusal, since nothing is said. A port that highlights from this test instead would be
    /// tidier and would show the player a different grid than the game does.</para>
    /// </remarks>
    public static bool Accepts(CombatGrid grid, int x, int y) =>
        Highlights(grid, x, y) && grid.TerrainAt(x, y) != CombatTerrain.Crystal;

    /// <summary>The highlight passed to the renderer for a legal cell.</summary>
    public const int LegalHighlight = 3;

    /// <summary>The value passed when the cell is not legal — no highlight.</summary>
    public const int NoHighlight = -1;

    /// <summary>Which highlight a cell draws with.</summary>
    public static int HighlightFor(CombatGrid grid, int x, int y) =>
        Highlights(grid, x, y) ? LegalHighlight : NoHighlight;

    /// <summary><b>The picker cannot be cancelled.</b></summary>
    public static bool CanCancel => false;
}
