namespace BetrayalAtKrondor.Tests.Dialog;
using GameData.Resources.Content;
using GameData.Resources.Dialog;
using GameData.Resources.Dialog.Actions;
using GameData.Resources.Dialog.Branches;
using GameData.Resources.GameState;
using System.Collections.Generic;
using System.Linq;
using Xunit;

public class DialogBranchWalkerTests {
    private static DialogEntry E(int offset, string text, params DialogBranchBase[] br) =>
        new DialogEntry { Offset = offset, Text = text, Branches = new List<DialogBranchBase>(br) };

    private static DialogEntry EA(int offset, IEnumerable<DialogActionBase> actions, params DialogBranchBase[] br) =>
        new DialogEntry {
            Offset = offset,
            Actions = new List<DialogActionBase>(actions),
            Branches = new List<DialogBranchBase>(br),
        };

    private static Dialog Dlg(params DialogEntry[] es) {
        var d = new Dialog("T"); d.Entries.AddRange(es); StampKeys(d); return d;
    }

    // Mirrors DdxExtractor.StampDialogKeys so the synthetic dialogs the walker consumes carry the
    // same de-indexed keys the real extractor emits (entry Key + branch/push TargetKey). Dialog id
    // "T" → file segment "t", so entry keys are base:ddx:t:<offset>.
    private static void StampKeys(Dialog d) {
        const long idBit = 0x80000000;
        foreach (DialogEntry e in d.Entries) {
            e.Key = ContentKey.ForBase("ddx:t", e.Offset);
            foreach (DialogBranchBase b in e.Branches) {
                if (b.TargetOffset is int off) {
                    b.TargetKey = off == 0 ? null : ContentKey.ForBase("ddx:t", off);
                } else if (b.TargetId is int id) {
                    b.TargetKey = ContentKey.ForBase("dialog", id);
                }
            }
            foreach (PushDialogEntryAction push in e.Actions.OfType<PushDialogEntryAction>()) {
                uint raw = (uint)push.Offset;
                push.TargetKey = raw >= idBit ? ContentKey.ForBase("dialog", (int)(raw - idBit))
                    : raw == 0 ? null : ContentKey.ForBase("ddx:t", (int)raw);
            }
        }
    }

    // ---- TASK-543: the push stack a conversation carries ------------------------------------

    [Fact]
    public void AWalkPushesOnlyForARecordThatHasANextRecord() {
        // DIAL_Z30 @133248's shape: a text-less router pushes its farewell (@133277) and defaults on to
        // the next record. Op 0x10 is stacked only while record_key != 0 (DIALOG.C:1441), so the router
        // pushes and the leaf it lands on - which has no branch - pushes nothing.
        var router = EA(100, new DialogActionBase[] { new PushDialogEntryAction { Offset = 300 } },
            new DefaultBranch { TargetOffset = 200 });
        var leaf = EA(200, new DialogActionBase[] { new PushDialogEntryAction { Offset = 400 } });
        leaf.Text = "Your will, lord?";
        var pushed = new Stack<string>();

        DialogEntry reached = DialogBranchWalker.WalkToLeaf(Dlg(router, leaf), router, _ => null, pushed: pushed);

        Assert.Same(leaf, reached);
        Assert.Equal(new[] { "base:ddx:t:300" }, pushed.ToArray());
    }

    // ---- Northwarden's router: three conditionals that END, then the default that acts -----

