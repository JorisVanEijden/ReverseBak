namespace GameData.Resources.Layout;

/// <summary>How a screen's design frame is mapped onto the window when their aspects differ.</summary>
public enum LayoutFit {
    /// <summary>Fit the whole frame inside the window, preserving its aspect — letter/pillarboxed.
    /// The faithful default: the original ran at a fixed 4:3.</summary>
    Contain,

    /// <summary>Let the frame span the window. Percentage lengths and anchors then resolve against
    /// the real size, so layout reflows instead of being boxed.</summary>
    Fill
}

/// <summary>
/// The coordinate space a screen's layout is expressed in. Every position and size on the screen
/// resolves against this frame, so a resource is only interpretable alongside it — e.g. a panel at
/// (65,66,1470,606) means nothing without knowing it sits in a 1600x1200 space.
///
/// <para>Extractors emit the canonical space derived from the original display mode (see
/// AspectCorrection), with <see cref="Fit"/> = Contain, which reproduces the original's fixed
/// presentation exactly. An override changes Fit — or the frame itself — to reflow.</para>
/// </summary>
public class DesignFrame {
    public int Width { get; set; }

    public int Height { get; set; }

    public LayoutFit Fit { get; set; } = LayoutFit.Contain;
}
