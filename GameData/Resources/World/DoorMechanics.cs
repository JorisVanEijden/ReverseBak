namespace GameData.Resources.World;

/// <summary>
/// Opening and closing a world door — <c>wcursor_object_toggle_open_close</c>
/// (<c>SRC/INPUT/WCURSOR.C</c>), the handler behind <see cref="WorldEntityType.Door"/>.
///
/// <para>The decision only; the swing animation, the lockpicking screen and the sounds belong to
/// the caller. What is here is everything that decides <i>whether</i> the door moves.</para>
/// </summary>
public static class DoorMechanics {
    /// <summary>
    /// Global flag base for door state: door <c>id</c> is open iff flag <c>7000 + id</c> is set
    /// (<c>DOOR_OPEN</c>). Door state therefore lives in the save like any other story flag, which
    /// is why a door left open stays open.
    /// </summary>
    public const int OpenFlagBase = 7000;

    /// <summary>Bit of the animation-state word that holds "this door is open".</summary>
    public const int OpenBit = 0x800;

    /// <summary>Mask of the door id within the animation-state word, before shifting.</summary>
    public const int IdMask = 0x7f8;

    /// <summary>Bits the id is shifted by — the low three bits are the animation frame.</summary>
    public const int IdShift = 3;

    /// <summary>Frames in the swing; opening runs 0→7 and closing runs 7→0.</summary>
    public const int FrameCount = 8;

    /// <summary>Low bits of the state word holding the current swing frame.</summary>
    public const int FrameMask = 0x7;

    /// <summary>
    /// How near the party may be and still close a door, in world units on each axis.
    ///
    /// <para>The test is a <b>square</b>, not a radius: within ±800 on <i>both</i> X and Y. So the
    /// reachable area is a box around the door and standing diagonally away from it is further than
    /// the same distance straight on.</para>
    /// </summary>
    public const int CloseBlockedRange = 800;

    /// <summary>
    /// Shape id of a shut door. <b>Open and shut are two different shapes</b>, not one shape with a
    /// swing frame — the loader swaps the id outright, and the frame bits only drive the animation
    /// between them.
    /// </summary>
    public const int ClosedShapeId = 0x5c;

    /// <summary>Shape id of an open door.</summary>
    public const int OpenShapeId = 0x5d;

    /// <summary>
    /// Highest door id. <c>worlddoor_pref_slots_clear_all</c> clears flags for 0..255, which is
    /// exactly what the eight id bits in the state word can hold.
    /// </summary>
    public const int MaxDoorId = 0xff;

    /// <summary>Which shape a door of a given state should be drawn as.</summary>
    public static int ShapeFor(bool isOpen) => isOpen ? OpenShapeId : ClosedShapeId;

    /// <summary>
    /// The state word a door starts a zone with — <c>worlddoor_load_door_records</c>.
    ///
    /// <para>Built at zone load from the door's saved open flag, so a door left open stays open.
    /// An open door is seeded at the <i>end</i> of its swing (frame 7) and a shut one at the start
    /// (frame 0), which is why neither animates on arrival.</para>
    /// </summary>
    /// <param name="doorVariant">
    /// The door id. <b>It does not come from the world file.</b> The WLD record only says a door
    /// shape stands here; the id and the lock come from the fixed-object actor record at that same
    /// position — see the task notes on TASK-134 for the full chain.
    /// </param>
    public static int SeedState(int doorVariant, bool isOpen) {
        int state = isOpen ? FrameCount - 1 : 0;
        state |= (doorVariant & (IdMask >> IdShift)) << IdShift;
        if (isOpen) {
            state |= OpenBit;
        }
        return state;
    }

    /// <summary>What a click on a door does.</summary>
    public enum DoorAction {
        /// <summary>Not a door, or an uninitialised entity: the original returns immediately.</summary>
        Ignored,

        /// <summary>It swings open. The caller sets the flag and runs frames 0→7.</summary>
        Open,

        /// <summary>It swings shut. The caller clears the flag and runs frames 7→0.</summary>
        Close,

        /// <summary>
        /// Shut, and locked. The caller runs the lockpicking screen with
        /// <see cref="DoorDecision.LockValue"/>; only if that succeeds does the door open.
        /// </summary>
        Locked,

        /// <summary>
        /// Open, but the party is standing too near to pull it shut — the original plays a refusal
        /// line rather than closing it. Stops you shutting a door on yourself.
        /// </summary>
        TooCloseToClose,
    }

    /// <summary>The verdict plus what the caller needs to act on it.</summary>
    public readonly struct DoorDecision {
        public DoorDecision(DoorAction action, int doorId, int lockValue) {
            Action = action;
            DoorId = doorId;
            LockValue = lockValue;
        }

        public DoorAction Action { get; }

        /// <summary>Door id from the state word; the open flag is <see cref="OpenFlagBase"/> + this.</summary>
        public int DoorId { get; }

        /// <summary>Lock difficulty, or 0 for an unlocked door.</summary>
        public int LockValue { get; }

        /// <summary>The global flag this door's open state is stored in.</summary>
        public int OpenFlag => OpenFlagBase + DoorId;
    }

    /// <summary>The door id packed into an animation-state word.</summary>
    public static int DoorIdOf(int animationState) => (animationState & IdMask) >> IdShift;

    /// <summary>Whether the state word has the open bit set.</summary>
    public static bool IsOpenState(int animationState) => (animationState & OpenBit) != 0;

    /// <summary>The state word with a swing frame written into its low bits.</summary>
    public static int WithFrame(int animationState, int frame) =>
        (animationState & ~FrameMask) | (frame & FrameMask);

    /// <summary>The state word after the door has been opened or closed.</summary>
    public static int WithOpen(int animationState, bool open) =>
        open ? animationState | OpenBit : animationState & ~OpenBit;

    /// <summary>
    /// Decides what a click on this door does.
    /// </summary>
    /// <param name="animationState">The entity's animation-state word: id, open bit and frame.</param>
    /// <param name="lockValue">
    /// The door's lock difficulty. <b>The original keeps this in the entity's
    /// <c>orientation.pitch</c></b> — a rotation field reused as lock data, which is worth knowing
    /// before anyone tries to read a door's pitch as an angle. Zero means unlocked.
    /// </param>
    /// <param name="isOpen">
    /// Whether flag <c>7000 + id</c> is set. <b>This, not the state word's open bit, is what the
    /// original branches on</b> — the flag is the truth and the bit is the visual that follows it.
    /// </param>
    /// <param name="partyDx">Party-to-door distance on X, in world units (sign ignored).</param>
    /// <param name="partyDy">Party-to-door distance on Y, in world units (sign ignored).</param>
    public static DoorDecision Decide(int animationState, int lockValue, bool isOpen,
        int partyDx, int partyDy) {
        if (animationState == 0) {
            return new DoorDecision(DoorAction.Ignored, 0, 0);
        }

        int id = DoorIdOf(animationState);
        if (isOpen) {
            bool tooClose = Abs(partyDx) <= CloseBlockedRange && Abs(partyDy) <= CloseBlockedRange;
            return new DoorDecision(tooClose ? DoorAction.TooCloseToClose : DoorAction.Close, id, 0);
        }

        return lockValue != 0
            ? new DoorDecision(DoorAction.Locked, id, lockValue)
            : new DoorDecision(DoorAction.Open, id, 0);
    }

    private static int Abs(int value) => value < 0 ? -value : value;
}
