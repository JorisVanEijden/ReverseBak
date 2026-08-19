namespace GameData.Resources.World;

/// <summary>
/// The in-zone map — <c>map_main_loop</c> (MAP.C:93) over <c>REQ_MAP.DAT</c>.
/// </summary>
/// <remarks>
/// <b>IT IS NOT A MAP. IT IS THE WORLD SEEN FROM ABOVE.</b> There is no separate map render: the
/// screen snaps the camera to face south, raises it, and keeps drawing the live world. The arrows
/// still move and turn the PARTY, and the extra controls only change the camera's height. A port
/// that draws a top-down picture of the zone is building a different feature — and one where
/// walking around while looking at it would be impossible.
///
/// <para>Distinct from the continent map, which is its own screen.</para>
/// </remarks>
public static class LocalMapScreen {
    /// <summary>The REQ this screen is built from.</summary>
    public const string Layout = "REQ_MAP.DAT";

    /// <summary>What one of the screen's controls does.</summary>
    public enum MapAction {
        None,

        /// <summary>Walk the party forward.</summary>
        MoveForward,

        /// <summary>Walk the party back.</summary>
        MoveBackward,

        /// <summary>Turn the party left.</summary>
        TurnLeft,

        /// <summary>Turn the party right.</summary>
        TurnRight,

        /// <summary>Lower the camera one step.</summary>
        LowerOneStep,

        /// <summary>Raise the camera one step.</summary>
        RaiseOneStep,

        /// <summary>Lower the camera five steps, clamped.</summary>
        LowerFiveSteps,

        /// <summary>Raise the camera five steps, clamped.</summary>
        RaiseFiveSteps,
    }

    /// <summary>The action an id drives — the ids are DOS arrow and navigation scancodes.</summary>
    public static MapAction ActionFor(int actionId) {
        switch (actionId) {
            case 0x48: return MapAction.MoveForward;
            case 0x50: return MapAction.MoveBackward;
            case 0x4b: return MapAction.TurnLeft;
            case 0x4d: return MapAction.TurnRight;
            case 0x51: return MapAction.LowerOneStep;
            case 0x49: return MapAction.RaiseOneStep;
            case 0x4f: return MapAction.LowerFiveSteps;
            case 0x47: return MapAction.RaiseFiveSteps;
            default: return MapAction.None;
        }
    }

    /// <summary>How many steps an action moves the camera, negative for down.</summary>
    public static int StepsFor(MapAction action) {
        switch (action) {
            case MapAction.LowerOneStep: return -1;
            case MapAction.RaiseOneStep: return 1;
            case MapAction.LowerFiveSteps: return -5;
            case MapAction.RaiseFiveSteps: return 5;
            default: return 0;
        }
    }

    /// <summary>
    /// <b>ONLY THE FIVE-STEP MOVES CLAMP; THE ONE-STEP MOVES DO NOT.</b>
    /// </summary>
    /// <remarks>
    /// The single-step arms add or subtract and write the result back with no bounds test at all.
    /// They are safe because the BUTTONS are switched off at the limits — see
    /// <see cref="CanLower"/> and <see cref="CanRaise"/>, which the loop re-evaluates every pass. So
    /// the guard is the enable gate, not the arithmetic, and a port that keeps the arithmetic while
    /// leaving the buttons always live will walk the camera straight out of range.
    /// </remarks>
    public static bool ClampsItsOwnMove(MapAction action) =>
        action == MapAction.LowerFiveSteps || action == MapAction.RaiseFiveSteps;

    /// <summary>Whether the lower control is live — one full step must still fit.</summary>
    public static bool CanLower(long cameraZ, long step, long minimum) =>
        cameraZ - step >= minimum;

    /// <summary>Whether the raise control is live.</summary>
    public static bool CanRaise(long cameraZ, long step, long maximum) =>
        cameraZ + step <= maximum;

    /// <summary>The camera height an action produces, clamped where the original clamps.</summary>
    public static long CameraZAfter(MapAction action, long cameraZ, long step,
        long minimum, long maximum) {
        long moved = cameraZ + (StepsFor(action) * step);
        if (!ClampsItsOwnMove(action)) {
            return moved;
        }

        return moved < minimum ? minimum
            : moved > maximum ? maximum
            : moved;
    }

    /// <summary>
    /// The line a SECONDARY click on a control answers with.
    /// </summary>
    /// <remarks>
    /// <b>Six lines for eight controls: the two zoom pairs share.</b> A single step and a five-step
    /// jump in the same direction say the same thing, so the wording is about the direction rather
    /// than the size — which is why there is no "you cannot go much further" variant.
    ///
    /// <para>The button is <see cref="Menu.MenuClickButton"/>; the original reads it as
    /// <c>refusal_mode</c>, which is simply the secondary being held.</para>
    /// </remarks>
    public static int DescribeDialogFor(MapAction action) {
        switch (action) {
            case MapAction.MoveForward: return 0xdf;
            case MapAction.MoveBackward: return 0xe0;
            case MapAction.TurnLeft: return 0xe1;
            case MapAction.TurnRight: return 0xe2;
            case MapAction.LowerOneStep:
            case MapAction.LowerFiveSteps: return 0xe9;
            case MapAction.RaiseOneStep:
            case MapAction.RaiseFiveSteps: return 0xea;
            default: return 0;
        }
    }

    /// <summary>
    /// <b>The camera is snapped to face south on entry.</b>
    /// </summary>
    /// <remarks>
    /// And the previous height and yaw are saved, to be put back when the screen closes — so the
    /// map is a temporary camera state over the same world rather than a place the party goes.
    /// </remarks>
    public static bool SnapsToFaceSouthOnEntry => true;
}
