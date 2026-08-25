namespace GameData.Resources.GameState;

/// <summary>
/// Where a global event flag lives in the save's two bitmaps — <c>gstate_event_read</c> /
/// <c>gstate_event_write</c> (canassa GAME/STATE/GSTATE.C:40 and :118).
/// </summary>
/// <remarks>
/// <b>The two bitmaps are packed DIFFERENTLY, and only one of them is linear.</b> The low map is the
/// obvious <c>id &gt;&gt; 3</c> / <c>id &amp; 7</c>. The high map is <b>ten flags per byte</b>,
/// indexed from a sum that <b>wraps in 16 bits</b>:
/// <code>
/// unsigned int cx = id + 0x2540;   // 16-bit: 56000 + 9536 wraps to 0
/// row = cx / 10;
/// bit = cx % 10 - 1;
/// </code>
/// Reading it as one more linear bitfield puts 114 shipped story flags on the wrong bits.
/// </remarks>
public static class GlobalFlagLayout {
    /// <summary>Ids below this live in the low bitmap.</summary>
    public const int LowLimit = 0x2134;

    /// <summary>Ids at or above this live in the high bitmap.</summary>
    public const int HighBase = 0xdac0;

    /// <summary>The constant the high index adds before wrapping.</summary>
    public const int HighBias = 0x2540;

    /// <summary>Flags the original packs into each high-map byte.</summary>
    /// <remarks>
    /// <b>Ten, into eight bits.</b> That is not a typo in the reconstruction: the arithmetic yields
    /// bit values -1 through 8, and only 0..7 are addressable. See <see cref="TryHighPosition"/>.
    /// </remarks>
    public const int HighFlagsPerByte = 10;

    /// <summary>Bytes the high bitmap occupies — <c>event_bitmap_hi[50]</c>.</summary>
    public const int HighByteCount = 50;

    /// <summary>Bytes the low bitmap occupies — <c>event_bitmap_lo[1063]</c>.</summary>
    public const int LowByteCount = 1063;

    /// <summary>Whether an id is a plain low-bitmap flag.</summary>
    public static bool IsLowFlag(int id) => id >= 0 && id < LowLimit;

    /// <summary>Whether an id is a high-bitmap flag.</summary>
    public static bool IsHighFlag(int id) => id >= HighBase && id <= ushort.MaxValue;

    /// <summary>
    /// Where a high-bitmap flag sits, or false when the arithmetic lands outside a byte.
    /// </summary>
    /// <remarks>
    /// <b>The wrap is load-bearing.</b> Computing <c>id + 0x2540</c> in 32 bits — the natural C#
    /// reading — gives rows in the thousands, far outside the fifty bytes the block has, and sends
    /// ids ending in 4 to <c>bit == -1</c>. In 16 bits every id the shipped dialogs use lands in
    /// rows 0..40 on bits 0..7, which is the check that says this reading is the right one.
    ///
    /// <para><b>Two positions are unaddressable and both are refused rather than guessed:</b>
    /// <c>cx % 10 == 0</c> gives <c>bit == -1</c>, which is <c>1 &lt;&lt; -1</c> — undefined in C
    /// and a different wrong answer in C#; and <c>cx % 10 == 9</c> gives bit 8, which no
    /// <c>unsigned char</c> can hold, so the original's read is always 0 and its write is discarded
    /// by the truncation. <b>No shipped flag reaches either</b>, which is itself evidence the
    /// packing is meant to be read this way.</para>
    /// </remarks>
    public static bool TryHighPosition(int id, out int row, out int bit) {
        row = 0;
        bit = 0;
        if (!IsHighFlag(id)) {
            return false;
        }
        int cx = (id + HighBias) & 0xffff;
        row = cx / HighFlagsPerByte;
        bit = (cx % HighFlagsPerByte) - 1;
        return row >= 0 && row < HighByteCount && bit >= 0 && bit <= 7;
    }

    /// <summary>Where a low-bitmap flag sits: the ordinary <c>id &gt;&gt; 3</c> / <c>id &amp; 7</c>.</summary>
    public static bool TryLowPosition(int id, out int index, out int bit) {
        index = 0;
        bit = 0;
        if (!IsLowFlag(id)) {
            return false;
        }
        index = id >> 3;
        bit = id & 7;
        return index < LowByteCount;
    }

    /// <summary>
    /// Writes a flag into whichever bitmap owns it.
    /// </summary>
    /// <param name="low">The low bitmap.</param>
    /// <param name="high">The high bitmap.</param>
    /// <param name="id">The event id.</param>
    /// <param name="set">Whether to set or clear it.</param>
    /// <returns>False when the id belongs to neither map, or has no addressable position.</returns>
    /// <remarks>
    /// <b>The inverse of the reads above and deliberately in the same class</b>, because the two got
    /// out of step once already: a reader and a writer that each compute the position are free to
    /// disagree, and a round trip between them agrees with itself either way (TASK-203, TASK-209).
    ///
    /// <para><b>An unaddressable high position is REFUSED, not rounded.</b> The original's write for
    /// <c>bit == 8</c> is discarded by the byte truncation and its <c>bit == -1</c> is undefined —
    /// picking a bit here would invent state the game does not keep.</para>
    /// </remarks>
    public static bool TryWrite(byte[] low, byte[] high, int id, bool set) {
        if (low != null && TryLowPosition(id, out int index, out int lowBit) && index < low.Length) {
            low[index] = (byte)(set ? low[index] | (1 << lowBit) : low[index] & ~(1 << lowBit));
            return true;
        }
        if (high != null && TryHighPosition(id, out int row, out int bit) && row < high.Length) {
            high[row] = (byte)(set ? high[row] | (1 << bit) : high[row] & ~(1 << bit));
            return true;
        }
        return false;
    }

    /// <summary>Reads a high-bitmap flag out of the block.</summary>
    /// <returns>False when the id has no addressable position — see <see cref="TryHighPosition"/>.</returns>
    public static bool TryReadHigh(byte[] highBitmap, int id, out int value) {
        value = 0;
        if (highBitmap == null || !TryHighPosition(id, out int row, out int bit)
            || row >= highBitmap.Length) {
            return false;
        }
        value = (highBitmap[row] >> bit) & 1;
        return true;
    }
}
