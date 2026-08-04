namespace GameData.Resources.Layout;

/// <summary>
/// Marks a container whose children are placed on a fixed-size cell grid — the one thing UI
/// Toolkit's flexbox model cannot express (a child spanning multiple columns/rows, e.g. a
/// two-handed weapon in an inventory grid). This describes the CONTAINER's cell geometry only:
/// how big one cell is, and how many columns/rows the grid has. Which cells an individual child
/// occupies is <see cref="LayoutGridPlacement"/>, carried on the child, not here.
///
/// <para>Like <see cref="LayoutFlow"/>, this is purely about the element's OWN CHILDREN — it says
/// nothing about how the element itself is placed in ITS parent (<see cref="LayoutHint.Position"/>)
/// or whether it is also a flowing container (<see cref="LayoutHint.Flow"/>). All three are
/// independent switches on the same <see cref="LayoutHint"/>, exactly as Position and Flow are
/// independent of each other — a container is never forced to pick just one.</para>
///
/// <para>Deciding WHICH cells a given child should occupy (e.g. "this two-handed sword needs a
/// 2x2 footprint") is a game rule, not layout — that packing decision belongs to the view/
/// inventory logic that builds the <see cref="LayoutGridPlacement"/> instances. It does not
/// belong here, and it does not belong in <c>LayoutApplier</c> either.</para>
/// </summary>
public class LayoutGrid {
    /// <summary>Width of one cell, in design-frame px or percent of the parent. Percent cells
    /// are what let the grid genuinely reflow when the container resizes.</summary>
    public LayoutLength CellWidth { get; set; } = LayoutLength.Auto;

    /// <summary>Height of one cell. See <see cref="CellWidth"/>.</summary>
    public LayoutLength CellHeight { get; set; } = LayoutLength.Auto;

    /// <summary>Number of columns in the grid.</summary>
    public int Columns { get; set; }

    /// <summary>Number of rows in the grid.</summary>
    public int Rows { get; set; }

    /// <summary>A copy whose properties can be changed without affecting this instance.</summary>
    public LayoutGrid Clone() => new() { CellWidth = CellWidth, CellHeight = CellHeight, Columns = Columns, Rows = Rows };
}
