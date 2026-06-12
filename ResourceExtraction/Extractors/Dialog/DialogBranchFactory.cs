namespace ResourceExtraction.Extractors.Dialog;

using GameData.Resources.Dialog.Branches;
using GameData.Resources.GameState;

using ResourceExtraction.Extractors.GameState;

using System.Collections.Generic;

/// <summary>
/// Decodes the raw branch triple (<c>globalKey</c>, <c>unknown3</c>,
/// <c>unknown4</c>) into a semantic <see cref="DialogBranchBase"/>. Global-state
/// gates become a <see cref="ConditionalBranch"/> carrying a shared
/// <see cref="Condition"/> (via <see cref="GlobalRef"/>); the 56000+ masked form
/// is decoded here into a composite condition because it repurposes
/// unknown3/unknown4 as bitmasks. See <c>docs/specs/dialog-system.md</c> and
/// <c>docs/specs/global-value-destructuring.md</c>.
/// </summary>
internal static class DialogBranchFactory {
    private const ushort NoUpperBound = 0xFFFF;
    private const int FlagBase = 56000;
    private const int FlagStride = 10;

    public static DialogBranchBase Build(
        bool isChoiceMenu, ushort globalKey, ushort unknown3, ushort unknown4, long rawTarget) {
        DialogBranchBase branch = DecodeBranch(isChoiceMenu, globalKey, unknown3, unknown4);

        if (rawTarget >= 0x80000000) {
            branch.TargetId = (int)(rawTarget - 0x80000000);
        } else {
            branch.TargetOffset = (int)rawTarget;
        }
        return branch;
    }

    private static DialogBranchBase DecodeBranch(
        bool isChoiceMenu, ushort globalKey, ushort unknown3, ushort unknown4) {
        if (isChoiceMenu) {
            return new KeywordChoiceBranch { Keyword = globalKey };
        }
        if (globalKey == 0) {
            return new DefaultBranch();
        }

        // 56000+ divisible by 10: masked bitfield test (uses unknown3/unknown4 as masks).
        if (globalKey >= FlagBase && (globalKey - FlagBase) % FlagStride == 0) {
            return new ConditionalBranch { Condition = DecodeMaskedFlags(globalKey, unknown3, unknown4) };
        }

        // Everything else: a range test on a single key -> shared decoder.
        int? max = unknown4 == NoUpperBound ? null : unknown4;
        return new ConditionalBranch { Condition = GlobalRef.DecodeCondition(globalKey, unknown3, max) };
    }

    private static Condition DecodeMaskedFlags(ushort globalKey, ushort unknown3, ushort unknown4) {
        int group = (globalKey - FlagBase) / FlagStride;
        int xorMask = unknown3 & 0xFF;
        int matchMask = (unknown3 >> 8) & 0xFF;
        int selector = unknown4 & 0xFF;
        int chapterMask = (unknown4 >> 8) & 0xFF;

        var flags = new List<Condition>();
        for (var bit = 0; bit < 8; bit++) {
            if (((matchMask >> bit) & 1) != 0) {
                flags.Add(new FlagCondition {
                    Flag = FlagBase + group * FlagStride + bit + 1,
                    Set = ((xorMask >> bit) & 1) == 0,
                });
            }
        }

        List<int>? chapters = DecodeChapters(chapterMask);
        if (chapters != null) {
            flags.Add(new InChapters { Chapters = chapters });
        }

        return selector != 0
            ? new AllOf { Conditions = flags }
            : new AnyOf { Conditions = flags };
    }

    // chapter_mask test is `(1 << (chapter-1)) & mask`, chapter bit capped at 0x80
    // for chapter >= 9. 0xFF = any chapter (null); else list set chapters (8 covers 9+).
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
