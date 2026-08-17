namespace BetrayalAtKrondor.Tests.Dialog;

using GameData.Resources.Dialog;
using GameData.Resources.Dialog.Branches;
using GameData.Resources.GameState;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// <c>TakeRandomBranch</c> (flag 0x0800) — the arm at 0x4a47a.
/// </summary>
public class DialogRandomBranchTests {
    private static DialogEntry Entry(string key, string text, DialogEntryFlags flags,
        params DialogBranchBase[] branches) =>
        new DialogEntry {
            Key = key, Text = text, Flags = flags,
            Branches = new List<DialogBranchBase>(branches),
        };

    private static DialogEntry Leaf(string key, string text) =>
        new DialogEntry { Key = key, Text = text, Branches = new List<DialogBranchBase>() };

    private static Dialog Of(params DialogEntry[] entries) =>
        new Dialog("test") { Entries = new List<DialogEntry>(entries) };

    private static int? NoGlobals(int key) => 0;

    private static Dialog ThreeWay(DialogEntryFlags flags) => Of(
        Entry("a", "line", flags,
            new DefaultBranch { TargetKey = "x" },
            new DefaultBranch { TargetKey = "y" },
            new DefaultBranch { TargetKey = "z" }),
        Leaf("x", "first"), Leaf("y", "second"), Leaf("z", "third"));

    [Fact]
    public void TheRollSelectsTheBranch() {
        Dialog d = ThreeWay(DialogEntryFlags.TakeRandomBranch);
        DialogEntry start = d.Entries[0];

        Assert.Equal("first", DialogBranchWalker.NextLine(d, start, NoGlobals, () => 0)?.Text);
        Assert.Equal("second", DialogBranchWalker.NextLine(d, start, NoGlobals, () => 1)?.Text);
        Assert.Equal("third", DialogBranchWalker.NextLine(d, start, NoGlobals, () => 2)?.Text);
    }

    [Fact]
    public void TheRollWrapsByTheBranchCount() =>
        // index = roll % count, so a roll past the end wraps rather than clamping.
        Assert.Equal("second",
            DialogBranchWalker.NextLine(ThreeWay(DialogEntryFlags.TakeRandomBranch),
                ThreeWay(DialogEntryFlags.TakeRandomBranch).Entries[0], NoGlobals, () => 4)?.Text);

    [Fact]
    public void WithoutTheFlagTheRollIsNeverConsulted() {
        // A conditional that does not hold plus a default: without the flag the default wins every
        // time, and — the point of the test — the roll is not asked for at all.
        Dialog d = Of(
            Entry("a", "line", DialogEntryFlags.None,
                new ConditionalBranch {
                    TargetKey = "x", Condition = new FlagCondition { Flag = 4242, Set = true },
                },
                new DefaultBranch { TargetKey = "y" }),
            Leaf("x", "gated"), Leaf("y", "open"));
        var rolls = 0;

        for (var i = 0; i < 5; i++) {
            Assert.Equal("open",
                DialogBranchWalker.NextLine(d, d.Entries[0], NoGlobals,
                    () => { rolls++; return 0; })?.Text);
        }

        Assert.Equal(0, rolls);
    }

    [Fact]
    public void ARandomEntryIgnoresBranchConditionsEntirely() {
        // The flag test jumps PAST the condition loop, so a branch whose condition is false is
        // still a candidate. Filtering by condition first — the natural thing to write — would
        // make this line unreachable.
        Dialog d = Of(
            Entry("a", "line", DialogEntryFlags.TakeRandomBranch,
                new ConditionalBranch {
                    TargetKey = "x", Condition = new FlagCondition { Flag = 4242, Set = true },
                },
                new DefaultBranch { TargetKey = "y" }),
            Leaf("x", "gated"), Leaf("y", "open"));

        // Global 4242 is 0, so the condition is FALSE — yet roll 0 still takes that branch.
        Assert.Equal("gated", DialogBranchWalker.NextLine(d, d.Entries[0], NoGlobals, () => 0)?.Text);
    }

    [Fact]
    public void WithNoRollTheFirstBranchIsTaken() =>
        // A caller that has not wired an RNG gets a valid line rather than an exception — the same
        // one every time, which is why the executor always supplies one.
        Assert.Equal("first",
            DialogBranchWalker.NextLine(ThreeWay(DialogEntryFlags.TakeRandomBranch),
                ThreeWay(DialogEntryFlags.TakeRandomBranch).Entries[0], NoGlobals)?.Text);

    [Fact]
    public void ANegativeRollStillLandsOnABranch() =>
        // Guards the modulo: C#'s % keeps the dividend's sign, so a negative roll would index
        // outside the list.
        Assert.NotNull(DialogBranchWalker.NextLine(ThreeWay(DialogEntryFlags.TakeRandomBranch),
            ThreeWay(DialogEntryFlags.TakeRandomBranch).Entries[0], NoGlobals, () => -7));

    [Fact]
    public void TheRollWindowIsTheOriginalsTwelveBits() =>
        // rand() & 0xFFF. Kept raw because the remainder of 4096 by most branch counts is biased,
        // and that bias is the shipped behaviour.
        Assert.Equal(0x1000, DialogBranchWalker.RandomBranchRollWindow);
}
