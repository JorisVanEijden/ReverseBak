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
                // Dead end BY OFFSET. An id-addressed target is not really a dead end — see
                // IdAddressedTargetOf, which the caller resolves by loading that dialog.
                return current;
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

    /// <summary>
    /// The dialog id a walk that stopped on <paramref name="entry"/> was heading for, when its
    /// chosen branch names its destination by ID rather than by offset — otherwise null.
    /// </summary>
    /// <remarks>
    /// <b>The two addressing modes are not interchangeable, and this walker can only follow one of
    /// them.</b> A branch names its target either as an offset into the file it is already in or as
    /// a global dialog id; the original picks between them on bit 31 of the key and, for an id,
    /// re-derives the FILE from the id itself (<c>key / 100000</c> names the DDX) before loading.
    /// This walker indexes one file by offset, so every id-addressed branch — including one that
    /// happens to land in the same file — resolves to nothing and reads as the end of the
    /// conversation.
    ///
    /// <para><b>That is not a rare edge.</b> Hundreds of shipped branches are id-addressed, and it
    /// is how every NPC reaches the shared ask-about pages, which all live in DIAL_Z20. Squire
    /// Phillip's router (DIAL_Z30 offset 131137) is an empty entry whose one default branch is
    /// dialog 2000001 and nothing else, so his conversation ended precisely where his topic list
    /// belongs.</para>
    ///
    /// <para><b>Decided by which destination field is set, never by whether a key resolves.</b>
    /// That is the original's own test, and it keeps a dangling in-file offset — a genuine dead end
    /// — from being mistaken for a hop and sent off to load a DDX named by a null id.</para>
    ///
    /// <para>Asking the entry the walk STOPPED on re-picks the branch the walk itself picked:
    /// <see cref="ChooseBranch"/> is a pure function of the entry and the globals.</para>
    /// </remarks>
    public static int? IdAddressedTargetOf(DialogEntry entry, Func<int, int?> getGlobal,
        Func<int> roll = null) {
        if (entry == null) {
            return null;
        }
        DialogBranchBase chosen = ChooseBranch(entry, getGlobal, roll);
        return chosen != null && chosen.TargetOffset == null ? chosen.TargetId : null;
    }

    /// <summary>
    /// How many id-addressed targets one resolve may follow before giving up.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="MaxHops"/>, which bounds offset hops within a file: each of these
    /// may load a different DDX, so the guard against malformed or cyclic data wants to be much
    /// tighter.
    /// </remarks>
    public const int MaxIdAddressedHops = 4;

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

    /// <summary>
    /// A branch's condition, asked the way the engine asks it.
    /// </summary>
    /// <remarks>
    /// <b>Every one of these is a RANGE TEST ON ONE GLOBAL KEY</b> — `gstate_event_read(id)` with a
    /// min and a max — and the vocabulary of condition types is only the extractor naming the key's
    /// range. So each arm resolves back to its key and asks <paramref name="getGlobal"/>; adding a
    /// range is a matter of teaching the reader that key, not of teaching this method a new idea.
    ///
    /// <para><b>*** A NULL UPPER BOUND MEANS UNBOUNDED, NOT "EQUAL TO MIN". ***</b> The DDX
    /// sentinel is 0xFFFF and `DialogBranchFactory` decodes it to null; this read it as
    /// `value &lt;= (Max ?? Min)`, turning every open-ended test into an exact match. Measured
    /// 2026-09-10: Limm's "worth a hundred sovereigns if it's worth a pence" gates on
    /// `Var 1 &gt;= 100` — the party's gold in sovereigns — and with 123 in the purse the deal was
    /// refused as "a few coins short", because 123 is not 100.</para>
    ///
    /// <para><b>And an item gate is a real read, not an unknown.</b> `HasItemCondition` is key
    /// 50000+id, which `gstate_event_read` answers with `itemtbl_partySize_by_kind` (GSTATE.C:45).
    /// Returning false for it made every "do you have X" branch in the game unreachable — including
    /// the seal that opens Romney's bridge.</para>
    ///
    /// <para><b>The 56000-stride keys are not a range test at all</b> — they are a masked bitfield
    /// test over one byte of <c>event_bitmap_hi</c>, ANDed or ORed with a chapter mask, and the
    /// extractor already splits them into an <see cref="AllOf"/> / <see cref="AnyOf"/> of per-bit
    /// <see cref="FlagCondition"/>s plus an <see cref="InChapters"/>. DIALOG.C:1399-1420 is the
    /// original: with the selector byte set it takes the branch only when every selected bit matches
    /// AND the chapter bit is in the mask; with the selector clear, when any selected bit matches OR
    /// the chapter bit is. <b>141 shipped branches</b> are one of those two — seven times everything
    /// else left unmodelled put together.</para>
    ///
    /// <para>Still unmodelled and still false: PartyCondition (12 shipped branches),
    /// HasNoteCondition (3), SpellTimerActiveCondition (3), RandomCondition (1) and
    /// RawGlobalCondition (0). Those need the READER to answer their key ranges, which is
    /// TASK-410; these three needed nothing new, because everything inside them was already
    /// answerable.</para>
    /// </remarks>
    private static bool Holds(Condition condition, Func<int, int?> getGlobal) {
        if (condition is FlagCondition f) {
            return ((getGlobal(f.Flag) ?? 0) != 0) == f.Set;
        }
        if (condition is VarCondition v) {
            return InRange(getGlobal(30000 + v.Var) ?? 0, v.Min, v.Max);
        }
        if (condition is HasItemCondition h) {
            return InRange(getGlobal(ItemCountGlobalBase + h.Item) ?? 0, h.AtLeast, h.AtMost);
        }
        if (condition is AllOf all) {
            // Empty is TRUE, and that is the shipped arithmetic rather than a convention:
            // a zero match mask makes `((bits ^ xor) & mask) == mask` hold vacuously.
            foreach (Condition part in all.Conditions) {
                if (!Holds(part, getGlobal)) {
                    return false;
                }
            }
            return true;
        }
        if (condition is AnyOf any) {
            foreach (Condition part in any.Conditions) {
                if (Holds(part, getGlobal)) {
                    return true;
                }
            }
            return false;
        }
        if (condition is InChapters chapters) {
            return chapters.Chapters != null && chapters.Chapters.Contains(ChapterOf(getGlobal));
        }
        return false;
    }

    /// <summary>Global 30007, with everything past chapter 8 folded onto 8.</summary>
    /// <remarks>
    /// The original builds a one-bit mask, <c>chapter &lt; 9 ? 1 &lt;&lt; (chapter - 1) : 0x80</c>
    /// (DIALOG.C:1402), so chapter 9 and beyond share chapter 8's bit — and the extractor's chapter
    /// list, which only ever runs 1..8, is the other half of the same rule.
    /// </remarks>
    private static int ChapterOf(Func<int, int?> getGlobal) {
        int chapter = getGlobal(ChapterGlobalKey) ?? 0;
        return chapter >= LastChapterBit ? LastChapterBit : chapter;
    }

    /// <summary>Global 30007 — the chapter, which is Var 7.</summary>
    public const int ChapterGlobalKey = 30007;

    private const int LastChapterBit = 8;

    /// <summary>The global key range that answers "how many of object N does the party hold".</summary>
    public const int ItemCountGlobalBase = 50000;

    private static bool InRange(int value, int min, int? max) =>
        value >= min && value <= (max ?? int.MaxValue);
}
