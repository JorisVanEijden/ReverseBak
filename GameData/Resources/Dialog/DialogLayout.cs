namespace GameData.Resources.Dialog;

using GameData.Resources.Layout;

/// <summary>
/// Intra-panel geometry of a DDX dialog box, in design-frame (1600x1200) px — everything the
/// renderer needs to place the speaker pill and the body text INSIDE the box, plus the paint
/// scalars for the chrome and text drop shadows.
///
/// <para>None of these values are read from a game file. Like <see cref="Credits.CreditsLayout"/>
/// and <see cref="Inventory.InventoryLayout"/>, they are immediate operands in the original's
/// renderers — <c>dialog_DrawChrome</c> (KRONDOR.EXE 0x48632), <c>RenderDialogText</c> (0x48d7b)
/// and <c>dialog_DrawNameBubble</c> (0x488a5) — so they are transcribed here rather than parsed.
/// Converted from VGA once (x5 horizontal / x6 vertical, see AspectCorrection) so the renderer
/// needs no knowledge of the original 320x200 display; each property's remarks carry the raw VGA
/// number and the factor applied to it, so every literal below stays checkable against the
/// binary.</para>
///
/// <para><b>Why it hangs off <see cref="DialogStyleTable"/>.</b> The table is the dialog
/// resource (<c>DIALSTYL.DAT</c>), and these numbers are dialog geometry — a mod author who
/// moves the box with <c>DefaultArea</c> and then wants the speaker pill to sit differently
/// inside it should be editing one document, not two. They are per-TABLE rather than per-ROW
/// because the original has exactly one copy of each: <c>RenderDialogText</c> and
/// <c>dialog_DrawNameBubble</c> read no per-style field for any of them.</para>
///
/// <para><b>The defaults are the faithful values</b>, so an override that omits a property still
/// gets the original geometry, and <c>new DialogLayout()</c> is the shipped dialog.</para>
///
/// <para><b>On the mixture of types.</b> Only values a percentage can actually RESOLVE are
/// <see cref="LayoutLength"/>/<see cref="LayoutPadding"/>: an inset and a padding are measured
/// against a parent UI Toolkit knows how to measure, so <c>"12%"</c> means something. The border
/// widths and shadow offsets are plain <c>float</c> for the same reason
/// <c>InventoryLayout.TextShadowOffsetX/Y</c> and <c>ContainerBorderWidthX/Y</c> are: UI Toolkit
/// border widths are px-only, and a shadow offset is added to a position, which needs a resolved
/// parent size, i.e. a layout solver. Typing them as <see cref="LayoutLength"/> would let an
/// override write <c>"1%"</c> and have the unit silently discarded — a worse lie than the missing
/// expressiveness.</para>
///
/// <para>The test is whether the value is APPLIED ON ITS OWN, not whether it looks like an
/// inset. <see cref="SpeakerToBodyGap"/> looks exactly like one and is a <c>float</c>, because
/// it is only ever a TERM in a px sum and is never handed to UI Toolkit by itself — see its
/// remarks.</para>
/// </summary>
public class DialogLayout {
    /// <summary>
    /// The full-width row that centres the speaker pill horizontally (the original draws the
    /// bubble centred on x~160) and anchors its TOP just inside the dialog area's top edge, so the
    /// whole pill sits within the area — below the cutscene image — and never overlaps the
    /// cutscene viewport. Top inset VGA 1 -> canonical 6; Left/Right 0 make the row span the
    /// panel, which is what gives <see cref="LayoutFlowJustify.Center"/> something to centre in.
    /// </summary>
    public LayoutHint SpeakerPillRow { get; set; } = new LayoutHint {
        Left = LayoutLength.Px(0f),
        Top = LayoutLength.Px(6f),
        Right = LayoutLength.Px(0f),
        Flow = new LayoutFlow {
            Direction = LayoutFlowDirection.Row,
            Wrap = false,
            Justify = LayoutFlowJustify.Center,
            Align = LayoutFlowAlign.Center,
        },
    };

