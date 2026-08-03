namespace GameData.Resources.Layout;

/// <summary>
/// Responsive-layout description for a UI element. Defaults are "classic": absolute top-left
/// placement, no slicing, size determined by the element's own geometry — i.e. the original look.
/// Extractors emit design-frame pixel lengths, so extracted layout reproduces the original
/// exactly; overrides use percentages and <see cref="Flow"/> to reflow.
/// </summary>
public class LayoutHint {
    /// <summary>How this element itself is placed in its parent. Defaults to
    /// <see cref="LayoutPosition.Absolute"/> — every extracted hint keeps behaving exactly as
    /// it does today. Independent of <see cref="Flow"/>: an absolutely-placed element can
    /// still be a flow container for its own children (e.g. the credits row — pinned at
    /// fixed insets in the scroller, but a flex row for its role/leader/name children).</summary>
    public LayoutPosition Position { get; set; } = LayoutPosition.Absolute;

    /// <summary>Which point of the container the element binds to once the stage fills
    /// (rather than pillarboxes) its frame.</summary>
    public LayoutAnchor Anchor { get; set; } = LayoutAnchor.TopLeft;

    /// <summary>9-slice margins for a stretchable background graphic. All-zero = no slicing,
    /// which means the graphic anchors at its native proportion instead of stretching.</summary>
    public NineSlice Slice { get; set; } = default;

    /// <summary>Width, in design-frame px or percent of the parent. Auto = intrinsic.</summary>
    public LayoutLength Width { get; set; } = LayoutLength.Auto;

    /// <summary>Height, in design-frame px or percent of the parent. Auto = intrinsic.</summary>
    public LayoutLength Height { get; set; } = LayoutLength.Auto;

    /// <summary>Distance from the parent's left edge. Auto = not pinned by an inset; the
    /// element's <see cref="Anchor"/> decides instead. An explicit inset always wins over
    /// the anchor's implied pinning on the same edge.</summary>
    public LayoutLength Left { get; set; } = LayoutLength.Auto;

    /// <summary>Distance from the parent's top edge. See <see cref="Left"/>.</summary>
    public LayoutLength Top { get; set; } = LayoutLength.Auto;

    /// <summary>Distance from the parent's right edge. See <see cref="Left"/>.</summary>
    public LayoutLength Right { get; set; } = LayoutLength.Auto;

    /// <summary>Distance from the parent's bottom edge. See <see cref="Left"/>.</summary>
    public LayoutLength Bottom { get; set; } = LayoutLength.Auto;

    /// <summary>Optional proportion to preserve. Meaningful when a size is given in percent —
    /// percent-of-width and percent-of-height are different units, so painted art needs this
    /// to avoid stretching. Applied via <c>LayoutApplier.ApplyAspectRatio</c> (native UI Toolkit
    /// <c>style.aspectRatio</c>); ignored (with a warning) when both Width and Height are
    /// explicit, since that over-constrains the element.</summary>
    public LayoutAspectRatio? AspectRatio { get; set; }

    /// <summary>Non-null makes this element a flowing container for its own children (their
    /// direction, wrap, justify and align). This is purely about the CHILDREN's layout — it is
    /// independent of <see cref="Position"/>, which decides how THIS element is placed in its
    /// own parent. Null = children are not flowed (each positions itself independently).</summary>
    public LayoutFlow? Flow { get; set; }
}
