namespace GameData.Resources.World;

/// <summary>
/// How a roaming encounter actor faces the camera and walks — <c>rgnenc_render_object</c>
/// (<c>SRC/GAME/ENC/RGNENC.C</c>).
///
/// <para>These are billboards, not models: the actor has one sprite per facing, chosen from the
/// angle between it and the camera, and the far side of the turn is drawn by <b>mirroring</b> the
/// near side rather than by having its own art. So a creature's sprite sheet only ever holds half a
/// turn.</para>
/// </summary>
public static class EncounterActorPose {
    /// <summary>Facings the direction test resolves to.</summary>
    public const int Octants = 8;

    /// <summary>A full turn in the original's 16-bit angle unit.</summary>
    public const int FullTurn = 0x10000;

    /// <summary>Quarter turn, subtracted to bring the angle into the sprite's frame.</summary>
    public const int QuarterTurn = 0x4000;

    /// <summary>
    /// Half an octant, added so a facing is centred on its sprite rather than starting at it.
    /// </summary>
    public const int HalfOctant = FullTurn / (Octants * 2);

    /// <summary>The kind that walks: eight facings and a three-frame gait.</summary>
    public const int WalkingKind = 3;

    /// <summary>The kind that does not: four facings and a single pose.</summary>
    public const int StandingKind = 4;

    /// <summary>Frames in the walk cycle.</summary>
    public const int WalkFrames = 3;

    /// <summary>Bit of the packed state word that holds the gait's direction of travel.</summary>
    public const int AdvancingBit = 4;

    // Sprite column per octant. Octants 5-7 reuse 3, 0 and 1 mirrored, which is why the last three
    // repeat earlier columns. Stride 3, one per walk frame.
    private static readonly int[] WalkingColumns = { 0, 3, 6, 9, 12, 9, 0, 3 };

    // Sprite column per quadrant. Quadrant 3 reuses quadrant 1 mirrored. Stride 4 — and note the
    // first is 3, not 0: this kind's sheet does not start at column zero.
    private static readonly int[] StandingColumns = { 3, 7, 11, 7 };

    /// <summary>
    /// Which of the eight facings the actor presents to the camera.
    /// </summary>
    /// <param name="angleToCamera">
    /// The angle from the actor to the camera, in the original's 16-bit unit (<c>r3d_tbl_atan2</c>).
    /// </param>
    /// <param name="actorYaw">The actor's own heading, same unit.</param>
    /// <remarks>
    /// The whole expression is <c>(~((angle - 0x4000) - yaw) + 0x1000) &gt;&gt; 13</c>, evaluated in
    /// <b>16 bits</b>. The complement is what makes the sprite turn the opposite way to the camera —
    /// walk around a creature and it keeps facing you — and the <c>+ 0x1000</c> centres each facing
    /// on its sprite instead of starting it there. Widen the arithmetic and the complement stops
    /// wrapping, which puts every facing an octant out.
    /// </remarks>
    public static int Octant(int angleToCamera, int actorYaw) {
        var wrapped = (ushort)(~((angleToCamera - QuarterTurn) - actorYaw) + HalfOctant);

        return wrapped >> 13;
    }

    /// <summary>
    /// The sprite column for a facing, and whether it is drawn mirrored.
    /// </summary>
    /// <remarks>
    /// <b>A standing actor is not a walking one with fewer frames</b> — it resolves the octant to a
    /// quadrant first (<c>octant &gt;&gt; 1</c>), uses a different column stride, and carries no
    /// frame at all.
    /// </remarks>
    public static int SpriteColumn(int kind, int octant, out bool mirrored) {
        if (octant < 0 || octant >= Octants) {
            mirrored = false;

            return 0;
        }
        if (kind == WalkingKind) {
            mirrored = octant >= 5;

            return WalkingColumns[octant];
        }
        if (kind == StandingKind) {
            int quadrant = octant >> 1;
            mirrored = quadrant == 3;

            return StandingColumns[quadrant];
        }
        mirrored = false;

        return 0;
    }

    /// <summary>
    /// Whether this kind of actor is drawn at all. Anything that is not
    /// <see cref="WalkingKind"/> or <see cref="StandingKind"/> is skipped outright.
    /// </summary>
    public static bool IsDrawn(int kind) => kind == WalkingKind || kind == StandingKind;

    /// <summary>
    /// Advance the walk cycle one tick.
    /// </summary>
    /// <remarks>
    /// <b>It is a ping-pong, not a loop</b>: the frames run 0, 1, 2, 1, 0, 1, 2 … so the middle
    /// frame is passed through twice per cycle and the gait reverses at each end. Playing it as a
    /// three-frame loop (0, 1, 2, 0) snaps the leg back instead of swinging it, which is the classic
    /// way a ported walk animation ends up limping.
    /// </remarks>
    /// <param name="frame">Current frame, 0-2.</param>
    /// <param name="advancing">Whether the cycle is currently counting up.</param>
    public static void Advance(ref int frame, ref bool advancing) {
        if (advancing) {
            if (frame < WalkFrames - 1) {
                frame++;
            } else {
                advancing = false;
                frame--;
            }
        } else {
            if (frame != 0) {
                frame--;
            } else {
                advancing = true;
                frame++;
            }
        }
    }

    /// <summary>
    /// Pack kind, frame and gait direction back into the actor's state word, as the original stores
    /// it: kind in the high byte, frame in the low two bits, the gait's direction at
    /// <see cref="AdvancingBit"/>.
    /// </summary>
    public static ushort PackState(int kind, int frame, bool advancing) =>
        (ushort)(((kind & 0xff) << 8) | (frame & 3) | (advancing ? AdvancingBit : 0));

    /// <summary>Read the three fields back out of a state word.</summary>
    public static void UnpackState(ushort state, out int kind, out int frame, out bool advancing) {
        kind = (state >> 8) & 0xff;
        frame = state & 3;
        advancing = (state & AdvancingBit) != 0;
    }
}
