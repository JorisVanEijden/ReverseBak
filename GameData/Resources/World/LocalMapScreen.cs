namespace GameData.Resources.World;

/// <summary>
/// The overhead map — <c>sub_ovr180_11F</c> (IDA 0x6d49f, canassa <c>map_main_loop</c>) over
/// <c>REQ_MAP.DAT</c>. Opened from the travel HUD's map button (action 50) and closed by the same
/// button, by Esc, or by action 1.
/// </summary>
/// <remarks>
/// <b>IT IS NOT A DRAWN MAP. IT IS THE WORLD SEEN FROM ABOVE.</b> Entry saves the camera's height
/// and pitch, sets the pitch straight down and the height to the remembered map height, and keeps
/// rendering the live world; exit puts both back. The arrows still move and turn the PARTY, and the
/// game's own help text calls the extra controls "zoom the map view up/down". A port that draws a
/// top-down picture of the zone is building a different feature.
///
/// <para><b>canassa calls the entry function <c>map_camera_snap_face_south</c> and it is wrong.</b>
/// IDA (0x6dbf7) writes <c>0xC000</c> — <see cref="TopDownPitch"/>, −90° — into
/// <c>camera.rotation3d.x</c>, the field the zone def's DefaultCameraPitch also lands in. Nothing
/// touches the yaw. The camera is tipped to look straight down, and the view still faces wherever
/// the party faces, which is exactly why the CD build has a non-rotating-map option
/// (<see cref="MapAction.ToggleNonRotating"/>).</para>
///
/// <para>Distinct from the continent map, which is its own screen and is reachable from this one
/// (<see cref="MapAction.ShowFullMap"/>).</para>
/// </remarks>
public static class LocalMapScreen {
    /// <summary>The REQ this screen is built from.</summary>
    public const string Layout = "REQ_MAP.DAT";

    /// <summary>What one of the screen's controls does.</summary>
    public enum MapAction {
        None,

        /// <summary>Walk the party forward — the travel HUD's own forward button.</summary>
        MoveForward,

        /// <summary>Walk the party back.</summary>
        MoveBackward,

        /// <summary>Turn the party left.</summary>
        TurnLeft,

        /// <summary>Turn the party right.</summary>
        TurnRight,

        /// <summary>Zoom the view down one step (camera height falls). Button + PageDown.</summary>
        ZoomDownOneStep,

        /// <summary>Zoom the view up one step (camera height rises). Button + PageUp.</summary>
        ZoomUpOneStep,

        /// <summary>Zoom down five steps. <b>End key only — there is no button.</b></summary>
        ZoomDownFiveSteps,

        /// <summary>Zoom up five steps. <b>Home key only — there is no button.</b></summary>
        ZoomUpFiveSteps,

        /// <summary>The follow-road toggle, shared with the travel HUD.</summary>
        ToggleFollowRoad,

        /// <summary>
        /// Toggle the north-up map. <b>Keyboard only ('N'), and only in the CD build.</b>
        /// </summary>
        ToggleNonRotating,

        /// <summary>Open the continent map.</summary>
        ShowFullMap,

        /// <summary>Encamp, shared with the travel HUD.</summary>
        Encamp,

        /// <summary>Back to the world view.</summary>
        Close,
    }

    /// <summary>The action an id drives — the ids are the DOS scancodes REQ_MAP carries.</summary>
    /// <remarks>
    /// The four movement ids, the follow-road toggle and encamp are the travel HUD's own ids, so the
    /// two screens share those handlers as well as their help records.
    /// </remarks>
    public static MapAction ActionFor(int actionId) {
        switch (actionId) {
            case 0x48: return MapAction.MoveForward;
            case 0x50: return MapAction.MoveBackward;
            case 0x4b: return MapAction.TurnLeft;
            case 0x4d: return MapAction.TurnRight;
            case 0x51: return MapAction.ZoomDownOneStep;
            case 0x49: return MapAction.ZoomUpOneStep;
            case 0x4f: return MapAction.ZoomDownFiveSteps;
            case 0x47: return MapAction.ZoomUpFiveSteps;
            case 0x13: return MapAction.ToggleFollowRoad;
            case 0x31: return MapAction.ToggleNonRotating;
            case 0x21: return MapAction.ShowFullMap;
            case 0x12: return MapAction.Encamp;
            case 0x32:
            case 0x01: return MapAction.Close;
            default: return MapAction.None;
        }
    }