    /// <summary>
    /// The pill itself: an in-flow box that hugs its label plus <see cref="LayoutHint.Padding"/>,
    /// mirroring the original's <c>width = getStringWidthInPixels(name) + 10</c> plus its
    /// semicircular caps (<c>dialog_DrawNameBubble</c> @0x488a5). The padding gives the box
    /// generous horizontal width (VGA 18 x5 -> 90) and a little vertical room above/below the text
    /// (VGA 3 x6 -> 18), so the pill reads as ~1.75x the glyph height like the original's.
    ///
    /// <para><see cref="LayoutPosition.InFlow"/> is load-bearing: the pill is placed by
    /// <see cref="SpeakerPillRow"/>'s centring flow, not by insets of its own. Its padding is the
    /// one value on this type that IS a <see cref="LayoutPadding"/> rather than a float, because
    /// UI Toolkit's <c>paddingLeft</c>/etc. genuinely accept <c>Length.Percent</c> — see
    /// <see cref="LayoutPadding"/>'s own remarks.</para>
    /// </summary>
    public LayoutHint SpeakerPill { get; set; } = new LayoutHint {
        Position = LayoutPosition.InFlow,
        Flow = new LayoutFlow {
            Direction = LayoutFlowDirection.Row,
            Wrap = false,
            Justify = LayoutFlowJustify.Center,
            Align = LayoutFlowAlign.Center,
        },
        Padding = new LayoutPadding {
            Left = LayoutLength.Px(90f),
            Top = LayoutLength.Px(18f),
            Right = LayoutLength.Px(90f),
            Bottom = LayoutLength.Px(18f),
        },
    };

    /// <summary>
    /// How far down-RIGHT the pill's drop shadow is offset, in design-frame px — the original lays
    /// a 1 px pen-1 rim under the bubble for a raised look (the +113/+120 fills in
    /// <c>dialog_DrawNameBubble</c> @0x488a5).
    ///
    /// <para><b>One scalar for BOTH axes, carrying the VERTICAL factor (VGA 1 x6 -> 6).</b> That
    /// is what ships and what the reference screenshots were verified against; it is preserved
    /// deliberately rather than "corrected" to a per-axis 5/6 pair, which would move shipped
    /// pixels. If a future change wants per-axis rims, that is a second property and a deliberate
    /// pixel move, not a silent fix here.</para>
    /// </summary>
    public float SpeakerPillShadowOffset { get; set; } = 6f;

    /// <summary>
    /// Width of the pill's outline (pen 0x0F edge lines / cap arcs, <c>dialog_DrawNameBubble</c>
    /// @0x4896b) on all four edges, in design-frame px. VGA 1 x6 -> 6, the vertical factor on both
    /// axes for the same reason <see cref="SpeakerPillShadowOffset"/> uses it.
    ///
    /// <para>Deliberately its OWN property rather than a reference to
    /// <see cref="ChromeBorderWidth"/>, which happens to carry the same 6 in the shipped data: the
    /// pill rim comes from <c>dialog_DrawNameBubble</c> and the panel bevel from
    /// <c>dialog_DrawChrome</c> — two independent routines in the original — so an author taking
    /// the dialog frame off must not silently lose the name bubble's outline too. (Before this
    /// existed it was a bare <c>6</c> literal in the renderer with a comment pointing at
    /// <c>ChromeBorderWidth</c>, which meant it moved with neither.)</para>
    /// </summary>
    public float SpeakerPillBorderWidth { get; set; } = 6f;

