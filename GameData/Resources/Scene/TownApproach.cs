namespace GameData.Resources.Scene;

using GameData.Resources.World;

/// <summary>
/// Where a town or background trigger puts the party before its scene opens —
/// <c>approachWalkToScenePosition</c> (ovr180 @0x6dc6a).
///
/// <para>Every shipped <c>DEF_TOWN</c> record asks for this, so it is the normal way a location is
/// entered rather than a special case.</para>
/// </summary>
public static class TownApproach {
    /// <summary>The sub-tile cell east of the tile's edge, from the record's packed offset.</summary>
    /// <remarks>
    /// <b>The offset is two bytes in one field</b> — X low, Y high — like several other fields in
    /// this data. Reading it as a single number sends the party to a cell that does not exist.
    /// </remarks>
    public static int SubTileX(int approachTileOffset) => approachTileOffset & 0xFF;

    /// <inheritdoc cref="SubTileX"/>
    public static int SubTileY(int approachTileOffset) => (approachTileOffset >> 8) & 0xFF;

    /// <summary>
    /// The world position the party walks to.
    /// </summary>
    /// <param name="currentTileX">The tile the party is standing on when the trigger fires.</param>
    /// <param name="currentTileY">As <paramref name="currentTileX"/>.</param>
    /// <param name="approachTileOffset">The record's packed sub-tile offset.</param>
    /// <remarks>
    /// <b>The destination is relative to the party, not absolute.</b> The routine reads the party's
    /// current tile and combines it with the record's sub-tile offsets, so one record serves every
    /// tile its trigger covers — a town gate spanning several tiles walks you to the same spot
    /// <i>within whichever tile you stepped on</i>. Treating the offset as a world position would
    /// send the party to the corner of the map.
    ///
    /// <para>Centred within the sub-cell, via <see cref="WorldPlacement.CentreOf"/> — the same
    /// placement rule as a chapter start or a teleport arrival.</para>
    /// </remarks>
    public static (long X, long Y) DestinationOf(int currentTileX, int currentTileY, int approachTileOffset) => (
        WorldPlacement.CentreOf(currentTileX, SubTileX(approachTileOffset)),
        WorldPlacement.CentreOf(currentTileY, SubTileY(approachTileOffset)));

    /// <summary>
    /// <b>The party turns to face the destination heading before it starts moving.</b>
    /// </summary>
    /// <remarks>
    /// The routine writes the record's heading straight into the camera rotation and only then runs
    /// the travel loop, so the approach is a walk in a fixed direction rather than a turn-as-you-go.
    /// The heading is also what the party is left facing when the location closes.
    /// </remarks>
    public static bool HeadingIsSetBeforeWalking => true;

    /// <summary>
    /// <b>Underground, the approach walks in half-size steps.</b>
    /// </summary>
    /// <remarks>
    /// The step is halved when the zone is an underground one, so the same record takes twice as many
    /// steps below ground. It changes only the pacing of the walk, not where it ends.
    /// </remarks>
    public static int StepFor(int step, bool underground) => underground ? step >> 1 : step;
}
