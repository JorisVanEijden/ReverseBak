namespace GameData.Resources.World;

/// <summary>
/// Whether a position is lined up to step onto a crossing — the gate at the head of
/// <c>worldmove_crossing_check_8dir</c> (canassa WORLDMOV.C:357).
///
/// <para><b>Shared, not encounter-specific.</b> Three callers use it: the party's own movement
/// (WORLDMOV.C:107), the map screen's auto-travel loop (MAP.C:537), and a roaming encounter actor
/// following a road (RGNENC.C:740). So the rule below is what makes road-following look the same
/// whether the party or a monster is doing it.</para>
///
/// <para><b>The gate is about POSITION, not about what is there.</b> It runs before any adjacent
/// cell is examined, so a perfectly good crossing is simply not taken when the mover is standing off
/// the line for its heading. That is why a road-follower turns at some points and walks past others
/// that look identical.</para>
/// </summary>
public static class CrossingAlignment {
    /// <summary>World units across one sub-tile cell.</summary>
    public const int SubCellSize = WorldPlacement.SubCellSize;

    /// <summary>The middle of a sub-tile cell — <c>0x320</c>.</summary>
    public const int SubCellCentre = SubCellSize / 2;

    /// <summary>Full turn in the engine's 16-bit angle unit.</summary>
    public const int FullTurn = 0x10000;

    /// <summary>The eight headings the probe recognises, as 16-bit angle units.</summary>
    public const int East = 0x0000;

    /// <inheritdoc cref="East"/>
    public const int NorthEast = 0x2000;

    /// <inheritdoc cref="East"/>
    public const int North = 0x4000;

    /// <inheritdoc cref="East"/>
    public const int NorthWest = 0x6000;

    /// <inheritdoc cref="East"/>
    public const int West = 0x8000;

    /// <inheritdoc cref="East"/>
    public const int SouthWest = 0xA000;

    /// <inheritdoc cref="East"/>
    public const int South = 0xC000;

    /// <inheritdoc cref="East"/>
    public const int SouthEast = 0xE000;

    /// <summary>
    /// The mode that probes BACKWARDS — it flips the heading by half a turn before looking.
    /// </summary>
    public const int ReversedProbeMode = 4;

    /// <summary>Normalises any heading into <c>[0, 0x10000)</c>.</summary>
    /// <remarks>
    /// The original's <c>R3D_DEG</c> yields a SHORT, so 180 degrees and beyond arrive negative and
    /// the C switch compares them sign-extended. Folding to unsigned here makes the eight cases
    /// contiguous constants instead of four positives and four negatives.
    /// </remarks>
    public static int Normalise(int heading) => ((heading % FullTurn) + FullTurn) % FullTurn;

    /// <summary>
    /// Whether the probe recognises this heading at all.
    /// </summary>
    /// <remarks>
    /// Only the eight 45-degree headings. Anything else returns immediately — a mover on an
    /// arbitrary heading can never take a crossing, whatever it is standing on.
    /// </remarks>
    public static bool IsProbeableHeading(int heading) => Normalise(heading) % (FullTurn / 8) == 0;

    /// <summary>The heading the probe actually looks along, given the caller's mode.</summary>
    public static int ProbeHeading(int heading, int mode) =>
        mode == ReversedProbeMode ? Normalise(heading + (FullTurn / 2)) : Normalise(heading);

    /// <summary>
    /// Whether <paramref name="worldX"/>/<paramref name="worldY"/> is lined up for
    /// <paramref name="heading"/>.
    /// </summary>
    /// <remarks>
    /// Each heading family checks a different thing about the position within its sub-tile cell:
    /// <list type="bullet">
    ///   <item><b>East/West</b> — x must be at the cell centre; y is unconstrained.</item>
    ///   <item><b>North/South</b> — y must be at the cell centre; x is unconstrained.</item>
    ///   <item><b>The NW/SE diagonal</b> — x and y must be equal within the cell.</item>
    ///   <item><b>The NE/SW diagonal</b> — x and y must sum to a whole cell, <b>or</b> both be zero.
    ///     That second clause is not a tidy special case of the first: at the cell corner the sum is
    ///     0, not <see cref="SubCellSize"/>, so without it a mover standing exactly on a corner could
    ///     never take a diagonal crossing.</item>
    /// </list>
    /// </remarks>
    public static bool IsAligned(int worldX, int worldY, int heading) {
        int xInCell = Mod(worldX, SubCellSize);
        int yInCell = Mod(worldY, SubCellSize);

        switch (Normalise(heading)) {
            case East:
            case West:
                return xInCell == SubCellCentre;
            case North:
            case South:
                return yInCell == SubCellCentre;
            case NorthWest:
            case SouthEast:
                return xInCell == yInCell;
            case NorthEast:
            case SouthWest:
                return xInCell + yInCell == SubCellSize || (xInCell == 0 && yInCell == 0);
            default:
                return false;
        }
    }

    // World coordinates are positive in practice, but C's % keeps the sign of the dividend and this
    // one must not: a negative remainder would silently fail every alignment test.
    private static int Mod(int value, int modulus) => ((value % modulus) + modulus) % modulus;
}
