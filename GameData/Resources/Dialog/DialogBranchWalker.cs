namespace GameData.Resources.Dialog;

using GameData.Resources.Dialog.Actions;
using GameData.Resources.Dialog.Branches;
using GameData.Resources.GameState;
using System;
using System.Collections.Generic;

/// <summary>
/// Faithful port of ExecuteDialog's branch traversal (KRONDOR.EXE 0x494bb): an entry with no
/// leaf text is resolved by following the first satisfied ConditionalBranch (else the
/// DefaultBranch) via TargetOffset to the next entry, until one has displayable text. Each visited
/// entry's <see cref="GlobalEffectAction"/>s are applied via <paramref name="applyEffect"/> as the
/// entry is processed — that's how a shown dialog mutates global flags (e.g. the corpse-flavor
/// "recently examined" flag), matching ExecuteDialog running an entry's actions when it reaches it.
/// </summary>
public static class DialogBranchWalker {
    private const int MaxHops = 32; // guard against malformed/cyclic data

    public static DialogEntry WalkToLeaf(Dialog dialog, DialogEntry start, Func<int, int?> getGlobal,
        Action<Effect> applyEffect = null) {
        if (dialog == null || start == null) {
            return start;
        }
        Dictionary<string, DialogEntry> byKey = BuildKeyIndex(dialog);
        DialogEntry current = start;
        for (int hop = 0; hop < MaxHops; hop++) {
            ApplyEffects(current, applyEffect);
            if (!string.IsNullOrEmpty(current.Text)) {
                return current; // leaf
            }
            DialogBranchBase chosen = ChooseBranch(current, getGlobal);
            if (chosen?.TargetKey == null || !byKey.TryGetValue(chosen.TargetKey, out DialogEntry next)) {
                return current; // dead end (incl. sentinel target / cross-file id key)
            }
            current = next;
        }
        return current;
    }

    // De-indexed entry index: entries keyed by their stable content key (base:ddx:<file>:<offset>).
    // A branch/push TargetKey resolves here only for same-file offset targets; a cross-file
    // base:dialog:<id> key is absent (that DialogEntry lives in another DDX) and reads as a dead end,
    // matching the original engine's in-file traversal.
    private static Dictionary<string, DialogEntry> BuildKeyIndex(Dialog dialog) {
        var byKey = new Dictionary<string, DialogEntry>();
        foreach (DialogEntry e in dialog.Entries) {
            byKey[e.Key] = e;
        }
        return byKey;
    }

    private static void ApplyEffects(DialogEntry entry, Action<Effect> applyEffect) {
        if (applyEffect == null) {
            return;
        }
        foreach (DialogActionBase action in entry.Actions) {
            if (action is GlobalEffectAction g && g.Effect != null) {
                applyEffect(g.Effect);
            }
        }
    }

    /// <summary>
    /// Execute a side-effect dialog: walk from <paramref name="start"/>, handing every action on
    /// each visited entry to <paramref name="apply"/>, then following the chosen continuation.
    /// Faithful to <c>ExecuteDialog</c> running an entry's actions and then taking either its chosen
    /// branch or a queued <see cref="PushDialogEntryAction"/> continuation (KRONDOR.EXE 0x494bb).
    /// Used for the chapter-setup dialog <c>go_to_chapter_impl → dialog_Show(2000023)</c> (0x41f0a):
    /// entry 2000023 pushes the Var-7 (chapter) branch node, whose selected chapter leaf carries the
    /// <see cref="ChangePartyAction"/> that fixes the runtime party/head order (DIAL_Z20.DDX).
    /// Unlike <see cref="WalkToLeaf"/> this does not stop on displayable text — these entries have
    /// none; it runs purely for the actions.
    /// </summary>
    public static void ExecuteActions(Dialog dialog, DialogEntry start, Func<int, int?> getGlobal,
        Action<DialogActionBase> apply) {
        if (dialog == null || start == null || apply == null) {
            return;
        }
        Dictionary<string, DialogEntry> byKey = BuildKeyIndex(dialog);
        DialogEntry current = start;
        for (int hop = 0; hop < MaxHops && current != null; hop++) {
            foreach (DialogActionBase action in current.Actions) {
                apply(action);
            }
            current = ResolveContinuation(current, getGlobal, byKey);
        }
    }

    // Pick the next entry to process: prefer a chosen branch whose target resolves in-file; otherwise
    // fall back to a PushDialogEntry continuation (the engine's LIFO — popped when the current tree
    // returns via a cross-file/return branch target, as entry 2000023's default branch does).
    private static DialogEntry ResolveContinuation(DialogEntry entry, Func<int, int?> getGlobal,
        Dictionary<string, DialogEntry> byKey) {
        DialogBranchBase chosen = ChooseBranch(entry, getGlobal);
        if (chosen?.TargetKey != null && byKey.TryGetValue(chosen.TargetKey, out DialogEntry viaBranch)) {
            return viaBranch;
        }
        foreach (DialogActionBase action in entry.Actions) {
            // A same-file offset push resolves here; a cross-file base:dialog:<id> key (or null
            // sentinel) is absent from byKey and is skipped, matching the original.
            if (action is PushDialogEntryAction push
                && push.TargetKey != null
                && byKey.TryGetValue(push.TargetKey, out DialogEntry viaPush)) {
                return viaPush;
            }
        }
        return null;
    }

    private static DialogBranchBase ChooseBranch(DialogEntry entry, Func<int, int?> getGlobal) {
        DialogBranchBase fallback = null;
        foreach (DialogBranchBase b in entry.Branches) {
            if (b is DefaultBranch) { fallback = b; continue; }
            if (b is ConditionalBranch cb && Holds(cb.Condition, getGlobal)) { return cb; }
        }
        return fallback;
    }

    // Faithful for FlagCondition (the corpse path) and VarCondition (the named-variable range
    // 30000+Var, e.g. Var 7 = chapter for the chapter-setup branch); unknown conditions -> false.
    private static bool Holds(Condition condition, Func<int, int?> getGlobal) {
        if (condition is FlagCondition f) {
            return ((getGlobal(f.Flag) ?? 0) != 0) == f.Set;
        }
        if (condition is VarCondition v) {
            int value = getGlobal(30000 + v.Var) ?? 0;
            return value >= v.Min && value <= (v.Max ?? v.Min);
        }
        return false;
    }
}
