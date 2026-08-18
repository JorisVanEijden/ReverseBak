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

    /// <summary>The creak a door makes as it swings — <c>sound_dooropen</c> (38).</summary>
    /// <remarks>
    /// <b>Played on BOTH paths, and it is not "the opening sound".</b> handle_Door plays it at the
    /// start of the swing whichever way the door is going (0x77a16) and follows with
    /// <see cref="LatchSound"/> once the swing finishes. So it is the hinge, not the direction —
    /// naming it "open" and playing it only when opening loses half its uses.
    /// </remarks>
    public const int SwingSound = 38;

    /// <summary>The latch at the end of a swing — <c>sound_doorclos</c> (39).</summary>
    /// <remarks>Played with wait-for-completion after the frames finish (0x77a54).</remarks>
    public const int LatchSound = 39;

    /// <summary>
    /// The line played when the party is too close to pull a door shut — ddx 157.
    /// </summary>
    /// <remarks>
    /// <c>handle_Door</c> @0x779e5. It is a joke about walking into the door rather than a bare
    /// refusal, which is worth knowing before anyone substitutes a terser "you cannot do that":
    /// the original tells you WHY by having a companion laugh at you.
    /// </remarks>
    public const int TooCloseDialog = 157;

    /// <summary>
    /// Shape id of a shut door — <c>m_doorgi</c>, the one that BLOCKS.
    /// </summary>
    /// <remarks>
    /// <b>The two door shapes are not two pictures; they are the same picture with and without
    /// collision.</b> Entities 0x5c (<c>m_door</c>) and 0x5d (<c>m_doorgi</c>) have identical
    /// geometry in every zone that carries them — same 25 meshes, same vertex pool, same eight
    /// swing frames. They differ in exactly one thing: 0x5d has a GID region (XRadius 80, YRadius
    /// 800) and 0x5c has none. "gi" is the GID.
    ///
    /// <para>So the shut door is the solid one and the open door is the one you can walk through,
    /// and the visible swing is not the shape at all — it is the flip-book frame the state word's
    /// low bits select. An earlier reading had these the other way round, which let the party walk
    /// through shut doors and be stopped by open ones.</para>
    /// </remarks>
    public const int ClosedShapeId = 0x5d;

    /// <summary>Shape id of an open door — <c>m_door</c>, the one with no GID region.</summary>
    public const int OpenShapeId = 0x5c;

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
