namespace GameData.Resources.Layout;

/// <summary>
/// Optional responsive-layout hints for a UI element. Defaults are "classic":
/// absolute top-left placement, no slicing, fixed size — i.e. the original look.
/// Enhanced mode / mods override these; Phase-1 extraction only emits the defaults.
/// </summary>
public class LayoutHint {
    public LayoutAnchor Anchor { get; set; } = LayoutAnchor.TopLeft;
    public NineSlice Slice { get; set; } = default;
    public bool RelativeWidth { get; set; } = false;
    public bool RelativeHeight { get; set; } = false;
}
