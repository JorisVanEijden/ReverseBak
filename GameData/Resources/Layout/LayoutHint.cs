namespace GameData.Resources.Layout;

/// <summary>
/// Responsive-layout description for a UI element. Defaults are "classic": absolute top-left
/// placement, no slicing, size determined by the element's own geometry — i.e. the original look.
/// Extractors emit design-frame pixel lengths, so extracted layout reproduces the original
/// exactly; overrides use percentages and <see cref="Flow"/> to reflow.
/// </summary>
public class LayoutHint {
    /// <summary>
    /// A plain design-frame pixel rectangle: the four insets/sizes set, every other field left at
    /// its faithful default (absolute, top-left anchored, no far-edge opinion, no flow/grid/
    /// padding). This is the exact shape an extracted rect has, so the places that need to state
    /// one — the dialog style table's rows and the per-entry <c>ResizeDialogAction</c> that
    /// replaces them, which must produce interchangeable areas — say it once here instead of each
    /// spelling out the same four <see cref="LayoutLength.Px(float)"/> calls and silently
    /// drifting apart.
    /// </summary>
    public static LayoutHint PxRect(float left, float top, float width, float height) => new() {
        Left = LayoutLength.Px(left),
        Top = LayoutLength.Px(top),
        Width = LayoutLength.Px(width),
        Height = LayoutLength.Px(height),
    };

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

    /// <summary>Non-null makes this element a fixed-cell-grid container for its own children —
    /// UI Toolkit's flexbox has no "this child spans 2 columns x 2 rows" concept, so this fills
    /// that gap (see <see cref="LayoutGrid"/>). Purely about the CHILDREN's layout, exactly like
    /// <see cref="Flow"/> — independent of <see cref="Position"/> (how THIS element sits in its
    /// own parent) AND independent of <see cref="Flow"/> itself: nothing here forces an either/or
    /// choice between a grid and a flow container. Null = children are not grid-placed, which is
    /// the default and what every extractor emits.</summary>
    public LayoutGrid? Grid { get; set; }

    /// <summary>Inner spacing between this element's own border and its content/children — e.g.
    /// the speaker pill, which hugs its label plus this padding rather than sitting at a fixed
    /// inset. Independent of <see cref="Flow"/>/<see cref="Grid"/>/<see cref="Position"/> exactly
    /// as they are independent of each other. Null = no padding opinion, which is the default and
    /// what every extractor emits (an element renders exactly as it did before this field
    /// existed).</summary>
    public LayoutPadding? Padding { get; set; }

    /// <summary>
    /// A deep copy: every field is value-copied, and the three reference-typed fields
    /// (<see cref="Flow"/>, <see cref="Grid"/>, <see cref="Padding"/>) are cloned rather than
    /// shared, so mutating the result can never reach this instance. Needed at any boundary
    /// where a shared, long-lived hint (e.g. a <c>DialogStyleTable</c> row) is handed to a
    /// caller that treats the result as its own live state — a table row must stay the same
    /// for every dialog that resolves to it, no matter what the caller later does with what it
    /// got back.
    /// </summary>
    public LayoutHint Clone() => new() {
        Position = Position,
        Anchor = Anchor,
        Slice = Slice,
        Width = Width,
        Height = Height,
        Left = Left,
        Top = Top,
        Right = Right,
        Bottom = Bottom,
        AspectRatio = AspectRatio,
        Flow = Flow?.Clone(),
        Grid = Grid?.Clone(),
        Padding = Padding?.Clone(),
    };
}
