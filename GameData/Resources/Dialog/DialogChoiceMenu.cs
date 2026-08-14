namespace GameData.Resources.Dialog;

/// <summary>
/// What happens when the player picks something out of a dialog menu — IDA
/// <c>ShowDialogChoiceMenu</c> (ovr144 @0x4b54c).
///
/// <para><b>This is what follows a chosen keyword</b>, and it is not a jump: the menu writes a flag
/// and lets the ordinary branch dispatch find its way there. The function named
/// <c>ProcessKeywordSelection</c> does not do it — see <see cref="PartyMemberPicker"/>.</para>
/// </summary>
public static class DialogChoiceMenu {
    /// <summary>Poll result meaning the player dismissed the menu rather than clicking an entry.</summary>
    public const int DismissedResult = 1;

    /// <summary>Action ids at or above this identify an entry; below it, the value is a keystroke.</summary>
    public const int EntryActionIdBase = 0x80;

    /// <summary>The value a chosen branch's key is latched to.</summary>
    public const int ChosenValue = 1;

    /// <summary>Bytes per branch record, and the offset the records begin at.</summary>
    public const int BranchRecordSize = 10;

    /// <summary>Offset of the first branch record within a dialog entry.</summary>
    public const int FirstBranchOffset = 9;

    /// <summary>What the picker returns when the player cancels it.</summary>
    public const int PartyPickerCancelled = 1;

    /// <summary>What the picker returns when a member was chosen.</summary>
    public const int PartyPickerChose = 0;

    /// <summary>
    /// Dismissing the menu is resolved to the <b>last entry</b> rather than to nothing.
    /// </summary>
    /// <remarks>
    /// <b>Escape does not close the menu; it presses the last button.</b> For a keyword grid that is
    /// the farewell, and for the party row it is cancel — which is why neither needs its own dismiss
    /// path. A port that treats dismissal as "no selection" would leave a conversation with no way
    /// out of the grid.
    /// </remarks>
    public static bool DismissalPressesLastEntry(int pollResult) => pollResult == DismissedResult;

    /// <summary>The entry index an action id names, or -1 when the value is not an entry.</summary>
    public static int EntryIndexOf(int actionId) =>
        actionId >= EntryActionIdBase ? actionId - EntryActionIdBase : -1;

    /// <summary>
    /// Whether a first-letter keystroke resolves.
    /// </summary>
    /// <remarks>
    /// <b>An ambiguous letter selects nothing at all.</b> The scan counts every entry whose label
    /// starts with the pressed letter and, if more than one does, deliberately throws the match away
    /// rather than taking the first. So a menu with two topics beginning "S" cannot be driven by the
    /// keyboard for either of them — pressing S simply does nothing, and the player has to click.
    /// Taking the first match is the obvious "improvement" and it changes what the keyboard does.
    /// </remarks>
    public static bool AcceleratorResolves(int matchCount) => matchCount == 1;

    /// <summary>Where a branch record sits inside the dialog entry.</summary>
    public static int BranchOffset(int branchIndex) =>
        (branchIndex * BranchRecordSize) + FirstBranchOffset;

    /// <summary>
    /// <b>Choosing a branch latches its global key to 1 — it does not follow the branch.</b>
    /// </summary>
    /// <remarks>
    /// The menu's whole effect on a keyword or choice selection is one flag write. The dialog's main
    /// branch loop then finds the branch whose condition matches that key and goes there, so the
    /// navigation is the ordinary conditional dispatch rather than anything the menu does.
    ///
    /// <para>The keys are transient latches, reset when the menu's entries are rebuilt — they are not
    /// durable story flags, even though they live in the same global space. Treating them as
    /// persistent would leave a conversation permanently answered.</para>
    /// </remarks>
    public static int ValueWrittenForChoice() => ChosenValue;

    /// <summary>
    /// What the party picker returns.
    /// </summary>
    /// <remarks>
    /// <b>The two menu kinds return different things from the same function.</b> A branch menu
    /// answers with the chosen index; the party picker answers 1 for cancelled and 0 for chosen —
    /// so a caller cannot read the result without knowing which kind it opened, and 1 means
    /// "cancelled" in one mode and "entry 1" in the other.
    /// </remarks>
    public static int PartyPickerResult(bool cancelled) =>
        cancelled ? PartyPickerCancelled : PartyPickerChose;

    /// <summary>
    /// The value a chosen party member is recorded as: the roster entry plus one.
    /// </summary>
    /// <remarks>
    /// Written into text variable slot 0, which is what makes the member's name appear in the
    /// dialog's <c>@</c> placeholder. The plus one is a 1-based convention, not an off-by-one.
    /// </remarks>
    public static int TextVariableForMember(int activeRosterValue) => activeRosterValue + 1;
}
