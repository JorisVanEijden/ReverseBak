namespace GameData.Resources.Dialog;

/// <summary>
/// The "which of you?" row of buttons a dialog puts along the bottom of its panel — IDA
/// <c>ProcessKeywordSelection</c> (ovr144 @0x4b3ab).
///
/// <para><b>The name is wrong and so is one line of the spec.</b> This builds a party-member
/// picker; it has nothing to do with following a chosen keyword. <c>docs/specs/dialog-system.md</c>
/// says both things in different places — its flag table has it right (the
/// <c>PartyMemberSelection</c> bit routes here), and its Keyword System walkthrough lists it as
/// "handles the player's choice, follows the branch to the target dialog entry", which it does
/// not.</para>
/// </summary>
public static class PartyMemberPicker {
    /// <summary>
    /// Action ids start here and count up by party position.
    /// </summary>
    /// <remarks>
    /// Deliberately the <b>same base</b> the keyword grid uses — pointed at that constant rather
    /// than repeated, because the sharing is the point: an action id alone does not say which menu
    /// produced it, and a caller has to know which one is open to read it. Two independent 128s
    /// would hide that.
    /// </remarks>
    public const int FirstActionId = KeywordMenu.FirstKeywordActionId;

    /// <summary>The last button's label, which is not a party member.</summary>
    public const string CancelLabel = "Cancel";

    /// <summary>Padding added to the widest label to give every button its width.</summary>
    public const int LabelPadding = 10;

    /// <summary>Added to each button's computed x.</summary>
    public const int RowInset = 4;

    /// <summary>Added to the font height for each button's height.</summary>
    public const int HeightPadding = 4;

    /// <summary>Distance the row's top sits above the panel's bottom edge, before the font height.</summary>
    public const int BottomMargin = 11;

    /// <summary>Buttons the row has: one per active member, plus cancel.</summary>
    public static int ButtonCount(int activePartyMembers) => activePartyMembers + 1;

    /// <summary>Index of the cancel button, which is always last.</summary>
    public static int CancelIndex(int activePartyMembers) => ButtonCount(activePartyMembers) - 1;

    /// <summary>The action id a button reports.</summary>
    /// <remarks>
    /// <b>Cancel is not special-cased</b> — it keeps the action id its slot would have had, one past
    /// the last member. A caller that expects a distinct cancel id will read it as a fourth party
    /// member.
    /// </remarks>
    public static int ActionIdFor(int buttonIndex) => FirstActionId + buttonIndex;

    /// <summary>
    /// Every button's width: the widest label plus padding.
    /// </summary>
    /// <param name="widestLabelWidth">The widest measured label, cancel's included or not — see remarks.</param>
    /// <remarks>
    /// <b>The measuring pass walks one entry past the party and never measures "Cancel".</b> It asks
    /// for the actor at the cancel slot's index — one beyond the last member — and measures whatever
    /// name comes back, then the label is overwritten afterwards. So the row is sized partly by a
    /// name that is never displayed, and the word actually shown was never measured at all. Sizing
    /// the row from the labels you intend to draw is more correct and less faithful; if a button
    /// ever looks the wrong width against the original, this is why.
    /// </remarks>
    public static int ButtonWidth(int widestLabelWidth) => widestLabelWidth + LabelPadding;

    /// <summary>
    /// Where a button sits horizontally.
    /// </summary>
    /// <remarks>
    /// The row is spread across the panel by dividing its width into <c>count + 1</c> parts and
    /// centring each button on a division — so the buttons are evenly spaced with a gap at each end
    /// rather than packed or edge-aligned.
    /// </remarks>
    public static int ButtonX(int buttonIndex, int panelWidth, int buttonCount, int buttonWidth) =>
        (((buttonIndex + 1) * (panelWidth / (buttonCount + 1))) + RowInset) - (buttonWidth / 2);

    /// <summary>The row's top, measured up from the panel's bottom edge.</summary>
    public static int RowY(int panelHeight, int fontHeight) =>
        panelHeight - (fontHeight + BottomMargin);

    /// <summary>Each button's height.</summary>
    public static int ButtonHeight(int fontHeight) => fontHeight + HeightPadding;
}
