namespace GameData.Resources.Label;

/// <summary>
/// Semantic role of an LBL_*.DAT label, replacing the raw <c>colorIndex</c> /
/// <c>shadowColorIndex</c> pen pair read from disk. The Unity theme maps each role to its own
/// colour; the original data only ever distinguished a title pen (colourIndex 10) from every
/// other (caption) pen.
/// </summary>
public enum LabelRole {
    Caption,
    Title
}