    [Fact]
    public void ARouterWhoseConditionalsAllFailTakesTheDefaultAndRunsItsActions() {
        // DIAL_Z15 offset 25808 (dialog 1500156) — the Great Hall hotspot's action dialog. Var 7 in
        // 1..4 ends, Var 7 in 6..9 ends, flag 7633 CLEAR ends, and only the default reaches 25857,
        // which is where SetReturnValue(-4) lives. Every "end" is a ConditionalBranch with a null
        // target, so a walker that treated one as a dead end would look identical to a walker that
        // took it — hence the leaf assertion rather than a "did not throw".
        var router = EA(25808, new DialogActionBase[0],
            new ConditionalBranch { Condition = new VarCondition { Var = 7, Min = 1, Max = 4 }, TargetOffset = 0 },
            new ConditionalBranch { Condition = new VarCondition { Var = 7, Min = 6, Max = 9 }, TargetOffset = 0 },
            new ConditionalBranch { Condition = new FlagCondition { Flag = 7633, Set = false }, TargetOffset = 0 },
            new DefaultBranch { TargetOffset = 25857 });
        var leaf = EA(25857, new DialogActionBase[] { new SetReturnValueAction { Value = -4 } });
        Dialog d = Dlg(router, leaf);

        int? Globals(int key) => key == 30007 ? 5 : key == 7633 ? 1 : (int?)0;

        var visited = new List<int>();
        DialogEntry got = DialogBranchWalker.WalkToLeaf(d, router, Globals,
            onEntryVisited: e => visited.Add(e.Offset));

        Assert.Equal(25857, got.Offset);
        Assert.Equal(new[] { 25808, 25857 }, visited);
    }

    [Fact]
    public void TheSameRouterInChapterFourStopsAtItselfWithNoReturnValue() {
        // The control: in chapter 1..4 the FIRST branch wins and the dialog ends where it started,
        // so the hotspot keeps its own action code. Without this the test above would pass for a
        // walker that ignored conditions entirely.
        var router = EA(25808, new DialogActionBase[0],
            new ConditionalBranch { Condition = new VarCondition { Var = 7, Min = 1, Max = 4 }, TargetOffset = 0 },
            new ConditionalBranch { Condition = new VarCondition { Var = 7, Min = 6, Max = 9 }, TargetOffset = 0 },
            new ConditionalBranch { Condition = new FlagCondition { Flag = 7633, Set = false }, TargetOffset = 0 },
            new DefaultBranch { TargetOffset = 25857 });
        var leaf = EA(25857, new DialogActionBase[] { new SetReturnValueAction { Value = -4 } });
        Dialog d = Dlg(router, leaf);

        int? Globals(int key) => key == 30007 ? 4 : key == 7633 ? 1 : (int?)0;

        DialogEntry got = DialogBranchWalker.WalkToLeaf(d, router, Globals);

        Assert.Equal(25808, got.Offset);
    }

    // ---- Id-addressed targets ------------------------------------------------------------
    //
    // A branch names its target by offset-in-this-file OR by global dialog id, and this walker can
    // only follow the first. Squire Phillip's router (DIAL_Z30 offset 131137) is empty and carries
    // a single default branch to dialog 2000001 — the shared ask-about page — so read as a dead end
    // his conversation ends where the topic list belongs. That is the bug these pin.

    [Fact]
    public void AnIdAddressedBranchIsReportedRatherThanSwallowed() {
        var router = E(131137, null, new DefaultBranch { TargetId = 2000001 });
        Dialog d = Dlg(router);

        // The walk itself still stops — it only indexes this file.
        Assert.Same(router, DialogBranchWalker.WalkToLeaf(d, router, _ => 0));
        // ...but the destination is recoverable, which is what lets the caller load the other DDX.
        Assert.Equal(2000001, DialogBranchWalker.IdAddressedTargetOf(router, _ => 0));
    }

    [Fact]
    public void AnOffsetBranchIsNotMistakenForAnIdAddressedOne() {
        // *** The failure this catches. *** Deciding it by whether the key resolves would send
        // every dangling in-file offset off to load a dialog named by a null id. The destination
        // FIELD decides, the same way bit 31 of the key decides in the original.
        var router = E(200, null, new DefaultBranch { TargetOffset = 300 });
        Assert.Null(DialogBranchWalker.IdAddressedTargetOf(router, _ => 0));
    }