    /// <summary>How many zoom steps an action moves the camera, negative for down.</summary>
    public static int ZoomStepsFor(MapAction action) {
        switch (action) {
            case MapAction.ZoomDownOneStep: return -1;
            case MapAction.ZoomUpOneStep: return 1;
            case MapAction.ZoomDownFiveSteps: return -5;
            case MapAction.ZoomUpFiveSteps: return 5;
            default: return 0;
        }
    }

    /// <summary>
    /// Whether REQ_MAP carries a button for this action, or it is reachable by key only.
    /// </summary>
    /// <remarks>
    /// The screen has eleven widgets — four movement, the follow-road toggle, the two one-step
    /// zooms, the continent map, encamp, close, and three invisible portrait click areas. The
    /// five-step zooms and the north-up toggle are <b>not</b> among them: End, Home and 'N' are the
    /// only ways to reach those, which is also why they have no help record.
    /// </remarks>
    public static bool HasButton(MapAction action) {
        switch (action) {
            case MapAction.ZoomDownFiveSteps:
            case MapAction.ZoomUpFiveSteps:
            case MapAction.ToggleNonRotating:
            case MapAction.None: return false;
            default: return true;
        }
    }

    /// <summary>
    /// <b>ONLY THE FIVE-STEP ZOOMS CLAMP; THE ONE-STEP ZOOMS DO NOT — AND THAT FOLLOWS FROM WHICH
    /// ONES HAVE A BUTTON.</b>
    /// </summary>
    /// <remarks>
    /// The one-step arms add or subtract and write the result back with no bounds test at all. They
    /// are safe because their buttons go dead at the limits — see <see cref="CanZoomUp"/> and
    /// <see cref="CanZoomDown"/>, which the loop re-evaluates every pass. The five-step arms have no
    /// button to switch off, so they clamp in the arithmetic instead.
    ///
    /// <para>So a port that keeps the arithmetic while leaving the buttons always live walks the
    /// camera straight out of range.</para>
    /// </remarks>
    public static bool ClampsItsOwnZoom(MapAction action) =>
        ZoomStepsFor(action) != 0 && !HasButton(action);

    /// <summary>Whether the zoom-down button is live — one full step must still fit.</summary>
    public static bool CanZoomDown(long cameraZ, long step, long minimum) =>
        cameraZ - step >= minimum;

    /// <summary>Whether the zoom-up button is live.</summary>
    public static bool CanZoomUp(long cameraZ, long step, long maximum) =>
        cameraZ + step <= maximum;

    /// <summary>The camera height an action produces, clamped where the original clamps.</summary>
    /// <param name="action">The control that was used.</param>
    /// <param name="cameraZ">Current camera height.</param>
    /// <param name="step">The zone's <c>MapZoomStep</c>.</param>
    /// <param name="minimum">The zone's <c>MapMinZ</c>.</param>
    /// <param name="maximum">The zone's <c>MapMaxZ</c>.</param>
    public static long CameraZAfter(MapAction action, long cameraZ, long step,
        long minimum, long maximum) {
        long moved = cameraZ + (ZoomStepsFor(action) * step);
        if (!ClampsItsOwnZoom(action)) {
            return moved;
        }

        return moved < minimum ? minimum
            : moved > maximum ? maximum
            : moved;
    }

