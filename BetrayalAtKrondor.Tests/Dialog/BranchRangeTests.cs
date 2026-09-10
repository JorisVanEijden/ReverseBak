namespace BetrayalAtKrondor.Tests.Dialog;

using GameData.Resources.Content;
using GameData.Resources.Dialog;
using GameData.Resources.Dialog.Branches;
using GameData.Resources.GameState;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// A branch condition is a RANGE TEST on one global key, and a null upper bound means unbounded.
/// </summary>
/// <remarks>
/// <c>DialogBranchFactory</c> decodes the DDX sentinel 0xFFFF to null. The walker read that as
/// "equal to Min", which turned every open-ended test into an exact match — measured 2026-09-10 on
/// Limm's hundred-sovereign offer, refused as "a few coins short" with 123 sovereigns in the purse.
/// </remarks>
public class BranchRangeTests {
    private const int PartyGoldInSovereigns = 30001;
    private const int GlazersGuildSeal = 93;

    private static Dialog TwoWay(Condition condition) {
        var d = new Dialog("T");
        d.Entries.Add(new DialogEntry {
            Offset = 1,
            Key = ContentKey.ForBase("ddx:t", 1),
            Branches = new List<DialogBranchBase> {
                new ConditionalBranch {
                    Condition = condition, TargetOffset = 10,
                    TargetKey = ContentKey.ForBase("ddx:t", 10),
                },
                new DefaultBranch {
                    TargetOffset = 20, TargetKey = ContentKey.ForBase("ddx:t", 20),
                },
            },
        });
        d.Entries.Add(new DialogEntry {
            Offset = 10, Key = ContentKey.ForBase("ddx:t", 10), Text = "the condition held",
        });
        d.Entries.Add(new DialogEntry {
            Offset = 20, Key = ContentKey.ForBase("ddx:t", 20), Text = "the default",
        });
        return d;
    }

    private static string Walk(Condition condition, System.Func<int, int?> globals) {
        Dialog dialog = TwoWay(condition);
        DialogEntry leaf = DialogBranchWalker.WalkToLeaf(dialog, dialog.Entries[0], globals);
        return leaf?.Text;
    }

    [Fact]
    public void AnOpenEndedVarTestPassesForAnythingAtOrAboveTheMinimum() {
        var condition = new VarCondition { Var = 1, Min = 100, Max = null };

        Assert.Equal("the condition held",
            Walk(condition, key => key == PartyGoldInSovereigns ? 123 : (int?)null));
        Assert.Equal("the condition held",
            Walk(condition, key => key == PartyGoldInSovereigns ? 100 : (int?)null));
        Assert.Equal("the default",
            Walk(condition, key => key == PartyGoldInSovereigns ? 99 : (int?)null));
    }

    [Fact]
    public void AClosedVarTestStillBoundsBothEnds() {
        // The chapter dispatch is the common closed form: Var 7, Min 2, Max 2.
        var condition = new VarCondition { Var = 7, Min = 2, Max = 2 };

        Assert.Equal("the condition held", Walk(condition, _ => 2));
        Assert.Equal("the default", Walk(condition, _ => 3));
    }

    [Fact]
    public void AnItemGateIsAReadOfGlobal50000PlusTheObjectId() {
        // GSTATE.C:45 answers 0xc350+id with itemtbl_partySize_by_kind. Returning false for these
        // made every "do you have X" branch unreachable — the seal that opens Romney's bridge
        // among them.
        var condition = new HasItemCondition { Item = GlazersGuildSeal, AtLeast = 1, AtMost = null };
        int key = DialogBranchWalker.ItemCountGlobalBase + GlazersGuildSeal;

        Assert.Equal("the condition held", Walk(condition, k => k == key ? 1 : (int?)null));
        Assert.Equal("the default", Walk(condition, k => k == key ? 0 : (int?)null));
    }

    // The 56000-stride masked-bitfield branches, which the extractor splits into AllOf/AnyOf of
    // per-bit FlagConditions plus an InChapters. 141 shipped branches, all of them false until now.
    private const int StoryFlagA = 56201;
    private const int StoryFlagB = 56202;

    private static System.Func<int, int?> Flags(int chapter, params int[] set) =>
        key => key == DialogBranchWalker.ChapterGlobalKey ? chapter
            : System.Array.IndexOf(set, key) >= 0 ? 1 : 0;

    [Fact]
    public void AllOfNeedsEveryBitToMatch() {
        // DIALOG.C:1400-1409, the selector-set arm: ((bits ^ xor) & match) == match.
        var condition = new AllOf {
            Conditions = new List<Condition> {
                new FlagCondition { Flag = StoryFlagA, Set = true },
                new FlagCondition { Flag = StoryFlagB, Set = false },
            },
        };

        Assert.Equal("the condition held", Walk(condition, Flags(3, StoryFlagA)));
        Assert.Equal("the default", Walk(condition, Flags(3, StoryFlagA, StoryFlagB)));
        Assert.Equal("the default", Walk(condition, Flags(3)));
    }

    [Fact]
    public void AnyOfNeedsOnlyOne() {
        // The selector-clear arm: ((bits ^ xor) & match) != 0 OR the chapter bit is in the mask.
        var condition = new AnyOf {
            Conditions = new List<Condition> {
                new FlagCondition { Flag = StoryFlagA, Set = true },
                new InChapters { Chapters = new List<int> { 5 } },
            },
        };

        Assert.Equal("the condition held", Walk(condition, Flags(3, StoryFlagA)));
        Assert.Equal("the condition held", Walk(condition, Flags(5)));
        Assert.Equal("the default", Walk(condition, Flags(3)));
    }

    [Fact]
    public void AnEmptyAllOfHoldsAndAnEmptyAnyOfDoesNot() {
        // Not a convention: a zero match mask makes ((bits ^ xor) & 0) == 0 hold vacuously on the
        // AND arm and fail on the OR arm, which is the same asymmetry.
        Assert.Equal("the condition held",
            Walk(new AllOf { Conditions = new List<Condition>() }, Flags(1)));
        Assert.Equal("the default",
            Walk(new AnyOf { Conditions = new List<Condition>() }, Flags(1)));
    }

    [Fact]
    public void EveryChapterPastTheEighthSharesTheEighthsBit() {
        // `chapter < 9 ? 1 << (chapter - 1) : 0x80` (DIALOG.C:1402), which is why the extractor's
        // chapter list only ever runs 1..8.
        var condition = new InChapters { Chapters = new List<int> { 8 } };

        Assert.Equal("the condition held", Walk(condition, Flags(8)));
        Assert.Equal("the condition held", Walk(condition, Flags(9)));
        Assert.Equal("the default", Walk(condition, Flags(7)));
    }

    [Fact]
    public void AnEmptyChapterListMatchesNoChapterAtAll() {
        // chapterMask 0 decodes to an empty list, and it is a real "never" arm rather than a
        // missing one — 0xFF is what means "any chapter", and DecodeChapters drops it entirely.
        var condition = new InChapters { Chapters = new List<int>() };

        for (var chapter = 1; chapter <= 9; chapter++) {
            Assert.Equal("the default", Walk(condition, Flags(chapter)));
        }
    }
}
