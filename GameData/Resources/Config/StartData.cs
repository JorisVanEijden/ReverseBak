namespace GameData.Resources.Config;

/// <summary>
/// Engine-independent view of START.DAT — ten little-endian int16 the engine reads straight over
/// globals at boot. It is the game's smallest data file (20 bytes) and it decides three unrelated
/// things: where the COMBAT camera sits, how big a combat tile is, and how large the full-screen
/// 3D window is.
///
/// <para>Reversed from <c>LoadSTART.DAT</c> (ovr129 @0x41620), which is a flat run of ten
/// <c>readFromFile(size=2, count=1)</c> calls with no header, count or terminator — so the field
/// ORDER is the format.</para>
///
/// <para><b>Every value here belongs to the ARENA camera, not the walking-around one.</b> The
/// engine runs two cameras and this file feeds only the second:</para>
/// <list type="table">
///   <item>
///     <term>explore</term>
///     <description>The world viewport the player walks around in. Its height and pitch come from
///       <see cref="World.ZoneDefinition.DefaultCameraZ"/>/<c>DefaultCameraPitch</c> (230/280
///       outdoors) at zone load, and its viewport rect comes from ZONE.DAT.</description>
///   </item>
///   <item>
///     <term>arena</term>
///     <description>A separate full-screen camera that copies the explore camera's X/Y and heading
///       but overwrites its height and pitch from THIS file every frame. It is drawn by exactly one
///       routine, and that routine's only callers are the combat grid and the combat arena.</description>
///   </item>
/// </list>
///
/// <para><b>This is the distinction the numbers themselves give away.</b> 1024 against the zone
/// def's 230, and a pitch of the opposite sign — two values that far apart are not two readings of
/// one camera. Feeding these to the explore view visibly tilts it wrong, which is how the
/// misreading was caught; the confirmation is that nothing outside combat ever reads them.</para>
///
/// <para><b>The viewport rect is the same trap.</b> The rectangle here is the full-screen window's;
/// the explore view's comes from ZONE.DAT — and the two are byte-identical (13, 11, 294, 101), so
/// reading either one looks right and only one is the world view's own source.</para>
/// </summary>
public class StartData : IResource {
    public StartData(string id) {
        Id = id;
    }

    public string Id { get; }
    public ResourceType Type => ResourceType.DAT;

    /// <summary>
    /// Height of the combat camera above the floor in an outdoor fight, in game units.
    /// </summary>
    /// <remarks>
    /// <b>The arena camera, not the party's eye.</b> See the type doc: this is set on a separate
    /// full-screen camera that only the combat renderer draws through, so it says nothing about how
    /// high the player's view sits while walking.
    /// </remarks>
    public int CombatCameraHeightAboveGround { get; set; }

    /// <summary>Height of the combat camera in an underground fight, in game units.</summary>
    /// <remarks>
    /// Lower than <see cref="CombatCameraHeightAboveGround"/> — a dungeon is a tighter space.
    ///
    /// <para><b>The discriminator is the zone KIND, not the chapter.</b> The engine branches on the
    /// zone's location field being 2, which is what the three dungeon zones ship. canassa reads the
    /// same branch as chapter-dependent and calls the pair a field of view; both halves of that
    /// name are wrong.</para>
    /// </remarks>
    public int CombatCameraHeightUnderground { get; set; }

    /// <summary>
    /// Downward tilt of the combat camera outdoors, in the engine's 16-bit angle units (a full
    /// revolution is 0x10000, so the shipped -2112 is about -11.6°).
    /// </summary>
    public int CombatCameraPitchAboveGround { get; set; }

    /// <summary>Downward tilt of the combat camera underground, same units.</summary>
    /// <remarks>
    /// Steeper than above ground. The one case that uses neither value is the underground
    /// look-straight-down targeting view, which the caller hard-codes to 0xC000 (-90°) rather than
    /// reading it from here — so a port must not treat this as "the underground pitch" in every
    /// situation.
    /// </remarks>
    public int CombatCameraPitchUnderground { get; set; }

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
    /// Left edge of the full-screen 3D window in canonical screen space; with
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