    [Fact]
    public void ADanglingOffsetIsStillNotAnIdAddressedHop() {
        var router = E(200, null, new DefaultBranch { TargetOffset = 999 });
        Dialog d = Dlg(router); // 999 is not in the file
        Assert.Same(router, DialogBranchWalker.WalkToLeaf(d, router, _ => 0));
        Assert.Null(DialogBranchWalker.IdAddressedTargetOf(router, _ => 0));
    }

    [Fact]
    public void TheIdAddressedTargetFollowsTHESAMEBranchTheWalkChose() {
        // A router whose choice depends on state must not report the other arm's destination.
        var router = E(500, null,
            new ConditionalBranch { Condition = new FlagCondition { Flag = 42, Set = true }, TargetId = 111 },
            new DefaultBranch { TargetId = 222 });
        Assert.Equal(111, DialogBranchWalker.IdAddressedTargetOf(router, k => k == 42 ? 1 : 0));
        Assert.Equal(222, DialogBranchWalker.IdAddressedTargetOf(router, _ => 0));
    }

    [Fact]
    public void AnEntryWithNoBranchesAtAllReportsNothing() {
        Assert.Null(DialogBranchWalker.IdAddressedTargetOf(E(700, null), _ => 0));
        Assert.Null(DialogBranchWalker.IdAddressedTargetOf(null, _ => 0));
    }

    [Fact]
    public void AChoiceMenuIsWhereTheWalkSTOPS_NotSomethingItWalksThrough() {
        // Dialog 2000001 is the shape: no text, and twelve KeywordChoiceBranches. The walk has to
        // hand it back so the renderer can draw the topic grid — walking INTO a topic would answer
        // a question the player was never asked, and there is no id-addressed hop to chase either.
        var menu = E(3000, null,
            new KeywordChoiceBranch { Keyword = 1, TargetOffset = 3799 },
            new KeywordChoiceBranch { Keyword = 2, TargetOffset = 4359 });
        var topic = E(3799, "about the inns");
        Assert.Same(menu, DialogBranchWalker.WalkToLeaf(Dlg(menu, topic), menu, _ => 0));
        Assert.Null(DialogBranchWalker.IdAddressedTargetOf(menu, _ => 0));
    }

    [Fact] public void LeafWithText_ReturnedAsIs() {
        var leaf = E(100, "hello");
        Assert.Same(leaf, DialogBranchWalker.WalkToLeaf(Dlg(leaf), leaf, _ => 0));
    }

    [Fact] public void DefaultBranch_FollowedToLeaf() {
        var leaf = E(200, "loot message");
        var root = E(78, null, new DefaultBranch { TargetOffset = 200 });
        Assert.Same(leaf, DialogBranchWalker.WalkToLeaf(Dlg(root, leaf), root, _ => 0));
    }

    [Fact] public void ConditionalBranch_TakenWhenFlagSet_ElseDefault() {
        var yes = E(300, "yes"); var no = E(400, "no");
        var root = E(78, null,
            new ConditionalBranch { Condition = new FlagCondition { Flag = 8127, Set = true }, TargetOffset = 300 },
            new DefaultBranch { TargetOffset = 400 });
        Assert.Same(yes, DialogBranchWalker.WalkToLeaf(Dlg(root, yes, no), root, k => k == 8127 ? 1 : 0));
        Assert.Same(no,  DialogBranchWalker.WalkToLeaf(Dlg(root, yes, no), root, _ => 0)); // flag unset -> default
    }

    [Fact] public void MultiHop_WalksUntilText() {
        var leaf = E(500, "deep");
        var mid  = E(400, null, new DefaultBranch { TargetOffset = 500 });
        var root = E(78, null, new DefaultBranch { TargetOffset = 400 });
        Assert.Same(leaf, DialogBranchWalker.WalkToLeaf(Dlg(root, mid, leaf), root, _ => 0));
    }

