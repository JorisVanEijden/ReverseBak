namespace GameData.Resources.Layout;

/// <summary>How an element is placed in its parent — independent of how it lays out its
/// own children (that is <see cref="LayoutHint.Flow"/>).</summary>
public enum LayoutPosition {
    /// <summary>Positioned directly against the parent's edges via anchor and insets.
    /// The default: every original screen places its elements absolutely.</summary>
    Absolute,

    /// <summary>Participates in the parent's flow, sized and placed by the parent's layout.</summary>
    InFlow
}
