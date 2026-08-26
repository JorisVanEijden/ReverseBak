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

    // Mirrors the chapter-setup dialog DIAL_Z20 #2000023: entry pushes a branch node (Var-7 chapter
    // switch) whose selected chapter leaf carries the ChangeParty action. The default branch targets a
    // cross-file id (unresolvable in-file), so traversal must fall back to the PushDialogEntry.
    [Fact] public void ExecuteActions_FollowsPushThenChapterBranch_AppliesLeafActions() {
        var change = new ChangePartyAction { PartySize = 3, Member1 = 0, Member2 = 2, Member3 = 1 };
        var leaf = EA(671, new DialogActionBase[] { change });
        var branchNode = E(223,
            null,
            new ConditionalBranch { Condition = new VarCondition { Var = 7, Min = 1, Max = 1 }, TargetOffset = 671 });
        var root = EA(194,
            new DialogActionBase[] { new PushDialogEntryAction { Offset = 223 } },
            new DefaultBranch { TargetOffset = null }); // cross-file return in the real data

        var applied = new List<DialogActionBase>();
        DialogBranchWalker.ExecuteActions(
            Dlg(root, branchNode, leaf), root, k => k == 30007 ? 1 : 0, applied.Add);

        var change2 = applied.OfType<ChangePartyAction>().Single();
        Assert.Equal(3, change2.PartySize);
        Assert.Equal(new[] { 0, 2, 1 }, new[] { change2.Member1, change2.Member2, change2.Member3 });
    }

    [Fact] public void ExecuteActions_WrongChapterBranch_DoesNotReachLeaf() {
        var leaf = EA(671, new DialogActionBase[] { new ChangePartyAction { PartySize = 3 } });
        var branchNode = E(223,
            null,
            new ConditionalBranch { Condition = new VarCondition { Var = 7, Min = 1, Max = 1 }, TargetOffset = 671 });
        var root = EA(194,
            new DialogActionBase[] { new PushDialogEntryAction { Offset = 223 } },
            new DefaultBranch { TargetOffset = null });

        var applied = new List<DialogActionBase>();
        DialogBranchWalker.ExecuteActions(
            Dlg(root, branchNode, leaf), root, k => k == 30007 ? 2 : 0, applied.Add); // chapter 2

        Assert.Empty(applied.OfType<ChangePartyAction>());
    }
}
