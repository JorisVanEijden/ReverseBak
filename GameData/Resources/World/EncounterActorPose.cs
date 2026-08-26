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

    /// <summary>
    /// The kind a DOWNED actor draws with: four facings and a single pose, no gait.
    /// </summary>
    /// <remarks>
    /// <b>Called "standing" here until 2026-08-26, and that was wrong.</b> The name cost real work:
    /// the combat arena asked for this kind for every live enemy and drew corpses, and three
    /// sessions went looking for the fault in mesh indexing before anyone looked at the art.
    ///
    /// <para>What settles it: <c>renderEncounterEnemySprite</c> writes this kind's column into
    /// slot 1 of the actor's flags block, a mesh reads the slot its <c>RuntimeFlagsIndex</c> names,
    /// and for a mordel that mesh's columns 3/7/11 are bitmaps 18, 22 and 26 — which are pictures of
    /// a dead mordel collapsed under its cloak. Column 7 of the WALK set, bitmap 7, is the same
    /// creature standing with a sword.</para>
    ///
    /// <para>The shape of the model was right the whole time — "four facings and a single pose, no
    /// frame" describes a corpse exactly. Only the name lied.</para>
    ///
    /// <para><b>A live but stationary actor is not this kind.</b> It uses
    /// <see cref="WalkingKind"/> at frame 0.</para>
    /// </remarks>
    public const int DownedKind = 4;

    /// <summary>Frames in the walk cycle.</summary>
    public const int WalkFrames = 3;

    /// <summary>Bit of the packed state word that holds the gait's direction of travel.</summary>
    public const int AdvancingBit = 4;

    // Sprite column per octant, stride 3 (one per walk frame). The far half of the turn is the near
    // half MIRRORED, so the table is symmetric about octant 4: 5 reuses 3's column, 6 reuses 2's,
    // 7 reuses 1's.
    //
    // *** INDEX 6 WAS 0 UNTIL 2026-08-26 AND SHOULD ALWAYS HAVE BEEN 6. *** The original's
    // `case 6:` sets the mirror flag and does NOT reassign spriteDir, so the column stays 6 — a
    // transcription that read the empty arm as "falls to zero" instead. The symmetry is the tell:
    // 5<->3 and 7<->1 pair up, and a 0 at 6 leaves octant 2 with no partner while drawing the
    // creature's FRONT when it is walking away.
    private static readonly int[] WalkingColumns = { 0, 3, 6, 9, 12, 9, 6, 3 };

    // Sprite column per quadrant. Quadrant 3 reuses quadrant 1 mirrored. Stride 4 — and note the
    // first is 3, not 0: this kind's sheet does not start at column zero. Confirmed against
    // renderEncounterEnemySprite's own switch (0x75f3e): {3, 7, 11, 7}, mirrored on quadrant 3.
    private static readonly int[] DownedColumns = { 3, 7, 11, 7 };

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
        if (kind == DownedKind) {
            int quadrant = octant >> 1;
            mirrored = quadrant == 3;

            return DownedColumns[quadrant];
        }
        mirrored = false;

        return 0;
    }

    /// <summary>
    /// Whether this kind of actor is drawn at all. Anything that is not
    /// <see cref="WalkingKind"/> or <see cref="DownedKind"/> is skipped outright.
    /// </summary>
    public static bool IsDrawn(int kind) => kind == WalkingKind || kind == DownedKind;

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
