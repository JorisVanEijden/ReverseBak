namespace GameData.Resources.Layout;

/// <summary>
/// Which cell(s) of its parent's <see cref="LayoutGrid"/> a child occupies. Carried on the
/// child, not the container — the container only describes cell geometry
/// (<see cref="LayoutGrid"/>); this says where in that grid one particular child sits.
///
/// <para>Column/Row are 0-based cell indices. ColumnSpan/RowSpan default to 1 (a single cell) —
/// wider footprints (e.g. a two-handed weapon occupying 2 columns x 2 rows) are a game-rule
/// decision made by whatever packs the inventory grid, not by this type and not by
/// <c>LayoutApplier</c>. This type only records an already-decided placement.</para>
/// </summary>
public class LayoutGridPlacement {
    /// <summary>0-based column index of the child's top-left cell.</summary>
    public int Column { get; set; }

    /// <summary>0-based row index of the child's top-left cell.</summary>
    public int Row { get; set; }

    /// <summary>How many columns wide the child is. Defaults to 1 (a single cell).</summary>
    public int ColumnSpan { get; set; } = 1;

    /// <summary>How many rows tall the child is. Defaults to 1 (a single cell).</summary>
    public int RowSpan { get; set; } = 1;
}
