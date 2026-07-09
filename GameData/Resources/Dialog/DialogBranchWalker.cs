namespace GameData.Resources.Dialog;

using GameData.Resources.Dialog.Branches;
using GameData.Resources.GameState;
using System;
using System.Collections.Generic;

/// <summary>
/// Faithful port of ExecuteDialog's branch traversal (KRONDOR.EXE 0x494bb): an entry with no
/// leaf text is resolved by following the first satisfied ConditionalBranch (else the
/// DefaultBranch) via TargetOffset to the next entry, until one has displayable text.
/// </summary>
public static class DialogBranchWalker {
    private const int MaxHops = 32; // guard against malformed/cyclic data

    public static DialogEntry WalkToLeaf(Dialog dialog, DialogEntry start, Func<int, int?> getGlobal) {
        if (dialog == null || start == null) {
            return start;
        }
        var byOffset = new Dictionary<int, DialogEntry>();
        foreach (DialogEntry e in dialog.Entries) {
            byOffset[e.Offset] = e;
        }
        DialogEntry current = start;
        for (int hop = 0; hop < MaxHops; hop++) {
            if (!string.IsNullOrEmpty(current.Text)) {
                return current; // leaf
            }
            DialogBranchBase chosen = ChooseBranch(current, getGlobal);
            if (chosen?.TargetOffset == null || !byOffset.TryGetValue(chosen.TargetOffset.Value, out DialogEntry next)) {
                return current; // dead end (incl. sentinel offset 0 / cross-file TargetId)
            }
            current = next;
        }
        return current;
    }

    private static DialogBranchBase ChooseBranch(DialogEntry entry, Func<int, int?> getGlobal) {
        DialogBranchBase fallback = null;
        foreach (DialogBranchBase b in entry.Branches) {
            if (b is DefaultBranch) { fallback = b; continue; }
            if (b is ConditionalBranch cb && Holds(cb.Condition, getGlobal)) { return cb; }
        }
        return fallback;
    }

    // Faithful for FlagCondition (the corpse path); unknown conditions -> false (skip).
    private static bool Holds(Condition condition, Func<int, int?> getGlobal) {
        if (condition is FlagCondition f) {
            return ((getGlobal(f.Flag) ?? 0) != 0) == f.Set;
        }
        return false;
    }
}
