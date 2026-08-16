namespace GameData.Resources.Location;

/// <summary>What a queued teleport actually asks the world to do.</summary>
public enum ZoneTransitionKind {
    /// <summary>Nothing is queued, or the destination turned out to be where the party already is.</summary>
    None,

    /// <summary>
    /// Run a GDS scene without moving the party. The 3D view is blanked first, because the scene
    /// draws over it and whatever was behind would otherwise show through at the edges.
    /// </summary>
    SceneOnly,

    /// <summary>Move within the zone already loaded — no dispose, no reload.</summary>
    Reposition,

    /// <summary>Leave this zone for another: dispose, relocate, load the new zone.</summary>
    ChangeZone,
}

/// <summary>
/// Decides what a pending teleport means. Faithful port of the dispatcher at the top of
/// <c>ProcessTeleportation</c> @0x4ebe7 (ovr150), which every teleport in the game funnels through
/// — temple rift map, dialog teleport actions, ladders and tunnels alike.
///
/// <para>The destination is a <see cref="Location"/> whose <see cref="Location.ZoneNumber"/> doubles
/// as the discriminator, so two of its values are not zones at all. Both appear in the shipped
/// <c>TELEPORT.DAT</c>: one row is all zeroes, and three carry
/// <see cref="SceneOnlyZone"/> with a GDS scene and no coordinates.</para>
/// </summary>
public static class ZoneTransition {
    /// <summary>Zone 0 means no teleport is queued. The engine clears the slot to this after taking one.</summary>
    public const int NoTransitionZone = 0;

    /// <summary>
    /// Zone -1 means "run the scene, do not move". Stored as a byte, so the shipped data reads 255.
    /// </summary>
    public const int SceneOnlyZone = -1;

    /// <summary>
    /// Normalises the zone byte as the original's signed comparison sees it, so <c>255</c> from the
    /// file and <c>-1</c> in code are the same sentinel rather than two cases every caller repeats.
    /// </summary>
    public static int NormalizeZone(int zoneNumber) => zoneNumber == 255 ? SceneOnlyZone : zoneNumber;

    /// <summary>
    /// Whether this destination wants a GDS scene run before anything else happens.
    /// </summary>
    /// <remarks>
    /// The scene runs even for destinations that also move the party, and it runs in a <b>loop</b>:
    /// a scene can queue a further teleport of its own, which is how one door leads into another.
    /// The loop is why the destination is re-read after the scenes finish rather than being decided
    /// once up front.
    /// </remarks>
    public static bool RunsAScene(int gdsNumber) => gdsNumber != 0;

    /// <summary>
    /// What a destination asks for, given where the party is now.
    /// </summary>
    /// <param name="destination">The queued destination; null or zone 0 means nothing is queued.</param>
    /// <param name="currentZone">The zone the party is in.</param>
    /// <param name="currentX">The party's tile x.</param>
    /// <param name="currentY">The party's tile y.</param>
    /// <param name="currentRotation">The party's facing.</param>
    public static ZoneTransitionKind KindOf(Location? destination,
        int currentZone, int currentX, int currentY, int currentRotation) {
        if (destination == null) {
            return ZoneTransitionKind.None;
        }

        int zone = NormalizeZone(destination.ZoneNumber);
        if (zone == NoTransitionZone) {
            return ZoneTransitionKind.None;
        }

        if (zone == SceneOnlyZone) {
            return ZoneTransitionKind.SceneOnly;
        }

        if (SkipsTheMove(destination, currentZone, currentX, currentY, currentRotation)) {
            return ZoneTransitionKind.None;
        }

        return zone == currentZone ? ZoneTransitionKind.Reposition : ZoneTransitionKind.ChangeZone;
    }

    /// <summary>
    /// The original's "we are already there, do nothing" test — <b>reproduced with its bug intact</b>.
    /// </summary>
    /// <remarks>
    /// <b>The y comparison is inverted.</b> At 0x4ecbe the branch is <c>jz</c> where its two
    /// siblings (zone at 0x4ecac, x at 0x4ecb5) are <c>jnz</c>, so the condition that survives is
    /// same-zone AND same-x AND <b>different</b>-y AND same-facing. Two observable consequences:
    /// a teleport onto the tile the party already occupies does a full relocate instead of being
    /// skipped (wasteful, invisible), and a same-zone move that changes only y without turning the
    /// party is <b>silently dropped</b>.
    ///
    /// <para>Kept rather than corrected because the shipped data is the same data the original ran:
    /// same rules over the same destinations reproduce the same journeys, and the level designers
    /// evidently never placed a destination that trips it. "Fixing" it would make our world behave
    /// differently from the one the game was authored against, which is the opposite of the goal.
    /// <see cref="SkipsTheMoveAsIntended"/> is what the comparison was meant to say; nothing calls
    /// it, and it exists so the difference is visible instead of being an argument in a comment.</para>
    /// </remarks>
    public static bool SkipsTheMove(Location destination,
        int currentZone, int currentX, int currentY, int currentRotation) =>
        NormalizeZone(destination.ZoneNumber) == currentZone
        && destination.X == currentX
        && destination.Y != currentY
        && destination.ZRotation == currentRotation;

    /// <summary>
    /// What <see cref="SkipsTheMove"/> was evidently meant to be: already there in every component.
    /// Deliberately uncalled — see that method's remarks.
    /// </summary>
    public static bool SkipsTheMoveAsIntended(Location destination,
        int currentZone, int currentX, int currentY, int currentRotation) =>
        NormalizeZone(destination.ZoneNumber) == currentZone
        && destination.X == currentX
        && destination.Y == currentY
        && destination.ZRotation == currentRotation;

    // ---- placing the party ------------------------------------------------------------------
    //
    // Where the party lands is NOT restated here. TeleportToLocation @0x735bf builds it as
    // tile*0FA00h + offset*640h + 800, which is exactly World.WorldPlacement.CentreOf — the same
    // helper the town approach already places arrivals with. One owner, so a teleport and a walk
    // cannot disagree about where the middle of a cell is.

    /// <summary>
    /// Whether arriving resets the camera's height and pitch to the zone's defaults.
    /// </summary>
    /// <remarks>
    /// The original guards this on a flag (<c>word_dseg_1A76</c> at 0x73624) and, when it is set,
    /// leaves eye altitude and pitch exactly as they were. Facing is reset either way — only the
    /// vertical pair is conditional. Modelled as a parameter rather than assumed, because a teleport
    /// that silently levelled the camera would undo whatever set that flag.
    /// </remarks>
    public static bool ResetsCameraHeightAndPitch(bool cameraOverrideActive) => !cameraOverrideActive;
}