    /// <summary>
    /// Top inset of the body text when the entry has NO speaker — the tuned offset the borderless
    /// narrative strips (<c>PlainWithoutBox</c> / <c>PlainFullScreen</c>) keep. VGA 30 x6 -> 180.
    ///
    /// <para>Also the fallback the renderer falls back to when a speaker IS present but
    /// <see cref="SpeakerTop"/> is a percentage, so the body's top inset cannot be summed (see
    /// <see cref="SpeakerToBodyGap"/>).</para>
    /// </summary>
    public LayoutLength NarrativeBodyTop { get; set; } = LayoutLength.Px(180f);

    /// <summary>
    /// Top inset of the speaker name (the plain centred title branch of
    /// <c>RenderDialogText</c> @0x48e58) — and the first term of the body's own top inset when a
    /// speaker is present. VGA 6 x6 -> 36.
    /// </summary>
    public LayoutLength SpeakerTop { get; set; } = LayoutLength.Px(36f);

    /// <summary>
    /// Clearance between the bottom of the speaker line and the top of the body text, in
    /// design-frame px. VGA 20 x6 -> 120.
    ///
    /// <para><b>Plain <c>float</c>, not a <see cref="LayoutLength"/>, and that is the rule three
    /// paragraphs up being obeyed rather than broken.</b> This value has NO independent consumer:
    /// it exists only inside the sum <see cref="SpeakerTop"/> + the body font size + this, which
    /// the renderer computes in design-frame px and which refuses a percentage outright (a
    /// percentage cannot be added to a px font size without measuring the parent). So a
    /// percentage here could never resolve — typing it as a <see cref="LayoutLength"/> would let
    /// an author write <c>"9.7%"</c>, watch the unit survive every round trip, and then have the
    /// renderer discard the whole sum and fall back to <see cref="NarrativeBodyTop"/>. That is
    /// the "silently discarded unit" lie the border widths and shadow offsets are floats to
    /// avoid.</para>
    ///
    /// <para>Contrast <see cref="SpeakerTop"/>, which stays a <see cref="LayoutLength"/> because
    /// it IS applied on its own — it is the speaker label's own top inset, where a percentage
    /// resolves against the panel and arrives intact even when the body's sum is refused.</para>
    /// </summary>
    public float SpeakerToBodyGap { get; set; } = 120f;

    /// <summary>
    /// Width of the panel's bevelled border on all four edges, in design-frame px
    /// (<c>dialog_DrawChrome</c> @0x48632 draws a 1 px border, highlight pen on top+right, border
    /// pen on left+bottom). VGA 1 x6 -> 6: the VERTICAL factor on both axes, which is what ships
    /// and is preserved deliberately — a per-axis 5/6 pair would move shipped pixels.
    /// </summary>
    public float ChromeBorderWidth { get; set; } = 6f;

    /// <summary>
    /// How far the panel's drop shadow is offset down-LEFT (x is negated by the renderer), in
    /// design-frame px — the 1 px black shadow sitting just outside the border on the left and
    /// bottom. VGA 1 x6 -> 6, the vertical factor on both axes, preserved for the same reason as
    /// <see cref="ChromeBorderWidth"/>.
    /// </summary>
    public float ChromeShadowOffset { get; set; } = 6f;

    /// <summary>
    /// How far right the drop shadow under dialog text is offset, in design-frame px. The original
    /// draws the string twice, the back pass at (+1,+1), for both the body
    /// (<c>RenderDialogText</c> @0x490bf) and the speaker name (@0x48e87 /
    /// <c>dialog_DrawNameBubble</c>). One original pixel -> canonical 5.
    /// </summary>
    public float TextShadowOffsetX { get; set; } = 5f;

    /// <summary>
    /// How far down the drop shadow under dialog text is offset, in design-frame px. One original
    /// pixel -> canonical 6. Different from <see cref="TextShadowOffsetX"/> because the original's
    /// pixels were not square — this is the one shadow on this type that IS per-axis, because it
    /// is the one the renderer applies as a 2-D offset in both factors.
    /// </summary>
    public float TextShadowOffsetY { get; set; } = 6f;
}
