namespace GameData.Resources.Layout;

/// <summary>Main-axis direction for a flowing container.</summary>
public enum LayoutFlowDirection { Row, Column }

/// <summary>Main-axis distribution of children in a flowing container.</summary>
public enum LayoutFlowJustify { Start, Center, End, SpaceBetween, SpaceAround }

/// <summary>Cross-axis alignment of children in a flowing container.</summary>
public enum LayoutFlowAlign { Start, Center, End, Stretch }

/// <summary>
/// Marks a container whose children flow (and may wrap) rather than each positioning itself
/// independently. This is about the element's OWN CHILDREN only — it says nothing about how the
/// element itself is placed in ITS parent (see <see cref="LayoutHint.Position"/> for that). A
/// null <see cref="LayoutHint.Flow"/> is the faithful default, and what every extractor emits.
/// Setting one is how an override turns, say, a fixed 7-column item grid into a grid that
/// rewraps to the available width — while the grid itself can still be absolutely pinned in its
/// parent via <see cref="LayoutHint.Position"/> = <see cref="LayoutPosition.Absolute"/>.
/// </summary>
public class LayoutFlow {
    public LayoutFlowDirection Direction { get; set; } = LayoutFlowDirection.Row;

    /// <summary>Whether children wrap onto new lines when they overflow the main axis.</summary>
    public bool Wrap { get; set; } = true;

    public LayoutFlowJustify Justify { get; set; } = LayoutFlowJustify.Start;

    public LayoutFlowAlign Align { get; set; } = LayoutFlowAlign.Start;

    /// <summary>Spacing between children.</summary>
    public LayoutLength Gap { get; set; } = LayoutLength.Px(0f);

    /// <summary>A copy whose properties can be changed without affecting this instance.</summary>
    public LayoutFlow Clone() => new() { Direction = Direction, Wrap = Wrap, Justify = Justify, Align = Align, Gap = Gap };
}
