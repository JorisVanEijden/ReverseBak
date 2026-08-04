namespace GameData.Resources.Layout;

/// <summary>
/// Inner spacing between an element's own border and its content/children — distinct from
/// <see cref="LayoutHint"/>'s Left/Top/Right/Bottom, which are INSETS (distance from the
/// PARENT's edge to this element). Padding hugs this element's own content instead: the speaker
/// pill (90 x 18 canonical px) sizes itself to its label plus this padding, it does not sit at a
/// fixed offset from anything else.
///
/// <para>Genuinely resolvable as a percentage — UI Toolkit's <c>paddingLeft</c>/etc. accept
/// <c>Length.Percent</c> natively — which is why this is <see cref="LayoutLength"/> rather than
/// a plain <c>float</c> the way <c>InventoryLayout</c>'s shadow offsets and border widths are:
/// those cannot resolve a percentage (there is no parent-relative axis to take a percentage of),
/// so typing them as <see cref="LayoutLength"/> would let an override write "1%" and have the
/// unit silently discarded. Padding has no such problem.</para>
///
/// <para>All four sides default to <see cref="LayoutLength.Auto"/> — "no opinion" — so an
/// element with no explicit padding renders exactly as it did before this type existed, and an
/// override that sets only one side leaves the other three exactly as <c>LayoutApplier</c> (or
/// USS) already has them.</para>
/// </summary>
public class LayoutPadding {
    public LayoutLength Left { get; set; } = LayoutLength.Auto;

    public LayoutLength Top { get; set; } = LayoutLength.Auto;

    public LayoutLength Right { get; set; } = LayoutLength.Auto;

    public LayoutLength Bottom { get; set; } = LayoutLength.Auto;
}
