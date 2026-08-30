namespace GameData.Resources.Location;

/// <summary>
/// What a temple charges to send the party somewhere — the quote shown as you hover a destination
/// on the rift map (<c>MODALSCR.C</c>:236-242, the panel <c>drawTeleportMenu</c> @0x4ede0 draws).
/// </summary>
public static class TeleportCost {
    /// <summary>
    /// The fare's distance term — <see cref="World.WorldDistance.Octagonal"/>.
    /// </summary>
    /// <remarks>
    /// <b>Kept as a named step of the fare, delegating rather than reimplementing.</b> This was the
    /// only copy for a while, which made a general R3D core routine look like a pricing detail; the
    /// stash-exposure sweep needs the same distance and would have grown a second one.
    /// </remarks>
    public static int OctagonalDistance(int dx, int dy) =>
        World.WorldDistance.Octagonal(dx, dy);

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
