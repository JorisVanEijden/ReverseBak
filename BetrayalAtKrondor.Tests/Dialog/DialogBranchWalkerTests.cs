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
