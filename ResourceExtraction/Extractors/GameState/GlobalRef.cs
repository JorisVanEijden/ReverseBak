namespace ResourceExtraction.Extractors.GameState;

using System.Collections.Generic;

using GameData.Resources.GameState;

/// <summary>
/// Decodes raw global-variable references (key + range, or key + mask write)
/// into the shared <see cref="Condition"/>/<see cref="Effect"/> vocabulary, by
/// the key's numeric range. Default-to-raw: only confirmed ranges specialize.
/// All decode knowledge for the global store lives here. See
/// <c>docs/Global values.md</c> and <c>docs/specs/global-value-destructuring.md</c>.
/// </summary>
public static class GlobalRef {
    private const int Flags2Base = 56000;   // global_flags2 namespace
    private const int Flags2Stride = 10;    // 10 decimal keys per flag byte

    /// <summary>
    /// Decode a read/condition. <paramref name="max"/> is null for the engine's
    /// no-upper-bound sentinel. Caller must NOT pass key 0 (that is the carrier's
    /// "always" case, handled by the carrier).
    /// </summary>
    public static Condition DecodeCondition(int key, int min, int? max) {
        // Computed / named ranges first (most specific).
        if (key is >= 30000 and <= 30029) {
            return new VarCondition { Var = key - 30000, Min = min, Max = max };
        }
        if (key is >= 40001 and <= 40013) {
            return new PartyCondition { Check = key - 40000, Min = min, Max = max };
        }
        if (key is >= 50000 and <= 50137) {
            return new HasItemCondition { Item = key - 50000, AtLeast = min, AtMost = max };
        }
        if (key is >= 51000 and <= 51099) {
            return new HasNoteCondition { Note = key - 51000 };
        }
        if (key is >= 52001 and <= 52006) {
            return new SpellTimerActiveCondition { Timer = key - 52000 };
        }
        if (key is >= 53000 and <= 53100) {
            return new RandomCondition { Bound = key - 53000, Min = min, Max = max };
        }

        // global_flags2 single-bit reads: key >= 56000 and NOT divisible by 10.
        if (key >= Flags2Base) {
            int rel = key - Flags2Base;
            if (rel % Flags2Stride != 0) {
                return new FlagCondition { Flag = key, Set = FlagSetFromRange(min, max) };
            }
            // Divisible by 10 with only a range (no mask context here) -> raw.
            return new RawGlobalCondition { Key = key, Min = min, Max = max };
        }

        // global_flags story bits: 1..8499 read as a boolean state test.
        if (key is >= 1 and <= 8499) {
            return new FlagCondition { Flag = key, Set = FlagSetFromRange(min, max) };
        }

        // Anything else (8500..29999 fall-through namespace, unconfirmed) -> raw.
        return new RawGlobalCondition { Key = key, Min = min, Max = max };
    }

    /// <summary>
    /// A boolean flag read returns 0/1; the inclusive range [min, max] then tests
    /// it. "Set" iff 1 is in range and 0 is not. All shipped data uses (1, null).
    /// </summary>
    internal static bool FlagSetFromRange(int min, int? max) {
        int upper = max ?? int.MaxValue;
        bool oneMatches = min <= 1 && 1 <= upper;
        bool zeroMatches = min == 0;
        return oneMatches && !zeroMatches;
    }

    /// <summary>
    /// Decode a write.
    /// </summary>
    /// <remarks>
    /// <b>The bit-group form is THREE masks applied in order, not a "which bits" mask plus its
    /// data.</b> The setter (canassa DIALOG.C, dialog op 4) does, for a key that is
    /// <c>&gt;= 56000</c> and a multiple of 10:
    /// <code>
    /// group &amp;= andMask;   group |= orMask;   group ^= xorMask;
    /// </code>
    /// So a bit is forced to 0 only where <paramref name="andMask"/> clears it, forced to 1 where
    /// <paramref name="orMask"/> sets it, and otherwise LEFT ALONE. Reading the first byte as
    /// "which bits are being written" gets the eight-bit case exactly backwards: with the shipped
    /// <c>and=0xDF or=0x00</c> it would clear the seven bits the AND preserves and skip the one it
    /// actually clears.
    ///
    /// <para>Measured across the shipped tree: 125 bit-group writes, <b>117 with andMask 0xFF</b>
    /// (pure sets, which is why the old reading looked right) and <b>8 that really do clear</b>
    /// — including one <c>and=0xDF or=0x10</c> that clears one bit and sets another in the same op.
    /// <paramref name="xorMask"/> is <b>0 in every one of them</b>, so no shipped write toggles.</para>
    ///
    /// <para>Which key selects this form is the setter's own test, not a range guess: the reader and
    /// writer both compute <c>row = (key - 56000) / 10</c> and <c>bit = (key - 56000) % 10 - 1</c>
    /// (GSTATE.C), which is exactly the absolute-flag arithmetic used below.</para>
    /// </remarks>
    public static Effect DecodeEffect(int key, int andMask, int orMask, int xorMask, int value) {
        if (key >= Flags2Base && key % Flags2Stride == 0) {
            int group = (key - Flags2Base) / Flags2Stride;
            var flags = new List<FlagState>();
            for (var bit = 0; bit < 8; bit++) {
                bool clears = ((andMask >> bit) & 1) == 0;
                bool sets = ((orMask >> bit) & 1) != 0;
                bool toggles = ((xorMask >> bit) & 1) != 0;
                if (toggles || (!clears && !sets)) {
                    // A toggle is not expressible as {Flag, Set} and never occurs in shipped data;
                    // an untouched bit must not become a row, or the write clears its neighbours.
                    continue;
                }
                flags.Add(new FlagState {
                    Flag = Flags2Base + group * Flags2Stride + bit + 1,
                    // OR wins: it runs after the AND, so a bit in both masks ends up set.
                    Set = sets,
                });
            }
            return new SetFlagsEffect { Flags = flags };
        }

        if (key is >= 30000 and <= 30029) {
            return new SetVarEffect { Var = key - 30000, Value = value };
        }
        if (key is >= 1 and <= 8499) {
            return new SetFlagEffect { Flag = key, Set = value != 0 };
        }
        // Unconfirmed direct write. Its own type, not a SetVarEffect carrying a raw key: that
        // overload meant `30000 + Var` — the obvious way to apply a var write — silently landed on a
        // global nothing reads. RawGlobalCondition is the same idea on the read side.
        return new RawGlobalWriteEffect { Key = key, Value = value };
    }

    /// <summary>Decode a SetTemporaryFlag write (a timed flag set).</summary>
    public static Effect DecodeTemporaryEffect(int key, uint durationTicks) {
        return new SetFlagEffect { Flag = key, Set = true, ForTicks = durationTicks };
    }

    /// <summary>
    /// Expand a per-chapter bitmask into the list of chapters whose bit is set
    /// (chapter c -> bit c-1; bit 7 / chapter 8 also covers 9+). Never returns a
    /// packed value.
    /// </summary>
    public static List<int> ChaptersFromMask(int chapterMask) {
        var chapters = new List<int>();
        for (var chapter = 1; chapter <= 8; chapter++) {
            if (((chapterMask >> (chapter - 1)) & 1) != 0) {
                chapters.Add(chapter);
            }
        }
        return chapters;
    }
}
