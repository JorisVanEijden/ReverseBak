namespace GameData.Resources.Location;

/// <summary>
/// What a temple charges to send the party somewhere — the quote shown as you hover a destination
/// on the rift map (<c>MODALSCR.C</c>:236-242, the panel <c>drawTeleportMenu</c> @0x4ede0 draws).
/// </summary>
public static class TeleportCost {
    /// <summary>
    /// The octagonal distance approximation the original uses instead of a real hypotenuse:
    /// <c>max + min * 3 / 8</c>, with integer division at each step.
    ///
    /// <para>It is deliberately cheap and deliberately not Euclidean — the 3/8 term makes a
    /// diagonal cost about 1.375× a straight line where the true figure is 1.414×, so diagonal
    /// journeys come out slightly under-priced. Reproduce it rather than substituting a hypotenuse:
    /// every published fare in the game is this number.</para>
    /// </summary>
    public static int OctagonalDistance(int dx, int dy) {
        int a = dx < 0 ? -dx : dx;
        int b = dy < 0 ? -dy : dy;
        return a < b ? b + (a * 3 / 8) : a + (b * 3 / 8);
    }

    /// <summary>
    /// The fare between two destinations, in royals.
    /// </summary>
    /// <param name="sourceX">
    /// <b>Screen</b> x of the source temple's button on the rift map, not a world coordinate.
    /// </param>
    /// <param name="sourceY">Screen y of the source temple's button.</param>
    /// <param name="destinationX">Screen x of the destination temple's button.</param>
    /// <param name="destinationY">Screen y of the destination temple's button.</param>
    /// <param name="baseCost">The temple's flat charge (its shop block's <c>+0xE</c>).</param>
    /// <param name="costPerUnit">Its charge per unit of distance (its shop block's <c>+5</c>).</param>
    /// <remarks>
    /// <b>The distance is measured between the two buttons on the map picture</b>, not between the
    /// destinations' world positions. Where a temple sits in the world has no bearing on what it
    /// costs to reach — only where the artist put its dot. So a port that "improves" this by using
    /// the real coordinates from <c>teleport.json</c> would reprice every journey in the game.
    /// </remarks>
    public static long Price(int sourceX, int sourceY, int destinationX, int destinationY,
        int baseCost, int costPerUnit) {
        int distance = OctagonalDistance(sourceX - destinationX, sourceY - destinationY);

        // The original computes this as (v * 10 + 5) / 10, which LOOKS like rounding to the nearest
        // whole royal and is not: for an integer v that expression is exactly v. Every input here is
        // an integer, so the round trip cannot change the answer — it is vestigial, presumably from
        // a version whose fare had a fractional part. Left out rather than copied, because copying
        // it would imply a rounding rule that does not exist.
        return baseCost + ((long)distance * costPerUnit);
    }
}
