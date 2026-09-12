namespace BetrayalAtKrondor.Tests.Dialog;

using System.Collections.Generic;

using GameData.Resources.Content;
using GameData.Resources.Dialog;
using GameData.Resources.Dialog.Branches;
using GameData.Resources.GameState;

using Xunit;

/// <summary>
/// The four branch-condition ranges that used to read as false — TASK-410.
/// </summary>
/// <remarks>
/// <b>Every one of them is a key, and the walker's job is only to resolve it.</b> The rules behind
/// the keys live in the reader (<c>GameSession.GetGlobalValue</c>) because that is where the party,
/// the packs and the dice are; these pin the resolution and the range arithmetic, which is what a
/// refactor can silently get wrong.
/// </remarks>
public class BranchConditionRangesTests {
    private const string Held = "held";
    private const string Fell = "default";

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
            Offset = 10, Key = ContentKey.ForBase("ddx:t", 10), Text = Held,
        });
        d.Entries.Add(new DialogEntry {
            Offset = 20, Key = ContentKey.ForBase("ddx:t", 20), Text = Fell,
        });
        return d;
    }

    private static string TargetOf(Condition condition, Dictionary<int, int> globals) {
        Dialog dialog = TwoWay(condition);
        DialogEntry leaf = DialogBranchWalker.WalkToLeaf(
            dialog, dialog.Entries[0],
            key => globals.TryGetValue(key, out int v) ? v : (int?)null);

        return leaf?.Text;
    }

    [Theory]
    [InlineData(1, 40001)]
    [InlineData(9, 40009)]
    [InlineData(13, 40013)]
    public void APartyCheckResolvesTo40000PlusItsNumber(int check, int key) {
        var condition = new PartyCondition { Check = check, Min = 1, Max = null };

        Assert.Equal(Held, TargetOf(condition, new Dictionary<int, int> { [key] = 1 }));
        Assert.Equal(Fell, TargetOf(condition, new Dictionary<int, int> { [key] = 0 }));
    }

    [Fact]
    public void APartyCheckIsARangeTest_NotJustNonZero() {
        // Check 4 ships as Min 0 / Max 0 — an "it is NOT the case" arm, which a non-zero test would
        // invert. Check 12 returns a COUNT, so an upper bound is meaningful there too.
        var isZero = new PartyCondition { Check = 4, Min = 0, Max = 0 };

        Assert.Equal(Held, TargetOf(isZero, new Dictionary<int, int> { [40004] = 0 }));
        Assert.Equal(Fell, TargetOf(isZero, new Dictionary<int, int> { [40004] = 1 }));
    }

    [Fact]
    public void ANoteResolvesTo51000PlusItsNumber() {
        // The three shipped notes are 19, 24 and 25, all in DIAL_Z22.
        var condition = new HasNoteCondition { Note = 24 };

        Assert.Equal(Held, TargetOf(condition, new Dictionary<int, int> { [51024] = 1 }));
        Assert.Equal(Fell, TargetOf(condition, new Dictionary<int, int> { [51024] = 0 }));
        Assert.Equal(Fell, TargetOf(condition, new Dictionary<int, int> { [51019] = 1 }));
    }

    [Fact]
    public void ASpellTimerResolvesTo52000PlusItsNumber() {
        var condition = new SpellTimerActiveCondition { Timer = 6 };

        Assert.Equal(Held, TargetOf(condition, new Dictionary<int, int> { [52006] = 1 }));
        Assert.Equal(Fell, TargetOf(condition, new Dictionary<int, int> { [52006] = 0 }));
    }

    [Fact]
    public void ARandomConditionResolvesTo53000PlusItsBound_AndRangeTestsTheRoll() {
        // The one shipped RandomCondition is Bound 4, Min 1, Max 1 — a one-in-four, in DIAL_Z19.
        var condition = new RandomCondition { Bound = 4, Min = 1, Max = 1 };

        Assert.Equal(Held, TargetOf(condition, new Dictionary<int, int> { [53004] = 1 }));
        foreach (int roll in new[] { 0, 2, 3 }) {
            Assert.Equal(Fell, TargetOf(condition, new Dictionary<int, int> { [53004] = roll }));
        }
    }

    [Fact]
    public void EachRangeKeepsToItsOwnKeys() {
        // The four bases are 1000 apart and the ranges are 13, 100, 7 and 101 wide, so nothing
        // overlaps — but a base typed one digit out would still "work" for one test and break the
        // others, which is what this asserts against.
        Assert.Equal(40000, DialogBranchWalker.PartyCheckGlobalBase);
        Assert.Equal(51000, DialogBranchWalker.NoteGlobalBase);
        Assert.Equal(52000, DialogBranchWalker.SpellTimerGlobalBase);
        Assert.Equal(53000, DialogBranchWalker.RandomGlobalBase);
        Assert.Equal(50000, DialogBranchWalker.ItemCountGlobalBase);
    }
}
