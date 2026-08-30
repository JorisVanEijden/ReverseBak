namespace GameData.Resources.Data;

/// <summary>
/// <c>SUBREC_PARAMS</c> — the four bytes that follow a container record when flag 0x01 is set.
///
/// <para><b>It is a UNION of three readings, not a lock record.</b> <c>ActorSubrec01_Params</c>
/// (ACTOR.H:200) overlays <c>Cipher</c>, <c>DoorKey</c> and <c>Proximity</c> on the same four
/// bytes, and which one applies is decided by what the actor IS. The property names here are the
/// lock arm because that was the first consumer; they are not the meaning of the bytes.</para>
///
/// <para>This nearly cost a session: the stash-exposure sweep needs the proximity reading, and the
/// port looked as though it did not parse it at all — the data was here the whole time, wearing
/// the wrong names.</para>
/// </summary>
public class SaveGameContainerLockData {
    public SaveGameContainerLockData(
        byte flags,
        byte difficulty,
        byte puzzleChest,
        byte trapDamage
    ) {
        Flags = flags;
        Difficulty = difficulty;
        PuzzleChest = puzzleChest;
        TrapDamage = trapDamage;
    }

    /// <summary>Byte 0. Lock arm: lock flags. Proximity arm: <c>bFlags</c>.</summary>
    public byte Flags { get; }

    /// <summary>Byte 1. Lock arm: lock difficulty. Proximity arm: <c>bIntensity</c>.</summary>
    public byte Difficulty { get; }

    /// <summary>
    /// Byte 2. Lock arm: puzzle-chest id. Proximity arm: <c>bHundred_flag</c>. Cipher arm:
    /// <c>bCipher_puzzle_id</c>.
    /// </summary>
    public byte PuzzleChest { get; }

    /// <summary>Byte 3. <c>b_pad3</c> in every arm the header names.</summary>
    public byte TrapDamage { get; }

    // ------------------------------------------------------------------ the proximity arm

    /// <summary>
    /// How exposed a spot is, for the stash-exposure decay —
    /// <c>ActorSubrec01_Proximity.bIntensity</c>.
    /// </summary>
    /// <remarks>
    /// Named accessors rather than making callers remember that "Difficulty" means intensity here.
    /// The bytes are shared; only the reading differs.
    /// </remarks>
    public byte ProximityIntensity => Difficulty;

    /// <summary>
    /// <c>bHundred_flag</c>. <b>In the V102CD build this zeroes the score outright</b>, where the
    /// floppy divides by a hundred — an absolute exemption, not a hundredfold reduction. We target
    /// the CD build.
    /// </summary>
    public bool ProximityHundredFlag => PuzzleChest != 0;

    /// <summary>Bit 2 of <c>bFlags</c>, which divides the exposure score by 0x32.</summary>
    public bool ProximityFlagBit2 => (Flags & 0x04) != 0;
}
