namespace BetrayalAtKrondor.Tests.Dialog;
using GameData.Resources.Dialog;
using GameData.Resources.Dialog.Branches;
using GameData.Resources.GameState;
using System.Collections.Generic;
using Xunit;

public class DialogBranchWalkerTests {
    private static DialogEntry E(int offset, string text, params DialogBranchBase[] br) =>
        new DialogEntry { Offset = offset, Text = text, Branches = new List<DialogBranchBase>(br) };

    private static Dialog Dlg(params DialogEntry[] es) {
        var d = new Dialog("T"); d.Entries.AddRange(es); return d;
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
}
