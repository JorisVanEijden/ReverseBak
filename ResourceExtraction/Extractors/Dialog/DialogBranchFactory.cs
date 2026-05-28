namespace ResourceExtraction.Extractors.Dialog;

using GameData.Resources.Dialog.Branches;

using System.Collections.Generic;

/// <summary>
/// Decodes the raw branch triple (<c>globalKey</c>, <c>unknown3</c>,
/// <c>unknown4</c>) into a semantic <see cref="DialogBranchBase"/>. All the
/// engine's branch-evaluation knowledge lives here so consumers never see the
/// original bit-encoding. See <c>docs/specs/dialog-system.md</c>
/// §"Branch Evaluation" and §"global_flags2 Key Encoding".
/// </summary>
internal static class DialogBranchFactory {
    private const ushort NoUpperBound = 0xFFFF; // engine's "-1" range sentinel
    private const int FlagBase = 56000;         // global_flags2 key namespace
    private const int FlagStride = 10;          // 10 decimal keys per flag byte

    public static DialogBranchBase Build(
        bool isChoiceMenu, ushort globalKey, ushort unknown3, ushort unknown4, long rawTarget) {
        DialogBranchBase branch = DecodeCondition(isChoiceMenu, globalKey, unknown3, unknown4);

        if (rawTarget >= 0x80000000) {
            branch.TargetId = (int)(rawTarget - 0x80000000);
        } else {
            branch.TargetOffset = (int)rawTarget;
        }
        return branch;
    }

    private static DialogBranchBase DecodeCondition(
        bool isChoiceMenu, ushort globalKey, ushort unknown3, ushort unknown4) {
        // A ChoiceMenu entry routes branches through EvaluateDialogCondition;
        // globalKey is a keyword-label index and unknown3/4 are unused.
        if (isChoiceMenu) {
            return new KeywordChoiceBranch { Keyword = globalKey };
        }

        // globalKey 0 always matches (default / fall-through).
        if (globalKey == 0) {
            return new DefaultBranch();
        }

        // global_flags2 namespace: key >= 56000.
        if (globalKey >= FlagBase) {
            int rel = globalKey - FlagBase;
            int group = rel / FlagStride;
            int remainder = rel % FlagStride;

            // Not divisible by 10 -> single-bit read via GetGlobalValue.
            if (remainder != 0) {
                return new FlagBitBranch {
                    FlagGroup = group,
                    Bit = remainder - 1,
                    RequiredState = BitRequiredState(unknown3, unknown4),
                };
            }

            // Divisible by 10 -> masked bitfield test.
            int xorMask = unknown3 & 0xFF;
            int matchMask = (unknown3 >> 8) & 0xFF;
            int selector = unknown4 & 0xFF;
            int chapterMask = (unknown4 >> 8) & 0xFF;

            var conditions = new List<FlagBitCondition>();
            for (var bit = 0; bit < 8; bit++) {
                if (((matchMask >> bit) & 1) != 0) {
                    // After XOR, a masked bit must read 1; so the required state
                    // of the underlying flag bit is the complement of its xor bit.
                    conditions.Add(new FlagBitCondition {
                        Bit = bit,
                        Set = ((xorMask >> bit) & 1) == 0,
                    });
                }
            }

            return new FlagMaskBranch {
                FlagGroup = group,
                Match = selector != 0 ? BranchMatchMode.All : BranchMatchMode.Any,
                Conditions = conditions,
                Chapters = DecodeChapters(chapterMask),
            };
        }

        // 1..55999: inclusive range check on GetGlobalValue(globalKey).
        return new GlobalConditionBranch {
            GlobalKey = globalKey,
            Min = unknown3,
            Max = unknown4 == NoUpperBound ? null : unknown4,
        };
    }

    // GetGlobalValue returns the single bit as 0 or 1, then the range
    // [unknown3, unknown4] (unsigned; 0xFFFF = no upper bound) is checked. That
    // reduces to a state test: the bit must be set iff 1 is in range and 0 is
    // not. All shipped data uses (1, 0xFFFF) -> set.
    private static bool BitRequiredState(ushort min, ushort max) {
        int upper = max == NoUpperBound ? 0xFFFF : max;
        bool oneMatches = min <= 1 && 1 <= upper;
        bool zeroMatches = min == 0;
        return oneMatches && !zeroMatches;
    }

    // chapter_mask test is `(1 << (chapter-1)) & mask`, with the chapter bit
    // capped at 0x80 for chapter >= 9. 0xFF therefore means "any chapter";
    // otherwise list the chapters whose bit is set (8 also covers 9+).
    private static List<int>? DecodeChapters(int chapterMask) {
        if (chapterMask == 0xFF) {
            return null;
        }
        var chapters = new List<int>();
        for (var chapter = 1; chapter <= 8; chapter++) {
            if (((chapterMask >> (chapter - 1)) & 1) != 0) {
                chapters.Add(chapter);
            }
        }
        return chapters;
    }
}
