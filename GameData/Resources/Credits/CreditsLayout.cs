namespace GameData.Resources.Credits;

using GameData.Resources.Layout;

/// <summary>
/// Geometry of the scrolling credits screen, in design-frame (1600x1200) px.
///
/// <para>These values are transcribed from <c>scrollCredits</c> (KRONDOR.EXE 0x405f1) and
/// <c>ShowCredits</c> (0x40934), where they exist only as immediate operands rather than in a
/// table — so unlike the (role, name) strings they cannot be read out of CRED.DAT. They are
/// converted from VGA once here (x5 horizontal / x6 vertical, see AspectCorrection) exactly as
/// the flag predicates on <see cref="CreditLine"/> are resolved at extraction, so the renderer
/// needs no knowledge of the original display.</para>
///
/// <para>Expressed as <see cref="LayoutHint"/> boxes — title band, clipped window, row template —
/// rather than 13 loose named coordinates, so <c>LayoutApplier.Apply</c> can consume them
/// directly and an override can reflow the screen with percentages. Only paint parameters that
/// are genuinely not box geometry (font size, fade bands, leader-dot metrics) remain plain
/// lengths.</para>
///
/// <para>The defaults are the faithful values: an override that omits a property still gets the
/// original geometry.</para>
/// </summary>
public class CreditsLayout {
    /// <summary>Title above the scroll region. Replaces the old <c>TitleY</c> coordinate (VGA
    /// y=41 -> 246 canonical), which was the title element's <b>top edge</b> (<c>title.style.top
    /// = TitleY</c> in the pre-conversion view) — not a band height, so this is an inset, not
    /// <see cref="LayoutHint.Height"/>. The title sizes to its own content, as it did before.
    /// <see cref="LayoutAnchor.TopCenter"/> supplies the horizontal centring that the
    /// (now-deleted) <c>CenterX</c> constant did before.</summary>
    public LayoutHint Title { get; set; } = new LayoutHint {
        Anchor = LayoutAnchor.TopCenter,
        Top = LayoutLength.Px(246f)
    };

    /// <summary>The scrolling window; lines outside it are clipped. Top inset is VGA y=54 -> 324,
    /// unchanged. Height is derived, not transcribed: bottom was VGA y=158 -> 948, so
    /// height = 948 - 324 = <b>624</b>.</summary>
    public LayoutHint Window { get; set; } = new LayoutHint {
        Top = LayoutLength.Px(324f),
        Height = LayoutLength.Px(624f)
    };

    /// <summary>Template for one credit row (role, leader, name). Left inset is VGA x=42 -> 210,
    /// unchanged. Right inset is derived, not transcribed: the name column's right edge was VGA
    /// x=277 -> 1385, and the design frame is 1600px wide, so
    /// right inset = 1600 - 1385 = <b>215</b>. Height is the line advance, VGA 11 -> 66,
    /// unchanged. <see cref="LayoutHint.Flow"/> flows role and name to opposite edges with the
    /// leader filling the gap between (<c>flexGrow: 1</c>), reproducing the original's absolute
    /// placement exactly at these faithful insets. <see cref="LayoutFlowAlign.Start"/> matches the
    /// original, which top-aligns both labels (neither sets a <c>top</c> offset, so both sit at
    /// 0 within the row) — not centred. The leader's own vertical placement (<c>bottom = 25%</c>
    /// of the row height, i.e. the lower quarter) stays a paint concern drawn directly by the
    /// renderer rather than a flex alignment, since no <see cref="LayoutFlowAlign"/> value can
    /// express "25% from the bottom".</summary>
    public LayoutHint Row { get; set; } = new LayoutHint {
        Left = LayoutLength.Px(210f),
        Right = LayoutLength.Px(215f),
        Height = LayoutLength.Px(66f),
        Flow = new LayoutFlow {
            Direction = LayoutFlowDirection.Row,
            Wrap = false,
            Justify = LayoutFlowJustify.SpaceBetween,
            Align = LayoutFlowAlign.Start
        }
    };

    /// <summary>Height of the fade band at the top of the window. VGA 16.</summary>
    public LayoutLength FadeTopBand { get; set; } = LayoutLength.Px(96f);

    /// <summary>Height of the fade band at the bottom of the window. VGA 17.</summary>
    public LayoutLength FadeBottomBand { get; set; } = LayoutLength.Px(102f);

    /// <summary>Credits glyph size. VGA cell height 8. Only <see cref="LayoutLengthUnit.Px"/> is
    /// meaningful here — <c>Percent</c>/<c>Auto</c> on a font size are untested and probably
    /// nonsense (UI Toolkit's <c>fontSize</c> style takes a length, not a percentage of a
    /// meaningful parent axis).</summary>
    public LayoutLength FontSize { get; set; } = LayoutLength.Px(48f);

    /// <summary>Spacing between dots of the leader that joins role to name. The original
    /// draws one pixel every 4 VGA px.</summary>
    public LayoutLength LeaderDotPitch { get; set; } = LayoutLength.Px(20f);

    /// <summary>Radius of a single leader dot — half a VGA px horizontally.</summary>
    public LayoutLength LeaderDotRadius { get; set; } = LayoutLength.Px(2.5f);

    /// <summary>
    /// Local x of the first leader dot, given where the leader element starts inside its row.
    ///
    /// <para>Dot phase is GLOBAL: the original steps absolute screen x on a 4-VGA-px grid
    /// (<c>scrollCredits</c> @0x40888-0x408B3), so every row's dots land on the same multiples and
    /// read as vertical columns down the credits. Phasing from each row's own leader element
    /// instead — whose position moves with the role text width — makes the columns drift apart,
    /// which is what "the leaders look wrong" was reporting.</para>
    ///
    /// <para>Returns the first grid point at or after <paramref name="bandStart" />, expressed in
    /// the leader's local space. That is equivalent to the original's snap-down-then-clip without
    /// emitting dots it would only clip away.</para>
    /// </summary>
    /// <param name="originInRow">The leader element's offset inside its row.</param>
    /// <param name="bandStart">Where the dot band starts, in the leader's local space.</param>
    /// <param name="pitch">Dot spacing (<see cref="LeaderDotPitch" />).</param>
    public static float FirstDotOffset(float originInRow, float bandStart, float pitch) {
        if (pitch <= 0f) {
            return bandStart;
        }
        float absolute = originInRow + bandStart;
        float firstAbsolute = (float)(System.Math.Ceiling(absolute / pitch) * pitch);

        return firstAbsolute - originInRow;
    }

    /// <summary>Clearance between the ROLE text and the first leader dot. VGA 2.</summary>
    /// <remarks>
    /// The original's dot band starts at <c>rowX + roleWidth + 24</c> (scrollCredits @0x40876),
    /// and the role itself is drawn at <c>rowX + 22</c> (@0x40797), so the clearance past the
    /// role's end is <c>24 - 22 = 2</c> VGA px. The 24 is not a gap — it absorbs the role's own
    /// x-offset, which is easy to misread as a large clearance.
    /// </remarks>
    public LayoutLength LeaderGap { get; set; } = LayoutLength.Px(10f);

    /// <summary>Clearance between the last leader dot and the NAME text. VGA 4.</summary>
    /// <remarks>
    /// Deliberately NOT the same as <see cref="LeaderGap"/>: the original ends the band at
    /// <c>nameLeft - 4</c> (@0x4085E) while starting it 2 px past the role, so the two sides are
    /// genuinely asymmetric. Both used to be 10 here.
    /// </remarks>
    public LayoutLength LeaderGapName { get; set; } = LayoutLength.Px(20f);
}
