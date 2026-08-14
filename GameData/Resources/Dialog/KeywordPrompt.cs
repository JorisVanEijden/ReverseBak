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
}
