namespace GameData.Resources.Dialog;

/// <summary>
/// The row of buttons a dialog lays along the bottom of its panel.
///
/// <para>Shared, not coincidental: <c>CreateMenuEntriesFromDialogData</c> (@0x4b1e7) and
/// <c>ProcessKeywordSelection</c> (@0x4b3ab) compute their geometry with the same instructions —
/// one shared width, the same horizontal division, the same anchoring to the panel's bottom edge.
/// The two differ in <i>what</i> they put in the row, never in where it goes.</para>
/// </summary>
public static class DialogButtonRow {
    /// <summary>Padding added to the widest label to give every button its width.</summary>
    public const int LabelPadding = 10;

    /// <summary>Added to each button's computed x.</summary>
    public const int RowInset = 4;

    /// <summary>Added to the font height for each button's height.</summary>
    public const int HeightPadding = 4;

    /// <summary>Distance the row's top sits above the panel's bottom edge, before the font height.</summary>
    public const int BottomMargin = 11;

    /// <summary>
    /// Every button's width: the widest label plus padding. One width for the whole row, so the
    /// buttons are uniform rather than fitted.
    /// </summary>
    public static int ButtonWidth(int widestLabelWidth) => widestLabelWidth + LabelPadding;

    /// <summary>
    /// Where a button sits horizontally.
    /// </summary>
    /// <remarks>
    /// The row is spread by dividing the panel's width into <c>count + 1</c> parts and centring each
    /// button on a division — evenly spaced with a gap at each end, rather than packed or
    /// edge-aligned.
    /// </remarks>
    public static int ButtonX(int buttonIndex, int panelWidth, int buttonCount, int buttonWidth) =>
        (((buttonIndex + 1) * (panelWidth / (buttonCount + 1))) + RowInset) - (buttonWidth / 2);

    /// <summary>The row's top, measured up from the panel's bottom edge.</summary>
    public static int RowY(int panelHeight, int fontHeight) =>
        panelHeight - (fontHeight + BottomMargin);

    /// <summary>Each button's height.</summary>
    public static int ButtonHeight(int fontHeight) => fontHeight + HeightPadding;
}
