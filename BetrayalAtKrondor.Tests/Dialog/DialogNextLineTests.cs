namespace BetrayalAtKrondor.Tests.Dialog;

using GameData.Resources.Dialog;
using GameData.Resources.Dialog.Branches;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// Continuing a conversation (<c>DialogBranchWalker.NextLine</c>). The counterpart to
/// <c>WalkToLeaf</c>: that one asks "which entry is the answer", this one asks "having said that,
/// is there more".
/// </summary>
public class DialogNextLineTests {
    private static DialogEntry Entry(string key, string text, string target = null) {
        var e = new DialogEntry { Key = key, Text = text, Branches = new List<DialogBranchBase>() };
        if (target != null) {
            e.Branches.Add(new DefaultBranch { TargetKey = target });
        }
        return e;
    }

    private static Dialog Of(params DialogEntry[] entries) =>
        new Dialog("test") { Entries = new List<DialogEntry>(entries) };

    private static int? NoGlobals(int key) => 0;

    [Fact]
    public void ALineThatBranchesOnwardHasANextLine() {
        DialogEntry first = Entry("a", "Gorath seemed distant.", target: "b");
        DialogEntry second = Entry("b", "Do you wish to bury him?");
        Dialog d = Of(first, second);

        Assert.Same(second, DialogBranchWalker.NextLine(d, first, NoGlobals));
    }

    [Fact]
    public void TheLastLineEndsTheConversation() {
        DialogEntry only = Entry("a", "It is not our way.");

        Assert.Null(DialogBranchWalker.NextLine(Of(only), only, NoGlobals));
    }

    [Fact]
    public void ATextlessEntryNeverContinues() {
        // Guards the difference from WalkToLeaf: a text-less router is something to walk THROUGH
        // while resolving, never something to continue FROM. Otherwise resolving an item
        // description would start paging through the routing table.
        DialogEntry router = Entry("a", string.Empty, target: "b");
        DialogEntry leaf = Entry("b", "A wooden staff.");

        Assert.Null(DialogBranchWalker.NextLine(Of(router, leaf), router, NoGlobals));
    }

    [Fact]
    public void ADeadEndTargetEndsTheConversation() {
        // A cross-file target resolves to nothing, exactly as the original's in-file traversal
        // treats it — the conversation stops rather than throwing.
        DialogEntry first = Entry("a", "...", target: "base:dialog:1800001");

        Assert.Null(DialogBranchWalker.NextLine(Of(first), first, NoGlobals));
    }

    [Fact]
    public void AChainWalksToItsEnd() {
        // The DIAL_Z16 shape: a flat run of spoken lines, each pointing at the next.
        DialogEntry a = Entry("a", "one", "b");
        DialogEntry b = Entry("b", "two", "c");
        DialogEntry c = Entry("c", "three");
        Dialog d = Of(a, b, c);

        var said = new List<string>();
        DialogEntry cur = a;
        while (cur != null) {
            said.Add(cur.Text);
            cur = DialogBranchWalker.NextLine(d, cur, NoGlobals);
        }

        Assert.Equal(new[] { "one", "two", "three" }, said);
    }

    [Fact]
    public void NothingToContinueFromIsNotAnError() {
        Assert.Null(DialogBranchWalker.NextLine(null, null, NoGlobals));
        Assert.Null(DialogBranchWalker.NextLine(Of(), null, NoGlobals));
    }
}
