namespace GameData.Resources.Inventory;

/// <summary>
/// When a press on an inventory item becomes a DRAG, and what the screen does when it does —
/// <c>invui_handle_item_drag</c> (INVENTOR.C:609).
/// </summary>
/// <remarks>
/// <b>These were on TASK-30's "still guessed, wants a side-by-side capture" list.</b> They are not
/// guesses that need an eye: the routine states all of them, and our values differ from it in kind
/// rather than in degree.
/// </remarks>
public static class InventoryDragGesture {
    /// <summary>
    /// The original's threshold: <b>4, in VGA pixels, measured as a MANHATTAN distance.</b>
    /// </summary>
    /// <remarks>
    /// <c>dist = abs(cx - init_cx) + abs(cy - init_cy); if (dist > 4)</c>. Two things a port gets
    /// wrong here, and ours got both:
    ///
    /// <para><b>The metric is Manhattan, not Euclidean.</b> The boundary is a diamond, not a circle,
    /// so no single radius reproduces it.</para>
    ///
    /// <para><b>And the space is anisotropic.</b> Canonical x is x5 and y is x6, so 4 VGA px is 20
    /// canonical px across and <b>24 down</b>. A scalar of 20 — which is what we carried, described
    /// in a comment as "the original's own threshold" — is the horizontal vertex of that diamond and
    /// is a fifth too tight vertically.</para>
    ///
    /// <para>Strictly <c>&gt;</c>: a movement of exactly 4 does not start a drag.</para>
    /// </remarks>
    public const int ThresholdVga = 4;

    /// <summary>
    /// Whether a movement starts a drag, <b>in the original's own pixels</b>.
    /// </summary>
    /// <param name="dxVga">Horizontal movement since the press, VGA px.</param>
    /// <param name="dyVga">Vertical movement since the press, VGA px.</param>
    /// <remarks>
    /// <b>The caller converts, and that is deliberate.</b> The display scales are a property of the
    /// canonical frame, which is the Unity layer's business, not this one's — and there is no single
    /// canonical number to convert the threshold INTO, because the two axes scale differently. So
    /// the rule stays in the units the routine wrote it in and the caller divides each axis by its
    /// own scale before asking.
    /// </remarks>
    public static bool StartsDrag(float dxVga, float dyVga) {
        float x = dxVga < 0 ? -dxVga : dxVga;
        float y = dyVga < 0 ? -dyVga : dyVga;
        return x + y > ThresholdVga;
    }

    /// <summary>
    /// <b>The origin cell is EMPTIED, not dimmed.</b>
    /// </summary>
    /// <remarks>
    /// On the frame the drag starts, the routine sets <c>focused-&gt;wSprite_base = 0</c> and
    /// re-renders — the cell's sprite is removed outright. Our screen instead sets the cell's opacity
    /// to 0.25, which reads as a ghost left behind. Same intent, different picture: the original
    /// shows an empty slot with the item on the cursor, so at no point are there two of it.
    ///
    /// <para><b>Except in a shop</b>, where the guard is <c>if (!dragging &amp;&amp; !is_shop)</c> —
    /// so a shop's stock keeps its sprite while you drag a copy of it, which is the correct picture
    /// for goods you have not bought.</para>
    /// </remarks>
    public static bool EmptiesTheOriginCell(bool isShop) => !isShop;

    /// <summary>
    /// The dragged item follows the cursor at its own size, CENTRED on the hotspot.
    /// </summary>
    /// <remarks>
    /// <c>invui_cur_spr_paint_ctrd(invui_item_sprite_select(item_ptr), 0)</c> — the item's own
    /// sprite, no scale argument, centred. So "ghost sizing" is not a choice: it is the icon at 1:1
    /// with its centre under the pointer.
    /// </remarks>
    public static bool GhostIsTheIconAtNaturalSizeCentredOnTheCursor => true;

    /// <summary>
    /// The selected cell's highlight: a filled rectangle, <b>outlined one pixel wide</b>.
    /// </summary>
    /// <remarks>
    /// <c>UI_DrawInventory</c>'s <c>highlight_slot</c> arm sets <c>fill_color = 0x8f</c> and
    /// <c>outline_color = 0x8b</c> and calls <c>draw_rect_filled</c> over the cell's own rect. The
    /// outline is a PEN, so its width is the blitter's one pixel — never a number the screen picks.
    ///
    /// <para><b>One VGA pixel is 5 canonical across and 6 down</b>, so even this has no single
    /// correct width: the border is thicker top-and-bottom than left-and-right, which is what the
    /// anisotropy means. Our <c>OutlineWidth = 8f</c> is neither.</para>
    ///
    /// <para><b>CORRECTION to an earlier reading of mine.</b> I first recorded this outline as a pen
    /// that CYCLES, from <c>(m &gt; 3) ? ('q' - m) : (m + 'k')</c>. Those lines are real but belong
    /// to <c>invui_portr_panel_fill_pulsing</c> and <c>invui_portrait_panel_draw</c> — the PORTRAIT
    /// panel, which does pulse while a drag hovers it. The item cell's own highlight does not: 0x8b
    /// is a constant. Two highlights, two rules, and the pen-cycling lines are nowhere near the
    /// cell-drawing loop.</para>
    /// </remarks>
    public const int OutlineWidthVga = 1;

    /// <summary>INVENTOR.PAL pen for the selected cell's outline. Fixed.</summary>
    public const int SelectedCellOutlinePen = 0x8b;

    /// <summary>INVENTOR.PAL pen the selected cell is filled with.</summary>
    public const int SelectedCellFillPen = 0x8f;

    /// <summary>
    /// The pens the PORTRAIT highlight cycles through while a drag hovers it — a different element
    /// from the item cell, and the one that actually animates.
    /// </summary>
    /// <remarks>
    /// <c>phase % 6</c> indexes <c>(m &gt; 3) ? ('q' - m) : (m + 'k')</c>: 0x6b, 0x6c, 0x6d, 0x6e
    /// then back down through 0x6d, 0x6c. Six steps, out and back, so the pulse has no seam.
    /// </remarks>
    public static readonly int[] PortraitPulsePens = { 0x6b, 0x6c, 0x6d, 0x6e, 0x6d, 0x6c };
}
