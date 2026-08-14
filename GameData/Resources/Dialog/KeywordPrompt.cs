namespace GameData.Resources.Dialog;

/// <summary>
/// Putting the topic grid on screen and reading the answer — IDA <c>ShowKeywordDialog</c>
/// (ovr144 @0x4b0fd). The last step of the keyword flow.
/// </summary>
public static class KeywordPrompt {
    /// <summary>Appended to the speaker's name to make the prompt. The leading space is part of it.</summary>
    public const string PromptSuffix = " asked about:";

    /// <summary>What the prompt reports when nothing was chosen.</summary>
    public const int NothingChosen = -1;

    /// <summary>
    /// The line above the grid.
    /// </summary>
    /// <remarks>
    /// Built by concatenation, so the speaker's name goes in verbatim — there is no placeholder
    /// substitution and no punctuation beyond the suffix.
    /// </remarks>
    public static string PromptFor(string speakerName) => speakerName + PromptSuffix;

    /// <summary>
    /// Whether the prompt appears at all.
    /// </summary>
    /// <remarks>
    /// <b>It builds the grid first and gives up if there is nothing to ask about.</b> The prompt is
    /// never drawn for an empty grid — so an NPC with no available topics shows no "asked about:"
    /// line at all, rather than an empty box under a heading.
    /// </remarks>
    public static bool Appears(int availableKeywords) => KeywordMenu.Opens(availableKeywords);

    /// <summary>
    /// <b>Choosing the farewell and dismissing the prompt are the same path.</b>
    /// </summary>
    /// <remarks>
    /// This is why the farewell's action id is 1: <b>1 is the dismiss code</b>. The loop converts
    /// ids at or above <see cref="DialogChoiceMenu.EntryActionIdBase"/> into an index and then
    /// exits on a raw 1 — which the farewell's id already is — leaving the index at
    /// <see cref="NothingChosen"/>. Giving the farewell an id of its own would take it off the
    /// dismiss path and require a second exit branch that the original does not have.
    /// </remarks>
    public static bool EndsTheConversation(int pollResult) =>
        pollResult == DialogChoiceMenu.DismissedResult;

    /// <summary>
    /// What the prompt answers with: the chosen branch index, or <see cref="NothingChosen"/>.
    /// </summary>
    /// <remarks>
    /// <b>The grid runs its own input loop</b> — it does not go through
    /// <see cref="DialogChoiceMenu"/>, despite that being where the other menus resolve their
    /// selections. The two loops are similar but not identical: this one has no first-letter
    /// accelerator and no last-entry substitution, because its farewell already sits on the dismiss
    /// code.
    /// </remarks>
    public static int Result(int pollResult) {
        if (pollResult >= DialogChoiceMenu.EntryActionIdBase) {
            return pollResult - DialogChoiceMenu.EntryActionIdBase;
        }

        return NothingChosen;
    }

    /// <summary>Offset of a branch record's jump target, within the record.</summary>
    public const int BranchTargetOffset = 6;

    /// <summary>Dialog id meaning the conversation is over.</summary>
    public const int ConversationOver = 0;

    /// <summary>
    /// <b>A chosen topic jumps straight to its branch's target — it does not latch a flag.</b>
    /// </summary>
    /// <remarks>
    /// This is where the keyword path and the choice path genuinely diverge, and it is the opposite
    /// of what the shared menu code suggests. A choice menu writes
    /// <see cref="DialogChoiceMenu.ValueWrittenForChoice"/> to the branch's key and lets the
    /// conditional branch loop discover it; a keyword reads the <b>target dialog id straight out of
    /// the branch record</b> and goes there. No condition is evaluated and no latch is involved.
    ///
    /// <para>Implementing keywords by latching, on the strength of the menus sharing their builders
    /// and their action ids, produces a conversation that goes nowhere: nothing is watching those
    /// keys on this path.</para>
    /// </remarks>
    public static int BranchTargetOffsetFor(int branchIndex) =>
        DialogChoiceMenu.BranchOffset(branchIndex) + BranchTargetOffset;

    /// <summary>
    /// The flag a chosen topic writes: <b>the asked-about flag</b>, not the choice latch.
    /// </summary>
    /// <remarks>
    /// It is the same <c>7500 + key</c> flag the grid reads back to grey a topic out — see
    /// <see cref="KeywordMenu.AskedFlag"/>. So asking about something is recorded for the next time
    /// the grid is built, and that is the <i>only</i> flag the keyword path writes.
    /// </remarks>
    public static int FlagWrittenFor(int branchGlobalKey) => KeywordMenu.AskedFlag(branchGlobalKey);

    /// <summary>What the conversation continues with when the player says goodbye.</summary>
    /// <remarks>Zero — the dialog ends rather than falling through to a branch.</remarks>
    public static int TargetWhenDismissed() => ConversationOver;
}
