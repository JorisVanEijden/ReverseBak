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
/// <para>The defaults are the faithful values: an override that omits a property still gets the
/// original geometry.</para>
/// </summary>
public class CreditsLayout {
    /// <summary>Baseline of the centred title above the scroll region. VGA y=41.</summary>
    public LayoutLength TitleY { get; set; } = LayoutLength.Px(246f);

    /// <summary>Top of the scrolling window; lines above this are clipped. VGA y=54.</summary>
    public LayoutLength WindowTop { get; set; } = LayoutLength.Px(324f);

    /// <summary>Bottom of the scrolling window. VGA y=158.</summary>
    public LayoutLength WindowBottom { get; set; } = LayoutLength.Px(948f);

    /// <summary>Vertical advance between successive credit lines. VGA 11.</summary>
    public LayoutLength LineHeight { get; set; } = LayoutLength.Px(66f);

    /// <summary>Left edge of the role column. VGA x=42.</summary>
    public LayoutLength RoleLeftX { get; set; } = LayoutLength.Px(210f);

    /// <summary>Right edge the name column is flushed against. VGA x=277.</summary>
    public LayoutLength NameRightX { get; set; } = LayoutLength.Px(1385f);

    /// <summary>Horizontal centre used for the title and for centred closing lines. VGA x=160.</summary>
    public LayoutLength CenterX { get; set; } = LayoutLength.Px(800f);

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

    /// <summary>Clearance between the text and the nearest leader dot. VGA 2.</summary>
    public LayoutLength LeaderGap { get; set; } = LayoutLength.Px(10f);
}
