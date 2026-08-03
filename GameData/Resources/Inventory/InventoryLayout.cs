namespace GameData.Resources.Inventory;

using GameData.Resources.Layout;

/// <summary>
/// Geometry of the loot/inventory grid screen (<c>REQ_INV</c>), in design-frame (1600x1200) px.
///
/// <para>None of these values are read from REQ_INV.DAT — they are immediate operands in the
/// layout builder <c>sub_ovr157_0</c> (KRONDOR.EXE 0x54210), exactly like the credits screen's
/// geometry (<see cref="Credits.CreditsLayout"/>), so they are transcribed here rather than
/// parsed. Converted from VGA once (x5 horizontal / x6 vertical, see AspectCorrection) so the
/// renderer needs no knowledge of the original 320x200 display.</para>
///
/// <para>Expressed as <see cref="LayoutHint"/> boxes (with a <see cref="LayoutGrid"/> for the
/// fixed-cell grid container) rather than loose named coordinates, so <c>LayoutApplier.Apply</c>
/// can consume them directly and an override can reflow the screen with percentages.</para>
///
/// <para>The defaults are the faithful values: an override that omits a property still gets the
/// original geometry.</para>
/// </summary>
public class InventoryLayout {
    /// <summary>The 7-column x 4-row cell grid container. Origin VGA (14,12) -> canonical
    /// (70,72); one cell 40x30 VGA -> 200x180 canonical (<c>sub_ovr157_0</c> @0x54210). Columns
    /// and rows are cell COUNTS, not lengths, so they are not VGA-scaled.</summary>
    public LayoutHint GridArea { get; set; } = new LayoutHint {
        Left = LayoutLength.Px(70f),
        Top = LayoutLength.Px(72f),
        Grid = new LayoutGrid {
            CellWidth = LayoutLength.Px(200f),
            CellHeight = LayoutLength.Px(180f),
            Columns = 7,
            Rows = 4,
        },
    };

    /// <summary>Member/shop mode: every general (non-equipped) item is nudged this far right,
    /// into the right-hand panel box, clear of the paperdoll's reserved columns. VGA 12 ->
    /// canonical 60 (the member shift, <c>sub_ovr157_0</c> @0x545f3).</summary>
    public LayoutLength MemberShiftX { get; set; } = LayoutLength.Px(60f);

    /// <summary>Loot mode's centering box: the packed cluster of items (unshifted, unlike member
    /// mode) is centered inside this box after packing — the box itself is never drawn. VGA
    /// 307x132 -> canonical 1535x792, anchored at the screen's own origin (the loot centering
    /// pass, <c>sub_ovr157_0</c> @0x54663).</summary>
    public LayoutHint LootBox { get; set; } = new LayoutHint {
        Width = LayoutLength.Px(1535f),
        Height = LayoutLength.Px(792f),
    };

    /// <summary>Paperdoll rect for an equipped sword: one grid row tall. VGA (14,12,80,30) ->
    /// canonical (70,72,400,180) (the paperdoll blacking pass, <c>sub_ovr157_0</c> @0x569e4;
    /// slot selection @0x5451e). Shares row 0 with <see cref="StaffSlot"/> — a member carries
    /// one or the other.</summary>
    public LayoutHint SwordSlot { get; set; } = new LayoutHint {
        Left = LayoutLength.Px(70f),
        Top = LayoutLength.Px(72f),
        Width = LayoutLength.Px(400f),
        Height = LayoutLength.Px(180f),
    };

    /// <summary>Paperdoll rect for an equipped staff: two grid rows tall (row 0). VGA
    /// (14,12,80,60) -> canonical (70,72,400,360) (<c>sub_ovr157_0</c> @0x569e4, @0x5451e).</summary>
    public LayoutHint StaffSlot { get; set; } = new LayoutHint {
        Left = LayoutLength.Px(70f),
        Top = LayoutLength.Px(72f),
        Width = LayoutLength.Px(400f),
        Height = LayoutLength.Px(360f),
    };

    /// <summary>Paperdoll rect for an equipped crossbow: one grid row tall (row 1). VGA
    /// (14,42,80,30) -> canonical (70,252,400,180) (<c>sub_ovr157_0</c> @0x569e4, @0x5451e).</summary>
    public LayoutHint CrossbowSlot { get; set; } = new LayoutHint {
        Left = LayoutLength.Px(70f),
        Top = LayoutLength.Px(252f),
        Width = LayoutLength.Px(400f),
        Height = LayoutLength.Px(180f),
    };

