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

    // ------------------------------------------------------------------ canonical space

    /// <summary>Canonical-space scale factors — VGA x5 across, x6 down.</summary>
    /// <remarks>
    /// <b>Everything above is in the original's mode-13h pixels, because that is how the routine
    /// reads; every renderer works in canonical 1600x1200.</b> Converting here rather than in the
    /// renderer is the house rule — see <see cref="KeywordMenu.CanonicalScaleX"/>, which says why:
    /// it keeps the 320x200 space from leaking into the UI layer.
    ///
    /// <para>Its absence is what made this model unusable. A caller reaching for
    /// <see cref="ButtonX"/> directly, with the canonical panel width it actually has, would get a
    /// row laid out to fifths of the right offsets — arithmetic that is individually correct and
    /// collectively nonsense. The sibling <see cref="DialogSpeakerNamePill"/> has scaled constants
    /// and this had none, in the same folder.</para>
    /// </remarks>
    public const int CanonicalScaleX = 5;

    /// <inheritdoc cref="CanonicalScaleX"/>
    public const int CanonicalScaleY = 6;

    /// <summary>
    /// One button's box in canonical space.
    /// </summary>
    /// <param name="buttonIndex">Its place in the row.</param>
    /// <param name="panelWidth">The panel's width in ORIGINAL px.</param>
    /// <param name="panelHeight">The panel's height in ORIGINAL px.</param>
    /// <param name="buttonCount">How many buttons share the row.</param>
    /// <param name="widestLabelWidth">
    /// The widest label's width in ORIGINAL px — <see cref="Font.FontMetrics.WidestOf"/>.
    /// </param>
    /// <param name="fontHeight">The game font's height in ORIGINAL px.</param>
    /// <remarks>
    /// <b>The arithmetic is done in original px and only the RESULT is scaled</b>, which is not
    /// interchangeable with scaling the inputs first: the row's spread is an integer division by
    /// <c>count + 1</c>, so dividing a canonical width instead spreads the remainder differently
    /// and walks the buttons off the original's positions by a pixel or two each.
    /// </remarks>
    public static (int X, int Y, int Width, int Height) ButtonRect(int buttonIndex, int panelWidth,
        int panelHeight, int buttonCount, int widestLabelWidth, int fontHeight) {
        int width = ButtonWidth(widestLabelWidth);
        return (
            ButtonX(buttonIndex, panelWidth, buttonCount, width) * CanonicalScaleX,
            RowY(panelHeight, fontHeight) * CanonicalScaleY,
            width * CanonicalScaleX,
            ButtonHeight(fontHeight) * CanonicalScaleY);
    }

    /// <summary>
    /// The same box, for a caller that only has the panel's CANONICAL size — which is every
    /// renderer, because the extractor rescales the dialog's rect on the way out.
    /// </summary>
    /// <remarks>
    /// <b>The division back to original px is exact, and it has to happen before the layout
    /// arithmetic rather than after.</b> <c>AspectCorrection.ScaleVgaX</c> is a plain <c>x * 5</c>
    /// on the whole VGA pixels the binary stores, so canonical / 5 recovers the original value with
    /// nothing lost. Doing it here rather than in the renderer keeps the one place that knows about
    /// 320x200 inside the model — and it is the same reason
    /// <see cref="ButtonRect"/> scales its result instead of its inputs: the row's spread is an
    /// integer division, so it must be computed in the space the original computed it in.
    ///
    /// <para>The label width and font height are NOT converted, because they never left original
    /// space: <see cref="Font.FontMetrics"/> measures the game's own bitmap font and the font
    /// height is its character cell.</para>
    /// </remarks>
    public static (int X, int Y, int Width, int Height) ButtonRectOnCanonicalPanel(int buttonIndex,
        int canonicalPanelWidth, int canonicalPanelHeight, int buttonCount, int widestLabelWidth,
        int fontHeight) =>
        ButtonRect(buttonIndex, canonicalPanelWidth / CanonicalScaleX,
            canonicalPanelHeight / CanonicalScaleY, buttonCount, widestLabelWidth, fontHeight);
}
