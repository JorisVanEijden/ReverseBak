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
    /// What a door says when you ask about it instead of working it, by the state it is in.
    /// </summary>
    /// <remarks>
    /// <b>Asking is the SECONDARY click, and the answer depends on the door.</b>
    /// <c>wcursor_object_toggle_open_close</c> tests the held button inside each arm — after it has
    /// already branched on open versus shut — so this is not one "a door" line but two, and a port
    /// that showed a single description would tell the player their open door was shut half the
    /// time.
    ///
    /// <para><b>It answers before the lock, not after.</b> Asking about a locked door describes it;
    /// it does not open the picklock screen. Testing the button after the lock check would turn
    /// every question about a locked door into a lockpicking attempt.</para>
    /// </remarks>
    public static int DescriptionDialogFor(bool isOpen) =>
        isOpen ? OpenDoorDescriptionDialog : ShutDoorDescriptionDialog;

    /// <summary>What an open door says when asked about.</summary>
    public const int OpenDoorDescriptionDialog = 99;

    /// <summary>What a shut door says when asked about.</summary>
    public const int ShutDoorDescriptionDialog = 100;

    /// <summary>
    /// The click a door makes when touched at all.
    /// </summary>
    /// <remarks>
    /// <b>Played before anything is decided</b> — <c>audio_play(0x30)</c> sits ahead of the
    /// state-word guard and both branches — so a door answers a question with the same click it
    /// answers a shove with, and even a door with no state clicks.
    /// </remarks>
    public const int TouchSound = 0x30;

    /// <summary>
    /// Shape id of a shut door — <c>m_door</c>, the one with NO GID region.
    /// </summary>
    /// <remarks>
    /// <b>The two door shapes are not two pictures; they are the same picture with and without a
    /// GID region.</b> Entities 0x5c (<c>m_door</c>) and 0x5d (<c>m_doorgi</c>) have identical
    /// geometry in every zone that carries them — same meshes, same vertex pool, same eight swing
    /// frames. They differ in exactly one thing: 0x5d has a GID region (XRadius 80, YRadius 800)
    /// and 0x5c has none. The visible swing is not the shape — it is the flip-book frame the state
    /// word's low bits select.
    ///
    /// <para><b>A GID region is GROUND, not a blocker — which is what makes 0x5d the OPEN one.</b>
    /// The proximity scan asks "is there authored ground under the destination", and a MISS is what
    /// stops the move (collision spec §2.3/§2.4; <see cref="WorldEntityType.Door"/> is a walkable
    /// kind). So the shape carrying a region is the one you can walk through, and the shape with
    /// none is the one you cannot. An open doorway gives you floor; a shut door gives you nothing
    /// to stand on.</para>
    ///
    /// <para><b>These two constants were the wrong way round until 2026-08-21</b>, on the reasoning
    /// that "0x5d has a region, so 0x5d is the solid one" — which reads a GID as a collider and is
    /// backwards for this engine. Three independent places in the original say otherwise, and they
    /// agree with each other: <c>worlddoor_load_door_records</c> sets shapeId 0x5d when the door's
    /// open flag is set and 0x5c when it is clear; <c>worlddoor_rndr_enc_mark_actor</c> re-derives
    /// the shape from the same <see cref="OpenBit"/>; and the click handler in WCURSOR.C treats a
    /// set flag as open (it plays <see cref="OpenDoorDescriptionDialog"/>, refuses to close from
    /// inside <see cref="CloseBlockedRange"/>, and animates the swing frames 7→0 while CLEARING the
    /// flag). The 800 in that refusal is the open shape's own YRadius: you cannot pull a door shut
    /// while standing in the doorway it gives you.</para>
    ///
    /// <para>Corroborated by the shipped data: every authored door placement in Z10/Z11/Z12 is
    /// 0x5c (22 + 5 + 52, and zero of 0x5d), and <c>worlddoor_pref_slots_clear_all</c> zeroes every
    /// door flag — so a dungeon's doors all start SHUT, which is only true if 0x5c is the shut one.
    /// </para>
    /// </remarks>
    public const int ClosedShapeId = 0x5c;

    /// <summary>Shape id of an open door — <c>m_doorgi</c>, the one whose GID region is the floor
    /// of the doorway. <inheritdoc cref="ClosedShapeId" path="/remarks"/></summary>
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