    /// <summary>Paperdoll rect for equipped armor: two grid rows tall (row 2). VGA (14,72,80,60)
    /// -> canonical (70,432,400,360) (<c>sub_ovr157_0</c> @0x569e4, @0x5451e).</summary>
    public LayoutHint ArmorSlot { get; set; } = new LayoutHint {
        Left = LayoutLength.Px(70f),
        Top = LayoutLength.Px(432f),
        Width = LayoutLength.Px(400f),
        Height = LayoutLength.Px(360f),
    };

    // ---- black item-panel fills (UI_DrawInventory @0x5687d) -------------------------------
    // The screen's item area is blacked before anything is drawn into it. Which boxes are painted
    // depends on the mode: loot (and the item-inspect view) paint ONE continuous box; a member's
    // own inventory paints the paperdoll and general boxes separately, leaving the background
    // art's divider showing in the gap between them.

    /// <summary>The paperdoll fill in member mode — and, by construction, the equip drop target:
    /// <c>invui_handle_item_drag</c> (INVENTOR.C:609, compares at 0x57307-0x5731e) accepts a drop
    /// strictly inside this same rect, so the visible box <i>is</i> the target and the two cannot
    /// drift apart. VGA (13,11,82,121) -> canonical (65,66,410,726).</summary>
    public LayoutHint PaperdollBox { get; set; } = new LayoutHint {
        Left = LayoutLength.Px(65f),
        Top = LayoutLength.Px(66f),
        Width = LayoutLength.Px(410f),
        Height = LayoutLength.Px(726f),
    };

    /// <summary>The general-items fill in member mode, right of the paperdoll box. VGA
    /// (105,11,202,121) -> canonical (525,66,1010,726).</summary>
    public LayoutHint GeneralItemsBox { get; set; } = new LayoutHint {
        Left = LayoutLength.Px(525f),
        Top = LayoutLength.Px(66f),
        Width = LayoutLength.Px(1010f),
        Height = LayoutLength.Px(726f),
    };

    /// <summary>The single continuous fill used in loot mode and in the item-inspect view — the
    /// paperdoll and general boxes merged, divider and all. VGA (13,11,294,121) -> canonical
    /// (65,66,1470,726). This is also the rect the original re-clears on every frame of the
    /// icon-flight animation (<c>invinspect_animate_item_move</c>).</summary>
    public LayoutHint FullItemsBox { get; set; } = new LayoutHint {
        Left = LayoutLength.Px(65f),
        Top = LayoutLength.Px(66f),
        Width = LayoutLength.Px(1470f),
        Height = LayoutLength.Px(726f),
    };

    /// <summary>Top-left of the empty-crossbow silhouette (<c>INVSHP2.BMX#10</c>) drawn in member
    /// mode when the slot is unoccupied — skipped for casters, who cannot carry one
    /// (<c>invui_grid_render</c> INVENTOR.C:402-406 / <c>UI_DrawInventory</c> @0x56990). The sprite
    /// keeps its native size, so only a position is needed. VGA (14,43) -> canonical (70,258) —
    /// one original pixel below <see cref="CrossbowSlot"/>'s top edge, as the original draws
    /// it.</summary>
    public LayoutHint CrossbowPlaceholder { get; set; } = new LayoutHint {
        Left = LayoutLength.Px(70f),
        Top = LayoutLength.Px(258f),
    };

    /// <summary>Top-left of the empty-armor silhouette (<c>INVSHP2.BMX#11</c>), drawn in member
    /// mode whenever the slot is unoccupied. VGA (14,73) -> canonical (70,438). There is no
    /// sword/staff placeholder in the original.</summary>
    public LayoutHint ArmorPlaceholder { get; set; } = new LayoutHint {
        Left = LayoutLength.Px(70f),
        Top = LayoutLength.Px(438f),
    };

    // ---- item-inspect view (UI_showItem @0x5A778) ------------------------------------------
    // Four text lines and an icon, each at a named point rather than in a grid. All four lines
    // are drawn with align == 1, which invui_draw_text_aligned_shadow implements as
    // "x -= width/2" — so Left is the line's CENTRE, not its left edge.

