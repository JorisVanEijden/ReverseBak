namespace GameData.Resources.Spells;

/// <summary>
/// The figure drawn inside the casting ring — one closed six-vertex outline per school.
/// </summary>
/// <remarks>
/// <b>It is line work, not a picture.</b> <c>hexanim_move_tiles</c> (HEXANIM.C) takes the six
/// vertices of the school being left and the six of the school being entered and draws them with
/// <c>draw_line</c>, so the figure is stroked in a single pen with nothing behind it. That is why
/// it reads as red line art over the ring's own texture rather than as a sprite on a disc.
///
/// <para><b>The shapes look like a spell's book icon, and are not it.</b> The school's spellbook
/// entry (<c>BICONS1.BMX#&lt;group.Icon&gt;</c>) shows the same figure drawn as a picture on a dark
/// disc — close enough that comparing the two crops matches, which is what first identified the
/// figure. Drawing the icon here instead gets the geometry right and everything else wrong: the
/// wrong colour, the disc, and no morph.</para>
///
/// <para><b>Switching school animates.</b> The original walks the vertices from one set to the
/// other over <see cref="MorphSteps"/> frames and trails <see cref="TrailLength"/> older copies
/// behind the leading edge, each one pen higher, so the outline appears to flow into its new
/// shape. The resting figure is a single outline in <see cref="RestingPen"/>.</para>
///
/// <para>Coordinates are absolute VGA screen positions, which is how the original stores them —
/// they land inside the ring's own rect (VGA 18,15 size 111x93, the region
/// <c>cspell_cast_menu_open_transition</c> saves and restores around every redraw). Scale by
/// <c>x*5, y*6</c> for canonical space.</para>
/// </remarks>
public static class CastRingSigil {

    /// <summary>Vertices per figure.</summary>
    public const int VertexCount = 6;

    /// <summary>
    /// Figures, indexed by school, in VGA screen coordinates: <c>[figure][vertex]</c>.
    /// </summary>
    /// <remarks>
    /// Seven sets for six schools — <c>g_apHexVertexX/Y</c> carry a seventh the cast screen never
    /// selects, kept at its own index so a school still indexes this directly.
    /// </remarks>
    public static readonly int[][] VertexX = {
        new[] { 71, 95, 123, 19, 49, 59 },
        new[] { 19, 47, 108, 34, 92, 123 },
        new[] { 124, 71, 40, 104, 71, 19 },
        new[] { 104, 19, 105, 39, 123, 41 },
        new[] { 37, 37, 107, 107, 37, 107 },
        new[] { 75, 114, 75, 75, 31, 75 },
        new[] { 70, 115, 117, 72, 26, 25 },
    };

    /// <inheritdoc cref="VertexX"/>
    public static readonly int[][] VertexY = {
        new[] { 16, 101, 61, 61, 101, 61 },
        new[] { 70, 22, 93, 93, 20, 70 },
        new[] { 62, 16, 97, 97, 16, 62 },
        new[] { 98, 62, 26, 26, 61, 98 },
        new[] { 29, 96, 29, 96, 96, 96 },
        new[] { 72, 89, 17, 72, 89, 17 },
        new[] { 16, 37, 83, 107, 86, 40 },
    };

    /// <summary>
    /// The edges, as vertex index pairs.
    /// </summary>
    /// <remarks>
    /// <c>hexanim_draw_hexagon_outline</c> strokes 0-1, 1-2, 2-3, 3-4, 4-5 and then closes 0-5 —
    /// note the close joins the FIRST vertex to the LAST, which is an ordinary closed path only
    /// because the loop stops at 4. Sets repeat vertices to fold the path back on itself, which is
    /// how six edges make figures like a square with both diagonals (set 4) rather than a hexagon.
    /// </remarks>
    public static readonly int[][] Edges = {
        new[] { 0, 1 }, new[] { 1, 2 }, new[] { 2, 3 },
        new[] { 3, 4 }, new[] { 4, 5 }, new[] { 0, 5 },
    };

    /// <summary>Pen the resting figure is stroked in.</summary>
    public const int RestingPen = 0x89;

    /// <summary>Pen the oldest copy in the morph trail is stroked in; each newer copy is one higher.</summary>
    public const int TrailFirstPen = 0x83;

    /// <summary>Copies drawn behind the leading edge while the figure morphs.</summary>
    public const int TrailLength = 7;

    /// <summary>Frames the morph runs for; the vertices stop moving at 30 and it settles over the rest.</summary>
    public const int MorphSteps = 0x25;

    /// <summary>Whether <paramref name="school"/> has a figure.</summary>
    public static bool Has(int school) => school >= 0 && school < VertexX.Length;
}
