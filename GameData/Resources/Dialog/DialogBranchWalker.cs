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

    /// <param name="onEntryVisited">Runs for every entry the walk touches, in order, INCLUDING the
    /// leaf it stops on. The engine's op loop runs per record it reaches, not only on the one it
    /// ends up displaying (DIALOG.C:855-870) — and that matters: 57 shipped entries are text-less
    /// routers whose only job is to fill a text variable before branching to the leaf that uses it.
    /// Skipping them leaves those tokens showing the seeded default instead of what the dialog
    /// meant.</param>
    public static DialogEntry WalkToLeaf(Dialog dialog, DialogEntry start, Func<int, int?> getGlobal,
        Action<Effect> applyEffect = null, Action<DialogEntry> onEntryVisited = null,
        Func<int> roll = null) {
        if (dialog == null || start == null) {
            return start;
        }
        Dictionary<string, DialogEntry> byKey = BuildKeyIndex(dialog);
        DialogEntry current = start;
        for (int hop = 0; hop < MaxHops; hop++) {
            onEntryVisited?.Invoke(current);
            ApplyEffects(current, applyEffect);
            if (!string.IsNullOrEmpty(current.Text)) {
                return current; // leaf
            }
            DialogBranchBase chosen = ChooseBranch(current, getGlobal, roll);
            if (chosen?.TargetKey == null || !byKey.TryGetValue(chosen.TargetKey, out DialogEntry next)) {
                return current; // dead end (incl. sentinel target / cross-file id key)
            }
            current = next;
        }
        return current;
    }

    /// <summary>
    /// The next LINE of a conversation, or null when this one ends it.
    /// </summary>
    /// <param name="dialog">The dialog the entry belongs to.</param>
    /// <param name="current">A line that has just been shown.</param>
    /// <param name="getGlobal">Reads a global, for a continuation that branches on state.</param>
    /// <remarks>
    /// <b>A branch out of an entry that HAS text means "then say this".</b> Most spoken dialog in
    /// the game is a flat chain of such entries — 3464 of 5932 text-bearing entries across the
    /// shipped DDX carry one — and <see cref="WalkToLeaf"/> deliberately stops at the first of them,
    /// because for CONDITIONAL ROUTING (an item description, say) the first text IS the answer. This
    /// is the other question: having shown a line, is there another?
    ///
    /// <para>Returns null for an entry with no text, so this can only ever continue a conversation
    /// that has started — and null at a dead end, which is how the last line is recognised.</para>
    ///
    /// <para>Branch choice goes through the same <c>ChooseBranch</c> the routing walk uses rather
    /// than assuming an unconditional default, so a continuation that depends on state picks the
    /// same successor the original would.</para>
    /// </remarks>
    public static DialogEntry NextLine(Dialog dialog, DialogEntry current, Func<int, int?> getGlobal,
        Func<int> roll = null) {
        if (dialog == null || current == null || string.IsNullOrEmpty(current.Text)) {
            return null;
        }
        DialogBranchBase chosen = ChooseBranch(current, getGlobal, roll);
        if (chosen?.TargetKey == null) {
            return null;
        }
        return BuildKeyIndex(dialog).TryGetValue(chosen.TargetKey, out DialogEntry next) ? next : null;
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

    /// <summary>
    /// The roll's range — the original's <c>GetRandomNumber() &amp; 0xFFF</c>, so 0..4095.
    /// </summary>
    /// <remarks>
    /// Kept as the raw window rather than folded into a "pick one of n" helper because the original
    /// takes the remainder of THIS number, and 4096 does not divide evenly by most branch counts.
    /// The resulting bias towards the low branches is the shipped behaviour; a uniform pick would
    /// be a different distribution.
    /// </remarks>
    public const int RandomBranchRollWindow = 0x1000;

    private static DialogBranchBase ChooseBranch(DialogEntry entry, Func<int, int?> getGlobal,
        Func<int> roll = null) {
        if ((entry.Flags & DialogEntryFlags.TakeRandomBranch) != 0 && entry.Branches.Count > 0) {
            return RandomBranch(entry, roll);
        }

        DialogBranchBase fallback = null;
        foreach (DialogBranchBase b in entry.Branches) {
            if (b is DefaultBranch) { fallback = b; continue; }
            if (b is ConditionalBranch cb && Holds(cb.Condition, getGlobal)) { return cb; }
        }
        return fallback;
    }

    /// <summary>
    /// One branch chosen at random — the <c>TakeRandomBranch</c> arm at 0x4a47a.
    /// </summary>
    /// <remarks>
    /// <b>No branch condition is evaluated at all.</b> The flag test comes first and jumps clean
    /// past the condition loop, so a conditional branch on a random entry is never consulted and a
    /// DefaultBranch has no special standing — every branch is equally a candidate, including ones
    /// whose condition is false. Filtering by condition first, which is the natural thing to write,
    /// would change which lines can come up.
    ///
    /// <para>With no roll supplied this takes the first branch, so a caller that has not wired an
    /// RNG gets a stable, valid line rather than an exception — but it gets the SAME one every
    /// time, which is why the executor always passes one.</para>
    /// </remarks>
    private static DialogBranchBase RandomBranch(DialogEntry entry, Func<int> roll) {
        if (roll == null) {
            return entry.Branches[0];
        }

        int index = Math.Abs(roll()) % entry.Branches.Count;
        return entry.Branches[index];
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