    /// <summary>Centre of the inspected item's icon, which is drawn at its native size. Also the
    /// endpoint of the fly-in from the item's grid cell. VGA (58,71) -> canonical (290,426).</summary>
    public LayoutHint InspectIcon { get; set; } = new LayoutHint {
        Left = LayoutLength.Px(290f),
        Top = LayoutLength.Px(426f),
    };

    /// <summary>First line of the item's name — used <b>only when the name needs two lines</b>; a
    /// name that fits on one is drawn at <see cref="InspectNameSecondLine"/> instead, so a
    /// one-line name sits lower rather than higher. Whether a name wraps is a content question
    /// answered by the text layer, not by this geometry. VGA (58,15) -> canonical (290,90).</summary>
    public LayoutHint InspectNameFirstLine { get; set; } = new LayoutHint {
        Left = LayoutLength.Px(290f),
        Top = LayoutLength.Px(90f),
    };

    /// <summary>Second line of a wrapped item name — and where an unwrapped name is drawn.
    /// VGA (58,25) -> canonical (290,150).</summary>
    public LayoutHint InspectNameSecondLine { get; set; } = new LayoutHint {
        Left = LayoutLength.Px(290f),
        Top = LayoutLength.Px(150f),
    };

    /// <summary>The item's type line, under the name. VGA (58,35) -> canonical (290,210).</summary>
    public LayoutHint InspectTypeLine { get; set; } = new LayoutHint {
        Left = LayoutLength.Px(290f),
        Top = LayoutLength.Px(210f),
    };

    /// <summary>The status line ("Using ...", condition, ...), below the icon. Its centre sits one
    /// original pixel right of the other three lines — an asymmetry in the original, preserved.
    /// VGA (59,101) -> canonical (295,606).</summary>
    public LayoutHint InspectStatusLine { get; set; } = new LayoutHint {
        Left = LayoutLength.Px(295f),
        Top = LayoutLength.Px(606f),
    };

    /// <summary>How far right the drop shadow under game-font text is offset. One original pixel
    /// (<c>invui_draw_text_aligned_shadow</c> draws the same string twice, the shadow at +1,+1 in
    /// pen 0) -> canonical 5.</summary>
    public LayoutLength TextShadowOffsetX { get; set; } = LayoutLength.Px(5f);

    /// <summary>How far down the drop shadow under game-font text is offset. One original pixel
    /// -> canonical 6. Different from <see cref="TextShadowOffsetX"/> because the original's
    /// pixels were not square.</summary>
    public LayoutLength TextShadowOffsetY { get; set; } = LayoutLength.Px(6f);

    /// <summary>Horizontal granularity of the inspect icon's flight
    /// (<c>invinspect_animate_item_move</c> @0x5A0CB). The animation is a position <i>lattice</i>,
    /// not a smooth interpolation: each frame advances a twelfth of the remaining distance plus
    /// one step, and the "+1 step" is what puts a floor on the speed and guarantees the icon
    /// arrives. The original's step was one of its own pixels, so a step of canonical 5 reproduces
    /// its pacing exactly; a smaller step makes the tail of the flight visibly slower, not
    /// smoother. Set this to 1px for a genuinely continuous flight.</summary>
    public LayoutLength IconFlightStepX { get; set; } = LayoutLength.Px(5f);

    /// <summary>Vertical granularity of the inspect icon's flight. See
    /// <see cref="IconFlightStepX"/>; canonical 6 = one original pixel down.</summary>
    public LayoutLength IconFlightStepY { get; set; } = LayoutLength.Px(6f);

    // ---- interaction ------------------------------------------------------------------------

    /// <summary>How far a press must travel before it counts as a drag rather than a click
    /// (<c>sub_ovr158_1013</c> @0x57184). Roughly 4 original px -> canonical 20. Compared against
    /// the pointer's travel distance, so it is a single scalar rather than a per-axis pair.</summary>
    public LayoutLength DragThreshold { get; set; } = LayoutLength.Px(20f);

    /// <summary>Left/right border width of the container window's drag-time highlight
    /// (<c>sub_ovr158_3D0</c> @0x56420 draws a one-pixel frame). One original px -> canonical
    /// 5.</summary>
    public LayoutLength ContainerBorderWidthX { get; set; } = LayoutLength.Px(5f);

    /// <summary>Top/bottom border width of the container window's drag-time highlight. One
    /// original px -> canonical 6.</summary>
    public LayoutLength ContainerBorderWidthY { get; set; } = LayoutLength.Px(6f);
}
