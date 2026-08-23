namespace GameData.Resources.Config;

/// <summary>
/// Engine-independent view of START.DAT — ten little-endian int16 the engine reads straight over
/// globals at boot. It is the game's smallest data file (20 bytes) and it decides three unrelated
/// things: where the eye sits, how big a combat tile is, and how large the 3D view is.
///
/// <para>Reversed from <c>LoadSTART.DAT</c> (ovr129 @0x41620), which is a flat run of ten
/// <c>readFromFile(size=2, count=1)</c> calls with no header, count or terminator — so the field
/// ORDER is the format. Consumers: <c>sub_seg021_9FB</c> @0x2210b (camera height and pitch) and
/// <c>setupRenderView</c> @0x23d18 (viewport and projection shift).</para>
///
/// <para><b>These are global, not per-zone.</b> <see cref="World.ZoneDefinition"/> also carries a
/// <c>DefaultCameraZ</c>/<c>DefaultCameraPitch</c> pair, and they are a different thing: the render
/// entity that actually draws the world takes its X/Y and heading from the party camera but its
/// HEIGHT and PITCH from this file, choosing between the above-ground and underground values on
/// <c>zoneLocation</c>. So every outdoor zone shares one eye height, and every dungeon another.</para>
/// </summary>
public class StartData : IResource {
    public StartData(string id) {
        Id = id;
    }

    public string Id { get; }
    public ResourceType Type => ResourceType.DAT;

    /// <summary>Height of the viewing eye above the floor outdoors, in game units.</summary>
    public int CameraHeightAboveGround { get; set; }

    /// <summary>Height of the viewing eye underground, in game units.</summary>
    /// <remarks>Lower than <see cref="CameraHeightAboveGround"/> — a dungeon is a tighter space.</remarks>
    public int CameraHeightUnderground { get; set; }

    /// <summary>
    /// Downward tilt of the view outdoors, in the engine's 16-bit angle units (a full revolution is
    /// 0x10000, so the shipped -2112 is about -11.6°).
    /// </summary>
    public int CameraPitchAboveGround { get; set; }

    /// <summary>Downward tilt of the view underground, same units.</summary>
    /// <remarks>
    /// Steeper than above ground. The one case that uses neither value is the underground
    /// look-straight-down view, which the caller hard-codes to 0xC000 (-90°) rather than reading it
    /// from here — so a port must not treat this as "the underground pitch" in every situation.
    /// </remarks>
    public int CameraPitchUnderground { get; set; }

    /// <summary>
    /// Side of one combat-grid tile in game units — the scale that turns a combatant's grid
    /// coordinates into a world position.
    /// </summary>
    /// <remarks>
    /// <b>The combat grid is laid out relative to the camera, not to the zone.</b> The engine places
    /// tile (x, y) at <c>x * CombatGridCellSize + CombatGridCellSize/2 - 1200</c> across and
    /// <c>y * CombatGridCellSize + CombatGridCellSize/2 + 3200</c> away, then rotates that offset by
    /// the camera heading and adds the camera position. With the shipped 300 and
    /// <see cref="Combat.CombatGrid.Width"/> of 8, the -1200 is exactly half the grid's width, which
    /// is what centres the arena on the party's line of sight.
    /// </remarks>
    public int CombatGridCellSize { get; set; }

    /// <summary>
    /// Left edge of the 3D view in canonical screen space; with
    /// <see cref="ViewportY"/>, <see cref="ViewportWidth"/> and <see cref="ViewportHeight"/> it is
    /// the rectangle the renderer clips to.
    /// </summary>
    /// <remarks>
    /// Stored in the file as VGA pixels and scaled here, so no consumer needs to know the original
    /// was 320x200. REQ_MAIN.DAT carries an invisible click area with the same rectangle — that one
    /// is the MOUSE region; this one is what the renderer clips to. They agree, which is exactly why
    /// citing the click area as the renderer's source looks right and is not.
    /// </remarks>
    public int ViewportX { get; set; }

    /// <inheritdoc cref="ViewportX"/>
    public int ViewportY { get; set; }

    /// <inheritdoc cref="ViewportX"/>
    public int ViewportWidth { get; set; }

    /// <inheritdoc cref="ViewportX"/>
    public int ViewportHeight { get; set; }

    /// <summary>
    /// Perspective scale as a power of two — the renderer keeps both this exponent and
    /// <c>1 &lt;&lt; </c> it (9 and 512 as shipped).
    /// </summary>
    /// <remarks>
    /// Named for what the code does with it rather than for a focal length we have not derived: it
    /// is used only as a shift count, so a value that is not a small non-negative integer would be
    /// meaningless here.
    /// </remarks>
    public int ProjectionShift { get; set; }
}
