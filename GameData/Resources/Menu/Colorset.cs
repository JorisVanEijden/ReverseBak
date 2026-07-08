namespace GameData.Resources.Menu;

/// <summary>
/// Semantic selector for a REQ screen's colour palette range, replacing the raw
/// <c>colorBase</c> byte read from disk (base index into the renderer's 7-colour palette
/// range, base..base+6). The Unity theme maps each selector to its own colour ramp; the
/// numeric values below are the original on-disk bases observed across shipped REQ files,
/// kept only so the extractor can cast the raw byte straight into this enum.
/// </summary>
public enum Colorset {
    Plain = 0,
    EditorA = 4,
    EditorB = 7,
    Overlay = 32,
    Picker = 144,

    /// <summary>Default fullscreen-menu set; also the fallback for any unrecognized on-disk value.</summary>
    Menu = 169
}
