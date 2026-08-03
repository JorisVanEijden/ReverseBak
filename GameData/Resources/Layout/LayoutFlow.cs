namespace GameData.Resources.Layout;

/// <summary>Main-axis direction for a flowing container.</summary>
public enum LayoutFlowDirection { Row, Column }

/// <summary>Main-axis distribution of children in a flowing container.</summary>
public enum LayoutFlowJustify { Start, Center, End, SpaceBetween, SpaceAround }

/// <summary>Cross-axis alignment of children in a flowing container.</summary>
public enum LayoutFlowAlign { Start, Center, End, Stretch }

/// <summary>
/// Marks a container whose children flow (and may wrap) rather than being absolutely placed.
/// A null <see cref="LayoutHint.Flow"/> means absolute placement — the faithful default, and
/// what every extractor emits. Setting one is how an override turns, say, a fixed 7-column
/// item grid into a grid that rewraps to the available width.
/// </summary>
public class LayoutFlow {
    public LayoutFlowDirection Direction { get; set; } = LayoutFlowDirection.Row;

    /// <summary>Whether children wrap onto new lines when they overflow the main axis.</summary>
    public bool Wrap { get; set; } = true;

    public LayoutFlowJustify Justify { get; set; } = LayoutFlowJustify.Start;

    public LayoutFlowAlign Align { get; set; } = LayoutFlowAlign.Start;

    /// <summary>Spacing between children.</summary>
    public LayoutLength Gap { get; set; } = LayoutLength.Px(0f);
}
