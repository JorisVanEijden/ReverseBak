namespace ResourceExtraction.Extractors.GameState;

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
    /// Decode a write. <paramref name="mask"/> != 0 is a masked global_flags2
    /// bitfield write (expanded to a flag list); mask == 0 is a direct write of
    /// <paramref name="value"/> to <c>global[key]</c>.
    /// </summary>
    public static Effect DecodeEffect(int key, int mask, int data, int value) {
        if (mask != 0) {
            int group = (key - Flags2Base) / Flags2Stride;
            var flags = new System.Collections.Generic.List<FlagState>();
            for (var bit = 0; bit < 8; bit++) {
                if (((mask >> bit) & 1) != 0) {
                    flags.Add(new FlagState {
                        Flag = Flags2Base + group * Flags2Stride + bit + 1,
                        Set = ((data >> bit) & 1) != 0,
                    });
                }
            }
            return new SetFlagsEffect { Flags = flags };
        }

        if (key is >= 30000 and <= 30029) {
            return new SetVarEffect { Var = key - 30000, Value = value };
        }
        if (key is >= 1 and <= 8499) {
            return new SetFlagEffect { Flag = key, Set = value != 0 };
        }
        // Unconfirmed direct write -> keep the raw key as a var write (lossless).
        return new SetVarEffect { Var = key, Value = value };
    }

    /// <summary>Decode a SetTemporaryFlag write (a timed flag set).</summary>
    public static Effect DecodeTemporaryEffect(int key, uint durationTicks) {
        return new SetFlagEffect { Flag = key, Set = true, ForTicks = durationTicks };
    }
}
