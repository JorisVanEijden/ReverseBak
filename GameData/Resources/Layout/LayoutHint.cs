namespace GameData.Resources.Layout;

/// <summary>
/// Responsive-layout description for a UI element. Defaults are "classic": absolute top-left
/// placement, no slicing, size determined by the element's own geometry — i.e. the original look.
/// Extractors emit design-frame pixel lengths, so extracted layout reproduces the original
/// exactly; overrides use percentages and <see cref="Flow"/> to reflow.
/// </summary>
public class LayoutHint {
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

    /// <summary>Optional proportion to preserve. Meaningful when a size is given in percent —
    /// percent-of-width and percent-of-height are different units, so painted art needs this
    /// to avoid stretching. Ignored (with a warning) when both Width and Height are explicit,
    /// since that over-constrains the element.</summary>
    public LayoutAspectRatio? AspectRatio { get; set; }

    /// <summary>Non-null makes this element a flowing container. Null = absolute placement.</summary>
    public LayoutFlow? Flow { get; set; }
}
