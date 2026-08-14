namespace GameData.Resources.Dialog;

/// <summary>
/// Building the buttons for an ordinary choice menu — IDA
/// <c>CreateMenuEntriesFromDialogData</c> (ovr144 @0x4b1e7). The non-keyword half of
/// <see cref="DialogChoiceMenu"/>; its geometry is <see cref="DialogButtonRow"/>'s.
/// </summary>
public static class DialogChoiceEntries {
    /// <summary>The value every candidate latch is cleared to before the menu is shown.</summary>
    public const int ClearedValue = 0;

    /// <summary>
    /// Buttons the menu has: exactly one per branch.
    /// </summary>
    /// <remarks>
    /// <b>No spare slot, so no cancel and no farewell.</b> A keyword grid reserves its last slot for
    /// "GoodBye" and the party row for "Cancel"; a choice menu has only its branches. Combined with
    /// the dismiss rule in <see cref="DialogChoiceMenu.DismissalPressesLastEntry"/>, that means
    /// <b>escaping a choice menu picks its last branch</b> rather than backing out — so the last
    /// branch is the de-facto default and its order in the data matters.
    /// </remarks>
    public static int ButtonCount(int branchCount) => branchCount;

    /// <summary>
    /// <b>Every candidate latch is cleared before the menu appears.</b>
    /// </summary>
    /// <remarks>
    /// The builder walks the branches and writes zero to each one's global key on the way past — so
    /// for a Yes/No menu, both keys are cleared, not just the one that will be chosen. Without it a
    /// latch left set by an earlier menu would auto-match the moment this one opened, and the player
    /// would never see the question.
    ///
    /// <para><b>Scoped to this menu's branches only.</b> There is no global clear of choice keys, so
    /// a latch for a key this menu does not offer survives untouched — which is what lets the same
    /// key mean something durable elsewhere.</para>
    /// </remarks>
    public static bool ClearsEveryCandidateFirst => true;

    /// <summary>The action id a branch's button reports.</summary>
    /// <remarks>Same encoding as every other dialog menu — see <see cref="KeywordMenu.ActionIdFor"/>.</remarks>
    public static int ActionIdFor(int branchIndex) => KeywordMenu.ActionIdFor(branchIndex);

    /// <summary>Index into the keyword table for a branch's label.</summary>
    /// <remarks>
    /// The labels come from the <b>same 1-based keyword table</b> the topic grid uses — which is why
    /// a Yes/No menu's buttons are KEYWORD.DAT strings rather than anything stored with the branch.
    /// </remarks>
    public static int LabelIndexFor(int globalKey) => KeywordMenu.LabelIndexFor(globalKey);
}