    [Fact] public void VarCondition_TakenWhenChapterInRange() {
        // Var 7 = global key 30007 = chapter number.
        var ch1 = E(671, "chapter one");
        var root = E(223, null,
            new ConditionalBranch { Condition = new VarCondition { Var = 7, Min = 1, Max = 1 }, TargetOffset = 671 },
            new ConditionalBranch { Condition = new VarCondition { Var = 7, Min = 2, Max = 2 }, TargetOffset = 999 });
        Assert.Same(ch1, DialogBranchWalker.WalkToLeaf(Dlg(root, ch1), root, k => k == 30007 ? 1 : 0));
        // Chapter 2 -> the first (chapter-1) branch must NOT be taken.
        Assert.Same(root, DialogBranchWalker.WalkToLeaf(Dlg(root, ch1), root, k => k == 30007 ? 2 : 0));
    }

    // Mirrors the chapter-setup dialog DIAL_Z20 #2000023: the entry pushes the Var-7 chapter node (223)
    // and its default branch targets id 2000026, an entry in the SAME file that clears story flags.
    // DIALOG.C:1441-1475 follows the branch first and pops the push when that tree ends.
    private static (Dialog dialog, DialogEntry root) ChapterSetupDialog() {
        var leaf = EA(671, new DialogActionBase[] { new ChangePartyAction { PartySize = 3, Member1 = 0, Member2 = 2, Member3 = 1 } });
        var branchNode = E(223,
            null,
            new ConditionalBranch { Condition = new VarCondition { Var = 7, Min = 1, Max = 1 }, TargetOffset = 671 });
        var clears = EA(322, new DialogActionBase[] {
            new GlobalEffectAction { Effect = new SetFlagEffect { Flag = 7812, Set = false } },
            new GlobalEffectAction { Effect = new SetFlagEffect { Flag = 7865, Set = false } },
        });
        clears.Id = 2000026;
        var root = EA(194,
            new DialogActionBase[] { new PushDialogEntryAction { Offset = 223 } },
            new DefaultBranch { TargetId = 2000026 });
        return (Dlg(root, branchNode, leaf, clears), root);
    }

    [Fact] public void ExecuteActions_FollowsTheIdBranchThenPopsThePush() {
        var (dialog, root) = ChapterSetupDialog();

        var applied = new List<DialogActionBase>();
        DialogBranchWalker.ExecuteActions(dialog, root, k => k == 30007 ? 1 : 0, applied.Add);

        var flags = applied.OfType<GlobalEffectAction>().Select(g => ((SetFlagEffect)g.Effect).Flag).ToArray();
        Assert.Equal(new[] { 7812, 7865 }, flags);
        var change = applied.OfType<ChangePartyAction>().Single();
        Assert.Equal(new[] { 0, 2, 1 }, new[] { change.Member1, change.Member2, change.Member3 });
        // The clears come before the party change: the branch tree runs, then the push pops.
        Assert.True(applied.FindIndex(a => a is GlobalEffectAction) < applied.FindIndex(a => a is ChangePartyAction));
    }

    [Fact] public void ExecuteActions_WrongChapterBranch_StillClearsButDoesNotReachLeaf() {
        var (dialog, root) = ChapterSetupDialog();

        var applied = new List<DialogActionBase>();
        DialogBranchWalker.ExecuteActions(dialog, root, k => k == 30007 ? 2 : 0, applied.Add); // chapter 2

        Assert.Equal(2, applied.OfType<GlobalEffectAction>().Count());
        Assert.Empty(applied.OfType<ChangePartyAction>());
    }

    [Fact] public void ExecuteActions_APushOnAnEntryWithNoBranchIsNotFollowed() {
        // op 0x10 is stacked only while record_key != 0 — an entry that ends its tree pushes nothing.
        var leaf = EA(671, new DialogActionBase[] { new ChangePartyAction { PartySize = 3 } });
        var root = EA(194, new DialogActionBase[] { new PushDialogEntryAction { Offset = 671 } });

        var applied = new List<DialogActionBase>();
        DialogBranchWalker.ExecuteActions(Dlg(root, leaf), root, _ => 0, applied.Add);

        Assert.Empty(applied.OfType<ChangePartyAction>());
    }
}