    /// <summary>
    /// The help record a SECONDARY click on a control answers with, or 0 for none.
    /// </summary>
    /// <remarks>
    /// <b>Six of the eight are the travel HUD's own records, verbatim.</b> Only "zoom the map view
    /// down/up" (233/234), "a complete map of Midkemia" (235) and "return you to the world view"
    /// (236) are new — so the map screen's help text is the HUD's, extended, not a second set.
    ///
    /// <para>The two zoom pairs share a record because the wording is about the direction rather
    /// than the size; in practice only the one-step arms can reach it, since a secondary click needs
    /// a button and the five-step arms have none.</para>
    ///
    /// <para>The button is <see cref="Menu.MenuClickButton"/>; the original reads it as
    /// <c>menu_getButtonClicked() == button_Secondary</c>.</para>
    /// </remarks>
    public static int DescribeDialogFor(MapAction action) {
        switch (action) {
            case MapAction.MoveForward: return 223;
            case MapAction.MoveBackward: return 224;
            case MapAction.TurnLeft: return 225;
            case MapAction.TurnRight: return 226;
            case MapAction.ToggleFollowRoad: return 227;
            case MapAction.Encamp: return 229;
            case MapAction.ZoomDownOneStep:
            case MapAction.ZoomDownFiveSteps: return 233;
            case MapAction.ZoomUpOneStep:
            case MapAction.ZoomUpFiveSteps: return 234;
            case MapAction.ShowFullMap: return 235;
            case MapAction.Close: return 236;
            default: return 0;
        }
    }

    /// <summary>A full turn in the engine's angle unit — 0x10000, not 360.</summary>
    public const int AngleUnitsPerTurn = 0x10000;

    /// <summary>
    /// The pitch entry forces: <b>−90°, straight down</b> (0xC000 as written by IDA 0x6dbf7).
    /// </summary>
    public const short TopDownPitch = unchecked((short)0xC000);

    /// <summary>
    /// <b>The yaw is not touched, so the view turns with the party.</b>
    /// </summary>
    /// <remarks>
    /// Which is what the north-up option exists to undo — see
    /// <see cref="MapRendersWithYaw"/>. Entry saves the pitch and puts it back on exit; a port that
    /// leaves the camera looking down has broken the world view it returns to.
    /// </remarks>
    public static bool YawIsUntouchedOnEntry => true;

    /// <summary>
    /// <b>The camera height is remembered between visits, not recomputed.</b>
    /// </summary>
    /// <remarks>
    /// Entry loads it from the saved map height and exit writes the current height back, so the
    /// player's zoom survives closing the screen. <c>resource_loadZoneDataFiles</c> seeds it from
    /// <see cref="ZoneDefinition.CameraZPosition"/> when the zone changes — that field is the map's
    /// starting height, not a second travel-camera height.
    /// </remarks>
    public static bool ZoomIsRemembered => true;

    /// <summary>
    /// The yaw the world is rendered at while the map is up.
    /// </summary>
    /// <remarks>
    /// North-up mode renders at yaw 0 and puts the party's heading into the marker instead; the
    /// default renders at the party's own yaw and the marker is a fixed arrow. Either way there is a
    /// marker from <c>mapicons.bmp</c> at the centre of the viewport — the party is drawn, not
    /// implied by the camera. (<c>drawMap</c>, IDA 0x21711, which saves and restores the yaw around
    /// the render rather than turning the camera for good.)
    /// </remarks>
    public static int MapRendersWithYaw(int partyYaw, bool northUp) => northUp ? 0 : partyYaw;

    /// <summary>
    /// <b>The map view is NOT the travel render seen from above.</b>
    /// </summary>
    /// <remarks>
    /// <c>drawMap</c>'s outdoor branch (IDA <c>sub_seg021_231</c> @0x21941) fills the viewport with
    /// a solid pen and then draws the depth-sorted world items over it. There is no sky, no horizon
    /// strip and no ground band — the things a first-person view needs and an overhead one has no
    /// use for. Pointing the travel renderer downwards therefore shows the horizon backdrop and a
    /// fog-flattened ground rather than a map.
    ///
    /// <para>So a port needs a render mode of its own, not just a camera pose: flat background,
    /// items from above, and the synthetic <c>typeId 181</c> item the same function places at
    /// <c>z = 0</c>.</para>
    /// </remarks>
    public static bool HasItsOwnRenderMode => true;

    /// <summary>
    /// <b>Underground zones draw the dungeon automap instead of the world.</b>
    /// </summary>
    /// <remarks>
    /// <c>drawMap</c> branches on the zone's kind: an underground zone renders the automap recorded
    /// as the party walks (<c>renderDungeonAutomap</c>) and never runs the 3D pass, so the overhead
    /// map underground shows only what has been explored. See
    /// <see cref="ZoneDefinition.IsUnderground"/>.
    /// </remarks>
    public static bool DrawsDungeonAutomap(bool isUnderground) => isUnderground;
}
